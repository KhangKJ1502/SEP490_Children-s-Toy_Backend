using ToyStore.Application.DTOs;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Services;

public interface IProductFollowerService
{
    /// <summary>
    /// Đăng ký theo dõi sản phẩm.
    /// </summary>
    Task<Result> FollowProductAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hủy theo dõi sản phẩm.
    /// </summary>
    Task<Result> UnfollowProductAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra xem khách hàng có đang theo dõi sản phẩm không.
    /// </summary>
    Task<Result<bool>> IsFollowingAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách ID các sản phẩm khách hàng đang theo dõi.
    /// </summary>
    Task<Result<List<int>>> GetFollowedProductIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gửi thông báo báo hàng về cho tất cả người theo dõi sản phẩm (nếu sản phẩm đủ điều kiện in-stock).
    /// </summary>
    Task NotifyFollowersIfBackInStockAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset trạng thái NotifiedAt = null khi sản phẩm hết hàng để chuẩn bị cho lần báo hàng về tiếp theo.
    /// </summary>
    Task ResetFollowerNotificationStateAsync(int productId, CancellationToken cancellationToken = default);
}
