using FluentValidation;
using ToyStore.Application.DTOs.Vouchers;

namespace ToyStore.Application.Validators.Vouchers;

/// <summary>
/// Validator cho request tạo mới voucher.
/// </summary>
public class CreateVoucherValidator : AbstractValidator<CreateVoucherDto>
{
    public CreateVoucherValidator()
    {
        RuleFor(x => x.VoucherCode)
            .NotEmpty().WithMessage("Voucher code is required.")
            .MinimumLength(3).WithMessage("Voucher code must be at least 3 characters.")
            .MaximumLength(30).WithMessage("Voucher code must not exceed 30 characters.")
            .Matches("^[A-Za-z0-9_-]+$").WithMessage("Voucher code must contain only letters, numbers, underscores, or hyphens.");

        RuleFor(x => x.VoucherName)
            .NotEmpty().WithMessage("Voucher name is required.")
            .MinimumLength(3).WithMessage("Voucher name must be at least 3 characters.")
            .MaximumLength(255).WithMessage("Voucher name must not exceed 255 characters.");

        RuleFor(x => x.VoucherDescription)
            .NotEmpty().WithMessage("Voucher description is required.")
            .MinimumLength(3).WithMessage("Voucher description must be at least 3 characters.")
            .MaximumLength(255).WithMessage("Voucher description must not exceed 255 characters.");

        RuleFor(x => x.DiscountType)
            .NotEmpty().WithMessage("Discount type is required.")
            .Must(v => VoucherValidatorConstants.AllowedDiscountTypes.Contains(v.Trim()))
            .WithMessage("Discount type must be either FIXED or PERCENTAGE.");

        RuleFor(x => x.DiscountTarget)
            .NotEmpty().WithMessage("Discount target is required.")
            .Must(v => VoucherValidatorConstants.AllowedDiscountTargets.Contains(v.Trim()))
            .WithMessage("Discount target must be either ORDER_TOTAL or SHIPPING_FEE.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(v => VoucherValidatorConstants.AllowedStatuses.Contains(v.Trim()))
            .WithMessage("Status must be one of Scheduled, Active, Inactive, or Expired.");

        RuleFor(x => x.DiscountValue)
            .GreaterThan(0).WithMessage("Discount value must be greater than 0.")
            .LessThanOrEqualTo(1_000_000_000).WithMessage("Discount value must not exceed 1,000,000,000.");

        // Giá trị % không được vượt 100
        RuleFor(x => x.DiscountValue)
            .LessThanOrEqualTo(100)
            .When(x => IsPercentage(x.DiscountType))
            .WithMessage("Discount value must be less than or equal to 100 for percentage vouchers.");

        RuleFor(x => x.MaxDiscountCap)
            .GreaterThan(0).When(x => x.MaxDiscountCap.HasValue)
            .WithMessage("Max discount cap must be greater than 0 when provided.");

        RuleFor(x => x.MaxDiscountCap)
            .NotNull()
            .When(x => IsPercentage(x.DiscountType))
            .WithMessage("Max discount cap is required for percentage vouchers.");

        // MaxDiscountCap chỉ áp dụng cho loại PERCENTAGE
        RuleFor(x => x.MaxDiscountCap)
            .Null()
            .When(x => IsFixed(x.DiscountType))
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

        RuleFor(x => x.StartDate)
            .NotEqual(default(DateTime)).WithMessage("Start date is required.")
            .Must(d => d >= DateTime.UtcNow.AddMinutes(9))
            .When((x, ctx) => !ctx.RootContextData.ContainsKey("IsUpdate"))
            .WithMessage("Start date must be at least 10 minutes from now.");

        RuleFor(x => x.EndDate)
            .NotEqual(default(DateTime)).WithMessage("End date is required.");

        // StartDate phải trước EndDate
        RuleFor(x => x)
            .Must(x => x.StartDate < x.EndDate)
            .WithMessage("Start date must be earlier than end date.");

        // DiscountValue không được vượt MinOrderAmount đối với loại FIXED
        RuleFor(x => x)
            .Must(x => !IsFixed(x.DiscountType) || !x.MinOrderAmount.HasValue || x.DiscountValue <= x.MinOrderAmount.Value)
            .WithMessage("Discount value cannot be greater than the minimum order amount for fixed vouchers.");

        // MaxUsagePerUser không được vượt TotalQuantity
        RuleFor(x => x)
            .Must(x => !x.TotalQuantity.HasValue || !x.MaxUsagePerUser.HasValue || x.MaxUsagePerUser.Value <= x.TotalQuantity.Value)
            .WithMessage("Max usage per user must be less than or equal to total quantity.");
    }

    private static bool IsPercentage(string discountType)
        => string.Equals(discountType?.Trim(), "PERCENTAGE", StringComparison.OrdinalIgnoreCase);

    private static bool IsFixed(string discountType)
        => string.Equals(discountType?.Trim(), "FIXED", StringComparison.OrdinalIgnoreCase);
}
