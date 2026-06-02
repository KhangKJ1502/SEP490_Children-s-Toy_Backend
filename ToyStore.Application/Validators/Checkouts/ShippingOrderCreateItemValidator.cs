using FluentValidation;
using ToyStore.Application.Common.Helpers;
using ToyStore.Application.DTOs.Checkouts;

namespace ToyStore.Application.Validators.Checkouts;

public class ShippingOrderCreateItemValidator : AbstractValidator<ShippingOrderCreateItemDto>
{
    private const decimal MaxMoney = 1_000_000_000m;
    private const int MaxWeight = 50_000;

    public ShippingOrderCreateItemValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Item name is required.")
            .MinimumLength(2).WithMessage("Item name must be at least 2 characters.")
            .MaximumLength(200).WithMessage("Item name must not exceed 200 characters.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0.")
            .LessThanOrEqualTo(1000).WithMessage("Quantity must not exceed 1000.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price must be greater than or equal to 0.")
            .LessThanOrEqualTo(MaxMoney).WithMessage("Price must not exceed 1,000,000,000.");

        RuleFor(x => x.Weight)
            .GreaterThan(0).WithMessage("Weight must be greater than 0.")
            .LessThanOrEqualTo(MaxWeight).WithMessage("Weight must not exceed 50000.");

        RuleFor(x => x.Length)
            .GreaterThan(0).WithMessage("Item length must be greater than 0.")
            .LessThanOrEqualTo(GhnShippingLimits.Type5MaxCm)
            .WithMessage($"Item length must not exceed {GhnShippingLimits.Type5MaxCm} cm (GHN shipping limit).")
            .When(x => x.Length > 0);

        RuleFor(x => x.Width)
            .GreaterThan(0).WithMessage("Item width must be greater than 0.")
            .LessThanOrEqualTo(GhnShippingLimits.Type5MaxCm)
            .WithMessage($"Item width must not exceed {GhnShippingLimits.Type5MaxCm} cm (GHN shipping limit).")
            .When(x => x.Width > 0);

        RuleFor(x => x.Height)
            .GreaterThan(0).WithMessage("Item height must be greater than 0.")
            .LessThanOrEqualTo(GhnShippingLimits.Type5MaxCm)
            .WithMessage($"Item height must not exceed {GhnShippingLimits.Type5MaxCm} cm (GHN shipping limit).")
            .When(x => x.Height > 0);
    }
}
