namespace ToyStore.Application.DTOs.Orders;

/// <summary>
/// Chi tiet don hang cho customer.
/// </summary>
public class CustomerOrderDetailDto
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }

    public string ShippingName { get; set; } = string.Empty;
    public string ShippingPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingWardName { get; set; } = string.Empty;
    public string ShippingDistrictName { get; set; } = string.Empty;
    public string ShippingProvinceName { get; set; } = string.Empty;

    public List<CustomerOrderDetailItemDto> Items { get; set; } = [];

    public decimal SubTotal { get; set; }
    public decimal VoucherDiscountAmount { get; set; }
    public decimal EstimatedShippingFee { get; set; }
    public decimal? ActualShippingFee { get; set; }
    public decimal TotalAmount { get; set; }
    public bool HasActiveRefund { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public string StatusBucket { get; set; } = string.Empty;
    public string DisplayLabel { get; set; } = string.Empty;
    public string PaymentDisplay { get; set; } = string.Empty;
    public string RefundDestination { get; set; } = "wallet";
    public bool CanCancel { get; set; }
    public bool CanComplete { get; set; }
    public bool IsAwaitingRefund { get; set; }
    public bool CanRefund { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }

    public List<CustomerOrderStatusHistoryDto> StatusHistory { get; set; } = [];
    public CustomerShippingTransactionDto? Shipping { get; set; }
}

public class CustomerOrderDetailItemDto
{
    public int OrderDetailId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImage { get; set; }
    public string? CategoryName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal? LineTotal { get; set; }
    public string Variant { get; set; } = string.Empty;
}

public class CustomerOrderStatusHistoryDto
{
    public string StatusName { get; set; } = string.Empty;
    public string? ChangedByName { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CustomerShippingTransactionDto
{
    public string Provider { get; set; } = string.Empty;
    public string? TrackingNumber { get; set; }
    public string? ProviderOrderCode { get; set; }
    public string? Status { get; set; }
    public decimal? ShippingFee { get; set; }
    public DateTime? EstimatedDelivery { get; set; }
}
