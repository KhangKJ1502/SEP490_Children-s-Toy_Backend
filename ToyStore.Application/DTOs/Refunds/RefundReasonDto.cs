using System;

namespace ToyStore.Application.DTOs.Refunds;

/// <summary>
/// Data Transfer Object (DTO) thể hiện thông tin danh mục lý do hoàn tiền/đổi trả.
/// </summary>
public class RefundReasonDto
{
    /// <summary>
    /// Mã ID lý do hoàn tiền.
    /// </summary>
    public byte RefundReasonId { get; set; }

    /// <summary>
    /// Tiêu đề / Nội dung lý do hoàn tiền (ví dụ: "Sản phẩm bị lỗi kỹ thuật", "Giao sai sản phẩm",...).
    /// </summary>
    public string Content { get; set; } = null!;

    /// <summary>
    /// Mô tả chi tiết thêm về lý do hoàn tiền.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Bên chịu trách nhiệm thanh toán phí ship hoàn trả mặc định theo lý do này: "Store" (Cửa hàng) hoặc "Customer" (Khách hàng).
    /// Giao diện Frontend dùng để hiển thị cảnh báo mức phí hoàn trả khi khách hàng chọn lý do tương ứng.
    /// </summary>
    public string ResponsibleParty { get; set; } = "Store";
}
