using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class ShiftTemplate
{
    public byte ShiftTemplateId { get; set; }

    public string ShiftName { get; set; } = null!;

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    /// <summary>Dùng cho báo cáo, không dùng để phân đơn. MaxLoad thực tế lấy từ StaffShiftCapacity.</summary>
    public short MaxOrdersPerShift { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<WorkSchedule> WorkSchedules { get; set; } = new List<WorkSchedule>();
}
