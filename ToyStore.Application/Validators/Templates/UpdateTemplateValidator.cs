using FluentValidation;
using ToyStore.Application.DTOs.Templates;

namespace ToyStore.Application.Validators.Templates;

public class UpdateTemplateValidator : AbstractValidator<UpdateTemplateDto>
{
    public UpdateTemplateValidator()
    {
        When(x => !x.IsDeleted, () =>
        {
            RuleFor(x => x.TitleTemplate)
                .NotEmpty().WithMessage("Template title is required.")
                .MinimumLength(3).WithMessage("Template title must be at least 3 characters.")
                .MaximumLength(255).WithMessage("Template title must not exceed 255 characters.");

            RuleFor(x => x.MessageTemplate)
                .NotEmpty().WithMessage("Template message is required.")
                .MinimumLength(5).WithMessage("Template message must be at least 5 characters.")
                .MaximumLength(2000).WithMessage("Template message must not exceed 2000 characters.");
        });
    }
}