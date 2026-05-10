namespace ToyStore.Application.DTOs.Vouchers;

/// <summary>
/// DTO tạo mới voucher.
/// </summary>
public class CreateVoucherDto
{
    public string VoucherCode { get; set; } = string.Empty;
    public string VoucherName { get; set; } = string.Empty;
    public string VoucherDescription { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountCap { get; set; }
    public string DiscountTarget { get; set; } = string.Empty;
    public decimal? MinOrderAmount { get; set; }
    public int? TotalQuantity { get; set; }
    public short? MaxUsagePerUser { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}
