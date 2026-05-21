using AutoMapper;
using FluentValidation;
using ToyStore.Application.Constants;
using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs.Assignments;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Services;

public class ShiftAssignmentService : IShiftAssignmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IValidator<AssignQueueOrderRequestDto> _assignQueueValidator;
    private readonly IValidator<ReassignOrderRequestDto> _reassignValidator;
    private readonly IValidator<UpdateShiftCapacityDto> _updateCapacityValidator;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ICurrentUserService _currentUser;
    private readonly ITimeProvider _timeProvider;
    private readonly IShiftCapacityMonitor _shiftCapacityMonitor;

    private const byte StaffRoleId = 3;
    private const byte MerchRoleId = 4;

    public ShiftAssignmentService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IValidator<AssignQueueOrderRequestDto> assignQueueValidator,
        IValidator<ReassignOrderRequestDto> reassignValidator,
        IValidator<UpdateShiftCapacityDto> updateCapacityValidator,
        IDomainEventPublisher eventPublisher,
        ICurrentUserService currentUser,
        ITimeProvider timeProvider,
        IShiftCapacityMonitor shiftCapacityMonitor)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _assignQueueValidator = assignQueueValidator;
        _reassignValidator = reassignValidator;
        _updateCapacityValidator = updateCapacityValidator;
        _eventPublisher = eventPublisher;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _shiftCapacityMonitor = shiftCapacityMonitor;
    }

    public async Task<Result<AssignmentResultDto>> AutoAssignOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        if (orderId <= 0)
        {
            return Result<AssignmentResultDto>.Failure("VALIDATION_ERROR", "Order ID must be greater than 0.");
        }

        var order = await _unitOfWork.Orders.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            return Result<AssignmentResultDto>.NotFound("Order", orderId);
        }

        var activeAssignments = await _unitOfWork.OrderAssignments.GetActiveAssignmentsAsync(orderId, cancellationToken);
        if (activeAssignments.Count > 0)
        {
            var result = new AssignmentResultDto
            {
                Result = "ASSIGNED",
                StaffAccountId = activeAssignments.FirstOrDefault(x => x.RoleId == StaffRoleId)?.AccountId,
                MerchAccountId = activeAssignments.FirstOrDefault(x => x.RoleId == MerchRoleId)?.AccountId
            };
            return Result<AssignmentResultDto>.Success(result);
        }

        var alreadyQueued = await _unitOfWork.OrderQueues.ExistsPendingForOrderAsync(orderId, cancellationToken);
        if (alreadyQueued)
        {
            return Result<AssignmentResultDto>.Success(new AssignmentResultDto { Result = "QUEUED" });
        }

        var assignResult = await _unitOfWork.OrderAssignments.AutoAssignAsync(orderId, null, cancellationToken);

        if (string.Equals(assignResult.Result, "ASSIGNED", StringComparison.OrdinalIgnoreCase))
        {
            if (assignResult.StaffAccountId.HasValue)
            {
                var orderForUpdate = await _unitOfWork.Orders.GetByIdForUpdateAsync(orderId, cancellationToken);
                if (orderForUpdate is not null)
                {
                    orderForUpdate.AssignedToStaffId = assignResult.StaffAccountId.Value;
                    orderForUpdate.UpdatedAt = _timeProvider.UtcNow;
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }

            await _eventPublisher.PublishAsync(
                "Order",
                orderId.ToString(),
                ShiftEventTypes.OrderAssigned,
                new
                {
                    orderId,
                    orderCode = order.OrderCode,
                    staffAccountId = assignResult.StaffAccountId,
                    merchAccountId = assignResult.MerchAccountId
                },
                CancellationToken.None);
        }
        else if (string.Equals(assignResult.Result, "QUEUED", StringComparison.OrdinalIgnoreCase))
        {
            await _eventPublisher.PublishAsync(
                "Order",
                orderId.ToString(),
                ShiftEventTypes.OrderQueued,
                new
                {
                    orderId,
                    orderCode = order.OrderCode,
                    reason = assignResult.Reason
                },
                CancellationToken.None);
        }

        return Result<AssignmentResultDto>.Success(assignResult);
    }

    public async Task<Result> ReleaseCapacityAsync(int orderId, CancellationToken cancellationToken = default)
    {
        if (orderId <= 0)
        {
            return Result.Failure("VALIDATION_ERROR", "Order ID must be greater than 0.");
        }

        await _unitOfWork.OrderAssignments.ReleaseCapacityAsync(orderId, cancellationToken);

        await _eventPublisher.PublishAsync(
            "Order",
            orderId.ToString(),
            ShiftEventTypes.CapacityFreed,
            new { orderId },
            CancellationToken.None);

        return Result.Success();
    }

    public async Task<Result<List<OrderQueueItemDto>>> GetQueueAsync(CancellationToken cancellationToken = default)
    {
        var items = await _unitOfWork.OrderQueues.GetPendingAsync(cancellationToken);
        var dtos = _mapper.Map<List<OrderQueueItemDto>>(items);
        return Result<List<OrderQueueItemDto>>.Success(dtos);
    }

    public async Task<Result> TryAssignOldestQueueAsync(CancellationToken cancellationToken = default)
    {
        var queueEntry = await _unitOfWork.OrderQueues.GetOldestPendingAsync(cancellationToken);
        if (queueEntry is null)
        {
            return Result.Success();
        }

        var assignResult = await _unitOfWork.OrderAssignments.AutoAssignAsync(queueEntry.OrderId, null, cancellationToken);
        if (!string.Equals(assignResult.Result, "ASSIGNED", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Success();
        }

        var now = _timeProvider.UtcNow;
        queueEntry.IsResolved = true;
        queueEntry.ResolvedAt = now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(
            "Order",
            queueEntry.OrderId.ToString(),
            ShiftEventTypes.OrderAssigned,
            new
            {
                orderId = queueEntry.OrderId,
                orderCode = queueEntry.Order?.OrderCode,
                staffAccountId = assignResult.StaffAccountId,
                merchAccountId = assignResult.MerchAccountId
            },
            CancellationToken.None);

        return Result.Success();
    }

    public async Task<Result> AssignQueueAsync(int queueId, AssignQueueOrderRequestDto dto, CancellationToken cancellationToken = default)
    {
        if (queueId <= 0)
        {
            return Result.Failure("VALIDATION_ERROR", "Queue ID must be greater than 0.");
        }

        var validation = await _assignQueueValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
            return Result.ValidationFailure(errors);
        }

        var queueEntry = await _unitOfWork.OrderQueues.GetByIdAsync(queueId, cancellationToken);
        if (queueEntry is null)
        {
            return Result.NotFound("OrderQueue", queueId);
        }

        if (queueEntry.IsResolved)
        {
            return Result.Failure("BUSINESS_RULE_VIOLATION", "Queue entry is already resolved.");
        }

        var staffSchedule = await _unitOfWork.WorkSchedules.GetByIdForUpdateAsync(dto.StaffScheduleId, cancellationToken);
        var merchSchedule = await _unitOfWork.WorkSchedules.GetByIdForUpdateAsync(dto.MerchScheduleId, cancellationToken);

        if (staffSchedule is null || merchSchedule is null)
        {
            return Result.Failure("NOT_FOUND", "Schedule not found.");
        }

        var now = _timeProvider.UtcNow;
        var nowTime = now.TimeOfDay;

        if (!IsScheduleAvailable(staffSchedule, StaffRoleId, now, nowTime)
            || !IsScheduleAvailable(merchSchedule, MerchRoleId, now, nowTime))
        {
            return Result.Failure("BUSINESS_RULE_VIOLATION", "Selected schedules are not available for assignment.");
        }

        if (staffSchedule.StaffShiftCapacity.CurrentLoad >= staffSchedule.StaffShiftCapacity.MaxLoad
            || merchSchedule.StaffShiftCapacity.CurrentLoad >= merchSchedule.StaffShiftCapacity.MaxLoad)
        {
            return Result.Failure("BUSINESS_RULE_VIOLATION", "Selected schedules are at full capacity.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var assignments = new List<OrderAssignment>
            {
                new()
                {
                    OrderId = queueEntry.OrderId,
                    ScheduleId = staffSchedule.ScheduleId,
                    AccountId = staffSchedule.AccountId,
                    RoleId = StaffRoleId,
                    IsActive = true,
                    AssignedAt = now,
                    AssignedBy = _currentUser.AccountId,
                    Notes = dto.Notes
                },
                new()
                {
                    OrderId = queueEntry.OrderId,
                    ScheduleId = merchSchedule.ScheduleId,
                    AccountId = merchSchedule.AccountId,
                    RoleId = MerchRoleId,
                    IsActive = true,
                    AssignedAt = now,
                    AssignedBy = _currentUser.AccountId,
                    Notes = dto.Notes
                }
            };

            await _unitOfWork.OrderAssignments.AddRangeAsync(assignments, cancellationToken);

            staffSchedule.StaffShiftCapacity.CurrentLoad += 1;
            staffSchedule.StaffShiftCapacity.UpdatedAt = now;
            merchSchedule.StaffShiftCapacity.CurrentLoad += 1;
            merchSchedule.StaffShiftCapacity.UpdatedAt = now;

            queueEntry.IsResolved = true;
            queueEntry.AssignedBy = _currentUser.AccountId;
            queueEntry.ResolvedAt = now;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _shiftCapacityMonitor.TryNotifyShiftFullAsync(staffSchedule.ScheduleId, cancellationToken);
            await _shiftCapacityMonitor.TryNotifyShiftFullAsync(merchSchedule.ScheduleId, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        await _eventPublisher.PublishAsync(
            "Order",
            queueEntry.OrderId.ToString(),
            ShiftEventTypes.OrderAssigned,
            new
            {
                orderId = queueEntry.OrderId,
                orderCode = queueEntry.Order?.OrderCode,
                staffAccountId = staffSchedule.AccountId,
                merchAccountId = merchSchedule.AccountId
            },
            CancellationToken.None);

        if (staffSchedule.AccountId > 0)
        {
            var orderForUpdate = await _unitOfWork.Orders.GetByIdForUpdateAsync(queueEntry.OrderId, cancellationToken);
            if (orderForUpdate is not null)
            {
                orderForUpdate.AssignedToStaffId = staffSchedule.AccountId;
                orderForUpdate.UpdatedAt = _timeProvider.UtcNow;
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        return Result.Success();
    }

    public async Task<Result> ReassignOrderAsync(int orderId, ReassignOrderRequestDto dto, CancellationToken cancellationToken = default)
    {
        if (orderId <= 0)
        {
            return Result.Failure("VALIDATION_ERROR", "Order ID must be greater than 0.");
        }

        var validation = await _reassignValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
            return Result.ValidationFailure(errors);
        }

        var schedule = await _unitOfWork.WorkSchedules.GetByIdForUpdateAsync(dto.NewScheduleId, cancellationToken);
        if (schedule is null)
        {
            return Result.NotFound("WorkSchedule", dto.NewScheduleId);
        }

        var now = _timeProvider.UtcNow;
        var nowTime = now.TimeOfDay;

        if (!IsScheduleAvailable(schedule, dto.RoleId, now, nowTime))
        {
            return Result.Failure("BUSINESS_RULE_VIOLATION", "Target schedule is not available.");
        }

        if (schedule.StaffShiftCapacity.CurrentLoad >= schedule.StaffShiftCapacity.MaxLoad)
        {
            return Result.Failure("BUSINESS_RULE_VIOLATION", "Target schedule is at full capacity.");
        }

        await _unitOfWork.OrderAssignments.ReassignAsync(
            orderId,
            dto.RoleId,
            dto.NewScheduleId,
            _currentUser.AccountId,
            dto.Notes,
            cancellationToken);

        if (dto.RoleId == StaffRoleId)
        {
            var orderForUpdate = await _unitOfWork.Orders.GetByIdForUpdateAsync(orderId, cancellationToken);
            if (orderForUpdate is not null)
            {
                orderForUpdate.AssignedToStaffId = schedule.AccountId;
                orderForUpdate.UpdatedAt = _timeProvider.UtcNow;
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        await _eventPublisher.PublishAsync(
            "Order",
            orderId.ToString(),
            ShiftEventTypes.OrderAssigned,
            new
            {
                orderId,
                staffAccountId = dto.RoleId == StaffRoleId ? schedule.AccountId : (int?)null,
                merchAccountId = dto.RoleId == MerchRoleId ? schedule.AccountId : (int?)null
            },
            CancellationToken.None);

        return Result.Success();
    }

    public async Task<Result> UpdateMaxLoadAsync(int scheduleId, UpdateShiftCapacityDto dto, CancellationToken cancellationToken = default)
    {
        if (scheduleId <= 0)
        {
            return Result.Failure("VALIDATION_ERROR", "Schedule ID must be greater than 0.");
        }

        var validation = await _updateCapacityValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
            return Result.ValidationFailure(errors);
        }

        var capacity = await _unitOfWork.StaffShiftCapacities.GetByScheduleIdForUpdateAsync(scheduleId, cancellationToken);
        if (capacity is null)
        {
            return Result.NotFound("StaffShiftCapacity", scheduleId);
        }

        if (dto.MaxLoad < capacity.CurrentLoad)
        {
            return Result.Failure("BUSINESS_RULE_VIOLATION", "Max load cannot be less than current load.");
        }

        capacity.MaxLoad = dto.MaxLoad;
        capacity.UpdatedAt = _timeProvider.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static bool IsScheduleAvailable(WorkSchedule schedule, byte roleId, DateTime now, TimeSpan nowTime)
    {
        if (schedule.Account.RoleId != roleId)
        {
            return false;
        }

        if (schedule.Status != "OnDuty")
        {
            return false;
        }

        if (schedule.WorkDate != now.Date)
        {
            return false;
        }

        var start = schedule.ShiftTemplate.StartTime;
        var end = schedule.ShiftTemplate.EndTime;

        return start <= nowTime && end >= nowTime;
    }
}
