using FluentValidation;
using Microsoft.AspNetCore.Http;
using ToyStore.Application.DTOs.Reviews;

namespace ToyStore.Application.Validators.Reviews;

public class UpdateReviewProductValidator : AbstractValidator<UpdateReviewProductDto>
{
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

    public UpdateReviewProductValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween((byte)1, (byte)5).WithMessage("Rating must be between 1 and 5.")
            .When(x => x.Rating.HasValue);

        RuleFor(x => x.Comment)
            .MaximumLength(500).WithMessage("Comment must not exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Comment));

        RuleFor(x => x.Images)
            .Must(images => images == null || images.Count <= 3)
            .WithMessage("A maximum of 3 images can be uploaded.");

        RuleForEach(x => x.Images)
            .ChildRules(image =>
            {
                image.RuleFor(i => i)
                    .Must(BeValidFileSize)
                    .WithMessage("Each image must not exceed 5MB.");

                image.RuleFor(i => i)
                    .Must(BeValidExtension)
                    .WithMessage("Only .jpg, .jpeg, .png, and .webp formats are allowed.");
            })
            .When(x => x.Images != null && x.Images.Any());
    }

    private bool BeValidFileSize(IFormFile file)
    {
        if (file == null) return true;
        return file.Length <= MaxFileSize;
    }

    private bool BeValidExtension(IFormFile file)
    {
        if (file == null) return true;
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return _allowedExtensions.Contains(ext);
    }
}
