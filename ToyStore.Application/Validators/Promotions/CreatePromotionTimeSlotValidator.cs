using FluentValidation;
using ToyStore.Application.DTOs.Promotions;

namespace ToyStore.Application.Validators.Promotions;

public class CreatePromotionTimeSlotValidator : AbstractValidator<CreatePromotionTimeSlotDto>
{
    public CreatePromotionTimeSlotValidator()
    {
        // StartAt — phải là UTC datetime hợp lệ, không được là mặc định
        RuleFor(x => x.StartAt)
            .NotEmpty().WithMessage("Start date/time is required.");

        // EndAt — phải sau StartAt ít nhất 5 phút
        RuleFor(x => x.EndAt)
            .NotEmpty().WithMessage("End date/time is required.")
            .GreaterThan(x => x.StartAt.AddMinutes(5))
            .WithMessage("End time must be at least 5 minutes after start time.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(s => s == "Active" || s == "Inactive" || s == "Scheduled")
            .WithMessage("Status must be either 'Active', 'Inactive', or 'Scheduled'.");

        // Validate từng sản phẩm trong slot
        RuleForEach(x => x.PromotionProductSlots)
            .SetValidator(new CreatePromotionProductSlotValidator());
    }
}
