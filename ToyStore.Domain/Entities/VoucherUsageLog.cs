using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class VoucherUsageLog
{
    public int UsageId { get; set; }

    public int VoucherId { get; set; }

    public int AccountId { get; set; }

    public int OrderId { get; set; }

    public DateTime UsedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;

    public virtual Voucher Voucher { get; set; } = null!;
}
