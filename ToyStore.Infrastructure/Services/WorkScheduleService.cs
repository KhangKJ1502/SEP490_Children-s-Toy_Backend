using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs.Shifts;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Constants;
using ToyStore.Domain.OrderFulfillment;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Services;

public class WorkScheduleService : IWorkScheduleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateWorkScheduleDto> _createValidator;
    private readonly IValidator<UpdateWorkScheduleDto> _updateValidator;
    private readonly ICurrentUserService _currentUser;
    private readonly ITimeProvider _timeProvider;
    private readonly IShiftAssignmentService _shiftAssignmentService;
    private readonly IWorkScheduleShiftRules _workScheduleShiftRules;
    private readonly ILogger<WorkScheduleService> _logger;

    private static readonly byte StaffRoleId = 3;
    private static readonly byte MerchRoleId = 4;

    private static readonly IReadOnlyCollection<string> StaffTransferStatuses =
        [OrderStatuses.Pending, OrderStatuses.DeliveryFailed];

    private static readonly IReadOnlyCollection<string> MerchTransferStatuses =
        [OrderStatuses.Confirmed, OrderStatuses.Processing];

    private static bool RoleNeedsHandoff(byte roleId, string statusName)
        => roleId == StaffRoleId
            ? !OrderStatusTransitionValidator.StaffRoleWorkComplete(statusName)
            : roleId == MerchRoleId && !OrderStatusTransitionValidator.MerchRoleWorkComplete(statusName);

    private const string MinimumCoverageError =
        "Each shift must have at least 1 sales staff and 1 warehouse staff";

    public WorkScheduleService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IValidator<CreateWorkScheduleDto> createValidator,
        IValidator<UpdateWorkScheduleDto> updateValidator,
        ICurrentUserService currentUser,
        ITimeProvider timeProvider,
        IShiftAssignmentService shiftAssignmentService,
        IWorkScheduleShiftRules workScheduleShiftRules,
        ILogger<WorkScheduleService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _shiftAssignmentService = shiftAssignmentService;
        _workScheduleShiftRules = workScheduleShiftRules;
        _logger = logger;
    }

    public async Task<Result<WorkScheduleDto>> CreateAsync(CreateWorkScheduleDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
            return Result<WorkScheduleDto>.ValidationFailure(errors);
        }

        var account = await _unitOfWork.Accounts.GetByIdAsync(dto.AccountId, cancellationToken);
        if (account is null || account.IsDeleted || !account.IsActive)
        {
            return Result<WorkScheduleDto>.NotFound("Account", dto.AccountId);
        }

        if (account.RoleId != StaffRoleId && account.RoleId != MerchRoleId)
        {
            return Result<WorkScheduleDto>.Failure("BUSINESS_RULE_VIOLATION", "Account role must be Staff or Merchandise.");
        }

        var template = await _unitOfWork.ShiftTemplates.GetByIdAsync(dto.ShiftTemplateId, cancellationToken);
        if (template is null)
        {
            return Result<WorkScheduleDto>.NotFound("ShiftTemplate", dto.ShiftTemplateId);
        }

        if (!template.IsActive)
        {
            return Result<WorkScheduleDto>.Failure("BUSINESS_RULE_VIOLATION", "Shift template is inactive.");
        }

        if (dto.WorkDate.Date < _timeProvider.TodayVn.Date)
        {
            return Result<WorkScheduleDto>.Failure("BUSINESS_RULE_VIOLATION", "Cannot schedule shifts in the past.");
        }

        var existing = await _unitOfWork.WorkSchedules.GetByUniqueKeyForUpdateAsync(dto.AccountId, dto.WorkDate, dto.ShiftTemplateId, cancellationToken);
        if (existing is not null)
        {
            if (existing.Status != "Cancelled")
            {
                return Result<WorkScheduleDto>.Conflict("Schedule already exists for this account, date, and shift.");
            }

            // Reactivate the existing Cancelled schedule to avoid unique constraint violation UQ_WorkSchedules_StaffShiftDay
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                existing.Status = "Scheduled";
                existing.CreatedBy = _currentUser.AccountId;
                existing.UpdatedAt = _timeProvider.UtcNow;

                if (dto.MaxLoadOverride.HasValue)
                {
                    if (existing.StaffShiftCapacity is not null)
                    {
                        existing.StaffShiftCapacity.MaxLoad = dto.MaxLoadOverride.Value;
                        existing.StaffShiftCapacity.UpdatedAt = _timeProvider.UtcNow;
                    }
                }

                await _unitOfWork.WorkSchedules.UpdateAsync(existing, cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                var fullSchedule = await _unitOfWork.WorkSchedules.GetByIdAsync(existing.ScheduleId, cancellationToken);
                return Result<WorkScheduleDto>.Success(_mapper.Map<WorkScheduleDto>(fullSchedule));
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        var schedule = new WorkSchedule
        {
            AccountId = dto.AccountId,
            ShiftTemplateId = dto.ShiftTemplateId,
            WorkDate = dto.WorkDate.Date,
            Status = "Scheduled",
            CreatedBy = _currentUser.AccountId
        };

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var created = await _unitOfWork.WorkSchedules.CreateAsync(schedule, cancellationToken);

            if (dto.MaxLoadOverride.HasValue)
            {
                var capacity = await _unitOfWork.StaffShiftCapacities
                    .GetByScheduleIdForUpdateAsync(created.ScheduleId, cancellationToken);
                if (capacity is not null)
                {
                    capacity.MaxLoad = dto.MaxLoadOverride.Value;
                    capacity.UpdatedAt = _timeProvider.UtcNow;
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            var fullSchedule = await _unitOfWork.WorkSchedules.GetByIdAsync(created.ScheduleId, cancellationToken);
            return Result<WorkScheduleDto>.Success(_mapper.Map<WorkScheduleDto>(fullSchedule));
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Result<List<WorkScheduleListDto>>> GetListAsync(WorkScheduleQueryDto query, CancellationToken cancellationToken = default)
    {
        var items = await _unitOfWork.WorkSchedules.GetListAsync(query.WorkDate, query.Status, query.RoleId, cancellationToken);
        var dtos = _mapper.Map<List<WorkScheduleListDto>>(items);
        return Result<List<WorkScheduleListDto>>.Success(dtos);
    }

    public async Task<Result<MarkAbsentResultDto>> MarkAbsentAsync(int scheduleId, CancellationToken cancellationToken = default)
    {
        if (scheduleId <= 0)
        {
            return Result<MarkAbsentResultDto>.Failure("VALIDATION_ERROR", "Schedule ID must be greater than 0.");
        }

        var schedule = await _unitOfWork.WorkSchedules.GetByIdForUpdateAsync(scheduleId, cancellationToken);
        if (schedule is null)
        {
            return Result<MarkAbsentResultDto>.NotFound("WorkSchedule", scheduleId);
        }

        if (schedule.Status is "Completed" or "Cancelled")
        {
            return Result<MarkAbsentResultDto>.Failure("BUSINESS_RULE_VIOLATION", "Cannot mark a completed or cancelled shift as absent.");
        }

        var assignmentRows = await _unitOfWork.OrderAssignments
            .GetActiveByScheduleWithStatusAsync(scheduleId, cancellationToken);

        schedule.Status = "Absent";
        schedule.UpdatedAt = _timeProvider.UtcNow;

        var toReassignOrderIds = new HashSet<int>();
        var keptOrderIds = new List<int>();

        foreach (var row in assignmentRows)
        {
            var needsHandoff = RoleNeedsHandoff(row.Assignment.RoleId, row.StatusName);

            if (needsHandoff)
            {
                await _unitOfWork.OrderAssignments.DeactivateAssignmentAsync(row.Assignment, cancellationToken);
                toReassignOrderIds.Add(row.Assignment.OrderId);
            }
            else
            {
                keptOrderIds.Add(row.Assignment.OrderId);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var summary = new MarkAbsentResultDto
        {
            AffectedPendingOrderIds = toReassignOrderIds.ToList(),
            KeptOrderIds = keptOrderIds.Distinct().ToList()
        };

        foreach (var orderId in toReassignOrderIds)
        {
            var assignResult = await _shiftAssignmentService.AutoAssignOrderAsync(orderId, cancellationToken);
            if (!assignResult.IsSuccess || assignResult.Data is null)
            {
                continue;
            }

            if (string.Equals(assignResult.Data.Result, "ASSIGNED", StringComparison.OrdinalIgnoreCase))
            {
                summary.ReassignedCount++;
            }
            else if (string.Equals(assignResult.Data.Result, "QUEUED", StringComparison.OrdinalIgnoreCase))
            {
                summary.QueuedCount++;
            }
        }

        return Result<MarkAbsentResultDto>.Success(summary);
    }

    public async Task<Result<UpdateWorkScheduleResultDto>> UpdateAsync(int scheduleId, UpdateWorkScheduleDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
            return Result<UpdateWorkScheduleResultDto>.ValidationFailure(errors);
        }

        var schedule = await _unitOfWork.WorkSchedules.GetByIdForUpdateAsync(scheduleId, cancellationToken);
        if (schedule is null)
        {
            return Result<UpdateWorkScheduleResultDto>.NotFound("WorkSchedule", scheduleId);
        }

        if (schedule.Status is "Completed" or "Cancelled" or "Absent")
        {
            return Result<UpdateWorkScheduleResultDto>.Failure(
                "BUSINESS_RULE_VIOLATION",
                "Cannot update a completed, cancelled, or absent shift.");
        }

        var accountChanging = schedule.AccountId != dto.AccountId;
        var oldAccountId = schedule.AccountId;
        var transferredAt = _timeProvider.UtcNow;
        var transferredOrders = new List<TransferredOrderItemDto>();

        if (accountChanging)
        {
            var oldAccount = await _unitOfWork.Accounts.GetByIdAsync(oldAccountId, cancellationToken);
            if (oldAccount is null || oldAccount.IsDeleted)
            {
                return Result<UpdateWorkScheduleResultDto>.NotFound("Account", oldAccountId);
            }

            var newAccount = await _unitOfWork.Accounts.GetByIdAsync(dto.AccountId, cancellationToken);
            if (newAccount is null || newAccount.IsDeleted || !newAccount.IsActive)
            {
                return Result<UpdateWorkScheduleResultDto>.NotFound("Account", dto.AccountId);
            }

            if (newAccount.RoleId != StaffRoleId && newAccount.RoleId != MerchRoleId)
            {
                return Result<UpdateWorkScheduleResultDto>.Failure(
                    "BUSINESS_RULE_VIOLATION",
                    "Account role must be Staff or Merchandise.");
            }

            if (newAccount.RoleId != oldAccount.RoleId)
            {
                return Result<UpdateWorkScheduleResultDto>.Failure(
                    "BUSINESS_RULE_VIOLATION",
                    "Replacement account must have the same role as the current assignee (Staff or Merchandise).");
            }

            if (dto.ExpectedUpdatedAt.HasValue && schedule.UpdatedAt.HasValue)
            {
                var expected = dto.ExpectedUpdatedAt.Value.ToUniversalTime();
                var actual = schedule.UpdatedAt.Value.ToUniversalTime();
                if (expected != actual)
                {
                    return Result<UpdateWorkScheduleResultDto>.Conflict(
                        "Schedule was modified by another user. Refresh and try again.");
                }
            }
        }

        if (schedule.ShiftTemplateId != dto.ShiftTemplateId)
        {
            var template = await _unitOfWork.ShiftTemplates.GetByIdAsync(dto.ShiftTemplateId, cancellationToken);
            if (template is null)
            {
                return Result<UpdateWorkScheduleResultDto>.NotFound("ShiftTemplate", dto.ShiftTemplateId);
            }

            if (!template.IsActive)
            {
                return Result<UpdateWorkScheduleResultDto>.Failure(
                    "BUSINESS_RULE_VIOLATION",
                    "Shift template is inactive.");
            }
        }

        if (dto.WorkDate.Date < _timeProvider.TodayVn.Date)
        {
            return Result<UpdateWorkScheduleResultDto>.Failure(
                "BUSINESS_RULE_VIOLATION",
                "Cannot schedule shifts in the past.");
        }

        if (schedule.AccountId != dto.AccountId
            || schedule.WorkDate != dto.WorkDate.Date
            || schedule.ShiftTemplateId != dto.ShiftTemplateId)
        {
            var exists = await _unitOfWork.WorkSchedules.ExistsAsync(
                dto.AccountId,
                dto.WorkDate,
                dto.ShiftTemplateId,
                cancellationToken);
            if (exists)
            {
                return Result<UpdateWorkScheduleResultDto>.Conflict(
                    "Schedule already exists for this account, date, and shift (including absent or cancelled rows).");
            }
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (accountChanging)
            {
                var oldAccountForRole = await _unitOfWork.Accounts.GetByIdAsync(oldAccountId, cancellationToken);
                var assignmentRoleId = oldAccountForRole!.RoleId;

                var transferStatuses = assignmentRoleId == StaffRoleId
                    ? StaffTransferStatuses
                    : MerchTransferStatuses;

                var assignedBy = _currentUser.AccountId;
                var auditNote =
                    $"Transferred schedule swap: {oldAccountId} -> {dto.AccountId} at {transferredAt:O}";

                var transferItems = await _unitOfWork.OrderAssignments.TransferAssignmentsToAccountAsync(
                    scheduleId,
                    assignmentRoleId,
                    dto.AccountId,
                    assignedBy,
                    auditNote,
                    transferStatuses,
                    cancellationToken);

                foreach (var item in transferItems)
                {
                    transferredOrders.Add(new TransferredOrderItemDto
                    {
                        OrderId = item.OrderId,
                        OrderCode = item.OrderCode,
                        StatusName = item.StatusName,
                        OldAccountId = oldAccountId,
                        NewAccountId = dto.AccountId
                    });
                }

                if (assignmentRoleId == StaffRoleId)
                {
                    var pendingOrderIds = transferItems
                        .Where(x => x.StatusName == OrderStatuses.Pending)
                        .Select(x => x.OrderId)
                        .Distinct()
                        .ToList();

                    foreach (var orderId in pendingOrderIds)
                    {
                        var order = await _unitOfWork.Orders.GetByIdForUpdateAsync(orderId, cancellationToken);
                        if (order is not null)
                        {
                            order.AssignedToStaffId = dto.AccountId;
                        }
                    }
                }

                var capacity = await _unitOfWork.StaffShiftCapacities
                    .GetByScheduleIdForUpdateAsync(scheduleId, cancellationToken);
                if (capacity is not null)
                {
                    capacity.AccountId = dto.AccountId;
                    capacity.UpdatedAt = transferredAt;
                }

                if (transferredOrders.Count > 0)
                {
                    _logger.LogInformation(
                        "Schedule staff swap: ScheduleId={ScheduleId} OldAccountId={OldAccountId} NewAccountId={NewAccountId} OrderIds={OrderIds} TransferredAt={TransferredAt}",
                        scheduleId,
                        oldAccountId,
                        dto.AccountId,
                        transferredOrders.Select(x => x.OrderId).ToArray(),
                        transferredAt);
                }
            }

            schedule.AccountId = dto.AccountId;
            schedule.ShiftTemplateId = dto.ShiftTemplateId;
            schedule.WorkDate = dto.WorkDate.Date;
            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                schedule.Status = dto.Status;
            }

            schedule.UpdatedAt = transferredAt;

            await _unitOfWork.WorkSchedules.UpdateAsync(schedule, cancellationToken);

            if (dto.MaxLoadOverride.HasValue)
            {
                var capacity = await _unitOfWork.StaffShiftCapacities
                    .GetByScheduleIdForUpdateAsync(scheduleId, cancellationToken);
                if (capacity is not null)
                {
                    capacity.MaxLoad = dto.MaxLoadOverride.Value;
                    capacity.UpdatedAt = transferredAt;
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        var updated = await _unitOfWork.WorkSchedules.GetByIdAsync(scheduleId, cancellationToken);
        var result = new UpdateWorkScheduleResultDto
        {
            Schedule = _mapper.Map<WorkScheduleDto>(updated),
            TransferredCount = transferredOrders.Count,
            TransferredAt = transferredAt,
            TransferredOrders = transferredOrders,
            AutoAssignReassignedCount = 0,
            AutoAssignQueuedCount = 0
        };

        return Result<UpdateWorkScheduleResultDto>.Success(result);
    }

    public async Task<Result> DeleteAsync(int scheduleId, CancellationToken cancellationToken = default)
    {
        var schedule = await _unitOfWork.WorkSchedules.GetByIdForUpdateAsync(scheduleId, cancellationToken);
        if (schedule is null)
        {
            return Result.NotFound("WorkSchedule", scheduleId);
        }

        if (schedule.Status is not "Scheduled")
        {
            return Result.Failure(
                "BUSINESS_RULE_VIOLATION",
                "Only scheduled shifts with no assignments can be cancelled.");
        }

        var activeAssignments = await _unitOfWork.OrderAssignments
            .GetActiveByScheduleWithStatusAsync(scheduleId, cancellationToken);

        if (activeAssignments.Count > 0)
        {
            return Result.Failure(
                "BUSINESS_RULE_VIOLATION",
                "Cannot cancel a shift that has active order assignments.");
        }

        var currentLoad = schedule.StaffShiftCapacity?.CurrentLoad ?? 0;
        if (currentLoad > 0)
        {
            return Result.Failure(
                "BUSINESS_RULE_VIOLATION",
                "Cannot cancel a shift while capacity load is greater than zero.");
        }

        var coverageOk = await _workScheduleShiftRules.ValidateMinimumCoverageAsync(
            schedule.WorkDate.Date,
            schedule.ShiftTemplateId,
            excludeScheduleId: schedule.ScheduleId,
            accountIdForCreate: null,
            forDelete: true,
            cancellationToken: cancellationToken);

        if (!coverageOk)
        {
            return Result.Failure("BUSINESS_RULE_VIOLATION", MinimumCoverageError);
        }

        schedule.Status = "Cancelled";
        schedule.UpdatedAt = _timeProvider.UtcNow;
        await _unitOfWork.WorkSchedules.UpdateAsync(schedule, cancellationToken);

        return Result.Success();
    }

    public async Task<Result<CloneWeekResultDto>> CloneWeekAsync(
        DateTime sourceMonday,
        DateTime targetMonday,
        CancellationToken cancellationToken = default)
    {
        var source = sourceMonday.Date;
        var target = targetMonday.Date;

        if (source.DayOfWeek != DayOfWeek.Monday || target.DayOfWeek != DayOfWeek.Monday)
        {
            return Result<CloneWeekResultDto>.Failure("VALIDATION_ERROR", "Source and target dates must be Mondays.");
        }

        var sourceEnd = source.AddDays(6);
        var todayVn = _timeProvider.TodayVn.Date;

        var sourceRows = await _unitOfWork.WorkSchedules.GetByDateRangeAsync(source, sourceEnd, cancellationToken);

        var summary = new CloneWeekResultDto();

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var row in sourceRows)
            {
                var offsetDays = (row.WorkDate.Date - source).Days;
                var targetDate = target.AddDays(offsetDays);

                if (targetDate < todayVn)
                {
                    summary.Skipped++;
                    summary.Reasons.Add($"Past date skipped: {targetDate:d}");
                    continue;
                }

                var existing = await _unitOfWork.WorkSchedules.GetByUniqueKeyForUpdateAsync(
                    row.AccountId, targetDate, row.ShiftTemplateId, cancellationToken);

                if (existing is not null)
                {
                    if (existing.Status != "Cancelled")
                    {
                        summary.Skipped++;
                        summary.Reasons.Add($"Duplicate: Account {row.AccountId}, {targetDate:d}, shift {row.ShiftTemplateId}.");
                        continue;
                    }

                    // Reactivate instead of inserting duplicate to avoid unique constraint UQ_WorkSchedules_StaffShiftDay
                    existing.Status = "Scheduled";
                    existing.CreatedBy = _currentUser.AccountId;
                    existing.UpdatedAt = _timeProvider.UtcNow;
                    await _unitOfWork.WorkSchedules.UpdateAsync(existing, cancellationToken);
                    summary.Cloned++;
                    continue;
                }

                var clone = new WorkSchedule
                {
                    AccountId = row.AccountId,
                    ShiftTemplateId = row.ShiftTemplateId,
                    WorkDate = targetDate,
                    Status = "Scheduled",
                    CreatedBy = _currentUser.AccountId
                };

                await _unitOfWork.WorkSchedules.CreateAsync(clone, cancellationToken);
                summary.Cloned++;
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return Result<CloneWeekResultDto>.Success(summary);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Result<TransferLoadResultDto>> TransferLoadAsync(
        int sourceScheduleId,
        TransferLoadRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        if (sourceScheduleId <= 0 || dto.TargetScheduleId <= 0)
        {
            return Result<TransferLoadResultDto>.Failure("VALIDATION_ERROR", "Schedule IDs must be greater than 0.");
        }

        if (sourceScheduleId == dto.TargetScheduleId)
        {
            return Result<TransferLoadResultDto>.Failure("VALIDATION_ERROR", "Source and target schedules must differ.");
        }

        var source = await _unitOfWork.WorkSchedules.GetByIdForUpdateAsync(sourceScheduleId, cancellationToken);
        var target = await _unitOfWork.WorkSchedules.GetByIdForUpdateAsync(dto.TargetScheduleId, cancellationToken);

        if (source is null)
        {
            return Result<TransferLoadResultDto>.NotFound("WorkSchedule", sourceScheduleId);
        }

        if (target is null)
        {
            return Result<TransferLoadResultDto>.NotFound("WorkSchedule", dto.TargetScheduleId);
        }

        var sourceAccount = await _unitOfWork.Accounts.GetByIdAsync(source.AccountId, cancellationToken);
        var targetAccount = await _unitOfWork.Accounts.GetByIdAsync(target.AccountId, cancellationToken);

        if (sourceAccount is null || targetAccount is null || sourceAccount.RoleId != targetAccount.RoleId)
        {
            return Result<TransferLoadResultDto>.Failure(
                "BUSINESS_RULE_VIOLATION",
                "Source and target schedules must belong to accounts with the same role.");
        }

        var rows = await _unitOfWork.OrderAssignments.GetActiveByScheduleWithStatusAsync(sourceScheduleId, cancellationToken);
        var result = new TransferLoadResultDto();
        var assignedBy = _currentUser.AccountId;
        var note = dto.Note ?? $"Bulk transfer load to schedule {dto.TargetScheduleId}";

        foreach (var row in rows)
        {
            if (!RoleNeedsHandoff(row.Assignment.RoleId, row.StatusName))
            {
                result.KeptCount++;
                continue;
            }

            await _unitOfWork.OrderAssignments.ReassignAsync(
                row.Assignment.OrderId,
                row.Assignment.RoleId,
                dto.TargetScheduleId,
                assignedBy,
                note,
                cancellationToken);

            result.TransferredCount++;
            result.TransferredOrderIds.Add(row.Assignment.OrderId);
        }

        return Result<TransferLoadResultDto>.Success(result);
    }
}
