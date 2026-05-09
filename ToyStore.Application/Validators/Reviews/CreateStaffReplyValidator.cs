using FluentValidation;
using ToyStore.Application.DTOs.Reviews;

namespace ToyStore.Application.Validators.Reviews;

public class CreateStaffReplyValidator : AbstractValidator<CreateStaffReplyDto>
{
    public CreateStaffReplyValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Reply content is required.")
            .MaximumLength(1000).WithMessage("Reply content must not exceed 1000 characters.");
    }
}
