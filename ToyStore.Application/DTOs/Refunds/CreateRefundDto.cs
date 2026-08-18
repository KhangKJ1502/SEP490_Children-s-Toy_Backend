using System.Collections.Generic;

namespace ToyStore.Application.DTOs.Refunds;

/// <summary>
/// Data Transfer Object (DTO) nhận dữ liệu từ khách hàng khi gửi yêu cầu Hoàn tiền / Đổi trả mới.
/// </summary>
public class CreateRefundDto
{
    /// <summary>
    /// Mã ID của đơn hàng đã hoàn tất (Completed) cần yêu cầu hoàn tiền.
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Mã ID lý do hoàn tiền (tham chiếu bảng OrderRefundReason).
    /// </summary>
    public byte RefundReasonId { get; set; }

    /// <summary>
    /// Mô tả chi tiết lý do hoàn tiền từ khách hàng.
    /// </summary>
    public string? ReasonDetails { get; set; }

    /// <summary>
    /// Loại yêu cầu hoàn tiền ("ReturnAndRefund": Trả hàng & Hoàn tiền, hoặc "RefundOnly": Chỉ hoàn tiền không trả hàng). Mặc định là "ReturnAndRefund".
    /// </summary>
    public string RefundType { get; set; } = "ReturnAndRefund";

    /// <summary>
    /// Danh sách các URL hình ảnh bằng chứng (đã được upload lên Cloudinary).
    /// </summary>
    public List<string> Images { get; set; } = new List<string>();

    /// <summary>
    /// Danh sách các sản phẩm và số lượng yêu cầu hoàn trả.
    /// </summary>
    public List<CreateRefundItemDto> Items { get; set; } = new List<CreateRefundItemDto>();
}
