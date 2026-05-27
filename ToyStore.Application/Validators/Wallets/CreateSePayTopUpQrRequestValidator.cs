using FluentValidation;
using ToyStore.Application.DTOs.Wallets;

namespace ToyStore.Application.Validators.Wallets;

public class CreateSePayTopUpQrRequestValidator : AbstractValidator<CreateSePayTopUpQrRequestDto>
{
    private static readonly decimal[] AllowedAmounts =
    [
        10000m,
        20000m,
        50000m,
        100000m,
        200000m,
        500000m
    ];

    public CreateSePayTopUpQrRequestValidator()
    {
        RuleFor(x => x.TopUpToken)
            .NotEmpty().WithMessage("Top-up token is required.")
            .MaximumLength(128).WithMessage("Top-up token must not exceed 128 characters.");

        RuleFor(x => x.Amount)
            .Must(amount => AllowedAmounts.Contains(amount))
            .WithMessage("Amount must be one of the supported quick top-up values.");
    }
}
