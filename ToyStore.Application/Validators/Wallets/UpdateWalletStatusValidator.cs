using FluentValidation;
using ToyStore.Application.DTOs.Wallets;

namespace ToyStore.Application.Validators.Wallets;

public class UpdateWalletStatusValidator : AbstractValidator<UpdateWalletStatusDto>
{
    private static readonly string[] AllowedStatuses = { "Active", "Frozen" };

    public UpdateWalletStatusValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(status => AllowedStatuses.Contains(status?.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage("Status must be either Active or Frozen.");
    }
}
