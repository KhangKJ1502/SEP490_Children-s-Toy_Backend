using FluentValidation;
using ToyStore.Application.DTOs.Wallets;

namespace ToyStore.Application.Validators.Wallets;

public class CreateWalletRequestValidator : AbstractValidator<CreateWalletRequestDto>
{
    public CreateWalletRequestValidator()
    {
        RuleFor(x => x.Pin)
            .NotEmpty().WithMessage("PIN is required.")
            .Length(6).WithMessage("PIN must be exactly 6 digits.")
            .Matches(@"^\d{6}$").WithMessage("PIN must contain only digits.");

        RuleFor(x => x.ConfirmPin)
            .NotEmpty().WithMessage("Confirm PIN is required.")
            .Equal(x => x.Pin).WithMessage("PIN confirmation does not match.");
    }
}
