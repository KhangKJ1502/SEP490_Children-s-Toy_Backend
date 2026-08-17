using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using ToyStore.Application.DTOs.Refunds;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.Worker.Workers;

/// <summary>
/// Background Service định kỳ quét các yêu cầu hoàn tiền bị treo thanh toán phí hoàn trả (Stale Unpaid Refunds) quá hạn 48 giờ.
/// Tự động cập nhật CustomerResponse = "Disposed", đánh dấu hoàn tất phí và chuyển trạng thái yêu cầu sang RefundCompleted (tiêu hủy sản phẩm hỏng thay vì gửi trả).
/// Chu kỳ quét: Mỗi 5 phút một lần.
/// </summary>
public class RefundTimeoutJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<RefundTimeoutJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5); // Quét định kỳ mỗi 5 phút

    /// <summary>
    /// Khởi tạo worker RefundTimeoutJob với ServiceProvider và Logger.
    /// </summary>
    public RefundTimeoutJob(IServiceProvider services, ILogger<RefundTimeoutJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    /// <summary>
    /// Vòng lặp thực thi chính của BackgroundService, kích hoạt phương thức RunAsync sau mỗi khoảng thời gian _interval (5 phút).
    /// </summary>
    /// <param name="stoppingToken">Token báo hiệu dừng ứng dụng worker.</param>
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
                _logger.LogError(ex, "RefundTimeoutJob executed with error.");
            }
            await Task.Delay(_interval, stoppingToken);
        }
    }

    /// <summary>
    /// Logic nghiệp vụ kiểm tra và xử lý tự động tiêu hủy cho các yêu cầu hoàn tiền quá hạn 48 giờ.
    /// </summary>
    /// <param name="ct">Token hủy tác vụ bất đồng bộ.</param>
    private async Task RunAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var refundService = scope.ServiceProvider.GetRequiredService<IRefundService>();

        var cutoff = DateTime.UtcNow;
        // 1. Tìm danh sách các yêu cầu hoàn tiền chưa thanh toán phí gửi lại và đã quá hạn chót (48h)
        var staleRefunds = await uow.Refunds.GetStaleUnpaidRefundsAsync(cutoff, ct);

        if (staleRefunds.Count > 0)
        {
            _logger.LogInformation("RefundTimeoutJob: Found {Count} stale unpaid refunds past deadline.", staleRefunds.Count);
        }

        // 2. Duyệt qua từng yêu cầu hoàn tiền quá hạn để xử lý tiêu hủy tự động
        foreach (var refund in staleRefunds)
        {
            try
            {
                _logger.LogInformation("RefundTimeoutJob: Auto-disposing stale unpaid refund {Id} (Code: {Code}) due to 48h timeout.", refund.RefundId, refund.RefundCode);

                // Cập nhật phản hồi khách hàng thành Disposed và ghi nhận đã xử lý phí
                refund.CustomerResponse = "Disposed";
                refund.ReturnToCustomerFeePaid = true;
                refund.UpdatedAt = DateTime.UtcNow;
                await uow.SaveChangesAsync(ct);

                // Tạo DTO chuyển trạng thái sang RefundCompleted kèm ghi chú hệ thống
                var completeDto = new UpdateRefundStatusDto
                {
                    Status = "RefundCompleted",
                    AdminNote = "System Auto-Completed: Return shipping fee shortfall unpaid after 48h. Damaged products marked as Disposed."
                };

                var result = await refundService.UpdateRefundStatusAsync(
                    staffId: refund.ApprovedBy ?? 1, // Fallback to system user ID
                    roleId: 2, // Staff role
                    refundId: refund.RefundId,
                    dto: completeDto,
                    isAdmin: true,
                    cancellationToken: ct);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("RefundTimeoutJob: Successfully auto-completed stale refund {Id}.", refund.RefundId);
                }
                else
                {
                    _logger.LogError("RefundTimeoutJob: Failed to complete stale refund {Id}: {Error}", refund.RefundId, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RefundTimeoutJob: Exception while processing stale refund {Id}.", refund.RefundId);
            }
        }
    }
}
