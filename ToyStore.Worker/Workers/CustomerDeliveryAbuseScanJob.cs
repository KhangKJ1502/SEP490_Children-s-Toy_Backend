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
    private const int FirstWarningThreshold = 1;
    private const int CodRestrictionThreshold = 2;
    private const int ManualReviewThreshold = 3;
    private const string StatusNormal = "NORMAL";
    private const string StatusCodRestricted = "COD_RESTRICTED";
    private const string StatusCodProbation = "COD_PROBATION";
    private const string StatusPendingReview = "PENDING_ADMIN_REVIEW";
    private const string StatusAppealApprovedStrict = "APPEAL_APPROVED_STRICT";
    private const string StatusPermanentBlocked = "PERMANENT_BLOCKED";
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
    private readonly TimeSpan _codRestrictionReviewDelay;
    private readonly TimeSpan _codProbationPeriod;

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

        var codRestrictionReviewMinutes = configuration.GetValue<int?>("CustomerAbuseScan:CodRestrictionReviewMinutes");
        if (codRestrictionReviewMinutes.HasValue)
        {
            _codRestrictionReviewDelay = TimeSpan.FromMinutes(Math.Max(1, codRestrictionReviewMinutes.Value));
        }
        else
        {
            var codRestrictionReviewDays = configuration.GetValue<int?>("CustomerAbuseScan:CodRestrictionReviewDays") ?? 7;
            _codRestrictionReviewDelay = TimeSpan.FromDays(Math.Max(1, codRestrictionReviewDays));
        }

        var codProbationDays = configuration.GetValue<int?>("CustomerAbuseScan:CodProbationDays") ?? 30;
        _codProbationPeriod = TimeSpan.FromDays(Math.Max(1, codProbationDays));
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
            await RestoreCodRestrictionCasesAsync(db, dispatcher, now, _codRestrictionReviewDelay, _codProbationPeriod, ct);
            await RestoreCodProbationCasesAsync(db, dispatcher, now, _codProbationPeriod, ct);
            await RestoreStrictCasesAsync(db, dispatcher, now, ct);

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

            var suspiciousAccountIds = suspiciousOrders
                .Select(o => o.AccountId)
                .Distinct()
                .ToList();

            var abuseCases = await db.CustomerDeliveryAbuseCases
                .Where(x => suspiciousAccountIds.Contains(x.AccountId))
                .ToDictionaryAsync(x => x.AccountId, ct);

            var offenders = suspiciousOrders
                .GroupBy(o => new { o.AccountId, o.AccountName, o.Email })
                .Select(g =>
                {
                    var scoped = abuseCases.TryGetValue(g.Key.AccountId, out var abuseCase) && abuseCase.CountingFrom.HasValue
                        ? g.Where(o => o.OrderDate >= abuseCase.CountingFrom.Value)
                        : g;
                    var ordered = scoped.OrderByDescending(o => o.OrderDate).ToList();
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
                .Where(x => x.SuspiciousOrderCount >= FirstWarningThreshold)
                .ToList();

            if (offenders.Count == 0)
            {
                message = "No COD delivery abuse accounts found.";
                await BackgroundJobTelemetry.RecordAsync(db, nameof(CustomerDeliveryAbuseScanJob), success, message, _logger, ct);
                return;
            }

            var adminIds = await db.Accounts
                .Where(a => a.IsActive
                         && !a.IsDeleted
                         && (a.Role.RoleName == "Admin" || a.RoleId == 4))
                .Select(a => a.AccountId)
                .ToListAsync(ct);

            foreach (var offender in offenders)
            {
                var abuseCase = await UpsertDeliveryAbuseCaseAsync(
                    db,
                    offender.AccountId,
                    offender.SuspiciousOrderCount,
                    offender.LastFailCode,
                    suspiciousOrders
                        .Where(o => o.AccountId == offender.AccountId)
                        .OrderByDescending(o => o.OrderDate)
                        .Select(o => (DateTime?)o.OrderDate)
                        .FirstOrDefault(),
                    now,
                    ct);
                var effectiveSuspiciousOrderCount = abuseCase.Status == StatusPendingReview
                    ? Math.Max(offender.SuspiciousOrderCount, ManualReviewThreshold)
                    : offender.SuspiciousOrderCount;

                if (abuseCase.Status == StatusPermanentBlocked)
                {
                    await NotifyPermanentBlockAsync(dispatcher, offender.AccountId, effectiveSuspiciousOrderCount, ct);
                    continue;
                }

                await NotifyCustomerByThresholdAsync(dispatcher, offender.AccountId, offender.AccountName, effectiveSuspiciousOrderCount, offender.OrderCodes, ct);

                if (effectiveSuspiciousOrderCount >= ManualReviewThreshold)
                {
                    foreach (var adminId in adminIds)
                    {
                        await dispatcher.DispatchAsync(new NotificationContext
                        {
                            RecipientAccountId = adminId,
                            RecipientType = RecipientTypes.Admin,
                            NotificationType = NotificationTypes.System,
                            Title = "Customer requires manual lock review",
                            Message = $"{offender.AccountName} has reached delivery-abuse manual review after repeated unpaid COD delivery failures. Review the customer and lock the account if appropriate.",
                            SendBell = true,
                            SendEmail = false,
                            ActionTarget = "/admin/customers",
                            Payload = new Dictionary<string, object>
                            {
                                ["accountId"] = offender.AccountId,
                                ["email"] = offender.Email,
                                ["orderCodes"] = offender.OrderCodes,
                                ["lastFailCode"] = offender.LastFailCode ?? string.Empty,
                                ["manualBlockRecommended"] = true,
                                ["suspiciousOrderCount"] = effectiveSuspiciousOrderCount
                            },
                            IdempotencyKey = $"customer-delivery-abuse-review:{offender.AccountId}:{effectiveSuspiciousOrderCount}:{adminId}"
                        }, ct);
                    }
                }
            }

            message = $"Scanned {offenders.Count} customer account(s) for COD delivery abuse.";
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

    private static async Task<CustomerDeliveryAbuseCase> UpsertDeliveryAbuseCaseAsync(
        SEP490ToyStoreContext db,
        int accountId,
        int suspiciousOrderCount,
        string? lastFailCode,
        DateTime? lastOrderDate,
        DateTime now,
        CancellationToken ct)
    {
        var abuseCase = await db.CustomerDeliveryAbuseCases
            .FirstOrDefaultAsync(x => x.AccountId == accountId, ct);

        if (abuseCase is null)
        {
            abuseCase = new CustomerDeliveryAbuseCase
            {
                AccountId = accountId,
                Status = StatusNormal,
                CreatedAt = now
            };
            db.CustomerDeliveryAbuseCases.Add(abuseCase);
        }

        if (abuseCase.Status == StatusAppealApprovedStrict && suspiciousOrderCount >= FirstWarningThreshold)
        {
            var account = await db.Accounts.FirstAsync(x => x.AccountId == accountId, ct);
            account.IsActive = false;
            account.UpdatedAt = now;

            abuseCase.Status = StatusPermanentBlocked;
            abuseCase.WarningLevel = ManualReviewThreshold;
            abuseCase.SuspiciousOrderCount = suspiciousOrderCount;
            abuseCase.LastGHNFailCode = lastFailCode;
            abuseCase.LastSuspiciousOrderDate = lastOrderDate;
            abuseCase.PermanentBlockedAt = now;
            abuseCase.PermanentBlockReason = "A new unpaid COD delivery-abuse order occurred during the stricter appeal monitoring period.";
            abuseCase.Note = "Permanently blocked after appeal approval due to another delivery-abuse incident.";
            abuseCase.UpdatedAt = now;
            await db.SaveChangesAsync(ct);
            return abuseCase;
        }

        if (abuseCase.Status == StatusCodProbation && suspiciousOrderCount >= FirstWarningThreshold)
        {
            abuseCase.Status = StatusPendingReview;
            abuseCase.WarningLevel = ManualReviewThreshold;
            abuseCase.SuspiciousOrderCount = Math.Max(ManualReviewThreshold, abuseCase.SuspiciousOrderCount + suspiciousOrderCount);
            abuseCase.LastGHNFailCode = lastFailCode;
            abuseCase.LastSuspiciousOrderDate = lastOrderDate;
            abuseCase.ReviewRequestedAt ??= now;
            abuseCase.Note = "Customer had another suspicious COD delivery failure during COD probation after a previous COD restriction.";
            abuseCase.UpdatedAt = now;
            await db.SaveChangesAsync(ct);
            return abuseCase;
        }

        abuseCase.SuspiciousOrderCount = suspiciousOrderCount;
        abuseCase.LastGHNFailCode = lastFailCode;
        abuseCase.LastSuspiciousOrderDate = lastOrderDate;
        abuseCase.WarningLevel = (byte)Math.Min(suspiciousOrderCount, ManualReviewThreshold);
        abuseCase.UpdatedAt = now;

        if (suspiciousOrderCount >= ManualReviewThreshold)
        {
            abuseCase.Status = StatusPendingReview;
            abuseCase.CodRestrictedAt ??= now;
            abuseCase.ReviewRequestedAt ??= now;
        }
        else if (suspiciousOrderCount >= CodRestrictionThreshold)
        {
            abuseCase.Status = StatusCodRestricted;
            abuseCase.CodRestrictedAt ??= now;
        }
        else if (abuseCase.Status is StatusNormal)
        {
            abuseCase.Status = StatusNormal;
        }

        await db.SaveChangesAsync(ct);
        return abuseCase;
    }

    private static async Task RestoreStrictCasesAsync(
        SEP490ToyStoreContext db,
        INotificationDispatcher dispatcher,
        DateTime now,
        CancellationToken ct)
    {
        var strictCases = await db.CustomerDeliveryAbuseCases
            .Where(x => x.Status == StatusAppealApprovedStrict
                     && x.StrictPeriodUntil.HasValue
                     && x.StrictPeriodUntil.Value <= now)
            .ToListAsync(ct);

        foreach (var abuseCase in strictCases)
        {
            var hasNewIncident = await db.Orders
                .AsNoTracking()
                .AnyAsync(o => o.AccountId == abuseCase.AccountId
                            && !o.IsDeleted
                            && (!abuseCase.CountingFrom.HasValue || o.OrderDate >= abuseCase.CountingFrom.Value)
                            && o.DeliveryFailCount >= 3
                            && o.PaymentMethod == "SHIP_COD"
                            && o.PaymentStatus != "PAID"
                            && o.LastGHNFailCode != null
                            && SuspiciousFailCodes.Contains(o.LastGHNFailCode),
                    ct);

            if (hasNewIncident)
            {
                continue;
            }

            abuseCase.Status = StatusNormal;
            abuseCase.WarningLevel = 0;
            abuseCase.SuspiciousOrderCount = 0;
            abuseCase.CountingFrom = now;
            abuseCase.CodRestrictedAt = null;
            abuseCase.StrictPeriodUntil = null;
            abuseCase.Note = "Restored to normal after one month without new delivery-abuse incidents.";
            abuseCase.UpdatedAt = now;

            await dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = abuseCase.AccountId,
                RecipientType = RecipientTypes.Customer,
                NotificationType = NotificationTypes.System,
                Title = "Your account status has returned to normal",
                Message = "Your stricter delivery monitoring period has ended with no new unpaid COD delivery failures. Cash on Delivery is available again.",
                SendBell = true,
                SendEmail = true,
                ActionTarget = "/profile/orders",
                Payload = new Dictionary<string, object>
                {
                    ["accountId"] = abuseCase.AccountId,
                    ["policy"] = "DELIVERY_ABUSE_RESTORED"
                },
                IdempotencyKey = $"customer-delivery-abuse-restored:{abuseCase.AccountId}:{now:yyyyMMdd}"
            }, ct);
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task RestoreCodRestrictionCasesAsync(
        SEP490ToyStoreContext db,
        INotificationDispatcher dispatcher,
        DateTime now,
        TimeSpan reviewDelay,
        TimeSpan probationPeriod,
        CancellationToken ct)
    {
        var reviewBefore = now.Subtract(reviewDelay);
        var codRestrictedCases = await db.CustomerDeliveryAbuseCases
            .Where(x => x.Status == StatusCodRestricted
                     && x.CodRestrictedAt.HasValue
                     && x.CodRestrictedAt.Value <= reviewBefore)
            .ToListAsync(ct);

        foreach (var abuseCase in codRestrictedCases)
        {
            var currentSuspiciousCount = await CountSuspiciousCodFailOrdersAsync(db, abuseCase.AccountId, abuseCase.CountingFrom, ct);
            if (currentSuspiciousCount > abuseCase.SuspiciousOrderCount)
            {
                abuseCase.SuspiciousOrderCount = currentSuspiciousCount;
                abuseCase.UpdatedAt = now;
                continue;
            }

            abuseCase.Status = StatusCodProbation;
            abuseCase.WarningLevel = CodRestrictionThreshold;
            abuseCase.SuspiciousOrderCount = Math.Max(abuseCase.SuspiciousOrderCount, CodRestrictionThreshold);
            abuseCase.CountingFrom = now;
            abuseCase.CodRestrictedAt = null;
            abuseCase.Note = $"COD restored after {reviewDelay.TotalDays:0} day(s) without new suspicious COD delivery failures. Customer is now under COD probation for {probationPeriod.TotalDays:0} day(s).";
            abuseCase.UpdatedAt = now;

            await dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = abuseCase.AccountId,
                RecipientType = RecipientTypes.Customer,
                NotificationType = NotificationTypes.System,
                Title = "Cash on Delivery is available again",
                Message = "Cash on Delivery has been restored for your account because there were no new suspicious COD delivery failures during the review period. Please note that your account is under COD probation; another suspicious COD delivery failure may trigger admin review.",
                SendBell = true,
                SendEmail = true,
                ActionTarget = "/profile/orders",
                Payload = new Dictionary<string, object>
                {
                    ["accountId"] = abuseCase.AccountId,
                    ["policy"] = "DELIVERY_ABUSE_COD_RESTORED"
                },
                IdempotencyKey = $"customer-delivery-abuse-cod-restored:{abuseCase.AccountId}:{now:yyyyMMdd}"
            }, ct);
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task RestoreCodProbationCasesAsync(
        SEP490ToyStoreContext db,
        INotificationDispatcher dispatcher,
        DateTime now,
        TimeSpan probationPeriod,
        CancellationToken ct)
    {
        var probationStartedBefore = now.Subtract(probationPeriod);
        var probationCases = await db.CustomerDeliveryAbuseCases
            .Where(x => x.Status == StatusCodProbation
                     && x.CountingFrom.HasValue
                     && x.CountingFrom.Value <= probationStartedBefore)
            .ToListAsync(ct);

        foreach (var abuseCase in probationCases)
        {
            var hasNewIncident = await CountSuspiciousCodFailOrdersAsync(db, abuseCase.AccountId, abuseCase.CountingFrom, ct) > 0;
            if (hasNewIncident)
            {
                continue;
            }

            abuseCase.Status = StatusNormal;
            abuseCase.WarningLevel = 0;
            abuseCase.SuspiciousOrderCount = 0;
            abuseCase.CountingFrom = now;
            abuseCase.Note = "Restored to normal after COD probation ended without new suspicious COD delivery failures.";
            abuseCase.UpdatedAt = now;

            await dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = abuseCase.AccountId,
                RecipientType = RecipientTypes.Customer,
                NotificationType = NotificationTypes.System,
                Title = "Your COD status has returned to normal",
                Message = "Your COD probation period has ended with no new suspicious COD delivery failures. Your account has returned to normal delivery status.",
                SendBell = true,
                SendEmail = true,
                ActionTarget = "/profile/orders",
                Payload = new Dictionary<string, object>
                {
                    ["accountId"] = abuseCase.AccountId,
                    ["policy"] = "DELIVERY_ABUSE_COD_PROBATION_CLEARED"
                },
                IdempotencyKey = $"customer-delivery-abuse-cod-probation-cleared:{abuseCase.AccountId}:{now:yyyyMMdd}"
            }, ct);
        }

        await db.SaveChangesAsync(ct);
    }

    private static Task<int> CountSuspiciousCodFailOrdersAsync(
        SEP490ToyStoreContext db,
        int accountId,
        DateTime? countingFrom,
        CancellationToken ct)
    {
        return db.Orders
            .AsNoTracking()
            .CountAsync(o => o.AccountId == accountId
                          && !o.IsDeleted
                          && (!countingFrom.HasValue || o.OrderDate >= countingFrom.Value)
                          && o.DeliveryFailCount >= 3
                          && o.PaymentMethod == "SHIP_COD"
                          && o.PaymentStatus != "PAID"
                          && o.LastGHNFailCode != null
                          && SuspiciousFailCodes.Contains(o.LastGHNFailCode),
                ct);
    }

    private static Task NotifyPermanentBlockAsync(
        INotificationDispatcher dispatcher,
        int accountId,
        int suspiciousOrderCount,
        CancellationToken ct)
    {
        return dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = accountId,
            RecipientType = RecipientTypes.Customer,
            NotificationType = NotificationTypes.System,
            Title = "Your account has been permanently locked",
            Message = "Your account has been permanently locked because another unpaid COD delivery failure occurred during the stricter monitoring period after your appeal was accepted. This decision cannot be appealed.",
            SendBell = true,
            SendEmail = true,
            ActionTarget = "/profile/orders",
            Payload = new Dictionary<string, object>
            {
                ["accountId"] = accountId,
                ["suspiciousOrderCount"] = suspiciousOrderCount,
                ["policy"] = "DELIVERY_ABUSE_PERMANENT_LOCK"
            },
            IdempotencyKey = $"customer-delivery-abuse-permanent-lock:{accountId}"
        }, ct);
    }

    private static Task NotifyCustomerByThresholdAsync(
        INotificationDispatcher dispatcher,
        int accountId,
        string accountName,
        int suspiciousOrderCount,
        IReadOnlyCollection<string> orderCodes,
        CancellationToken ct)
    {
        var threshold = suspiciousOrderCount >= ManualReviewThreshold
            ? ManualReviewThreshold
            : suspiciousOrderCount >= CodRestrictionThreshold
                ? CodRestrictionThreshold
                : FirstWarningThreshold;

        var (title, message) = threshold switch
        {
            FirstWarningThreshold => (
                "Important notice about your COD deliveries",
                "We noticed that one of your unpaid Cash on Delivery orders reached repeated delivery failures. Please make sure your delivery information is accurate and that you are available to receive future orders."),
            CodRestrictionThreshold => (
                "COD payment has been temporarily disabled",
                "We noticed a second unpaid COD order with repeated delivery failures. Cash on Delivery is now temporarily unavailable for your account. You can still place orders using QR bank transfer or wallet payment."),
            _ => (
                "Your account is under admin review",
                "Your account has reached three unpaid COD orders with repeated delivery failures. Our admin team has been notified and may lock the account after review. If you believe this is incorrect, please contact support by email.")
        };

        var orderList = orderCodes.Count > 0
            ? $" Related orders: {string.Join(", ", orderCodes)}."
            : string.Empty;

        return dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = accountId,
            RecipientType = RecipientTypes.Customer,
            NotificationType = NotificationTypes.System,
            Title = title,
            Message = $"{message}{orderList}",
            SendBell = true,
            SendEmail = true,
            ActionTarget = "/profile/orders",
            Payload = new Dictionary<string, object>
            {
                ["accountId"] = accountId,
                ["accountName"] = accountName,
                ["suspiciousOrderCount"] = suspiciousOrderCount,
                ["warningLevel"] = threshold,
                ["orderCodes"] = orderCodes
            },
            IdempotencyKey = $"customer-delivery-abuse-warning:{accountId}:level-{threshold}"
        }, ct);
    }
}
