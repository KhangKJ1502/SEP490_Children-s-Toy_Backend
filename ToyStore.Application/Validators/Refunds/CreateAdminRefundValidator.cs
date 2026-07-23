using FluentValidation;
using ToyStore.Application.DTOs.Refunds;

namespace ToyStore.Application.Validators.Refunds;

public class CreateAdminRefundValidator : AbstractValidator<CreateAdminRefundDto>
{
    public CreateAdminRefundValidator()
    {
        RuleFor(x => x.OrderId)
            .GreaterThan(0).WithMessage("OrderId is required and must be greater than 0.");

        RuleFor(x => x.RefundReasonId)
            .GreaterThan((byte)0).WithMessage("RefundReasonId is required.");

        RuleFor(x => x.ReasonDetails)
            .MaximumLength(500).WithMessage("ReasonDetails must not exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.ReasonDetails));
    }
}
