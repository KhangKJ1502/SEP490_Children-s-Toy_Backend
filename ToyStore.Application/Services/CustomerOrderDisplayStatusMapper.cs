using ToyStore.Application.DTOs.Orders;
using ToyStore.Domain.Constants;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Enums;

namespace ToyStore.Application.Services;

/// <summary>
/// Maps internal order status + optional GHN shipping status to customer-facing labels.
/// Never exposes raw StatusID or internal names like Returning/ReturnCompleted.
/// </summary>
public static class CustomerOrderDisplayStatusMapper
{
    public const string DeliveringLabel = "Delivering";
    public const string ReturningLabel = "Returning to shop";
    public const string ReturnedToWarehouseLabel = "Returned to shop";
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


    private static OrderRefund? GetLatestRefund(Order order)
        => order.OrderRefunds
            .Where(r => !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();

    public static string ToCustomerDisplayStatus(
        string? internalStatusName,
        string? ghnShippingStatus = null,
        bool hasActiveRefund = false,
        byte? refundStatusId = null)
    {
        if (refundStatusId.HasValue)
        {
            var rStatus = (RefundStatusEnum)refundStatusId.Value;
            if (rStatus == RefundStatusEnum.RefundReturnShipmentCreated
                || rStatus == RefundStatusEnum.RefundReturningToCustomer)
            {
                return "Returning to you";
            }
            if (rStatus == RefundStatusEnum.RefundReturnedToCustomer)
            {
                return "Returned (Rejected)";
            }
            if (rStatus == RefundStatusEnum.RefundReturnToCustomerFailed)
            {
                return "Return to you failed";
            }
        }

        if (string.IsNullOrWhiteSpace(internalStatusName))
            return internalStatusName ?? string.Empty;

        if (internalStatusName.Equals(OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
            return hasActiveRefund ? RefundProcessingLabel : CancelledLabel;

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

    public static bool HasActiveRefund(Order order)
        => order.OrderRefunds.Any(r =>
            !r.IsDeleted
            && r.StatusId != (byte)RefundStatusEnum.RefundRejected
            && r.StatusId != (byte)RefundStatusEnum.RefundCancelled
            && r.StatusId != (byte)RefundStatusEnum.RefundReturnedToCustomer
            && r.StatusId != (byte)RefundStatusEnum.RefundReturnToCustomerFailed);

    public static string MapOrderListStatus(Order order)
    {
        var ghnStatus = GetLatestGhnStatus(order);
        var hasActiveRefund = HasActiveRefund(order);
        var latestRefund = GetLatestRefund(order);
        var refundStatusId = latestRefund?.StatusId;
        return ToCustomerDisplayStatus(order.Status.StatusName, ghnStatus, hasActiveRefund, refundStatusId);
    }

    public static void ApplyCustomerOrderContract(Order order, CustomerOrderListItemDto dto)
    {
        var ghnStatus = GetLatestGhnStatus(order);
        var internalName = order.Status.StatusName;
        var hasActiveRefund = HasActiveRefund(order);
        var latestRefund = GetLatestRefund(order);
        var refundStatusId = latestRefund?.StatusId;
        var display = ToCustomerDisplayStatus(internalName, ghnStatus, hasActiveRefund, refundStatusId);

        dto.StatusName = display;
        dto.HasActiveRefund = hasActiveRefund;
        dto.StatusCode = internalName;
        dto.StatusBucket = MapStatusBucket(internalName);
        dto.DisplayLabel = display;
        dto.PaymentDisplay = MapPaymentDisplay(order.PaymentStatus, order.PaymentMethod, hasActiveRefund);
        dto.RefundDestination = "wallet";
        dto.CanCancel = CanCustomerCancel(order);
        dto.CanComplete = string.Equals(internalName, OrderStatuses.Delivered, StringComparison.OrdinalIgnoreCase);
        dto.IsAwaitingRefund = hasActiveRefund
            && string.Equals(order.PaymentStatus, PaymentStatuses.Paid, StringComparison.OrdinalIgnoreCase);
        dto.CanRefund = CanRefund(order);
    }

    public static void ApplyCustomerOrderContract(Order order, CustomerOrderDetailDto dto)
    {
        var ghnStatus = GetLatestGhnStatus(order);
        var internalName = order.Status.StatusName;
        var hasActiveRefund = HasActiveRefund(order);
        var latestRefund = GetLatestRefund(order);
        var refundStatusId = latestRefund?.StatusId;
        var display = ToCustomerDisplayStatus(internalName, ghnStatus, hasActiveRefund, refundStatusId);

        dto.StatusName = display;
        dto.HasActiveRefund = hasActiveRefund;
        dto.StatusCode = internalName;
        dto.StatusBucket = MapStatusBucket(internalName);
        dto.DisplayLabel = display;
        dto.PaymentDisplay = MapPaymentDisplay(order.PaymentStatus, order.PaymentMethod, hasActiveRefund);
        dto.RefundDestination = "wallet";
        dto.CanCancel = CanCustomerCancel(order);
        dto.CanComplete = string.Equals(internalName, OrderStatuses.Delivered, StringComparison.OrdinalIgnoreCase);
        dto.IsAwaitingRefund = hasActiveRefund
            && string.Equals(order.PaymentStatus, PaymentStatuses.Paid, StringComparison.OrdinalIgnoreCase);
        dto.CanRefund = CanRefund(order);
    }

    private static string? GetLatestGhnStatus(Order order)
        => order.ShippingProviderTransactions
            .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
            .FirstOrDefault()?.Status;

    public static string MapStatusBucket(string internalStatusName)
    {
        var n = internalStatusName ?? string.Empty;
        if (string.Equals(n, OrderStatuses.Pending, StringComparison.OrdinalIgnoreCase))
            return "pending";
        if (n is OrderStatuses.Confirmed or OrderStatuses.Processing or OrderStatuses.Shipped)
            return "shipping";
        if (n is OrderStatuses.Delivered or OrderStatuses.Completed)
            return "completed";
        if (string.Equals(n, OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
            return "cancelled";
        if (string.Equals(n, OrderStatuses.Refunded, StringComparison.OrdinalIgnoreCase))
            return "refunded";
        return "delivering";
    }

    public static string MapPaymentDisplay(string paymentStatus, string paymentMethod, bool hasActiveRefund)
    {
        if (string.Equals(paymentStatus, PaymentStatuses.Refunded, StringComparison.OrdinalIgnoreCase))
            return "Refunded to wallet";
        if (string.Equals(paymentStatus, PaymentStatuses.PartiallyRefunded, StringComparison.OrdinalIgnoreCase))
            return "Partially refunded to wallet";
        if (hasActiveRefund && string.Equals(paymentStatus, PaymentStatuses.Paid, StringComparison.OrdinalIgnoreCase))
            return "Paid — refund to wallet pending";
        if (string.Equals(paymentStatus, PaymentStatuses.Paid, StringComparison.OrdinalIgnoreCase))
            return "Paid";
        if (string.Equals(paymentStatus, PaymentStatuses.CodPending, StringComparison.OrdinalIgnoreCase))
            return "Pay on delivery";
        if (string.Equals(paymentStatus, PaymentStatuses.Pending, StringComparison.OrdinalIgnoreCase)
            && string.Equals(paymentMethod, "SE_PAY", StringComparison.OrdinalIgnoreCase))
            return "Awaiting payment";
        return paymentStatus;
    }

    private static bool CanCustomerCancel(Order order)
    {
        var status = order.Status.StatusName;
        if (!OrderStatuses.CancellableStatuses.Contains(status))
            return false;
        if (string.Equals(order.PaymentMethod, "SHIP_COD", StringComparison.OrdinalIgnoreCase)
            && string.Equals(status, OrderStatuses.Confirmed, StringComparison.OrdinalIgnoreCase))
            return false;
        return true;
    }

    /// <summary>
    /// True when order should appear under customer "delivering" tab filter.
    /// </summary>
    public static bool IsDeliveringTabStatus(string? internalStatusName, string? ghnShippingStatus)
    {
        if (internalStatusName is OrderStatuses.Cancelled or OrderStatuses.Refunded or OrderStatuses.Completed or OrderStatuses.Delivered)
            return false;

        return DeliveringLikeStatuses.Contains(internalStatusName ?? string.Empty)
           || internalStatusName is OrderStatuses.Returning or OrderStatuses.ReturnCompleted
           || internalStatusName is OrderStatuses.DeliveryFailed or OrderStatuses.WaitingReturn or OrderStatuses.ReturnFailed or OrderStatuses.Lost or OrderStatuses.Damaged
            || IsGhnReturnInProgress(ghnShippingStatus)
            || IsGhnReturned(ghnShippingStatus);
    }

    public static bool CanRefund(Order order)
    {
        var internalName = order.Status.StatusName;
        if (!string.Equals(internalName, OrderStatuses.Completed, StringComparison.OrdinalIgnoreCase))
            return false;

        if (order.CompletedAt == null || (DateTime.UtcNow - order.CompletedAt.Value).TotalDays > 3)
            return false;

        var existingRefunds = order.OrderRefunds.Where(r => !r.IsDeleted).ToList();

        // Limit to maximum 2 refund attempts per order
        if (existingRefunds.Count >= 2)
            return false;

        foreach (var r in existingRefunds)
        {
            if (r.StatusId != (byte)RefundStatusEnum.RefundCancelled && 
                r.StatusId != (byte)RefundStatusEnum.RefundRejected &&
                r.StatusId != (byte)RefundStatusEnum.RefundReturnedToCustomer &&
                r.StatusId != (byte)RefundStatusEnum.RefundReturnToCustomerFailed)
            {
                return false;
            }

            var wentPastRequested = r.RefundStatusHistories.Any(h =>
                h.StatusId != (byte)RefundStatusEnum.RefundRequested &&
                h.StatusId != (byte)RefundStatusEnum.RefundCancelled &&
                h.StatusId != (byte)RefundStatusEnum.RefundRejected);

            if (wentPastRequested && r.StatusId != (byte)RefundStatusEnum.RefundRejected && r.StatusId != (byte)RefundStatusEnum.RefundReturnedToCustomer)
            {
                return false;
            }
        }

        return true;
    }
}
