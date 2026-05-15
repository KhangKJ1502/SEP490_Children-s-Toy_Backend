using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class WorkSchedule
{
    public int ScheduleId { get; set; }

    public int AccountId { get; set; }

    public byte ShiftTemplateId { get; set; }

    public DateTime WorkDate { get; set; }

    /// <summary>Scheduled | OnDuty | Completed | Absent | Cancelled</summary>
    public string Status { get; set; } = "Scheduled";

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ShiftTemplate ShiftTemplate { get; set; } = null!;

    public virtual Account CreatedByNavigation { get; set; } = null!;

    public virtual StaffShiftCapacity StaffShiftCapacity { get; set; } = null!;

    public virtual ICollection<OrderAssignment> OrderAssignments { get; set; } = new List<OrderAssignment>();
}
