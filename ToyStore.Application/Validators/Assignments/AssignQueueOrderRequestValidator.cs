using FluentValidation;
using ToyStore.Application.DTOs.Assignments;

namespace ToyStore.Application.Validators.Assignments;

public class AssignQueueOrderRequestValidator : AbstractValidator<AssignQueueOrderRequestDto>
{
    public AssignQueueOrderRequestValidator()
    {
        RuleFor(x => x.StaffScheduleId)
            .GreaterThan(0).WithMessage("Staff schedule ID must be greater than 0.");

        RuleFor(x => x.MerchScheduleId)
            .GreaterThan(0).WithMessage("Merchandise schedule ID must be greater than 0.");

        When(x => x.Notes is not null, () =>
        {
            RuleFor(x => x.Notes)
                .MaximumLength(200).WithMessage("Notes must not exceed 200 characters.");
        });
    }
}
