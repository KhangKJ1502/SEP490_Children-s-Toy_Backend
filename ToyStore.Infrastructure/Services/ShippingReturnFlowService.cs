using Microsoft.Extensions.Logging;
using ToyStore.Application.Constants;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Constants;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Enums;

namespace ToyStore.Infrastructure.Services;

public class ShippingReturnFlowService : IShippingReturnFlowService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRefundService _refundService;
    private readonly ILogger<ShippingReturnFlowService> _logger;

    private static readonly HashSet<string> PrepaidMethods =
        new(StringComparer.OrdinalIgnoreCase) { "SE_PAY", "WALLET", "BANK_TRANSFER" };

    public ShippingReturnFlowService(
        IUnitOfWork unitOfWork,
        IRefundService refundService,
        ILogger<ShippingReturnFlowService> logger)
    {
        _unitOfWork = unitOfWork;
        _refundService = refundService;
        _logger = logger;
    }

    public async Task<ShippingReturnFlowResult> ProcessActionAsync(
        ShippingWebhookAction action,
        Order order,
        ShippingProviderTransaction tx,
        string ghnStatus,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        if (await IsTerminalOrderAsync(order, cancellationToken))
        {
            _logger.LogInformation(
                "Order {OrderId} is terminal (Cancelled/Refunded); skipping return-flow action {Action}",
                order.OrderId, action);
            return new ShippingReturnFlowResult();
        }

        return action switch
        {
            ShippingWebhookAction.HandleDeliveryFail =>
                await HandleDeliveryFailAsync(order, tx, ghnStatus, now, cancellationToken),
            ShippingWebhookAction.SetReturning =>
                await HandleSetReturningAsync(order, ghnStatus, now, cancellationToken),
            ShippingWebhookAction.HandleReturnStarted =>
                await HandleReturnStartedAsync(order, ghnStatus, now, cancellationToken),
            ShippingWebhookAction.KeepReturning =>
                await HandleKeepReturningAsync(order, ghnStatus, now, cancellationToken),
            ShippingWebhookAction.HandleReturnCompleted =>
                await HandleReturnCompletedAsync(order, OrderCancelReasons.DeliveryFailedGhn, now, cancellationToken),
            ShippingWebhookAction.HandleReturnFail =>
                await HandleReturnFailAsync(order, tx, ghnStatus, now, cancellationToken),
            ShippingWebhookAction.HandleDamageLost =>
                await HandleDamageLostAsync(order, ghnStatus, now, cancellationToken),
            ShippingWebhookAction.HandleGhnCancel =>
                await HandleGhnCancelAsync(order, now, cancellationToken),
            ShippingWebhookAction.HandleException =>
                await HandleExceptionAsync(order, ghnStatus, now, cancellationToken),
            _ => new ShippingReturnFlowResult()
        };
    }

    private async Task<ShippingReturnFlowResult> HandleDeliveryFailAsync(
        Order order, ShippingProviderTransaction tx, string ghnStatus, DateTime now, CancellationToken ct)
    {
        var notifications = new List<PendingShippingNotification>();
        var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(ct);
        var deliveringId = statusMap.GetValueOrDefault(OrderStatuses.Delivering, (byte)OrderStatus.Delivering);

        if (string.IsNullOrEmpty(order.CancelReason) || order.CancelReason == OrderCancelReasons.DeliveryFailedGhn)
        {
            order.CancelReason = OrderCancelReasons.DeliveryFailedGhn;
        }
        order.UpdatedAt = now;

        var detailedReason = string.IsNullOrEmpty(order.CancelReason) || order.CancelReason == OrderCancelReasons.DeliveryFailedGhn
            ? "No detailed reason from shipping provider"
            : order.CancelReason;

        tx.LastErrorMessage = $"Delivery failed: {detailedReason} (Code: {order.LastGHNFailCode ?? "N/A"})";

        var attempt = await _unitOfWork.Orders.CountShippingStatusHistoryAsync(
            tx.ShippingTransactionId, ShippingStatuses.DeliveryFail, ct);

        await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
        {
            OrderId = order.OrderId,
            StatusId = deliveringId,
            ChangedBy = null,
            Note = $"GHN delivery_fail attempt {attempt + 1}.\n Reason: {detailedReason}",
            CreatedAt = now
        }, ct);

        notifications.Add(BuildOrderNotification(
            NotificationEventTypes.OrderDeliveryFailed, order, ghnStatus, tx.ProviderOrderCode));

        return new ShippingReturnFlowResult { Notifications = notifications };
    }

    private async Task<ShippingReturnFlowResult> HandleReturnStartedAsync(
        Order order, string ghnStatus, DateTime now, CancellationToken ct)
    {
        if (string.Equals(order.PaymentMethod, "SHIP_COD", StringComparison.OrdinalIgnoreCase))
        {
            await ApplyCodCancelForDeliveryFailAsync(order, OrderCancelReasons.DeliveryFailedGhn, now, ct);

            return new ShippingReturnFlowResult
            {
                Notifications =
                [
                    new PendingShippingNotification(
                        NotificationEventTypes.OrderCancelledDeliveryFail,
                        new { orderId = order.OrderId, orderCode = order.OrderCode })
                ],
                ReleaseShiftCapacity = true
            };
        }

        return await HandleSetReturningAsync(order, ghnStatus, now, ct);
    }

    private async Task<ShippingReturnFlowResult> HandleSetReturningAsync(
        Order order, string ghnStatus, DateTime now, CancellationToken ct)
    {
        var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(ct);
        var returningId = ResolveStatusId(statusMap, OrderStatuses.Returning, OrderStatus.Returning);

        if (order.StatusId == returningId)
            return new ShippingReturnFlowResult();

        order.StatusId = returningId;
        order.UpdatedAt = now;

        await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
        {
            OrderId = order.OrderId,
            StatusId = returningId,
            ChangedBy = null,
            Note = $"GHN {ghnStatus}: return to warehouse started",
            CreatedAt = now
        }, ct);

        return new ShippingReturnFlowResult
        {
            Notifications =
            [
                BuildOrderNotification(NotificationEventTypes.OrderReturning, order, ghnStatus, null)
            ]
        };
    }

    private async Task<ShippingReturnFlowResult> HandleKeepReturningAsync(
        Order order, string ghnStatus, DateTime now, CancellationToken ct)
    {
        var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(ct);
        var returningId = ResolveStatusId(statusMap, OrderStatuses.Returning, OrderStatus.Returning);

        if (order.StatusId != returningId)
        {
            order.StatusId = returningId;
            order.UpdatedAt = now;
            await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                StatusId = returningId,
                ChangedBy = null,
                Note = $"GHN {ghnStatus}",
                CreatedAt = now
            }, ct);
        }
        else
        {
            await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                StatusId = order.StatusId,
                ChangedBy = null,
                Note = $"GHN {ghnStatus}",
                CreatedAt = now
            }, ct);
        }

        return new ShippingReturnFlowResult();
    }

    private async Task<ShippingReturnFlowResult> HandleReturnCompletedAsync(
        Order order, string cancelReason, DateTime now, CancellationToken ct)
    {
        var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(ct);
        var returnCompletedId = ResolveStatusId(statusMap, OrderStatuses.ReturnCompleted, OrderStatus.ReturnCompleted);
        var cancelledId = ResolveStatusId(statusMap, OrderStatuses.Cancelled, OrderStatus.Cancelled);

        if (order.StatusId == returnCompletedId || order.StatusId == cancelledId)
        {
            return new ShippingReturnFlowResult();
        }

        if (string.Equals(order.PaymentMethod, "SHIP_COD", StringComparison.OrdinalIgnoreCase))
        {
            return await ApplyReturnPaymentBranchAsync(order, cancelReason, now, ct);
        }

        order.StatusId = cancelledId;
        order.CancelReason = cancelReason;
        order.CancelledAt = now;
        order.UpdatedAt = now;

        await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
        {
            OrderId = order.OrderId,
            StatusId = cancelledId,
            ChangedBy = null,
            Note = "GHN returned: goods received at warehouse. Order cancelled, refund pending.",
            CreatedAt = now
        }, ct);

        return await ApplyReturnPaymentBranchAsync(order, cancelReason, now, ct);
    }

    private async Task<ShippingReturnFlowResult> HandleReturnFailAsync(
        Order order, ShippingProviderTransaction tx, string ghnStatus, DateTime now, CancellationToken ct)
    {
        tx.LastErrorMessage = "GHN return_fail";
        var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(ct);
        var returningId = statusMap.GetValueOrDefault(OrderStatuses.Returning, order.StatusId);

        await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
        {
            OrderId = order.OrderId,
            StatusId = returningId,
            ChangedBy = null,
            Note = "GHN return_fail: requires manual admin action",
            CreatedAt = now
        }, ct);

        return new ShippingReturnFlowResult
        {
            Notifications =
            [
                new PendingShippingNotification(
                    NotificationEventTypes.OrderReturnFail,
                    new
                    {
                        orderId = order.OrderId,
                        orderCode = order.OrderCode,
                        providerOrderCode = tx.ProviderOrderCode ?? "",
                        providerStatus = ghnStatus
                    })
            ]
        };
    }

    private async Task<ShippingReturnFlowResult> HandleDamageLostAsync(
        Order order, string ghnStatus, DateTime now, CancellationToken ct)
    {
        var cancelReason = ghnStatus.Equals(ShippingStatuses.Lost, StringComparison.OrdinalIgnoreCase)
            ? OrderCancelReasons.LostInTransit
            : OrderCancelReasons.DamagedInTransit;

        return await ApplyDirectCancelReturnAsync(order, cancelReason, ghnStatus, now, ct);
    }

    private async Task<ShippingReturnFlowResult> HandleGhnCancelAsync(
        Order order, DateTime now, CancellationToken ct)
    {
        return await ApplyDirectCancelReturnAsync(
            order, OrderCancelReasons.GhnCancelled, ShippingStatuses.Cancel, now, ct);
    }

    private async Task<ShippingReturnFlowResult> ApplyDirectCancelReturnAsync(
        Order order, string cancelReason, string ghnStatus, DateTime now, CancellationToken ct)
    {
        if (string.Equals(order.PaymentMethod, "SHIP_COD", StringComparison.OrdinalIgnoreCase))
        {
            await ApplyCodCancelForDeliveryFailAsync(order, cancelReason, now, ct);

            return new ShippingReturnFlowResult
            {
                Notifications =
                [
                    new PendingShippingNotification(
                        NotificationEventTypes.OrderCancelledDeliveryFail,
                        new { orderId = order.OrderId, orderCode = order.OrderCode })
                ],
                ReleaseShiftCapacity = true
            };
        }

        var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(ct);
        var cancelledId = ResolveStatusId(statusMap, OrderStatuses.Cancelled, OrderStatus.Cancelled);

        order.StatusId = cancelledId;
        order.CancelReason = cancelReason;
        order.CancelledAt = now;
        order.UpdatedAt = now;

        await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
        {
            OrderId = order.OrderId,
            StatusId = cancelledId,
            ChangedBy = null,
            Note = $"GHN {ghnStatus}: {cancelReason}",
            CreatedAt = now
        }, ct);

        return await ApplyReturnPaymentBranchAsync(order, cancelReason, now, ct, skipReturnCompletedStep: true);
    }

    private async Task<ShippingReturnFlowResult> HandleExceptionAsync(
        Order order, string ghnStatus, DateTime now, CancellationToken ct)
    {
        await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
        {
            OrderId = order.OrderId,
            StatusId = order.StatusId,
            ChangedBy = null,
            Note = $"GHN exception: {ghnStatus}: Requires admin review",
            CreatedAt = now
        }, ct);

        return new ShippingReturnFlowResult
        {
            Notifications =
            [
                new PendingShippingNotification(
                    NotificationEventTypes.SystemShippingWebhookError,
                    new
                    {
                        orderId = order.OrderId,
                        orderCode = order.OrderCode,
                        providerStatus = ghnStatus,
                        message = $"GHN exception on order {order.OrderCode}"
                    })
            ]
        };
    }

    private async Task<ShippingReturnFlowResult> ApplyReturnPaymentBranchAsync(
        Order order,
        string cancelReason,
        DateTime now,
        CancellationToken ct,
        bool skipReturnCompletedStep = false)
    {
        var notifications = new List<PendingShippingNotification>();

        if (string.Equals(order.PaymentMethod, "SHIP_COD", StringComparison.OrdinalIgnoreCase))
        {
            await ApplyCodCancelForDeliveryFailAsync(order, cancelReason, now, ct);

            notifications.Add(new PendingShippingNotification(
                NotificationEventTypes.OrderCancelledDeliveryFail,
                new { orderId = order.OrderId, orderCode = order.OrderCode }));

            return new ShippingReturnFlowResult
            {
                Notifications = notifications,
                ReleaseShiftCapacity = true
            };
        }

        if (PrepaidMethods.Contains(order.PaymentMethod))
        {
            var reason = await _unitOfWork.Refunds.GetReasonByContentAsync(RefundReasons.DeliveryFailedGhn, ct);
            if (reason is null)
            {
                _logger.LogError("Refund reason '{Reason}' not found cannot create system refund for order {OrderId}",
                    RefundReasons.DeliveryFailedGhn, order.OrderId);
                return new ShippingReturnFlowResult { Notifications = notifications };
            }

            var refund = await _refundService.CreateSystemRefundForDeliveryFailAsync(order, reason.RefundReasonId, ct);
            if (refund is not null)
            {
                notifications.Add(new PendingShippingNotification(
                    NotificationEventTypes.OrderReturnRefundPending,
                    new { orderId = order.OrderId, orderCode = order.OrderCode }));

                notifications.Add(new PendingShippingNotification(
                    NotificationEventTypes.RefundNewRequest,
                    new
                    {
                        refundId = refund.RefundId,
                        orderId = order.OrderId,
                        orderCode = order.OrderCode,
                        customerId = order.AccountId
                    }));
            }

            if (!skipReturnCompletedStep)
            {
                notifications.Add(new PendingShippingNotification(
                    NotificationEventTypes.MerchReturned,
                    new
                    {
                        orderId = order.OrderId,
                        orderCode = order.OrderCode,
                        providerStatus = ShippingStatuses.Returned
                    }));
            }
        }

        return new ShippingReturnFlowResult { Notifications = notifications };
    }

    private async Task ApplyCodCancelForDeliveryFailAsync(
        Order order, string cancelReason, DateTime now, CancellationToken ct)
    {
        var fullOrder = await _unitOfWork.Orders.GetByIdForUpdateAsync(order.OrderId, ct)
            ?? order;

        var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(ct);
        var cancelledId = ResolveStatusId(statusMap, OrderStatuses.Cancelled, OrderStatus.Cancelled);

        bool productsSubtracted = fullOrder.PaymentStatus != "PAID";

        foreach (var detail in fullOrder.OrderDetails)
        {
            if (productsSubtracted)
                await _unitOfWork.Products.AdjustStockAsync(detail.ProductId, detail.Quantity, ct);

            if (detail.SlotProductId.HasValue)
            {
                if (fullOrder.PaymentStatus == "PAID")
                    await _unitOfWork.Orders.AdjustFlashSaleStockAsync(
                        detail.SlotProductId.Value, -detail.Quantity, 0, ct);
                else
                    await _unitOfWork.Orders.AdjustFlashSaleStockAsync(
                        detail.SlotProductId.Value, -detail.Quantity, 0, ct);
            }
        }

        await _unitOfWork.Orders.RestoreVoucherAsync(fullOrder.OrderId, ct);

        fullOrder.StatusId = cancelledId;
        fullOrder.CancelledAt = now;
        fullOrder.CancelReason = cancelReason;
        fullOrder.PaymentStatus = "CANCELLED";
        fullOrder.UpdatedAt = now;

        await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
        {
            OrderId = fullOrder.OrderId,
            StatusId = cancelledId,
            ChangedBy = null,
            Note = $"Order cancelled: {cancelReason}",
            CreatedAt = now
        }, ct);

        await _unitOfWork.Orders.AddPaymentHistoryAsync(new PaymentHistory
        {
            AccountId = fullOrder.AccountId,
            OrderId = fullOrder.OrderId,
            PaymentStatus = "CANCELLED",
            PaymentMethod = fullOrder.PaymentMethod,
            Amount = fullOrder.TotalAmount,
            CreatedAt = now
        }, ct);

        // Sync tracked order reference from webhook
        order.StatusId = fullOrder.StatusId;
        order.CancelledAt = fullOrder.CancelledAt;
        order.CancelReason = fullOrder.CancelReason;
        order.PaymentStatus = fullOrder.PaymentStatus;
        order.UpdatedAt = fullOrder.UpdatedAt;
    }

    private static PendingShippingNotification BuildOrderNotification(
        string eventType, Order order, string providerStatus, string? providerOrderCode)
        => new(eventType, new
        {
            orderId = order.OrderId,
            orderCode = order.OrderCode,
            providerStatus,
            providerOrderCode = providerOrderCode ?? ""
        });

    private async Task<bool> IsTerminalOrderAsync(Order order, CancellationToken ct)
    {
        var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(ct);
        var cancelled = ResolveStatusId(statusMap, OrderStatuses.Cancelled, OrderStatus.Cancelled);
        var refunded = ResolveStatusId(statusMap, OrderStatuses.Refunded, OrderStatus.Refunded);
        return order.StatusId == cancelled || order.StatusId == refunded;
    }

    private byte ResolveStatusId(
        Dictionary<string, byte> statusMap,
        string statusName,
        OrderStatus enumFallback)
    {
        if (statusMap.TryGetValue(statusName, out var id))
            return id;

        _logger.LogError(
            "Status '{Status}' missing from StatusOrders run 20260526_ReturnFlow_StatusAndRefundReason.sql. Using enum fallback {FallbackId}.",
            statusName, (byte)enumFallback);
        return (byte)enumFallback;
    }
}
