using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

/// <summary>
/// Thực thể quan hệ nhiều-nhiều liên kết giữa Đơn hàng (Order) và Voucher được áp dụng, lưu lại số tiền giảm thực tế.
/// </summary>
public partial class OrderVoucher
{
    /// <summary>
    /// Mã ID đơn hàng (khóa chính kết hợp).
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Mã ID của voucher được áp dụng (khóa chính kết hợp).
    /// </summary>
    public int VoucherId { get; set; }

    /// <summary>
    /// Số tiền thực tế được giảm trừ vào đơn hàng từ voucher này (VNĐ).
    /// </summary>
    public decimal DiscountAmountApplied { get; set; }

    /// <summary>
    /// Đối tượng áp dụng của voucher tại thời điểm đặt hàng ("ORDER_TOTAL", "SHIPPING_FEE", hoặc "FINAL_PRICE").
    /// </summary>
    public string VoucherTarget { get; set; } = null!;

    /// <summary>
    /// Navigation property trỏ tới Đơn hàng.
    /// </summary>
    public virtual Order Order { get; set; } = null!;

    /// <summary>
    /// Navigation property trỏ tới Voucher.
    /// </summary>
    public virtual Voucher Voucher { get; set; } = null!;
}
