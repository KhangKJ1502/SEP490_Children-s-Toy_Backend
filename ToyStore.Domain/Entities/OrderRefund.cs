using System;
using System.Collections.Generic;
using ToyStore.Domain.Constants;

namespace ToyStore.Domain.Entities;

/// <summary>
/// Thực thể đại diện cho một Yêu cầu Hoàn tiền / Trả hàng (OrderRefund) trong cơ sở dữ liệu.
/// Quản lý toàn bộ vòng đời xử lý hoàn tiền từ khi yêu cầu được tạo, phê duyệt, điều phối vận đơn thu hồi hàng,
/// kiểm tra chất lượng sản phẩm nhập kho cho đến khi hoàn tiền vào ví khách hàng.
/// </summary>
public partial class OrderRefund
{
    /// <summary>
    /// Khóa chính (Primary Key) của yêu cầu hoàn tiền.
    /// </summary>
    public int RefundId { get; set; }

    /// <summary>
    /// Khóa ngoại tham chiếu đến đơn hàng gốc (Order).
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Khóa ngoại tham chiếu đến lý do hoàn tiền (OrderRefundReason).
    /// </summary>
    public byte? RefundReasonId { get; set; }

    /// <summary>
    /// Khóa ngoại tham chiếu đến tài khoản khách hàng (Account).
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Khóa ngoại tham chiếu đến người gửi yêu cầu (Account).
    /// </summary>
    public int? RequestedBy { get; set; }

    /// <summary>
    /// Khóa ngoại tham chiếu đến nhân viên/quản trị viên phê duyệt yêu cầu (Account).
    /// </summary>
    public int? ApprovedBy { get; set; }

    /// <summary>
    /// Khóa ngoại tham chiếu đến giao dịch cộng tiền vào ví khách hàng (WalletTransaction).
    /// </summary>
    public int? WalletTransactionId { get; set; }

    /// <summary>
    /// Mô tả chi tiết lý do hoàn tiền từ khách hàng.
    /// </summary>
    public string? ReasonDetails { get; set; }

    /// <summary>
    /// Nguồn tạo refund: "Customer" (khách tự tạo) hoặc "System" (hệ thống tạo khi GHN trả hàng thất bại).
    /// System refund không sử dụng quy trình GHN pickup thu hồi mà đi thẳng sang kiểm tra hàng về kho.
    /// </summary>
    public string RefundSource { get; set; } = RefundSources.Customer;

    /// <summary>
    /// Loại hình hoàn tiền: "ReturnAndRefund" (Trả hàng & Hoàn tiền) hoặc "RefundOnly" (Chỉ hoàn tiền không trả hàng).
    /// </summary>
    public string RefundType { get; set; } = RefundTypes.ReturnAndRefund;

    /// <summary>
    /// Số tiền phê duyệt hoàn ban đầu (tổng tiền các sản phẩm được duyệt hoàn).
    /// </summary>
    public decimal ApprovedAmount { get; set; }

    /// <summary>
    /// Mã định danh yêu cầu hoàn tiền (Refund Code).
    /// </summary>
    public string RefundCode { get; set; } = null!;

    /// <summary>
    /// Mã vận đơn chuyển hàng ban đầu từ đơn vị vận chuyển GHN.
    /// </summary>
    public string? ShippingOrderCode { get; set; }

    /// <summary>
    /// Mã vận đơn trả hàng từ khách về kho cửa hàng.
    /// </summary>
    public string? ReturnShippingOrderCode { get; set; }

    /// <summary>
    /// Ghi chú kiểm tra chất lượng hàng hoàn từ bộ phận Merchandise/Kho.
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
    /// Tiền hàng ban đầu của đơn hàng gốc trước giảm giá.
    /// </summary>
    public decimal? SubTotal { get; set; }

    /// <summary>
    /// Tổng số tiền thanh toán thực tế của đơn hàng gốc.
    /// </summary>
    public decimal? TotalAmount { get; set; }

    /// <summary>
    /// Ghi chú nội bộ của Quản trị viên / Nhân viên.
    /// </summary>
    public string? AdminNote { get; set; }

    /// <summary>
    /// Thời điểm yêu cầu được phê duyệt (UTC).
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Thời điểm yêu cầu bị từ chối (UTC).
    /// </summary>
    public DateTime? RejectedAt { get; set; }

    /// <summary>
    /// Thời điểm yêu cầu hoàn tiền hoàn tất thành công (UTC).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Thời điểm yêu cầu bị hủy (UTC).
    /// </summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Cờ đánh dấu xóa mềm bản ghi.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Khóa ngoại tham chiếu đến trạng thái hoàn tiền (StatusRefund).
    /// </summary>
    public byte StatusId { get; set; }

    /// <summary>
    /// Thời điểm tạo bản ghi (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Thời điểm cập nhật bản ghi gần nhất (UTC).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Phí vận chuyển chiều hoàn trả (customer -> shop).
    /// Được cập nhật 2 lần: ước tính từ GHN GetFeeAsync lúc Approve,
    /// sau đó ghi đè bằng TotalFee thực tế từ CreateOrderAsync lúc PickupCreated.
    /// = 0 nếu ReturnShippingFeeBy = "Store" hoặc RefundType = "RefundOnly".
    /// </summary>
    public decimal ReturnShippingFee { get; set; } = 0m;

    /// <summary>
    /// Bên chịu phí vận chuyển hoàn trả: "Store" hoặc "Customer".
    /// Được xác định lúc Approve (dựa trên RefundReason.ResponsibleParty), Admin/Staff có thể override.
    /// </summary>
    public string ReturnShippingFeeBy { get; set; } = RefundResponsibleParty.Store;

