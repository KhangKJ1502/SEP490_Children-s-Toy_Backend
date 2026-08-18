using FluentValidation;
using ToyStore.Application.DTOs.Vouchers;

namespace ToyStore.Application.Validators.Vouchers;

/// <summary>
/// Validator kiểm tra tính hợp lệ của dữ liệu cập nhật từng phần cho Voucher (UpdateVoucherDto).
/// Hỗ trợ kiểm tra có ít nhất một trường được cung cấp và chỉ áp dụng các ràng buộc cho những trường có giá trị khác null.
/// </summary>
public class UpdateVoucherValidator : AbstractValidator<UpdateVoucherDto>
{
    /// <summary>
    /// Khởi tạo các quy tắc kiểm tra cho UpdateVoucherDto.
    /// </summary>
    public UpdateVoucherValidator()
    {
        // Ràng buộc chung: Phải cung cấp ít nhất một trường dữ liệu hoặc cờ IsDeleted để cập nhật
        RuleFor(x => x)
            .Must(HasAtLeastOneField)
            .WithMessage("At least one field must be provided for update.");

        // Kiểm tra mã VoucherCode khi được cung cấp: từ 3 đến 30 ký tự, định dạng an toàn
        RuleFor(x => x.VoucherCode)
            .MinimumLength(3).When(x => x.VoucherCode is not null)
            .WithMessage("Voucher code must be at least 3 characters.")
            .MaximumLength(30).When(x => x.VoucherCode is not null)
            .WithMessage("Voucher code must not exceed 30 characters.")
            .Matches("^[A-Za-z0-9_-]+$").When(x => x.VoucherCode is not null)
            .WithMessage("Voucher code must contain only letters, numbers, underscores, or hyphens.");

        // Kiểm tra tên voucher khi được cung cấp: từ 3 đến 255 ký tự
        RuleFor(x => x.VoucherName)
            .MinimumLength(3).When(x => x.VoucherName is not null)
            .WithMessage("Voucher name must be at least 3 characters.")
            .MaximumLength(255).When(x => x.VoucherName is not null)
            .WithMessage("Voucher name must not exceed 255 characters.");

        // Kiểm tra mô tả voucher khi được cung cấp: từ 3 đến 255 ký tự
        RuleFor(x => x.VoucherDescription)
            .MinimumLength(3).When(x => x.VoucherDescription is not null)
            .WithMessage("Voucher description must be at least 3 characters.")
            .MaximumLength(255).When(x => x.VoucherDescription is not null)
            .WithMessage("Voucher description must not exceed 255 characters.");

        // Kiểm tra loại giảm giá (DiscountType) khi được cung cấp: phải là FIXED hoặc PERCENTAGE
        RuleFor(x => x.DiscountType)
            .Must(v => VoucherValidatorConstants.AllowedDiscountTypes.Contains(v!.Trim()))
            .When(x => x.DiscountType is not null)
            .WithMessage("Discount type must be either FIXED or PERCENTAGE.");

        // Kiểm tra đối tượng giảm giá (DiscountTarget) khi được cung cấp: ORDER_TOTAL, SHIPPING_FEE hoặc FINAL_PRICE
        RuleFor(x => x.DiscountTarget)
            .Must(v => VoucherValidatorConstants.AllowedDiscountTargets.Contains(v!.Trim()))
            .When(x => x.DiscountTarget is not null)
            .WithMessage("Discount target must be ORDER_TOTAL, SHIPPING_FEE, or FINAL_PRICE.");

        // Kiểm tra trạng thái (Status) khi được cung cấp: phải thuộc danh sách trạng thái hợp lệ
        RuleFor(x => x.Status)
            .Must(v => VoucherValidatorConstants.AllowedStatuses.Contains(v!.Trim()))
            .When(x => x.Status is not null)
            .WithMessage("Status must be one of Scheduled, Active, Inactive, or Expired.");

        // Kiểm tra giá trị giảm (DiscountValue) khi được cung cấp: phải > 0 và <= 1 tỷ VNĐ
        RuleFor(x => x.DiscountValue)
            .GreaterThan(0).When(x => x.DiscountValue.HasValue)
            .WithMessage("Discount value must be greater than 0.")
            .LessThanOrEqualTo(1_000_000_000).When(x => x.DiscountValue.HasValue)
            .WithMessage("Discount value must not exceed 1,000,000,000.");

        // Nếu cập nhật DiscountValue của loại PERCENTAGE: giá trị giảm không được vượt quá 100%
        RuleFor(x => x.DiscountValue)
            .LessThanOrEqualTo(100)
            .When(x => x.DiscountValue.HasValue && IsPercentage(x.DiscountType))
            .WithMessage("Discount value must be less than or equal to 100 for percentage vouchers.");

        // Kiểm tra mức giảm tối đa (MaxDiscountCap) khi được cung cấp: phải > 0
        RuleFor(x => x.MaxDiscountCap)
            .GreaterThan(0).When(x => x.MaxDiscountCap.HasValue)
            .WithMessage("Max discount cap must be greater than 0 when provided.");

        // MaxDiscountCap không được phép có giá trị nếu là loại FIXED, trừ khi đối tượng là FINAL_PRICE hoặc SHIPPING_FEE
        RuleFor(x => x.MaxDiscountCap)
            .Null()
            .When(x => IsFixed(x.DiscountType) 
                && x.MaxDiscountCap.HasValue 
                && !string.Equals(x.DiscountTarget?.Trim(), "FINAL_PRICE", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(x.DiscountTarget?.Trim(), "SHIPPING_FEE", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Max discount cap is only allowed for percentage vouchers, unless the target is FINAL_PRICE or SHIPPING_FEE.");

        // Giá trị đơn hàng tối thiểu (MinOrderAmount): phải >= 0 khi được cung cấp
        RuleFor(x => x.MinOrderAmount)
            .GreaterThanOrEqualTo(0).When(x => x.MinOrderAmount.HasValue)
            .WithMessage("Minimum order amount must be greater than or equal to 0 when provided.");

        // Tổng số lượng phát hành (TotalQuantity): phải > 0 khi được cung cấp
        RuleFor(x => x.TotalQuantity)
            .GreaterThan(0).When(x => x.TotalQuantity.HasValue)
            .WithMessage("Total quantity must be greater than 0 when provided.");

        // Giới hạn số lần dùng trên mỗi user (MaxUsagePerUser): phải >= 1 khi được cung cấp
        RuleFor(x => x.MaxUsagePerUser)
            .GreaterThanOrEqualTo((short)1).When(x => x.MaxUsagePerUser.HasValue)
            .WithMessage("Max usage per user must be greater than or equal to 1 when provided.");

        // Ràng buộc thời gian: StartDate phải trước EndDate khi cả hai trường ngày đều được cung cấp
        RuleFor(x => x)
            .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.StartDate.Value < x.EndDate.Value)
            .WithMessage("Start date must be earlier than end date when both dates are provided.");

        // Ràng buộc thời hạn hiệu lực: từ 1 đến 30 ngày khi cả hai trường ngày đều được cung cấp
        RuleFor(x => x)
            .Must(x => (x.EndDate!.Value - x.StartDate!.Value).TotalDays >= 1.0)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("Voucher duration must be at least 1 day.")
            .Must(x => (x.EndDate!.Value - x.StartDate!.Value).TotalDays <= 30.0)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("Voucher duration must not exceed 30 days.");

        // Ràng buộc số lượng: MaxUsagePerUser không được lớn hơn TotalQuantity khi cả hai đều được cung cấp
        RuleFor(x => x)
            .Must(x => !x.TotalQuantity.HasValue || !x.MaxUsagePerUser.HasValue || x.MaxUsagePerUser.Value <= x.TotalQuantity.Value)
            .WithMessage("Max usage per user must be less than or equal to total quantity.");

        // ── Ràng buộc cho FINAL_PRICE trong luồng cập nhật ──
        RuleFor(x => x.DiscountType)
            .Must(t => string.Equals(t?.Trim(), "FIXED", StringComparison.OrdinalIgnoreCase))
            .When(x => string.Equals(x.DiscountTarget?.Trim(), "FINAL_PRICE", StringComparison.OrdinalIgnoreCase) && x.DiscountType is not null)
            .WithMessage("Discount type must be FIXED for FINAL_PRICE vouchers.");

        RuleFor(x => x.MinOrderAmount)
            .NotNull()
            .When(x => string.Equals(x.DiscountTarget?.Trim(), "FINAL_PRICE", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Minimum order amount is required for FINAL_PRICE vouchers.")
            .GreaterThan(0)
            .When(x => string.Equals(x.DiscountTarget?.Trim(), "FINAL_PRICE", StringComparison.OrdinalIgnoreCase) && x.MinOrderAmount.HasValue)
            .WithMessage("Minimum order amount must be greater than 0 for FINAL_PRICE vouchers.");

        RuleFor(x => x.TotalQuantity)
            .NotNull()
            .When(x => string.Equals(x.DiscountTarget?.Trim(), "FINAL_PRICE", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Total quantity limit is required for FINAL_PRICE vouchers.");

        RuleFor(x => x.MaxUsagePerUser)
            .Equal((short)1)
            .When(x => string.Equals(x.DiscountTarget?.Trim(), "FINAL_PRICE", StringComparison.OrdinalIgnoreCase) && x.MaxUsagePerUser.HasValue)
            .WithMessage("Max usage per user must be exactly 1 for FINAL_PRICE vouchers.");

        // Lý do (Reason): tối đa 500 ký tự khi được cung cấp
        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters.")
            .When(x => x.Reason is not null);
    }

    /// <summary>
    /// Kiểm tra xem DTO cập nhật có chứa ít nhất một thuộc tính có giá trị (không null) hay không.
    /// </summary>
    /// <param name="dto">Đối tượng UpdateVoucherDto cần kiểm tra.</param>
    /// <returns>true nếu có ít nhất 1 trường khác null, ngược lại false.</returns>
    private static bool HasAtLeastOneField(UpdateVoucherDto dto)
    {
        return dto.VoucherCode is not null
               || dto.VoucherName is not null
               || dto.VoucherDescription is not null
               || dto.DiscountType is not null
               || dto.DiscountValue.HasValue
               || dto.MaxDiscountCap.HasValue
               || dto.DiscountTarget is not null
               || dto.MinOrderAmount.HasValue
               || dto.TotalQuantity.HasValue
               || dto.MaxUsagePerUser.HasValue
               || dto.StartDate.HasValue
               || dto.EndDate.HasValue
               || dto.Status is not null
               || dto.Reason is not null
               || dto.IsDeleted.HasValue;
    }

    /// <summary>
    /// Kiểm tra xem chuỗi DiscountType có phải là PERCENTAGE hay không.
    /// </summary>
    private static bool IsPercentage(string? discountType)
        => string.Equals(discountType?.Trim(), "PERCENTAGE", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Kiểm tra xem chuỗi DiscountType có phải là FIXED hay không.
    /// </summary>
    private static bool IsFixed(string? discountType)
        => string.Equals(discountType?.Trim(), "FIXED", StringComparison.OrdinalIgnoreCase);
}
