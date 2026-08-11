using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace ToyStore.API.Hubs;

/// <summary>
/// Hub SignalR quản lý kết nối thời gian thực cho tính năng giỏ hàng (Cart) của khách hàng.
/// </summary>
[Authorize]
public class CartHub : Hub
{
    private readonly ILogger<CartHub> _logger;

    /// <summary>
    /// Khởi tạo CartHub với dịch vụ ghi log.
    /// </summary>
    public CartHub(ILogger<CartHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Tạo tên nhóm (group name) định danh cho giỏ hàng của từng tài khoản.
    /// </summary>
    public static string BuildUserGroup(int accountId) => $"cart-user-{accountId}";

    /// <summary>
    /// Kích hoạt khi khách hàng kết nối thành công đến Hub. Hệ thống tự động thêm kết nối của khách hàng vào nhóm giỏ hàng tương ứng.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var accountIdClaim = Context.UserIdentifier;

        if (int.TryParse(accountIdClaim, out var accountId) && accountId > 0)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, BuildUserGroup(accountId));
            _logger.LogInformation("CartHub connected: ConnectionId={ConnectionId}, AccountId={AccountId}", Context.ConnectionId, accountId);
        }
        else
        {
            _logger.LogWarning("CartHub connected without valid AccountID. ConnectionId={ConnectionId}", Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }
}
