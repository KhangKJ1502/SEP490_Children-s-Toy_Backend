using FluentValidation;
using ToyStore.Application.DTOs.Templates;

namespace ToyStore.Application.Validators.Templates;

public class CreateTemplateValidator : AbstractValidator<CreateTemplateDto>
{
    public CreateTemplateValidator()
    {
        RuleFor(x => x.TemplateCode)
            .NotEmpty().WithMessage("Template code is required.")
            .MinimumLength(3).WithMessage("Template code must be at least 3 characters.")
            .MaximumLength(50).WithMessage("Template code must not exceed 50 characters.")
            .Matches("^[A-Za-z0-9_]+$").WithMessage("Template code can only contain letters, numbers, and underscores.");

        RuleFor(x => x.TitleTemplate)
            .NotEmpty().WithMessage("Template title is required.")
            .MinimumLength(3).WithMessage("Template title must be at least 3 characters.")
            .MaximumLength(255).WithMessage("Template title must not exceed 255 characters.");

        RuleFor(x => x.MessageTemplate)
            .NotEmpty().WithMessage("Template message is required.")
            .MinimumLength(5).WithMessage("Template message must be at least 5 characters.")
            .MaximumLength(500).WithMessage("Template message must not exceed 500 characters.");
    }
}