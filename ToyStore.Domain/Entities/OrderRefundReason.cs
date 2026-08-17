using System;
using System.Collections.Generic;
using ToyStore.Domain.Constants;

namespace ToyStore.Domain.Entities;

/// <summary>
/// Thực thể danh mục Lý do hoàn tiền / Đổi trả sản phẩm (OrderRefundReason) trong cơ sở dữ liệu.
/// </summary>
public partial class OrderRefundReason
{
    /// <summary>
    /// Khóa chính (Primary Key) của lý do hoàn tiền.
    /// </summary>
    public byte RefundReasonId { get; set; }

    /// <summary>
    /// Nội dung / Tiêu đề ngắn gọn của lý do hoàn tiền (ví dụ: "Sản phẩm lỗi", "Giao sai hàng", "Đổi ý",...).
    /// </summary>
    public string Content { get; set; } = null!;

    /// <summary>
    /// Mô tả chi tiết thêm về lý do hoàn tiền.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Cờ đánh dấu xóa mềm lý do.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Cờ đánh dấu lý do do hệ thống định nghĩa sẵn (không cho phép xóa).
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// Bên chịu trách nhiệm thanh toán phí vận chuyển hoàn trả mặc định cho lý do này:
    /// "Store" = Lỗi từ phía cửa hàng (Cửa hàng chịu phí ship hoàn trả).
    /// "Customer" = Thay đổi/Lỗi từ phía khách hàng (Khách hàng chịu phí ship hoàn trả).
    /// </summary>
    public string ResponsibleParty { get; set; } = RefundResponsibleParty.Store;

    /// <summary>
    /// Thời điểm tạo lý do hoàn tiền (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Navigation property: Danh sách các yêu cầu hoàn tiền thuộc lý do này.
    /// </summary>
    public virtual ICollection<OrderRefund> OrderRefunds { get; set; } = new List<OrderRefund>();
}
