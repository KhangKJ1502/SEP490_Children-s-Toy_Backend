using FluentValidation;
using ToyStore.Application.DTOs.Profiles;

namespace ToyStore.Application.Validators.Profiles;

public class ChangePasswordValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .MinimumLength(8).WithMessage("Current password must be at least 8 characters.")
            .MaximumLength(255).WithMessage("Current password must not exceed 255 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.CurrentPassword));

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("New password must be at least 8 characters.")
            .MaximumLength(255).WithMessage("New password must not exceed 255 characters.")
            .Matches(@"[A-Z]").WithMessage("New password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("New password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("New password must contain at least one digit.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("New password must contain at least one special character.");

        RuleFor(x => x.ConfirmNewPassword)
            .NotEmpty().WithMessage("Confirm new password is required.")
            .Equal(x => x.NewPassword).WithMessage("Confirm new password does not match new password.");
    }
}
