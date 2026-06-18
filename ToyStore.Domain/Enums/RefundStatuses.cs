namespace ToyStore.Domain.Enums;

public static class RefundStatuses
{
    public const string Requested = "RefundRequested";
    public const string Approved = "RefundApproved";
    public const string Rejected = "RefundRejected";
    public const string PickupCreated = "RefundPickupCreated";
    public const string Shipping = "RefundShipping";
    public const string Received = "RefundReceived";
    public const string InspectionPending = "RefundInspectionPending";
    public const string Completed = "RefundCompleted";
    public const string Cancelled = "RefundCancelled";
    public const string Damage = "RefundDamage";
}
