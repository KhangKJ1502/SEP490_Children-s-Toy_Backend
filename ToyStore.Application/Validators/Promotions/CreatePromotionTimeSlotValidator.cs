using FluentValidation;
using ToyStore.Application.DTOs.Promotions;

namespace ToyStore.Application.Validators.Promotions;

public class CreatePromotionTimeSlotValidator : AbstractValidator<CreatePromotionTimeSlotDto>
{
    public CreatePromotionTimeSlotValidator()
    {
        // StartAt — phải là UTC datetime hợp lệ, không được là mặc định
        RuleFor(x => x.StartAt)
            .NotEmpty().WithMessage("Start date/time is required.")
            .Must(d => d >= DateTime.UtcNow.AddMinutes(9))
            .When(x => x.Status == "Scheduled")
            .WithMessage("Start time must be at least 10 minutes from now.");

        // EndAt — phải sau StartAt ít nhất 10 phút
        RuleFor(x => x.EndAt)
            .NotEmpty().WithMessage("End date/time is required.")
            .GreaterThan(x => x.StartAt.AddMinutes(9))
            .WithMessage("End time must be at least 10 minutes after start time.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(s => s == "Active" || s == "Scheduled")
            .WithMessage("Status must be either 'Active' or 'Scheduled'.");

        // Validate từng sản phẩm trong slot
        RuleForEach(x => x.PromotionProductSlots)
            .SetValidator(new CreatePromotionProductSlotValidator());
    }
}
