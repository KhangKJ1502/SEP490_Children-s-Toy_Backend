using AutoMapper;
using FluentValidation;
using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs.Shifts;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
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

    private static readonly byte StaffRoleId = 3;
    private static readonly byte MerchRoleId = 4;

    public WorkScheduleService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IValidator<CreateWorkScheduleDto> createValidator,
        IValidator<UpdateWorkScheduleDto> updateValidator,
        ICurrentUserService currentUser,
        ITimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
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

        var exists = await _unitOfWork.WorkSchedules.ExistsAsync(dto.AccountId, dto.WorkDate, dto.ShiftTemplateId, cancellationToken);
        if (exists)
        {
            return Result<WorkScheduleDto>.Conflict("Schedule already exists for this account, date, and shift.");
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

    public async Task<Result> MarkAbsentAsync(int scheduleId, CancellationToken cancellationToken = default)
    {
        if (scheduleId <= 0)
        {
            return Result.Failure("VALIDATION_ERROR", "Schedule ID must be greater than 0.");
        }

        var schedule = await _unitOfWork.WorkSchedules.GetByIdForUpdateAsync(scheduleId, cancellationToken);
        if (schedule is null)
        {
            return Result.NotFound("WorkSchedule", scheduleId);
        }

        if (schedule.Status is "Completed" or "Cancelled")
        {
            return Result.Failure("BUSINESS_RULE_VIOLATION", "Cannot mark a completed or cancelled shift as absent.");
        }

        schedule.Status = "Absent";
        schedule.UpdatedAt = _timeProvider.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<WorkScheduleDto>> UpdateAsync(int scheduleId, UpdateWorkScheduleDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
            return Result<WorkScheduleDto>.ValidationFailure(errors);
        }

        var schedule = await _unitOfWork.WorkSchedules.GetByIdForUpdateAsync(scheduleId, cancellationToken);
        if (schedule is null)
        {
            return Result<WorkScheduleDto>.NotFound("WorkSchedule", scheduleId);
        }

        if (schedule.Status is "Completed" or "Cancelled")
        {
            return Result<WorkScheduleDto>.Failure("BUSINESS_RULE_VIOLATION", "Cannot update a completed or cancelled shift.");
        }

        // Validate account role if account changed
        if (schedule.AccountId != dto.AccountId)
        {
            var account = await _unitOfWork.Accounts.GetByIdAsync(dto.AccountId, cancellationToken);
            if (account is null || account.IsDeleted || !account.IsActive)
            {
                return Result<WorkScheduleDto>.NotFound("Account", dto.AccountId);
            }

            if (account.RoleId != StaffRoleId && account.RoleId != MerchRoleId)
            {
                return Result<WorkScheduleDto>.Failure("BUSINESS_RULE_VIOLATION", "Account role must be Staff or Merchandise.");
            }
        }

        // Validate template if template changed
        if (schedule.ShiftTemplateId != dto.ShiftTemplateId)
        {
            var template = await _unitOfWork.ShiftTemplates.GetByIdAsync(dto.ShiftTemplateId, cancellationToken);
            if (template is null)
            {
                return Result<WorkScheduleDto>.NotFound("ShiftTemplate", dto.ShiftTemplateId);
            }

            if (!template.IsActive)
            {
                return Result<WorkScheduleDto>.Failure("BUSINESS_RULE_VIOLATION", "Shift template is inactive.");
            }
        }

        // Check conflict if account/date/template changed
        if (schedule.AccountId != dto.AccountId || schedule.WorkDate != dto.WorkDate.Date || schedule.ShiftTemplateId != dto.ShiftTemplateId)
        {
            var exists = await _unitOfWork.WorkSchedules.ExistsAsync(dto.AccountId, dto.WorkDate, dto.ShiftTemplateId, cancellationToken);
            if (exists)
            {
                return Result<WorkScheduleDto>.Conflict("Schedule already exists for this account, date, and shift.");
            }
        }

        _mapper.Map(dto, schedule);
        schedule.WorkDate = dto.WorkDate.Date;
        schedule.UpdatedAt = _timeProvider.UtcNow;

        await _unitOfWork.WorkSchedules.UpdateAsync(schedule, cancellationToken);
        
        if (dto.MaxLoadOverride.HasValue)
        {
            var capacity = await _unitOfWork.StaffShiftCapacities
                .GetByScheduleIdForUpdateAsync(scheduleId, cancellationToken);
            if (capacity is not null)
            {
                capacity.MaxLoad = dto.MaxLoadOverride.Value;
                capacity.UpdatedAt = _timeProvider.UtcNow;
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        var updated = await _unitOfWork.WorkSchedules.GetByIdAsync(scheduleId, cancellationToken);
        return Result<WorkScheduleDto>.Success(_mapper.Map<WorkScheduleDto>(updated));
    }

    public async Task<Result> DeleteAsync(int scheduleId, CancellationToken cancellationToken = default)
    {
        var schedule = await _unitOfWork.WorkSchedules.GetByIdForUpdateAsync(scheduleId, cancellationToken);
        if (schedule is null)
        {
            return Result.NotFound("WorkSchedule", scheduleId);
        }

        if (schedule.Status is "Completed")
        {
            return Result.Failure("BUSINESS_RULE_VIOLATION", "Cannot delete a completed shift.");
        }

        await _unitOfWork.WorkSchedules.DeleteAsync(schedule, cancellationToken);
        return Result.Success();
    }
}
