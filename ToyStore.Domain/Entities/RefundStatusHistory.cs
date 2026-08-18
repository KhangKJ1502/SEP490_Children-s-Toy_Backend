using System;

namespace ToyStore.Domain.Entities;

/// <summary>
/// Thực thể ghi nhận lịch sử các lần chuyển trạng thái của một yêu cầu hoàn tiền (RefundStatusHistory).
/// </summary>
public partial class RefundStatusHistory
{
    /// <summary>
    /// Khóa chính (Primary Key) của bản ghi lịch sử trạng thái.
    /// </summary>
    public int HistoryId { get; set; }

    /// <summary>
    /// Khóa ngoại tham chiếu đến yêu cầu hoàn tiền (OrderRefund).
    /// </summary>
    public int RefundId { get; set; }

    /// <summary>
    /// Khóa ngoại tham chiếu đến trạng thái hoàn tiền được chuyển đến (StatusRefund).
    /// </summary>
    public byte StatusId { get; set; }

    /// <summary>
    /// Khóa ngoại tham chiếu đến tài khoản người thực hiện thay đổi trạng thái (Account), null nếu do hệ thống.
    /// </summary>
    public int? ChangedBy { get; set; }

    /// <summary>
    /// Ghi chú kèm theo khi thực hiện thay đổi trạng thái.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Thời điểm chuyển trạng thái (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Navigation property: Thực thể yêu cầu hoàn tiền cha.
    /// </summary>
    public virtual OrderRefund Refund { get; set; } = null!;

    /// <summary>
    /// Navigation property: Thực thể trạng thái hoàn tiền tương ứng.
    /// </summary>
    public virtual StatusRefund Status { get; set; } = null!;

    /// <summary>
    /// Navigation property: Tài khoản người thực hiện thay đổi.
    /// </summary>
    public virtual Account? ChangedByNavigation { get; set; }
}
