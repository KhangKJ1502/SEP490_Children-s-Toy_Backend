namespace ToyStore.Application.DTOs.Checkouts;

/// <summary>
/// Response payload after checkout confirmation.
/// </summary>
public class CheckoutConfirmResponseDto
{
    public int OrderId { get; set; }

    public string OrderCode { get; set; } = string.Empty;

    public string? ShippingOrderCode { get; set; }

    public decimal ShippingFee { get; set; }

    public DateTime? EstimatedDeliveryTime { get; set; }

    public decimal TotalAmount { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;

    public string PaymentStatus { get; set; } = string.Empty;

    /// <summary>SE_PAY: attempt code nhúng vào nội dung chuyển khoản.</summary>
    public string? PaymentAttemptCode { get; set; }

    /// <summary>SE_PAY: URL QR image từ SePay.</summary>
    public string? QrImageUrl { get; set; }
}

/// <summary>
/// Preview: shipping fee + totals trước khi đặt hàng.
/// </summary>
public class CheckoutPreviewResponseDto
{
    public decimal SubTotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public int TotalWeightGrams { get; set; }
    public DateTime? EstimatedDeliveryTime { get; set; }
    public List<CheckoutPreviewItemErrorDto> ItemErrors { get; set; } = [];
}

public class CheckoutPreviewItemErrorDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}

/// <summary>Response khi retry QR.</summary>
public class RetryPaymentResponseDto
{
    public string PaymentAttemptCode { get; set; } = string.Empty;
    public string QrImageUrl { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}