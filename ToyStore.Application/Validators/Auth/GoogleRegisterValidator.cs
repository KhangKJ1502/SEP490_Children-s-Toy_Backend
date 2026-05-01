using FluentValidation;
using ToyStore.Application.DTOs.Auth;

namespace ToyStore.Application.Validators.Auth;

public class GoogleRegisterValidator : AbstractValidator<GoogleRegisterDto>
{
    public GoogleRegisterValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("ID token is required.")
            .MinimumLength(100).WithMessage("Invalid ID token format.")
            .MaximumLength(5000).WithMessage("ID token is too long.");
    }
}
