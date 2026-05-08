using FluentValidation;
using ToyStore.Application.DTOs.Promotions;

namespace ToyStore.Application.Validators.Promotions;

/// <summary>
/// Validator cho từng sản phẩm trong một time slot FLASH_SALE.
/// Áp dụng khi tạo hoặc cập nhật PromotionProductSlot.
/// </summary>
public class CreatePromotionProductSlotValidator : AbstractValidator<CreatePromotionProductSlotDto>
{
    public CreatePromotionProductSlotValidator()
    {
        // ProductId phải là IDENTITY (> 0)
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("Product ID must be greater than 0.");

        // Giá sale phải dương
        RuleFor(x => x.SalePrice)
            .GreaterThan(0).WithMessage("Sale price must be greater than 0.")
            .LessThanOrEqualTo(100_000_000).WithMessage("Sale price must not exceed 100,000,000 VND.");

        // DiscountPercent — nếu có, phải từ 1–99
        RuleFor(x => x.DiscountPercent)
            .InclusiveBetween(1, 99)
            .WithMessage("Discount percent must be between 1% and 99%.")
            .When(x => x.DiscountPercent.HasValue);

        // SaleQuantity bắt buộc với FLASH_SALE, phải > 0
        RuleFor(x => x.SaleQuantity)
            .GreaterThan(0).WithMessage("Sale quantity must be greater than 0.")
            .LessThanOrEqualTo(100_000).WithMessage("Sale quantity must not exceed 100,000 units.");
    }
}
