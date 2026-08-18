using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

/// <summary>
/// Thực thể định nghĩa danh mục các trạng thái hoàn tiền (StatusRefund) trong cơ sở dữ liệu.
/// </summary>
public partial class StatusRefund
{
    /// <summary>
    /// Khóa chính (Primary Key) của trạng thái hoàn tiền.
    /// </summary>
    public byte StatusId { get; set; }

    /// <summary>
    /// Tên trạng thái hoàn tiền (ví dụ: "Pending", "Approved", "Returning", "Received", "Refunded", "Rejected", "Cancelled",...).
    /// </summary>
    public string StatusName { get; set; } = null!;

    /// <summary>
    /// Mô tả chi tiết ý nghĩa của trạng thái hoàn tiền.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Thời điểm tạo bản ghi trạng thái (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Thời điểm cập nhật bản ghi trạng thái (UTC).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property: Danh sách các yêu cầu hoàn tiền đang ở trạng thái này.
    /// </summary>
    public virtual ICollection<OrderRefund> OrderRefunds { get; set; } = new List<OrderRefund>();

    /// <summary>
    /// Navigation property: Danh sách các bản ghi lịch sử trạng thái liên quan.
    /// </summary>
    public virtual ICollection<RefundStatusHistory> RefundStatusHistories { get; set; } = new List<RefundStatusHistory>();
}
