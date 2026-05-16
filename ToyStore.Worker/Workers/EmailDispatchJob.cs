using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Constants;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Safety-net worker: gửi lại các email còn trạng thái Pending sau ít nhất 2 phút
/// (EmailChannel đã cố gắng gửi ngay; những email vẫn Pending là do lỗi transient SMTP
/// hoặc crash giữa chừng).
/// Chạy mỗi 30 giây; chỉ lấy tối đa 50 email.
/// </summary>
public class EmailDispatchJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<EmailDispatchJob> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly TimeSpan _interval    = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _retryAfter  = TimeSpan.FromMinutes(2);

    public EmailDispatchJob(
        IServiceProvider services, 
        ILogger<EmailDispatchJob> logger,
        ITimeProvider timeProvider)
    {
        _services     = services;
        _logger       = logger;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailDispatchJob (safety-net) started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchPendingEmailsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in EmailDispatchJob");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task DispatchPendingEmailsAsync(CancellationToken ct)
    {
        using var scope   = _services.CreateScope();
        var db            = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();
        var emailSender   = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        // Chỉ retry những email vẫn Pending ít nhất _retryAfter để tránh race với EmailChannel
        var cutoff = _timeProvider.UtcNow - _retryAfter;

        var pending = await db.Deliveries
            .Include(d => d.Account)
            .Where(d => d.Channel     == NotificationChannels.Email
                     && d.EmailStatus == EmailStatuses.Pending
                     && d.CreatedAt   <= cutoff)
            .OrderBy(d => d.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        if (pending.Count == 0) return;

        _logger.LogInformation("EmailDispatchJob retrying {Count} pending emails", pending.Count);

        foreach (var delivery in pending)
        {
            try
            {
                var emailMsg = new EmailMessage(
                    ToEmail:  delivery.Account.Email,
                    ToName:   delivery.Account.AccountName,
                    Subject:  delivery.Title,
                    HtmlBody: BuildHtmlBody(delivery.Title, delivery.Message, delivery.ActionTarget));

                await emailSender.SendAsync(emailMsg, ct);

                delivery.EmailStatus = EmailStatuses.Sent;
                delivery.UpdatedAt   = _timeProvider.UtcNow;

                _logger.LogInformation(
                    "Email (retry) sent. DeliveryID={DeliveryId} To={Email}",
                    delivery.DeliveryId, delivery.Account.Email);
            }
            catch (Exception ex)
            {
                delivery.EmailStatus = EmailStatuses.Failed;
                delivery.UpdatedAt   = _timeProvider.UtcNow;

                _logger.LogError(ex,
                    "Email (retry) send failed. DeliveryID={DeliveryId}",
                    delivery.DeliveryId);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static string BuildHtmlBody(string title, string message, string? actionTarget)
    {
        var linkSection = actionTarget is not null
            ? $"<p><a href=\"{System.Net.WebUtility.HtmlEncode(actionTarget)}\">View details</a></p>"
            : string.Empty;

        return $"""
            <html><body>
            <h2>{System.Net.WebUtility.HtmlEncode(title)}</h2>
            <p>{System.Net.WebUtility.HtmlEncode(message)}</p>
            {linkSection}
            </body></html>
            """;
    }
}
