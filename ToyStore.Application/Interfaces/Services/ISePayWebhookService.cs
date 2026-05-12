using System.Text.Json.Serialization;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Services;

public interface ISePayWebhookService
{
    /// <summary>
    /// Xử lý payload webhook từ SE_PAY.
    /// Phân nhánh dựa theo prefix content: SPX_ (đơn hàng) hoặc WLT_ (nạp ví).
    /// </summary>
    Task HandleAsync(SePayWebhookPayload payload, CancellationToken cancellationToken = default);
}

/// <summary>
/// Payload webhook từ SE_PAY (theo tài liệu SE_PAY integration).
/// </summary>
public class SePayWebhookPayload
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("gateway")]
    public string? Gateway { get; set; }

    [JsonPropertyName("transactionDate")]
    public string? TransactionDate { get; set; }

    [JsonPropertyName("accountNumber")]
    public string? AccountNumber { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("transferAmount")]
    public decimal TransferAmount { get; set; }

    [JsonPropertyName("accumulated")]
    public decimal? AccumulatedAmount { get; set; }

    [JsonPropertyName("subAccount")]
    public string? SubAccount { get; set; }

    [JsonPropertyName("referenceCode")]
    public string? ReferenceCode { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("transferType")]
    public string? TransferType { get; set; }
}
