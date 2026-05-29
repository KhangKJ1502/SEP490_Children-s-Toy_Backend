namespace ToyStore.Domain.Constants;

public static class PaymentStatuses
{
    public const string Pending = "PENDING";
    public const string Paid = "PAID";
    public const string Failed = "FAILED";
    public const string Expired = "EXPIRED";
    public const string Refunded = "REFUNDED";
    public const string PartiallyRefunded = "PARTIALLY_REFUNDED";
    public const string CodPending = "COD_PENDING";
    public const string Cancelled = "CANCELLED";
}
