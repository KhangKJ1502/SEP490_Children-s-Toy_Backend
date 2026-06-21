using FluentValidation;
using ToyStore.Application.DTOs.Refunds;
using System;

namespace ToyStore.Application.Validators.Refunds;

public class CreateRefundValidator : AbstractValidator<CreateRefundDto>
{
    public CreateRefundValidator()
    {
        RuleFor(x => x.OrderId)
            .GreaterThan(0).WithMessage("OrderId is required and must be greater than 0.");

        RuleFor(x => x.RefundReasonId)
            .GreaterThan((byte)0).WithMessage("RefundReasonId is required.");

        RuleFor(x => x.ReasonDetails)
            .MaximumLength(500).WithMessage("ReasonDetails must not exceed 500 characters.");

        RuleFor(x => x.Images)
            .NotEmpty().WithMessage("At least one evidence image is required.")
            .Must(x => x == null || x.Count <= 5).WithMessage("A maximum of 5 evidence photos is allowed.")
            .ForEach(image => image.Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute)).WithMessage("Image URL is not valid."));
    }
}
