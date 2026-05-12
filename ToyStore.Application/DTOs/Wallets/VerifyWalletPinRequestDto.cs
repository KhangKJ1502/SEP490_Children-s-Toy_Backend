namespace ToyStore.Application.DTOs.Wallets;

public class VerifyWalletPinRequestDto
{
    public string Pin { get; set; } = null!;

    public string ActionType { get; set; } = null!;
}
