using FluentValidation;
using ToyStore.Application.DTOs.Blogs;

namespace ToyStore.Application.Validators.Blogs;

public class UpdateBlogFeaturedValidator : AbstractValidator<UpdateBlogFeaturedDto>
{
    public UpdateBlogFeaturedValidator()
    {
        RuleFor(x => x.IsFeatured)
            .NotNull()
            .WithMessage("IsFeatured is required.");
    }
}
