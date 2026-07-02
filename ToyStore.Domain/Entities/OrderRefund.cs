using System;
using System.Collections.Generic;
using ToyStore.Domain.Constants;

namespace ToyStore.Domain.Entities;

public partial class OrderRefund
{
    public int RefundId { get; set; }

    public int OrderId { get; set; }

    public byte? RefundReasonId { get; set; }

    public int CustomerId { get; set; }

    public int? RequestedBy { get; set; }

    public int? ApprovedBy { get; set; }

    public int? WalletTransactionId { get; set; }

    public string? ReasonDetails { get; set; }

    /// <summary>
    /// Nguồn tạo refund: "Customer" (khách tự tạo) hoặc "System" (hệ thống tạo khi GHN returned).
    /// System refund không sử dụng GHN pickup — chỉ Approve → Complete.
    /// </summary>
    public string RefundSource { get; set; } = RefundSources.Customer;

    /// <summary>
    /// Loại hình hoàn tiền: "ReturnAndRefund" hoặc "RefundOnly".
    /// </summary>
    public string RefundType { get; set; } = RefundTypes.ReturnAndRefund;

    public decimal ApprovedAmount { get; set; }

    public string RefundCode { get; set; } = null!;

    public string? ShippingOrderCode { get; set; }

    public string? ReturnShippingOrderCode { get; set; }

    public string? InspectionNote { get; set; }

    public bool? InspectionPassed { get; set; }

    public decimal ShippingFee { get; set; }

    public decimal? SubTotal { get; set; }

    public decimal? TotalAmount { get; set; }

    public string? AdminNote { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime? RejectedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public bool IsDeleted { get; set; }

    public byte StatusId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Phí vận chuyển chiều hoàn trả (customer → shop).
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
    /// Merchandise đề xuất → Staff/Admin xác nhận.
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
    /// true = hoàn phí ship → FinalRefundAmount = TotalAmount.
    /// false = không hoàn ship → FinalRefundAmount = TotalAmount - CustomerShippingPaid.
    /// </summary>
    public bool? IncludeShippingInRefund { get; set; }

    public decimal ItemApprovedSubTotal { get; set; } = 0m;
    public decimal ItemRejectedSubTotal { get; set; } = 0m;
    public decimal ReturnToCustomerFee { get; set; } = 0m;
    public DateTime? CustomerResponseDeadline { get; set; }
    public string? CustomerResponse { get; set; }
    public bool ReturnToCustomerFeePaid { get; set; } = false;

    public virtual Account? ApprovedByNavigation { get; set; }

    public virtual Account Customer { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;

    public virtual OrderRefundReason? RefundReason { get; set; }

    public virtual Account? RequestedByNavigation { get; set; }

    public virtual WalletTransaction? WalletTransaction { get; set; }

    public virtual StatusRefund Status { get; set; } = null!;

    public virtual ICollection<RefundImage> RefundImages { get; set; } = new List<RefundImage>();

    public virtual ICollection<RefundDetail> RefundDetails { get; set; } = new List<RefundDetail>();

    public virtual ICollection<RefundStatusHistory> RefundStatusHistories { get; set; } = new List<RefundStatusHistory>();
}
