using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class Wallet
{
    public int WalletId { get; set; }

    public int AccountId { get; set; }

    public string Currency { get; set; } = null!;

    public decimal Balance { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? LastTransactionAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
}
