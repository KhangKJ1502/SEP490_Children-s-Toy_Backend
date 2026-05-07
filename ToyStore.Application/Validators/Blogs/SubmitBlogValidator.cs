using FluentValidation;
using ToyStore.Application.DTOs.Blogs;

namespace ToyStore.Application.Validators.Blogs;

public class SubmitBlogValidator : AbstractValidator<SubmitBlogDto>
{
    public SubmitBlogValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(x => string.Equals(x?.Trim(), "Pending", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Status must be Pending.");
    }
}
