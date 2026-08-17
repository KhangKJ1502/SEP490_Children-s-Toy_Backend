namespace ToyStore.Application.DTOs.Reviews;

/// <summary>
/// Data Transfer Object (DTO) chứa chi tiết đánh giá sản phẩm trả về cho khách hàng sau khi tạo mới hoặc chỉnh sửa.
/// </summary>
public class ReviewProductDto
{
    /// <summary>
    /// Mã ID định danh của đánh giá.
    /// </summary>
    public int ReviewId { get; set; }
    
    /// <summary>
    /// Mã ID tài khoản khách hàng viết đánh giá.
    /// </summary>
    public int AccountId { get; set; }
    
    /// <summary>
    /// Mã ID sản phẩm được đánh giá.
    /// </summary>
    public int ProductId { get; set; }
    
    /// <summary>
    /// Mã ID đơn hàng liên quan.
    /// </summary>
    public int OrderId { get; set; }
    
    /// <summary>
    /// Điểm số sao đánh giá (1-5 sao).
    /// </summary>
    public byte Rating { get; set; }
    
    /// <summary>
    /// Nội dung bình luận chi tiết.
    /// </summary>
    public string? Comment { get; set; }
    
    /// <summary>
    /// Trạng thái kiểm duyệt ("Pending", "Approved", "Rejected", "ManualReview", "Hidden").
    /// </summary>
    public string ModerationStatus { get; set; } = null!;
    
    /// <summary>
    /// Cờ cho biết đánh giá này đã từng được chỉnh sửa hay chưa.
    /// </summary>
    public bool IsEdited { get; set; }
    
    /// <summary>
    /// Thời điểm tạo đánh giá (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Thời điểm cập nhật đánh giá gần nhất (UTC).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Tổng số lượt Like của đánh giá.
    /// </summary>
    public int LikeCount { get; set; }
    
    /// <summary>
    /// Cờ cho biết người dùng hiện tại đã Like đánh giá này hay chưa.
    /// </summary>
    public bool IsLiked { get; set; }
    
    /// <summary>
    /// Danh sách các hình ảnh đính kèm trong đánh giá.
    /// </summary>
    public List<ReviewImageDto> Images { get; set; } = new();
    
    /// <summary>
    /// Danh sách các phản hồi từ phía nhân viên cho đánh giá này.
    /// </summary>
    public List<StaffReplyDto> Replies { get; set; } = new();
}
