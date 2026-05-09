using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Constants;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Worker.Workers;

public class EmailDispatchJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<EmailDispatchJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

    public EmailDispatchJob(IServiceProvider services, ILogger<EmailDispatchJob> logger)
    {
        _services = services;
        _logger   = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailDispatchJob started");

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

        var pending = await db.Deliveries
            .Include(d => d.Account)
            .Where(d => d.Channel     == NotificationChannels.Email
                     && d.EmailStatus == EmailStatuses.Pending)
            .OrderBy(d => d.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        if (pending.Count == 0) return;

        _logger.LogInformation("EmailDispatchJob processing {Count} pending emails", pending.Count);

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
                delivery.UpdatedAt   = DateTime.UtcNow;

                _logger.LogInformation(
                    "Email sent. DeliveryID={DeliveryId} To={Email}",
                    delivery.DeliveryId, delivery.Account.Email);
            }
            catch (Exception ex)
            {
                delivery.EmailStatus = EmailStatuses.Failed;
                delivery.UpdatedAt   = DateTime.UtcNow;

                _logger.LogError(ex,
                    "Email send failed. DeliveryID={DeliveryId} IdempotencyKey={Key}",
                    delivery.DeliveryId, delivery.IdempotencyKey);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static string BuildHtmlBody(string title, string message, string? actionTarget)
    {
        var linkSection = actionTarget is not null
            ? $"<p><a href=\"{actionTarget}\">View Details</a></p>"
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
