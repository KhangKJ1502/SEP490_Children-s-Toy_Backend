using FluentValidation;
using ToyStore.Application.DTOs.Blogs;

namespace ToyStore.Application.Validators.Blogs;

public class ApproveBlogValidator : AbstractValidator<ApproveBlogDto>
{
    public ApproveBlogValidator()
    {
        RuleFor(x => x.Decision)
            .NotEmpty().WithMessage("Decision is required.")
            .Must(IsValidDecision).WithMessage("Decision must be Approved or Rejected.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required when rejecting a blog.")
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters.")
            .When(x => string.Equals(x.Decision?.Trim(), "Rejected", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsValidDecision(string? decision)
    {
        if (string.IsNullOrWhiteSpace(decision))
        {
            return false;
        }

        var normalized = decision.Trim();
        return string.Equals(normalized, "Approved", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "Rejected", StringComparison.OrdinalIgnoreCase);
    }
}
