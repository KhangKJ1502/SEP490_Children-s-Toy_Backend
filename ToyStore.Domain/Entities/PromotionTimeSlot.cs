using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class PromotionTimeSlot
{
    public int TimeSlotId { get; set; }

    public int PromotionId { get; set; }

    /// <summary>Thời điểm bắt đầu slot — lưu UTC (DATETIME2(0)).</summary>
    public DateTime StartAt { get; set; }

    /// <summary>Thời điểm kết thúc slot — lưu UTC (DATETIME2(0)).</summary>
    public DateTime EndAt { get; set; }

    public string Status { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Promotion Promotion { get; set; } = null!;

    /// <summary>Danh sách sản phẩm tham gia khung giờ này (chỉ FLASH_SALE).</summary>
    public virtual ICollection<PromotionProductSlot> PromotionProductSlots { get; set; } = new List<PromotionProductSlot>();
}
