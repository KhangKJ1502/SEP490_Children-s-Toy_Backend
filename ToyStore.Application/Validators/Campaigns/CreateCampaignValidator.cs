using FluentValidation;
using ToyStore.Application.DTOs.Campaigns;

namespace ToyStore.Application.Validators.Campaigns;

public class CreateCampaignValidator : AbstractValidator<CreateCampaignDto>
{
    private static readonly HashSet<string> ValidSourceTypes = ["ADMIN", "SYSTEM"];
    private static readonly HashSet<string> ValidTargetTypes = ["ALL", "INDIVIDUAL", "ROLE"];
    private static readonly HashSet<string> ValidCampaignTargetTypes = ["ACCOUNT_ID", "ROLE_ID"];
    private static readonly HashSet<string> ValidReferenceTypes = ["VOUCHER", "PRODUCT", "BLOG", "SALE", "OTHER"];

    public CreateCampaignValidator()
    {
        RuleFor(x => x.CampaignName)
            .NotEmpty().WithMessage("Campaign name is required.")
            .MaximumLength(255).WithMessage("Campaign name must not exceed 255 characters.");

        RuleFor(x => x.SourceType)
            .NotEmpty().WithMessage("Source type is required.")
            .Must(v => ValidSourceTypes.Contains(v))
            .WithMessage($"Source type must be one of: {string.Join(", ", ValidSourceTypes)}.");

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
            .MaximumLength(2000).WithMessage("Message override must not exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.MessageOverride));

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.TemplateCode)
                || (!string.IsNullOrWhiteSpace(x.TitleOverride)
                    && !string.IsNullOrWhiteSpace(x.MessageOverride)))
            .WithMessage("Template code is required unless both title and message overrides are provided.");

        RuleFor(x => x.ScheduledAt)
            .Null()
            .WithMessage("Do not set ScheduledAt when creating a campaign; use the schedule endpoint after approval.");

        RuleFor(x => x.EventKey)
            .MaximumLength(100).WithMessage("Event key must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.EventKey));

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

        RuleFor(x => x.CreatedByAccountId)
            .GreaterThan(0).WithMessage("Created by account ID must be greater than 0.");

        RuleFor(x => x.Targets)
            .Must(t => t != null && t.Count > 0)
            .WithMessage("At least one target is required when target type is INDIVIDUAL or ROLE.")
            .When(x => x.TargetType is "INDIVIDUAL" or "ROLE");

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
