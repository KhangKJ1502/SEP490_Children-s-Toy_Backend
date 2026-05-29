namespace ToyStore.Application.DTOs.Orders;

/// <summary>
/// Customer-facing status contract (bucket, labels, action flags).
/// </summary>
public class CustomerOrderStatusContractDto
{
    public string StatusCode { get; set; } = string.Empty;
    public string StatusBucket { get; set; } = string.Empty;
    public string DisplayLabel { get; set; } = string.Empty;
    public string PaymentDisplay { get; set; } = string.Empty;
    public string RefundDestination { get; set; } = "wallet";
    public bool CanCancel { get; set; }
    public bool CanComplete { get; set; }
    public bool IsAwaitingRefund { get; set; }
}
