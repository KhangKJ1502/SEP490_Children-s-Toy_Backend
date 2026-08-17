using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

/// <summary>
/// Thực thể Khung giờ Flash Sale (PromotionTimeSlot) thuộc một chương trình khuyến mãi FLASH_SALE.
/// </summary>
public partial class PromotionTimeSlot
{
    /// <summary>
    /// Mã ID định danh duy nhất (khóa chính) của khung giờ Flash Sale.
    /// </summary>
    public int TimeSlotId { get; set; }

    /// <summary>
    /// Mã ID chương trình khuyến mãi cha.
    /// </summary>
    public int PromotionId { get; set; }

    /// <summary>
    /// Thời điểm bắt đầu khung giờ (UTC, DATETIME2(0)).
    /// </summary>
    public DateTime StartAt { get; set; }

    /// <summary>
    /// Thời điểm kết thúc khung giờ (UTC, DATETIME2(0)).
    /// </summary>
    public DateTime EndAt { get; set; }

    /// <summary>
    /// Trạng thái của khung giờ ("Scheduled", "Active", "Inactive", "Expired").
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Đánh dấu bản ghi đã bị xóa mềm (true).
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
    /// Navigation property trỏ tới Chương trình khuyến mãi chứa khung giờ này.
    /// </summary>
    public virtual Promotion Promotion { get; set; } = null!;

    /// <summary>
    /// Danh sách các sản phẩm mở bán trong khung giờ này.
    /// </summary>
    public virtual ICollection<PromotionProductSlot> PromotionProductSlots { get; set; } = new List<PromotionProductSlot>();
}
