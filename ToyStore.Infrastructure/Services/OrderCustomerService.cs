using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Constants;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Constants;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Services;

/// <summary>
/// Hủy đơn và tracking cho customer (và admin).
/// Tuân theo Clean Architecture: không dùng trực tiếp DbContext.
/// </summary>
public class OrderCustomerService : IOrderCustomerService
{
    private readonly IUnitOfWork _uow;
    private readonly IGhnClient _ghnClient;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ILogger<OrderCustomerService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly IOrderLifecycleService _orderLifecycle;

    private static readonly HashSet<string> NonCancellableGhnStatuses =
        new(StringComparer.OrdinalIgnoreCase)
        { "delivering", "money_collect_delivering", "delivered" };

    public OrderCustomerService(
        IUnitOfWork uow,
        IGhnClient ghnClient,
        IDomainEventPublisher eventPublisher,
        ILogger<OrderCustomerService> logger,
        ITimeProvider timeProvider,
        IOrderLifecycleService orderLifecycle)
    {
        _uow            = uow;
        _ghnClient      = ghnClient;
        _eventPublisher = eventPublisher;
        _logger         = logger;
        _timeProvider   = timeProvider;
        _orderLifecycle = orderLifecycle;
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    public async Task<Result<CancelOrderCustomerResponseDto>> CancelAsync(
        int orderId,
        int? actorAccountId,
        bool isAdmin,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var order = await _uow.Orders.GetByIdForUpdateAsync(orderId, cancellationToken);
        if (order is null)
            return Result<CancelOrderCustomerResponseDto>.NotFound("Order", orderId);

        // Ownership check
        if (!isAdmin && order.AccountId != actorAccountId)
            return Result<CancelOrderCustomerResponseDto>.Unauthorized("Bạn không có quyền hủy đơn này.");

        // Chỉ hủy khi trạng thái còn Pending hoặc Confirmed
        if (!OrderStatuses.CancellableStatuses.Contains(order.Status.StatusName))
            return Result<CancelOrderCustomerResponseDto>.UnprocessableEntity(
                $"Không thể hủy đơn ở trạng thái '{order.Status.StatusName}'.");

        if (order.CancelledAt.HasValue)
            return Result<CancelOrderCustomerResponseDto>.UnprocessableEntity("Đơn đã bị hủy.");

        // Kiểm tra trạng thái GHN
        var shippingTxn = order.ShippingProviderTransactions.FirstOrDefault();
        if (shippingTxn is not null
            && !string.IsNullOrEmpty(shippingTxn.Status)
            && NonCancellableGhnStatuses.Contains(shippingTxn.Status))
        {
            return Result<CancelOrderCustomerResponseDto>.UnprocessableEntity(
                "Đơn đang giao, không thể hủy. Liên hệ hỗ trợ.");
        }

        // Gọi GHN cancel nếu đã tạo đơn vận chuyển (ngoài transaction DB)
        if (!string.IsNullOrEmpty(order.ShippingOrderCode))
        {
            _logger.LogInformation("Cancelling GHN order {GhnCode} for Order {OrderCode}",
                order.ShippingOrderCode, order.OrderCode);
            // TODO: Call GHN cancel API if available in IGhnClient
        }

        var result = await _orderLifecycle.CancelOrderInternalAsync(order, reason ?? "Khách hàng hủy", actorAccountId ?? order.AccountId, cancellationToken);
        if (!result.IsSuccess)
        {
            return Result<CancelOrderCustomerResponseDto>.Failure(result.ErrorCode!, result.ErrorMessage!);
        }

        var cancelPayload = new { orderId = order.OrderId, orderCode = order.OrderCode, reason = reason };
        await _eventPublisher.PublishAsync("Order", order.OrderId.ToString(), NotificationEventTypes.OrderCancelled,
            cancelPayload, CancellationToken.None);

        return Result<CancelOrderCustomerResponseDto>.Success(new CancelOrderCustomerResponseDto
        {
            OrderId       = order.OrderId,
            OrderCode     = order.OrderCode,
            Status        = OrderStatuses.Cancelled,
            Message       = "Hủy đơn hàng thành công."
        });
    }

    // ── Tracking ─────────────────────────────────────────────────────────────

    public async Task<Result<OrderTrackingDto>> GetTrackingAsync(
        int orderId,
        int accountId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var order = await _uow.Orders.GetByIdWithTrackingAsync(orderId, cancellationToken);
        if (order is null)
            return Result<OrderTrackingDto>.NotFound("Order", orderId);

        if (!isAdmin && order.AccountId != accountId)
            return Result<OrderTrackingDto>.Unauthorized("Bạn không có quyền xem thông tin đơn này.");

        var shippingTxn = order.ShippingProviderTransactions.FirstOrDefault();
        
        var dto = new OrderTrackingDto
        {
            OrderId = order.OrderId,
            OrderCode = order.OrderCode,
            ShippingOrderCode = order.ShippingOrderCode,
            CurrentStatus = shippingTxn?.Status,
            StatusDescription = MapGhnStatusToDescription(shippingTxn?.Status),
            EstimatedDelivery = shippingTxn?.EstimatedDelivery,
            Events = shippingTxn?.ShippingStatusHistories
                .Select(h => new OrderTrackingEventDto
                {
                    Time = h.ProcessedAt,
                    Status = h.NewStatus,
                    Description = MapGhnStatusToDescription(h.NewStatus)
                }).ToList() ?? []
        };

        return Result<OrderTrackingDto>.Success(dto);
    }

    // ── Payment Status ───────────────────────────────────────────────────────

    public async Task<Result<OrderPaymentStatusDto>> GetPaymentStatusAsync(
        int orderId,
        int accountId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var order = await _uow.Orders.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
            return Result<OrderPaymentStatusDto>.NotFound("Order", orderId);

        if (!isAdmin && order.AccountId != accountId)
            return Result<OrderPaymentStatusDto>.Unauthorized("Bạn không có quyền xem thông tin đơn này.");

        return Result<OrderPaymentStatusDto>.Success(new OrderPaymentStatusDto
        {
            OrderId = order.OrderId,
            OrderCode = order.OrderCode,
            PaymentStatus = order.PaymentStatus,
            PaidAt = order.PaidAt
        });
    }

    private static string? MapGhnStatusToDescription(string? status)
    {
        if (string.IsNullOrEmpty(status)) return "Đang xử lý";
        return status.ToLower() switch
        {
            "ready_to_pick" => "Chờ lấy hàng",
            "picking" => "Đang lấy hàng",
            "cancel" => "Đã hủy đơn vận chuyển",
            "picked" => "Đã lấy hàng",
            "storing" => "Đang nhập kho",
            "transporting" => "Đang luân chuyển",
            "sorting" => "Đang phân loại",
            "delivering" => "Đang giao hàng",
            "money_collect_delivering" => "Đang giao hàng và thu tiền",
            "delivered" => "Giao hàng thành công",
            "delivery_failed" => "Giao hàng thất bại",
            "waiting_to_return" => "Chờ chuyển hoàn",
            "return" => "Đang chuyển hoàn",
            "returned" => "Đã chuyển hoàn",
            "return_fail" => "Chuyển hoàn thất bại",
            _ => status
        };
    }
}
