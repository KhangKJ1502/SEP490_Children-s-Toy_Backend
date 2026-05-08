using FluentValidation;
using ToyStore.Application.DTOs.Checkouts;

namespace ToyStore.Application.Validators.Checkouts;

public class CheckoutOrderDraftValidator : AbstractValidator<CheckoutOrderDraftDto>
{
    private const decimal MaxMoney = 1_000_000_000m;

    public CheckoutOrderDraftValidator()
    {
        RuleFor(x => x.AccountId)
            .GreaterThan(0).WithMessage("Account ID must be greater than 0.");

        RuleFor(x => x.StatusId)
            .GreaterThan((byte)0).WithMessage("Status ID must be greater than 0.");

        RuleFor(x => x.OrderCode)
            .NotEmpty().WithMessage("Order code is required.")
            .MaximumLength(50).WithMessage("Order code must not exceed 50 characters.");

        RuleFor(x => x.ShippingName)
            .NotEmpty().WithMessage("Shipping name is required.")
            .MaximumLength(100).WithMessage("Shipping name must not exceed 100 characters.");

        RuleFor(x => x.ShippingPhone)
            .NotEmpty().WithMessage("Shipping phone is required.")
            .Matches("^0\\d{9}$").WithMessage("Shipping phone must start with 0 and contain 10 digits.");

        RuleFor(x => x.ShippingAddress)
            .NotEmpty().WithMessage("Shipping address is required.")
            .MaximumLength(200).WithMessage("Shipping address must not exceed 200 characters.");

        RuleFor(x => x.ShippingWardCode)
            .NotEmpty().WithMessage("Shipping ward code is required.")
            .MaximumLength(20).WithMessage("Shipping ward code must not exceed 20 characters.");

        RuleFor(x => x.ShippingWardName)
            .NotEmpty().WithMessage("Shipping ward name is required.")
            .MaximumLength(100).WithMessage("Shipping ward name must not exceed 100 characters.");

        RuleFor(x => x.ShippingDistrictId)
            .GreaterThan(0).WithMessage("Shipping district ID must be greater than 0.");

        RuleFor(x => x.ShippingDistrictName)
            .NotEmpty().WithMessage("Shipping district name is required.")
            .MaximumLength(100).WithMessage("Shipping district name must not exceed 100 characters.");

        RuleFor(x => x.ShippingProvinceId)
            .GreaterThan(0).WithMessage("Shipping province ID must be greater than 0.");

        RuleFor(x => x.ShippingProvinceName)
            .NotEmpty().WithMessage("Shipping province name is required.")
            .MaximumLength(100).WithMessage("Shipping province name must not exceed 100 characters.");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("Payment method is required.")
            .MaximumLength(30).WithMessage("Payment method must not exceed 30 characters.")
            .Matches("^[A-Za-z0-9_]+$").WithMessage("Payment method can only contain letters, numbers, or underscores.");

        RuleFor(x => x.PaymentStatus)
            .NotEmpty().WithMessage("Payment status is required.")
            .MaximumLength(30).WithMessage("Payment status must not exceed 30 characters.")
            .Matches("^[A-Za-z0-9_]+$").WithMessage("Payment status can only contain letters, numbers, or underscores.");

        RuleFor(x => x.SubTotal)
            .GreaterThanOrEqualTo(0).WithMessage("Sub total must be greater than or equal to 0.")
            .LessThanOrEqualTo(MaxMoney).WithMessage("Sub total must not exceed 1,000,000,000.");

        RuleFor(x => x.VoucherDiscountAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Voucher discount amount must be greater than or equal to 0.")
            .LessThanOrEqualTo(MaxMoney).WithMessage("Voucher discount amount must not exceed 1,000,000,000.");

        RuleFor(x => x.EstimatedShippingFee)
            .GreaterThanOrEqualTo(0).WithMessage("Estimated shipping fee must be greater than or equal to 0.")
            .LessThanOrEqualTo(MaxMoney).WithMessage("Estimated shipping fee must not exceed 1,000,000,000.");

        RuleFor(x => x.TotalAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Total amount must be greater than or equal to 0.")
            .LessThanOrEqualTo(MaxMoney).WithMessage("Total amount must not exceed 1,000,000,000.");
    }
}
