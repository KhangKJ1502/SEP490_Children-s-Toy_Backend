namespace ToyStore.Application.DTOs.Reviews;

/// <summary>
/// Data Transfer Object (DTO) chứa thông tin phản hồi của nhân viên cửa hàng cho đánh giá sản phẩm.
/// </summary>
public class StaffReplyDto
{
    /// <summary>
    /// Mã ID định danh của bản ghi phản hồi.
    /// </summary>
    public int ReplyProductId { get; set; }
    
    /// <summary>
    /// Mã ID tài khoản nhân viên đã phản hồi.
    /// </summary>
    public int StaffId { get; set; }
    
    /// <summary>
    /// Tên của nhân viên đã phản hồi.
    /// </summary>
    public string StaffName { get; set; } = null!;
    
    /// <summary>
    /// Nội dung phản hồi.
    /// </summary>
    public string Content { get; set; } = null!;
    
    /// <summary>
    /// Thời điểm gửi phản hồi (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Thời điểm chỉnh sửa phản hồi gần nhất (UTC).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
