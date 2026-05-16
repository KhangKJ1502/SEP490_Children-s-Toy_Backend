namespace ToyStore.Application.DTOs.Wallets;

public class WalletTransactionDto
{
    public int WalletTransactionId { get; set; }


    public int? RelatedOrderId { get; set; }

    public string? RelatedOrderCode { get; set; }

    public string TxnType { get; set; } = null!;

    public string Direction { get; set; } = null!;

    public decimal Amount { get; set; }

    public decimal SignedAmount { get; set; }

    public string Method { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}
