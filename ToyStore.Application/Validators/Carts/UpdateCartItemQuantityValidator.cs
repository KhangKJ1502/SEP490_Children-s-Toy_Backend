using FluentValidation;
using ToyStore.Application.DTOs.Carts;

namespace ToyStore.Application.Validators.Carts;

public class UpdateCartItemQuantityValidator : AbstractValidator<UpdateCartItemQuantityDto>
{
    public UpdateCartItemQuantityValidator()
    {
        RuleFor(x => x.Quantity)
            .GreaterThan((short)0)
            .WithMessage("Quantity must be greater than 0.")
            .LessThanOrEqualTo((short)100)
            .WithMessage("Quantity must not exceed 100.");
    }
}
