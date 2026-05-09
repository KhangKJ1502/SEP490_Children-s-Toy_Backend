using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Notifications;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Sends birthday greetings at 08:00 daily to customers and their children.
/// Uses BirthdayNotifiedYear to prevent duplicate sends within the same year.
/// </summary>
public class BirthdayNotificationJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<BirthdayNotificationJob> _logger;

    public BirthdayNotificationJob(IServiceProvider services, ILogger<BirthdayNotificationJob> logger)
    {
        _services = services;
        _logger   = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BirthdayNotificationJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var now  = DateTime.UtcNow.AddHours(7); // UTC+7
            var next = now.Date.AddHours(8);
            if (now.Hour >= 8) next = next.AddDays(1);

            await Task.Delay(next - now, stoppingToken);
            if (stoppingToken.IsCancellationRequested) break;

            await RunAsync(stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope      = _services.CreateScope();
        var db               = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();
        var dispatcher       = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();
        var prefChecker      = scope.ServiceProvider.GetRequiredService<IUserPreferenceChecker>();

        bool success = true;
        string? message = null;

        try
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
            var year  = (short)today.Year;

            // --- Customer birthdays ---
            var accountBirthdays = await db.Accounts
                .Where(a => a.Dob != null
                         && a.Dob.Value.Day   == today.Day
                         && a.Dob.Value.Month == today.Month
                         && a.IsActive
                         && !a.IsDeleted
                         && (a.BirthdayNotifiedYear == null || a.BirthdayNotifiedYear < year))
                .ToListAsync(ct);

            foreach (var account in accountBirthdays)
            {
                if (!await prefChecker.CanSendAsync(account.AccountId, PreferenceKeys.Promotions, ct))
                    continue;

                await dispatcher.DispatchAsync(new NotificationContext
                {
                    RecipientAccountId = account.AccountId,
                    RecipientType      = RecipientTypes.Customer,
                    NotificationType   = NotificationTypes.Promotion,
                    Title              = "Chúc mừng sinh nhật!",
                    Message            = $"Chúc {account.AccountName} sinh nhật vui vẻ! 🎂",
                    SendBell           = true,
                    SendEmail          = true,
                    TemplateCode       = NotificationTemplates.BirthdayCustomer,
                    IdempotencyKey     = $"birthday.customer:{account.AccountId}:{year}",
                }, ct);

                account.BirthdayNotifiedYear = year;
                account.UpdatedAt            = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(ct);
            message = $"Processed {accountBirthdays.Count} account birthdays";
        }
        catch (Exception ex)
        {
            success = false;
            message = ex.Message;
            _logger.LogError(ex, "BirthdayNotificationJob failed");
        }

        await BackgroundJobTelemetry.RecordAsync(db, "BirthdayNotificationJob", success, message, _logger, ct);
    }
}
