namespace ToyStore.Domain.Enums;

public static class WalletTxnTypes
{
    public const string TopUp = "TopUp";
    public const string Payment = "Payment";
    public const string Refund = "Refund";
    public const string Withdrawal = "Withdrawal";
}

public static class WithdrawalStatuses
{
    public const string Pending = "PENDING";
    public const string Processing = "PROCESSING";
    public const string Success = "SUCCESS";
    public const string Failed = "FAILED";
    public const string Cancelled = "CANCELLED";
}

public static class WithdrawalHistorySources
{
    public const string User = "USER";
    public const string Webhook = "WEBHOOK";
    public const string Job = "JOB";
    public const string System = "SYSTEM";
}

public static class WalletPinActions
{
    public const string Payment = "PAYMENT";
    public const string ViewBalance = "VIEW_BALANCE";
    public const string TopUp = "TOP_UP";
    public const string Withdrawal = "WITHDRAWAL";
}
