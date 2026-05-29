using Microsoft.Extensions.Logging;
using ToyStore.Application.Constants;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Constants;
using ToyStore.Domain.Enums;

namespace ToyStore.Application.Services;

public class ShippingStatusMapper : IShippingStatusMapper
{
    private readonly ILogger<ShippingStatusMapper> _logger;

    public ShippingStatusMapper(ILogger<ShippingStatusMapper> logger)
    {
        _logger = logger;
    }

    public OrderStatus? MapToInternalStatus(string? providerStatus)
    {
        if (string.IsNullOrWhiteSpace(providerStatus)) return null;

        var status = providerStatus.ToLowerInvariant();

        switch (status)
        {
            case ShippingStatuses.ReadyToPick:
            case ShippingStatuses.Picking:
            case ShippingStatuses.Picked:
            case ShippingStatuses.Storing:
            case ShippingStatuses.Sorting:
            case ShippingStatuses.Transporting:
                return OrderStatus.Shipped;

            case ShippingStatuses.Delivering:
            case ShippingStatuses.MoneyCollectDelivering:
                return OrderStatus.Delivering;

            case ShippingStatuses.Delivered:
                return OrderStatus.Delivered;

            default:
                return null;
        }
    }

    public ShippingWebhookAction ResolveWebhookAction(string? providerStatus)
    {
        if (string.IsNullOrWhiteSpace(providerStatus))
            return ShippingWebhookAction.Unknown;

        var status = providerStatus.ToLowerInvariant();

        return status switch
        {
            ShippingStatuses.DeliveryFail => ShippingWebhookAction.HandleDeliveryFail,
            ShippingStatuses.WaitingToReturn => ShippingWebhookAction.SetReturning,
            ShippingStatuses.Return => ShippingWebhookAction.HandleReturnStarted,
            ShippingStatuses.ReturnTransporting or ShippingStatuses.ReturnSorting
                or ShippingStatuses.Returning => ShippingWebhookAction.KeepReturning,
            ShippingStatuses.Returned => ShippingWebhookAction.HandleReturnCompleted,
            ShippingStatuses.ReturnFail => ShippingWebhookAction.HandleReturnFail,
            ShippingStatuses.Damage or ShippingStatuses.Lost => ShippingWebhookAction.HandleDamageLost,
            ShippingStatuses.Cancel => ShippingWebhookAction.HandleGhnCancel,
            ShippingStatuses.Exception => ShippingWebhookAction.HandleException,
            _ when MapToInternalStatus(status).HasValue => ShippingWebhookAction.UpdateOrderStatus,
            _ => ShippingWebhookAction.Unknown
        };
    }

    public string? ResolveNotificationEventType(string? providerStatus)
    {
        if (string.IsNullOrWhiteSpace(providerStatus)) return null;

        return providerStatus.ToLowerInvariant() switch
        {
            ShippingStatuses.Picked => NotificationEventTypes.MerchPickedUp,
            ShippingStatuses.Delivering or ShippingStatuses.MoneyCollectDelivering
                => NotificationEventTypes.OrderDelivering,
            ShippingStatuses.Delivered => NotificationEventTypes.OrderDelivered,
            ShippingStatuses.DeliveryFail => NotificationEventTypes.OrderDeliveryFailed,
            ShippingStatuses.Return or ShippingStatuses.WaitingToReturn
                or ShippingStatuses.ReturnTransporting or ShippingStatuses.ReturnSorting
                or ShippingStatuses.Returning => NotificationEventTypes.OrderReturning,
            ShippingStatuses.Returned => NotificationEventTypes.MerchReturned,
            ShippingStatuses.ReturnFail => NotificationEventTypes.OrderReturnFail,
            ShippingStatuses.Damage or ShippingStatuses.Lost
                => NotificationEventTypes.SystemShippingDamageLost,
            ShippingStatuses.Exception => NotificationEventTypes.SystemShippingWebhookError,
            _ => null
        };
    }

    public string GetStatusDescription(string? providerStatus)
    {
        if (string.IsNullOrWhiteSpace(providerStatus)) return "Processing";

        return providerStatus.ToLowerInvariant() switch
        {
            ShippingStatuses.ReadyToPick => "Ready to pick",
            ShippingStatuses.Picking => "Picking up",
            ShippingStatuses.Picked => "Picked up",
            ShippingStatuses.Storing => "Storing in warehouse",
            ShippingStatuses.Transporting => "Transporting",
            ShippingStatuses.Sorting => "Sorting",
            ShippingStatuses.Delivering => "Out for delivery",
            ShippingStatuses.MoneyCollectDelivering => "Delivering and collecting payment",
            ShippingStatuses.Delivered => "Delivered successfully",
            ShippingStatuses.DeliveryFail => "Delivery failed",
            ShippingStatuses.Cancel => "Shipping cancelled",
            ShippingStatuses.WaitingToReturn => "Waiting for return",
            ShippingStatuses.Return => "Returning",
            ShippingStatuses.Returned => "Returned",
            ShippingStatuses.ReturnFail => "Return failed",
            ShippingStatuses.ReturnSorting => "Return sorting",
            ShippingStatuses.ReturnTransporting => "Return transporting",
            ShippingStatuses.Returning => "In return process",
            ShippingStatuses.Exception => "Delivery exception",
            ShippingStatuses.Damage => "Damaged",
            ShippingStatuses.Lost => "Lost",
            _ => providerStatus
        };
    }

    public string NormalizeStatus(string? providerStatus, OrderStatus currentInternalStatus)
    {
        var mapped = MapToInternalStatus(providerStatus);
        if (mapped.HasValue)
            return mapped.Value.ToString();

        if (currentInternalStatus is OrderStatus.Returning or OrderStatus.ReturnCompleted)
            return OrderStatuses.Delivering;

        return currentInternalStatus.ToString();
    }
}
