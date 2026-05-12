namespace ToyStore.Application.DTOs.Wallets;

public class VerifyWalletPinResponseDto
{
    public int WalletId { get; set; }

    public string ActionType { get; set; } = null!;

    public bool IsVerified { get; set; }

    public int RemainingAttempts { get; set; }

    public DateTime? LockedUntil { get; set; }

    public string WalletStatus { get; set; } = null!;
}
