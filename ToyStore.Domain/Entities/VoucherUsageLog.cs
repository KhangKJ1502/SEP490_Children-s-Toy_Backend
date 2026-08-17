using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

/// <summary>
/// Thực thể lưu nhật ký lượt sử dụng Voucher của từng tài khoản khách hàng gắn liền với từng đơn hàng cụ thể.
/// </summary>
public partial class VoucherUsageLog
{
    /// <summary>
    /// Mã định danh duy nhất (khóa chính) của bản ghi nhật ký sử dụng voucher.
    /// </summary>
    public int UsageId { get; set; }

    /// <summary>
    /// Mã ID của Voucher được sử dụng.
    /// </summary>
    public int VoucherId { get; set; }

    /// <summary>
    /// Mã ID tài khoản khách hàng đã sử dụng voucher.
    /// </summary>
    public int AccountId { get; set; }

    /// <summary>
    /// Mã ID đơn hàng áp dụng voucher.
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Thời điểm áp dụng/sử dụng voucher (UTC).
    /// </summary>
    public DateTime UsedAt { get; set; }

    /// <summary>
    /// Navigation property tới tài khoản khách hàng.
    /// </summary>
    public virtual Account Account { get; set; } = null!;

    /// <summary>
    /// Navigation property tới đơn hàng áp dụng voucher.
    /// </summary>
    public virtual Order Order { get; set; } = null!;

    /// <summary>
    /// Navigation property tới Voucher được sử dụng.
    /// </summary>
    public virtual Voucher Voucher { get; set; } = null!;
}
