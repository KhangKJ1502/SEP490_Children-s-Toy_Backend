using FluentValidation;
using ToyStore.Application.DTOs.Refunds;
using ToyStore.Domain.Enums;

namespace ToyStore.Application.Validators.Refunds;

public class UpdateRefundStatusValidator : AbstractValidator<UpdateRefundStatusDto>
{
    public UpdateRefundStatusValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(status => status == RefundStatuses.Approved || status == RefundStatuses.Rejected || status == RefundStatuses.Completed)
            .WithMessage($"Status must be {RefundStatuses.Approved}, {RefundStatuses.Rejected}, or {RefundStatuses.Completed}.");

        RuleFor(x => x.RejectReason)
            .NotEmpty().WithMessage("RejectReason is required when status is Rejected.")
            .When(x => x.Status == RefundStatuses.Rejected)
            .MaximumLength(500).WithMessage("RejectReason must not exceed 500 characters.");
    }
}
