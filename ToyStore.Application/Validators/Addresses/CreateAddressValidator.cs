using FluentValidation;
using ToyStore.Application.DTOs.Addresses;

namespace ToyStore.Application.Validators.Addresses;

public class CreateAddressValidator : AbstractValidator<CreateAddressDto>
{
    public CreateAddressValidator()
    {
        RuleFor(x => x.RecipientName)
            .NotEmpty().WithMessage("Recipient name is required.")
            .MinimumLength(2).WithMessage("Recipient name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Recipient name must not exceed 100 characters.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^0\d{9}$").WithMessage("Phone number must be a valid 10-digit Vietnamese number.");

        RuleFor(x => x.AddressLine)
            .NotEmpty().WithMessage("Address line is required.")
            .MinimumLength(5).WithMessage("Address line must be at least 5 characters.")
            .MaximumLength(500).WithMessage("Address line must not exceed 500 characters.");

        RuleFor(x => x.WardCode)
            .NotEmpty().WithMessage("Ward code is required.")
            .MaximumLength(20).WithMessage("Ward code must not exceed 20 characters.");

        RuleFor(x => x.DistrictId)
            .GreaterThan(0).WithMessage("District ID must be greater than 0.");

        RuleFor(x => x.ProvinceId)
            .GreaterThan(0).WithMessage("Province ID must be greater than 0.");
    }
}
