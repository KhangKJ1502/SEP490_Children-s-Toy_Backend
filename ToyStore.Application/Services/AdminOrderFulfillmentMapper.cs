using ToyStore.Domain.Constants;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Enums;

namespace ToyStore.Application.Services;

/// <summary>
/// Admin-facing labels for order fulfillment and GHN return-to-warehouse flow.
/// </summary>
public static class AdminOrderFulfillmentMapper
{
    private static readonly HashSet<string> GhnReturnInProgressStatuses =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ShippingStatuses.WaitingToReturn,
            ShippingStatuses.Return,
            ShippingStatuses.ReturnTransporting,
            ShippingStatuses.ReturnSorting,
            ShippingStatuses.Returning,
            ShippingStatuses.ReturnFail,
        };

    private static readonly HashSet<string> GhnReturnStatuses =
        new(GhnReturnInProgressStatuses, StringComparer.OrdinalIgnoreCase)
        {
            ShippingStatuses.Returned
        };

    public static readonly byte DeliveringStatusId = (byte)OrderStatus.Delivering;
    public static readonly byte ReturningStatusId = (byte)OrderStatus.Returning;
    public static readonly byte ReturnCompletedStatusId = (byte)OrderStatus.ReturnCompleted;

    public static IReadOnlyCollection<byte> DeliveringGroupStatusIds { get; } =
        [DeliveringStatusId, ReturningStatusId, ReturnCompletedStatusId];

    public static string? GetLatestGhnStatus(Order order)
    {
        return CustomerOrderDisplayStatusMapper.GetOriginalOrderShippingTransaction(order)?.Status;
    }

    public static bool IsGhnReturnInProgress(string? ghnStatus)
        => !string.IsNullOrWhiteSpace(ghnStatus)
           && GhnReturnInProgressStatuses.Contains(ghnStatus.Trim());

    public static bool IsGhnReturned(string? ghnStatus)
        => string.Equals(ghnStatus, ShippingStatuses.Returned, StringComparison.OrdinalIgnoreCase);

    public static string GetFulfillmentLabel(Order order)
        => GetFulfillmentLabel(
            order.Status?.StatusName ?? string.Empty,
            GetLatestGhnStatus(order),
            order.CancelReason);

    public static string GetFulfillmentLabel(
        string internalStatusName,
        string? ghnShippingStatus,
        string? cancelReason)
    {
        if (internalStatusName.Equals(OrderStatuses.Returning, StringComparison.OrdinalIgnoreCase))
            return "Returning to shop";

        if (internalStatusName.Equals(OrderStatuses.ReturnCompleted, StringComparison.OrdinalIgnoreCase))
            return "Returned";

        if (internalStatusName.Equals(OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(cancelReason)
                ? "Cancelled"
                : $"Cancelled: {cancelReason}";
        }

        if (IsGhnReturnInProgress(ghnShippingStatus))
            return $"Delivering (GHN: {FormatGhnStatus(ghnShippingStatus)})";

        if (IsGhnReturned(ghnShippingStatus))
            return "Returned to shop (GHN)";

        return internalStatusName;
    }

    public static string FormatGhnStatus(string? ghnStatus)
    {
        if (string.IsNullOrWhiteSpace(ghnStatus)) return string.Empty;

        return ghnStatus.Trim().ToLowerInvariant() switch
        {
            ShippingStatuses.ReadyToPick => "Ready to pick",
            ShippingStatuses.Picking => "Picking",
            ShippingStatuses.Picked => "Picked",
            ShippingStatuses.Delivering => "Delivering",
            ShippingStatuses.MoneyCollectDelivering => "Delivering + COD",
            ShippingStatuses.Delivered => "Delivered",
            ShippingStatuses.DeliveryFail => "Delivery failed",
            ShippingStatuses.WaitingToReturn => "Waiting to return",
            ShippingStatuses.Return => "Return",
            ShippingStatuses.ReturnTransporting => "Return transporting",
            ShippingStatuses.ReturnSorting => "Return sorting",
            ShippingStatuses.Returning => "Returning",
            ShippingStatuses.Returned => "Returned",
            ShippingStatuses.ReturnFail => "Return failed",
            ShippingStatuses.Cancel => "GHN cancelled",
            _ => ghnStatus
        };
    }

    public static bool MatchesDeliveringGroupFilter(Order order)
    {
        if (order.StatusId == (byte)OrderStatus.Cancelled
            || order.StatusId == (byte)OrderStatus.Refunded
            || order.StatusId == (byte)OrderStatus.Completed
            || order.StatusId == (byte)OrderStatus.Delivered)
        {
            return false;
        }

        if (DeliveringGroupStatusIds.Contains(order.StatusId))
            return true;

        return order.ShippingProviderTransactions.Any(t =>
            t.Status != null
            && (GhnReturnInProgressStatuses.Contains(t.Status)
                || GhnReturnStatuses.Contains(t.Status)));
    }
}
