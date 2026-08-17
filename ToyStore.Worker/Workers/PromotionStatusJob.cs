using Microsoft.EntityFrameworkCore;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Notifications;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Background Service định kỳ tự động quét và chuyển đổi trạng thái vòng đời của Chương trình Khuyến mãi (Promotion),
/// Khung giờ Flash Sale (PromotionTimeSlot) và Mã giảm giá (Voucher) dựa trên thời gian thực tế.
/// Tần suất thực thi: Mỗi 1 phút một lần.
/// </summary>
public class PromotionStatusJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PromotionStatusJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Khởi tạo background job đồng bộ trạng thái khuyến mãi.
    /// </summary>
    /// <param name="services">Service provider dùng để tạo scope nạp DbContext.</param>
    /// <param name="logger">Logger ghi log hoạt động của worker.</param>
    public PromotionStatusJob(IServiceProvider services, ILogger<PromotionStatusJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    /// <summary>
    /// Vòng lặp chính của BackgroundService, thực thi RunAsync định kỳ mỗi 1 phút cho đến khi ứng dụng dừng.
    /// </summary>
    /// <param name="stoppingToken">Token hủy tác vụ khi tắt ứng dụng.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try 
            { 
                await RunAsync(stoppingToken); 
            }
            catch (Exception ex) 
            { 
                _logger.LogError(ex, "PromotionStatusJob gặp lỗi trong quá trình thực thi"); 
            }

            // Chờ 1 phút trước lần quét tiếp theo
            await Task.Delay(_interval, stoppingToken);
        }
    }

    /// <summary>
    /// Quét và cập nhật trạng thái các thực thể Promotion, TimeSlot và Voucher:
    /// 1. Promotion: Scheduled -> Active (khi StartDate &lt;= nowUtc)
    /// 2. Promotion: Active -> Expired (khi EndDate &lt; nowUtc)
    /// 3. TimeSlot: Scheduled -> Active (khi StartAt &lt;= nowUtc)
    /// 4. TimeSlot: Active -> Expired (khi EndAt &lt; nowUtc)
    /// 5. Voucher: Scheduled -> Active (khi StartDate &lt;= nowUtc)
    /// 6. Voucher: Active/Pending/Scheduled -> Expired (khi EndDate &lt; nowUtc)
    /// </summary>
    /// <param name="ct">Token hủy tác vụ bất đồng bộ.</param>
    private async Task RunAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();
        
        bool success = true;
        string? message = null;
        
        try
        {
            var nowUtc = DateTime.UtcNow;

            // 1. Promotion: Chuyển từ Scheduled -> Active khi đã đến thời điểm bắt đầu
            var scheduledPromotions = await db.Promotions
                .Where(p => p.Status == "Scheduled" && !p.IsDeleted && p.StartDate <= nowUtc)
                .ToListAsync(ct);
            foreach (var p in scheduledPromotions)
            {
                p.Status = "Active";
                p.UpdatedAt = nowUtc;
            }

            // 2. Promotion: Chuyển từ Active -> Expired khi đã quá thời điểm kết thúc
            var activePromotions = await db.Promotions
                .Where(p => p.Status == "Active" && !p.IsDeleted && p.EndDate < nowUtc)
                .ToListAsync(ct);
            foreach (var p in activePromotions)
            {
                p.Status = "Expired";
                p.UpdatedAt = nowUtc;
            }

            // 3. TimeSlot: Chuyển khung giờ Flash Sale từ Scheduled -> Active khi đã đến StartAt
            var scheduledSlots = await db.PromotionTimeSlots
                .Where(s => s.Status == "Scheduled" && s.StartAt <= nowUtc)
                .ToListAsync(ct);
            foreach (var s in scheduledSlots)
            {
                s.Status = "Active";
                s.UpdatedAt = nowUtc;
            }

            // 4. TimeSlot: Chuyển khung giờ Flash Sale từ Active -> Expired khi đã qua EndAt
            var activeSlots = await db.PromotionTimeSlots
                .Where(s => s.Status == "Active" && s.EndAt < nowUtc)
                .ToListAsync(ct);
            foreach (var s in activeSlots)
            {
                s.Status = "Expired";
                s.UpdatedAt = nowUtc;
            }

            // 5. Voucher: Chuyển Voucher từ Scheduled -> Active khi đã đến StartDate
            var scheduledVouchers = await db.Vouchers
                .Where(v => v.Status == "Scheduled" && !v.IsDeleted && v.StartDate <= nowUtc)
                .ToListAsync(ct);
            foreach (var v in scheduledVouchers)
            {
                v.Status = "Active";
                v.UpdatedAt = nowUtc;
            }

            // 6. Voucher: Chuyển Voucher (Active/Pending/Scheduled) -> Expired khi đã quá EndDate
            var activeVouchers = await db.Vouchers
                .Where(v => (v.Status == "Active" || v.Status == "Pending" || v.Status == "Scheduled") && !v.IsDeleted && v.EndDate < nowUtc)
                .ToListAsync(ct);
            foreach (var v in activeVouchers)
            {
                v.Status = "Expired";
                v.UpdatedAt = nowUtc;
            }
            
            int totalUpdated = scheduledPromotions.Count + activePromotions.Count + scheduledSlots.Count + activeSlots.Count + scheduledVouchers.Count + activeVouchers.Count;
            if (totalUpdated > 0)
            {
                // Lưu tất cả thay đổi vào cơ sở dữ liệu
                await db.SaveChangesAsync(ct);
                message = $"Updated {scheduledPromotions.Count} scheduled promos, {activePromotions.Count} active promos, {scheduledSlots.Count} scheduled slots, {activeSlots.Count} active slots, {scheduledVouchers.Count} scheduled vouchers, {activeVouchers.Count} active vouchers.";
                _logger.LogInformation("PromotionStatusJob: {Message}", message);
            }
            else
            {
                message = "No status updates required.";
            }
        }
        catch (Exception ex)
        {
            success = false;
            message = ex.Message;
            _logger.LogError(ex, "PromotionStatusJob failed");
        }

        // Ghi log kết quả thực thi công việc vào telemetry
        await BackgroundJobTelemetry.RecordAsync(
            scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>(),
            "PromotionStatusJob", success, message, _logger, ct);
    }
}
