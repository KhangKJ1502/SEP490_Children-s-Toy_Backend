using FluentValidation;
using ToyStore.Application.DTOs.Campaigns;

namespace ToyStore.Application.Validators.Campaigns;

public class ScheduleCampaignValidator : AbstractValidator<ScheduleCampaignDto>
{
    public ScheduleCampaignValidator()
    {
        RuleFor(x => x.ScheduledAt)
            .NotNull()
            .WithMessage("Scheduled time is required.");

        RuleFor(x => x)
            .Must(x => !x.ValidFrom.HasValue || !x.ValidTo.HasValue || x.ValidFrom < x.ValidTo)
            .WithMessage("ValidFrom must be before ValidTo when both are set.");
    }
}
