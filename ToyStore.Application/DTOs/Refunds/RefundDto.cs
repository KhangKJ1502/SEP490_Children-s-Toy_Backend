using System;
using System.Collections.Generic;

namespace ToyStore.Application.DTOs.Refunds;

/// <summary>
/// Data Transfer Object (DTO) chứa thông tin chi tiết đầy đủ của một yêu cầu Hoàn tiền / Đổi trả.
/// Bao gồm: thông tin đơn hàng gốc, khách hàng, lý do hoàn, số tiền phê duyệt, phí ship hoàn trả,
/// danh sách sản phẩm, hình ảnh bằng chứng, lịch sử chuyển trạng thái và lịch sử vận đơn GHN.
/// </summary>
public class RefundDto
{
    /// <summary>
    /// Mã ID của yêu cầu hoàn tiền.
    /// </summary>
    public int RefundId { get; set; }

    /// <summary>
    /// Mã ID đơn hàng gốc.
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Mã code hiển thị của đơn hàng gốc (ví dụ: "ORD-20260301-1234").
    /// </summary>
    public string OrderCode { get; set; } = null!;

    /// <summary>
    /// Trạng thái đơn hàng gốc tại thời điểm hiện tại.
    /// </summary>
    public string OrderStatus { get; set; } = null!;

    /// <summary>
    /// Trạng thái thanh toán của đơn hàng gốc.
    /// </summary>
    public string PaymentStatus { get; set; } = null!;

    /// <summary>
    /// Mã ID lý do hoàn tiền.
    /// </summary>
    public byte? RefundReasonId { get; set; }

    /// <summary>
    /// Nội dung mô tả lý do hoàn tiền.
    /// </summary>
    public string? RefundReasonContent { get; set; }

    /// <summary>
    /// Bên chịu phí vận chuyển hoàn trả mặc định theo lý do hoàn: "Store" (Cửa hàng) hoặc "Customer" (Khách hàng).
    /// </summary>
    public string? RefundReasonResponsibleParty { get; set; }

    /// <summary>
    /// Mã ID tài khoản khách hàng tạo yêu cầu.
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Họ tên khách hàng.
    /// </summary>
    public string CustomerName { get; set; } = null!;

    /// <summary>
    /// Số điện thoại liên hệ của khách hàng.
    /// </summary>
    public string CustomerPhone { get; set; } = null!;

    /// <summary>
    /// Email liên hệ của khách hàng.
    /// </summary>
    public string CustomerEmail { get; set; } = null!;

    /// <summary>
    /// Tên người gửi yêu cầu (khách hàng hoặc hệ thống).
    /// </summary>
    public string? RequestedByName { get; set; }

    /// <summary>
    /// Mã ID người gửi yêu cầu.
    /// </summary>
    public int? RequestedBy { get; set; }

    /// <summary>
    /// Mã ID nhân viên/quản trị viên phê duyệt yêu cầu.
    /// </summary>
    public int? ApprovedBy { get; set; }

    /// <summary>
    /// Chi tiết lý do hoàn tiền từ khách hàng.
    /// </summary>
    public string? ReasonDetails { get; set; }

    /// <summary>
    /// Nguồn gốc phát sinh yêu cầu hoàn tiền ("Customer": Khách hàng gửi, "System": Hệ thống tự tạo do giao hàng thất bại).
    /// </summary>
    public string RefundSource { get; set; } = "Customer";

    /// <summary>
    /// Loại yêu cầu hoàn tiền ("ReturnAndRefund" hoặc "RefundOnly").
    /// </summary>
    public string RefundType { get; set; } = "ReturnAndRefund";

