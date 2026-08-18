using System.Collections.Generic;

namespace ToyStore.Application.DTOs.Refunds;

/// <summary>
/// Data Transfer Object (DTO) nhận dữ liệu từ Quản trị viên/Nhân viên/Bộ phận Kho khi cập nhật trạng thái tiến trình xử lý yêu cầu hoàn tiền.
/// </summary>
public class UpdateRefundStatusDto
{
    /// <summary>
    /// Trạng thái mới cần chuyển đến (ví dụ: "RefundApproved", "RefundRejected", "RefundReturning", "RefundReceived", "RefundInspectionPending", "RefundCompleted",...).
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Lý do từ chối yêu cầu hoàn tiền (bắt buộc khi Status = "RefundRejected").
    /// </summary>
    public string? RejectReason { get; set; }

    /// <summary>
    /// Mã vận đơn chuyển hàng ban đầu từ đơn vị vận chuyển GHN.
    /// </summary>
    public string? ShippingOrderCode { get; set; }

    /// <summary>
    /// Mã vận đơn trả hàng từ khách hàng về kho cửa hàng.
    /// </summary>
    public string? ReturnShippingOrderCode { get; set; }

    /// <summary>
    /// URL ảnh chụp bằng chứng giao nhận hàng trả về kho.
    /// </summary>
    public string? ReturnDeliveryImageUrl { get; set; }

    /// <summary>
    /// URL ảnh chụp bằng chứng gửi trả lại hàng cho khách hàng.
    /// </summary>
    public string? ReturnToCustomerImageUrl { get; set; }

    /// <summary>
    /// Kết quả kiểm tra chất lượng hàng hoàn tại kho (true: Đạt yêu cầu, false: Không đạt).
    /// </summary>
    public bool? InspectionPassed { get; set; }

    /// <summary>
    /// Ghi chú chi tiết kết quả kiểm tra chất lượng hàng hoàn từ nhân viên kho.
    /// </summary>
    public string? InspectionNote { get; set; }

    /// <summary>
    /// Ghi chú nội bộ của Quản trị viên / Nhân viên xử lý.
    /// </summary>
    public string? AdminNote { get; set; }

    // --- Dùng khi Status = RefundApproved ---

    /// <summary>
    /// Ghi đè (override) bên chịu phí vận chuyển hoàn trả: "Store" hoặc "Customer".
    /// null = sử dụng giá trị mặc định được gợi ý từ RefundReason.ResponsibleParty.
    /// </summary>
    public string? ReturnShippingFeeBy { get; set; }

    /// <summary>
    /// Ghi chú lý do thay đổi bên chịu phí hoàn trả (bắt buộc nếu ReturnShippingFeeBy khác với gợi ý mặc định, ghi vào audit trail).
    /// </summary>
    public string? ReturnShippingFeeNote { get; set; }

    // --- Dùng khi Status = RefundInspectionPending (Merchandise đề xuất)
    //     và Status = RefundCompleted (Staff/Admin xác nhận) ---

    /// <summary>
    /// Bên chịu trách nhiệm khi hàng hóa bị hư hỏng: "Customer" hoặc "Carrier".
    /// Merchandise thiết lập khi kiểm tra hàng; Staff/Admin xác nhận khi hoàn tất.
    /// </summary>
    public string? DamageResponsibility { get; set; }

    // --- Dùng khi Status = RefundInspectionPending (System Return — Merchandise) ---

    /// <summary>
    /// [System Return] Danh sách số lượng sản phẩm nhập lại kho theo từng mặt hàng.
    /// Nhân viên kho điền khi chuyển sang RefundInspectionPending.
    /// </summary>
    public List<RefundDetailRestockDto>? RestockItems { get; set; }

    // --- Dùng khi Status = RefundCompleted (System Return — Staff) ---

    /// <summary>
    /// [System Return] Nhân viên CSKH lựa chọn có hoàn lại tiền phí vận chuyển cho khách hay không (bắt buộc khi hoàn tất System Return).
    /// true = Hoàn toàn bộ (FinalRefundAmount = TotalAmount).
    /// false = Trừ phí ship đã chi trả (FinalRefundAmount = TotalAmount - CustomerShippingPaid).
    /// </summary>
    public bool? IncludeShippingInRefund { get; set; }
}

/// <summary>
/// Data Transfer Object (DTO) thể hiện số lượng nhập kho lại và phân loại lỗi hư hỏng cho từng sản phẩm trong quá trình kiểm tra hàng hoàn.
/// </summary>
public class RefundDetailRestockDto
{
    /// <summary>
    /// Mã ID sản phẩm.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Số lượng sản phẩm còn nguyên vẹn, đủ điều kiện nhập lại kho bán tiếp (0 ≤ RestorableQuantity ≤ Số lượng trong đơn).
    /// </summary>
    public short RestorableQuantity { get; set; }

    /// <summary>
    /// Số lượng sản phẩm bị hỏng do lỗi từ phía khách hàng (không được hoàn tiền).
    /// </summary>
    public short FailedCustomerQty { get; set; }

    /// <summary>
    /// Số lượng sản phẩm bị hỏng do lỗi từ đơn vị vận chuyển (được hoàn tiền).
    /// </summary>
    public short FailedCarrierQty { get; set; }
}
