using FluentValidation;
using ToyStore.Application.DTOs.Campaigns;

namespace ToyStore.Application.Validators.Campaigns;

public class RescheduleCampaignValidator : AbstractValidator<RescheduleCampaignDto>
{
    public RescheduleCampaignValidator()
    {
        RuleFor(x => x.NewScheduledAt)
            .NotEmpty()
            .WithMessage("New scheduled time is required.");

        RuleFor(x => x.Reason)
            .MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.Reason));
    }
}
