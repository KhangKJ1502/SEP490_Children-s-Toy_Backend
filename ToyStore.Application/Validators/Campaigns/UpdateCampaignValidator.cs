using FluentValidation;
using ToyStore.Application.DTOs.Campaigns;

namespace ToyStore.Application.Validators.Campaigns;

public class UpdateCampaignValidator : AbstractValidator<UpdateCampaignDto>
{
    private static readonly HashSet<string> ValidTargetTypes         = ["ALL", "SEGMENT", "INDIVIDUAL", "ROLE"];
    private static readonly HashSet<string> ValidCampaignTargetTypes = ["ACCOUNT_ID", "ROLE_ID", "SEGMENT"];
    private static readonly HashSet<string> ValidReferenceTypes      = ["VOUCHER", "PRODUCT", "BLOG", "SALE"];

    public UpdateCampaignValidator()
    {
        RuleFor(x => x.CampaignId)
            .GreaterThan(0).WithMessage("Campaign ID must be greater than 0.");

        RuleFor(x => x.CampaignName)
            .NotEmpty().WithMessage("Campaign name is required.")
            .MinimumLength(3).WithMessage("Campaign name must be at least 3 characters.")
            .MaximumLength(255).WithMessage("Campaign name must not exceed 255 characters.");

        RuleFor(x => x.TargetType)
            .NotEmpty().WithMessage("Target type is required.")
            .Must(v => ValidTargetTypes.Contains(v))
            .WithMessage($"Target type must be one of: {string.Join(", ", ValidTargetTypes)}.");

        RuleFor(x => x.TemplateCode)
            .MaximumLength(50).WithMessage("Template code must not exceed 50 characters.")
            .Matches(@"^[A-Za-z0-9_]+$").WithMessage("Template code can only contain letters, numbers, and underscores.")
            .When(x => !string.IsNullOrEmpty(x.TemplateCode));

        RuleFor(x => x.ReferenceType)
            .Must(v => ValidReferenceTypes.Contains(v!.ToUpper()))
            .WithMessage($"ReferenceType must be one of: {string.Join(", ", ValidReferenceTypes)}.")
            .When(x => !string.IsNullOrEmpty(x.ReferenceType));

        RuleFor(x => x.ReferenceId)
            .GreaterThan(0).WithMessage("ReferenceId must be greater than 0 when ReferenceType is provided.")
            .When(x => !string.IsNullOrEmpty(x.ReferenceType));

        RuleFor(x => x.TitleOverride)
            .MaximumLength(255).WithMessage("Title override must not exceed 255 characters.")
            .When(x => !string.IsNullOrEmpty(x.TitleOverride));

        RuleFor(x => x.MessageOverride)
            .MaximumLength(500).WithMessage("Message override must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.MessageOverride));

        RuleFor(x => x.ScheduledAt)
            .GreaterThanOrEqualTo(DateTime.UtcNow.AddMinutes(-1))
            .WithMessage("Scheduled time must not be in the past.")
            .When(x => x.ScheduledAt.HasValue);

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500).WithMessage("Image URL must not exceed 500 characters.")
            .Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute))
            .WithMessage("Image URL is not a valid URL.")
            .When(x => !string.IsNullOrEmpty(x.ImageUrl));

        RuleFor(x => x.ActionType)
            .MaximumLength(20).WithMessage("Action type must not exceed 20 characters.")
            .When(x => !string.IsNullOrEmpty(x.ActionType));

        RuleFor(x => x.ActionTarget)
            .MaximumLength(500).WithMessage("Action target must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.ActionTarget));

        RuleFor(x => x.Targets)
            .Must(t => t != null && t.Count > 0)
            .WithMessage("At least one target is required when target type is SEGMENT, INDIVIDUAL, or ROLE.")
            .When(x => x.TargetType is "SEGMENT" or "INDIVIDUAL" or "ROLE");

        RuleFor(x => x.Targets)
            .Must(t => t == null || t.Count <= 100)
            .WithMessage("Targets must not exceed 100 items.");

        RuleFor(x => x.Targets)
            .Must(t => t == null || t.Count == 0)
            .WithMessage("Targets must be empty when target type is ALL.")
            .When(x => x.TargetType == "ALL");

        RuleForEach(x => x.Targets).ChildRules(target =>
        {
            target.RuleFor(t => t.TargetType)
                .NotEmpty().WithMessage("Target item type is required.")
                .MaximumLength(20).WithMessage("Target item type must not exceed 20 characters.")
                .Must(v => ValidCampaignTargetTypes.Contains(v))
                .WithMessage($"Target item type must be one of: {string.Join(", ", ValidCampaignTargetTypes)}.");

            target.RuleFor(t => t.TargetValue)
                .NotEmpty().WithMessage("Target value is required.")
                .MaximumLength(200).WithMessage("Target value must not exceed 200 characters.");
        });
    }
}
