namespace ToyStore.Application.DTOs.Orders;

/// <summary>
/// Chi tiet day du cua 1 don hang cho admin.
/// </summary>
public class AdminOrderDetailDto
{
    // Thong tin don
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

    // Dia chi giao hang
    public string ShippingName { get; set; } = string.Empty;
    public string ShippingPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingWardName { get; set; } = string.Empty;
    public string ShippingDistrictName { get; set; } = string.Empty;
    public string ShippingProvinceName { get; set; } = string.Empty;

    // Danh sach san pham
    public List<AdminOrderDetailItemDto> Items { get; set; } = [];

    // Tong tien
    public decimal SubTotal { get; set; }
    public decimal VoucherDiscountAmount { get; set; }
    public decimal EstimatedShippingFee { get; set; }
    public decimal? ActualShippingFee { get; set; }
    public decimal TotalAmount { get; set; }

    // Thanh toan
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }

    // Phan cong
    public int? AssignedToStaffId { get; set; }
    public string? AssignedToStaffName { get; set; }
    public int? AssignedToMerchId { get; set; }
    public string? AssignedToMerchName { get; set; }

    // Lich su trang thai
    public List<OrderStatusHistoryDto> StatusHistory { get; set; } = [];

    // Thong tin van chuyen (neu co)
    public ShippingTransactionDto? Shipping { get; set; }
}

public class AdminOrderDetailItemDto
{
    public int OrderDetailId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImage { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal? LineTotal { get; set; }
}

public class OrderStatusHistoryDto
{
    public string StatusName { get; set; } = string.Empty;
    public string? ChangedByName { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ShippingTransactionDto
{
    public string Provider { get; set; } = string.Empty;
    public string? TrackingNumber { get; set; }
    public string? ProviderOrderCode { get; set; }
    public string? Status { get; set; }
    public decimal? ShippingFee { get; set; }
    public DateTime? EstimatedDelivery { get; set; }
}
