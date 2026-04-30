using Microsoft.EntityFrameworkCore;
using ToyStore.Application.DTOs.Campaigns;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Services.Resolvers;

public class SaleResolver : IBusinessObjectResolver
{
    private readonly SEP490ToyStoreContext _context;

    public SaleResolver(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    public string ReferenceType => "SALE";

    public IReadOnlyList<PlaceholderInfoDto> AvailablePlaceholders =>
    [
        new() { Token = "{{PromotionName}}", Description = "Ten chuong trinh sale" },
        new() { Token = "{{StartDate}}",     Description = "Ngay bat dau (dd/MM/yyyy)" },
        new() { Token = "{{EndDate}}",       Description = "Ngay ket thuc (dd/MM/yyyy)" },
        new() { Token = "{{PromotionId}}",   Description = "ID chuong trinh" }
    ];

    public async Task<ResolvedReferenceDto?> ResolveAsync(int referenceId, CancellationToken cancellationToken = default)
    {
        var promotion = await _context.Promotions
            .AsNoTracking()
            .Where(p => p.PromotionId == referenceId && !p.IsDeleted)
            .Select(p => new
            {
                p.PromotionId,
                p.PromotionName,
                p.StartDate,
                p.EndDate
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (promotion is null) return null;

        return new ResolvedReferenceDto
        {
            DisplayName = promotion.PromotionName,
            DefaultActionTarget = $"/sale/{promotion.PromotionId}",
            Placeholders = new Dictionary<string, string>
            {
                ["{{PromotionName}}"] = promotion.PromotionName,
                ["{{StartDate}}"]     = promotion.StartDate.ToString("dd/MM/yyyy"),
                ["{{EndDate}}"]       = promotion.EndDate.ToString("dd/MM/yyyy"),
                ["{{PromotionId}}"]   = promotion.PromotionId.ToString()
            }
        };
    }
}
