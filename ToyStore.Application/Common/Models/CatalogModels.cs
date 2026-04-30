namespace ToyStore.Application.Common.Models;

public class BrandModel
{
    public short BrandId { get; set; }

    public string BrandName { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
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
