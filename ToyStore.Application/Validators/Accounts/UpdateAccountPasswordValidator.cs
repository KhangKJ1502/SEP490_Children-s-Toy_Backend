using FluentValidation;
using ToyStore.Application.DTOs.Accounts;

namespace ToyStore.Application.Validators.Accounts;

public class UpdateAccountPasswordValidator : AbstractValidator<UpdateAccountPasswordDto>
{
    public UpdateAccountPasswordValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("New password must be at least 8 characters.")
            .MaximumLength(100).WithMessage("New password must not exceed 100 characters.")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$")
            .WithMessage("New password must contain at least one uppercase letter, one lowercase letter, and one digit.");

        RuleFor(x => x.ConfirmNewPassword)
            .NotEmpty().WithMessage("Confirm new password is required.")
            .Equal(x => x.NewPassword).WithMessage("Confirm new password does not match new password.");
    }
}
