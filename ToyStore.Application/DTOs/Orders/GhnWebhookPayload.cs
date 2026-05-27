using System;

namespace ToyStore.Application.DTOs.Orders;

public class GhnWebhookPayload
{
    public string OrderCode { get; set; } = null!;        // = ShippingProviderTransactions.ProviderOrderCode
    public string? ClientOrderCode { get; set; }          // = Orders.OrderCode (có thể rỗng)
    public string Status { get; set; } = null!;           // GHN status
    public string Type { get; set; } = null!;             // create | switch_status | update_weight | update_cod | update_fee
    public DateTime Time { get; set; }
    public long CODAmount { get; set; }
    public string? ReasonCode { get; set; }
    public string? Reason { get; set; }
    public decimal TotalFee { get; set; }
    public int Weight { get; set; }
    public int Length { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int PaymentType { get; set; }
}
