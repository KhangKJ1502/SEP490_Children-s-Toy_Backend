using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ToyStore.Application.DTOs.Recommendations;
using ToyStore.Infrastructure.Data;
using ToyStore.Recommendation.MongoDb.Documents;

namespace ToyStore.Recommendation.Algorithms;

/// <summary>
/// Lọc danh sách ứng viên gợi ý theo Business Rules (BẮT BUỘC theo spec):
///  - Loại sản phẩm IsDeleted = 1.
///  - Loại sản phẩm ProductStatus != 'Active'.
///  - Loại sản phẩm Quantity = 0.
///  - Loại sản phẩm đã có trong purchasedProductIds của user.
///  - Ưu tiên sản phẩm đang có PromotionID (gia tăng score nhẹ).
/// Đồng thời enrich data product để FE render (ảnh, giá, tên, ...).
/// </summary>
public class BusinessRulesFilter
{
    private readonly SEP490ToyStoreContext _db;
    private readonly ILogger<BusinessRulesFilter> _logger;

    public BusinessRulesFilter(SEP490ToyStoreContext db, ILogger<BusinessRulesFilter> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Áp dụng business rules + enrich data product → trả về danh sách RecommendationItemDto sẵn sàng cho FE.
    /// </summary>
    /// <param name="candidates">Ứng viên đầu ra của 1 algorithm (đã sort theo score giảm dần).</param>
    /// <param name="maxItems">Số item tối đa muốn lấy.</param>
    /// <param name="userProfile">Profile MongoDB (để biết user đã mua gì) — nullable cho guest.</param>
    /// <param name="skipPurchasedFilter">Nếu true, KHÔNG loại bỏ sản phẩm đã mua (dùng cho trending/public widgets).</param>
    public async Task<List<RecommendationItemDto>> FilterAndEnrichAsync(
        IList<RecommendationCandidate> candidates,
        int maxItems,
        UserProfileDocument? userProfile,
        CancellationToken ct,
        bool skipPurchasedFilter = false)
    {
        if (candidates.Count == 0 || maxItems <= 0)
            return new List<RecommendationItemDto>();

        // 1. Loại các product đã mua (chỉ cho widget cá nhân hóa, KHÔNG áp dụng cho trending/public widgets)
        var purchasedSet = skipPurchasedFilter 
            ? new HashSet<int>() 
            : (userProfile?.PurchasedProductIds?.ToHashSet() ?? new HashSet<int>());
        
        var candidateIds = candidates
            .Where(c => !purchasedSet.Contains(c.ProductId))
            .Select(c => c.ProductId)
            .Distinct()
            .ToList();

        _logger.LogInformation(
            "BusinessRulesFilter: {CandidateCount} candidates, {PurchasedCount} purchased, {RemainingCount} after purchase filter, skipPurchasedFilter={Skip}",
            candidates.Count, purchasedSet.Count, candidateIds.Count, skipPurchasedFilter);

        if (candidateIds.Count == 0)
            return new List<RecommendationItemDto>();

        // 2. Lấy product enrich (kèm ảnh, rating, soldQty, promotion) — chỉ Active + còn hàng
        //    Giới hạn lấy candidateIds + lấy gấp 2 lần maxItems để có buffer khi filter business
        var bufferLimit = Math.Min(candidateIds.Count, Math.Max(maxItems * 3, maxItems + 20));
        var idsToFetch = candidateIds.Take(bufferLimit).ToList();

        var now = DateTime.UtcNow;

        var products = await _db.Products
            .AsNoTracking()
            .Where(p => idsToFetch.Contains(p.ProductId)
                     && !p.IsDeleted
                     && (p.ProductStatus == "Active" || p.ProductStatus == "active")
                     && p.Quantity > 0)
            .Select(p => new EnrichedProduct
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Price = p.Price,
                Quantity = p.Quantity,
                ProductStatus = p.ProductStatus,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.CategoryName,
                BrandId = p.BrandId,
                BrandName = p.Brand != null ? p.Brand.BrandName : null,
                MainImageUrl = p.ProductImage != null ? p.ProductImage.ImageUrl : null,
                AverageRating = p.ReviewProducts.Where(r => !r.IsDeleted).Select(r => (double?)r.Rating).Average(),
                ReviewCount = p.ReviewProducts.Count(r => !r.IsDeleted),
                SoldQuantity = p.OrderDetails
                    .Where(od => !od.Order.IsDeleted && od.Order.CancelledAt == null)
                    .Sum(od => (int?)od.Quantity) ?? 0,
                // --- Flash Sale (ưu tiên cao nhất) ---
                BestFlashSalePrice = p.PromotionProductSlots
                    .Where(pps => !pps.IsDeleted
                        && pps.TimeSlot != null
                        && pps.TimeSlot.Status == "Active"
                        && pps.TimeSlot.StartAt <= now
                        && pps.TimeSlot.EndAt >= now
                        && pps.TimeSlot.Promotion != null
                        && !pps.TimeSlot.Promotion.IsDeleted
                        && (pps.TimeSlot.Promotion.Status == "Active" || pps.TimeSlot.Promotion.Status == "Scheduled")
                        && (pps.SoldQuantity + pps.ReservedQuantity) < pps.SaleQuantity)
                    .OrderByDescending(pps => pps.TimeSlot.Promotion.Priority)
                    .ThenBy(pps => pps.SalePrice)
                    .Select(pps => (decimal?)pps.SalePrice)
                    .FirstOrDefault(),
                BestFlashSaleDiscountPercent = p.PromotionProductSlots
                    .Where(pps => !pps.IsDeleted
                        && pps.TimeSlot != null
                        && pps.TimeSlot.Status == "Active"
                        && pps.TimeSlot.StartAt <= now
                        && pps.TimeSlot.EndAt >= now
                        && pps.TimeSlot.Promotion != null
                        && !pps.TimeSlot.Promotion.IsDeleted
                        && (pps.TimeSlot.Promotion.Status == "Active" || pps.TimeSlot.Promotion.Status == "Scheduled")
                        && (pps.SoldQuantity + pps.ReservedQuantity) < pps.SaleQuantity)
                    .OrderByDescending(pps => pps.TimeSlot.Promotion.Priority)
                    .ThenBy(pps => pps.SalePrice)
                    .Select(pps => pps.DiscountPercent)
                    .FirstOrDefault(),
                BestFlashSalePromotionType = p.PromotionProductSlots
                    .Where(pps => !pps.IsDeleted
                        && pps.TimeSlot != null
                        && pps.TimeSlot.Status == "Active"
                        && pps.TimeSlot.StartAt <= now
                        && pps.TimeSlot.EndAt >= now
                        && pps.TimeSlot.Promotion != null
                        && !pps.TimeSlot.Promotion.IsDeleted
                        && (pps.TimeSlot.Promotion.Status == "Active" || pps.TimeSlot.Promotion.Status == "Scheduled")
                        && (pps.SoldQuantity + pps.ReservedQuantity) < pps.SaleQuantity)
                    .OrderByDescending(pps => pps.TimeSlot.Promotion.Priority)
                    .ThenBy(pps => pps.SalePrice)
                    .Select(pps => pps.TimeSlot.Promotion.PromotionType)
                    .FirstOrDefault(),
                // --- Regular Promotion ---
                BestRegularPromoPrice = p.ProductPromotions
                    .Where(pp => !pp.IsDeleted
                        && pp.Promotion != null
                        && !pp.Promotion.IsDeleted
                        && (pp.Promotion.Status == "Active" || pp.Promotion.Status == "Scheduled")
                        && pp.Promotion.StartDate <= now
                        && pp.Promotion.EndDate >= now)
                    .OrderByDescending(pp => pp.Promotion.Priority)
                    .ThenBy(pp => pp.SalePrice)
                    .Select(pp => (decimal?)pp.SalePrice)
                    .FirstOrDefault(),
                BestRegularPromoDiscountPercent = p.ProductPromotions
                    .Where(pp => !pp.IsDeleted
                        && pp.Promotion != null
                        && !pp.Promotion.IsDeleted
                        && (pp.Promotion.Status == "Active" || pp.Promotion.Status == "Scheduled")
                        && pp.Promotion.StartDate <= now
                        && pp.Promotion.EndDate >= now)
                    .OrderByDescending(pp => pp.Promotion.Priority)
                    .ThenBy(pp => pp.SalePrice)
                    .Select(pp => pp.DiscountPercent)
                    .FirstOrDefault(),
                BestRegularPromoPromotionType = p.ProductPromotions
                    .Where(pp => !pp.IsDeleted
                        && pp.Promotion != null
                        && !pp.Promotion.IsDeleted
                        && (pp.Promotion.Status == "Active" || pp.Promotion.Status == "Scheduled")
                        && pp.Promotion.StartDate <= now
                        && pp.Promotion.EndDate >= now)
                    .OrderByDescending(pp => pp.Promotion.Priority)
                    .ThenBy(pp => pp.SalePrice)
                    .Select(pp => pp.Promotion.PromotionType)
                    .FirstOrDefault(),
            })
            .ToListAsync(ct);

        _logger.LogInformation(
            "BusinessRulesFilter: Queried {IdsCount} product IDs, found {ProductsCount} valid products (Active + Quantity > 0)",
            idsToFetch.Count, products.Count);

        if (products.Count == 0)
            return new List<RecommendationItemDto>();

        var productMap = products.ToDictionary(p => p.ProductId);

        // 3. Build ranked list theo thứ tự candidate, ưu tiên promotion (+10% score)
        var ranked = new List<RecommendationItemDto>();
        foreach (var c in candidates)
        {
            if (!productMap.TryGetValue(c.ProductId, out var p)) continue;
            if (purchasedSet.Contains(c.ProductId)) continue;

            var finalScore = c.Score;

            // Chọn promotion tốt nhất: Flash Sale ưu tiên hơn Regular
            var discountedPrice = p.BestFlashSalePrice ?? p.BestRegularPromoPrice;

            // Boost score 10% nếu sản phẩm đang có khuyến mãi
            if (discountedPrice.HasValue && discountedPrice.Value < p.Price)
            {
                finalScore *= 1.1m; // boost 10% — ưu tiên item đang khuyến mãi
            }

            var rawDiscountPercent = p.BestFlashSalePrice.HasValue
                ? p.BestFlashSaleDiscountPercent
                : p.BestRegularPromoDiscountPercent;
            var promotionType = p.BestFlashSalePrice.HasValue
                ? p.BestFlashSalePromotionType
                : p.BestRegularPromoPromotionType;

            // Tính lại discountPercent nếu không có sẵn (fallback)
            int? discountPercent = null;
            if (discountedPrice.HasValue && discountedPrice.Value < p.Price && p.Price > 0)
            {
                discountPercent = rawDiscountPercent.HasValue
                    ? (int)Math.Round((double)rawDiscountPercent.Value)
                    : (int)Math.Round((1 - (double)(discountedPrice.Value / p.Price)) * 100);
            }

            ranked.Add(new RecommendationItemDto
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Price = p.Price,
                DiscountedPrice = (discountedPrice.HasValue && discountedPrice.Value < p.Price) ? discountedPrice : null,
                DiscountPercent = discountPercent,
                PromotionType = promotionType,
                Quantity = p.Quantity,
                ProductStatus = p.ProductStatus,
                CategoryId = p.CategoryId,
                CategoryName = p.CategoryName,
                BrandId = p.BrandId,
                BrandName = p.BrandName,
                MainImageUrl = p.MainImageUrl,
                AverageRating = p.AverageRating,
                ReviewCount = p.ReviewCount,
                SoldQuantity = p.SoldQuantity,
                Score = Math.Round(finalScore, 4),
                Reason = c.Reason,
                ReasonCode = c.ReasonCode,
            });

            if (ranked.Count >= maxItems) break;
        }

        // Sort lần cuối theo score giảm dần (đã có boost promotion)
        return ranked.OrderByDescending(x => x.Score).Take(maxItems).ToList();
    }

    private sealed class EnrichedProduct
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string ProductStatus { get; set; } = string.Empty;
        public short CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public short? BrandId { get; set; }
        public string? BrandName { get; set; }
        public string? MainImageUrl { get; set; }
        public double? AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public int SoldQuantity { get; set; }
        // Flash Sale
        public decimal? BestFlashSalePrice { get; set; }
        public decimal? BestFlashSaleDiscountPercent { get; set; }
        public string? BestFlashSalePromotionType { get; set; }
        // Regular Promotion
        public decimal? BestRegularPromoPrice { get; set; }
        public decimal? BestRegularPromoDiscountPercent { get; set; }
        public string? BestRegularPromoPromotionType { get; set; }
    }
}
