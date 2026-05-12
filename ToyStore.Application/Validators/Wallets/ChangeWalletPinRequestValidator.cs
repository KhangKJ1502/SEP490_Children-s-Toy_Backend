using FluentValidation;
using ToyStore.Application.DTOs.Wallets;

namespace ToyStore.Application.Validators.Wallets;

public class ChangeWalletPinRequestValidator : AbstractValidator<ChangeWalletPinRequestDto>
{
    public ChangeWalletPinRequestValidator()
    {
        RuleFor(x => x.OldPin)
            .NotEmpty().WithMessage("Old PIN is required.")
            .Length(6).WithMessage("Old PIN must be exactly 6 digits.")
            .Matches(@"^\d{6}$").WithMessage("Old PIN must contain only digits.");

        RuleFor(x => x.NewPin)
            .NotEmpty().WithMessage("New PIN is required.")
            .Length(6).WithMessage("New PIN must be exactly 6 digits.")
            .Matches(@"^\d{6}$").WithMessage("New PIN must contain only digits.")
            .NotEqual(x => x.OldPin).WithMessage("New PIN must be different from old PIN.");

        RuleFor(x => x.ConfirmNewPin)
            .NotEmpty().WithMessage("Confirm new PIN is required.")
            .Equal(x => x.NewPin).WithMessage("Confirm new PIN does not match.");
    }
}
