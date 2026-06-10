using System;

namespace ToyStore.Domain.Entities;

public partial class WithdrawalStatusHistory
{
    public int HistoryId { get; set; }

    public int WithdrawalId { get; set; }

    public string? FromStatus { get; set; }

    public string ToStatus { get; set; } = null!;

    public string Source { get; set; } = null!;

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual WithdrawalRequest Withdrawal { get; set; } = null!;
}
