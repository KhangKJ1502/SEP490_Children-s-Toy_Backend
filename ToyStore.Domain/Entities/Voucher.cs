using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

/// <summary>
/// Thực thể Voucher (Mã giảm giá/Khuyến mãi) trong cơ sở dữ liệu.
/// </summary>
public partial class Voucher
{
    /// <summary>
    /// Khóa chính định danh duy nhất cho Voucher.
    /// </summary>
    public int VoucherId { get; set; }

    /// <summary>
    /// ID tài khoản người dùng đã tạo ra voucher.
    /// </summary>
    public int CreatedBy { get; set; }

    /// <summary>
    /// Mã code khuyến mãi duy nhất (ví may: TET2026, FREESHIP50).
    /// </summary>
    public string VoucherCode { get; set; } = null!;

    /// <summary>
    /// Tên hiển thị của voucher.
    /// </summary>
    public string VoucherName { get; set; } = null!;

    /// <summary>
    /// Mô tả chi tiết nội dung và điều kiện áp dụng của voucher.
    /// </summary>
    public string VoucherDescription { get; set; } = null!;

    /// <summary>
    /// Loại giảm giá ("FIXED" hoặc "PERCENTAGE").
    /// </summary>
    public string DiscountType { get; set; } = null!;

    /// <summary>
    /// Giá trị giảm (tiền VNĐ hoặc %).
    /// </summary>
    public decimal DiscountValue { get; set; }

    /// <summary>
    /// Mức trần giảm giá tối đa (VNĐ) khi áp dụng theo tỷ lệ phần trăm.
    /// </summary>
    public decimal? MaxDiscountCap { get; set; }

    /// <summary>
    /// Đối tượng giảm giá ("ORDER_TOTAL", "SHIPPING_FEE", hoặc "FINAL_PRICE").
    /// </summary>
    public string DiscountTarget { get; set; } = null!;

    /// <summary>
    /// Giá trị đơn hàng tối thiểu (VNĐ) để đủ điều kiện áp dụng voucher.
    /// </summary>
    public decimal? MinOrderAmount { get; set; }

    /// <summary>
    /// Tổng số lượng voucher phát hành tối đa (null nếu không giới hạn).
    /// </summary>
    public int? TotalQuantity { get; set; }

    /// <summary>
    /// Số lượng voucher đã được sử dụng thực tế.
    /// </summary>
    public int UsedQuantity { get; set; }

    /// <summary>
    /// Số lần sử dụng tối đa của mỗi tài khoản khách hàng.
    /// </summary>
    public short? MaxUsagePerUser { get; set; }

    /// <summary>
    /// Thời điểm bắt đầu có hiệu lực (UTC).
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Thời điểm kết thúc hiệu lực (UTC).
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Trạng thái hiện tại của voucher (Scheduled, Active, Inactive, Expired, Pending, Rejected).
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Lý do từ chối phê duyệt từ Admin (nếu có).
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Đánh dấu bản ghi đã bị xóa mềm hay chưa (true: đã xóa).
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Thời điểm tạo bản ghi (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Thời điểm cập nhật bản ghi gần nhất (UTC).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property trỏ tới tài khoản người tạo voucher.
    /// </summary>
    public virtual Account CreatedByNavigation { get; set; } = null!;

    /// <summary>
    /// Danh sách các đơn hàng đã áp dụng voucher này.
    /// </summary>
    public virtual ICollection<OrderVoucher> OrderVouchers { get; set; } = new List<OrderVoucher>();

    /// <summary>
    /// Danh sách lịch sử các lượt sử dụng voucher của từng tài khoản.
    /// </summary>
    public virtual ICollection<VoucherUsageLog> VoucherUsageLogs { get; set; } = new List<VoucherUsageLog>();
}
