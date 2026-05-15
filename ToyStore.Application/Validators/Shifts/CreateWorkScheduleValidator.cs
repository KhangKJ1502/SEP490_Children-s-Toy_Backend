using FluentValidation;
using ToyStore.Application.DTOs.Shifts;

namespace ToyStore.Application.Validators.Shifts;

public class CreateWorkScheduleValidator : AbstractValidator<CreateWorkScheduleDto>
{
    public CreateWorkScheduleValidator()
    {
        RuleFor(x => x.AccountId)
            .GreaterThan(0).WithMessage("Account ID must be greater than 0.");

        RuleFor(x => x.ShiftTemplateId)
            .GreaterThan((byte)0).WithMessage("Shift template ID must be greater than 0.");

        RuleFor(x => x.WorkDate)
            .NotEmpty().WithMessage("Work date is required.")
            .Must(d => d.Date >= DateTime.UtcNow.Date)
            .WithMessage("Work date cannot be in the past.");

        When(x => x.MaxLoadOverride.HasValue, () =>
        {
            RuleFor(x => x.MaxLoadOverride)
                .GreaterThan((short)0).WithMessage("Max load must be greater than 0.")
                .LessThanOrEqualTo((short)200).WithMessage("Max load must not exceed 200.");
        });
    }
}
