using FluentValidation;
using ToyStore.Application.DTOs.CustomerChildren;

namespace ToyStore.Application.Validators.CustomerChildren;

public class CreateChildValidator : AbstractValidator<CreateChildDto>
{
    public CreateChildValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

        RuleFor(x => x.NickName)
            .MaximumLength(50).WithMessage("Nick name must not exceed 50 characters.");

        RuleFor(x => x.Dob)
            .NotEmpty().WithMessage("Date of birth is required.")
            .LessThan(System.DateTime.UtcNow).WithMessage("Date of birth must be a past date.");
    }
}
