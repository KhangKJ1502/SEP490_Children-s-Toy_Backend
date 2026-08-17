namespace ToyStore.Application.DTOs.Vouchers;

/// <summary>
/// Data Transfer Object (DTO) dạng rút gọn phục vụ cho việc hiển thị danh sách voucher trong các bảng phân trang.
/// </summary>
public class VoucherListDto
{
    /// <summary>
    /// Mã ID của voucher.
    /// </summary>
    public int VoucherId { get; set; }

    /// <summary>
    /// Mã code voucher.
    /// </summary>
    public string VoucherCode { get; set; } = string.Empty;

    /// <summary>
    /// Tên hiển thị của voucher.
    /// </summary>
    public string VoucherName { get; set; } = string.Empty;

    /// <summary>
    /// Loại hình giảm giá ("FIXED" hoặc "PERCENTAGE").
    /// </summary>
    public string DiscountType { get; set; } = string.Empty;

    /// <summary>
    /// Giá trị giảm giá (VNĐ hoặc %).
    /// </summary>
    public decimal DiscountValue { get; set; }

    /// <summary>
    /// Đối tượng áp dụng ("ORDER_TOTAL", "SHIPPING_FEE", hoặc "FINAL_PRICE").
    /// </summary>
    public string DiscountTarget { get; set; } = string.Empty;

    /// <summary>
    /// Thời điểm bắt đầu hiệu lực (UTC).
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Thời điểm kết thúc hiệu lực (UTC).
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Trạng thái voucher (Scheduled, Active, Inactive, Expired, Pending, Rejected).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Lý do từ chối (nếu có).
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Tổng số lượng phát hành tối đa (null nếu không giới hạn).
    /// </summary>
    public int? TotalQuantity { get; set; }

    /// <summary>
    /// Số lượng đã sử dụng thực tế.
    /// </summary>
    public int UsedQuantity { get; set; }

    /// <summary>
    /// Giá trị đơn hàng tối thiểu để áp dụng.
    /// </summary>
    public decimal? MinOrderAmount { get; set; }

    /// <summary>
    /// Mô tả chi tiết voucher.
    /// </summary>
    public string VoucherDescription { get; set; } = string.Empty;

    /// <summary>
    /// Giới hạn số lần dùng trên mỗi tài khoản.
    /// </summary>
    public short? MaxUsagePerUser { get; set; }

    /// <summary>
    /// Số lần tài khoản người dùng đang đăng nhập đã sử dụng voucher này (null nếu chưa đăng nhập).
    /// </summary>
    public int? CurrentUserUsageCount { get; set; }
}
