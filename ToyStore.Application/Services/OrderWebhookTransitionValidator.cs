using ToyStore.Domain.Enums;

namespace ToyStore.Application.Services;

/// <summary>
/// Validates GHN webhook forward fulfillment transitions (prevents skipping e.g. Shipped to Delivered).
/// </summary>
public static class OrderWebhookTransitionValidator
{
    private static readonly HashSet<byte> TerminalStatusIds =
    [
        (byte)OrderStatus.Cancelled,
        (byte)OrderStatus.Refunded,
        (byte)OrderStatus.ReturnCompleted,
        (byte)OrderStatus.Lost,
        (byte)OrderStatus.Damaged,
        (byte)OrderStatus.ReturnFailed
    ];

    /// <summary>
    /// Returns true if webhook may set order to targetStatusId from currentStatusId.
    /// </summary>
    public static bool CanApplyWebhookStatus(byte currentStatusId, byte targetStatusId)
    {
        if (targetStatusId == 0 || currentStatusId == targetStatusId)
            return false;

        if (TerminalStatusIds.Contains(currentStatusId))
            return false;

        if (targetStatusId == (byte)OrderStatus.Delivered)
            return currentStatusId == (byte)OrderStatus.Delivering
                   || currentStatusId == (byte)OrderStatus.Shipped;

        if (targetStatusId == (byte)OrderStatus.Delivering)
            return currentStatusId == (byte)OrderStatus.Shipped
                   || currentStatusId == (byte)OrderStatus.Processing;

        if (targetStatusId == (byte)OrderStatus.Shipped)
            return currentStatusId == (byte)OrderStatus.Processing
                   || currentStatusId == (byte)OrderStatus.Confirmed;

        return targetStatusId > currentStatusId
               && targetStatusId <= (byte)OrderStatus.Delivered;
    }
}
