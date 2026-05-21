namespace ToyStore.Infrastructure.Options;

/// <summary>
/// Configuration used to call Python AI moderation service.
/// </summary>
public sealed class AiModerationOptions
{
    public const string SectionName = "AiModeration";

    public bool Enabled { get; set; } = true;

    public string BaseUrl { get; set; } = "http://localhost:8001";

    public string InternalApiKey { get; set; } = "change-me-to-a-secret-key";

    public int TimeoutSeconds { get; set; } = 10;
}

