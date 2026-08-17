using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

/// <summary>
/// Thực thể lưu trữ phản hồi của nhân viên / quản trị viên cửa hàng đối với một đánh giá sản phẩm.
/// </summary>
public partial class StaffReviewProductReply
{
    /// <summary>
    /// Mã ID khóa chính của bản ghi phản hồi.
    /// </summary>
    public int ReplyProductId { get; set; }

    /// <summary>
    /// Mã ID của đánh giá sản phẩm được phản hồi.
    /// </summary>
    public int ReviewProductId { get; set; }

    /// <summary>
    /// Mã ID tài khoản của nhân viên gửi phản hồi.
    /// </summary>
    public int StaffId { get; set; }

    /// <summary>
    /// Nội dung phản hồi.
    /// </summary>
    public string Content { get; set; } = null!;

    /// <summary>
    /// Cờ đánh dấu phản hồi đã bị xóa mềm hay chưa.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Thời điểm gửi phản hồi (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Thời điểm chỉnh sửa phản hồi gần nhất (UTC).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Thực thể đánh giá sản phẩm được phản hồi.
    /// </summary>
    public virtual ReviewProduct ReviewProduct { get; set; } = null!;

    /// <summary>
    /// Thực thể tài khoản nhân viên gửi phản hồi.
    /// </summary>
    public virtual Account Staff { get; set; } = null!;
}
