using ToyStore.Domain.Constants;
using ToyStore.Domain.Enums;

namespace ToyStore.Application.Services;

public static class RefundStatusTransitionValidator
{
    private static readonly HashSet<byte> FinalStatusIds =
    [
        (byte)RefundStatusEnum.RefundCompleted,
        (byte)RefundStatusEnum.RefundCancelled
    ];

    public static bool IsFinal(byte statusId) => FinalStatusIds.Contains(statusId);

    public static bool CanTransition(
        byte currentStatusId,
        byte newStatusId,
        bool isSystemReturnRefund,
        bool isAdmin)
    {
        if (currentStatusId == newStatusId)
            return false;

        if (IsFinal(currentStatusId))
            return false;

        if (isSystemReturnRefund && newStatusId == (byte)RefundStatusEnum.RefundRejected)
            return false;

        if (newStatusId == (byte)RefundStatusEnum.RefundRejected
            && currentStatusId == (byte)RefundStatusEnum.RefundCompleted)
            return false;

        if (newStatusId == (byte)RefundStatusEnum.RefundApproved
            && currentStatusId == (byte)RefundStatusEnum.RefundRejected)
            return isAdmin;

        if (isSystemReturnRefund)
        {
            return (currentStatusId, newStatusId) switch
            {
                ((byte)RefundStatusEnum.RefundRequested, (byte)RefundStatusEnum.RefundApproved) => true,
                ((byte)RefundStatusEnum.RefundApproved, (byte)RefundStatusEnum.RefundCompleted) => true,
                ((byte)RefundStatusEnum.RefundRejected, (byte)RefundStatusEnum.RefundApproved) => isAdmin,
                _ => false
            };
        }

        return newStatusId switch
        {
            (byte)RefundStatusEnum.RefundApproved => currentStatusId == (byte)RefundStatusEnum.RefundRequested
                || (currentStatusId == (byte)RefundStatusEnum.RefundRejected && isAdmin),
            (byte)RefundStatusEnum.RefundRejected => currentStatusId != (byte)RefundStatusEnum.RefundRejected,
            (byte)RefundStatusEnum.RefundPickupCreated => currentStatusId == (byte)RefundStatusEnum.RefundApproved,
            (byte)RefundStatusEnum.RefundShipping => currentStatusId == (byte)RefundStatusEnum.RefundPickupCreated,
            (byte)RefundStatusEnum.RefundReceived => currentStatusId == (byte)RefundStatusEnum.RefundShipping,
            (byte)RefundStatusEnum.RefundInspectionPending => currentStatusId == (byte)RefundStatusEnum.RefundReceived,
            (byte)RefundStatusEnum.RefundCompleted => currentStatusId == (byte)RefundStatusEnum.RefundInspectionPending
                || currentStatusId == (byte)RefundStatusEnum.RefundApproved,
            (byte)RefundStatusEnum.RefundCancelled => isAdmin,
            _ => false
        };
    }

    public static string? GetTransitionError(byte currentStatusId, byte newStatusId, bool isSystemReturnRefund, bool isAdmin)
    {
        return CanTransition(currentStatusId, newStatusId, isSystemReturnRefund, isAdmin)
            ? null
            : $"Cannot transition refund from {((RefundStatusEnum)currentStatusId)} to {((RefundStatusEnum)newStatusId)}.";
    }
}
