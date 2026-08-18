using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Worker.Workers;

public class OutboxProcessorJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<OutboxProcessorJob> _logger;
    private readonly ITimeProvider _timeProvider;
    private const int MaxAttempts = 5;
    private const int BatchSize = 20;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(10);

    public OutboxProcessorJob(
        IServiceProvider services, 
        ILogger<OutboxProcessorJob> logger,
        ITimeProvider timeProvider)
    {
        _services     = services;
        _logger       = logger;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessorJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in OutboxProcessorJob");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope   = _services.CreateScope();
        var db            = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();
        var handlers      = scope.ServiceProvider.GetServices<IOutboxEventHandler>()
                              .ToLookup(h => h.EventType, StringComparer.OrdinalIgnoreCase);

        var lockId    = Guid.NewGuid();
        var now       = _timeProvider.UtcNow;
        var staleTime = now.AddMinutes(-5);

        // ═══════════════════════════════════════════════════════════════
        // [FIX-1] Claim nguyên tử (Atomic) bằng UPDLOCK + READPAST
        // — Ngăn ngừa race condition khi có nhiều Worker instance chạy
        //   đồng thời. Mỗi instance sẽ nhận một tập sự kiện riêng biệt.
        // ═══════════════════════════════════════════════════════════════
        var claimedCount = await db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE TOP({BatchSize}) e
            SET    e.ProcessingLockId = {lockId},
                   e.ProcessingAt     = {now},
                   e.Attempts         = e.Attempts + 1
            FROM   [System].[DomainEventOutbox] AS e WITH (UPDLOCK, READPAST)
            WHERE  e.ProcessedOn IS NULL
              AND  e.Attempts    < {MaxAttempts}
              AND  (e.ProcessingAt IS NULL OR e.ProcessingAt < {staleTime})", ct);

        if (claimedCount == 0) return;

        // Chỉ load các sự kiện mà instance này đã claim thành công
        var batch = await db.DomainEventOutboxes
            .Where(e => e.ProcessingLockId == lockId)
            .OrderBy(e => e.OccurredOn)
            .ToListAsync(ct);

        foreach (var ev in batch)
        {
            var matchingHandlers = handlers[ev.EventType].ToList();
            if (matchingHandlers.Count == 0)
            {
                _logger.LogDebug("No handler for OutboxEvent EventType={EventType}", ev.EventType);
                ev.ProcessedOn = _timeProvider.UtcNow;
                // [FIX-4] Lưu DB sau mỗi sự kiện thay vì đợi xử lý hết lô (batch)
                await db.SaveChangesAsync(ct);
                continue;
            }

            // ═══════════════════════════════════════════════════════════
            // [FIX-3] Theo dõi lỗi của từng handler một cách độc lập
            // — Mỗi handler chạy trong try/catch riêng. Nếu một số handler
            //   thành công và một số thất bại, sự kiện vẫn được đánh dấu
            //   đã xử lý để tránh chạy lại handler đã thành công khi retry.
            // ═══════════════════════════════════════════════════════════
            var handlerErrors = new List<(string HandlerName, Exception Error)>();

            var data = new OutboxEventData(
                ev.EventId,
                ev.AggregateType,
                ev.AggregateId,
                ev.EventType,
                ev.Payload,
                ev.OccurredOn);

            foreach (var handler in matchingHandlers)
            {
                try
                {
                    await handler.HandleAsync(data, ct);
                }
                catch (Exception ex)
                {
                    handlerErrors.Add((handler.GetType().Name, ex));
                    _logger.LogError(ex,
                        "Handler {Handler} failed for OutboxEvent EventType={EventType} EventId={EventId}",
                        handler.GetType().Name, ev.EventType, ev.EventId);
                }
            }

            if (handlerErrors.Count == 0)
            {
                // Tất cả handler đều xử lý thành công
                ev.ProcessedOn = _timeProvider.UtcNow;
                ev.LastError   = null;
                _logger.LogInformation(
                    "Outbox event processed. EventType={EventType} EventId={EventId} Handlers={Count}",
                    ev.EventType, ev.EventId, matchingHandlers.Count);
            }
            else if (handlerErrors.Count < matchingHandlers.Count)
            {
                // Thành công một phần — đánh dấu đã xử lý để tránh chạy lại
                // các handler đã thành công; ghi lại log lỗi để kiểm tra
                ev.ProcessedOn = _timeProvider.UtcNow;
                ev.LastError   = string.Join(" | ", handlerErrors.Select(e =>
                    $"[{e.HandlerName}] {(e.Error.Message.Length > 200 ? e.Error.Message[..200] : e.Error.Message)}"));
                _logger.LogWarning(
                    "Outbox event partially processed. EventType={EventType} EventId={EventId} " +
                    "Succeeded={Succeeded}/{Total} FailedHandlers={Failed}",
                    ev.EventType, ev.EventId,
                    matchingHandlers.Count - handlerErrors.Count, matchingHandlers.Count,
                    string.Join(", ", handlerErrors.Select(e => e.HandlerName)));
            }
            else
            {
                // Tất cả handler đều thất bại — giữ ProcessedOn = null để chờ retry
                var firstError = handlerErrors[0].Error;
                ev.LastError = firstError.Message.Length > 500
                    ? firstError.Message[..500]
                    : firstError.Message;
                _logger.LogError(
                    "All handlers failed for OutboxEvent EventType={EventType} EventId={EventId} Attempt={Attempt}",
                    ev.EventType, ev.EventId, ev.Attempts);

                if (ev.Attempts >= MaxAttempts)
                {
                    // ═══════════════════════════════════════════════
                    // [FIX-2] Dùng CancellationToken.None bên trong
                    //   lambda để cảnh báo Admin không bị hủy khi
                    //   Worker đang ngắt (shutdown).
                    // [FIX-6] Truy vấn tất cả tài khoản Admin từ DB
                    //   thay vì hardcode AccountId = 1.
                    // ═══════════════════════════════════════════════
                    var capturedEventType = ev.EventType;
                    var capturedEventId   = ev.EventId;

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            using var alertScope = _services.CreateScope();
                            var dispatcher = alertScope.ServiceProvider
                                .GetRequiredService<INotificationDispatcher>();
                            var unitOfWork = alertScope.ServiceProvider
                                .GetRequiredService<IUnitOfWork>();
                            await NotifyAdminOutboxStuckAsync(
                                dispatcher, unitOfWork, capturedEventType, capturedEventId);
                        }
                        catch (Exception alertEx)
                        {
                            _logger.LogError(alertEx,
                                "Failed to send admin alert for stuck OutboxEvent EventId={EventId}",
                                capturedEventId);
                        }
                    }, CancellationToken.None);
                }
            }

            // [FIX-4] Lưu DB sau mỗi sự kiện để tránh mất trạng thái
            await db.SaveChangesAsync(ct);
        }
    }

    /// <summary>
    /// Gửi cảnh báo đến tất cả Admin khi Outbox event đạt số lần thử tối đa.
    /// </summary>
    private static async Task NotifyAdminOutboxStuckAsync(
        INotificationDispatcher dispatcher,
        IUnitOfWork unitOfWork,
        string eventType,
        Guid eventId)
    {
        // RoleId = 2 là Admin (1: Customer, 2: Admin, 3: Staff, 4: Merchandise)
        var admins = await unitOfWork.Accounts.GetByRoleIdsAsync(
            new byte[] { 2 }, CancellationToken.None);

        foreach (var admin in admins)
        {
            await dispatcher.DispatchAsync(new NotificationContext
            {
                RecipientAccountId = admin.AccountId,
                RecipientType      = RecipientTypes.Admin,
                NotificationType   = NotificationTypes.System,
                TemplateCode       = NotificationTemplates.AdminOutboxStuck,
                Placeholders       = new Dictionary<string, string>
                {
                    ["EventType"] = eventType,
                    ["EventId"]   = eventId.ToString(),
                },
                IdempotencyKey = $"system.outbox_max_attempts:{eventId}:{admin.AccountId}:WEB_BELL",
                SendBell       = true,
                SendEmail      = true,
            }, CancellationToken.None);
        }
    }
}
