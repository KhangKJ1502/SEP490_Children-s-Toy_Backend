using System.Text.Json.Serialization;

namespace ToyStore.Application.Interfaces.Services;

public interface IPayOsPayoutWebhookService
{
    Task HandleAsync(string rawPayload, CancellationToken ct = default);
}

/// <summary>Payload from PayOS payout webhook callback.</summary>
public class PayOsPayoutWebhookPayload
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("desc")]
    public string? Desc { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public PayOsPayoutWebhookData? Data { get; set; }

    [JsonPropertyName("signature")]
    public string? Signature { get; set; }
}

public class PayOsPayoutWebhookData
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("reference")]
    public string? Reference { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("transactionId")]
    public string? TransactionId { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