    /// <summary>
    /// Đánh dấu xem đây có phải là hoàn tiền tự động do lỗi giao hàng trả hàng hệ thống hay không.
    /// </summary>
    public bool IsSystemReturn => string.Equals(RefundSource, "System", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Số tiền hoàn trả được phê duyệt ban đầu (tổng tiền các sản phẩm được duyệt).
    /// </summary>
    public decimal ApprovedAmount { get; set; }

    /// <summary>
    /// Trạng thái xử lý của yêu cầu hoàn tiền (ví dụ: "Pending", "Approved", "Returning", "Received", "Refunded", "Rejected", "Cancelled").
    /// </summary>
    public string RefundStatus { get; set; } = null!;

    /// <summary>
    /// Thời gian tạo yêu cầu hoàn tiền (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Thời gian cập nhật trạng thái gần nhất (UTC).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Mã định danh yêu cầu hoàn tiền (Refund Code).
    /// </summary>
    public string? RefundCode { get; set; }

    /// <summary>
    /// Mã vận đơn chuyển hàng ban đầu từ đơn vị vận chuyển GHN.
    /// </summary>
    public string? ShippingOrderCode { get; set; }

    /// <summary>
    /// Mã vận đơn trả hàng từ khách hàng về kho cửa hàng.
    /// </summary>
    public string? ReturnShippingOrderCode { get; set; }

    /// <summary>
    /// Ghi chú kiểm tra hàng hoàn từ bộ phận kho/Merchandise.
    /// </summary>
    public string? InspectionNote { get; set; }

    /// <summary>
    /// Kết quả kiểm tra chất lượng hàng hoàn (true: Đạt yêu cầu, false: Không đạt).
    /// </summary>
    public bool? InspectionPassed { get; set; }

    /// <summary>
    /// Phí vận chuyển ban đầu của đơn hàng gốc.
    /// </summary>
    public decimal ShippingFee { get; set; }

    /// <summary>
    /// Tạm tính tiền hàng đơn hàng gốc trước giảm giá.
    /// </summary>
    public decimal? SubTotal { get; set; }

    /// <summary>
    /// Tổng giá trị thanh toán của đơn hàng gốc.
    /// </summary>
    public decimal? TotalAmount { get; set; }

    /// <summary>
    /// Ghi chú nội bộ của Quản trị viên/Nhân viên.
    /// </summary>
    public string? AdminNote { get; set; }

    /// <summary>
    /// Tên nhân viên CSKH/Staff được phân công xử lý yêu cầu.
    /// </summary>
    public string? AssignedToStaffName { get; set; }

    /// <summary>
    /// Tên nhân viên Kho/Merchandise được phân công nhận và kiểm tra hàng hoàn.
    /// </summary>
    public string? AssignedToMerchName { get; set; }

    /// <summary>
    /// Địa chỉ nhận hàng hoàn của khách hàng.
    /// </summary>
    public string? CustomerAddress { get; set; }

    /// <summary>
    /// Phí vận chuyển hoàn trả từ khách hàng về cửa hàng. (0 nếu Store chịu phí hoặc loại RefundOnly).
    /// </summary>
    public decimal ReturnShippingFee { get; set; }

    /// <summary>
    /// Bên chịu trách nhiệm thanh toán phí vận chuyển hoàn trả ("Store" hoặc "Customer").
    /// </summary>
    public string ReturnShippingFeeBy { get; set; } = "Store";

    /// <summary>
    /// Ghi chú lý do thay đổi bên chịu phí vận chuyển hoàn trả (lưu vết audit trail).
    /// </summary>
    public string? ReturnShippingFeeNote { get; set; }

    /// <summary>
    /// Số tiền thực tế được hoàn vào Ví (Wallet) của khách hàng:
    /// = ApprovedAmount nếu Store chịu phí.
    /// = Max(0, ApprovedAmount - ReturnShippingFee) nếu Customer chịu phí.
    /// </summary>
    public decimal FinalRefundAmount { get; set; }

    /// <summary>
    /// Bên chịu trách nhiệm khi hàng hóa bị hư hỏng: null / "Customer" / "Carrier".
    /// </summary>
    public string? DamageResponsibility { get; set; }

    /// <summary>
    /// [System Return] Tiền phí vận chuyển khách hàng đã thực trả trong đơn hàng gốc.
    /// </summary>
    public decimal CustomerShippingPaid { get; set; }

    /// <summary>
    /// [System Return] Nhân viên có chọn hoàn lại cả tiền phí vận chuyển cho khách hay không. (null nếu chưa hoàn tất).
    /// </summary>
    public bool? IncludeShippingInRefund { get; set; }

    /// <summary>
    /// Tổng tiền các sản phẩm được chấp thuận hoàn trả sau kiểm tra.
    /// </summary>
    public decimal ItemApprovedSubTotal { get; set; }

    /// <summary>
    /// Tổng tiền các sản phẩm bị từ chối hoàn trả sau kiểm tra.
    /// </summary>
    public decimal ItemRejectedSubTotal { get; set; }

    /// <summary>
    /// Phí gửi trả hàng ngược lại cho khách (trong trường hợp từ chối hoàn hoặc hàng lỗi do khách).
    /// </summary>
    public decimal ReturnToCustomerFee { get; set; }

    /// <summary>
    /// Hạn chót khách hàng phải phản hồi phương án xử lý hàng bị từ chối (UTC).
    /// </summary>
    public DateTime? CustomerResponseDeadline { get; set; }

    /// <summary>
    /// Phương án phản hồi từ khách hàng ("RECEIVE_BACK": Nhận lại hàng, hoặc "DISCARD": Hủy bỏ hàng).
    /// </summary>
    public string? CustomerResponse { get; set; }

    /// <summary>
    /// Đánh dấu khách hàng đã thanh toán phí gửi trả hàng lại hay chưa.
    /// </summary>
    public bool ReturnToCustomerFeePaid { get; set; }

    /// <summary>
    /// URL ảnh chụp bằng chứng giao nhận hàng hoàn về kho.
    /// </summary>
    public string? ReturnDeliveryImageUrl { get; set; }

    /// <summary>
    /// URL ảnh chụp bằng chứng gửi trả lại hàng cho khách hàng.
    /// </summary>
    public string? ReturnToCustomerImageUrl { get; set; }

    /// <summary>
    /// Giá trị giảm giá từ Voucher trong đơn hàng gốc (chỉ hiển thị thông tin, voucher không được khôi phục).
    /// </summary>
    public decimal VoucherDiscountAmount { get; set; }

    /// <summary>
    /// Danh sách URL các hình ảnh bằng chứng hoàn tiền do khách hàng cung cấp.
    /// </summary>
    public List<string> Images { get; set; } = new List<string>();

    /// <summary>
    /// Danh sách chi tiết các mặt hàng trong yêu cầu hoàn tiền.
    /// </summary>
    public List<RefundDetailDto> Details { get; set; } = new List<RefundDetailDto>();

    /// <summary>
    /// Lịch sử thay đổi trạng thái của yêu cầu hoàn tiền.
    /// </summary>
    public List<RefundStatusHistoryDto> StatusHistory { get; set; } = new List<RefundStatusHistoryDto>();

    /// <summary>
    /// Lịch sử hành trình giao vận GHN liên quan.
    /// </summary>
    public List<ToyStore.Application.DTOs.Orders.AdminShippingStatusHistoryDto> ShippingHistory { get; set; } = new List<ToyStore.Application.DTOs.Orders.AdminShippingStatusHistoryDto>();
}

/// <summary>
/// Data Transfer Object (DTO) thể hiện chi tiết một mặt hàng trong yêu cầu hoàn tiền.
/// </summary>
public class RefundDetailDto
{
    /// <summary>
    /// Mã ID sản phẩm.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Tên sản phẩm.
    /// </summary>
    public string ProductName { get; set; } = null!;

