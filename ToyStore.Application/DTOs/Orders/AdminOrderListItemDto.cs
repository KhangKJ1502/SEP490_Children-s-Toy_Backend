namespace ToyStore.Application.DTOs.Orders;

/// <summary>
/// Don gian hoa thong tin 1 don hang trong danh sach admin.
/// </summary>
public class AdminOrderListItemDto
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public byte StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string FulfillmentLabel { get; set; } = string.Empty;
    public string? GhnShippingStatus { get; set; }

    // Thong tin nguoi nhan
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;

    // Tien te
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;

    // Phan cong
    public int? AssignedToStaffId { get; set; }
    public string? AssignedToStaffName { get; set; }
    public int? AssignedToMerchId { get; set; }
    public string? AssignedToMerchName { get; set; }

    // Moc thoi gian
    public DateTime OrderDate { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
}
