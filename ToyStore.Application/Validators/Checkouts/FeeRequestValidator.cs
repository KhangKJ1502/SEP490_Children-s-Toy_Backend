using FluentValidation;
using ToyStore.Application.DTOs.Checkouts;

namespace ToyStore.Application.Validators.Checkouts;

public class FeeRequestValidator : AbstractValidator<FeeRequestDTO>
{
    private const decimal MaxMoney = 1_000_000_000m;
    private const int MaxWeight = 50_000;
    private const int MaxDimension = 200;

    public FeeRequestValidator()
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

        RuleFor(x => x.ServiceId)
            .GreaterThan(0).When(x => x.ServiceId.HasValue)
            .WithMessage("Service ID must be greater than 0.");

        RuleFor(x => x.ServiceTypeId)
            .GreaterThan(0).When(x => x.ServiceTypeId.HasValue)
            .WithMessage("Service type ID must be greater than 0.");

        RuleFor(x => x.InsuranceValue)
            .GreaterThanOrEqualTo(0).WithMessage("Insurance value must be greater than or equal to 0.")
            .LessThanOrEqualTo(MaxMoney).WithMessage("Insurance value must not exceed 1,000,000,000.");

        RuleFor(x => x.CodValue)
            .GreaterThanOrEqualTo(0).WithMessage("COD value must be greater than or equal to 0.")
            .LessThanOrEqualTo(MaxMoney).WithMessage("COD value must not exceed 1,000,000,000.");

        RuleFor(x => x.Weight)
            .GreaterThan(0).WithMessage("Weight must be greater than 0.")
            .LessThanOrEqualTo(MaxWeight).WithMessage("Weight must not exceed 50000.");

        RuleFor(x => x.Length)
            .GreaterThan(0).WithMessage("Length must be greater than 0.")
            .LessThanOrEqualTo(MaxDimension).WithMessage("Length must not exceed 200.");

        RuleFor(x => x.Width)
            .GreaterThan(0).WithMessage("Width must be greater than 0.")
            .LessThanOrEqualTo(MaxDimension).WithMessage("Width must not exceed 200.");

        RuleFor(x => x.Height)
            .GreaterThan(0).WithMessage("Height must be greater than 0.")
            .LessThanOrEqualTo(MaxDimension).WithMessage("Height must not exceed 200.");
    }
}
