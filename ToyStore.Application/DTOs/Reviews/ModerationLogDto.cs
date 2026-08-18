namespace ToyStore.Application.DTOs.Reviews;

/// <summary>
/// Data Transfer Object (DTO) thể hiện thông tin một dòng nhật ký kiểm duyệt đánh giá (cả từ AI và Staff).
/// </summary>
public class ModerationLogDto
{
    /// <summary>
    /// Mã ID định danh của dòng nhật ký kiểm duyệt.
    /// </summary>
    public int LogId { get; set; }
    
    /// <summary>
    /// Đối tượng được kiểm duyệt: "Text" (nội dung bình luận) hoặc "Image" (hình ảnh đính kèm).
    /// </summary>
    public string TargetType { get; set; } = null!;
    
    /// <summary>
    /// Mã ID hình ảnh được kiểm duyệt (null nếu TargetType = "Text").
    /// </summary>
    public int? ImageId { get; set; }
    
    /// <summary>
    /// Loại người/hệ thống kiểm duyệt: "AI" (Sidecar tự động) hoặc "Staff" (Nhân viên/Admin can thiệp thủ công).
    /// </summary>
    public string ModeratorType { get; set; } = null!;
    
    /// <summary>
    /// Tên nhân viên/quản trị viên đã thực hiện kiểm duyệt (null nếu ModeratorType = "AI").
    /// </summary>
    public string? ModeratedByName { get; set; }
    
    /// <summary>
    /// Hành động kiểm duyệt ("Approved", "Rejected", "Overridden", "ManualReview").
    /// </summary>
    public string Action { get; set; } = null!;
    
    /// <summary>
    /// Lý do kiểm duyệt hoặc thông tin vi phạm.
    /// </summary>
    public string? Reason { get; set; }
    
    /// <summary>
    /// Phiên bản mô hình AI sử dụng để phân loại (nếu ModeratorType = "AI").
    /// </summary>
    public string? AiModelVersion { get; set; }
    
    /// <summary>
    /// Thời điểm ghi nhận nhật ký (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
