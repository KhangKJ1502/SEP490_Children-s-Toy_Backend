using FluentValidation;
using ToyStore.Application.DTOs.Profiles;

namespace ToyStore.Application.Validators.Profiles;

public class UpdateProfileValidator : AbstractValidator<UpdateProfileDto>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .Matches(@"^0\d{9}$").WithMessage("Phone number must start with 0 and contain exactly 10 digits.")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.ImageUrl)
            .Must(BeValidHttpUrl).WithMessage("Image URL must be a valid absolute http/https URL.")
            .When(x => !string.IsNullOrWhiteSpace(x.ImageUrl));

        RuleFor(x => x.NewPassword)
            .MinimumLength(8).WithMessage("New password must be at least 8 characters.")
            .MaximumLength(255).WithMessage("New password must not exceed 255 characters.")
            .Matches(@"[A-Z]").WithMessage("New password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("New password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("New password must contain at least one digit.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("New password must contain at least one special character.")
            .When(HasPasswordChangeRequest);

        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required when changing password.")
            .When(HasPasswordChangeRequest);

        RuleFor(x => x.ConfirmNewPassword)
            .NotEmpty().WithMessage("Confirm new password is required when changing password.")
            .Equal(x => x.NewPassword).WithMessage("Confirm new password does not match new password.")
            .When(HasPasswordChangeRequest);
    }

    private static bool HasPasswordChangeRequest(UpdateProfileDto dto)
    {
        return !string.IsNullOrWhiteSpace(dto.CurrentPassword)
               || !string.IsNullOrWhiteSpace(dto.NewPassword)
               || !string.IsNullOrWhiteSpace(dto.ConfirmNewPassword);
    }

    private static bool BeValidHttpUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }
}
