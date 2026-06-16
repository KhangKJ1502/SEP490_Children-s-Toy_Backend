using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class WithdrawalRequest
{
    public int WithdrawalId { get; set; }

    public int WalletId { get; set; }

    public int AccountId { get; set; }

    public int? WalletTransactionId { get; set; }

    public string ReferenceId { get; set; } = null!;

    public decimal Amount { get; set; }

    public string ToBankBin { get; set; } = null!;

    public string ToBankName { get; set; } = null!;

    public string ToAccountNumber { get; set; } = null!;

    public string ToAccountName { get; set; } = null!;

    public string? PayosPayoutId { get; set; }

    public string? PayosTransactionId { get; set; }

    public string? PayosRawResponse { get; set; }

    public string Status { get; set; } = null!;

    public string? FailReason { get; set; }

    public byte RetryCount { get; set; }

    public DateTime? ProcessingAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual Wallet Wallet { get; set; } = null!;

    public virtual WalletTransaction? WalletTransaction { get; set; }

    public virtual ICollection<WithdrawalStatusHistory> WithdrawalStatusHistories { get; set; } = new List<WithdrawalStatusHistory>();
}
