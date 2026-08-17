namespace ToyStore.Application.DTOs.Reviews;

/// <summary>
/// Data Transfer Object (DTO) nhận dữ liệu từ nhân viên/admin để cập nhật nội dung hoặc xóa mềm phản hồi của nhân viên.
/// </summary>
public class UpdateStaffReplyDto
{
    /// <summary>
    /// Nội dung phản hồi mới (tùy chọn).
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Đánh dấu xóa mềm phản hồi (true: xóa mềm).
    /// </summary>
    public bool? IsDeleted { get; set; }
}
