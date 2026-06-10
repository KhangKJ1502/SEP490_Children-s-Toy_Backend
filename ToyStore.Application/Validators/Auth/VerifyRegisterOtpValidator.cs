using FluentValidation;
using ToyStore.Application.DTOs.Auth;

namespace ToyStore.Application.Validators.Auth;

public class VerifyRegisterOtpValidator : AbstractValidator<VerifyRegisterOtpDto>
{
    public VerifyRegisterOtpValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(100).WithMessage("Email must not exceed 100 characters.");

        RuleFor(x => x.OtpCode)
            .NotEmpty().WithMessage("OTP code is required.")
            .Length(6).WithMessage("OTP code must be exactly 6 digits.")
            .Matches(@"^\d{6}$").WithMessage("OTP code must contain only digits.");
    }
}
