using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class OrderVoucher
{
    public int OrderId { get; set; }

    public int VoucherId { get; set; }

    public decimal DiscountAmountApplied { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Voucher Voucher { get; set; } = null!;
}
