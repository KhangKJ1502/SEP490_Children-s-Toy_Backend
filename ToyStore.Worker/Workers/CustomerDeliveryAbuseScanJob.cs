using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Notifications;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Detects COD customers with repeated GHN delivery failures that indicate unpaid order abandonment.
/// Development default is short; production can set CustomerAbuseScan:IntervalMinutes to about 43200.
/// </summary>
public class CustomerDeliveryAbuseScanJob : BackgroundService
{
    private const string BlockReasonContent = "COD delivery abuse";
    private const int SuspiciousOrderThreshold = 3;
    private static readonly string[] SuspiciousFailCodes =
    {
        "GHN-DFC1A2",
        "GHN-DFC1A7",
        "GHN-DCD1A5",
        "GHN-DCD0A8",
        "GHN-DCD1A1"
    };

    private readonly IServiceProvider _services;
    private readonly ILogger<CustomerDeliveryAbuseScanJob> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly TimeSpan _interval;

    public CustomerDeliveryAbuseScanJob(
        IServiceProvider services,
        ILogger<CustomerDeliveryAbuseScanJob> logger,
        ITimeProvider timeProvider,
        IConfiguration configuration)
    {
        _services = services;
        _logger = logger;
        _timeProvider = timeProvider;

        var intervalMinutes = configuration.GetValue<int?>("CustomerAbuseScan:IntervalMinutes") ?? 2;
        _interval = TimeSpan.FromMinutes(Math.Max(1, intervalMinutes));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CustomerDeliveryAbuseScanJob started. Interval={Interval}", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CustomerDeliveryAbuseScanJob error");
            }

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
            var now = _timeProvider.UtcNow;
            var suspiciousOrders = await db.Orders
                .AsNoTracking()
                .Where(o => !o.IsDeleted
                         && o.DeliveryFailCount >= 3
                         && o.PaymentMethod == "SHIP_COD"
                         && o.PaymentStatus != "PAID"
                         && o.LastGHNFailCode != null
                         && SuspiciousFailCodes.Contains(o.LastGHNFailCode)
                         && o.Account.RoleId == 1
                         && o.Account.IsActive
                         && !o.Account.IsDeleted)
                .Select(o => new
                {
                    o.AccountId,
                    o.Account.AccountName,
                    o.Account.Email,
                    o.OrderCode,
                    o.OrderDate,
                    o.LastGHNFailCode
                })
                .ToListAsync(ct);

            var offenders = suspiciousOrders
                .GroupBy(o => new { o.AccountId, o.AccountName, o.Email })
                .Select(g =>
                {
                    var ordered = g.OrderByDescending(o => o.OrderDate).ToList();
                    return new
                    {
                        g.Key.AccountId,
                        g.Key.AccountName,
                        g.Key.Email,
                        SuspiciousOrderCount = ordered.Count,
                        OrderCodes = ordered.Select(o => o.OrderCode).Take(5).ToList(),
                        LastFailCode = ordered.Select(o => o.LastGHNFailCode).FirstOrDefault()
                    };
                })
                .Where(x => x.SuspiciousOrderCount >= SuspiciousOrderThreshold)
                .ToList();

            if (offenders.Count == 0)
            {
                message = "No COD delivery abuse accounts found.";
                await BackgroundJobTelemetry.RecordAsync(db, nameof(CustomerDeliveryAbuseScanJob), success, message, _logger, ct);
                return;
            }

            var blockReasonId = await GetOrCreateBlockReasonIdAsync(db, now, ct);
            var adminIds = await db.Accounts
                .Where(a => a.IsActive
                         && !a.IsDeleted
                         && (a.Role.RoleName == "Admin" || a.RoleId == 4))
                .Select(a => a.AccountId)
                .ToListAsync(ct);

            foreach (var offender in offenders)
            {
                var account = await db.Accounts
                    .FirstOrDefaultAsync(a => a.AccountId == offender.AccountId && a.IsActive && !a.IsDeleted, ct);
                if (account is null)
                {
                    continue;
                }

                account.IsActive = false;
                account.UpdatedAt = now;

                var hasOpenBlock = await db.UserBlockHistories
                    .AnyAsync(x => x.AccountId == offender.AccountId && x.UnblockedAt == null, ct);
                if (!hasOpenBlock)
                {
                    db.UserBlockHistories.Add(new UserBlockHistory
                    {
                        AccountId = offender.AccountId,
                        BlockedBy = null,
                        BlockReasonId = blockReasonId,
                        Note = $"Auto-locked: {offender.SuspiciousOrderCount} unpaid COD orders reached 3 GHN delivery failures. Orders: {string.Join(", ", offender.OrderCodes)}. Last GHN code: {offender.LastFailCode}.",
                        BlockedAt = now,
                        BlockedUntil = now.AddYears(10)
                    });
                }

                await db.SaveChangesAsync(ct);

                foreach (var adminId in adminIds)
                {
                    await dispatcher.DispatchAsync(new NotificationContext
                    {
                        RecipientAccountId = adminId,
                        RecipientType = RecipientTypes.Admin,
                        NotificationType = NotificationTypes.System,
                        Title = "Customer account locked for COD delivery abuse",
                        Message = $"{offender.AccountName} has {offender.SuspiciousOrderCount} unpaid COD orders with repeated GHN delivery failures.",
                        SendBell = true,
                        SendEmail = false,
                        ActionTarget = "/admin/customers",
                        Payload = new Dictionary<string, object>
                        {
                            ["accountId"] = offender.AccountId,
                            ["email"] = offender.Email,
                            ["orderCodes"] = offender.OrderCodes,
                            ["lastFailCode"] = offender.LastFailCode ?? string.Empty
                        },
                        IdempotencyKey = $"customer-delivery-abuse:{offender.AccountId}:{now:yyyyMMddHHmm}:{adminId}"
                    }, ct);
                }
            }

            message = $"Locked {offenders.Count} customer account(s) for COD delivery abuse.";
            _logger.LogInformation("CustomerDeliveryAbuseScanJob: {Message}", message);
        }
        catch (Exception ex)
        {
            success = false;
            message = ex.Message;
            _logger.LogError(ex, "CustomerDeliveryAbuseScanJob failed");
        }

        await BackgroundJobTelemetry.RecordAsync(db, nameof(CustomerDeliveryAbuseScanJob), success, message, _logger, ct);
    }

    private static async Task<byte> GetOrCreateBlockReasonIdAsync(
        SEP490ToyStoreContext db,
        DateTime now,
        CancellationToken ct)
    {
        var reason = await db.BlockReasons
            .OrderBy(x => x.BlockReasonId)
            .FirstOrDefaultAsync(x => x.Content == BlockReasonContent && !x.IsDeleted, ct);

        if (reason is not null)
        {
            return reason.BlockReasonId;
        }

        reason = new BlockReason
        {
            Content = BlockReasonContent,
            Description = "Customer has repeated unpaid COD delivery failures from GHN.",
            IsDeleted = false,
            CreatedAt = now
        };

        db.BlockReasons.Add(reason);
        await db.SaveChangesAsync(ct);
        return reason.BlockReasonId;
    }
}
