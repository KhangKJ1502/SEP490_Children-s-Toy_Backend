using System;

namespace ToyStore.Domain.Entities;

public partial class RefundDetail
{
    public int RefundDetailId { get; set; }

    public int RefundId { get; set; }

    public int ProductId { get; set; }

    public short Quantity { get; set; }

    /// <summary>
    /// Số lượng sản phẩm còn đủ điều kiện nhập kho lại sau khi Merchandise kiểm tra.
    /// NULL = chưa kiểm tra (chỉ có ý nghĩa với System Return).
    /// 0 = không nhập kho (hỏng hoặc Carrier fault).
    /// Default = Quantity khi tạo refund (giả định nguyên vẹn).
    /// </summary>
    public short? RestorableQuantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal RefundAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual OrderRefund Refund { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
