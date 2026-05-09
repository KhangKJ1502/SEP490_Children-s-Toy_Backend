using System.Text.Json;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;

namespace ToyStore.Application.Features.Notifications.Handlers;

/// <summary>
/// Notifies customers when a product on their wishlist is on promotion (price drop).
/// Triggered by FlashSaleActivationJob or PromotionService when a promotion goes active.
/// </summary>
public class WishlistPriceDropHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.WishlistPriceDrop;

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;
    private readonly IUserPreferenceChecker _prefChecker;

    public WishlistPriceDropHandler(
        IUnitOfWork unitOfWork,
        INotificationDispatcher dispatcher,
        IUserPreferenceChecker prefChecker)
    {
        _unitOfWork  = unitOfWork;
        _dispatcher  = dispatcher;
        _prefChecker = prefChecker;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;

        var productId   = root.GetProperty("productId").GetInt32();
        var productName = root.TryGetProperty("productName", out var pn) ? pn.GetString() : "Product";
        var salePrice   = root.TryGetProperty("salePrice", out var sp) ? sp.GetDecimal() : 0;

        // Get all accounts that wishlisted this product
        var wishlists = await _unitOfWork.Wishlists.GetByProductIdAsync(productId, ct);

        foreach (var wishlist in wishlists)
        {
            var accountId = wishlist.AccountId;

            if (!await _prefChecker.CanSendAsync(accountId, PreferenceKeys.Promotions, ct))
                continue;

            await _dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = accountId,
                RecipientType      = RecipientTypes.Customer,
                NotificationType   = NotificationTypes.Promotion,
                Title              = "Price drop on your wishlist!",
                Message            = $"\"{productName}\" is now on sale at {salePrice:N0}₫. Grab it before it's gone!",
                SendBell           = true,
                SendEmail          = false,
                ActionTarget       = $"/products/{productId}",
                IdempotencyKey     = $"{EventType}:{ev.AggregateId}:{accountId}",
                Payload            = new Dictionary<string, object>
                {
                    ["productId"]   = productId,
                    ["salePrice"]   = salePrice,
                },
            }, ct);
        }
    }
}
