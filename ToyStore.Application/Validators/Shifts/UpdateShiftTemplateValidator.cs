using FluentValidation;
using ToyStore.Application.DTOs.Shifts;

namespace ToyStore.Application.Validators.Shifts;

public class UpdateShiftTemplateValidator : AbstractValidator<UpdateShiftTemplateDto>
{
    public UpdateShiftTemplateValidator()
    {
        When(x => x.ShiftName is not null, () =>
        {
            RuleFor(x => x.ShiftName)
                .NotEmpty().WithMessage("Shift name is required.")
                .MinimumLength(2).WithMessage("Shift name must be at least 2 characters.")
                .MaximumLength(50).WithMessage("Shift name must not exceed 50 characters.");
        });

        When(x => x.MaxOrdersPerShift.HasValue, () =>
        {
            RuleFor(x => x.MaxOrdersPerShift)
                .GreaterThan((short)0).WithMessage("Max orders per shift must be greater than 0.")
                .LessThanOrEqualTo((short)200).WithMessage("Max orders per shift must not exceed 200.");
        });

        When(x => x.StartTime.HasValue || x.EndTime.HasValue, () =>
        {
            RuleFor(x => x)
                .Must(x =>
                {
                    if (!x.StartTime.HasValue || !x.EndTime.HasValue)
                    {
                        return true;
                    }

                    return x.EndTime > x.StartTime;
                })
                .WithMessage("End time must be later than start time.");
        });
    }
}
