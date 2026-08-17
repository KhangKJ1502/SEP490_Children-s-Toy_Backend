using System.Collections.Generic;
using ToyStore.Domain.Constants;
using ToyStore.Domain.Enums;

namespace ToyStore.Application.Services;

public static class RefundStatusTransitionValidator
{
    private static readonly HashSet<byte> FinalStatusIds =
    [
        (byte)RefundStatusEnum.RefundCompleted,
        (byte)RefundStatusEnum.RefundCancelled,
        (byte)RefundStatusEnum.RefundReturnedToCustomer,
        (byte)RefundStatusEnum.RefundReturnToCustomerFailed
    ];

    public static bool IsFinal(byte statusId) => FinalStatusIds.Contains(statusId);

    public static bool CanTransition(
        byte currentStatusId,
        byte newStatusId,
        string refundType,
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

        // 1. Luồng Chỉ hoàn tiền (RefundOnly)
        if (refundType == RefundTypes.RefundOnly)
        {
            return newStatusId switch
            {
                (byte)RefundStatusEnum.RefundApproved => currentStatusId == (byte)RefundStatusEnum.RefundRequested
                    || (currentStatusId == (byte)RefundStatusEnum.RefundRejected && isAdmin),
                (byte)RefundStatusEnum.RefundRejected => currentStatusId == (byte)RefundStatusEnum.RefundRequested
                    || currentStatusId == (byte)RefundStatusEnum.RefundApproved,
                (byte)RefundStatusEnum.RefundCompleted => currentStatusId == (byte)RefundStatusEnum.RefundApproved,
                (byte)RefundStatusEnum.RefundCancelled => isAdmin,
                _ => false
            };
        }

        // 2. System return (GHN delivery failure — customer did not receive)
        // Default: Received (auto) → InspectionPending (Merch) → Completed (Staff)
        // Legacy in-flight: Requested/Approved may still transition forward without Staff Approve.
        if (isSystemReturnRefund)
        {
            return (currentStatusId, newStatusId) switch
            {
                ((byte)RefundStatusEnum.RefundRequested, (byte)RefundStatusEnum.RefundApproved) => true,
                ((byte)RefundStatusEnum.RefundRequested, (byte)RefundStatusEnum.RefundReceived) => true,
                ((byte)RefundStatusEnum.RefundRequested, (byte)RefundStatusEnum.RefundInspectionPending) => true,
                ((byte)RefundStatusEnum.RefundApproved, (byte)RefundStatusEnum.RefundReceived) => true,
                ((byte)RefundStatusEnum.RefundApproved, (byte)RefundStatusEnum.RefundInspectionPending) => true,
                ((byte)RefundStatusEnum.RefundReceived, (byte)RefundStatusEnum.RefundInspectionPending) => true,
                ((byte)RefundStatusEnum.RefundInspectionPending, (byte)RefundStatusEnum.RefundCompleted) => true,
                ((byte)RefundStatusEnum.RefundRejected, (byte)RefundStatusEnum.RefundApproved) => isAdmin,
                ((byte)RefundStatusEnum.RefundDamage, (byte)RefundStatusEnum.RefundCompleted) => true,
                _ => false
            };
        }

        // 3. Luồng Trả hàng - Hoàn tiền (ReturnAndRefund)
        return newStatusId switch
        {
            (byte)RefundStatusEnum.RefundApproved => currentStatusId == (byte)RefundStatusEnum.RefundRequested
                || (currentStatusId == (byte)RefundStatusEnum.RefundRejected && isAdmin),
            // Cho phép từ chối ở bất kỳ giai đoạn nào (kể cả sau khi đã nhận hàng / kiểm kho).
            // Khi reject toàn bộ = đóng yêu cầu, giữ hàng tại kho, KHÔNG tạo đơn GHN gửi trả.
            // RefundReturnShipmentCreated chỉ dùng khi một phần hàng bị từ chối và cần ship trả cho khách.
            (byte)RefundStatusEnum.RefundRejected =>
                currentStatusId != (byte)RefundStatusEnum.RefundRejected,
            (byte)RefundStatusEnum.RefundPickupCreated => currentStatusId == (byte)RefundStatusEnum.RefundApproved,
            (byte)RefundStatusEnum.RefundShipping => currentStatusId == (byte)RefundStatusEnum.RefundPickupCreated,
            (byte)RefundStatusEnum.RefundReceived => currentStatusId == (byte)RefundStatusEnum.RefundShipping,
            (byte)RefundStatusEnum.RefundInspectionPending => currentStatusId == (byte)RefundStatusEnum.RefundReceived,
            (byte)RefundStatusEnum.RefundCompleted => currentStatusId == (byte)RefundStatusEnum.RefundInspectionPending
                || currentStatusId == (byte)RefundStatusEnum.RefundApproved
                || currentStatusId == (byte)RefundStatusEnum.RefundDamage,
            (byte)RefundStatusEnum.RefundCancelled => isAdmin,

            // Trạng thái vận chuyển trả ngược về cho khách
            // Bug Fix #3: Xóa RefundReturnToCustomerFailed khỏi source cho phép
            // vì đây là trạng thái Final — không cho phép tạo lại vận đơn giao lại.
            (byte)RefundStatusEnum.RefundReturnShipmentCreated => currentStatusId == (byte)RefundStatusEnum.RefundInspectionPending
                || currentStatusId == (byte)RefundStatusEnum.RefundReceived,
            (byte)RefundStatusEnum.RefundReturningToCustomer => currentStatusId == (byte)RefundStatusEnum.RefundReturnShipmentCreated,
            (byte)RefundStatusEnum.RefundReturnedToCustomer => currentStatusId == (byte)RefundStatusEnum.RefundReturningToCustomer,
            (byte)RefundStatusEnum.RefundReturnToCustomerFailed => currentStatusId == (byte)RefundStatusEnum.RefundReturningToCustomer,

            _ => false
        };
    }

    public static string? GetTransitionError(byte currentStatusId, byte newStatusId, string refundType, bool isSystemReturnRefund, bool isAdmin)
    {
        return CanTransition(currentStatusId, newStatusId, refundType, isSystemReturnRefund, isAdmin)
            ? null
            : $"Cannot transition refund from {((RefundStatusEnum)currentStatusId)} to {((RefundStatusEnum)newStatusId)} (Type: {refundType}).";
    }
}
