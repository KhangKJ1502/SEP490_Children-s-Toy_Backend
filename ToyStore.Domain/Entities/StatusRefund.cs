using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class StatusRefund
{
    public byte StatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<OrderRefund> OrderRefunds { get; set; } = new List<OrderRefund>();

    public virtual ICollection<RefundStatusHistory> RefundStatusHistories { get; set; } = new List<RefundStatusHistory>();
}
