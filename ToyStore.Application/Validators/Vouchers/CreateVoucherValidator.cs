using FluentValidation;
using ToyStore.Application.DTOs.Vouchers;

namespace ToyStore.Application.Validators.Vouchers;

/// <summary>
/// Validator kiểm tra tính hợp lệ của dữ liệu tạo mới Voucher (CreateVoucherDto).
/// Bao gồm kiểm tra định dạng, giới hạn ký tự, kiểu giảm giá, mức giảm, thời gian hiệu lực và các quy tắc đặc thù cho FINAL_PRICE.
/// </summary>
public class CreateVoucherValidator : AbstractValidator<CreateVoucherDto>
{
    /// <summary>
    /// Khởi tạo các quy tắc kiểm tra dữ liệu cho CreateVoucherDto.
    /// </summary>
    public CreateVoucherValidator()
    {
        // Kiểm tra mã VoucherCode: bắt buộc, độ dài từ 3 đến 30 ký tự, chỉ chứa chữ cái, số, gạch dưới (_) hoặc gạch nối (-)
        RuleFor(x => x.VoucherCode)
            .NotEmpty().WithMessage("Voucher code is required.")
            .MinimumLength(3).WithMessage("Voucher code must be at least 3 characters.")
            .MaximumLength(30).WithMessage("Voucher code must not exceed 30 characters.")
            .Matches("^[A-Za-z0-9_-]+$").WithMessage("Voucher code must contain only letters, numbers, underscores, or hyphens.");

        // Kiểm tra tên voucher: bắt buộc, độ dài từ 3 đến 255 ký tự
        RuleFor(x => x.VoucherName)
            .NotEmpty().WithMessage("Voucher name is required.")
            .MinimumLength(3).WithMessage("Voucher name must be at least 3 characters.")
            .MaximumLength(255).WithMessage("Voucher name must not exceed 255 characters.");

        // Kiểm tra mô tả voucher: bắt buộc, độ dài từ 3 đến 255 ký tự
        RuleFor(x => x.VoucherDescription)
            .NotEmpty().WithMessage("Voucher description is required.")
            .MinimumLength(3).WithMessage("Voucher description must be at least 3 characters.")
            .MaximumLength(255).WithMessage("Voucher description must not exceed 255 characters.");

        // Kiểm tra loại giảm giá (DiscountType): bắt buộc, phải là FIXED hoặc PERCENTAGE
        RuleFor(x => x.DiscountType)
            .NotEmpty().WithMessage("Discount type is required.")
            .Must(v => VoucherValidatorConstants.AllowedDiscountTypes.Contains(v.Trim()))
            .WithMessage("Discount type must be either FIXED or PERCENTAGE.");

        // Kiểm tra đối tượng giảm giá (DiscountTarget): bắt buộc, phải là ORDER_TOTAL, SHIPPING_FEE, hoặc FINAL_PRICE
        RuleFor(x => x.DiscountTarget)
            .NotEmpty().WithMessage("Discount target is required.")
            .Must(v => VoucherValidatorConstants.AllowedDiscountTargets.Contains(v.Trim()))
            .WithMessage("Discount target must be ORDER_TOTAL, SHIPPING_FEE, or FINAL_PRICE.");

        // Kiểm tra trạng thái voucher (Status): bắt buộc, thuộc danh sách trạng thái cho phép
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(v => VoucherValidatorConstants.AllowedStatuses.Contains(v.Trim()))
            .WithMessage("Status must be one of Scheduled, Active, Inactive, or Expired.");

        // Kiểm tra giá trị giảm (DiscountValue): phải lớn hơn 0 và không vượt quá 1 tỷ VNĐ
        RuleFor(x => x.DiscountValue)
            .GreaterThan(0).WithMessage("Discount value must be greater than 0.")
            .LessThanOrEqualTo(1_000_000_000).WithMessage("Discount value must not exceed 1,000,000,000.");

        // Nếu là loại giảm theo phần trăm (PERCENTAGE): giá trị giảm không được vượt quá 100%
        RuleFor(x => x.DiscountValue)
            .LessThanOrEqualTo(100)
            .When(x => IsPercentage(x.DiscountType))
            .WithMessage("Discount value must be less than or equal to 100 for percentage vouchers.");

        // Kiểm tra mức giảm tối đa (MaxDiscountCap) nếu được truyền: phải lớn hơn 0
        RuleFor(x => x.MaxDiscountCap)
            .GreaterThan(0).When(x => x.MaxDiscountCap.HasValue)
            .WithMessage("Max discount cap must be greater than 0 when provided.");

        // Bắt buộc phải có MaxDiscountCap đối với loại voucher giảm theo phần trăm (PERCENTAGE)
        RuleFor(x => x.MaxDiscountCap)
            .NotNull()
            .When(x => IsPercentage(x.DiscountType))
            .WithMessage("Max discount cap is required for percentage vouchers.");

        // MaxDiscountCap không được thiết lập cho loại FIXED trừ khi đối tượng áp dụng là FINAL_PRICE hoặc SHIPPING_FEE
        RuleFor(x => x.MaxDiscountCap)
            .Null()
            .When(x => IsFixed(x.DiscountType) 
                && !string.Equals(x.DiscountTarget?.Trim(), "FINAL_PRICE", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(x.DiscountTarget?.Trim(), "SHIPPING_FEE", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Max discount cap is only allowed for percentage vouchers, unless the target is FINAL_PRICE or SHIPPING_FEE.");

        // Giá trị đơn hàng tối thiểu (MinOrderAmount): phải >= 0 nếu được truyền
        RuleFor(x => x.MinOrderAmount)
            .GreaterThanOrEqualTo(0).When(x => x.MinOrderAmount.HasValue)
            .WithMessage("Minimum order amount must be greater than or equal to 0 when provided.");

        // Tổng số lượng phát hành (TotalQuantity): phải > 0 nếu được giới hạn
        RuleFor(x => x.TotalQuantity)
            .GreaterThan(0).When(x => x.TotalQuantity.HasValue)
            .WithMessage("Total quantity must be greater than 0 when provided.");

        // Giới hạn số lần dùng trên mỗi tài khoản (MaxUsagePerUser): phải >= 1 nếu có thiết lập
        RuleFor(x => x.MaxUsagePerUser)
            .GreaterThanOrEqualTo((short)1).When(x => x.MaxUsagePerUser.HasValue)
            .WithMessage("Max usage per user must be greater than or equal to 1 when provided.");

        // Ngày bắt đầu (StartDate): bắt buộc; khi tạo mới phải cách thời điểm hiện tại ít nhất 10 phút (trừ trường hợp kiểm tra trong luồng Update)
        RuleFor(x => x.StartDate)
            .NotEqual(default(DateTime)).WithMessage("Start date is required.")
            .Must(d => d >= DateTime.UtcNow.AddMinutes(9))
            .When((x, ctx) => !ctx.RootContextData.ContainsKey("IsUpdate"))
            .WithMessage("Start date must be at least 10 minutes from now.");

        // Ngày kết thúc (EndDate): bắt buộc phải có
        RuleFor(x => x.EndDate)
            .NotEqual(default(DateTime)).WithMessage("End date is required.");

        // Ràng buộc thời gian: StartDate phải xảy ra trước EndDate
        RuleFor(x => x)
            .Must(x => x.StartDate < x.EndDate)
            .WithMessage("Start date must be earlier than end date.");

        // Ràng buộc thời hạn của Voucher: từ 1 đến 30 ngày
        RuleFor(x => x)
            .Must(x => (x.EndDate - x.StartDate).TotalDays >= 1.0)
            .WithMessage("Voucher duration must be at least 1 day.")
            .Must(x => (x.EndDate - x.StartDate).TotalDays <= 30.0)
            .WithMessage("Voucher duration must not exceed 30 days.");

        // Ràng buộc cho loại FIXED: Giá trị giảm (DiscountValue) không được vượt quá giá trị đơn hàng tối thiểu (MinOrderAmount)
        RuleFor(x => x)
            .Must(x => !IsFixed(x.DiscountType) || !x.MinOrderAmount.HasValue || x.DiscountValue <= x.MinOrderAmount.Value)
            .WithMessage("Discount value cannot be greater than the minimum order amount for fixed vouchers.");

        // Ràng buộc số lượng: Số lần dùng tối đa mỗi user không được lớn hơn tổng số lượng voucher phát hành
        RuleFor(x => x)
            .Must(x => !x.TotalQuantity.HasValue || !x.MaxUsagePerUser.HasValue || x.MaxUsagePerUser.Value <= x.TotalQuantity.Value)
            .WithMessage("Max usage per user must be less than or equal to total quantity.");

        // ── Các quy tắc đặc thù cho Voucher loại giảm trên giá cuối (FINAL_PRICE) ──
        // Loại giảm giá bắt buộc phải là FIXED
        RuleFor(x => x.DiscountType)
            .Must(t => string.Equals(t?.Trim(), "FIXED", StringComparison.OrdinalIgnoreCase))
            .When(x => string.Equals(x.DiscountTarget?.Trim(), "FINAL_PRICE", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Discount type must be FIXED for FINAL_PRICE vouchers.");

        // Bắt buộc phải có MinOrderAmount và MinOrderAmount > 0
        RuleFor(x => x.MinOrderAmount)
            .NotNull()
            .When(x => string.Equals(x.DiscountTarget?.Trim(), "FINAL_PRICE", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Minimum order amount is required for FINAL_PRICE vouchers.")
            .GreaterThan(0)
            .When(x => string.Equals(x.DiscountTarget?.Trim(), "FINAL_PRICE", StringComparison.OrdinalIgnoreCase) && x.MinOrderAmount.HasValue)
            .WithMessage("Minimum order amount must be greater than 0 for FINAL_PRICE vouchers.");

        // Bắt buộc phải có giới hạn Tổng số lượng (TotalQuantity)
        RuleFor(x => x.TotalQuantity)
            .NotNull()
            .When(x => string.Equals(x.DiscountTarget?.Trim(), "FINAL_PRICE", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Total quantity limit is required for FINAL_PRICE vouchers.");

        // Bắt buộc MaxUsagePerUser phải chính xác là 1 (mỗi khách hàng chỉ được dùng đúng 1 lần)
        RuleFor(x => x.MaxUsagePerUser)
            .NotNull()
            .When(x => string.Equals(x.DiscountTarget?.Trim(), "FINAL_PRICE", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Max usage per user is required for FINAL_PRICE vouchers.")
            .Equal((short)1)
            .When(x => string.Equals(x.DiscountTarget?.Trim(), "FINAL_PRICE", StringComparison.OrdinalIgnoreCase) && x.MaxUsagePerUser.HasValue)
            .WithMessage("Max usage per user must be exactly 1 for FINAL_PRICE vouchers.");
    }

    /// <summary>
    /// Kiểm tra xem chuỗi DiscountType có phải là PERCENTAGE hay không.
    /// </summary>
    private static bool IsPercentage(string discountType)
        => string.Equals(discountType?.Trim(), "PERCENTAGE", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Kiểm tra xem chuỗi DiscountType có phải là FIXED hay không.
    /// </summary>
    private static bool IsFixed(string discountType)
        => string.Equals(discountType?.Trim(), "FIXED", StringComparison.OrdinalIgnoreCase);
}
