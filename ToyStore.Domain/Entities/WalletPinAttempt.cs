using System;

namespace ToyStore.Domain.Entities;

public partial class WalletPinAttempt
{
    public int AttemptId { get; set; }

    public int WalletId { get; set; }

    public int AccountId { get; set; }

    public string ActionType { get; set; } = null!;

    public bool IsSuccess { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual Wallet Wallet { get; set; } = null!;
}
