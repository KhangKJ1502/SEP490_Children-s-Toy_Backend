using FluentValidation;
using ToyStore.Application.DTOs.Orders;

namespace ToyStore.Application.Validators.Orders;

public class ShipOrderRequestValidator : AbstractValidator<ShipOrderRequestDto>
{
    private static readonly string[] SupportedProviders = ["GHN"];

    public ShipOrderRequestValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty().WithMessage("Provider is required.")
            .Must(p => SupportedProviders.Contains(p?.ToUpperInvariant()))
            .WithMessage($"Provider must be one of: {string.Join(", ", SupportedProviders)}.");

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Note must not exceed 500 characters.")
            .When(x => x.Note is not null);
    }
}
