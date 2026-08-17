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
            .NotEmpty().WithMessage("Mã sản phẩm là bắt buộc.")
            .GreaterThan(0).WithMessage("Mã sản phẩm phải lớn hơn 0.");

        // Kiểm tra giá bán khuyến mãi
        RuleFor(x => x.SalePrice)
            .GreaterThanOrEqualTo(0).WithMessage("Giá khuyến mãi phải lớn hơn hoặc bằng 0.");

        // Kiểm tra phần trăm giảm giá (nếu có)
        RuleFor(x => x.DiscountPercent)
            .InclusiveBetween(1, 99).WithMessage("Phần trăm giảm giá phải nằm trong khoảng từ 1% đến 99%.");
    }
}
