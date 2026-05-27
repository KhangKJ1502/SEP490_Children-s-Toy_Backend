using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Notifications;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Restores blog comment permission when a temporary comment ban expires.
/// Runs every 1 hour.
/// </summary>
public class BlogCommentPermissionUnlockJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<BlogCommentPermissionUnlockJob> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    public BlogCommentPermissionUnlockJob(
        IServiceProvider services,
        ILogger<BlogCommentPermissionUnlockJob> logger,
        ITimeProvider timeProvider)
    {
        _services = services;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "BlogCommentPermissionUnlockJob error"); }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();

        bool success = true;
        string? message = null;

        try
        {
            var nowUtc = _timeProvider.UtcNow;
            var expiredBans = await db.BlogCommentViolationCounts
                .Where(x => x.IsCommentBanned
                         && x.BanExpiresAt != null
                         && x.BanExpiresAt <= nowUtc)
                .ToListAsync(ct);

            foreach (var state in expiredBans)
            {
                state.IsCommentBanned = false;
                state.BanExpiresAt = null;
                state.ViolationCount = 0;
                state.LastViolatedAt = null;
                state.UnbannedAt = nowUtc;
                state.UpdatedAt = nowUtc;
            }

            if (expiredBans.Count > 0)
            {
                await db.SaveChangesAsync(ct);

                foreach (var state in expiredBans)
                {
                    await dispatcher.DispatchAsync(new NotificationContext
                    {
                        RecipientAccountId = state.AccountId,
                        RecipientType = RecipientTypes.Customer,
                        NotificationType = NotificationTypes.System,
                        Title = "Blog comment permission restored",
                        Message = "Your blog comment permission has been restored.",
                        SendBell = true,
                        SendEmail = false,
                        ActionTarget = "/blog",
                        IdempotencyKey = $"blog-comment-permission-restored:{state.AccountId}:{nowUtc.Ticks}"
                    }, ct);
                }
            }

            message = expiredBans.Count > 0
                ? $"Unlocked {expiredBans.Count} expired blog comment ban(s)."
                : "No expired blog comment bans.";

            if (expiredBans.Count > 0)
            {
                _logger.LogInformation("BlogCommentPermissionUnlockJob: {Message}", message);
            }
        }
        catch (Exception ex)
        {
            success = false;
            message = ex.Message;
            _logger.LogError(ex, "BlogCommentPermissionUnlockJob failed");
        }

        await BackgroundJobTelemetry.RecordAsync(
            scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>(),
            "BlogCommentPermissionUnlockJob", success, message, _logger, ct);
    }
}
