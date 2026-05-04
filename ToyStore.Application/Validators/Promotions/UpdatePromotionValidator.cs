using FluentValidation;
using ToyStore.Application.DTOs.Promotions;

namespace ToyStore.Application.Validators.Promotions;

public class UpdatePromotionValidator : AbstractValidator<UpdatePromotionDto>
{
    public UpdatePromotionValidator()
    {
        RuleFor(x => x.PromotionName)
            .NotEmpty().WithMessage("Promotion name is required.")
            .MaximumLength(200).WithMessage("Promotion name must not exceed 200 characters.")
            .When(x => x.PromotionName != null);

        RuleFor(x => x.PromotionType)
            .NotEmpty().WithMessage("Promotion type is required.")
            .MaximumLength(50).WithMessage("Promotion type must not exceed 50 characters.")
            .When(x => x.PromotionType != null);

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => x.Description != null);

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.")
            .When(x => x.StartDate.HasValue);

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required.")
            .GreaterThan(x => x.StartDate!.Value).WithMessage("End date must be greater than start date.")
            .When(x => x.EndDate.HasValue && x.StartDate.HasValue);

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .MaximumLength(50).WithMessage("Status must not exceed 50 characters.")
            .When(x => x.Status != null);

        RuleFor(x => x.Priority)
            .GreaterThanOrEqualTo(0).WithMessage("Priority must be greater than or equal to 0.")
            .When(x => x.Priority.HasValue);

        RuleForEach(x => x.ProductPromotions)
            .SetValidator(new CreateProductPromotionValidator())
            .When(x => x.ProductPromotions != null);
    }
}