    /// <summary>
    /// Lý do override ReturnShippingFeeBy (bắt buộc nếu override khác với suggestion từ RefundReason).
    /// Dùng cho audit trail.
    /// </summary>
    public string? ReturnShippingFeeNote { get; set; }

    /// <summary>
    /// Số tiền thực tế sẽ được credit vào ví khách hàng.
    /// = ApprovedAmount nếu Shop chịu phí.
    /// = Max(0, ApprovedAmount - ReturnShippingFee) nếu Customer chịu phí.
    /// ApprovedAmount KHÔNG bị thay đổi — FinalRefundAmount là giá trị tách biệt.
    /// </summary>
    public decimal FinalRefundAmount { get; set; } = 0m;

    /// <summary>
    /// Nguyên nhân hàng hóa bị hư hỏng trong quá trình vận chuyển hoàn trả.
    /// NULL = chưa xác định / không có hư hỏng.
    /// "Customer" = khách gửi hàng đã hỏng sẵn.
    /// "Carrier"  = hàng hỏng trong quá trình GHN vận chuyển.
    /// Merchandise đề xuất -> Staff/Admin xác nhận.
    /// </summary>
    public string? DamageResponsibility { get; set; }

    /// <summary>
    /// [System Return only] Tiền ship mà khách thực trả (sau khi trừ voucher freeship).
    /// = max(0, TotalAmount - sum(RefundDetail.RefundAmount)).
    /// Snapshot lúc tạo system refund, dùng để tính FinalRefundAmount khi Staff Complete.
    /// </summary>
    public decimal CustomerShippingPaid { get; set; } = 0m;

    /// <summary>
    /// [System Return only] Staff chọn có hoàn phí vận chuyển cho khách hay không khi Complete.
    /// NULL = chưa xác định (customer return hoặc chưa Complete).
    /// true = hoàn phí ship -> FinalRefundAmount = TotalAmount.
    /// false = không hoàn ship -> FinalRefundAmount = TotalAmount - CustomerShippingPaid.
    /// </summary>
    public bool? IncludeShippingInRefund { get; set; }

    /// <summary>
    /// Tổng tiền các sản phẩm được chấp thuận hoàn tiền.
    /// </summary>
    public decimal ItemApprovedSubTotal { get; set; } = 0m;

    /// <summary>
    /// Tổng tiền các sản phẩm bị từ chối hoàn tiền.
    /// </summary>
    public decimal ItemRejectedSubTotal { get; set; } = 0m;

    /// <summary>
    /// Phí vận chuyển gửi trả lại hàng cho khách hàng khi hàng bị từ chối hoàn.
    /// </summary>
    public decimal ReturnToCustomerFee { get; set; } = 0m;

    /// <summary>
    /// Hạn chót khách hàng phải phản hồi phương án xử lý hàng bị từ chối (UTC).
    /// </summary>
    public DateTime? CustomerResponseDeadline { get; set; }

    /// <summary>
    /// Phương án phản hồi từ khách hàng ("RECEIVE_BACK" hoặc "DISCARD").
    /// </summary>
    public string? CustomerResponse { get; set; }

    /// <summary>
    /// Đánh dấu khách hàng đã thanh toán phí gửi lại hàng bị từ chối hay chưa.
    /// </summary>
    public bool ReturnToCustomerFeePaid { get; set; } = false;

    /// <summary>
    /// URL hình ảnh bằng chứng giao nhận hàng hoàn về kho.
    /// </summary>
    public string? ReturnDeliveryImageUrl { get; set; }

    /// <summary>
    /// URL hình ảnh bằng chứng gửi trả lại hàng cho khách hàng.
    /// </summary>
    public string? ReturnToCustomerImageUrl { get; set; }

    /// <summary>
    /// Navigation property: Thông tin người phê duyệt.
    /// </summary>
    public virtual Account? ApprovedByNavigation { get; set; }

    /// <summary>
    /// Navigation property: Thông tin khách hàng.
    /// </summary>
    public virtual Account Customer { get; set; } = null!;

    /// <summary>
    /// Navigation property: Thông tin đơn hàng gốc.
    /// </summary>
    public virtual Order Order { get; set; } = null!;

    /// <summary>
    /// Navigation property: Thông tin lý do hoàn tiền.
    /// </summary>
    public virtual OrderRefundReason? RefundReason { get; set; }

    /// <summary>
    /// Navigation property: Thông tin người gửi yêu cầu.
    /// </summary>
    public virtual Account? RequestedByNavigation { get; set; }

    /// <summary>
    /// Navigation property: Giao dịch ví tương ứng.
    /// </summary>
    public virtual WalletTransaction? WalletTransaction { get; set; }

    /// <summary>
    /// Navigation property: Trạng thái hoàn tiền.
    /// </summary>
    public virtual StatusRefund Status { get; set; } = null!;

    /// <summary>
    /// Navigation property: Danh sách hình ảnh bằng chứng hoàn tiền.
    /// </summary>
    public virtual ICollection<RefundImage> RefundImages { get; set; } = new List<RefundImage>();

    /// <summary>
    /// Navigation property: Danh sách chi tiết các mặt hàng hoàn tiền.
    /// </summary>
    public virtual ICollection<RefundDetail> RefundDetails { get; set; } = new List<RefundDetail>();

    /// <summary>
    /// Navigation property: Lịch sử các lần chuyển trạng thái hoàn tiền.
    /// </summary>
    public virtual ICollection<RefundStatusHistory> RefundStatusHistories { get; set; } = new List<RefundStatusHistory>();

    /// <summary>
    /// Navigation property: Danh sách các giao dịch vận chuyển phục vụ quy trình hoàn tiền.
    /// </summary>
    public virtual ICollection<ShippingProviderTransaction> ShippingProviderTransactions { get; set; } = new List<ShippingProviderTransaction>();
}
