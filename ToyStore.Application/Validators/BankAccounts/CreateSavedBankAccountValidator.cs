using FluentValidation;
using ToyStore.Application.DTOs.BankAccounts;

namespace ToyStore.Application.Validators.BankAccounts;

public class CreateSavedBankAccountValidator : AbstractValidator<CreateSavedBankAccountDto>
{
    public CreateSavedBankAccountValidator()
    {
        RuleFor(x => x.BankBin)
            .NotEmpty().WithMessage("Bank BIN is required.")
            .MaximumLength(10).WithMessage("Bank BIN must not exceed 10 characters.")
            .Matches(@"^\d+$").WithMessage("Bank BIN must contain only digits.");

        RuleFor(x => x.BankName)
            .NotEmpty().WithMessage("Bank name is required.")
            .MaximumLength(100).WithMessage("Bank name must not exceed 100 characters.");

        RuleFor(x => x.BankShortName)
            .NotEmpty().WithMessage("Bank short name is required.")
            .MaximumLength(20).WithMessage("Bank short name must not exceed 20 characters.");

        RuleFor(x => x.BankCode)
            .NotEmpty().WithMessage("Bank code is required.")
            .MaximumLength(20).WithMessage("Bank code must not exceed 20 characters.");

        RuleFor(x => x.AccountNumber)
            .NotEmpty().WithMessage("Account number is required.")
            .MaximumLength(50).WithMessage("Account number must not exceed 50 characters.")
            .Matches(@"^[A-Za-z0-9]+$").WithMessage("Account number must contain only alphanumeric characters.");

        RuleFor(x => x.AccountName)
            .NotEmpty().WithMessage("Account name is required.")
            .MaximumLength(200).WithMessage("Account name must not exceed 200 characters.");
    }
}
