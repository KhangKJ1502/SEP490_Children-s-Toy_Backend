using FluentValidation;
using ToyStore.Application.DTOs.CustomerChildren;

namespace ToyStore.Application.Validators.CustomerChildren;

public class CreateChildValidator : AbstractValidator<CreateChildDto>
{
    private const int MaxChildAgeYears = 25;
    private const int MaxPastYears = 100;

    public CreateChildValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

        RuleFor(x => x.NickName)
            .MaximumLength(50).WithMessage("Nick name must not exceed 50 characters.");

        RuleFor(x => x.Dob)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Date of birth is required.")
            .Must(dob => dob.Date <= System.DateTime.UtcNow.Date)
                .WithMessage("Date of birth must be a past date.")
            .Must(dob => dob.Date >= System.DateTime.UtcNow.Date.AddYears(-MaxPastYears))
                .WithMessage("Date of birth must not be more than 100 years ago.")
            .Must(dob => dob.Date >= System.DateTime.UtcNow.Date.AddYears(-MaxChildAgeYears))
                .WithMessage("Child age must be 25 or younger.");
    }
}
