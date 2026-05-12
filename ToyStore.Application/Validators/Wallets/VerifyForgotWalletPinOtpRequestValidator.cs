using FluentValidation;
using ToyStore.Application.DTOs.Wallets;

namespace ToyStore.Application.Validators.Wallets;

public class VerifyForgotWalletPinOtpRequestValidator : AbstractValidator<VerifyForgotWalletPinOtpRequestDto>
{
    public VerifyForgotWalletPinOtpRequestValidator()
    {
        RuleFor(x => x.OtpCode)
            .NotEmpty().WithMessage("OTP code is required.")
            .Length(6).WithMessage("OTP code must be exactly 6 digits.")
            .Matches(@"^\d{6}$").WithMessage("OTP code must contain only digits.");
    }
}
