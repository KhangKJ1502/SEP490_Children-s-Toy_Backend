namespace ToyStore.Application.DTOs;

/// <summary>
/// Payment webhook request DTO.
/// </summary>
public class PaymentWebhookDto
{
    /// <summary>
    /// Payment provider (e.g., VNPay, Momo, ZaloPay).
    /// </summary>
    public string Provider { get; set; } = string.Empty;
    
    /// <summary>
    /// Transaction ID from payment provider.
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;
    
    /// <summary>
    /// Order reference/number.
    /// </summary>
    public string OrderReference { get; set; } = string.Empty;
    
    /// <summary>
    /// Payment status (e.g., success, failed, pending).
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// Amount paid.
    /// </summary>
    public decimal Amount { get; set; }
    
    /// <summary>
    /// Currency code.
    /// </summary>
    public string Currency { get; set; } = "VND";
    
    /// <summary>
    /// Webhook signature for verification.
    /// </summary>
    public string? Signature { get; set; }
    
    /// <summary>
    /// Raw payload as JSON string.
    /// </summary>
    public string? RawPayload { get; set; }
    
    /// <summary>
    /// Additional metadata.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Webhook processing result DTO.
/// </summary>
public class WebhookResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? OrderId { get; set; }
    public string? OrderNumber { get; set; }
}
