using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

/// <summary>
/// Thực thể lưu trữ tương tác phản ứng (Like/Unlike) của người dùng đối với một đánh giá sản phẩm.
/// </summary>
public partial class ReviewProductReaction
{
    /// <summary>
    /// Mã ID khóa chính của phản ứng đánh giá.
    /// </summary>
    public int ReactionProductId { get; set; }

    /// <summary>
    /// Mã ID đánh giá sản phẩm được tương tác.
    /// </summary>
    public int ReviewProductId { get; set; }

    /// <summary>
    /// Mã ID tài khoản người dùng tương tác.
    /// </summary>
    public int AccountId { get; set; }

    /// <summary>
    /// Mã ID loại phản ứng (ví dụ: LIKE).
    /// </summary>
    public int ReactionTypeId { get; set; }

    /// <summary>
    /// Cờ đánh dấu trạng thái tương tác (false: Đang Like, true: Đã bỏ Like).
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Thời điểm cập nhật trạng thái Like gần nhất (UTC).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Thời điểm thực hiện Like lần đầu (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Thông tin tài khoản người dùng tương tác.
    /// </summary>
    public virtual Account Account { get; set; } = null!;

    /// <summary>
    /// Thông tin đánh giá sản phẩm được tương tác.
    /// </summary>
    public virtual ReviewProduct ReviewProduct { get; set; } = null!;

    /// <summary>
    /// Thông tin loại phản ứng (ReactionType).
    /// </summary>
    public virtual ReactionType ReactionTypeNavigation { get; set; } = null!;
}
