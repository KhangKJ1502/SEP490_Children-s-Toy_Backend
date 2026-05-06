using FluentValidation;
using ToyStore.Application.DTOs.Products;

namespace ToyStore.Application.Validators.Products;

public class UpdateProductValidator : AbstractValidator<UpdateProductDto>
{
    private static readonly string[] AllowedStatuses =
    {
        "Active",
        "Inactive",
        "OutOfStock",
        "Discontinued",
        "ComingSoon"
    };

    public UpdateProductValidator()
    {
        RuleFor(x => x.CategoryId)
            .GreaterThan((short)0).WithMessage("Category ID must be greater than 0.")
            .When(x => x.CategoryId.HasValue);

        RuleFor(x => x.BrandId)
            .GreaterThan((short)0).WithMessage("Brand ID must be greater than 0.")
            .When(x => x.BrandId.HasValue);

        RuleFor(x => x.PriceRangeId)
            .GreaterThan((byte)0).WithMessage("Price range ID must be greater than 0.")
            .When(x => x.PriceRangeId.HasValue);

        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("Product name must not be empty.")
            .MaximumLength(255).WithMessage("Product name must not exceed 255 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.ProductName));

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0.")
            .LessThanOrEqualTo(100_000_000).WithMessage("Price must not exceed 100,000,000 VND.")
            .When(x => x.Price.HasValue);

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0).WithMessage("Quantity must not be negative.")
            .LessThanOrEqualTo(1_000_000).WithMessage("Quantity must not exceed 1,000,000.")
            .When(x => x.Quantity.HasValue);

        RuleFor(x => x.ProductStatus)
            .Must(status => status != null && AllowedStatuses.Contains(status))
            .WithMessage("Product status is invalid.")
            .When(x => !string.IsNullOrWhiteSpace(x.ProductStatus));

        RuleFor(x => x.LaunchDate)
            .NotNull().WithMessage("Launch date is required for coming soon products.")
            .When(x => x.ProductStatus == "ComingSoon");

        RuleFor(x => x.LaunchDate)
            .Must(date => date.HasValue && date.Value.Date >= DateTime.UtcNow.Date)
            .WithMessage("Launch date must be today or later for coming soon products.")
            .When(x => x.ProductStatus == "ComingSoon" && x.LaunchDate.HasValue);

        RuleFor(x => x.StockThreshold)
            .GreaterThanOrEqualTo((short)0).WithMessage("Stock threshold must not be negative.")
            .LessThanOrEqualTo((short)10_000).WithMessage("Stock threshold must not exceed 10,000.")
            .When(x => x.StockThreshold.HasValue);

        RuleFor(x => x.Description)
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
            .Must(url => Uri.IsWellFormedUriString(url, UriKind.Absolute))
            .WithMessage("Main image URL is not valid.")
            .When(x => !string.IsNullOrWhiteSpace(x.MainImageUrl));

        RuleFor(x => x.AdditionalImageUrls)
            .Must(urls => urls == null || urls.Count <= 6)
            .WithMessage("Additional images must not exceed 6.");

        RuleForEach(x => x.AdditionalImageUrls!)
            .Must(url => Uri.IsWellFormedUriString(url, UriKind.Absolute))
            .WithMessage("Additional image URL is not valid.")
            .When(x => x.AdditionalImageUrls != null);
    }
}
