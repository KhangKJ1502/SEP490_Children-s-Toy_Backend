using FluentValidation;
using ToyStore.Application.DTOs.CustomerChildren;

namespace ToyStore.Application.Validators.CustomerChildren;

public class UpdateChildValidator : AbstractValidator<UpdateChildDto>
{
    public UpdateChildValidator()
    {
        RuleFor(x => x.FullName)
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.FullName));

        RuleFor(x => x.NickName)
            .MaximumLength(50).WithMessage("Nick name must not exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.NickName));

        RuleFor(x => x.Dob)
            .LessThan(System.DateTime.UtcNow).WithMessage("Date of birth must be a past date.")
            .When(x => x.Dob.HasValue);
    }
}
