namespace ToyStore.Application.Common.Models;

public class SuperCategoryModel
{
    public short SuperCategoryId { get; set; }

    public string SuperCategoryName { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

public class CategoryModel
{
    public short CategoryId { get; set; }

    public short SuperCategoryId { get; set; }

    public string SuperCategoryName { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

public class ProductModel
{
    public int ProductId { get; set; }

    public short CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public short? BrandId { get; set; }

    public string? BrandName { get; set; }

    public byte? PriceRangeId { get; set; }

    public decimal? PriceRangeMin { get; set; }

    public decimal? PriceRangeMax { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public string ProductStatus { get; set; } = string.Empty;

    public DateTime? LaunchDate { get; set; }

    public short StockThreshold { get; set; }

    public bool LowStockNotificationEnabled { get; set; }

    public DateTime? LastLowStockNotifiedAt { get; set; }

    public string? Description { get; set; }

    public short? MaterialId { get; set; }

    public string? MaterialName { get; set; }

    public byte? AgeId { get; set; }

    public string? AgeRange { get; set; }

    public byte? SexId { get; set; }

    public string? SexName { get; set; }

    public byte? OriginId { get; set; }

    public string? OriginName { get; set; }

    public string? MainImageUrl { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

public class ProductCreateModel
{
    public short CategoryId { get; set; }

    public short? BrandId { get; set; }

    public byte? PriceRangeId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public string ProductStatus { get; set; } = string.Empty;

    public DateTime? LaunchDate { get; set; }

    public short StockThreshold { get; set; }

    public bool LowStockNotificationEnabled { get; set; }

    public string? Description { get; set; }

    public short? MaterialId { get; set; }

    public byte? AgeId { get; set; }

    public byte? SexId { get; set; }

    public byte? OriginId { get; set; }

    public string? MainImageUrl { get; set; }

    public bool HasDetail =>
        !string.IsNullOrWhiteSpace(Description)
        || MaterialId.HasValue
        || AgeId.HasValue
        || SexId.HasValue
        || OriginId.HasValue;
}

public class ProductUpdateModel
{
    public short? CategoryId { get; set; }

    public short? BrandId { get; set; }

    public byte? PriceRangeId { get; set; }

    public string? ProductName { get; set; }

    public decimal? Price { get; set; }

    public int? Quantity { get; set; }

    public string? ProductStatus { get; set; }

    public DateTime? LaunchDate { get; set; }

    public short? StockThreshold { get; set; }

    public bool? LowStockNotificationEnabled { get; set; }

    public string? Description { get; set; }

    public short? MaterialId { get; set; }

    public byte? AgeId { get; set; }

    public byte? SexId { get; set; }

    public byte? OriginId { get; set; }

    public string? MainImageUrl { get; set; }

    public bool HasDetail =>
        Description != null
        || MaterialId.HasValue
        || AgeId.HasValue
        || SexId.HasValue
        || OriginId.HasValue;
}

public class VoucherModel
{
    public int VoucherId { get; set; }

    public int? CreatedBy { get; set; }

    public string VoucherCode { get; set; } = string.Empty;

    public string VoucherName { get; set; } = string.Empty;

    public string VoucherDescription { get; set; } = string.Empty;

    public string DiscountType { get; set; } = string.Empty;

    public decimal DiscountValue { get; set; }

    public decimal? MaxDiscountCap { get; set; }

    public string DiscountTarget { get; set; } = string.Empty;

    public decimal? MinOrderAmount { get; set; }

    public int? TotalQuantity { get; set; }

    public int UsedQuantity { get; set; }

    public short? MaxUsagePerUser { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
