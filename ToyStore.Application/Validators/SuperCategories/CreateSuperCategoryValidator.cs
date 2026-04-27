using FluentValidation;
using ToyStore.Application.DTOs.SuperCategories;

namespace ToyStore.Application.Validators.SuperCategories;

public class CreateSuperCategoryValidator : AbstractValidator<CreateSuperCategoryDto>
{
    public CreateSuperCategoryValidator()
    {
        RuleFor(x => x.SuperCategoryName)
            .NotEmpty().WithMessage("Super category name is required.")
            .MinimumLength(2).WithMessage("Super category name must be at least 2 characters.")
            .MaximumLength(25).WithMessage("Super category name must not exceed 25 characters.");
    }
}