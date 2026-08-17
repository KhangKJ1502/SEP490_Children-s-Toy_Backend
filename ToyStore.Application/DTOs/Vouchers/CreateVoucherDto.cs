namespace ToyStore.Application.DTOs.Vouchers;

/// <summary>
/// Data Transfer Object (DTO) chứa dữ liệu đầu vào để tạo mới một Voucher.
/// </summary>
public class CreateVoucherDto
{
    /// <summary>
    /// Mã voucher (ví dụ: SUMMER2026, FREESHIP), là mã duy nhất dùng để áp dụng khi thanh toán.
    /// </summary>
    public string VoucherCode { get; set; } = string.Empty;

    /// <summary>
    /// Tên hiển thị của voucher (ví dụ: Giảm giá mùa hè 20%).
    /// </summary>
    public string VoucherName { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả chi tiết điều kiện hoặc thông tin của voucher.
    /// </summary>
    public string VoucherDescription { get; set; } = string.Empty;

    /// <summary>
    /// Loại hình giảm giá: "FIXED" (số tiền cố định) hoặc "PERCENTAGE" (phần trăm).
    /// </summary>
    public string DiscountType { get; set; } = string.Empty;

    /// <summary>
    /// Giá trị giảm giá: số tiền (VNĐ) nếu là FIXED, hoặc số phần trăm (1 - 100) nếu là PERCENTAGE.
    /// </summary>
    public decimal DiscountValue { get; set; }

    /// <summary>
    /// Mức giảm giá tối đa bằng tiền (VNĐ) khi áp dụng cho loại PERCENTAGE.
    /// </summary>
    public decimal? MaxDiscountCap { get; set; }

    /// <summary>
    /// Mục tiêu/phạm vi áp dụng giảm giá: "ORDER_TOTAL", "SHIPPING_FEE", hoặc "FINAL_PRICE".
    /// </summary>
    public string DiscountTarget { get; set; } = string.Empty;

    /// <summary>
    /// Giá trị đơn hàng tối thiểu (VNĐ) để đủ điều kiện áp dụng voucher.
    /// </summary>
    public decimal? MinOrderAmount { get; set; }

    /// <summary>
    /// Tổng số lượng voucher tối đa được phát hành (null nếu không giới hạn).
    /// </summary>
    public int? TotalQuantity { get; set; }

    /// <summary>
    /// Số lần sử dụng tối đa của mỗi tài khoản khách hàng (null nếu không giới hạn).
    /// </summary>
    public short? MaxUsagePerUser { get; set; }

    /// <summary>
    /// Thời điểm bắt đầu có hiệu lực của voucher (UTC).
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Thời điểm hết hiệu lực của voucher (UTC).
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Trạng thái ban đầu của voucher (ví dụ: Scheduled, Pending).
    /// </summary>
    public string Status { get; set; } = string.Empty;
}
