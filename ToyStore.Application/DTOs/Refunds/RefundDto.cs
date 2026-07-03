using System;
using System.Collections.Generic;

namespace ToyStore.Application.DTOs.Refunds;

public class RefundDto
{
    public int RefundId { get; set; }
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = null!;
    public string OrderStatus { get; set; } = null!;
    public string PaymentStatus { get; set; } = null!;
    public byte? RefundReasonId { get; set; }
    public string? RefundReasonContent { get; set; }
    /// <summary>Bên chịu phí vận chuyển hoàn trả mặc định theo lý do refund: "Store" hoặc "Customer".</summary>
    public string? RefundReasonResponsibleParty { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = null!;
    public string CustomerPhone { get; set; } = null!;
    public string CustomerEmail { get; set; } = null!;
    public string? RequestedByName { get; set; }
    public int? RequestedBy { get; set; }
    public int? ApprovedBy { get; set; }
    public string? ReasonDetails { get; set; }
    public string RefundSource { get; set; } = "Customer";
    public string RefundType { get; set; } = "ReturnAndRefund";
    public bool IsSystemReturn => string.Equals(RefundSource, "System", StringComparison.OrdinalIgnoreCase);
    public decimal ApprovedAmount { get; set; }
    public string RefundStatus { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? RefundCode { get; set; }
    public string? ShippingOrderCode { get; set; }
    public string? ReturnShippingOrderCode { get; set; }
    public string? InspectionNote { get; set; }
    public bool? InspectionPassed { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal? SubTotal { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? AdminNote { get; set; }
    public string? AssignedToStaffName { get; set; }
    public string? AssignedToMerchName { get; set; }
    public string? CustomerAddress { get; set; }

    /// <summary>Phí vận chuyển hoàn trả (customer → shop). 0 nếu Store chịu hoặc RefundOnly.</summary>
    public decimal ReturnShippingFee { get; set; }

    /// <summary>"Store" hoặc "Customer" — bên chịu phí vận chuyển hoàn trả.</summary>
    public string ReturnShippingFeeBy { get; set; } = "Store";

    /// <summary>Lý do override ReturnShippingFeeBy (audit trail).</summary>
    public string? ReturnShippingFeeNote { get; set; }

    /// <summary>
    /// Số tiền thực tế credit vào ví khách. Tách biệt hoàn toàn với ApprovedAmount.
    /// = ApprovedAmount nếu Store chịu phí.
    /// = Max(0, ApprovedAmount - ReturnShippingFee) nếu Customer chịu phí.
    /// </summary>
    public decimal FinalRefundAmount { get; set; }

    /// <summary>Nguyên nhân hàng hỏng: null / "Customer" / "Carrier".</summary>
    public string? DamageResponsibility { get; set; }

    /// <summary>[System Return] Tiền ship khách đã thực trả. Dùng để hiển thị breakdown và tính FinalRefundAmount.</summary>
    public decimal CustomerShippingPaid { get; set; }

    /// <summary>[System Return] Staff đã chọn có hoàn phí ship chưa. null = chưa Complete.</summary>
    public bool? IncludeShippingInRefund { get; set; }

    public decimal ItemApprovedSubTotal { get; set; }
    public decimal ItemRejectedSubTotal { get; set; }
    public decimal ReturnToCustomerFee { get; set; }
    public DateTime? CustomerResponseDeadline { get; set; }
    public string? CustomerResponse { get; set; }
    public bool ReturnToCustomerFeePaid { get; set; }

    /// <summary>Voucher discount amount từ đơn hàng gốc (display only, voucher không được trả lại).</summary>
    public decimal VoucherDiscountAmount { get; set; }

    public List<string> Images { get; set; } = new List<string>();
    public List<RefundDetailDto> Details { get; set; } = new List<RefundDetailDto>();
    public List<RefundStatusHistoryDto> StatusHistory { get; set; } = new List<RefundStatusHistoryDto>();
    public List<ToyStore.Application.DTOs.Orders.AdminShippingStatusHistoryDto> ShippingHistory { get; set; } = new List<ToyStore.Application.DTOs.Orders.AdminShippingStatusHistoryDto>();
}

public class RefundDetailDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string? ProductImage { get; set; }
    public short Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal RefundAmount { get; set; }

    /// <summary>[System Return] Số lượng Merchandise xác nhận nhập kho lại. null = chưa kiểm tra.</summary>
    public short? RestorableQuantity { get; set; }

    /// <summary>Số lượng sản phẩm hỏng do lỗi của khách (không hoàn tiền).</summary>
    public short FailedCustomerQty { get; set; }

    /// <summary>Số lượng sản phẩm hỏng do lỗi vận chuyển (được hoàn tiền).</summary>
    public short FailedCarrierQty { get; set; }
}

public class RefundStatusHistoryDto
{
    public string StatusName { get; set; } = null!;
    public string? ChangedByName { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
