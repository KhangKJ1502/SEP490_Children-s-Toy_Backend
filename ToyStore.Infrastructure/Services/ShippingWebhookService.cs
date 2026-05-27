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
    private const string WebhookSource = "GHN_WEBHOOK";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ILogger<ShippingWebhookService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly IOrderLifecycleService _orderLifecycle;
    private readonly IShippingReturnFlowService _returnFlow;
    private readonly IShippingStatusMapper _statusMapper;
    private readonly IShiftAssignmentService _shiftAssignmentService;

    public ShippingWebhookService(
        IUnitOfWork unitOfWork,
        IDomainEventPublisher eventPublisher,
        ILogger<ShippingWebhookService> logger,
        ITimeProvider timeProvider,
        IOrderLifecycleService orderLifecycle,
        IShippingReturnFlowService returnFlow,
        IShippingStatusMapper statusMapper,
        IShiftAssignmentService shiftAssignmentService)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _logger = logger;
        _timeProvider = timeProvider;
        _orderLifecycle = orderLifecycle;
        _returnFlow = returnFlow;
        _statusMapper = statusMapper;
        _shiftAssignmentService = shiftAssignmentService;
    }

    public async Task HandleAsync(
        string provider,
        string rawPayload,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawPayload);
            var root = doc.RootElement;

            var providerOrderCode = TryGetString(root, "OrderCode")
                ?? TryGetString(root, "order_code");

            var newStatus = TryGetString(root, "Status")
                ?? TryGetString(root, "status");

            if (string.IsNullOrWhiteSpace(providerOrderCode) || string.IsNullOrWhiteSpace(newStatus))
            {
                _logger.LogWarning(
                    "Shipping webhook from {Provider}: missing order_code or status. Payload: {Payload}",
                    provider, Truncate(rawPayload));
                return;
            }

            var tx = await _unitOfWork.Orders.GetShippingTransactionByProviderCodeAsync(
                providerOrderCode, cancellationToken);

            if (tx is null)
            {
                var refund = await _unitOfWork.Refunds.GetByShippingOrderCodeAsync(providerOrderCode, cancellationToken);
                if (refund is not null)
                {
                    await HandleRefundWebhookAsync(refund, newStatus, rawPayload, cancellationToken);
                    return;
                }

                _logger.LogWarning(
                    "Shipping webhook from {Provider}: ProviderOrderCode '{Code}' not found in shipping transactions or refund requests",
                    provider, providerOrderCode);
                return;
            }

            if (await _unitOfWork.Orders.ExistsShippingStatusHistoryAsync(
                    tx.ShippingTransactionId, newStatus, rawPayload, cancellationToken))
            {
                _logger.LogInformation(
                    "Shipping webhook duplicate skipped: code={Code}, status={Status}",
                    providerOrderCode, newStatus);
                return;
            }

            var previousStatus = tx.Status ?? string.Empty;
            var now = _timeProvider.UtcNow;
            var pendingNotifications = new List<PendingShippingNotification>();
            var releaseCapacity = false;
            var orderIdForCapacity = tx.OrderId;

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                await _unitOfWork.Orders.AddShippingStatusHistoryAsync(new ShippingStatusHistory
                {
                    ShippingTxId = tx.ShippingTransactionId,
                    OrderId = tx.OrderId,
                    PreviousStatus = previousStatus,
                    NewStatus = newStatus,
                    Source = WebhookSource,
                    RawPayload = rawPayload,
                    ProcessedAt = now
                }, cancellationToken);

                tx.Status = newStatus;
                tx.UpdatedAt = now;

                var action = _statusMapper.ResolveWebhookAction(newStatus);

                if (action == ShippingWebhookAction.UpdateOrderStatus)
                {
                    var targetStatus = _statusMapper.MapToInternalStatus(newStatus);
                    if (targetStatus.HasValue)
                        await UpdateOrderStatusAsync(tx.Order, targetStatus.Value, now, cancellationToken);
                }
                else if (action != ShippingWebhookAction.Unknown)
                {
                    var flowResult = await _returnFlow.ProcessActionAsync(
                        action, tx.Order, tx, newStatus, now, cancellationToken);
                    pendingNotifications.AddRange(flowResult.Notifications);
                    releaseCapacity = flowResult.ReleaseShiftCapacity;
                }
                else
                {
                    _logger.LogWarning(
                        "Shipping webhook unknown status '{Status}' for order {OrderId}",
                        newStatus, tx.OrderId);
                }

                if (tx.Order.PaymentMethod == "SHIP_COD" && tx.Order.PaymentStatus != "PAID")
                {
                    if (newStatus.Equals(ShippingStatuses.MoneyCollectDelivering, StringComparison.OrdinalIgnoreCase) ||
                        newStatus.Equals(ShippingStatuses.Delivered, StringComparison.OrdinalIgnoreCase))
                    {
                        tx.Order.PaymentStatus = "PAID";
                        tx.Order.PaidAt = now;
                        _logger.LogInformation(
                            "Order {OrderCode} payment status updated to PAID via webhook status: {Status}",
                            tx.Order.OrderCode, newStatus);
                    }
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation(
                    "Shipping webhook processed: provider={Provider}, code={Code}, status={Status}, action={Action}",
                    provider, providerOrderCode, newStatus, action);

                var notifEvent = _statusMapper.ResolveNotificationEventType(newStatus);
                if (notifEvent is not null
                    && pendingNotifications.All(n => n.EventType != notifEvent))
                {
                    pendingNotifications.Add(new PendingShippingNotification(notifEvent, new
                    {
                        orderId = tx.OrderId,
                        orderCode = tx.Order?.OrderCode ?? "",
                        providerStatus = newStatus,
                        providerOrderCode
                    }));
                }

                foreach (var pending in pendingNotifications)
                {
                    await _eventPublisher.PublishAsync(
                        "Order", tx.OrderId.ToString(), pending.EventType, pending.Payload,
                        cancellationToken);
                }

                if (releaseCapacity)
                {
                    try
                    {
                        await _shiftAssignmentService.ReleaseCapacityAsync(orderIdForCapacity, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to release capacity for order {OrderId}", orderIdForCapacity);
                    }
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

    private async Task UpdateOrderStatusAsync(
        Order order,
        OrderStatus targetStatus,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (targetStatus == OrderStatus.Delivered)
        {
            var result = await _orderLifecycle.DeliverOrderAsync(order.OrderId, cancellationToken);
            if (!result.IsSuccess)
            {
                _logger.LogWarning(
                    "Failed to mark order {OrderId} as delivered via lifecycle service: {Error}",
                    order.OrderId, result.ErrorMessage);
            }
            return;
        }

        var targetStatusName = targetStatus.ToString();
        var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(cancellationToken);

        if (!statusMap.TryGetValue(targetStatusName, out var targetStatusId))
        {
            _logger.LogWarning("Webhook: status '{Status}' not found in database", targetStatusName);
            return;
        }

        if (order.StatusId >= targetStatusId && order.StatusId != (byte)OrderStatus.Cancelled)
            return;

        order.StatusId = targetStatusId;
        order.UpdatedAt = now;

        await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
        {
            OrderId = order.OrderId,
            StatusId = targetStatusId,
            ChangedBy = null,
            Note = $"Auto-updated from shipping webhook: {targetStatusName}",
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

    private async Task HandleRefundWebhookAsync(
        OrderRefund refund,
        string newStatus,
        string rawPayload,
        CancellationToken cancellationToken)
    {
        try
        {
            byte? targetRefundStatusId = newStatus switch
            {
                ShippingStatuses.ReadyToPick or ShippingStatuses.Storing => (byte)RefundStatusEnum.RefundPickupCreated,
                
                ShippingStatuses.Picking or ShippingStatuses.Picked or 
                ShippingStatuses.Transporting or ShippingStatuses.Sorting or 
                ShippingStatuses.Delivering or ShippingStatuses.MoneyCollectDelivering or
                ShippingStatuses.ReturnTransporting or ShippingStatuses.ReturnSorting or 
                ShippingStatuses.Returning => (byte)RefundStatusEnum.RefundShipping,
                
                ShippingStatuses.Delivered or ShippingStatuses.Returned => (byte)RefundStatusEnum.RefundReceived,
                
                ShippingStatuses.Cancel or ShippingStatuses.DeliveryFail or 
                ShippingStatuses.ReturnFail or ShippingStatuses.Lost or 
                ShippingStatuses.Damage or ShippingStatuses.Exception => (byte)RefundStatusEnum.RefundCancelled,
                
                _ => null
            };

            if (targetRefundStatusId is null)
            {
                _logger.LogInformation(
                    "Refund webhook: No mapping found for GHN status '{Status}' on Refund request ID {RefundId}",
                    newStatus, refund.RefundId);
                return;
            }

            if (refund.StatusId == targetRefundStatusId.Value)
            {
                return;
            }

            if (refund.StatusId == (byte)RefundStatusEnum.RefundCompleted || 
                refund.StatusId == (byte)RefundStatusEnum.RefundCancelled)
            {
                _logger.LogInformation(
                    "Refund webhook: Skip updating Refund request ID {RefundId} (status is already final: {Status})",
                    refund.RefundId, refund.StatusId);
                return;
            }

            var now = _timeProvider.UtcNow;
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                refund.StatusId = targetRefundStatusId.Value;
                refund.UpdatedAt = now;

                if (targetRefundStatusId.Value == (byte)RefundStatusEnum.RefundCancelled)
                {
                    refund.CancelledAt = now;
                }

                refund.RefundStatusHistories.Add(new RefundStatusHistory
                {
                    StatusId = targetRefundStatusId.Value,
                    ChangedBy = null,
                    Note = $"Auto-updated from GHN shipping webhook: {newStatus}",
                    CreatedAt = now
                });

                _unitOfWork.Refunds.Update(refund);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation(
                    "Refund webhook processed: RefundId={RefundId}, status transitioned to StatusId={StatusId} via provider status={ProviderStatus}",
                    refund.RefundId, targetRefundStatusId.Value, newStatus);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Transaction failed while updating Refund ID {RefundId} via webhook", refund.RefundId);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Refund webhook for code {Code}", refund.ShippingOrderCode);
        }
    }
}
