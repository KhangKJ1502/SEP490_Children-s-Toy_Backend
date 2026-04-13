using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int AccountId { get; set; }

    public byte StatusId { get; set; }

    public int? AssignedToStaffId { get; set; }

    public string OrderCode { get; set; } = null!;

    public string ShippingName { get; set; } = null!;

    public string ShippingPhone { get; set; } = null!;

    public string ShippingAddress { get; set; } = null!;

    public string? ShippingWard { get; set; }

    public string ShippingDistrict { get; set; } = null!;

    public string ShippingCity { get; set; } = null!;

    public DateTime OrderDate { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public DateTime? ShippedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string PaymentStatus { get; set; } = null!;

    public string PaymentCode { get; set; } = null!;

    public DateTime? PaidAt { get; set; }

    public decimal SubTotal { get; set; }

    public decimal VoucherDiscountAmount { get; set; }

    public decimal EstimatedShippingFee { get; set; }

    public decimal? ActualShippingFee { get; set; }

    public decimal TotalAmount { get; set; }

    public string? CancelReason { get; set; }

    public int? CancelledBy { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual Account? AssignedToStaff { get; set; }

    public virtual Account? CancelledByNavigation { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<OrderRefund> OrderRefunds { get; set; } = new List<OrderRefund>();

    public virtual ICollection<OrderStatusHistory> OrderStatusHistories { get; set; } = new List<OrderStatusHistory>();

    public virtual ICollection<OrderVoucher> OrderVouchers { get; set; } = new List<OrderVoucher>();

    public virtual ICollection<PaymentHistory> PaymentHistories { get; set; } = new List<PaymentHistory>();

    public virtual ICollection<ReviewProduct> ReviewProducts { get; set; } = new List<ReviewProduct>();

    public virtual StatusOrder Status { get; set; } = null!;

    public virtual ICollection<VoucherUsageLog> VoucherUsageLogs { get; set; } = new List<VoucherUsageLog>();

    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
}
