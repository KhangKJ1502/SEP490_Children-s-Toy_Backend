using FluentValidation;
using ToyStore.Application.DTOs.Promotions;

namespace ToyStore.Application.Validators.Promotions;

public class CreatePromotionValidator : AbstractValidator<CreatePromotionDto>
{
    public CreatePromotionValidator()
    {
        RuleFor(x => x.PromotionName)
            .NotEmpty().WithMessage("Promotion name is required.")
            .MaximumLength(200).WithMessage("Promotion name must not exceed 200 characters.");

        RuleFor(x => x.PromotionType)
            .NotEmpty().WithMessage("Promotion type is required.")
            .MaximumLength(50).WithMessage("Promotion type must not exceed 50 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.")
            .Must(d => d >= DateTime.UtcNow.AddMinutes(9))
            .When(x => x.Status == "Scheduled")
            .WithMessage("Start date must be at least 10 minutes from now.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required.")
            .GreaterThan(x => x.StartDate).WithMessage("End date must be greater than start date.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .MaximumLength(50).WithMessage("Status must not exceed 50 characters.");

        RuleFor(x => x.Priority)
            .GreaterThanOrEqualTo(0).WithMessage("Priority must be greater than or equal to 0.");

        RuleForEach(x => x.ProductPromotions)
            .SetValidator(new CreateProductPromotionValidator());

        RuleForEach(x => x.PromotionTimeSlots)
            .SetValidator(new CreatePromotionTimeSlotValidator());

        RuleFor(x => x.PromotionTimeSlots)
            .Must((dto, slots) => slots == null || slots.All(s => s.StartAt >= dto.StartDate && s.EndAt <= dto.EndDate))
            .WithMessage("All time slots must be within the promotion's date range.")
            .When(x => x.PromotionTimeSlots != null && x.StartDate != default && x.EndDate != default);

        RuleFor(x => x.PromotionTimeSlots)
            .Must((dto, slots) => 
            {
                if (slots == null || !slots.Any()) return true;
                var activeSlots = slots.Where(s => s.Status != "Inactive").ToList();
                for (int i = 0; i < activeSlots.Count; i++)
                {
                    for (int j = i + 1; j < activeSlots.Count; j++)
                    {
                        if (activeSlots[i].StartAt < activeSlots[j].EndAt && activeSlots[j].StartAt < activeSlots[i].EndAt)
                            return false;
                    }
                }
                return true;
            })
            .WithMessage("Flash sale time slots cannot overlap with each other.")
            .When(x => x.PromotionTimeSlots != null);
    }
}
