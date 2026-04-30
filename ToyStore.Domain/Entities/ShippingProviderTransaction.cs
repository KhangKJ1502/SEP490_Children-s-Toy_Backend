using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class ShippingProviderTransaction
{
    public long ShippingTransactionId { get; set; }

    public int OrderId { get; set; }

    public string Provider { get; set; } = null!;

    public string? ProviderOrderCode { get; set; }

    public string? TrackingNumber { get; set; }

    public string? ServiceType { get; set; }

    public string? Status { get; set; }

    public decimal? ShippingFee { get; set; }

    public decimal? CodAmount { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public int RetryCount { get; set; }

    public string? LastErrorMessage { get; set; }

    public string? Metadata { get; set; }

    public DateTime? EstimatedDelivery { get; set; }

    public DateTime? ActualDelivery { get; set; }

    public DateTime? LastPolledAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual ICollection<ShippingStatusHistory> ShippingStatusHistories { get; set; } = new List<ShippingStatusHistory>();
}
