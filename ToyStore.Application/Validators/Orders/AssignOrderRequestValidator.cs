using FluentValidation;
using ToyStore.Application.DTOs.Orders;

namespace ToyStore.Application.Validators.Orders;

public class AssignOrderRequestValidator : AbstractValidator<AssignOrderRequestDto>
{
    public AssignOrderRequestValidator()
    {
        RuleFor(x => x.TargetAccountId)
            .GreaterThan(0).WithMessage("TargetAccountId must be a positive integer.");

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Note must not exceed 500 characters.")
            .When(x => x.Note is not null);
    }
}
