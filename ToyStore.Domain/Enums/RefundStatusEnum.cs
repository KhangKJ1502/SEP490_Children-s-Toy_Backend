namespace ToyStore.Domain.Enums;

/// <summary>
/// Enum đại diện cho các mã định danh số (byte ID) tương ứng với từng trạng thái hoàn tiền trong cơ sở dữ liệu.
/// </summary>
public enum RefundStatusEnum : byte
{
    /// <summary>1 - Đã gửi yêu cầu hoàn tiền.</summary>
    RefundRequested = 1,

    /// <summary>2 - Yêu cầu hoàn tiền đã được phê duyệt.</summary>
    RefundApproved = 2,

    /// <summary>3 - Yêu cầu hoàn tiền bị từ chối.</summary>
    RefundRejected = 3,

    /// <summary>4 - Đã tạo vận đơn lấy hàng trả về.</summary>
    RefundPickupCreated = 4,

    /// <summary>5 - Hàng đang trên đường chuyển về kho.</summary>
    RefundShipping = 5,

    /// <summary>6 - Kho đã nhận được hàng trả về.</summary>
    RefundReceived = 6,

    /// <summary>7 - Đang chờ/đã có kết quả kiểm tra chất lượng hàng hoàn.</summary>
    RefundInspectionPending = 7,

    /// <summary>8 - Quy trình hoàn tiền đã hoàn tất thành công.</summary>
    RefundCompleted = 8,

    /// <summary>9 - Yêu cầu hoàn tiền đã bị hủy.</summary>
    RefundCancelled = 9,

    /// <summary>10 - Hàng hóa bị hư hỏng.</summary>
    RefundDamage = 10,

    /// <summary>11 - Đã tạo vận đơn chuyển trả lại hàng cho khách.</summary>
    RefundReturnShipmentCreated = 11,

    /// <summary>12 - Đang chuyển trả lại hàng cho khách.</summary>
    RefundReturningToCustomer = 12,

    /// <summary>13 - Đã chuyển trả lại hàng thành công cho khách.</summary>
    RefundReturnedToCustomer = 13,

    /// <summary>14 - Chuyển trả lại hàng cho khách thất bại.</summary>
    RefundReturnToCustomerFailed = 14
}
