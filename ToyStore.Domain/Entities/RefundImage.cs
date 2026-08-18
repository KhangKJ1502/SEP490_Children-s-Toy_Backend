using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

/// <summary>
/// Thực thể đại diện cho hình ảnh bằng chứng (sản phẩm lỗi, sai hàng, hư hỏng, phiếu giao nhận) đính kèm trong yêu cầu hoàn tiền (RefundImage).
/// </summary>
public partial class RefundImage
{
    /// <summary>
    /// Khóa chính (Primary Key) của hình ảnh bằng chứng hoàn tiền.
    /// </summary>
    public int RefundImageId { get; set; }

    /// <summary>
    /// Khóa ngoại tham chiếu đến yêu cầu hoàn tiền cha (OrderRefund).
    /// </summary>
    public int RefundId { get; set; }

    /// <summary>
    /// Đường dẫn URL của hình ảnh lưu trữ trên Cloudinary.
    /// </summary>
    public string ImageUrl { get; set; } = null!;

    /// <summary>
    /// Cờ đánh dấu xóa mềm hình ảnh.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Thời điểm tải lên hình ảnh (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Navigation property: Thực thể yêu cầu hoàn tiền cha.
    /// </summary>
    public virtual OrderRefund Refund { get; set; } = null!;
}
