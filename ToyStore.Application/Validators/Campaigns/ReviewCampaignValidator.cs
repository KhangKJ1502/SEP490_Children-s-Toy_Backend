using FluentValidation;
using ToyStore.Application.DTOs.Campaigns;

namespace ToyStore.Application.Validators.Campaigns;

public class ReviewCampaignValidator : AbstractValidator<ReviewCampaignDto>
{
    private static readonly HashSet<string> ValidActions = ["Approved", "Rejected"];

    public ReviewCampaignValidator()
    {
        RuleFor(x => x.Action)
            .NotEmpty().WithMessage("Review action is required.")
            .Must(v => ValidActions.Contains(v))
            .WithMessage($"Action must be one of: {string.Join(", ", ValidActions)}.");

        // ReviewNote bắt buộc khi Rejected
        RuleFor(x => x.ReviewNote)
            .NotEmpty().WithMessage("Review note is required when rejecting a campaign.")
            .MaximumLength(500).WithMessage("Review note must not exceed 500 characters.")
            .When(x => x.Action == "Rejected");

        // Khi Approved, ReviewNote không bắt buộc nhưng nếu có thì giới hạn độ dài
        RuleFor(x => x.ReviewNote)
            .MaximumLength(500).WithMessage("Review note must not exceed 500 characters.")
            .When(x => x.Action == "Approved" && !string.IsNullOrEmpty(x.ReviewNote));
    }
}
