namespace ToyStore.Domain.Enums;

/// <summary>
/// Action to take when processing a GHN/GHTK shipping webhook status update.
/// </summary>
public enum ShippingWebhookAction
{
    /// <summary>Map to internal order status via MapToInternalStatus (shipped/delivering/delivered).</summary>
    UpdateOrderStatus,

    HandleDeliveryFail,
    /// <summary>GHN <c>return</c> — COD: cancel order; prepaid: set Returning.</summary>
    HandleReturnStarted,
    SetReturning,
    KeepReturning,
    HandleReturnCompleted,
    HandleReturnFail,
    HandleDamageLost,
    HandleGhnCancel,
    HandleException,

    /// <summary>Unknown status — log only, no order mutation.</summary>
    Unknown
}
