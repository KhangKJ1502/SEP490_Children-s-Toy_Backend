using System;

namespace ToyStore.Domain.Entities;

public partial class OrderAssignment
{
    public int AssignmentId { get; set; }

    public int OrderId { get; set; }

    public int ScheduleId { get; set; }

    public int AccountId { get; set; }

    /// <summary>2 = Staff, 3 = Merchandise</summary>
    public byte RoleId { get; set; }

    /// <summary>false khi bị reassign — không xoá record cũ để giữ lịch sử</summary>
    public bool IsActive { get; set; }

    public DateTime AssignedAt { get; set; }

    /// <summary>NULL = hệ thống tự động phân đơn</summary>
    public int? AssignedBy { get; set; }

    public string? Notes { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual WorkSchedule WorkSchedule { get; set; } = null!;

    public virtual Account Account { get; set; } = null!;

    public virtual Account? AssignedByNavigation { get; set; }

    public virtual Role Role { get; set; } = null!;
}
