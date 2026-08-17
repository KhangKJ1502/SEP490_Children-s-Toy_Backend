using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

/// <summary>
/// Thực thể lưu trữ hình ảnh đính kèm của một đánh giá sản phẩm.
/// </summary>
public partial class ReviewProductImage
{
    /// <summary>
    /// Mã ID khóa chính của ảnh đính kèm.
    /// </summary>
    public int ReviewProductImageId { get; set; }

    /// <summary>
    /// Mã ID của đánh giá sản phẩm cha.
    /// </summary>
    public int ReviewProductId { get; set; }

    /// <summary>
    /// Đường dẫn URL hình ảnh trên Cloudinary.
    /// </summary>
    public string ImageUrl { get; set; } = null!;

    /// <summary>
    /// Trạng thái kiểm duyệt ảnh: "Pending", "Approved", "Rejected", "ManualReview", "Hidden".
    /// </summary>
    public string ModerationStatus { get; set; } = null!;

    /// <summary>
    /// Cờ đánh dấu ảnh đã bị xóa mềm hay chưa.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Thời điểm tải ảnh lên (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Thời điểm cập nhật trạng thái kiểm duyệt gần nhất (UTC).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Thực thể đánh giá sản phẩm cha.
    /// </summary>
    public virtual ReviewProduct ReviewProduct { get; set; } = null!;

    /// <summary>
    /// Lịch sử các lần kiểm duyệt ảnh (AI/Staff).
    /// </summary>
    public virtual ICollection<ReviewModerationLog> ReviewModerationLogs { get; set; } = new List<ReviewModerationLog>();
}
