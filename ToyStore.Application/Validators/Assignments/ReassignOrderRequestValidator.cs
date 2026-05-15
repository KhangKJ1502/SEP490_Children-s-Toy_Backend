using FluentValidation;
using ToyStore.Application.DTOs.Assignments;

namespace ToyStore.Application.Validators.Assignments;

public class ReassignOrderRequestValidator : AbstractValidator<ReassignOrderRequestDto>
{
    public ReassignOrderRequestValidator()
    {
        RuleFor(x => x.RoleId)
            .Must(r => r is 3 or 4)
            .WithMessage("Role ID must be 3 (Staff) or 4 (Merchandise).");

        RuleFor(x => x.NewScheduleId)
            .GreaterThan(0).WithMessage("New schedule ID must be greater than 0.");

        When(x => x.Notes is not null, () =>
        {
            RuleFor(x => x.Notes)
                .MaximumLength(200).WithMessage("Notes must not exceed 200 characters.");
        });
    }
}
