using FluentValidation;
using ToyStore.Application.DTOs.Auth;

namespace ToyStore.Application.Validators.Auth;

public class LoginValidator : AbstractValidator<LoginDto>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(100).WithMessage("Email must not exceed 100 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MaximumLength(255).WithMessage("Password must not exceed 255 characters.");

        RuleFor(x => x.RoleId)
            .InclusiveBetween((byte)1, (byte)5)
            .When(x => x.RoleId.HasValue)
            .WithMessage("Invalid role.");

        RuleForEach(x => x.AllowedRoleIds)
            .InclusiveBetween((byte)1, (byte)5)
            .When(x => x.AllowedRoleIds is { Count: > 0 })
            .WithMessage("Invalid role.");
    }
}
