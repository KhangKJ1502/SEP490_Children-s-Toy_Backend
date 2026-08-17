namespace ToyStore.Application.DTOs.Reviews;

/// <summary>
/// Data Transfer Object (DTO) chứa các tham số lọc và phân trang cho danh sách đánh giá của tôi (My Reviews).
/// </summary>
public class MyReviewQueryDto
{
    /// <summary>
    /// Số trang cần lấy (bắt đầu từ 1, mặc định 1).
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Số lượng bản ghi mỗi trang (mặc định 10).
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Tên trường cần sắp xếp ("CreatedAt", "Rating").
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// true: sắp xếp giảm dần (mặc định true), false: sắp xếp tăng dần.
    /// </summary>
    public bool SortDesc { get; set; } = true;

    /// <summary>
    /// Lọc theo trạng thái kiểm duyệt ("Pending", "Approved", "Rejected", "ManualReview", "Hidden").
    /// </summary>
    public string? ModerationStatus { get; set; }
}
