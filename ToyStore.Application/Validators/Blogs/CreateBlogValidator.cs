using FluentValidation;
using ToyStore.Application.DTOs.Blogs;

namespace ToyStore.Application.Validators.Blogs;

public class CreateBlogValidator : AbstractValidator<CreateBlogDto>
{
    public CreateBlogValidator()
    {
        RuleFor(x => x.BlogCategoryId)
            .GreaterThan((short)0).WithMessage("Blog category is required.");

        RuleFor(x => x.BlogTitle)
            .NotEmpty().WithMessage("Blog title is required.")
            .MaximumLength(255).WithMessage("Blog title must not exceed 255 characters.");

        RuleFor(x => x.BlogContent)
            .NotEmpty().WithMessage("Blog content is required.");

        RuleFor(x => x.BlogThumbnail)
            .MaximumLength(500).WithMessage("Blog thumbnail must not exceed 500 characters.")
            .Must(BeValidAbsoluteUrl).WithMessage("Blog thumbnail must be a valid absolute URL.")
            .When(x => !string.IsNullOrWhiteSpace(x.BlogThumbnail));

        RuleFor(x => x.BlogAt)
            .Must(x => !x.HasValue || x.Value.Year >= 2000)
            .WithMessage("BlogAt is invalid.");
    }

    private static bool BeValidAbsoluteUrl(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out _);
    }
}
