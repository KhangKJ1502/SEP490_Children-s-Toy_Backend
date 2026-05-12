namespace ToyStore.Application.DTOs.Wallets;

public class ResetForgotWalletPinRequestDto
{
    public string NewPin { get; set; } = null!;

    public string ConfirmNewPin { get; set; } = null!;
}
