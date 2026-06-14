namespace ToyStore.Infrastructure.Options;

public sealed class PayOsOptions
{
    public const string SectionName = "PayOS";

    public string PayoutApiKey { get; set; } = string.Empty;
    public string PayoutClientId { get; set; } = string.Empty;
    public string ChecksumKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api-merchant.payos.vn";
}

public sealed class WithdrawalLimitsOptions
{
    public const string SectionName = "WithdrawalLimits";

    public decimal MinAmount { get; set; } = 10_000m;
    public decimal MaxAmountPerTransaction { get; set; } = 50_000_000m;
    public decimal MaxAmountPerDay { get; set; } = 100_000_000m;
    public int MaxCountPerDay { get; set; } = 5;
    public int TimeoutMinutes { get; set; } = 30;
}
