using FluentValidation;
using ToyStore.Application.DTOs.Accounts;

namespace ToyStore.Application.Validators.Accounts;

public class UpdateAccountStatusValidator : AbstractValidator<UpdateAccountStatusDto>
{
    public UpdateAccountStatusValidator()
    {
        RuleFor(x => x.IsActive)
            .NotNull().WithMessage("Status is required.");
    }
}
