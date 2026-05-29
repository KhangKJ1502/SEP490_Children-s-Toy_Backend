namespace ToyStore.Application.Constants;

/// <summary>
/// One wallet credit per paid order — shared by cancel and refund-complete paths.
/// </summary>
public static class WalletRefundKeys
{
    public static string ForOrder(string orderCode) => $"REFUND_{orderCode}";
}
