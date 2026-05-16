using FluentValidation;
using ToyStore.Application.DTOs.Vouchers;

namespace ToyStore.Application.Validators.Vouchers;

/// <summary>
/// Validator cho request cập nhật voucher.
/// </summary>
public class UpdateVoucherValidator : AbstractValidator<UpdateVoucherDto>
{
    public UpdateVoucherValidator()
    {
        RuleFor(x => x)
            .Must(HasAtLeastOneField)
            .WithMessage("At least one field must be provided for update.");

        RuleFor(x => x.VoucherCode)
            .MinimumLength(3).When(x => x.VoucherCode is not null)
            .WithMessage("Voucher code must be at least 3 characters.")
            .MaximumLength(30).When(x => x.VoucherCode is not null)
            .WithMessage("Voucher code must not exceed 30 characters.")
            .Matches("^[A-Za-z0-9_-]+$").When(x => x.VoucherCode is not null)
            .WithMessage("Voucher code must contain only letters, numbers, underscores, or hyphens.");

        RuleFor(x => x.VoucherName)
            .MinimumLength(3).When(x => x.VoucherName is not null)
            .WithMessage("Voucher name must be at least 3 characters.")
            .MaximumLength(255).When(x => x.VoucherName is not null)
            .WithMessage("Voucher name must not exceed 255 characters.");

        RuleFor(x => x.VoucherDescription)
            .MinimumLength(3).When(x => x.VoucherDescription is not null)
            .WithMessage("Voucher description must be at least 3 characters.")
            .MaximumLength(255).When(x => x.VoucherDescription is not null)
            .WithMessage("Voucher description must not exceed 255 characters.");

        RuleFor(x => x.DiscountType)
            .Must(v => VoucherValidatorConstants.AllowedDiscountTypes.Contains(v!.Trim()))
            .When(x => x.DiscountType is not null)
            .WithMessage("Discount type must be either FIXED or PERCENTAGE.");

        RuleFor(x => x.DiscountTarget)
            .Must(v => VoucherValidatorConstants.AllowedDiscountTargets.Contains(v!.Trim()))
            .When(x => x.DiscountTarget is not null)
            .WithMessage("Discount target must be either ORDER_TOTAL or SHIPPING_FEE.");

        RuleFor(x => x.Status)
            .Must(v => VoucherValidatorConstants.AllowedStatuses.Contains(v!.Trim()))
            .When(x => x.Status is not null)
            .WithMessage("Status must be one of Scheduled, Active, Inactive, or Expired.");

        RuleFor(x => x.DiscountValue)
            .GreaterThan(0).When(x => x.DiscountValue.HasValue)
            .WithMessage("Discount value must be greater than 0.")
            .LessThanOrEqualTo(1_000_000_000).When(x => x.DiscountValue.HasValue)
            .WithMessage("Discount value must not exceed 1,000,000,000.");

        // Giá trị % không được vượt 100
        RuleFor(x => x.DiscountValue)
            .LessThanOrEqualTo(100)
            .When(x => x.DiscountValue.HasValue && IsPercentage(x.DiscountType))
            .WithMessage("Discount value must be less than or equal to 100 for percentage vouchers.");

        RuleFor(x => x.MaxDiscountCap)
            .GreaterThan(0).When(x => x.MaxDiscountCap.HasValue)
            .WithMessage("Max discount cap must be greater than 0 when provided.");

        // MaxDiscountCap chỉ áp dụng cho loại PERCENTAGE
        RuleFor(x => x.MaxDiscountCap)
            .Null()
            .When(x => IsFixed(x.DiscountType) && x.MaxDiscountCap.HasValue)
            .WithMessage("Max discount cap is only allowed for percentage vouchers.");

        RuleFor(x => x.MinOrderAmount)
            .GreaterThanOrEqualTo(0).When(x => x.MinOrderAmount.HasValue)
            .WithMessage("Minimum order amount must be greater than or equal to 0 when provided.");

        RuleFor(x => x.TotalQuantity)
            .GreaterThan(0).When(x => x.TotalQuantity.HasValue)
            .WithMessage("Total quantity must be greater than 0 when provided.");

        RuleFor(x => x.MaxUsagePerUser)
            .GreaterThanOrEqualTo((short)1).When(x => x.MaxUsagePerUser.HasValue)
            .WithMessage("Max usage per user must be greater than or equal to 1 when provided.");

        // StartDate phải trước EndDate (khi cả 2 đều được cung cấp)
        RuleFor(x => x)
            .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.StartDate.Value < x.EndDate.Value)
            .WithMessage("Start date must be earlier than end date when both dates are provided.");

        // MaxUsagePerUser không được vượt TotalQuantity
        RuleFor(x => x)
            .Must(x => !x.TotalQuantity.HasValue || !x.MaxUsagePerUser.HasValue || x.MaxUsagePerUser.Value <= x.TotalQuantity.Value)
            .WithMessage("Max usage per user must be less than or equal to total quantity.");
    }

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

    private static bool IsPercentage(string? discountType)
        => string.Equals(discountType?.Trim(), "PERCENTAGE", StringComparison.OrdinalIgnoreCase);

    private static bool IsFixed(string? discountType)
        => string.Equals(discountType?.Trim(), "FIXED", StringComparison.OrdinalIgnoreCase);
}
