using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class PaymentGatewayTransaction
{
    public long PaymentGatewayTxnId { get; set; }

    public int OrderId { get; set; }

    public int? PaymentHistoryId { get; set; }

    public string Provider { get; set; } = null!;

    public string? RequestId { get; set; }

    public string? TransactionNo { get; set; }

    public decimal Amount { get; set; }

    public string? ResponseCode { get; set; }

    public string? ResponseMessage { get; set; }

    public string Status { get; set; } = null!;

    public string? RawCallback { get; set; }

    public int RetryCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual PaymentHistory? PaymentHistory { get; set; }
}
