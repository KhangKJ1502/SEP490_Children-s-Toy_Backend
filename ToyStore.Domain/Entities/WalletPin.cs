using System;

namespace ToyStore.Domain.Entities;

public partial class WalletPin
{
    public int WalletPinId { get; set; }

    public int WalletId { get; set; }

    public string PinHash { get; set; } = null!;

    public bool IsActive { get; set; }

    public byte FailedAttempts { get; set; }

    public byte TotalFailedAttempts { get; set; }

    public DateTime? LockedUntil { get; set; }

    public DateTime LastChangedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Wallet Wallet { get; set; } = null!;
}
