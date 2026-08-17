namespace ToyStore.Application.DTOs.Reviews;

/// <summary>
/// Data Transfer Object (DTO) dạng rút gọn hiển thị trong danh sách phân trang quản trị kiểm duyệt đánh giá của Admin/Staff.
/// </summary>
public class AdminReviewListDto
{
    /// <summary>
    /// Mã ID định danh của đánh giá.
    /// </summary>
    public int ReviewId { get; set; }
    
    /// <summary>
    /// Mã ID tài khoản khách hàng.
    /// </summary>
    public int AccountId { get; set; }
    
    /// <summary>
    /// Email của khách hàng đã viết đánh giá.
    /// </summary>
    public string AccountEmail { get; set; } = null!;
    
    /// <summary>
    /// Mã ID sản phẩm.
    /// </summary>
    public int ProductId { get; set; }
    
    /// <summary>
    /// Tên của sản phẩm.
    /// </summary>
    public string ProductName { get; set; } = null!;
    
    /// <summary>
    /// Mã ID đơn hàng.
    /// </summary>
    public int OrderId { get; set; }
    
    /// <summary>
    /// Mã code đơn hàng.
    /// </summary>
    public string OrderCode { get; set; } = null!;
    
    /// <summary>
    /// Số sao đánh giá (1-5 sao).
    /// </summary>
    public byte Rating { get; set; }
    
    /// <summary>
    /// Nội dung bình luận.
    /// </summary>
    public string? Comment { get; set; }
    
    /// <summary>
    /// Trạng thái kiểm duyệt ("Pending", "Approved", "Rejected", "ManualReview", "Hidden").
    /// </summary>
    public string ModerationStatus { get; set; } = null!;
    
    /// <summary>
    /// Cờ đánh dấu bản ghi đã bị xóa mềm hay chưa.
    /// </summary>
    public bool IsDeleted { get; set; }
    
    /// <summary>
    /// Cờ đánh dấu khách hàng đã từng sửa đánh giá hay chưa.
    /// </summary>
    public bool IsEdited { get; set; }
    
    /// <summary>
    /// Thời điểm tạo đánh giá (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Thời điểm cập nhật gần nhất (UTC).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Số lượng hình ảnh đính kèm trong đánh giá.
    /// </summary>
    public int ImagesCount { get; set; }
    
    /// <summary>
    /// Số lượng phản hồi từ nhân viên cho đánh giá này.
    /// </summary>
    public int RepliesCount { get; set; }
}
