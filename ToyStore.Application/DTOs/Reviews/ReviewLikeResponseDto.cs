namespace ToyStore.Application.DTOs.Reviews;

/// <summary>
/// Data Transfer Object (DTO) trả về sau khi khách hàng thực hiện Like hoặc Bỏ Like (Toggle Like) cho một đánh giá sản phẩm.
/// </summary>
public class ReviewLikeResponseDto
{
    /// <summary>
    /// Mã ID của đánh giá sản phẩm.
    /// </summary>
    public int ReviewId { get; set; }

    /// <summary>
    /// Tổng số lượt Like mới nhất của đánh giá này.
    /// </summary>
    public int LikeCount { get; set; }

    /// <summary>
    /// Trạng thái Like hiện tại của người dùng đối với đánh giá này (true: đang Like, false: đã bỏ Like).
    /// </summary>
    public bool IsLiked { get; set; }
}
