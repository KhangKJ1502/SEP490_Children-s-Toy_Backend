using FluentValidation;
using ToyStore.Application.DTOs.Shifts;

namespace ToyStore.Application.Validators.Shifts;

public class CreateShiftTemplateValidator : AbstractValidator<CreateShiftTemplateDto>
{
    public CreateShiftTemplateValidator()
    {
        RuleFor(x => x.ShiftName)
            .NotEmpty().WithMessage("Shift name is required.")
            .MinimumLength(2).WithMessage("Shift name must be at least 2 characters.")
            .MaximumLength(50).WithMessage("Shift name must not exceed 50 characters.");

        RuleFor(x => x.MaxOrdersPerShift)
            .GreaterThan((short)0).WithMessage("Max orders per shift must be greater than 0.")
            .LessThanOrEqualTo((short)200).WithMessage("Max orders per shift must not exceed 200.");

        RuleFor(x => x)
            .Must(x => x.EndTime > x.StartTime)
            .WithMessage("End time must be later than start time.");
    }
}
