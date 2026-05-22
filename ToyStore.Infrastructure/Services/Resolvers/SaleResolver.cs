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
        new() { Token = "{{PromotionName}}", Description = "Promotion campaign name" },
        new() { Token = "{{StartDate}}",     Description = "Start (dd/MM/yyyy HH:mm, VN time)" },
        new() { Token = "{{EndDate}}",       Description = "End (dd/MM/yyyy HH:mm, VN time)" },
        new() { Token = "{{PromotionId}}",   Description = "Promotion ID" }
    ];

    public async Task<ResolvedReferenceDto?> ResolveAsync(int referenceId, CancellationToken cancellationToken = default)
    {
        var promotion = await _context.Promotions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PromotionId == referenceId && !p.IsDeleted, cancellationToken);

        if (promotion is null) return null;

        // Query slot trực tiếp theo PromotionId — ổn định hơn Include + AsSplitQuery
        // (một số cấu hình EF có thể trả collection rỗng dù SQL có dòng).
        var timeSlotRows = await _context.PromotionTimeSlots
            .AsNoTracking()
            .Where(ts => ts.PromotionId == referenceId && !ts.IsDeleted)
            .Include(ts => ts.PromotionProductSlots)
            .ThenInclude(pps => pps.Product)
            .OrderBy(ts => ts.StartAt)
            .ToListAsync(cancellationToken);

        // Luôn trả mảng (có thể rỗng) — tránh JSON null/omit khiến FE tưởng thiếu API.
        var flashSlots = timeSlotRows
            .Select(s => new ResolvedFlashTimeSlotDto
            {
                TimeSlotId = s.TimeSlotId,
                StartAtUtc = NormalizeUtc(s.StartAt),
                EndAtUtc = NormalizeUtc(s.EndAt),
                Status = s.Status,
                ProductLines = s.PromotionProductSlots
                    .Where(pps => !pps.IsDeleted)
                    .OrderBy(pps => pps.Product != null ? pps.Product.ProductName : "")
                    .Select(pps => new ResolvedFlashProductLineDto
                    {
                        SlotProductId = pps.SlotProductId,
                        ProductId = pps.ProductId,
                        ProductName = pps.Product != null ? pps.Product.ProductName : $"#{pps.ProductId}",
                        SalePrice = pps.SalePrice,
                        DiscountPercent = pps.DiscountPercent,
                        SaleQuantity = pps.SaleQuantity,
                        SoldQuantity = pps.SoldQuantity,
                        ReservedQuantity = pps.ReservedQuantity,
                        IsActive = pps.IsActive
                    })
                    .ToList()
            })
            .ToList();

        return new ResolvedReferenceDto
        {
            DisplayName = promotion.PromotionName,
            PromotionType = promotion.PromotionType,
            DefaultActionTarget = $"/sale/{promotion.PromotionId}",
            Placeholders = new Dictionary<string, string>
            {
                ["{{PromotionName}}"] = promotion.PromotionName,
                ["{{StartDate}}"] = ReferenceDisplayTime.FormatVietnamDateTime(promotion.StartDate),
                ["{{EndDate}}"] = ReferenceDisplayTime.FormatVietnamDateTime(promotion.EndDate),
                ["{{PromotionId}}"] = promotion.PromotionId.ToString()
            },
            FlashTimeSlots = flashSlots
        };
    }

    /// <summary>Chuẩn hoá DateTime từ DB (thường Unspecified) thành UTC để JSON và admin UI thống nhất.</summary>
    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

}
