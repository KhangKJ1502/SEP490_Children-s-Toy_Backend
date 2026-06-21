using System.Text.Json;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Constants;
using ToyStore.Application.Services;
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

            providerOrderCode = providerOrderCode.Trim();
            newStatus = newStatus.Trim();

            var refund = await _unitOfWork.Refunds.GetByShippingOrReturnOrderCodeAsync(providerOrderCode, cancellationToken);
            if (refund is not null)
            {
                await HandleRefundWebhookAsync(refund, providerOrderCode, newStatus, rawPayload, cancellationToken);
                return;
            }

            var tx = await _unitOfWork.Orders.GetShippingTransactionByProviderCodeAsync(
                providerOrderCode, cancellationToken);

            if (tx is null)
            {
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

        if (!OrderWebhookTransitionValidator.CanApplyWebhookStatus(order.StatusId, targetStatusId))
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
        string providerOrderCode,
        string newStatus,
        string rawPayload,
        CancellationToken cancellationToken)
    {
        try
        {
            bool isReturnToCustomer = string.Equals(providerOrderCode, refund.ReturnShippingOrderCode, StringComparison.OrdinalIgnoreCase);

            byte? targetRefundStatusId;
            if (isReturnToCustomer)
            {
                targetRefundStatusId = newStatus.ToLowerInvariant() switch
                {
                    ShippingStatuses.ReadyToPick or ShippingStatuses.Storing => (byte)RefundStatusEnum.RefundReturnShipmentCreated,
                    
                    ShippingStatuses.Picking or ShippingStatuses.Picked or 
                    ShippingStatuses.Transporting or ShippingStatuses.Sorting or 
                    ShippingStatuses.Delivering or ShippingStatuses.MoneyCollectDelivering or
                    ShippingStatuses.ReturnTransporting or ShippingStatuses.ReturnSorting or 
                    ShippingStatuses.Returning => (byte)RefundStatusEnum.RefundReturningToCustomer,
                    
                    ShippingStatuses.Delivered or ShippingStatuses.Returned => (byte)RefundStatusEnum.RefundReturnedToCustomer,
                    
                    ShippingStatuses.Cancel or ShippingStatuses.DeliveryFail or ShippingStatuses.ReturnFail or 
                    ShippingStatuses.Lost or ShippingStatuses.Damage or ShippingStatuses.Exception => (byte)RefundStatusEnum.RefundReturnToCustomerFailed,
                    
                    _ => null
                };
            }
            else
            {
                targetRefundStatusId = newStatus.ToLowerInvariant() switch
                {
                    ShippingStatuses.ReadyToPick or ShippingStatuses.Storing => (byte)RefundStatusEnum.RefundPickupCreated,
                    
                    ShippingStatuses.Picking or ShippingStatuses.Picked or 
                    ShippingStatuses.Transporting or ShippingStatuses.Sorting or 
                    ShippingStatuses.Delivering or ShippingStatuses.MoneyCollectDelivering or
                    ShippingStatuses.ReturnTransporting or ShippingStatuses.ReturnSorting or 
                    ShippingStatuses.Returning or
                    ShippingStatuses.Delivered or ShippingStatuses.Returned => (byte)RefundStatusEnum.RefundShipping,
                    
                    ShippingStatuses.Cancel or ShippingStatuses.ReturnFail or 
                    ShippingStatuses.Exception => (byte)RefundStatusEnum.RefundCancelled,

                    ShippingStatuses.Lost or ShippingStatuses.Damage => (byte)RefundStatusEnum.RefundDamage,
                    
                    _ => null
                };
            }

            var now = _timeProvider.UtcNow;
            var tx = await _unitOfWork.Orders.GetShippingTransactionByProviderCodeAsync(providerOrderCode, cancellationToken);
            var previousStatus = tx?.Status ?? string.Empty;

            if (tx is not null && await _unitOfWork.Orders.ExistsShippingStatusHistoryAsync(
                    tx.ShippingTransactionId, newStatus, rawPayload, cancellationToken))
            {
                _logger.LogInformation(
                    "Refund shipping webhook duplicate skipped: code={Code}, status={Status}",
                    providerOrderCode, newStatus);
                return;
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // 1. Ensure ShippingProviderTransaction and ShippingStatusHistory exist
                if (tx is null)
                {
                    tx = new ShippingProviderTransaction
                    {
                        OrderId = refund.OrderId,
                        Provider = "GHN",
                        ProviderOrderCode = refund.ShippingOrderCode,
                        Status = newStatus,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    await _unitOfWork.Orders.AddShippingTransactionAsync(tx, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    tx.Status = newStatus;
                    tx.UpdatedAt = now;
                }

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

                // 2. Process Refund Status transition
                if (targetRefundStatusId is not null && refund.StatusId != targetRefundStatusId.Value)
                {
                    if (refund.StatusId != (byte)RefundStatusEnum.RefundCompleted && 
                        (refund.StatusId != (byte)RefundStatusEnum.RefundCancelled || targetRefundStatusId.Value == (byte)RefundStatusEnum.RefundDamage))
                    {
                        var previousRefundStatusId = refund.StatusId;
                        refund.StatusId = targetRefundStatusId.Value;
                        refund.UpdatedAt = now;

                        if (targetRefundStatusId.Value == (byte)RefundStatusEnum.RefundCancelled)
                        {
                            refund.CancelledAt = now;
                            await _unitOfWork.OrderAssignments.ReleaseCapacityAsync(refund.OrderId, cancellationToken);
                        }
                        else if (targetRefundStatusId.Value == (byte)RefundStatusEnum.RefundDamage)
                        {
                            refund.CancelledAt = null;
                            refund.AdminNote = "Orders are damaged/lost during shipping (GHN updates Damage/Lost). No quality inspection is required.";

                            if (previousRefundStatusId == (byte)RefundStatusEnum.RefundCancelled)
                            {
                                // Reactivate original order assignments and increment shift workloads
                                var orderAssignments = await _unitOfWork.OrderAssignments.GetAssignmentsByOrderIdAsync(refund.OrderId, cancellationToken);
                                foreach (var oa in orderAssignments)
                                {
                                    oa.IsActive = true;

                                    var capacity = await _unitOfWork.StaffShiftCapacities.GetByScheduleIdForUpdateAsync(oa.ScheduleId, cancellationToken);
                                    if (capacity is not null)
                                    {
                                        capacity.CurrentLoad++;
                                        capacity.UpdatedAt = now;
                                    }
                                }
                            }
                        }

                        refund.RefundStatusHistories.Add(new RefundStatusHistory
                        {
                            StatusId = targetRefundStatusId.Value,
                            ChangedBy = null,
                            Note = $"Auto-updated from GHN shipping webhook: {newStatus}",
                            CreatedAt = now
                        });

                        _unitOfWork.Refunds.Update(refund);
                    }
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation(
                    "Refund webhook processed: RefundId={RefundId}, status transitioned to StatusId={StatusId} via provider status={ProviderStatus}",
                    refund.RefundId, targetRefundStatusId?.ToString() ?? "none", newStatus);
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
            _logger.LogError(ex, "Error processing Refund webhook for code {Code}", providerOrderCode);
        }
    }
}
