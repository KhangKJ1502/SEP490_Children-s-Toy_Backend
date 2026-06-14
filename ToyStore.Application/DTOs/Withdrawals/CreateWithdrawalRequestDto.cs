namespace ToyStore.Application.DTOs.Withdrawals;

public class CreateWithdrawalRequestDto
{
    public decimal Amount { get; set; }

    /// <summary>ID of a saved bank account. Either this or the manual bank fields must be provided.</summary>
    public int? SavedBankAccountId { get; set; }

    // Optional manual bank fields (used when no saved account is selected)
    public string? ToBankBin { get; set; }
    public string? ToBankName { get; set; }
    public string? ToAccountNumber { get; set; }
    public string? ToAccountName { get; set; }

    /// <summary>6-digit wallet PIN for authorization.</summary>
    public string Pin { get; set; } = null!;
}
