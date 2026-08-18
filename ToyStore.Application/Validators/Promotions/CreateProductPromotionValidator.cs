using FluentValidation;
using ToyStore.Application.DTOs.Promotions;

namespace ToyStore.Application.Validators.Promotions;

/// <summary>
/// Validator kiểm tra tính hợp lệ của dữ liệu sản phẩm trong chương trình khuyến mãi trực tiếp (CreateProductPromotionDto).
/// </summary>
public class CreateProductPromotionValidator : AbstractValidator<CreateProductPromotionDto>
{
    /// <summary>
    /// Khởi tạo các quy tắc kiểm tra cho sản phẩm giảm giá:
    /// - ProductId: Bắt buộc và phải lớn hơn 0.
    /// - SalePrice: Giá bán khuyến mãi phải >= 0.
    /// - DiscountPercent: Tỷ lệ phần trăm giảm (nếu có) phải nằm trong khoảng từ 1% đến 99%.
    /// </summary>
    public CreateProductPromotionValidator()
    {
        // Kiểm tra mã định danh sản phẩm
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.")
            .GreaterThan(0).WithMessage("Product ID must be greater than 0.");

        // Kiểm tra giá bán khuyến mãi
        RuleFor(x => x.SalePrice)
            .GreaterThanOrEqualTo(0).WithMessage("Promotion price must be greater than or equal to 0.");

        // Kiểm tra phần trăm giảm giá (nếu có)
        RuleFor(x => x.DiscountPercent)
            .InclusiveBetween(1, 99).WithMessage("Discount percentage must be between 1% and 99%.");
    }
}