    /// <summary>
    /// URL hình ảnh đại diện của sản phẩm.
    /// </summary>
    public string? ProductImage { get; set; }

    /// <summary>
    /// Số lượng sản phẩm yêu cầu hoàn tiền.
    /// </summary>
    public short Quantity { get; set; }

    /// <summary>
    /// Đơn giá sản phẩm tại thời điểm mua (sau khi phân bổ chiết khấu).
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Số tiền hoàn tương ứng cho mặt hàng này (= Quantity * UnitPrice).
    /// </summary>
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// [System Return] Số lượng sản phẩm Merchandise xác nhận có thể nhập kho lại (null nếu chưa kiểm tra).
    /// </summary>
    public short? RestorableQuantity { get; set; }

    /// <summary>
    /// Số lượng sản phẩm hỏng do lỗi từ phía khách hàng (không được hoàn tiền).
    /// </summary>
    public short FailedCustomerQty { get; set; }

    /// <summary>
    /// Số lượng sản phẩm hỏng do lỗi đơn vị vận chuyển (được hoàn tiền).
    /// </summary>
    public short FailedCarrierQty { get; set; }
}

/// <summary>
/// Data Transfer Object (DTO) ghi nhận thông tin lịch sử thay đổi trạng thái của yêu cầu hoàn tiền.
/// </summary>
public class RefundStatusHistoryDto
{
    /// <summary>
    /// Tên trạng thái hoàn tiền (ví dụ: "Pending", "Approved", "Returned", "Refunded",...).
    /// </summary>
    public string StatusName { get; set; } = null!;

    /// <summary>
    /// Tên người thực hiện thay đổi trạng thái (Nhân viên, Khách hàng, Hệ thống).
    /// </summary>
    public string? ChangedByName { get; set; }

    /// <summary>
    /// Ghi chú kèm theo khi chuyển trạng thái.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Thời điểm chuyển trạng thái (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
