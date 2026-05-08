using FluentValidation;
using ToyStore.Application.DTOs.Checkouts;

namespace ToyStore.Application.Validators.Checkouts;

public class ShippingPreviewResponseValidator : AbstractValidator<ShippingPreviewResponse>
{
    public ShippingPreviewResponseValidator()
    {
        RuleFor(x => x.Fee)
            .GreaterThanOrEqualTo(0).WithMessage("Fee must be greater than or equal to 0.");

        RuleFor(x => x.EstimatedDeliveryTime)
            .NotEmpty().WithMessage("Estimated delivery time is required.");
    }
}
