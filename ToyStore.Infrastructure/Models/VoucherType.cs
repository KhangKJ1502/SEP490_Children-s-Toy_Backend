using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class VoucherType
{
    public byte VoucherTypeId { get; set; }

    public string VoucherTypeName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();
}
