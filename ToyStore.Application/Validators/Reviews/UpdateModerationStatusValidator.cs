using FluentValidation;
using ToyStore.Application.DTOs.Reviews;

namespace ToyStore.Application.Validators.Reviews;

public class UpdateModerationStatusValidator : AbstractValidator<UpdateModerationStatusDto>
{
    private readonly string[] _allowedStatuses = { "Approved", "Rejected" };

    public UpdateModerationStatusValidator()
    {
        RuleFor(x => x.ModerationStatus)
            .NotEmpty().WithMessage("ModerationStatus is required.")
            .When(x => x.IsDeleted != true);

        RuleFor(x => x.ModerationStatus)
            .Must(status => _allowedStatuses.Contains(status!))
            .WithMessage("ModerationStatus must be one of: Approved, Rejected.")
            .When(x => !string.IsNullOrWhiteSpace(x.ModerationStatus));
            
        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Reason));
    }
}
