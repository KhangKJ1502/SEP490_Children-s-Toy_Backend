using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class PromotionTimeSlot
{
    public int TimeSlotId { get; set; }

    public int PromotionId { get; set; }

    public DateOnly SlotDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Promotion Promotion { get; set; } = null!;
}
