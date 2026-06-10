using System;

namespace ToyStore.Domain.Entities;

public partial class PayosWebhookLog
{
    public long WebhookLogId { get; set; }

    public int? WithdrawalId { get; set; }

    public string? WebhookEventId { get; set; }

    public string? ReferenceId { get; set; }

    public string? EventType { get; set; }

    public bool IsSignatureValid { get; set; }

    public string RawPayload { get; set; } = null!;

    public string ProcessStatus { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual WithdrawalRequest? Withdrawal { get; set; }
}
