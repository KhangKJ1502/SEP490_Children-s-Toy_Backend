namespace ToyStore.Application.DTOs.Reviews;

/// <summary>
/// Data Transfer Object (DTO) nhận dữ liệu từ nhân viên/admin để tạo phản hồi cho đánh giá sản phẩm.
/// </summary>
public class CreateStaffReplyDto
{
    /// <summary>
    /// Nội dung phản hồi từ nhân viên / cửa hàng.
    /// </summary>
    public string Content { get; set; } = null!;
}
