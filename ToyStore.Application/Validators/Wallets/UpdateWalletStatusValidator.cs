using FluentValidation;
using ToyStore.Application.DTOs.Wallets;

namespace ToyStore.Application.Validators.Wallets;

public class UpdateWalletStatusValidator : AbstractValidator<UpdateWalletStatusDto>
{
    public UpdateWalletStatusValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(status => string.Equals(status?.Trim(), "Active", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Admin and Staff can only activate customer wallets.");
    }
}
