namespace ToyStore.Application.DTOs.Reviews;

/// <summary>
/// Data Transfer Object (DTO) hiển thị trong danh sách đánh giá công khai của một sản phẩm (dành cho Guest và Customer).
/// </summary>
public class ReviewProductListDto
{
    /// <summary>
    /// Mã ID định danh của đánh giá.
    /// </summary>
    public int ReviewId { get; set; }
    
    /// <summary>
    /// Mã ID của sản phẩm.
    /// </summary>
    public int ProductId { get; set; }
    
    /// <summary>
    /// Tên người viết đánh giá.
    /// </summary>
    public string ReviewerName { get; set; } = null!;
    
    /// <summary>
    /// URL ảnh đại diện của người viết đánh giá.
    /// </summary>
    public string? ReviewerAvatarUrl { get; set; }
    
    /// <summary>
    /// Điểm số sao (1-5 sao).
    /// </summary>
    public byte Rating { get; set; }
    
    /// <summary>
    /// Nội dung bình luận.
    /// </summary>
    public string? Comment { get; set; }
    
    /// <summary>
    /// Đánh dấu xem đánh giá đã từng được sửa hay chưa.
    /// </summary>
    public bool IsEdited { get; set; }
    
    /// <summary>
    /// Thời điểm gửi đánh giá (UTC).
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
    /// Người dùng hiện tại đã Like đánh giá này hay chưa (true nếu đã Like).
    /// </summary>
    public bool IsLiked { get; set; }
    
    /// <summary>
    /// Danh sách các hình ảnh đính kèm đã được duyệt (Approved).
    /// </summary>
    public List<ReviewImageDto> Images { get; set; } = new();
    
    /// <summary>
    /// Danh sách phản hồi của nhân viên cửa hàng.
    /// </summary>
    public List<StaffReplyDto> Replies { get; set; } = new();
}
