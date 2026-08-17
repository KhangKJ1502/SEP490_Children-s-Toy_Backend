using FluentValidation;
using ToyStore.Application.DTOs.Promotions;

namespace ToyStore.Application.Validators.Promotions;

/// <summary>
/// Validator kiểm tra tính hợp lệ của từng dòng sản phẩm trong một khung giờ Flash Sale (CreatePromotionProductSlotDto).
/// </summary>
public class CreatePromotionProductSlotValidator : AbstractValidator<CreatePromotionProductSlotDto>
{
    /// <summary>
    /// Khởi tạo các quy tắc kiểm tra cho sản phẩm trong khung giờ Flash Sale:
    /// - ProductId: Bắt buộc > 0.
    /// - SalePrice: Giá Flash Sale phải > 0 và <= 999,999,999 VNĐ.
    /// - DiscountPercent: Nếu có, phải từ 1% đến 99%.
    /// - SaleQuantity: Số lượng mở bán Flash Sale phải > 0 và <= 100,000 sản phẩm.
    /// </summary>
    public CreatePromotionProductSlotValidator()
    {
        // Kiểm tra mã định danh sản phẩm
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("Mã sản phẩm phải lớn hơn 0.");

        // Kiểm tra giá bán Flash Sale
        RuleFor(x => x.SalePrice)
            .GreaterThan(0).WithMessage("Giá bán Flash Sale phải lớn hơn 0.")
            .LessThanOrEqualTo(999_999_999).WithMessage("Giá bán Flash Sale không được vượt quá 999,999,999 VNĐ.");

        // Kiểm tra tỷ lệ phần trăm giảm giá (nếu có)
        RuleFor(x => x.DiscountPercent)
            .InclusiveBetween(1, 99)
            .WithMessage("Phần trăm giảm giá phải nằm trong khoảng từ 1% đến 99%.")
            .When(x => x.DiscountPercent.HasValue);

        // Kiểm tra số lượng sản phẩm mở bán Flash Sale
        RuleFor(x => x.SaleQuantity)
            .GreaterThan(0).WithMessage("Số lượng mở bán Flash Sale phải lớn hơn 0.")
            .LessThanOrEqualTo(100_000).WithMessage("Số lượng mở bán Flash Sale không được vượt quá 100,000 sản phẩm.");
    }
}
