using FluentValidation;
using ToyStore.Application.DTOs.Promotions;

namespace ToyStore.Application.Validators.Promotions;

public class CreateProductPromotionValidator : AbstractValidator<CreateProductPromotionDto>
{
    public CreateProductPromotionValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.")
            .GreaterThan(0).WithMessage("Product ID must be greater than 0.");

        RuleFor(x => x.SalePrice)
            .GreaterThanOrEqualTo(0).WithMessage("Sale price must be greater than or equal to 0.");

        RuleFor(x => x.DiscountPercent)
            .InclusiveBetween(1, 99).WithMessage("Discount percent must be between 1 and 99.")
            .When(x => x.DiscountPercent.HasValue);

        RuleFor(x => x.SaleQuantity)
            .GreaterThan(0).WithMessage("Sale quantity must be greater than 0.")
            .When(x => x.SaleQuantity.HasValue);
    }
}
