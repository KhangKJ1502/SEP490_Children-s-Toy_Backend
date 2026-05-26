using Microsoft.EntityFrameworkCore;
using ToyStore.Application.DTOs.Campaigns;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Services.Resolvers;

public class ProductResolver : IBusinessObjectResolver
{
    private readonly SEP490ToyStoreContext _context;

    public ProductResolver(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    public string ReferenceType => "PRODUCT";

    public IReadOnlyList<PlaceholderInfoDto> AvailablePlaceholders =>
    [
        new() { Token = "{{ProductName}}", Description = "Product name" },
        new() { Token = "{{Price}}",       Description = "Selling price (formatted currency)" },
        new() { Token = "{{ProductId}}",   Description = "Product ID" }
    ];

    public async Task<ResolvedReferenceDto?> ResolveAsync(int referenceId, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Where(p => p.ProductId == referenceId && !p.IsDeleted)
            .Select(p => new
            {
                p.ProductId,
                p.ProductName,
                p.Price,
                ImageUrl = p.ProductImage != null ? p.ProductImage.ImageUrl : null
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (product is null) return null;

        return new ResolvedReferenceDto
        {
            DisplayName = product.ProductName,
            ImageUrl = product.ImageUrl,
            DefaultActionTarget = $"/products/{product.ProductId}",
            Placeholders = new Dictionary<string, string>
            {
                ["{{ProductName}}"] = product.ProductName,
                ["{{Price}}"]       = product.Price.ToString("N0"),
                ["{{ProductId}}"]   = product.ProductId.ToString()
            }
        };
    }
}
