namespace ToyStore.Application.DTOs.Reviews;

/// <summary>
/// Data Transfer Object (DTO) thể hiện thông tin đánh giá cá nhân của khách hàng đang đăng nhập,
/// bao gồm cả trạng thái kiểm duyệt hiện tại và phản hồi từ phía nhân viên.
/// </summary>
public class MyReviewDto
{
    /// <summary>
    /// Mã ID định danh của đánh giá.
    /// </summary>
    public int ReviewId { get; set; }

    /// <summary>
    /// Mã ID sản phẩm được đánh giá.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Tên sản phẩm.
    /// </summary>
    public string ProductName { get; set; } = null!;

    /// <summary>
    /// URL ảnh đại diện của sản phẩm.
    /// </summary>
    public string? ProductImage { get; set; }

    /// <summary>
    /// Mã ID đơn hàng liên quan.
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Mã code đơn hàng.
    /// </summary>
    public string OrderCode { get; set; } = null!;

    /// <summary>
    /// Số sao đã chấm (1-5 sao).
    /// </summary>
    public byte Rating { get; set; }

    /// <summary>
    /// Nội dung bình luận đã viết.
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Trạng thái kiểm duyệt ("Pending", "Approved", "Rejected", "ManualReview", "Hidden").
    /// </summary>
    public string ModerationStatus { get; set; } = null!;

    /// <summary>
    /// Cờ cho biết đánh giá này đã được sửa 1 lần hay chưa.
    /// </summary>
    public bool IsEdited { get; set; }

    /// <summary>
    /// Thời điểm gửi đánh giá (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Thời điểm có kết quả duyệt từ hệ thống/nhân viên (UTC).
    /// </summary>
    public DateTime? ModeratedAt { get; set; }

    /// <summary>
    /// Danh sách hình ảnh đính kèm của đánh giá.
    /// </summary>
    public List<ReviewImageDto> Images { get; set; } = new();

    /// <summary>
    /// Danh sách các phản hồi từ phía nhân viên cho đánh giá này.
    /// </summary>
    public List<StaffReplyDto> Replies { get; set; } = new();
}
