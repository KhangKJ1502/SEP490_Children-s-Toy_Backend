using FluentValidation;
using ToyStore.Application.DTOs.CustomerChildren;

namespace ToyStore.Application.Validators.CustomerChildren;

public class UpdateChildValidator : AbstractValidator<UpdateChildDto>
{
    private const int MaxChildAgeYears = 25;
    private const int MaxPastYears = 100;

    public UpdateChildValidator()
    {
        RuleFor(x => x.FullName)
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.FullName));

        RuleFor(x => x.NickName)
            .MaximumLength(50).WithMessage("Nick name must not exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.NickName));

        RuleFor(x => x.Dob)
            .Cascade(CascadeMode.Stop)
            .Must(dob => dob.HasValue && dob.Value.Date <= System.DateTime.UtcNow.Date)
                .WithMessage("Date of birth must be a past date.")
            .Must(dob => dob.HasValue && dob.Value.Date >= System.DateTime.UtcNow.Date.AddYears(-MaxPastYears))
                .WithMessage("Date of birth must not be more than 100 years ago.")
            .Must(dob => dob.HasValue && dob.Value.Date >= System.DateTime.UtcNow.Date.AddYears(-MaxChildAgeYears))
                .WithMessage("Child age must be 25 or younger.")
            .When(x => x.Dob.HasValue);
    }
}
