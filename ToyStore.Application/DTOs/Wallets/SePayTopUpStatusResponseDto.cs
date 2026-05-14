namespace ToyStore.Application.DTOs.Wallets;

public class SePayTopUpStatusResponseDto
{
    public string AttemptCode { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Status { get; set; } = string.Empty;

    public int? WalletTransactionId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}
