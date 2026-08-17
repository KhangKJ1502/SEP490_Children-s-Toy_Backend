using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

/// <summary>
/// Thực thể lưu trữ thông tin Đánh giá sản phẩm (ReviewProduct) của khách hàng.
/// </summary>
public partial class ReviewProduct
{
    /// <summary>
    /// Mã ID khóa chính của đánh giá sản phẩm.
    /// </summary>
    public int ReviewId { get; set; }

    /// <summary>
    /// Mã ID tài khoản khách hàng thực hiện đánh giá.
    /// </summary>
    public int AccountId { get; set; }

    /// <summary>
    /// Mã ID sản phẩm được đánh giá.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Mã ID đơn hàng đã mua chứa sản phẩm này.
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Số sao đánh giá chất lượng sản phẩm (từ 1 đến 5 sao).
    /// </summary>
    public byte Rating { get; set; }

    /// <summary>
    /// Nội dung bình luận / nhận xét chi tiết.
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Trạng thái kiểm duyệt của đánh giá: "Pending", "Approved", "Rejected", "ManualReview", "Hidden".
    /// </summary>
    public string ModerationStatus { get; set; } = null!;

    /// <summary>
    /// Cờ đánh dấu bản ghi đã bị xóa mềm hay chưa.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Cờ đánh dấu đánh giá này đã từng được khách hàng chỉnh sửa một lần hay chưa.
    /// </summary>
    public bool IsEdited { get; set; }

    /// <summary>
    /// Thời điểm gửi đánh giá (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Thời điểm chỉnh sửa hoặc cập nhật trạng thái gần nhất (UTC).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Thông tin tài khoản người đánh giá.
    /// </summary>
    public virtual Account Account { get; set; } = null!;

    /// <summary>
    /// Thông tin đơn hàng liên quan.
    /// </summary>
    public virtual Order Order { get; set; } = null!;

    /// <summary>
    /// Thông tin sản phẩm được đánh giá.
    /// </summary>
    public virtual Product Product { get; set; } = null!;

    /// <summary>
    /// Danh sách hình ảnh đính kèm của đánh giá.
    /// </summary>
    public virtual ICollection<ReviewProductImage> ReviewProductImages { get; set; } = new List<ReviewProductImage>();

    /// <summary>
    /// Danh sách các phản ứng (Like/Unlike) của người dùng đối với đánh giá này.
    /// </summary>
    public virtual ICollection<ReviewProductReaction> ReviewProductReactions { get; set; } = new List<ReviewProductReaction>();

    /// <summary>
    /// Danh sách các phản hồi từ phía nhân viên cửa hàng.
    /// </summary>
    public virtual ICollection<StaffReviewProductReply> StaffReviewProductReplies { get; set; } = new List<StaffReviewProductReply>();

    /// <summary>
    /// Danh sách lịch sử nhật ký kiểm duyệt (AI Sidecar và Nhân viên).
    /// </summary>
    public virtual ICollection<ReviewModerationLog> ReviewModerationLogs { get; set; } = new List<ReviewModerationLog>();
}
