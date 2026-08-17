namespace ToyStore.Application.DTOs.Reviews;

/// <summary>
/// Data Transfer Object (DTO) nhận dữ liệu từ Admin/Staff để cập nhật thủ công trạng thái kiểm duyệt hoặc xóa mềm đánh giá.
/// </summary>
public class UpdateModerationStatusDto
{
    /// <summary>
    /// Trạng thái kiểm duyệt mới ("Approved", "Rejected", "Hidden", "ManualReview").
    /// </summary>
    public string? ModerationStatus { get; set; }
    
    /// <summary>
    /// Lý do thay đổi trạng thái hoặc lý do từ chối đánh giá.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Cờ đánh dấu xóa mềm đánh giá (true: xóa mềm).
    /// </summary>
    public bool? IsDeleted { get; set; }
}
