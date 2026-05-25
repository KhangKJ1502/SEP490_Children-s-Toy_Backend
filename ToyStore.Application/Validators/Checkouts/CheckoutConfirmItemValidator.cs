using FluentValidation;
using ToyStore.Application.DTOs.Checkouts;

namespace ToyStore.Application.Validators.Checkouts;

public class CheckoutConfirmItemValidator : AbstractValidator<CheckoutConfirmItemDto>
{
    public CheckoutConfirmItemValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("Product ID must be greater than 0.");

        RuleFor(x => x.Quantity)
            .Must(quantity => quantity > 0).WithMessage("Quantity must be greater than 0.");
    }
}
