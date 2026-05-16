namespace ToyStore.Application.DTOs.Wallets;

public class AdminWalletListDto
{
    public int WalletId { get; set; }
    public string Account { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
