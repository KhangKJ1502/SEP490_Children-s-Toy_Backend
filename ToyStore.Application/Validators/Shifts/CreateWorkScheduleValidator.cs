using FluentValidation;
using ToyStore.Application.DTOs.Shifts;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.Application.Validators.Shifts;

public class CreateWorkScheduleValidator : AbstractValidator<CreateWorkScheduleDto>
{
    public CreateWorkScheduleValidator(ITimeProvider timeProvider, IWorkScheduleShiftRules shiftRules)
    {
        RuleFor(x => x.AccountId)
            .GreaterThan(0).WithMessage("Account ID must be greater than 0.");

        RuleFor(x => x.ShiftTemplateId)
            .GreaterThan((byte)0).WithMessage("Shift template ID must be greater than 0.");

        RuleFor(x => x.WorkDate)
            .NotEmpty().WithMessage("Work date is required.")
            .Must(d => d.Date >= timeProvider.TodayVn)
            .WithMessage("Work date cannot be in the past.");

        RuleFor(x => x).MustAsync(async (dto, ct) =>
                await shiftRules.ValidateConsecutiveShiftAsync(dto.AccountId, dto.ShiftTemplateId, dto.WorkDate.Date, ct))
            .WithMessage("Cannot assign Morning Shift after Evening Shift the previous day (less than 8 hours rest)");

        RuleFor(x => x).MustAsync(async (dto, ct) =>
                await shiftRules.ValidateMinimumCoverageAsync(
                    dto.WorkDate.Date,
                    dto.ShiftTemplateId,
                    excludeScheduleId: null,
                    accountIdForCreate: dto.AccountId,
                    forCreate: true,
                    cancellationToken: ct))
            .WithMessage("Each shift must have at least 1 sales staff and 1 merchandise staff");

        When(x => x.MaxLoadOverride.HasValue, () =>
        {
            RuleFor(x => x.MaxLoadOverride)
                .GreaterThan((short)0).WithMessage("Max load must be greater than 0.")
                .LessThanOrEqualTo((short)200).WithMessage("Max load must not exceed 200.");
        });
    }
}
