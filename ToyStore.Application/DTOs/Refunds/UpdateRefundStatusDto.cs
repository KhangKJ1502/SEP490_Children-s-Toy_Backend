using System.Collections.Generic;

namespace ToyStore.Application.DTOs.Refunds;

public class UpdateRefundStatusDto
{
    public string Status { get; set; } = null!;
    public string? RejectReason { get; set; }
    public string? ShippingOrderCode { get; set; }
    public string? ReturnShippingOrderCode { get; set; }
    public bool? InspectionPassed { get; set; }
    public string? InspectionNote { get; set; }
    public string? AdminNote { get; set; }

    // --- Dùng khi Status = RefundApproved ---

    /// <summary>
    /// Override bên chịu phí vận chuyển hoàn trả: "Store" hoặc "Customer".
    /// null = dùng suggestion từ RefundReason.ResponsibleParty.
    /// </summary>
    public string? ReturnShippingFeeBy { get; set; }

    /// <summary>
    /// Bắt buộc nếu ReturnShippingFeeBy khác với suggestion từ RefundReason.
    /// Ghi vào audit trail.
    /// </summary>
    public string? ReturnShippingFeeNote { get; set; }

    // --- Dùng khi Status = RefundInspectionPending (Merchandise đề xuất)
    //     và Status = RefundCompleted (Staff/Admin xác nhận) ---

    /// <summary>
    /// Nguyên nhân hàng bị hư hỏng: "Customer" hoặc "Carrier".
    /// Merchandise set lúc InspectionPending. Staff xác nhận lúc Complete.
    /// </summary>
    public string? DamageResponsibility { get; set; }

    // --- Dùng khi Status = RefundInspectionPending (System Return — Merchandise) ---

    /// <summary>
    /// [System Return] Danh sách SL nhập kho theo từng sản phẩm.
    /// Merchandise điền lúc chuyển sang RefundInspectionPending.
    /// </summary>
    public List<RefundDetailRestockDto>? RestockItems { get; set; }

    // --- Dùng khi Status = RefundCompleted (System Return — Staff) ---

    /// <summary>
    /// [System Return] Staff chọn có hoàn phí vận chuyển cho khách không.
    /// Bắt buộc khi Complete system return.
    /// true = FinalRefundAmount = TotalAmount.
    /// false = FinalRefundAmount = TotalAmount - CustomerShippingPaid.
    /// </summary>
    public bool? IncludeShippingInRefund { get; set; }
}

/// <summary>SL nhập kho lại cho từng sản phẩm (System Return — Merchandise inspection).</summary>
public class RefundDetailRestockDto
{
    public int ProductId { get; set; }

    /// <summary>0 ≤ RestorableQuantity ≤ Quantity trong đơn.</summary>
    public short RestorableQuantity { get; set; }
}
