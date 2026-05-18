using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Notifications;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Auto-cancels ADMIN campaigns that stayed Approved past <see cref="Campaign.ApprovedExpireAt"/>.
/// Runs every 1 hour.
/// </summary>
public sealed class CampaignApprovedExpireJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<CampaignApprovedExpireJob> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    public CampaignApprovedExpireJob(
        IServiceProvider services,
        ILogger<CampaignApprovedExpireJob> logger,
        ITimeProvider timeProvider)
    {
        _services     = services;
        _logger       = logger;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "CampaignApprovedExpireJob error"); }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db         = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();
        var uow        = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();

        var success = true;
        string? message = null;

        try
        {
            var now = _timeProvider.UtcNow;
            var ids = await uow.Campaigns.ListApprovedExpiredCampaignIdsAsync(now, ct);
            var cancelled = 0;

            foreach (var id in ids)
            {
                var preview = await uow.Campaigns.GetByIdAsync(id, ct);
                var actor = preview?.CreatedByAccountId ?? preview?.SubmittedByAccountId ?? 1;

                await uow.Campaigns.SystemCancelWithAuditAsync(
                    id,
                    "Hết hạn phê duyệt (tự động).",
                    actor,
                    now,
                    ct);

                cancelled++;

                var notifyId = preview?.CreatedByAccountId ?? preview?.SubmittedByAccountId;
                if (notifyId is int nid && nid > 0)
                {
                    var name = preview?.CampaignName ?? $"#{id}";
                    await dispatcher.DispatchAsync(new NotificationContext
                    {
                        RecipientAccountId = nid,
                        RecipientType      = RecipientTypes.Staff,
                        NotificationType   = NotificationTypes.System,
                        Title              = "Chiến dịch hết hạn phê duyệt",
                        Message            = $"Chiến dịch '{name}' đã bị hủy vì quá hạn lịch phê duyệt. Vui lòng tạo hoặc gửi duyệt lại nếu cần.",
                        SendBell           = true,
                        SendEmail          = false,
                        IdempotencyKey     = $"campaign-approved-expired:{id}:{nid}:{now:yyyyMMddHH}"
                    }, ct);
                }
            }

            message = $"Cancelled {cancelled} approved-expired campaign(s)";
        }
        catch (Exception ex)
        {
            success = false;
            message = ex.Message;
            _logger.LogError(ex, "CampaignApprovedExpireJob failed");
        }

        await BackgroundJobTelemetry.RecordAsync(
            db,
            nameof(CampaignApprovedExpireJob),
            success,
            message,
            _logger,
            ct);
    }
}
