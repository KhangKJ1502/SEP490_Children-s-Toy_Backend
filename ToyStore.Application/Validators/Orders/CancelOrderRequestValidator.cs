using FluentValidation;
using ToyStore.Application.DTOs.Orders;

namespace ToyStore.Application.Validators.Orders;

public class CancelOrderRequestValidator : AbstractValidator<CancelOrderRequestDto>
{
    public CancelOrderRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Cancellation reason is required.")
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters.");
    }
}
