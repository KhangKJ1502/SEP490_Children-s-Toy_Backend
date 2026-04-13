namespace ToyStore.Domain.Entities;

/// <summary>
/// Logs payment webhook events from payment gateways.
/// </summary>
public class PaymentWebhook : Entity
{
    /// <summary>
    /// Order ID if applicable.
    /// </summary>
    public Guid? OrderId { get; set; }
    
    /// <summary>
    /// Order navigation property.
    /// </summary>
    public virtual Order? Order { get; set; }
    
    /// <summary>
    /// Payment provider (e.g., VNPay, Momo, ZaloPay).
    /// </summary>
    public string Provider { get; set; } = string.Empty;
    
    /// <summary>
    /// Transaction ID from payment provider.
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;
    
    /// <summary>
    /// Event type (e.g., payment.success, payment.failed).
    /// </summary>
    public string EventType { get; set; } = string.Empty;
    
    /// <summary>
    /// Raw webhook payload as JSON.
    /// </summary>
    public string Payload { get; set; } = string.Empty;
    
    /// <summary>
    /// Webhook signature for verification.
    /// </summary>
    public string? Signature { get; set; }
    
    /// <summary>
    /// Whether the webhook was processed successfully.
    /// </summary>
    public bool IsProcessed { get; set; }
    
    /// <summary>
    /// Processing result or error message.
    /// </summary>
    public string? ProcessingResult { get; set; }
    
    /// <summary>
    /// When the webhook was received.
    /// </summary>
    public DateTime ReceivedAt { get; set; }
    
    /// <summary>
    /// When the webhook was processed.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
    
    /// <summary>
    /// Number of processing attempts.
    /// </summary>
    public int ProcessingAttempts { get; set; }
    
    /// <summary>
    /// IP address of the webhook sender.
    /// </summary>
    public string? SourceIP { get; set; }
    
    public PaymentWebhook()
    {
        ReceivedAt = DateTime.UtcNow;
    }
}
