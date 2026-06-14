using FluentValidation;
using ToyStore.Application.DTOs.Wallets;

namespace ToyStore.Application.Validators.Wallets;

public class VerifyWalletPinRequestValidator : AbstractValidator<VerifyWalletPinRequestDto>
{
    private static readonly string[] AllowedActions = ["PAYMENT", "VIEW_BALANCE", "TOP_UP", "WITHDRAWAL"];

    public VerifyWalletPinRequestValidator()
    {
        RuleFor(x => x.Pin)
            .NotEmpty().WithMessage("PIN is required.")
            .Length(6).WithMessage("PIN must be exactly 6 digits.")
            .Matches(@"^\d{6}$").WithMessage("PIN must contain only digits.");

        RuleFor(x => x.ActionType)
            .NotEmpty().WithMessage("ActionType is required.")
            .Must(action => AllowedActions.Contains(action?.Trim().ToUpperInvariant()))
            .WithMessage("ActionType must be one of: PAYMENT, VIEW_BALANCE, TOP_UP, WITHDRAWAL.");
    }
}
