using ToyStore.Application.DTOs;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Service interface for webhook processing.
/// </summary>
public interface IWebhookService
{
    /// <summary>
    /// Processes a payment webhook from payment gateway.
    /// </summary>
    Task<WebhookResultDto> ProcessPaymentWebhookAsync(
        PaymentWebhookDto webhook, 
        string? sourceIP = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates webhook signature.
    /// </summary>
    Task<bool> ValidateSignatureAsync(
        string provider,
        string payload,
        string signature,
        CancellationToken cancellationToken = default);
}
