using Microsoft.Extensions.Logging;
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
            // Shipped
            case ShippingStatuses.ReadyToPick:
            case ShippingStatuses.Picking:
            case ShippingStatuses.Picked:
            case ShippingStatuses.Storing:
            case ShippingStatuses.Sorting:
            case ShippingStatuses.Transporting:
                return OrderStatus.Shipped;

            // Delivering
            case ShippingStatuses.Delivering:
            case ShippingStatuses.MoneyCollectDelivering:
                return OrderStatus.Delivering;

            // Delivered
            case ShippingStatuses.Delivered:
                return OrderStatus.Delivered;

            // Cancelled
            case ShippingStatuses.Cancel:
            case ShippingStatuses.DeliveryFail:
            case ShippingStatuses.Lost:
            case ShippingStatuses.Damage:
            case ShippingStatuses.Exception:
            case string s when s.StartsWith("return"):
                return OrderStatus.Cancelled;

            default:
                _logger.LogWarning("Unknown shipping provider status encountered: {Status}", providerStatus);
                return null;
        }
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
        // If we have a provider status, try to map it to a normalized business status
        var mapped = MapToInternalStatus(providerStatus);
        if (mapped.HasValue)
        {
            return mapped.Value.ToString();
        }

        // Fallback to current internal status if provider status is unknown or missing
        return currentInternalStatus.ToString();
    }
}
