namespace ToyStore.Application.DTOs.Vouchers;

/// <summary>
/// DTO rút gọn voucher cho danh sách phân trang.
/// </summary>
public class VoucherListDto
{
    public int VoucherId { get; set; }
    public string VoucherCode { get; set; } = string.Empty;
    public string VoucherName { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public string DiscountTarget { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public int? TotalQuantity { get; set; }
    public int UsedQuantity { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public string VoucherDescription { get; set; } = string.Empty;
    public short? MaxUsagePerUser { get; set; }
    public int? CurrentUserUsageCount { get; set; }
}
