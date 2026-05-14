namespace ToyStore.Application.DTOs.Wallets;

public class CreateSePayTopUpQrRequestDto
{
    public decimal Amount { get; set; }

    public string TopUpToken { get; set; } = string.Empty;
}
