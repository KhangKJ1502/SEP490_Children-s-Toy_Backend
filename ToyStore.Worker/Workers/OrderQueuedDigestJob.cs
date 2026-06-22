using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Digest email cho "Awaiting Assignment" orders — v2 (Redis watermark + lock).
///
/// Cơ chế:
///   1. Distributed lock (Redis SET NX EX): chỉ 1 instance chạy tại một thời điểm.
///   2. Watermark (Redis key): lưu mốc thời gian lần cuối đã xử lý để không bỏ sót
///      window khi Worker restart.
///   3. Threshold check: nếu số đơn trong window hiện tại vượt ngưỡng → gửi sớm
///      không cần đợi hết chu kỳ.
///   4. Escalation: đơn pending > EscalateAfterMinutes → đánh dấu "⚠️ Quá hạn" trong digest.
///   5. Chỉ gửi khi có đơn IsResolved=false → không spam khi đã tự assign xong.
/// </summary>
public class OrderQueuedDigestJob : BackgroundService
{
    
        // BUG FIX #5: giới hạn số đơn tối đa để tránh email khổng lồ / OOM
        // Nếu vượt ngưỡng, số dư sẽ được xử lý ở chu kỳ tiếp theo (watermark sẽ advance dần)
        const int MaxEntriesPerDigest = 50;
    // Redis keys
    private const string LockKey      = "digest:order-queued:lock";
    private const string WatermarkKey = "digest:order-queued:watermark";

    // Instance ID duy nhất — tránh dùng MachineName vì có thể trùng trong Docker
    private static readonly string InstanceId = Guid.NewGuid().ToString("N")[..8];

    private readonly IServiceProvider             _services;
    private readonly ILogger<OrderQueuedDigestJob> _logger;
    private readonly ITimeProvider                _timeProvider;
    private readonly IConfiguration              _configuration;
    private readonly TimeSpan                    _interval;
    private readonly int                         _urgentThreshold;
    private readonly int                         _escalateAfterMinutes;
    private bool                                 _lastSendFailed; // cooldown flag

