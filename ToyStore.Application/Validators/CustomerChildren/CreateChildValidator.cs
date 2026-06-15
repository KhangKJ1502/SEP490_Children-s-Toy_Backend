using FluentValidation;
using ToyStore.Application.DTOs.CustomerChildren;

namespace ToyStore.Application.Validators.CustomerChildren;

public class CreateChildValidator : AbstractValidator<CreateChildDto>
{
    public CreateChildValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MinimumLength(CustomerChildValidationRules.FullNameMinLength)
                .WithMessage("Full name must be at least 2 characters.")
            .MaximumLength(CustomerChildValidationRules.FullNameMaxLength)
                .WithMessage("Full name must not exceed 100 characters.");

        RuleFor(x => x.NickName)
            .MaximumLength(CustomerChildValidationRules.NickNameMaxLength)
                .WithMessage("Nick name must not exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.NickName));

        RuleFor(x => x.Dob)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Date of birth is required.")
            .Custom((dob, context) =>
            {
                if (!CustomerChildValidationRules.IsValidChildDob(dob, DateTime.UtcNow, out var error))
                    context.AddFailure(error!);
            });

        RuleFor(x => x.SexId)
            .NotNull().WithMessage("Gender is required.")
            .Must(sexId => sexId.HasValue && CustomerChildValidationRules.IsAllowedSexId(sexId.Value))
                .WithMessage("Please select a valid gender.");
    }
}
