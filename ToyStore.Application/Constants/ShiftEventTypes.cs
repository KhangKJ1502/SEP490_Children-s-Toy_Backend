namespace ToyStore.Application.Constants;

public static class ShiftEventTypes
{
    public const string OrderAssigned = "order.assigned.auto";
    public const string OrderQueued = "order.queued";
    public const string CapacityFreed = "order.capacity_freed";
    public const string ShiftStarted = "shift.started";
    public const string ShiftEndedWithPendingOrders = "shift.ended_with_pending";

    public const string ShiftFull = "shift.full";
}
