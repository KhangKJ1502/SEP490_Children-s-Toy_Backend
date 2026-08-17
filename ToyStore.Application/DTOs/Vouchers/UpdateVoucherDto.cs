namespace ToyStore.Application.DTOs.Vouchers;

/// <summary>
/// Data Transfer Object (DTO) phục vụ cập nhật thông tin Voucher (hỗ trợ cập nhật từng phần, các trường đều nullable).
/// </summary>
public class UpdateVoucherDto
{
    /// <summary>
    /// Mã code mới của voucher (nếu muốn thay đổi khi voucher chưa sử dụng).
    /// </summary>
    public string? VoucherCode { get; set; }

    /// <summary>
    /// Tên hiển thị mới của voucher.
    /// </summary>
    public string? VoucherName { get; set; }

    /// <summary>
    /// Mô tả chi tiết mới của voucher.
    /// </summary>
    public string? VoucherDescription { get; set; }

    /// <summary>
    /// Loại giảm giá mới ("FIXED" hoặc "PERCENTAGE").
    /// </summary>
    public string? DiscountType { get; set; }

    /// <summary>
    /// Giá trị giảm giá mới.
    /// </summary>
    public decimal? DiscountValue { get; set; }

    /// <summary>
    /// Mức giảm giá tối đa mới (VNĐ) khi áp dụng theo tỷ lệ phần trăm.
    /// </summary>
    public decimal? MaxDiscountCap { get; set; }

    /// <summary>
    /// Đối tượng giảm giá mới ("ORDER_TOTAL", "SHIPPING_FEE", hoặc "FINAL_PRICE").
    /// </summary>
    public string? DiscountTarget { get; set; }

    /// <summary>
    /// Giá trị đơn hàng tối thiểu mới (VNĐ).
    /// </summary>
    public decimal? MinOrderAmount { get; set; }

    /// <summary>
    /// Tổng số lượng voucher phát hành mới (phải >= số lượng đã dùng thực tế).
    /// </summary>
    public int? TotalQuantity { get; set; }

    /// <summary>
    /// Giới hạn số lần dùng mới trên mỗi tài khoản.
    /// </summary>
    public short? MaxUsagePerUser { get; set; }

    /// <summary>
    /// Thời điểm bắt đầu mới của voucher (UTC).
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Thời điểm kết thúc mới của voucher (UTC).
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Trạng thái mới của voucher (Scheduled, Active, Inactive, Expired, Pending, Rejected).
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Lý do từ chối hoặc ghi chú (bắt buộc khi Admin từ chối duyệt voucher).
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Đánh dấu yêu cầu xóa mềm (Soft Delete) voucher khi giá trị là true.
    /// </summary>
    public bool? IsDeleted { get; set; }
}
