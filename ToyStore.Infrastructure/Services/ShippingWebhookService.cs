using System.Text.Json;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Constants;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Constants;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Enums;

namespace ToyStore.Infrastructure.Services;

/// <summary>
/// Xu ly webhook tu shipper: cap nhat ShippingProviderTransaction,
/// ghi ShippingStatusHistory, va dong bo trang thai Order neu can.
/// </summary>
public class ShippingWebhookService : IShippingWebhookService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ILogger<ShippingWebhookService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly IOrderLifecycleService _orderLifecycle;
    private readonly IShippingStatusMapper _statusMapper;

    // Map provider status → notification event type
    private static readonly Dictionary<string, string?> WebhookEventMap = new(StringComparer.OrdinalIgnoreCase)
    {
        [ShippingStatuses.Picked]                = NotificationEventTypes.MerchPickedUp,
        [ShippingStatuses.Delivering]            = NotificationEventTypes.OrderDelivering,
        [ShippingStatuses.Delivered]             = NotificationEventTypes.OrderDelivered,
        [ShippingStatuses.DeliveryFail]          = NotificationEventTypes.OrderDeliveryFailed,
        [ShippingStatuses.Return]                = NotificationEventTypes.OrderReturning,
        [ShippingStatuses.Returned]              = NotificationEventTypes.MerchReturned,
    };

    public ShippingWebhookService(
        IUnitOfWork unitOfWork,
        IDomainEventPublisher eventPublisher,
        ILogger<ShippingWebhookService> logger,
        ITimeProvider timeProvider,
        IOrderLifecycleService orderLifecycle,
        IShippingStatusMapper statusMapper)
    {
        _unitOfWork     = unitOfWork;
        _eventPublisher = eventPublisher;
        _logger         = logger;
        _timeProvider   = timeProvider;
        _orderLifecycle = orderLifecycle;
        _statusMapper   = statusMapper;
    }

    public async Task HandleAsync(
        string provider,
        string rawPayload,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Parse payload lay ProviderOrderCode va NewStatus
            using var doc = JsonDocument.Parse(rawPayload);
            var root = doc.RootElement;

            var providerOrderCode = TryGetString(root, "OrderCode")
                ?? TryGetString(root, "order_code");

            var newStatus = TryGetString(root, "status")
                ?? TryGetString(root, "Status");

            if (string.IsNullOrWhiteSpace(providerOrderCode) || string.IsNullOrWhiteSpace(newStatus))
            {
                _logger.LogWarning(
                    "Shipping webhook from {Provider}: missing order_code or status. Payload: {Payload}",
                    provider, Truncate(rawPayload));
                return;
            }

            // Tim ShippingProviderTransaction
            var tx = await _unitOfWork.Orders.GetShippingTransactionByProviderCodeAsync(
                providerOrderCode, cancellationToken);

            if (tx is null)
            {
                _logger.LogWarning(
                    "Shipping webhook from {Provider}: ProviderOrderCode '{Code}' not found",
                    provider, providerOrderCode);
                return;
            }

            var previousStatus = tx.Status ?? string.Empty;
            var now = _timeProvider.UtcNow;

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // INSERT ShippingStatusHistory
                await _unitOfWork.Orders.AddShippingStatusHistoryAsync(new ShippingStatusHistory
                {
                    ShippingTxId   = tx.ShippingTransactionId,
                    OrderId        = tx.OrderId,
                    PreviousStatus = previousStatus,
                    NewStatus      = newStatus,
                    Source         = "webhook",
                    RawPayload     = rawPayload,
                    ProcessedAt    = now
                }, cancellationToken);

                // UPDATE ShippingProviderTransaction
                tx.Status    = newStatus;
                tx.UpdatedAt = now;

                // Map sang trang thai don hang
                var targetStatus = _statusMapper.MapToInternalStatus(newStatus);
                if (targetStatus.HasValue)
                {
                    await UpdateOrderStatusAsync(tx.Order, targetStatus.Value, now, cancellationToken);
                }

                // Cập nhật trạng thái thanh toán sang PAID nếu là trạng thái đã thu tiền hoặc đã giao
                if (tx.Order.PaymentMethod == "SHIP_COD" && tx.Order.PaymentStatus != "PAID")
                {
                    if (newStatus.Equals(ShippingStatuses.MoneyCollectDelivering, StringComparison.OrdinalIgnoreCase) ||
                        newStatus.Equals(ShippingStatuses.Delivered, StringComparison.OrdinalIgnoreCase))
                    {
                        tx.Order.PaymentStatus = "PAID";
                        tx.Order.PaidAt = now;
                        _logger.LogInformation("Order {OrderCode} payment status updated to PAID via webhook status: {Status}", tx.Order.OrderCode, newStatus);
                    }
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation(
                    "Shipping webhook processed: provider={Provider}, code={Code}, status={Status}",
                    provider, providerOrderCode, newStatus);

                // Publish notification event for webhook status (fire-and-forget)
                // Include orderCode in payload so handlers can display a human-readable order reference
                if (WebhookEventMap.TryGetValue(newStatus, out var notifEventType) && notifEventType is not null)
                {
                    _ = _eventPublisher.PublishAsync("Order", tx.OrderId.ToString(), notifEventType,
                        new
                        {
                            orderId           = tx.OrderId,
                            orderCode         = tx.Order?.OrderCode ?? "",
                            providerStatus    = newStatus,
                            providerOrderCode
                        },
                        CancellationToken.None);
                }
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex,
                "Shipping webhook from {Provider}: invalid JSON payload", provider);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Shipping webhook from {Provider}: unexpected error while processing", provider);
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task UpdateOrderStatusAsync(
        Order order,
        OrderStatus targetStatus,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // LUỒNG B: Sử dụng OrderLifecycleService cho các case đặc biệt
        if (targetStatus == OrderStatus.Delivered)
        {
            var result = await _orderLifecycle.DeliverOrderAsync(order.OrderId, cancellationToken);
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to mark order {OrderId} as delivered via lifecycle service: {Error}", order.OrderId, result.ErrorMessage);
            }
            return;
        }

        if (targetStatus == OrderStatus.Cancelled)
        {
            // NOTE: GHN "cancel" means the COURIER cancelled the shipment, NOT the customer.
            // We should NOT auto-cancel the order (which restores stock) — instead mark it as DeliveryFail
            // and let staff decide the next action (reshipping or manual cancellation).
            // Only cancel the order if the webhook explicitly maps to a final "lost" or "damage" scenario.
            // For now, log a warning and skip auto-cancellation.
            _logger.LogWarning(
                "Shipping webhook triggered Cancelled mapping for Order {OrderId} (provider status: {Status}). " +
                "Auto-cancel skipped — staff should review and take manual action.",
                order.OrderId, order.Status?.StatusName);
            return;
        }

        var targetStatusName = targetStatus.ToString();
        var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(cancellationToken);

        if (!statusMap.TryGetValue(targetStatusName, out var targetStatusId))
        {
            _logger.LogWarning("Webhook: status '{Status}' not found in database", targetStatusName);
            return;
        }

        // Khong ghi de neu don da o trang thai do hoac trang thai sau (simple progressive check)
        // Note: order.StatusId is byte, targetStatusId is also byte.
        if (order.StatusId >= targetStatusId && order.StatusId != (byte)OrderStatus.Cancelled)
            return;

        order.StatusId  = targetStatusId;
        order.UpdatedAt = now;

        await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
        {
            OrderId   = order.OrderId,
            StatusId  = targetStatusId,
            ChangedBy = null,
            Note      = $"Auto-updated from shipping webhook: {targetStatusName}",
            CreatedAt = now
        }, cancellationToken);
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop)
            && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString();
        }
        return null;
    }

    private static string Truncate(string s, int max = 300)
        => s.Length <= max ? s : s[..max];
}
