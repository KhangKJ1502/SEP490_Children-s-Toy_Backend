using FluentValidation;
using ToyStore.Application.DTOs.Profiles;

namespace ToyStore.Application.Validators.Profiles;

public class UpdateProfileValidator : AbstractValidator<UpdateProfileDto>
{
    private static readonly byte[] AllowedSexIds = [1, 2, 3];
    private const int MinimumAge = 15;

    public UpdateProfileValidator()
    {
        RuleFor(x => x.AccountName)
            .MaximumLength(100).WithMessage("Account name must not exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.AccountName));

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^0\d{9}$").WithMessage("Phone number must start with 0 and contain exactly 10 digits.")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.ImageUrl)
            .Must(BeValidHttpUrl).WithMessage("Image URL must be a valid absolute http/https URL.")
            .When(x => !string.IsNullOrWhiteSpace(x.ImageUrl));

        RuleFor(x => x.Dob)
            .Must(BeValidDob).WithMessage("You must be at least 15 years old.")
            .When(x => x.Dob.HasValue);

        RuleFor(x => x.SexId)
            .Must(sexId => sexId == null || AllowedSexIds.Contains(sexId.Value))
            .WithMessage("Sex ID is invalid.");
    }

    private static bool BeValidHttpUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }

    private static bool BeValidDob(DateTime? value)
    {
        if (!value.HasValue)
        {
            return true;
        }

        var dob = value.Value.Date;
        var today = DateTime.UtcNow.Date;
        var latestAllowedDob = today.AddYears(-MinimumAge);

        return dob <= latestAllowedDob;
    }
}
