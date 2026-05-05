using FluentValidation;
using ToyStore.Application.DTOs.Auth;

namespace ToyStore.Application.Validators.Auth;

public class GoogleLoginValidator : AbstractValidator<GoogleLoginDto>
{
    public GoogleLoginValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("ID token is required.")
            .MinimumLength(100).WithMessage("Invalid ID token format.")
            .MaximumLength(5000).WithMessage("ID token is too long.");

        // RoleId là optional, nếu có thì phải hợp lệ (1-5)
        // 1=Customer, 2=Staff, 3=Merchandise, 4=Admin, 5=Guest
        RuleFor(x => x.RoleId)
            .Must(roleId => !roleId.HasValue || (roleId.Value >= 1 && roleId.Value <= 5))
            .WithMessage("Role ID must be between 1 and 5.");
    }
}
