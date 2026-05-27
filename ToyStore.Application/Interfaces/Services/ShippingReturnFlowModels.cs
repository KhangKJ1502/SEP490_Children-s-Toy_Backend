namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Outbox notification to publish after webhook transaction commits.
/// </summary>
public sealed record PendingShippingNotification(string EventType, object Payload);

/// <summary>
/// Result of processing a GHN return/fail webhook action.
/// </summary>
public sealed class ShippingReturnFlowResult
{
    public bool SkippedDuplicate { get; init; }
    public bool ReleaseShiftCapacity { get; init; }
    public List<PendingShippingNotification> Notifications { get; init; } = [];
}
