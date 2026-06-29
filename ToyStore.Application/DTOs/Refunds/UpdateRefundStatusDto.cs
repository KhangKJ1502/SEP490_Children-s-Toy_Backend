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
}
