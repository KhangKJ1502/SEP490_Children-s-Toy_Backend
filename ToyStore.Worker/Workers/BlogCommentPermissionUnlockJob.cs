using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Notifications;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Worker chạy nền định kỳ (mỗi 1 giờ) để mở lại quyền gửi bình luận Blog cho người dùng
/// khi thời gian khóa phạt tạm thời (banned period) đã hết hạn.
/// </summary>
public class BlogCommentPermissionUnlockJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<BlogCommentPermissionUnlockJob> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1); // Chu kỳ chạy: 1 giờ

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


    /// <summary>
    /// Thực hiện quét cơ sở dữ liệu để tự động mở khóa các tài khoản bị phạt tạm thời đã hết hạn.
    /// </summary>
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
            
            // 1. Tìm các tài khoản đang bị cấm bình luận (IsCommentBanned = true) mà thời gian hết hạn cấm đã trôi qua
            var expiredBans = await db.BlogCommentViolationCounts
                .Where(x => x.IsCommentBanned
                         && x.BanExpiresAt != null
                         && x.BanExpiresAt <= nowUtc)
                .ToListAsync(ct);

            // 2. Lặp qua các tài khoản đã hết hạn cấm để phục hồi lại trạng thái bình thường
            foreach (var state in expiredBans)
            {
                state.IsCommentBanned = false; // Bỏ cờ bị cấm bình luận
                state.BannedAt = null;         // Xóa ngày bắt đầu cấm
                state.BanExpiresAt = null;     // Xóa ngày hết hạn cấm
                state.ViolationCount = 0;      // Reset điểm số lần vi phạm về 0
                state.LastViolatedAt = null;   // Xóa mốc thời gian vi phạm cuối cùng
                state.UnbannedAt = nowUtc;     // Lưu mốc thời gian mở khóa tự động
                state.UnbannedBy = null;       // Được mở khóa tự động bởi hệ thống (null)
                state.UpdatedAt = nowUtc;      // Cập nhật ngày sửa đổi trạng thái
            }

            // 3. Nếu có tài khoản được mở khóa, lưu thay đổi vào Database và gửi thông báo hệ thống cho người dùng
            if (expiredBans.Count > 0)
            {
                await db.SaveChangesAsync(ct);

                foreach (var state in expiredBans)
                {
                    // Gửi thông báo dạng chuông để thông báo cho khách hàng biết quyền bình luận đã được phục hồi
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

        // 4. Lưu vết hoạt động chạy Job nền vào bảng đo lường (Telemetry)
        await BackgroundJobTelemetry.RecordAsync(
            scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>(),
            "BlogCommentPermissionUnlockJob", success, message, _logger, ct);
    }
}
