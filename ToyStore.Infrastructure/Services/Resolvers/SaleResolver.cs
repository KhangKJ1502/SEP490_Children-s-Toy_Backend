using Microsoft.EntityFrameworkCore;
using ToyStore.Application.DTOs.Campaigns;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Services.Resolvers;

/// <summary>
/// Service giải quyết thông tin tham chiếu Khuyến mãi/Sale (IBusinessObjectResolver) dùng để tự động điền các placeholder
/// và nạp danh sách khung giờ Flash Sale cho các chiến dịch marketing và mẫu thông báo.
/// </summary>
public class SaleResolver : IBusinessObjectResolver
{
    private readonly SEP490ToyStoreContext _context;

    /// <summary>
    /// Khởi tạo SaleResolver với DbContext.
    /// </summary>
    /// <param name="context">DbContext kết nối cơ sở dữ liệu.</param>
    public SaleResolver(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Loại đối tượng nghiệp vụ tham chiếu được nhận diện là "SALE".
    /// </summary>
    public string ReferenceType => "SALE";

    /// <summary>
    /// Danh sách các placeholder được hỗ trợ cho chiến dịch khuyến mãi.
    /// </summary>
    public IReadOnlyList<PlaceholderInfoDto> AvailablePlaceholders =>
    [
        new() { Token = "{{PromotionName}}", Description = "Promotion campaign name" },
        new() { Token = "{{StartDate}}",     Description = "Start (dd/MM/yyyy HH:mm, VN time)" },
        new() { Token = "{{EndDate}}",       Description = "End (dd/MM/yyyy HH:mm, VN time)" },
        new() { Token = "{{PromotionId}}",   Description = "Promotion ID" }
    ];

    /// <summary>
    /// Giải quyết và trích xuất dữ liệu của chương trình khuyến mãi theo ID (kèm khung giờ và danh sách sản phẩm) để điền vào thông báo.
    /// </summary>
    /// <param name="referenceId">Mã ID của Promotion.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Đối tượng ResolvedReferenceDto hoặc null nếu không tìm thấy.</returns>
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
                .ThenInclude(p => p.ProductImage)
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
                        ImageUrl = pps.Product != null && pps.Product.ProductImage != null ? pps.Product.ProductImage.ImageUrl : null,
                        SalePrice = pps.SalePrice,
                        DiscountPercent = pps.DiscountPercent,
                        SaleQuantity = pps.SaleQuantity,
                        SoldQuantity = pps.SoldQuantity,
                        ReservedQuantity = pps.ReservedQuantity
                    })
                    .ToList()
            })
            .ToList();

        var firstProductImg = timeSlotRows
            .SelectMany(ts => ts.PromotionProductSlots)
            .Where(pps => !pps.IsDeleted && pps.Product?.ProductImage != null)
            .Select(pps => pps.Product!.ProductImage!.ImageUrl)
            .FirstOrDefault();

        return new ResolvedReferenceDto
        {
            DisplayName = promotion.PromotionName,
            PromotionType = promotion.PromotionType,
            ImageUrl = firstProductImg,
            DefaultActionTarget = $"/?flashSale={promotion.PromotionId}",
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
