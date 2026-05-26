namespace ToyStore.Domain.Enums;

public enum RefundStatusEnum : byte
{
    RefundRequested = 1,
    RefundApproved = 2,
    RefundRejected = 3,
    RefundPickupCreated = 4,
    RefundShipping = 5,
    RefundReceived = 6,
    RefundInspectionPending = 7,
    RefundCompleted = 8,
    RefundCancelled = 9
}
