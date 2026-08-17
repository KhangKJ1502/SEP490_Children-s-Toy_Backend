namespace ToyStore.Domain.Enums;

/// <summary>
/// Danh sách các hằng số chuỗi đại diện cho tên trạng thái trong quy trình xử lý Hoàn tiền / Đổi trả.
/// </summary>
public static class RefundStatuses
{
    /// <summary>Khách hàng vừa gửi yêu cầu hoàn tiền, chờ nhân viên CSKH tiếp nhận.</summary>
    public const string Requested = "RefundRequested";

    /// <summary>Yêu cầu hoàn tiền đã được nhân viên/quản trị viên phê duyệt.</summary>
    public const string Approved = "RefundApproved";

    /// <summary>Yêu cầu hoàn tiền bị từ chối.</summary>
    public const string Rejected = "RefundRejected";

    /// <summary>Đã tạo mã vận đơn lấy hàng trả về từ khách hàng qua GHN.</summary>
    public const string PickupCreated = "RefundPickupCreated";

    /// <summary>Đơn vị vận chuyển đang vận chuyển hàng hoàn trả về kho cửa hàng.</summary>
    public const string Shipping = "RefundShipping";

    /// <summary>Bộ phận kho đã nhận được kiện hàng hoàn trả từ shipper.</summary>
    public const string Received = "RefundReceived";

    /// <summary>Hàng hoàn đang chờ kiểm tra chất lượng hoặc đã có kết quả kiểm tra sơ bộ từ kho.</summary>
    public const string InspectionPending = "RefundInspectionPending";

    /// <summary>Quy trình hoàn tiền đã hoàn tất (đã cộng tiền vào ví và cập nhật kho hàng thành công).</summary>
    public const string Completed = "RefundCompleted";

    /// <summary>Yêu cầu hoàn tiền đã bị hủy (bởi khách hàng hoặc do quá hạn thanh toán).</summary>
    public const string Cancelled = "RefundCancelled";

    /// <summary>Hàng hóa hoàn trả bị hư hỏng trong quá trình vận chuyển.</summary>
    public const string Damage = "RefundDamage";

    /// <summary>Đã tạo vận đơn gửi trả lại hàng hóa cho khách hàng (khi từ chối hoàn).</summary>
    public const string ReturnShipmentCreated = "RefundReturnShipmentCreated";

    /// <summary>Đang giao lại hàng hóa trả về cho khách hàng.</summary>
    public const string ReturningToCustomer = "RefundReturningToCustomer";

    /// <summary>Đã giao trả lại hàng thành công cho khách hàng.</summary>
    public const string ReturnedToCustomer = "RefundReturnedToCustomer";

    /// <summary>Giao trả lại hàng cho khách hàng thất bại.</summary>
    public const string ReturnToCustomerFailed = "RefundReturnToCustomerFailed";
}
