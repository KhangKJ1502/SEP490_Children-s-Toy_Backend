using System;

namespace ToyStore.Domain.Entities;

public partial class StaffShiftCapacity
{
    public int CapacityId { get; set; }

    public int ScheduleId { get; set; }

    public int AccountId { get; set; }

    /// <summary>Số đơn đang xử lý đồng thời (real-time). Tăng khi nhận đơn, giảm khi đơn Completed/Cancelled.</summary>
    public short CurrentLoad { get; set; }

    /// <summary>Giới hạn số đơn đồng thời. Admin có thể tăng ngay trong ca.</summary>
    public short MaxLoad { get; set; }

    /// <summary>UTC timestamp when shift reached max load and admins were notified (once per schedule).</summary>
    public DateTime? ShiftFullNotifiedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual WorkSchedule WorkSchedule { get; set; } = null!;

    public virtual Account Account { get; set; } = null!;
}
