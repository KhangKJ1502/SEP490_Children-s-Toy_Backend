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
        if (string.IsNullOrWhiteSpace(internalStatusName))
            return string.Empty;

        // Kiểm tra trạng thái Refund liên quan đến giao hàng trả lại cho khách TRƯỚC KHI check
        // Completed/Delivered. CHỈ override khi refund đang ACTIVE (đang xử lý), KHÔNG override
        // khi refund đã ở trạng thái terminal (RefundReturnedToCustomer / RefundReturnToCustomerFailed)
        // để tránh đè lên trạng thái Completed/Delivered của đơn hàng gốc.
        // hasActiveRefund đã loại trừ đúng các trạng thái terminal này.
        if (hasActiveRefund && refundStatusId.HasValue)
        {
            var rStatus = (RefundStatusEnum)refundStatusId.Value;
            if (rStatus == RefundStatusEnum.RefundReturnShipmentCreated
                || rStatus == RefundStatusEnum.RefundReturningToCustomer)
            {
                return "Returning to you";
            }
        }

        if (internalStatusName.Equals(OrderStatuses.Completed, StringComparison.OrdinalIgnoreCase))
            return OrderStatuses.Completed;

        if (internalStatusName.Equals(OrderStatuses.Delivered, StringComparison.OrdinalIgnoreCase))
            return OrderStatuses.Delivered;

        if (internalStatusName.Equals(OrderStatuses.Refunded, StringComparison.OrdinalIgnoreCase))
            return RefundedLabel;

        if (internalStatusName.Equals(OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
            return CancelledLabel;

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

        if (internalStatusName.Equals(OrderStatuses.DeliveryFailed, StringComparison.OrdinalIgnoreCase))
        {
            return "Delivery failed";
        }

        if (internalStatusName.Equals(OrderStatuses.Lost, StringComparison.OrdinalIgnoreCase)
            || internalStatusName.Equals(OrderStatuses.Damaged, StringComparison.OrdinalIgnoreCase))
        {
            return "Delivery failed (Issue)";
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

        if (dto.StatusHistory != null && dto.StatusHistory.Any())
        {
            dto.StatusHistory = CustomerOrderTimelineFilter.FilterStatusHistory(dto.StatusHistory, order.CancelledAt, internalName);
        }
    }

    public static ShippingProviderTransaction? GetOriginalOrderShippingTransaction(Order order)
    {
        if (order?.ShippingProviderTransactions == null || !order.ShippingProviderTransactions.Any())
            return null;

        // 1. Ưu tiên 1: Tìm transaction có ProviderOrderCode trùng với ShippingOrderCode của đơn hàng gốc (không chứa prefix refund)
        if (!string.IsNullOrWhiteSpace(order.ShippingOrderCode))
        {
            var matchMain = order.ShippingProviderTransactions
                .Where(t => string.Equals(t.ProviderOrderCode, order.ShippingOrderCode, StringComparison.OrdinalIgnoreCase))
                .Where(t => !t.ProviderOrderCode.StartsWith("R-", StringComparison.OrdinalIgnoreCase) &&
                            !t.ProviderOrderCode.StartsWith("R2-", StringComparison.OrdinalIgnoreCase) &&
                            !t.ProviderOrderCode.StartsWith("REF-", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
                .FirstOrDefault();

            if (matchMain != null)
                return matchMain;
        }

        // 2. Ưu tiên 2: Transaction có RefundId == null
        var mainTx = order.ShippingProviderTransactions
            .Where(t => t.RefundId == null)
            .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
            .FirstOrDefault();

        if (mainTx != null)
            return mainTx;

        // 3. Ưu tiên 3: Transaction không nằm trong danh sách mã refund và không có tiền tố refund R-/R2-/REF-
        var refundCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (order.OrderRefunds != null)
        {
            foreach (var r in order.OrderRefunds)
            {
                if (!string.IsNullOrWhiteSpace(r.ShippingOrderCode)) refundCodes.Add(r.ShippingOrderCode.Trim());
                if (!string.IsNullOrWhiteSpace(r.ReturnShippingOrderCode)) refundCodes.Add(r.ReturnShippingOrderCode.Trim());
                if (!string.IsNullOrWhiteSpace(r.RefundCode)) refundCodes.Add(r.RefundCode.Trim());
            }
        }

        return order.ShippingProviderTransactions
            .Where(t => !string.IsNullOrWhiteSpace(t.ProviderOrderCode))
            .Where(t => !refundCodes.Contains(t.ProviderOrderCode.Trim()))
            .Where(t => !t.ProviderOrderCode.StartsWith("R-", StringComparison.OrdinalIgnoreCase) &&
                        !t.ProviderOrderCode.StartsWith("R2-", StringComparison.OrdinalIgnoreCase) &&
                        !t.ProviderOrderCode.StartsWith("REF-", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
            .FirstOrDefault();
        // Tuyệt đối không fallback về FirstOrDefault() nữa để tránh nhầm sang transaction của Refund (R-, R2-)
    }

    private static string? GetLatestGhnStatus(Order order)
        => GetOriginalOrderShippingTransaction(order)?.Status;

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
