using FluentValidation;
using ToyStore.Application.DTOs.Shifts;

namespace ToyStore.Application.Validators.Shifts;

public class UpdateWorkScheduleValidator : AbstractValidator<UpdateWorkScheduleDto>
{
    private static readonly string[] AllowedStatuses =
    {
        "Scheduled", "OnDuty", "Completed", "Absent", "Cancelled"
    };

    public UpdateWorkScheduleValidator()
    {
        RuleFor(x => x.AccountId)
            .GreaterThan(0).WithMessage("Account ID is required.");

        RuleFor(x => x.ShiftTemplateId)
            .GreaterThan((byte)0).WithMessage("Shift Template ID is required.");

        RuleFor(x => x.WorkDate)
            .NotEmpty().WithMessage("Work date is required.");

        When(x => x.Status is not null, () =>
        {
            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Status is required.")
                .Must(s => AllowedStatuses.Contains(s!))
                .WithMessage("Status must be one of Scheduled, OnDuty, Completed, Absent, Cancelled.");
        });
    }
}
