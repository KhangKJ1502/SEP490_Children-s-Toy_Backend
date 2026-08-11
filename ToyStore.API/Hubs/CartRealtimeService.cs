using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using ToyStore.Application.DTOs.Carts;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Hubs;

/// <summary>
/// Dịch vụ xử lý gửi sự kiện giỏ hàng thời gian thực đến khách hàng qua SignalR Hub.
/// </summary>
public class CartRealtimeService : ICartRealtimeService
{
    private readonly IHubContext<CartHub> _hubContext;
    private readonly ILogger<CartRealtimeService> _logger;

    /// <summary>
    /// Khởi tạo dịch vụ CartRealtimeService với SignalR Hub Context và Logger.
    /// </summary>
    public CartRealtimeService(IHubContext<CartHub> hubContext, ILogger<CartRealtimeService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Gửi một sự kiện giỏ hàng thời gian thực đến người dùng (AccountId) cụ thể thông qua SignalR.
    /// </summary>
    public Task PublishAsync(
        int accountId,
        string eventName,
        CartDto cart,
        string message,
        object? payload = null,
        CancellationToken cancellationToken = default)
    {
        var response = new
        {
            success = true,
            message,
            data = cart,
            errors = (IReadOnlyCollection<string>?)null,
            eventName,
            payload,
            serverTime = DateTime.UtcNow
        };

        var userId = accountId.ToString();
        _logger.LogInformation("Publishing cart realtime event {EventName} to account {AccountId}.", eventName, accountId);

        return _hubContext.Clients
            .User(userId)
            .SendAsync(eventName, response, cancellationToken);
    }
}
