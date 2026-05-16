using FluentValidation;
using ToyStore.Application.DTOs.Checkouts;

namespace ToyStore.Application.Validators.Checkouts;

public class CheckoutConfirmRequestValidator : AbstractValidator<CheckoutConfirmRequestDto>
{
    private const decimal MaxMoney = 1_000_000_000m;

    public CheckoutConfirmRequestValidator()
    {
        // AccountId từ JWT ở controller; client có thể bỏ trống / 0.
        RuleFor(x => x.AccountId)
            .GreaterThanOrEqualTo(0).WithMessage("Account ID must be non-negative.");

        RuleFor(x => x.AddressId)
            .GreaterThan(0).WithMessage("Address ID must be greater than 0.");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("Payment method is required.")
            .MaximumLength(30).WithMessage("Payment method must not exceed 30 characters.")
            .Matches("^[A-Za-z0-9_]+$").WithMessage("Payment method can only contain letters, numbers, or underscores.");

        RuleFor(x => x.VoucherDiscountAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Voucher discount amount must be greater than or equal to 0.")
            .LessThanOrEqualTo(MaxMoney).WithMessage("Voucher discount amount must not exceed 1,000,000,000.");

        RuleFor(x => x.CodValue)
            .GreaterThanOrEqualTo(0).WithMessage("COD value must be greater than or equal to 0.")
            .LessThanOrEqualTo(MaxMoney).WithMessage("COD value must not exceed 1,000,000,000.");

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Note must not exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Note));

        RuleFor(x => x.OrderVoucherCode)
            .MaximumLength(50).WithMessage("Order voucher code must not exceed 50 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.OrderVoucherCode));

        RuleFor(x => x.ShippingVoucherCode)
            .MaximumLength(50).WithMessage("Shipping voucher code must not exceed 50 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.ShippingVoucherCode));

        RuleFor(x => x.VoucherCode)
            .MaximumLength(50).WithMessage("Voucher code must not exceed 50 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.VoucherCode));

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Items are required.")
            .Must(items => items.Count <= 100).WithMessage("Items must not exceed 100.");

        RuleForEach(x => x.Items)
            .SetValidator(new CheckoutConfirmItemValidator());

        // COD: tổng tiền COD do server tính từ đơn, không bắt client gửi CodValue.
    }
}