    private static readonly Dictionary<string, string> ReasonLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["NO_STAFF_ON_DUTY"] = "No sales staff on duty",
        ["ALL_STAFF_FULL"]   = "All sales staff at full capacity",
        ["NO_MERCH_ON_DUTY"] = "No merchandise staff on duty",
        ["ALL_MERCH_FULL"]   = "All merchandise staff at full capacity",
        ["BOTH_FULL"]        = "No available shifts to assign",
    };

    public OrderQueuedDigestJob(
        IServiceProvider services,
        ILogger<OrderQueuedDigestJob> logger,
        ITimeProvider timeProvider,
        IConfiguration configuration)
    {
        _services             = services;
        _logger               = logger;
        _timeProvider         = timeProvider;
        _configuration        = configuration;

        _interval             = TimeSpan.FromMinutes(
            configuration.GetValue<int?>("DigestEmail:OrderQueuedIntervalMinutes") ?? 15);
        _urgentThreshold      = configuration.GetValue<int?>("DigestEmail:UrgentThreshold") ?? 10;
        _escalateAfterMinutes = configuration.GetValue<int?>("DigestEmail:EscalateAfterMinutes") ?? 30;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "OrderQueuedDigestJob started — InstanceId={Id} Interval={Interval} Threshold={Threshold} EscalateAfter={Escalate}min",
            InstanceId, _interval, _urgentThreshold, _escalateAfterMinutes);

        // Guard: escalateAfterMinutes nên lớn hơn interval để tránh overlap
        if (_escalateAfterMinutes <= (int)_interval.TotalMinutes)
        {
            _logger.LogWarning(
                "OrderQueuedDigestJob: EscalateAfterMinutes ({Esc}) <= IntervalMinutes ({Int}) — escalation sẽ không hoạt động đúng. Nên đặt EscalateAfterMinutes > IntervalMinutes.",
                _escalateAfterMinutes, (int)_interval.TotalMinutes);
        }

        // Khởi tạo watermark nếu chưa có (lần đầu chạy)
        await InitWatermarkAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // BUG FIX #1: nếu lần trước gửi fail, bắt buộc delay _interval
                // dù threshold vẫn cao, tránh busy-loop tấn công SMTP liên tục
                if (_lastSendFailed)
                {
                    _logger.LogInformation("OrderQueuedDigestJob: previous send failed — cooling down for {Interval}", _interval);
                    _lastSendFailed = false;
                    await Task.Delay(_interval, stoppingToken);
                }
                else
                {
                    var shouldRunEarly = await ShouldRunEarlyAsync(stoppingToken);
                    if (!shouldRunEarly)
                    {
                        await Task.Delay(_interval, stoppingToken);
                    }
                }

                await RunWithLockAsync(stoppingToken);
            }
            catch (OperationCanceledException) { /* shutdown graceful */ }
            catch (Exception ex)
            {
                _lastSendFailed = true; // đảm bảo cooldown sau lỗi bất ngờ
                _logger.LogError(ex, "OrderQueuedDigestJob unhandled error");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Distributed lock wrapper
    // ─────────────────────────────────────────────────────────────────────────

    private async Task RunWithLockAsync(CancellationToken ct)
    {
        using var scope  = _services.CreateScope();
        var redis        = scope.ServiceProvider.GetRequiredService<IRedisService>();

        // TTL của lock = interval + 5 phút buffer để tránh deadlock nếu job crash
        var lockTtl      = _interval + TimeSpan.FromMinutes(5);
        var instanceId   = InstanceId; // BUG FIX #3: Guid unique per process, tránh MachineName trùng trong Docker

        var acquired = await redis.SetIfNotExistsAsync(LockKey, instanceId, lockTtl);
        if (!acquired)
        {
            _logger.LogDebug("OrderQueuedDigestJob: lock held by another instance, skipping this cycle");
            return;
        }

        try
        {
            await RunAsync(redis, ct);
        }
        finally
        {
            // Chỉ xóa lock nếu do chính instance này hold (tránh xóa nhầm của instance khác)
            var holder = await redis.GetAsync(LockKey);
            if (holder == instanceId)
            {
                await redis.DeleteAsync(LockKey);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Core logic
    // ─────────────────────────────────────────────────────────────────────────

    private async Task RunAsync(IRedisService redis, CancellationToken ct)
    {
        using var scope  = _services.CreateScope();
        var db           = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();
        var emailSender  = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        // Đọc watermark — nếu không có thì dùng (now - interval) để bắt đầu từ window hiện tại
        var watermarkStr = await redis.GetAsync(WatermarkKey);
        var windowStart  = watermarkStr is not null
            ? DateTime.Parse(watermarkStr, null, System.Globalization.DateTimeStyles.RoundtripKind)
            : _timeProvider.UtcNow - _interval;
        var windowEnd    = _timeProvider.UtcNow;

        _logger.LogInformation(
            "OrderQueuedDigestJob: processing window [{Start:u}, {End:u})",
            windowStart, windowEnd);

        // BUG FIX #6: clock jump backward (NTP correction) — nếu windowStart > windowEnd
        // thì window bị đảo ngược, bỏ qua và không advance watermark
        if (windowStart >= windowEnd)
        {
            _logger.LogWarning(
                "OrderQueuedDigestJob: windowStart ({Start:u}) >= windowEnd ({End:u}) — clock jump detected, skipping cycle",
                windowStart, windowEnd);
            return;
        }

        // ── 1. Đơn mới trong window vừa qua (chưa resolved) ──────────────
        var newEntries = await db.OrderQueues
            .AsNoTracking()
            .Include(x => x.Order)
            .Where(x => x.QueuedAt >= windowStart
                     && x.QueuedAt <  windowEnd
                     && !x.IsResolved)
            .OrderBy(x => x.QueuedAt)
            .Take(MaxEntriesPerDigest)          // cap: tối đa 50 đơn
            .ToListAsync(ct);

        // phần còn lại để dành slot cho escalated
        var remainingSlots = MaxEntriesPerDigest - newEntries.Count;

        // ── 2. Đơn escalation: pending lâu quá ngưỡng, chưa resolved ────────
        var escalateCutoff = windowEnd - TimeSpan.FromMinutes(_escalateAfterMinutes);
        var escalatedEntries = await db.OrderQueues
            .AsNoTracking()
            .Include(x => x.Order)
            .Where(x => x.QueuedAt <  escalateCutoff
                     && x.QueuedAt <  windowStart
                     && !x.IsResolved)
            .OrderBy(x => x.QueuedAt)
            .Take(remainingSlots > 0 ? remainingSlots : 0)  // dùng slot còn lại
            .ToListAsync(ct);

        var totalCount = newEntries.Count + escalatedEntries.Count;

        if (totalCount == 0)
        {
            _logger.LogDebug(
                "OrderQueuedDigestJob: no unresolved entries in window or escalation — advancing watermark");
            await AdvanceWatermarkAsync(redis, windowEnd);
            return;
        }

        _logger.LogInformation(
            "OrderQueuedDigestJob: {New} new + {Escalated} escalated orders. Sending digest...",
            newEntries.Count, escalatedEntries.Count);

        // ── 3. Gửi email cho từng Admin ───────────────────────────────────────
        var admins = await db.Accounts
            .AsNoTracking()
            .Where(a => a.RoleId == 2 && !a.IsDeleted)
            .ToListAsync(ct);

        if (admins.Count == 0)
        {
            _logger.LogWarning("OrderQueuedDigestJob: no admin accounts found");
            await AdvanceWatermarkAsync(redis, windowEnd);
            return;
        }

        var adminOrdersUrl = (_configuration["FrontendUrls:Admin"] ?? "http://localhost:3001")
                             + "/admin/schedules";

        var windowVnStart = _timeProvider.ToVnTime(windowStart);
        var windowVnEnd   = _timeProvider.ToVnTime(windowEnd);

        var subject  = escalatedEntries.Count > 0
            ? $"[ToyStore] 🚨 {totalCount} orders pending assignment — {escalatedEntries.Count} overdue ({windowVnEnd:HH:mm})"
            : $"[ToyStore] ⚠️ {newEntries.Count} orders pending assignment ({windowVnStart:HH:mm} – {windowVnEnd:HH:mm})";

        var htmlBody = BuildDigestHtml(newEntries, escalatedEntries, windowVnStart, windowVnEnd, adminOrdersUrl);

        var allSucceeded = true;
        // Timeout per email: 30 giây — tránh treo vĩnh viễn nếu SMTP không phản hồi
        const int EmailSendTimeoutSeconds = 30;

        foreach (var admin in admins)
        {
            try
            {
                using var sendCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                sendCts.CancelAfter(TimeSpan.FromSeconds(EmailSendTimeoutSeconds));

                await emailSender.SendAsync(
                    new EmailMessage(
                        ToEmail:  admin.Email,
                        ToName:   admin.AccountName,
                        Subject:  subject,
                        HtmlBody: htmlBody),
                    sendCts.Token); // ← linked token: cancel khi app shutdown HOẶC timeout 30s

                _logger.LogInformation(
                    "OrderQueuedDigestJob: digest sent — Admin={Email} New={New} Escalated={Esc}",
                    admin.Email, newEntries.Count, escalatedEntries.Count);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // ct chưa cancel → đây là timeout 30s, không phải app shutdown
                allSucceeded = false;
                _logger.LogError(
                    "OrderQueuedDigestJob: SMTP timeout (>{Timeout}s) — Admin={Email}",
                    EmailSendTimeoutSeconds, admin.Email);
            }
            catch (Exception ex)
            {
                allSucceeded = false;
                _logger.LogError(ex,
                    "OrderQueuedDigestJob: FAILED to send digest — Admin={Email} New={New} Escalated={Esc}",
                    admin.Email, newEntries.Count, escalatedEntries.Count);
            }
        }

        // Chỉ advance watermark nếu tất cả admin nhận được email thành công
        // → nếu có admin fail, lần chạy sau sẽ retry với window bắt đầu từ watermark cũ
        if (allSucceeded)
        {
            _lastSendFailed = false;
            await AdvanceWatermarkAsync(redis, windowEnd);
        }
        else
        {
            _lastSendFailed = true; // BUG FIX #1: signal cooldown cho vòng lặp chính
            _logger.LogWarning(
                "OrderQueuedDigestJob: some admins failed to receive digest — watermark NOT advanced. Will retry after cooldown.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Threshold check — gửi sớm nếu vượt ngưỡng
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<bool> ShouldRunEarlyAsync(CancellationToken ct)
    {
        try
        {
            using var scope  = _services.CreateScope();
            var redis        = scope.ServiceProvider.GetRequiredService<IRedisService>();
            var db           = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();

            var watermarkStr = await redis.GetAsync(WatermarkKey);
            var windowStart  = watermarkStr is not null
                ? DateTime.Parse(watermarkStr, null, System.Globalization.DateTimeStyles.RoundtripKind)
                : _timeProvider.UtcNow - _interval;

            var count = await db.OrderQueues
                .AsNoTracking()
                .CountAsync(x => x.QueuedAt >= windowStart && !x.IsResolved, ct);

            if (count >= _urgentThreshold)
            {
                _logger.LogInformation(
                    "OrderQueuedDigestJob: urgent threshold reached ({Count}/{Threshold}) — running early",
                    count, _urgentThreshold);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OrderQueuedDigestJob: threshold check failed, defaulting to normal interval");
        }

        return false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Watermark helpers
    // ─────────────────────────────────────────────────────────────────────────

    private async Task InitWatermarkAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _services.CreateScope();
            var redis       = scope.ServiceProvider.GetRequiredService<IRedisService>();

            var existing = await redis.GetAsync(WatermarkKey);  // BUG FIX #7: Redis sử dụng ct
            if (existing is null)
            {
                var initial = (_timeProvider.UtcNow - _interval).ToString("O");
                await redis.SetAsync(WatermarkKey, initial, TimeSpan.FromDays(30));
                _logger.LogInformation("OrderQueuedDigestJob: watermark initialized to {Value}", initial);
            }
            else
            {
                _logger.LogInformation("OrderQueuedDigestJob: watermark resumed from {Value}", existing);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OrderQueuedDigestJob: failed to initialize watermark (Redis unavailable?)");
        }
    }

    private async Task AdvanceWatermarkAsync(IRedisService redis, DateTime newWatermark)
    {
        try
        {
            await redis.SetAsync(WatermarkKey, newWatermark.ToString("O"), TimeSpan.FromDays(30));
            _logger.LogDebug("OrderQueuedDigestJob: watermark advanced to {Value:u}", newWatermark);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OrderQueuedDigestJob: failed to advance watermark");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HTML Builder
    // ─────────────────────────────────────────────────────────────────────────

    private string BuildDigestHtml(
        IReadOnlyList<ToyStore.Domain.Entities.OrderQueue> newEntries,
        IReadOnlyList<ToyStore.Domain.Entities.OrderQueue> escalatedEntries,
        DateTime windowStart,
        DateTime windowEnd,
        string adminOrdersUrl)
    {
        var year       = _timeProvider.VnNow.Year;
        var totalCount = newEntries.Count + escalatedEntries.Count;
        var hasEscalated = escalatedEntries.Count > 0;
        var intervalMin  = (int)_interval.TotalMinutes;

        string BuildRow(ToyStore.Domain.Entities.OrderQueue entry, bool isOverdue)
        {
            var orderCode   = entry.Order?.OrderCode ?? $"#{entry.OrderId}";
            var reasonLabel = ReasonLabels.TryGetValue(entry.Reason, out var lbl) ? lbl : entry.Reason;
            var queuedVn    = _timeProvider.ToVnTime(entry.QueuedAt).ToString("HH:mm dd/MM");
            var overdueTag  = isOverdue
                ? "<span style=\"display:inline-block;background:#fee2e2;color:#dc2626;padding:2px 8px;border-radius:4px;font-size:11px;font-weight:700;margin-left:6px;\">OVERDUE</span>"
                : "";
            var rowBg = isOverdue ? "background-color:#fff5f5;" : "";

            return $"""
                <tr style="{rowBg}">
                  <td style="padding:10px 14px;border-bottom:1px solid #f0f0f0;font-size:13px;color:#1e293b;font-weight:600;">
                    {System.Net.WebUtility.HtmlEncode(orderCode)}{overdueTag}
                  </td>
                  <td style="padding:10px 14px;border-bottom:1px solid #f0f0f0;font-size:13px;color:#64748b;">
                    {System.Net.WebUtility.HtmlEncode(reasonLabel)}
                  </td>
                  <td style="padding:10px 14px;border-bottom:1px solid #f0f0f0;font-size:12px;color:#94a3b8;text-align:right;white-space:nowrap;">
                    {queuedVn}
                  </td>
                </tr>
                """;
        }

        var rowsHtml = new System.Text.StringBuilder();

        // Escalated orders trước (ưu tiên hiển thị)
        foreach (var entry in escalatedEntries)
            rowsHtml.Append(BuildRow(entry, isOverdue: true));

        // New orders trong window
        foreach (var entry in newEntries)
            rowsHtml.Append(BuildRow(entry, isOverdue: false));

        var headerBg      = hasEscalated ? "#b91c1c" : "#dc2626";
        var headerSubBg   = hasEscalated ? "#ef4444" : "#dc2626";
        var headerEmoji   = hasEscalated ? "🚨" : "⚠️";
        var headerTitle   = hasEscalated
            ? "Overdue orders — Action required!"
            : "Orders pending shift assignment";

        var escalatedBanner = hasEscalated ? $"""
            <tr>
              <td style="padding:12px 40px 0;">
                <div style="background:#fee2e2;border:1px solid #fca5a5;border-radius:10px;padding:12px 16px;font-size:13px;color:#7f1d1d;">
                  🚨 <strong>{escalatedEntries.Count} order(s)</strong> waiting for more than {_escalateAfterMinutes} minutes — please process with priority!
                </div>
              </td>
            </tr>
            """ : "";

        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="UTF-8"/>
              <meta name="viewport" content="width=device-width,initial-scale=1.0"/>
              <title>Order Queue Digest</title>
            </head>
            <body style="margin:0;padding:0;background-color:#f4f4f5;font-family:'Segoe UI',Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#f4f4f5;padding:40px 0;">
                <tr><td align="center">
                  <table width="600" cellpadding="0" cellspacing="0"
                         style="background:#fff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);">

                    <!-- Header -->
                    <tr>
                      <td style="background:linear-gradient(135deg,{headerBg} 0%,{headerSubBg} 100%);padding:28px 40px;text-align:center;">
                        <div style="font-size:32px;margin-bottom:6px;">{headerEmoji}</div>
                        <h1 style="margin:0;color:#fff;font-size:20px;font-weight:700;">{headerTitle}</h1>
                        <p style="margin:6px 0 0;color:rgba(255,255,255,0.8);font-size:13px;">
                          {windowStart:HH:mm} – {windowEnd:HH:mm} (VN Time)
                        </p>
                      </td>
                    </tr>

                    <!-- Summary -->
                    <tr>
                      <td style="padding:20px 40px 0;">
                        <div style="background:#fef2f2;border:1px solid #fecaca;border-radius:10px;padding:14px 20px;text-align:center;">
                          <span style="font-size:28px;font-weight:700;color:#dc2626;">{totalCount}</span>
                          <span style="font-size:14px;color:#7f1d1d;margin-left:8px;">order(s) pending assignment</span>
                        </div>
                      </td>
                    </tr>

                    {escalatedBanner}

                    <!-- Table -->
                    <tr>
                      <td style="padding:20px 40px;">
                        <table width="100%" cellpadding="0" cellspacing="0"
                               style="border:1px solid #e2e8f0;border-radius:10px;overflow:hidden;">
                          <thead>
                            <tr style="background:#f8fafc;">
                              <th style="padding:10px 14px;text-align:left;font-size:11px;color:#64748b;text-transform:uppercase;letter-spacing:0.5px;">Order Code</th>
                              <th style="padding:10px 14px;text-align:left;font-size:11px;color:#64748b;text-transform:uppercase;letter-spacing:0.5px;">Reason</th>
                              <th style="padding:10px 14px;text-align:right;font-size:11px;color:#64748b;text-transform:uppercase;letter-spacing:0.5px;">Queued At</th>
                            </tr>
                          </thead>
                          <tbody>{rowsHtml}</tbody>
                        </table>
                      </td>
                    </tr>

                    <!-- CTA -->
                    <tr>
                      <td style="padding:0 40px 28px;text-align:center;">
                        <a href="{adminOrdersUrl}"
                           style="display:inline-block;background:#dc2626;color:#fff;text-decoration:none;
                                  padding:13px 34px;border-radius:28px;font-size:14px;font-weight:600;
                                  box-shadow:0 4px 12px rgba(220,38,38,0.3);">
                          View & Assign Manually →
                        </a>
                      </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                      <td style="background:#f8f9fa;padding:16px 40px;border-top:1px solid #e9ecef;text-align:center;">
                        <p style="margin:0;color:#94a3b8;font-size:11px;">
                          © {year} ToyStore — Automated digest email every {intervalMin} minutes.
                          Orders pending after {_escalateAfterMinutes} minutes will be marked as OVERDUE.
                        </p>
                      </td>
                    </tr>

                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;
    }
}
