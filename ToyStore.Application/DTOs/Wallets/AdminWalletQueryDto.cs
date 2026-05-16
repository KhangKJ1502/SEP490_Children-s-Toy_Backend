namespace ToyStore.Application.DTOs.Wallets;

public class AdminWalletQueryDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Account { get; set; }
    public string? Status { get; set; }
}
