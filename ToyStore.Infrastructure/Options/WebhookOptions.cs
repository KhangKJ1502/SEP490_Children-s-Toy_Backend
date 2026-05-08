namespace ToyStore.Infrastructure.Options;

/// <summary>
/// Cau hinh token xac thuc webhook tu shipper.
/// Bind tu section "Webhooks" trong appsettings.
/// </summary>
public sealed class WebhookOptions
{
    public const string SectionName = "Webhooks";

    public ShippingWebhookOptions Shipping { get; set; } = new();
}

public sealed class ShippingWebhookOptions
{
    /// <summary>
    /// Token xac thuc cho tung provider. Key = ten provider (GHN, GHTK, ...).
    /// </summary>
    public Dictionary<string, string> Tokens { get; set; } = [];
}
