namespace ToyStore.Application.DTOs.Vouchers;

/// <summary>
/// Data Transfer Object (DTO) đại diện cho thông tin chi tiết đầy đủ của một Voucher để trả về cho Client.
/// </summary>
public class VoucherDto
{
    /// <summary>
    /// Mã định danh duy nhất (ID) của Voucher.
    /// </summary>
    public int VoucherId { get; set; }

    /// <summary>
    /// ID tài khoản người dùng đã tạo Voucher này.
    /// </summary>
    public int CreatedBy { get; set; }

    /// <summary>
    /// Mã voucher (Code) dùng để nhập khi áp dụng giảm giá.
    /// </summary>
    public string VoucherCode { get; set; } = string.Empty;

    /// <summary>
    /// Tên hiển thị của voucher.
    /// </summary>
    public string VoucherName { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả chi tiết điều kiện hoặc phạm vi của voucher.
    /// </summary>
    public string VoucherDescription { get; set; } = string.Empty;

    /// <summary>
    /// Loại hình giảm giá ("FIXED" hoặc "PERCENTAGE").
    /// </summary>
    public string DiscountType { get; set; } = string.Empty;

    /// <summary>
    /// Giá trị giảm giá (số tiền VNĐ hoặc tỷ lệ phần trăm).
    /// </summary>
    public decimal DiscountValue { get; set; }

    /// <summary>
    /// Mức giảm giá tối đa (VNĐ) khi áp dụng theo tỷ lệ phần trăm.
    /// </summary>
    public decimal? MaxDiscountCap { get; set; }

    /// <summary>
    /// Đối tượng áp dụng giảm giá ("ORDER_TOTAL", "SHIPPING_FEE", hoặc "FINAL_PRICE").
    /// </summary>
    public string DiscountTarget { get; set; } = string.Empty;

    /// <summary>
    /// Giá trị đơn hàng tối thiểu (VNĐ) để được áp dụng voucher.
    /// </summary>
    public decimal? MinOrderAmount { get; set; }

    /// <summary>
    /// Tổng số lượng voucher tối đa được phát hành (null nếu không giới hạn).
    /// </summary>
    public int? TotalQuantity { get; set; }

    /// <summary>
    /// Số lượng voucher đã được sử dụng thực tế tính đến thời điểm hiện tại.
    /// </summary>
    public int UsedQuantity { get; set; }

    /// <summary>
    /// Số lần sử dụng tối đa của mỗi tài khoản khách hàng.
    /// </summary>
    public short? MaxUsagePerUser { get; set; }

    /// <summary>
    /// Thời điểm bắt đầu có hiệu lực của voucher (UTC).
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Thời điểm kết thúc hiệu lực của voucher (UTC).
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Trạng thái hiện tại của voucher (Scheduled, Active, Inactive, Expired, Pending, Rejected).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Lý do từ chối hoặc ghi chú duyệt của Admin.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Thời điểm tạo bản ghi voucher (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Thời điểm cập nhật bản ghi gần nhất (UTC), null nếu chưa từng chỉnh sửa.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
