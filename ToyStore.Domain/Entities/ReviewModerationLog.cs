using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

/// <summary>
/// Thực thể lưu trữ lịch sử nhật ký kiểm duyệt (Moderation Log) của đánh giá sản phẩm hoặc hình ảnh đính kèm (do AI hoặc Nhân viên thực hiện).
/// </summary>
public partial class ReviewModerationLog
{
    /// <summary>
    /// Mã ID khóa chính của nhật ký kiểm duyệt.
    /// </summary>
    public int LogId { get; set; }

    /// <summary>
    /// Đối tượng kiểm duyệt: "Text" (nội dung bình luận) hoặc "Image" (hình ảnh đính kèm).
    /// </summary>
    public string TargetType { get; set; } = null!;

    /// <summary>
    /// Mã ID của đánh giá sản phẩm.
    /// </summary>
    public int ReviewId { get; set; }

    /// <summary>
    /// Mã ID hình ảnh được kiểm duyệt (null nếu kiểm duyệt văn bản).
    /// </summary>
    public int? ImageId { get; set; }

    /// <summary>
    /// Loại chủ thể kiểm duyệt: "AI" (Hệ thống AI Sidecar) hoặc "Staff" (Nhân viên kiểm duyệt thủ công).
    /// </summary>
    public string ModeratorType { get; set; } = null!;

    /// <summary>
    /// Mã ID tài khoản nhân viên kiểm duyệt (null nếu ModeratorType = "AI").
    /// </summary>
    public int? ModeratedBy { get; set; }

    /// <summary>
    /// Hành động kiểm duyệt ("Approved", "Rejected", "Overridden", "ManualReview").
    /// </summary>
    public string Action { get; set; } = null!;

    /// <summary>
    /// Phiên bản mô hình AI (nếu ModeratorType = "AI").
    /// </summary>
    public string? AiModelVersion { get; set; }

    /// <summary>
    /// Kết quả kiểm duyệt chi tiết ("Approved", "Rejected", "ManualReview", "Hidden").
    /// </summary>
    public string? ModerationResult { get; set; }

    /// <summary>
    /// Lý do duyệt / từ chối hoặc chi tiết nhãn vi phạm.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Thời điểm ghi nhận log kiểm duyệt (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Thực thể hình ảnh được kiểm duyệt (nếu TargetType = "Image").
    /// </summary>
    public virtual ReviewProductImage? Image { get; set; }

    /// <summary>
    /// Thực thể tài khoản nhân viên kiểm duyệt (nếu ModeratorType = "Staff").
    /// </summary>
    public virtual Account? ModeratedByNavigation { get; set; }

    /// <summary>
    /// Thực thể đánh giá sản phẩm liên quan.
    /// </summary>
    public virtual ReviewProduct Review { get; set; } = null!;
}
