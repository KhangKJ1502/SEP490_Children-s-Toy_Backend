namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Gateway giao tiếp với hệ thống kiểm duyệt tự động AI (AI Moderation Sidecar) cho Đánh giá sản phẩm.
/// </summary>
public interface IProductReviewModerationGateway
{
    /// <summary>
    /// Gửi yêu cầu kiểm duyệt nội dung văn bản và hình ảnh của một đánh giá sản phẩm qua AI Sidecar.
    /// </summary>
    /// <param name="reviewId">Mã ID của đánh giá cần kiểm duyệt.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>true nếu kiểm duyệt hoàn tất thành công, false nếu có lỗi hoặc cần đưa vào hàng đợi duyệt lại.</returns>
    Task<bool> ModerateReviewAsync(int reviewId, CancellationToken cancellationToken = default);
}
