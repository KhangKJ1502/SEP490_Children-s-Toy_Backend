using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

/// <summary>
/// Thực thể Chương trình Khuyến mãi (Promotion) trong cơ sở dữ liệu (bao gồm giảm giá trực tiếp DISCOUNT và Flash Sale theo khung giờ FLASH_SALE).
/// </summary>
public partial class Promotion
{
    /// <summary>
    /// Mã ID định danh duy nhất (khóa chính) của chương trình khuyến mãi.
    /// </summary>
    public int PromotionId { get; set; }

    /// <summary>
    /// ID tài khoản nhân viên/quản trị viên đã tạo chương trình này.
    /// </summary>
    public int CreatedBy { get; set; }

    /// <summary>
    /// Tên chương trình khuyến mãi.
    /// </summary>
    public string PromotionName { get; set; } = null!;

    /// <summary>
    /// Loại khuyến mãi ("DISCOUNT" hoặc "FLASH_SALE").
    /// </summary>
    public string PromotionType { get; set; } = null!;

    /// <summary>
    /// Mô tả chi tiết nội dung và điều kiện áp dụng của chương trình.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Thời điểm bắt đầu chương trình (UTC).
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Thời điểm kết thúc chương trình (UTC).
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Trạng thái hiện tại ("Scheduled", "Active", "Inactive", "Expired").
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Độ ưu tiên áp dụng của chương trình (số càng cao độ ưu tiên càng lớn).
    /// </summary>
    public int Priority { get; set; }

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
    /// Navigation property trỏ tới tài khoản người tạo chương trình khuyến mãi.
    /// </summary>
    public virtual Account CreatedByNavigation { get; set; } = null!;

    /// <summary>
    /// Danh sách các sản phẩm tham gia chương trình khuyến mãi thông thường (DISCOUNT).
    /// </summary>
    public virtual ICollection<ProductPromotion> ProductPromotions { get; set; } = new List<ProductPromotion>();

    /// <summary>
    /// Danh sách các khung giờ Flash Sale trực thuộc chương trình (FLASH_SALE).
    /// </summary>
    public virtual ICollection<PromotionTimeSlot> PromotionTimeSlots { get; set; } = new List<PromotionTimeSlot>();

    /// <summary>
    /// Danh sách các dòng chi tiết đơn hàng đã áp dụng khuyến mãi từ chương trình này.
    /// </summary>
    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
