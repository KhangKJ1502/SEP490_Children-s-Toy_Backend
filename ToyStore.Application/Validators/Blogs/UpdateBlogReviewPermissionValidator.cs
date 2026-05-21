using FluentValidation;
using ToyStore.Application.DTOs.Blogs;

namespace ToyStore.Application.Validators.Blogs;

public class UpdateBlogReviewPermissionValidator : AbstractValidator<UpdateBlogReviewPermissionDto>
{
    public UpdateBlogReviewPermissionValidator()
    {
        RuleFor(x => x.IsCommentBanned)
            .Equal(false)
            .WithMessage("Blog comment permission management only supports restoring comment permission.");
    }
}
