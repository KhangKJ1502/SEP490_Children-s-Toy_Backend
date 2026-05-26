using FluentValidation;
using ToyStore.Application.DTOs.Checkouts;

namespace ToyStore.Application.Validators.Checkouts;

public class LeadtimeRequestValidator : AbstractValidator<LeadtimeRequestDTO>
{
    public LeadtimeRequestValidator()
    {
        RuleFor(x => x.FromDistrictId)
            .GreaterThan(0).WithMessage("From district is required.");

        RuleFor(x => x.FromWardCode)
            .NotEmpty().WithMessage("From ward code is required.")
            .MaximumLength(20).WithMessage("From ward code must not exceed 20 characters.");

        RuleFor(x => x.ToDistrictId)
            .GreaterThan(0).WithMessage("To district is required.");

        RuleFor(x => x.ToWardCode)
            .NotEmpty().WithMessage("To ward code is required.")
            .MaximumLength(20).WithMessage("To ward code must not exceed 20 characters.");


    }
}
