namespace ToyStore.Application.DTOs.Orders;

/// <summary>
/// Thong tin don hang trong danh sach cua customer.
/// </summary>
public class CustomerOrderListItemDto
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public bool HasActiveRefund { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public string StatusBucket { get; set; } = string.Empty;
    public string DisplayLabel { get; set; } = string.Empty;
    public string PaymentDisplay { get; set; } = string.Empty;
    public string RefundDestination { get; set; } = "wallet";
    public bool CanCancel { get; set; }
    public bool CanComplete { get; set; }
    public bool IsAwaitingRefund { get; set; }
    public List<CustomerOrderListItemProductDto> Items { get; set; } = new();
}

public class CustomerOrderListItemProductDto
{
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImage { get; set; }
    public string? CategoryName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string Variant { get; set; } = string.Empty;
}
