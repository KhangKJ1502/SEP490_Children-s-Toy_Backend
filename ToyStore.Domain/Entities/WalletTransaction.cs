using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class WalletTransaction
{
    public int WalletTransactionId { get; set; }

    public int WalletId { get; set; }

    public int AccountId { get; set; }

    public int? RelatedOrderId { get; set; }

    public string TxnType { get; set; } = null!;

    public string Direction { get; set; } = null!;

    public decimal Amount { get; set; }

    public decimal BalanceBefore { get; set; }

    public decimal BalanceAfter { get; set; }

    public string Method { get; set; } = null!;

    public string? ExternalRef { get; set; }

    public string? IdempotencyKey { get; set; }

    public string Status { get; set; } = null!;

    public string? Reason { get; set; }

    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<OrderRefund> OrderRefunds { get; set; } = new List<OrderRefund>();

    public virtual ICollection<PaymentHistory> PaymentHistories { get; set; } = new List<PaymentHistory>();

    public virtual Order? RelatedOrder { get; set; }

    public virtual Wallet Wallet { get; set; } = null!;

    public virtual ICollection<WithdrawalRequest> WithdrawalRequests { get; set; } = new List<WithdrawalRequest>();
}
