using FluentValidation;
using ToyStore.Application.DTOs.CustomerChildren;

namespace ToyStore.Application.Validators.CustomerChildren;

public class UpdateChildValidator : AbstractValidator<UpdateChildDto>
{
    public UpdateChildValidator()
    {
        RuleFor(x => x.FullName)
            .Must(name => !string.IsNullOrWhiteSpace(name))
                .WithMessage("Full name cannot be empty.")
            .MinimumLength(CustomerChildValidationRules.FullNameMinLength)
                .WithMessage("Full name must be at least 2 characters.")
            .MaximumLength(CustomerChildValidationRules.FullNameMaxLength)
                .WithMessage("Full name must not exceed 100 characters.")
            .When(x => x.FullName != null);

        RuleFor(x => x.NickName)
            .MaximumLength(CustomerChildValidationRules.NickNameMaxLength)
                .WithMessage("Nick name must not exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.NickName));

        RuleFor(x => x.Dob)
            .Custom((dob, context) =>
            {
                if (!dob.HasValue)
                    return;

                if (!CustomerChildValidationRules.IsValidChildDob(dob.Value, DateTime.UtcNow, out var error))
                    context.AddFailure(error!);
            })
            .When(x => x.Dob.HasValue);

        RuleFor(x => x.SexId)
            .Must(sexId => sexId.HasValue && CustomerChildValidationRules.IsAllowedSexId(sexId.Value))
                .WithMessage("Please select a valid gender.")
            .When(x => x.SexId.HasValue);
    }
}
