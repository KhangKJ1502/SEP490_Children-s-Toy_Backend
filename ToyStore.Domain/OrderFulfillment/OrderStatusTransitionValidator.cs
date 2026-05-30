using ToyStore.Domain.Constants;

namespace ToyStore.Domain.OrderFulfillment;

/// <summary>
/// Centralized order status transition rules for fulfillment workflows.
/// </summary>
public static class OrderStatusTransitionValidator
{
    private static readonly Dictionary<string, HashSet<string>> AllowedTransitions = new(StringComparer.OrdinalIgnoreCase)
    {
        [OrderStatuses.Pending] = [OrderStatuses.Confirmed, OrderStatuses.Cancelled],
        [OrderStatuses.Confirmed] = [OrderStatuses.Processing, OrderStatuses.Cancelled],
        [OrderStatuses.Processing] = [OrderStatuses.Shipped, OrderStatuses.Cancelled],
        [OrderStatuses.Shipped] = [OrderStatuses.Delivering, OrderStatuses.Cancelled],
        [OrderStatuses.Delivering] = [OrderStatuses.Delivered, OrderStatuses.DeliveryFailed, OrderStatuses.Returning],
        [OrderStatuses.DeliveryFailed] = [OrderStatuses.Confirmed, OrderStatuses.Cancelled],
        [OrderStatuses.Delivered] = [OrderStatuses.Completed],
        [OrderStatuses.Returning] = [OrderStatuses.ReturnCompleted],
    };

    public static bool CanTransition(string fromStatus, string toStatus)
    {
        if (string.Equals(fromStatus, toStatus, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return AllowedTransitions.TryGetValue(fromStatus, out var targets)
               && targets.Contains(toStatus);
    }

    public static bool IsTerminal(string statusName)
        => statusName is OrderStatuses.Completed
            or OrderStatuses.Cancelled
            or OrderStatuses.Refunded;

    public static bool StaffRoleWorkComplete(string statusName)
        => statusName is not OrderStatuses.Pending and not OrderStatuses.DeliveryFailed;

    public static bool MerchRoleWorkComplete(string statusName)
        => statusName is OrderStatuses.Shipped
            or OrderStatuses.Delivering
            or OrderStatuses.Delivered
            or OrderStatuses.Completed
            or OrderStatuses.Returning
            or OrderStatuses.ReturnCompleted
            or OrderStatuses.DeliveryFailed
            or OrderStatuses.WaitingReturn
            or OrderStatuses.ReturnFailed
            or OrderStatuses.Lost
            or OrderStatuses.Damaged;
}
