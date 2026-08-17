using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToyStore.Domain.Entities;

/// <summary>
/// Thực thể quan hệ nhiều-nhiều gán một sản phẩm vào chương trình giảm giá thông thường (DISCOUNT).
/// </summary>
public partial class ProductPromotion
{
    /// <summary>
    /// Mã ID sản phẩm (khóa chính kết hợp).
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Mã ID chương trình khuyến mãi (khóa chính kết hợp).
    /// </summary>
    public int PromotionId { get; set; }

    /// <summary>
    /// Giá bán khuyến mãi của sản phẩm trong chương trình này (VNĐ).
    /// </summary>
    public decimal SalePrice { get; set; }

    /// <summary>
    /// Tỷ lệ phần trăm giảm giá tương ứng (%) so với giá niêm yết.
    /// </summary>
    public decimal? DiscountPercent { get; set; }

    /// <summary>
    /// Đánh dấu bản ghi đã bị xóa mềm khỏi chương trình khuyến mãi (true).
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
    /// Navigation property trỏ tới sản phẩm được áp dụng giảm giá.
    /// </summary>
    public virtual Product Product { get; set; } = null!;

    /// <summary>
    /// Navigation property trỏ tới chương trình khuyến mãi chứa sản phẩm này.
    /// </summary>
    public virtual Promotion Promotion { get; set; } = null!;
}
