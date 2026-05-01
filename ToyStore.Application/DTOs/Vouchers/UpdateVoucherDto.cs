namespace ToyStore.Application.DTOs.Vouchers;

/// <summary>
/// DTO cập nhật voucher (partial update, mọi field đều nullable).
/// </summary>
public class UpdateVoucherDto
{
    public string? VoucherCode { get; set; }
    public string? VoucherName { get; set; }
    public string? VoucherDescription { get; set; }
    public string? DiscountType { get; set; }
    public decimal? DiscountValue { get; set; }
    public decimal? MaxDiscountCap { get; set; }
    public string? DiscountTarget { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public int? TotalQuantity { get; set; }
    public short? MaxUsagePerUser { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Status { get; set; }
    public bool? IsDeleted { get; set; }
}
