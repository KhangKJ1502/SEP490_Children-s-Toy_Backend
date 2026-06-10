namespace ToyStore.Application.DTOs.Wallets;

public class SePayTopUpQrResponseDto
{
    public string AttemptCode { get; set; } = string.Empty;

    public string QrImageUrl { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTime ExpiresAt { get; set; }

    public string? BankName { get; set; }
    public string? BankCode { get; set; }
    public string? AccountNumber { get; set; }
    public string? AccountName { get; set; }
}
