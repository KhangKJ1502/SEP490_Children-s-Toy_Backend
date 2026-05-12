namespace ToyStore.Application.DTOs.Wallets;

public class CreateWalletRequestDto
{
    public string Pin { get; set; } = null!;

    public string ConfirmPin { get; set; } = null!;
}
