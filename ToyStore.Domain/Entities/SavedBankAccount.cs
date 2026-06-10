using System;

namespace ToyStore.Domain.Entities;

public partial class SavedBankAccount
{
    public int SavedBankAccountId { get; set; }

    public int AccountId { get; set; }

    public string BankBin { get; set; } = null!;

    public string BankName { get; set; } = null!;

    public string BankShortName { get; set; } = null!;

    public string? BankCode { get; set; }

    public string AccountNumber { get; set; } = null!;

    public string AccountName { get; set; } = null!;

    public bool IsDefault { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;
}
