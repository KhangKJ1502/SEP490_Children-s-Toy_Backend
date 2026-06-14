namespace ToyStore.Application.DTOs.Wallets;

public class WalletDto
{
    public int WalletId { get; set; }

    public string Currency { get; set; } = null!;

    public decimal Balance { get; set; }

    public decimal LockedBalance { get; set; }

    public decimal AvailableBalance { get; set; }

    public string Status { get; set; } = null!;

    public bool HasPin { get; set; }
}
