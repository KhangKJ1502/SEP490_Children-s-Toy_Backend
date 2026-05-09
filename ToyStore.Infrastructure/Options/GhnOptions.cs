namespace ToyStore.Infrastructure.Options;

/// <summary>
/// Cau hinh GHN, bind tu section "GHN" trong appsettings.
/// </summary>
public sealed class GhnOptions
{
    public const string SectionName = "GHN";

    public string ApiToken { get; set; } = string.Empty;

    public int ShopId { get; set; }

    public string ApiEndpoint { get; set; } = string.Empty;

    public int FromDistrictId { get; set; }

    public string FromWardCode { get; set; } = string.Empty;

    public int FeeServiceTypeId { get; set; }
    
    public int DefaultServiceId { get; set; }

    public int RetryCount { get; set; } = 3;

    public int TimeoutSeconds { get; set; } = 30;
}
