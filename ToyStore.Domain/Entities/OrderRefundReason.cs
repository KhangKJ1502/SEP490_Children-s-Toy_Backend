using System;
using System.Collections.Generic;
using ToyStore.Domain.Constants;

namespace ToyStore.Domain.Entities;

public partial class OrderRefundReason
{
    public byte RefundReasonId { get; set; }

    public string Content { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsDeleted { get; set; }

    public bool IsSystem { get; set; }

    /// <summary>
    /// Bên chịu trách nhiệm mặc định cho lý do này.
    /// "Store" = lỗi cửa hàng (shop chịu phí ship hoàn trả).
    /// "Customer" = lỗi/thay đổi từ khách (khách chịu phí ship hoàn trả).
    /// </summary>
    public string ResponsibleParty { get; set; } = RefundResponsibleParty.Store;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<OrderRefund> OrderRefunds { get; set; } = new List<OrderRefund>();
}
