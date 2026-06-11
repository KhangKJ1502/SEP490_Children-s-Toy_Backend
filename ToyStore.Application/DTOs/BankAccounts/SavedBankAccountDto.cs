using System;

namespace ToyStore.Application.DTOs.BankAccounts;

public class SavedBankAccountDto
{
    public int SavedBankAccountId { get; set; }
    public int AccountId { get; set; }
    public string BankBin { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankShortName { get; set; } = string.Empty;
    public string? BankCode { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
