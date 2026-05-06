using FluentValidation;
using ToyStore.Application.DTOs.Addresses;

namespace ToyStore.Application.Validators.Addresses;

public class UpdateAddressValidator : AbstractValidator<UpdateAddressDto>
{
    public UpdateAddressValidator()
    {
        RuleFor(x => x.RecipientName)
            .MinimumLength(2).WithMessage("Recipient name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Recipient name must not exceed 100 characters.")
            .When(x => x.RecipientName != null);

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^0\d{9}$").WithMessage("Phone number must be a valid 10-digit Vietnamese number.")
            .When(x => x.PhoneNumber != null);

        RuleFor(x => x.AddressLine)
            .MinimumLength(5).WithMessage("Address line must be at least 5 characters.")
            .MaximumLength(500).WithMessage("Address line must not exceed 500 characters.")
            .When(x => x.AddressLine != null);

        RuleFor(x => x.WardCode)
            .NotEmpty().WithMessage("Ward code cannot be empty.")
            .MaximumLength(20).WithMessage("Ward code must not exceed 20 characters.")
            .When(x => x.WardCode != null);

        RuleFor(x => x.DistrictId)
            .GreaterThan(0).WithMessage("District ID must be greater than 0.")
            .When(x => x.DistrictId.HasValue);

        RuleFor(x => x.ProvinceId)
            .GreaterThan(0).WithMessage("Province ID must be greater than 0.")
            .When(x => x.ProvinceId.HasValue);
    }
}
