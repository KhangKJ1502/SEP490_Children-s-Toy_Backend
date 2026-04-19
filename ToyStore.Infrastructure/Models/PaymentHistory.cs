using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class PaymentHistory
{
    public int PaymentHistoryId { get; set; }

    public int AccountId { get; set; }

    public int OrderId { get; set; }

    public int? WalletTransactionId { get; set; }

    public string PaymentStatus { get; set; } = null!;

    public string PaymentMethod { get; set; } = null!;

    public string? TransactionCode { get; set; }

    public decimal Amount { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;

    public virtual ICollection<PaymentGatewayTransaction> PaymentGatewayTransactions { get; set; } = new List<PaymentGatewayTransaction>();

    public virtual WalletTransaction? WalletTransaction { get; set; }
}
