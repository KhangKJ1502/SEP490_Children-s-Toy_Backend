using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class Promotion
{
    public int PromotionId { get; set; }

    public int CreatedBy { get; set; }

    public string PromotionName { get; set; } = null!;

    public string PromotionType { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string Status { get; set; } = null!;

    public int Priority { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<ProductPromotion> ProductPromotions { get; set; } = new List<ProductPromotion>();

    public virtual ICollection<PromotionTimeSlot> PromotionTimeSlots { get; set; } = new List<PromotionTimeSlot>();
}
