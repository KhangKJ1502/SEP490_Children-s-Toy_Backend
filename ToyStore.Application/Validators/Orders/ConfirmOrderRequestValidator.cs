using FluentValidation;
using ToyStore.Application.DTOs.Orders;

namespace ToyStore.Application.Validators.Orders;

public class ConfirmOrderRequestValidator : AbstractValidator<ConfirmOrderRequestDto>
{
    public ConfirmOrderRequestValidator()
    {
        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Note must not exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Note));
    }
}
