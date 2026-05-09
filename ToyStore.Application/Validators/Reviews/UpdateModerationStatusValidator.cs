using FluentValidation;
using ToyStore.Application.DTOs.Reviews;

namespace ToyStore.Application.Validators.Reviews;

public class UpdateModerationStatusValidator : AbstractValidator<UpdateModerationStatusDto>
{
    private readonly string[] _allowedStatuses = { "Approved", "Rejected", "ManualReview" };

    public UpdateModerationStatusValidator()
    {
        RuleFor(x => x.ModerationStatus)
            .NotEmpty().WithMessage("ModerationStatus is required.")
            .Must(status => _allowedStatuses.Contains(status))
            .WithMessage("ModerationStatus must be one of: Approved, Rejected, ManualReview.");
            
        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Reason));
    }
}
