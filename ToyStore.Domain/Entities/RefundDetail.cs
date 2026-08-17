using System;

namespace ToyStore.Domain.Entities;

/// <summary>
/// Thực thể đại diện cho chi tiết từng mặt hàng (sản phẩm, số lượng, đơn giá, số lượng đạt/hỏng) trong một yêu cầu hoàn tiền (RefundDetail).
/// </summary>
public partial class RefundDetail
{
    /// <summary>
    /// Khóa chính (Primary Key) của bản ghi chi tiết hoàn tiền.
    /// </summary>
    public int RefundDetailId { get; set; }

    /// <summary>
    /// Khóa ngoại tham chiếu đến yêu cầu hoàn tiền cha (OrderRefund).
    /// </summary>
    public int RefundId { get; set; }

    /// <summary>
    /// Khóa ngoại tham chiếu đến sản phẩm được hoàn trả (Product).
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Số lượng sản phẩm yêu cầu hoàn trả.
    /// </summary>
    public short Quantity { get; set; }

    /// <summary>
    /// Số lượng sản phẩm còn đủ điều kiện nhập kho lại sau khi Merchandise kiểm tra.
    /// NULL = chưa kiểm tra (chỉ có ý nghĩa với System Return).
    /// 0 = không nhập kho (hỏng hoặc Carrier fault).
    /// Default = Quantity khi tạo refund (giả định nguyên vẹn).
    /// </summary>
    public short? RestorableQuantity { get; set; }

    /// <summary>
    /// Số lượng sản phẩm hỏng do lỗi của khách (không được hoàn tiền).
    /// </summary>
    public short FailedCustomerQty { get; set; }

    /// <summary>
    /// Số lượng sản phẩm hỏng do lỗi vận chuyển (được hoàn tiền).
    /// </summary>
    public short FailedCarrierQty { get; set; }

    /// <summary>
    /// Đơn giá sản phẩm tại thời điểm mua (sau khi phân bổ chiết khấu).
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Số tiền hoàn dự kiến hoặc thực tế cho sản phẩm này (= Quantity * UnitPrice).
    /// </summary>
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// Thời điểm tạo bản ghi chi tiết (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Navigation property: Thực thể yêu cầu hoàn tiền cha.
    /// </summary>
    public virtual OrderRefund Refund { get; set; } = null!;

    /// <summary>
    /// Navigation property: Thực thể sản phẩm tương ứng.
    /// </summary>
    public virtual Product Product { get; set; } = null!;
}
