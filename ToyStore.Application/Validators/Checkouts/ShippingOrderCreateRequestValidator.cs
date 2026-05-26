using FluentValidation;
using ToyStore.Application.DTOs.Checkouts;

namespace ToyStore.Application.Validators.Checkouts;

public class ShippingOrderCreateRequestValidator : AbstractValidator<ShippingOrderCreateRequestDto>
{
    private const decimal MaxMoney = 1_000_000_000m;
    private const int MaxWeight = 50_000;
    private const int MaxDimension = 200;

    public ShippingOrderCreateRequestValidator()
    {
        RuleFor(x => x.ClientOrderCode)
            .NotEmpty().WithMessage("Client order code is required.")
            .MinimumLength(3).WithMessage("Client order code must be at least 3 characters.")
            .MaximumLength(50).WithMessage("Client order code must not exceed 50 characters.")
            .Matches("^[A-Za-z0-9_-]+$").WithMessage("Client order code can only contain letters, numbers, underscores, or hyphens.");

        RuleFor(x => x.ToName)
            .NotEmpty().WithMessage("Recipient name is required.")
            .MinimumLength(2).WithMessage("Recipient name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Recipient name must not exceed 100 characters.");

        RuleFor(x => x.ToPhone)
            .NotEmpty().WithMessage("Recipient phone is required.")
            .Matches("^0\\d{9}$").WithMessage("Recipient phone must start with 0 and contain 10 digits.");

        RuleFor(x => x.ToAddress)
            .NotEmpty().WithMessage("Recipient address is required.")
            .MinimumLength(5).WithMessage("Recipient address must be at least 5 characters.")
            .MaximumLength(200).WithMessage("Recipient address must not exceed 200 characters.");

        RuleFor(x => x.ToDistrictId)
            .GreaterThan(0).WithMessage("To district is required.");

        RuleFor(x => x.ToWardCode)
            .NotEmpty().WithMessage("To ward code is required.")
            .MaximumLength(20).WithMessage("To ward code must not exceed 20 characters.");


        RuleFor(x => x.InsuranceValue)
            .GreaterThanOrEqualTo(0).WithMessage("Insurance value must be greater than or equal to 0.")
            .LessThanOrEqualTo(MaxMoney).WithMessage("Insurance value must not exceed 1,000,000,000.");

        RuleFor(x => x.CodAmount)
            .GreaterThanOrEqualTo(0).WithMessage("COD amount must be greater than or equal to 0.")
            .LessThanOrEqualTo(MaxMoney).WithMessage("COD amount must not exceed 1,000,000,000.");

        RuleFor(x => x.Weight)
            .GreaterThan(0).WithMessage("Weight must be greater than 0.")
            .LessThanOrEqualTo(MaxWeight).WithMessage("Weight must not exceed 50000.");

        RuleFor(x => x.Length)
            .GreaterThan(0).WithMessage("Length must be greater than 0.")
            .LessThanOrEqualTo(MaxDimension).WithMessage("Length must not exceed 200.");

        RuleFor(x => x.Width)
            .GreaterThan(0).WithMessage("Width must be greater than 0.")
            .LessThanOrEqualTo(MaxDimension).WithMessage("Width must not exceed 200.");

        RuleFor(x => x.Height)
            .GreaterThan(0).WithMessage("Height must be greater than 0.")
            .LessThanOrEqualTo(MaxDimension).WithMessage("Height must not exceed 200.");

        RuleFor(x => x.Note)
            .MaximumLength(300).WithMessage("Note must not exceed 300 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Note));

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Items are required.")
            .Must(items => items.Count <= 50).WithMessage("Items must not exceed 50.");

        RuleForEach(x => x.Items)
            .SetValidator(new ShippingOrderCreateItemValidator());
    }
}
