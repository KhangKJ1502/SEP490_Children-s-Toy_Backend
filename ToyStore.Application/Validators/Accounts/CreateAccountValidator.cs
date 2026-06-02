using FluentValidation;
using ToyStore.Application.DTOs.Accounts;

namespace ToyStore.Application.Validators.Accounts;

public class CreateAccountValidator : AbstractValidator<CreateAccountDto>
{
    private const byte StaffRoleId = 3;
    private const byte MerchandiserRoleId = 4;

    public CreateAccountValidator()
    {
        RuleFor(x => x.RoleId)
            .GreaterThan((byte)0).WithMessage("Role ID must be greater than 0.")
            .Must(IsAllowedRoleId).WithMessage("Role ID must be either 3 (Staff) or 4 (Merchandiser).");

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

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(100).WithMessage("Email must not exceed 100 characters.")
            .EmailAddress().WithMessage("Email format is invalid.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(100).WithMessage("Password must not exceed 100 characters.")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$")
            .WithMessage("Password must contain at least one uppercase letter, one lowercase letter, and one digit.");
    }

    private static bool IsAllowedRoleId(byte roleId)
    {
        return roleId == StaffRoleId || roleId == MerchandiserRoleId;
    }
}
