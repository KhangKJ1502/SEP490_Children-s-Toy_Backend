using FluentValidation;
using ToyStore.Application.DTOs.Accounts;

namespace ToyStore.Application.Validators.Accounts;

public class UpdateAccountInfoValidator : AbstractValidator<UpdateAccountInfoDto>
{
    public UpdateAccountInfoValidator()
    {
        RuleFor(x => x.AccountName)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("Account name is required.")
            .Must(value => value.Trim().Length >= 2).WithMessage("Account name must be at least 2 characters.")
            .Must(value => value.Trim().Length <= 99).WithMessage("Account name must not exceed 99 characters.")
            .Must(value => System.Text.RegularExpressions.Regex.IsMatch(
                value.Trim(),
                @"^[\p{L}\p{N}]+(?: [\p{L}\p{N}]+)*$"))
            .WithMessage("Account name can contain only letters, numbers, and single spaces between words.");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^0\d{9}$").WithMessage("Phone number must start with 0 and contain exactly 10 digits.")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.IsActive)
            .NotNull().WithMessage("Status is required.");
    }
}
