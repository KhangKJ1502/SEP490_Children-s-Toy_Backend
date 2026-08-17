using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToyStore.Domain.Entities;

/// <summary>
/// Thực thể liên kết sản phẩm cụ thể vào một khung giờ Flash Sale (PromotionTimeSlot).
/// Lưu giá bán Flash Sale, số lượng phân bổ, số lượng đã bán và số lượng giữ chỗ cho từng cặp (TimeSlot - Product).
/// </summary>
public partial class PromotionProductSlot
{
    /// <summary>
    /// Mã ID định danh duy nhất (khóa chính) của bản ghi liên kết sản phẩm - khung giờ.
    /// </summary>
    public int SlotProductId { get; set; }

    /// <summary>
    /// Mã ID của khung giờ Flash Sale.
    /// </summary>
    public int TimeSlotId { get; set; }

    /// <summary>
    /// Mã ID của sản phẩm tham gia Flash Sale.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Giá bán Flash Sale trong khung giờ này (VNĐ).
    /// </summary>
    public decimal SalePrice { get; set; }

    /// <summary>
    /// Tỷ lệ phần trăm giảm giá tương ứng (%) so với giá gốc.
    /// </summary>
    public decimal? DiscountPercent { get; set; }

    /// <summary>
    /// Số lượng sản phẩm tối đa được phép bán với giá Flash Sale trong khung giờ này.
    /// </summary>
    public int SaleQuantity { get; set; }

    /// <summary>
    /// Số lượng sản phẩm đã bán thành công qua các đơn hàng đã thanh toán.
    /// </summary>
    public int SoldQuantity { get; set; }

    /// <summary>
    /// Số lượng sản phẩm đang được giữ chỗ trong các đơn hàng tạm chờ thanh toán.
    /// </summary>
    public int ReservedQuantity { get; set; }

    /// <summary>
    /// Đánh dấu bản ghi đã bị xóa mềm hay chưa (true: đã xóa).
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Thời điểm tạo bản ghi (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Thời điểm cập nhật bản ghi gần nhất (UTC).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property trỏ tới khung giờ Flash Sale chứa bản ghi này.
    /// </summary>
    public virtual PromotionTimeSlot TimeSlot { get; set; } = null!;

    /// <summary>
    /// Navigation property trỏ tới sản phẩm tham gia Flash Sale.
    /// </summary>
    public virtual Product Product { get; set; } = null!;

    /// <summary>
    /// Danh sách chi tiết đơn hàng áp dụng mức giá Flash Sale này.
    /// </summary>
    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
