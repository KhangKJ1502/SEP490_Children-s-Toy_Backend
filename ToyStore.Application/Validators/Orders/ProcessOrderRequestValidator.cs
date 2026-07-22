using FluentValidation;
using ToyStore.Application.DTOs.Orders;

namespace ToyStore.Application.Validators.Orders;

public class ProcessOrderRequestValidator : AbstractValidator<ProcessOrderRequestDto>
{
    public ProcessOrderRequestValidator()
    {
        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Note must not exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Note));
    }
}
