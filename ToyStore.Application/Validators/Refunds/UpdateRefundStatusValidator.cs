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
            .Must(status => 
                status == RefundStatuses.Requested ||
                status == RefundStatuses.Approved || 
                status == RefundStatuses.Rejected || 
                status == RefundStatuses.PickupCreated ||
                status == RefundStatuses.Shipping ||
                status == RefundStatuses.Received ||
                status == RefundStatuses.InspectionPending ||
                status == RefundStatuses.Completed ||
                status == RefundStatuses.Cancelled)
            .WithMessage("Status name is invalid.");

        RuleFor(x => x.RejectReason)
            .NotEmpty().WithMessage("RejectReason is required when status is RefundRejected.")
            .When(x => x.Status == RefundStatuses.Rejected)
            .MaximumLength(500).WithMessage("RejectReason must not exceed 500 characters.");

        RuleFor(x => x.ShippingOrderCode)
            .Matches(@"^[A-Z0-9]{5,20}$").WithMessage("Shipping Order Code must be uppercase alphanumeric (5 to 20 characters) and contain no spaces or special symbols.")
            .When(x => !string.IsNullOrEmpty(x.ShippingOrderCode));
    }
}
