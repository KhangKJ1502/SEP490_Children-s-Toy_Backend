using AutoMapper;
using ToyStore.Application.DTOs.Products;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Mappings;

/// <summary>
/// Cấu hình AutoMapper cho Product.
/// </summary>
public class ProductProfile : Profile
{
    public ProductProfile()
    {
        CreateMap<Product, ProductListDto>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.CategoryName))
            .ForMember(dest => dest.BrandName, opt => opt.MapFrom(src => src.Brand != null ? src.Brand.BrandName : null))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.IsDeleted ? "Inactive" : "Active"))
            .ForMember(dest => dest.MainImageUrl, opt => opt.MapFrom(src => src.ProductImage != null ? src.ProductImage.ImageUrl : null))
            .ForMember(dest => dest.DiscountedPrice, opt => opt.MapFrom(src => GetDiscountedPrice(src)))
            .ForMember(dest => dest.DiscountPercent, opt => opt.MapFrom(src => GetDiscountPercent(src)))
            .ForMember(dest => dest.PromotionType, opt => opt.MapFrom(src => GetPromotionType(src)));

        CreateMap<Product, InventoryReportItemDto>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.CategoryName))
            .ForMember(dest => dest.BrandName, opt => opt.MapFrom(src => src.Brand != null ? src.Brand.BrandName : null))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.IsDeleted ? "Inactive" : "Active"))
            .ForMember(dest => dest.DiscountedPrice, opt => opt.MapFrom(src => GetDiscountedPrice(src)))
            .ForMember(dest => dest.DiscountPercent, opt => opt.MapFrom(src => GetDiscountPercent(src)))
            .ForMember(dest => dest.PromotionType, opt => opt.MapFrom(src => GetPromotionType(src)))
            .ForMember(dest => dest.InventoryValue, opt => opt.MapFrom(src => src.Price * src.Quantity))
            .ForMember(dest => dest.LowStock, opt => opt.MapFrom(src => src.Quantity <= src.StockThreshold))
            .ForMember(dest => dest.AverageRating, opt => opt.MapFrom(src =>
                src.ReviewProducts
                    .Where(r => !r.IsDeleted && r.ModerationStatus == "Approved")
                    .Select(r => (double?)r.Rating)
                    .Average()))
            .ForMember(dest => dest.ReviewCount, opt => opt.MapFrom(src =>
                src.ReviewProducts.Count(r => !r.IsDeleted && r.ModerationStatus == "Approved")))
            .ForMember(dest => dest.SoldQuantity, opt => opt.MapFrom(src =>
                src.OrderDetails
                    .Where(od => !od.Order.IsDeleted && od.Order.CancelledAt == null)
                    .Sum(od => (int?)od.Quantity) ?? 0));

        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.CategoryName))
            .ForMember(dest => dest.BrandName, opt => opt.MapFrom(src => src.Brand != null ? src.Brand.BrandName : null))
            .ForMember(dest => dest.DiscountedPrice, opt => opt.MapFrom(src => GetDiscountedPrice(src)))
            .ForMember(dest => dest.DiscountPercent, opt => opt.MapFrom(src => GetDiscountPercent(src)))
            .ForMember(dest => dest.PromotionType, opt => opt.MapFrom(src => GetPromotionType(src)))
            .ForMember(dest => dest.PromotionSoldQuantity, opt => opt.MapFrom(src => GetPromotionSoldQuantity(src)))
            .ForMember(dest => dest.PromotionSaleQuantity, opt => opt.MapFrom(src => GetPromotionSaleQuantity(src)))
            .ForMember(dest => dest.PriceRangeMin, opt => opt.MapFrom(src => GetPriceRangeMin(src)))
            .ForMember(dest => dest.PriceRangeMax, opt => opt.MapFrom(src => GetPriceRangeMax(src)))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.ProductDetail != null ? src.ProductDetail.Description : null))
            .ForMember(dest => dest.MaterialId, opt => opt.MapFrom(src => src.ProductDetail != null ? src.ProductDetail.MaterialId : null))
            .ForMember(dest => dest.MaterialName, opt => opt.MapFrom(src => src.ProductDetail != null && src.ProductDetail.Material != null ? src.ProductDetail.Material.MaterialName : null))
            .ForMember(dest => dest.AgeId, opt => opt.MapFrom(src => src.ProductDetail != null ? src.ProductDetail.AgeId : null))
            .ForMember(dest => dest.AgeRange, opt => opt.MapFrom(src => src.ProductDetail != null && src.ProductDetail.Age != null ? src.ProductDetail.Age.AgeRange : null))
            .ForMember(dest => dest.SexId, opt => opt.MapFrom(src => src.ProductDetail != null ? src.ProductDetail.SexId : null))
            .ForMember(dest => dest.SexName, opt => opt.MapFrom(src => src.ProductDetail != null && src.ProductDetail.Sex != null ? src.ProductDetail.Sex.SexName : null))
            .ForMember(dest => dest.OriginId, opt => opt.MapFrom(src => src.ProductDetail != null ? src.ProductDetail.OriginId : null))
            .ForMember(dest => dest.OriginName, opt => opt.MapFrom(src => src.ProductDetail != null && src.ProductDetail.Origin != null ? src.ProductDetail.Origin.OriginName : null))
            .ForMember(dest => dest.WeightGram, opt => opt.MapFrom(src => src.ProductDetail != null ? (int?)src.ProductDetail.WeightGram : null))
            .ForMember(dest => dest.LengthCm, opt => opt.MapFrom(src => src.ProductDetail != null ? (int?)src.ProductDetail.LengthCm : null))
            .ForMember(dest => dest.WidthCm, opt => opt.MapFrom(src => src.ProductDetail != null ? (int?)src.ProductDetail.WidthCm : null))
            .ForMember(dest => dest.HeightCm, opt => opt.MapFrom(src => src.ProductDetail != null ? (int?)src.ProductDetail.HeightCm : null))
            .ForMember(dest => dest.MainImageUrl, opt => opt.MapFrom(src => src.ProductImage != null ? src.ProductImage.ImageUrl : null))
            .ForMember(dest => dest.AverageRating, opt => opt.MapFrom(src =>
                src.ReviewProducts
                    .Where(r => !r.IsDeleted && r.ModerationStatus == "Approved")
                    .Select(r => (double?)r.Rating)
                    .Average()))
            .ForMember(dest => dest.ReviewCount, opt => opt.MapFrom(src =>
                src.ReviewProducts.Count(r => !r.IsDeleted && r.ModerationStatus == "Approved")))
            .ForMember(dest => dest.SoldQuantity, opt => opt.MapFrom(src =>
                src.OrderDetails
                    .Where(od => !od.Order.IsDeleted && od.Order.CancelledAt == null)
                    .Sum(od => (int?)od.Quantity) ?? 0));

        CreateMap<CreateProductDto, Product>()
            .ForMember(dest => dest.ProductId, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.LastLowStockNotifiedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Brand, opt => opt.Ignore())
            .ForMember(dest => dest.CartItems, opt => opt.Ignore())
            .ForMember(dest => dest.Category, opt => opt.Ignore())
            .ForMember(dest => dest.ItemSimilaritySimilarProducts, opt => opt.Ignore())
            .ForMember(dest => dest.ItemSimilaritySourceProducts, opt => opt.Ignore())
            .ForMember(dest => dest.OrderDetails, opt => opt.Ignore())
            .ForMember(dest => dest.PriceRange, opt => opt.Ignore())
            .ForMember(dest => dest.ProductFollowers, opt => opt.Ignore())
            .ForMember(dest => dest.ProductPromotions, opt => opt.Ignore())
            .ForMember(dest => dest.ReviewProducts, opt => opt.Ignore())
            .ForMember(dest => dest.TrendingProducts, opt => opt.Ignore())
            .ForMember(dest => dest.UserProductScores, opt => opt.Ignore())
            .ForMember(dest => dest.Wishlists, opt => opt.Ignore())
            .ForMember(dest => dest.ProductDetail, opt => opt.MapFrom(src => BuildProductDetail(src)))
            .ForMember(dest => dest.ProductImage, opt => opt.MapFrom(src => BuildProductImage(src)));

        CreateMap<UpdateProductDto, Product>()
            .ForMember(dest => dest.ProductId, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.LastLowStockNotifiedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Brand, opt => opt.Ignore())
            .ForMember(dest => dest.CartItems, opt => opt.Ignore())
            .ForMember(dest => dest.Category, opt => opt.Ignore())
            .ForMember(dest => dest.ItemSimilaritySimilarProducts, opt => opt.Ignore())
            .ForMember(dest => dest.ItemSimilaritySourceProducts, opt => opt.Ignore())
            .ForMember(dest => dest.OrderDetails, opt => opt.Ignore())
            .ForMember(dest => dest.PriceRange, opt => opt.Ignore())
            .ForMember(dest => dest.ProductFollowers, opt => opt.Ignore())
            .ForMember(dest => dest.ProductPromotions, opt => opt.Ignore())
            .ForMember(dest => dest.ReviewProducts, opt => opt.Ignore())
            .ForMember(dest => dest.TrendingProducts, opt => opt.Ignore())
            .ForMember(dest => dest.UserProductScores, opt => opt.Ignore())
            .ForMember(dest => dest.Wishlists, opt => opt.Ignore())
            .ForMember(dest => dest.ProductDetail, opt => opt.Ignore())
            .ForMember(dest => dest.ProductImage, opt => opt.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
    }

    private static ProductDetail? BuildProductDetail(CreateProductDto src)
    {
        if (string.IsNullOrWhiteSpace(src.Description) && !src.MaterialId.HasValue &&
            !src.AgeId.HasValue && !src.SexId.HasValue && !src.OriginId.HasValue &&
            src.WeightGram <= 0 && src.LengthCm <= 0 && src.WidthCm <= 0 && src.HeightCm <= 0)
        {
            return null;
        }

        return new ProductDetail
        {
            Description = src.Description,
            MaterialId = src.MaterialId,
            AgeId = src.AgeId,
            SexId = src.SexId,
            OriginId = src.OriginId,
            WeightGram = src.WeightGram,
            LengthCm = src.LengthCm,
            WidthCm = src.WidthCm,
            HeightCm = src.HeightCm
        };
    }

    private static ProductImage? BuildProductImage(CreateProductDto src)
    {
        if (string.IsNullOrWhiteSpace(src.MainImageUrl))
        {
            return null;
        }

        return new ProductImage
        {
            ImageUrl = src.MainImageUrl
        };
    }

    private static decimal? GetPriceRangeMin(Product src)
    {
        return src.PriceRange?.PriceRangeMin;
    }

    private static decimal? GetPriceRangeMax(Product src)
    {
        return src.PriceRange?.PriceRangeMax;
    }

    private static (decimal SalePrice, string PromotionType, int? SoldQuantity, int? SaleQuantity)? GetActivePromotionData(Product src)
    {
        var now = DateTime.UtcNow;

        // 1. Flash Sale (Ưu tiên hàng đầu)
        var activeFlashSale = src.PromotionProductSlots
            .Where(pps => !pps.IsDeleted
                         && pps.TimeSlot != null
                         && string.Equals(pps.TimeSlot.Status, "Active", StringComparison.OrdinalIgnoreCase)
                         && pps.TimeSlot.StartAt <= now
                         && pps.TimeSlot.EndAt >= now
                         && pps.TimeSlot.Promotion != null
                         && !pps.TimeSlot.Promotion.IsDeleted
                         && (string.Equals(pps.TimeSlot.Promotion.Status, "Active", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(pps.TimeSlot.Promotion.Status, "Scheduled", StringComparison.OrdinalIgnoreCase))
                         && (pps.SoldQuantity + pps.ReservedQuantity < pps.SaleQuantity))
            .OrderByDescending(pps => pps.TimeSlot.Promotion.Priority)
            .ThenBy(pps => pps.SalePrice)
            .FirstOrDefault();

        if (activeFlashSale != null)
        {
            return (activeFlashSale.SalePrice, activeFlashSale.TimeSlot!.Promotion.PromotionType, activeFlashSale.SoldQuantity, activeFlashSale.SaleQuantity);
        }

        // 2. Regular Promotion
        var bestRegularPromotion = src.ProductPromotions
            .Where(pp => !pp.IsDeleted
                         && pp.Promotion != null
                         && !pp.Promotion.IsDeleted
                         && (string.Equals(pp.Promotion.Status, "Active", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(pp.Promotion.Status, "Scheduled", StringComparison.OrdinalIgnoreCase))
                         && pp.Promotion.StartDate <= now
                         && pp.Promotion.EndDate >= now
                         && (!pp.SaleQuantity.HasValue || pp.SoldQuantity + pp.ReservedQuantity < pp.SaleQuantity.Value)
                         && (pp.Promotion.PromotionTimeSlots == null || pp.Promotion.PromotionTimeSlots.Count == 0 || pp.Promotion.PromotionTimeSlots.Any(slot =>
                                string.Equals(slot.Status, "Active", StringComparison.OrdinalIgnoreCase)
                                && slot.StartAt <= now
                                && slot.EndAt >= now)))
            .OrderByDescending(pp => pp.Promotion.Priority)
            .ThenBy(pp => pp.SalePrice)
            .FirstOrDefault();

        if (bestRegularPromotion != null)
        {
            return (bestRegularPromotion.SalePrice, bestRegularPromotion.Promotion.PromotionType, bestRegularPromotion.SoldQuantity, bestRegularPromotion.SaleQuantity);
        }

        return null;
    }

    private static decimal? GetDiscountedPrice(Product src)
    {
        return GetActivePromotionData(src)?.SalePrice;
    }

    private static int? GetDiscountPercent(Product src)
    {
        var active = GetActivePromotionData(src);
        if (active == null || src.Price <= 0) return null;
        
        return (int)Math.Round((1 - (active.Value.SalePrice / src.Price)) * 100);
    }

    private static string? GetPromotionType(Product src)
    {
        return GetActivePromotionData(src)?.PromotionType;
    }

    private static int? GetPromotionSoldQuantity(Product src)
    {
        return GetActivePromotionData(src)?.SoldQuantity;
    }

    private static int? GetPromotionSaleQuantity(Product src)
    {
        return GetActivePromotionData(src)?.SaleQuantity;
    }
}
