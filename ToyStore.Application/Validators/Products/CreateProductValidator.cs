using FluentValidation;
using ToyStore.Application.DTOs.Products;

namespace ToyStore.Application.Validators.Products;

public class CreateProductValidator : AbstractValidator<CreateProductDto>
{
    private static readonly string[] AllowedStatuses =
    {
        "Active",
        "Inactive",
        "OutOfStock",
        "Discontinued",
        "ComingSoon"
    };

    public CreateProductValidator()
    {
        RuleFor(x => x.CategoryId)
            .GreaterThan((short)0).WithMessage("Category ID must be greater than 0.");

        RuleFor(x => x.BrandId)
            .GreaterThan((short)0).WithMessage("Brand ID must be greater than 0.")
            .When(x => x.BrandId.HasValue);

        RuleFor(x => x.PriceRangeId)
            .GreaterThan((byte)0).WithMessage("Price range ID must be greater than 0.")
            .When(x => x.PriceRangeId.HasValue);

        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("Product name is required.")
            .MinimumLength(3).WithMessage("Product name must be at least 3 characters.")
            .MaximumLength(255).WithMessage("Product name must not exceed 255 characters.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0.")
            .LessThanOrEqualTo(100_000_000).WithMessage("Price must not exceed 100,000,000 VND.");

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0).WithMessage("Quantity must not be negative.")
            .LessThanOrEqualTo(1_000_000).WithMessage("Quantity must not exceed 1,000,000.");

        RuleFor(x => x.ProductStatus)
            .NotEmpty().WithMessage("Product status is required.")
            .Must(status => AllowedStatuses.Contains(status))
            .WithMessage("Product status is invalid.");

        RuleFor(x => x.LaunchDate)
            .NotNull().WithMessage("Launch date is required for coming soon products.")
            .When(x => x.ProductStatus == "ComingSoon");

        RuleFor(x => x.LaunchDate)
            .Must(date => date.HasValue && date.Value.Date >= DateTime.UtcNow.Date)
            .WithMessage("Launch date must be today or later for coming soon products.")
            .When(x => x.ProductStatus == "ComingSoon" && x.LaunchDate.HasValue);

        RuleFor(x => x.StockThreshold)
            .GreaterThanOrEqualTo((short)0).WithMessage("Stock threshold must not be negative.")
            .LessThanOrEqualTo((short)10_000).WithMessage("Stock threshold must not exceed 10,000.");

        RuleFor(x => x.Description)
            .MinimumLength(10).WithMessage("Description must be at least 10 characters.")
            .MaximumLength(1500).WithMessage("Description must not exceed 1500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.MaterialId)
            .GreaterThan((short)0).WithMessage("Material ID must be greater than 0.")
            .When(x => x.MaterialId.HasValue);

        RuleFor(x => x.AgeId)
            .GreaterThan((byte)0).WithMessage("Age ID must be greater than 0.")
            .When(x => x.AgeId.HasValue);

        RuleFor(x => x.SexId)
            .GreaterThan((byte)0).WithMessage("Sex ID must be greater than 0.")
            .When(x => x.SexId.HasValue);

        RuleFor(x => x.OriginId)
            .GreaterThan((byte)0).WithMessage("Origin ID must be greater than 0.")
            .When(x => x.OriginId.HasValue);

        RuleFor(x => x.MainImageUrl)
            .NotEmpty().WithMessage("Main image is required.")
            .Must(url => Uri.IsWellFormedUriString(url, UriKind.Absolute))
            .WithMessage("Main image URL is not valid.");

        RuleFor(x => x.AdditionalImageUrls)
            .NotNull().WithMessage("Additional image URLs are required.")
            .Must(urls => urls != null && urls.Count >= 4 && urls.Count <= 6)
            .WithMessage("Additional images must be between 4 and 6.");

        RuleForEach(x => x.AdditionalImageUrls)
            .Must(url => !string.IsNullOrWhiteSpace(url) && Uri.IsWellFormedUriString(url, UriKind.Absolute))
            .WithMessage("Additional image URL is not valid.");
    }
}
