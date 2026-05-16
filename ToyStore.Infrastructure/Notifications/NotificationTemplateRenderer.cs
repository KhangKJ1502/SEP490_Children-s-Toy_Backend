using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Common.Exceptions;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;

namespace ToyStore.Infrastructure.Notifications;

/// <summary>
/// Đọc template từ [Notification].[Templates], cache 30 phút, thay placeholder {{Key}} và [Key].
/// </summary>
public class NotificationTemplateRenderer : INotificationTemplateRenderer
{
    private readonly ITemplateRepository _repo;
    private readonly IMemoryCache _cache;
    private readonly ILogger<NotificationTemplateRenderer> _logger;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

    public NotificationTemplateRenderer(
        ITemplateRepository repo,
        IMemoryCache cache,
        ILogger<NotificationTemplateRenderer> logger)
    {
        _repo   = repo;
        _cache  = cache;
        _logger = logger;
    }

    public async Task<(string Title, string Message)> RenderAsync(
        string templateCode,
        IReadOnlyDictionary<string, string>? placeholders,
        CancellationToken ct = default)
    {
        var cacheKey = $"notification:template:{templateCode}";

        if (!_cache.TryGetValue(cacheKey, out (string title, string message) cached))
        {
            var template = await _repo.GetActiveByCodeAsync(templateCode, ct);

            if (template is null)
                throw new TemplateNotFoundException(templateCode);

            cached = (template.TitleTemplate, template.MessageTemplate);

            _cache.Set(cacheKey, cached, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheTtl,
            });

            _logger.LogDebug("Template '{Code}' loaded from DB and cached.", templateCode);
        }

        var title   = Replace(cached.title, placeholders);
        var message = Replace(cached.message, placeholders);

        return (title, message);
    }

    // Thay thế cả hai dạng: {{Key}} và [Key] — khớp với cả hai convention trong dataseed.
    private static string Replace(string template, IReadOnlyDictionary<string, string>? placeholders)
    {
        if (placeholders is null || placeholders.Count == 0)
            return template;

        foreach (var (key, value) in placeholders)
        {
            template = template.Replace("{{" + key + "}}", value, StringComparison.Ordinal);
            template = template.Replace("[" + key + "]",   value, StringComparison.Ordinal);
        }

        return template;
    }
}
