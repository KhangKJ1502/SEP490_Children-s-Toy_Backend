using FluentValidation;
using ToyStore.Application.DTOs.Blogs;

namespace ToyStore.Application.Validators.Blogs;

public class UpdateBlogValidator : AbstractValidator<UpdateBlogDto>
{
    private static readonly string[] AllowedStatuses = ["Draft", "Pending"];

    public UpdateBlogValidator()
    {
        RuleFor(x => x.BlogCategoryId)
            .GreaterThan((short)0).WithMessage("Blog category must be greater than 0.")
            .When(x => x.BlogCategoryId.HasValue);

        RuleFor(x => x.BlogTitle)
            .MaximumLength(255).WithMessage("Blog title must not exceed 255 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.BlogTitle));

        RuleFor(x => x.BlogContent)
            .NotEmpty().WithMessage("Blog content must not be empty.")
            .When(x => x.BlogContent != null);

        RuleFor(x => x.BlogThumbnail)
            .MaximumLength(500).WithMessage("Blog thumbnail must not exceed 500 characters.")
            .Must(BeValidAbsoluteUrl).WithMessage("Blog thumbnail must be a valid absolute URL.")
            .When(x => !string.IsNullOrWhiteSpace(x.BlogThumbnail));

        RuleFor(x => x.Status)
            .Must(BeValidStatus).WithMessage("Status must be Draft or Pending.")
            .When(x => !string.IsNullOrWhiteSpace(x.Status));

        RuleFor(x => x.BlogAt)
            .Must(x => !x.HasValue || x.Value.Year >= 2000)
            .WithMessage("BlogAt is invalid.");
    }

    private static bool BeValidAbsoluteUrl(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out _);
    }

    private static bool BeValidStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return false;
        }

        var normalized = status.Trim();
        return AllowedStatuses.Any(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase));
    }
}
