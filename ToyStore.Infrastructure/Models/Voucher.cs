using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class Voucher
{
    public int VoucherId { get; set; }

    public byte VoucherTypeId { get; set; }

    public int? CreatedBy { get; set; }

    public string VoucherCode { get; set; } = null!;

    public string VoucherName { get; set; } = null!;

    public decimal DiscountAmount { get; set; }

    public string VoucherScope { get; set; } = null!;

    public decimal? MinOrderAmount { get; set; }

    public int Quantity { get; set; }

    public short? MaxUsagePerUser { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string Status { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account? CreatedByNavigation { get; set; }

    public virtual ICollection<OrderVoucher> OrderVouchers { get; set; } = new List<OrderVoucher>();

    public virtual VoucherType VoucherType { get; set; } = null!;

    public virtual ICollection<VoucherUsageLog> VoucherUsageLogs { get; set; } = new List<VoucherUsageLog>();
}
