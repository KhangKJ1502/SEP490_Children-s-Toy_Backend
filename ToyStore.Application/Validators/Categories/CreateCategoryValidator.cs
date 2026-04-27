using FluentValidation;
using ToyStore.Application.DTOs.Categories;

namespace ToyStore.Application.Validators.Categories;

public class CreateCategoryValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.SuperCategoryId)
            .GreaterThan((short)0).WithMessage("Super category ID must be greater than 0.");

        RuleFor(x => x.CategoryName)
            .NotEmpty().WithMessage("Category name is required.")
            .MinimumLength(2).WithMessage("Category name must be at least 2 characters.")
            .MaximumLength(25).WithMessage("Category name must not exceed 25 characters.");
    }
}