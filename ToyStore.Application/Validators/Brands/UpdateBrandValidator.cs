using FluentValidation;
using ToyStore.Application.DTOs.Brands;

namespace ToyStore.Application.Validators.Brands;

public class UpdateBrandValidator : AbstractValidator<UpdateBrandDto>
{
    public UpdateBrandValidator()
    {
        RuleFor(x => x.BrandName)
            .NotEmpty().WithMessage("Brand name is required.")
            .MinimumLength(2).WithMessage("Brand name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Brand name must not exceed 100 characters.");
    }
}
