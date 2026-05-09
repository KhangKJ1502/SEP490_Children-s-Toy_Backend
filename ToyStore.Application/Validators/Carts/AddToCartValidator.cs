using FluentValidation;
using ToyStore.Application.DTOs.Carts;

namespace ToyStore.Application.Validators.Carts;

public class AddToCartValidator : AbstractValidator<AddToCartDto>
{
    public AddToCartValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0)
            .WithMessage("Product ID must be greater than 0.");

        RuleFor(x => x.Quantity)
            .GreaterThan((short)0)
            .WithMessage("Quantity must be greater than 0.")
            .LessThanOrEqualTo((short)100)
            .WithMessage("Quantity must not exceed 100.");
    }
}
