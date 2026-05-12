namespace ToyStore.Application.DTOs.Wallets;

public class ChangeWalletPinRequestDto
{
    public string OldPin { get; set; } = null!;

    public string NewPin { get; set; } = null!;

    public string ConfirmNewPin { get; set; } = null!;
}
