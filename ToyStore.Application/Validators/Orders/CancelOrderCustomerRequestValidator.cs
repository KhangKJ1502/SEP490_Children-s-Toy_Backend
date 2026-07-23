using FluentValidation;
using ToyStore.Application.DTOs.Orders;

namespace ToyStore.Application.Validators.Orders;

public class CancelOrderCustomerRequestValidator : AbstractValidator<CancelOrderCustomerRequestDto>
{
    public CancelOrderCustomerRequestValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Reason));
    }
}
