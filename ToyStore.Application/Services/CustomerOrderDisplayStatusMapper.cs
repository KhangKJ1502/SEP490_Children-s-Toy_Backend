using ToyStore.Domain.Constants;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Services;

/// <summary>
/// Maps internal order status + optional GHN shipping status to customer-facing labels.
/// Never exposes raw StatusID or internal names like Returning/ReturnCompleted.
/// </summary>
public static class CustomerOrderDisplayStatusMapper
{
    public const string DeliveringLabel = "Delivering";
    public const string ReturningLabel = "Returning to warehouse";
    public const string ReturnedToWarehouseLabel = "Returned to warehouse";
    public const string RefundProcessingLabel = "Refund processing";
    public const string CancelledLabel = "Cancelled";
    public const string RefundedLabel = "Refunded";

    private static readonly HashSet<string> DeliveringLikeStatuses =
        new(StringComparer.OrdinalIgnoreCase)
        {
            OrderStatuses.Shipped,
            OrderStatuses.Delivering,
        };

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

    public static string ToCustomerDisplayStatus(
        string? internalStatusName,
        string? ghnShippingStatus = null,
        bool hasActiveRefund = false)
    {
        if (string.IsNullOrWhiteSpace(internalStatusName))
            return internalStatusName ?? string.Empty;

        if (internalStatusName.Equals(OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
            return CancelledLabel;

        if (internalStatusName.Equals(OrderStatuses.Refunded, StringComparison.OrdinalIgnoreCase))
            return RefundedLabel;

        if (internalStatusName.Equals(OrderStatuses.Returning, StringComparison.OrdinalIgnoreCase)
            || internalStatusName.Equals(OrderStatuses.WaitingReturn, StringComparison.OrdinalIgnoreCase)
            || internalStatusName.Equals(OrderStatuses.ReturnFailed, StringComparison.OrdinalIgnoreCase)
            || IsGhnReturnInProgress(ghnShippingStatus))
        {
            return ReturningLabel;
        }

        if (internalStatusName.Equals(OrderStatuses.ReturnCompleted, StringComparison.OrdinalIgnoreCase)
            || IsGhnReturned(ghnShippingStatus))
        {
            return hasActiveRefund ? RefundProcessingLabel : ReturnedToWarehouseLabel;
        }

        if (internalStatusName.Equals(OrderStatuses.DeliveryFailed, StringComparison.OrdinalIgnoreCase))
        {
            return "Delivery failed";
        }

        if (internalStatusName.Equals(OrderStatuses.Lost, StringComparison.OrdinalIgnoreCase)
            || internalStatusName.Equals(OrderStatuses.Damaged, StringComparison.OrdinalIgnoreCase))
        {
            return hasActiveRefund ? RefundProcessingLabel : "Delivery failed (Issue)";
        }

        if (DeliveringLikeStatuses.Contains(internalStatusName))
            return DeliveringLabel;

        return internalStatusName;
    }

    public static string ToCustomerHistoryDisplayStatus(
        string? internalStatusName,
        string? ghnShippingStatus = null)
    {
        if (string.IsNullOrWhiteSpace(internalStatusName))
            return internalStatusName ?? string.Empty;

        if (internalStatusName.Equals(OrderStatuses.Returning, StringComparison.OrdinalIgnoreCase)
            || internalStatusName.Equals(OrderStatuses.WaitingReturn, StringComparison.OrdinalIgnoreCase)
            || internalStatusName.Equals(OrderStatuses.ReturnFailed, StringComparison.OrdinalIgnoreCase)
            || IsGhnReturnInProgress(ghnShippingStatus))
        {
            return ReturningLabel;
        }

        if (internalStatusName.Equals(OrderStatuses.ReturnCompleted, StringComparison.OrdinalIgnoreCase)
            || IsGhnReturned(ghnShippingStatus))
        {
            return ReturnedToWarehouseLabel;
        }

        return ToCustomerDisplayStatus(internalStatusName, ghnShippingStatus);
    }

    public static bool IsGhnReturnInProgress(string? ghnShippingStatus)
        => !string.IsNullOrWhiteSpace(ghnShippingStatus)
           && GhnReturnInProgressStatuses.Contains(ghnShippingStatus.Trim());

    public static bool IsGhnReturned(string? ghnShippingStatus)
        => string.Equals(ghnShippingStatus, ShippingStatuses.Returned, StringComparison.OrdinalIgnoreCase);

    public static string MapOrderListStatus(Order order)
    {
        var ghnStatus = order.ShippingProviderTransactions
            .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
            .FirstOrDefault()?.Status;

        var hasActiveRefund = order.OrderRefunds.Any(r =>
            r.RefundStatus == "Requested" || r.RefundStatus == "Approved");

        return ToCustomerDisplayStatus(order.Status.StatusName, ghnStatus, hasActiveRefund);
    }

    /// <summary>
    /// True when order should appear under customer "delivering" tab filter.
    /// </summary>
    public static bool IsDeliveringTabStatus(string? internalStatusName, string? ghnShippingStatus)
        => DeliveringLikeStatuses.Contains(internalStatusName ?? string.Empty)
           || internalStatusName is OrderStatuses.Returning or OrderStatuses.ReturnCompleted
           || internalStatusName is OrderStatuses.DeliveryFailed or OrderStatuses.WaitingReturn or OrderStatuses.ReturnFailed or OrderStatuses.Lost or OrderStatuses.Damaged
           || IsGhnReturnInProgress(ghnShippingStatus)
           || IsGhnReturned(ghnShippingStatus);
}
