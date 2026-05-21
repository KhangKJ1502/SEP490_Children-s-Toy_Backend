using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Orders;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Constants;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Enums;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Options;

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
    private readonly IMapper _mapper;
    private readonly IShippingStatusMapper _statusMapper;
    private readonly SePayOptions _sePayOptions;
    private readonly SEP490ToyStoreContext _db;

    private static readonly HashSet<string> NonCancellableGhnStatuses =
        new(StringComparer.OrdinalIgnoreCase)
        { "delivering", "money_collect_delivering", "delivered" };

    public OrderCustomerService(
        IUnitOfWork uow,
        SEP490ToyStoreContext db,
        IGhnClient ghnClient,
        IDomainEventPublisher eventPublisher,
        ILogger<OrderCustomerService> logger,
        ITimeProvider timeProvider,
        IOrderLifecycleService orderLifecycle,
        IMapper mapper,
        IShippingStatusMapper statusMapper,
        IOptions<SePayOptions> sePayOptions)
    {
        _uow = uow;
        _db = db;
        _ghnClient = ghnClient;
        _eventPublisher = eventPublisher;
        _logger = logger;
        _timeProvider = timeProvider;
        _orderLifecycle = orderLifecycle;
        _mapper = mapper;
        _statusMapper = statusMapper;
        _sePayOptions = sePayOptions.Value;
    }


    public async Task<Result<PaginatedResponse<CustomerOrderListItemDto>>> GetListAsync(
        CustomerOrderQueryDto query,
        int accountId,
        CancellationToken cancellationToken = default)
    {
        var pageSize = Math.Min(query.PageSize, 100);
        var pageNumber = Math.Max(query.PageNumber, 1);

        var statusNames = MapCustomerStatusFilter(query.Status);

        var items = await _uow.Orders.GetCustomerPagedAsync(
            accountId,
            statusNames,
            pageNumber,
            pageSize,
            query.Keyword,
            query.FromDate,
            query.ToDate,
            cancellationToken);

        var count = await _uow.Orders.CountCustomerAsync(
            accountId,
            statusNames,
            query.Keyword,
            query.FromDate,
            query.ToDate,
            cancellationToken);

        var dtos = _mapper.Map<List<CustomerOrderListItemDto>>(items);
        var response = new PaginatedResponse<CustomerOrderListItemDto>(dtos, count, pageNumber, pageSize);

        return Result<PaginatedResponse<CustomerOrderListItemDto>>.Success(response);
    }

    // ── Detail ─────────────────────────────────────────────────────────────

    public async Task<Result<CustomerOrderDetailDto>> GetDetailAsync(
        int orderId,
        int accountId,
        CancellationToken cancellationToken = default)
    {
        var order = await _uow.Orders.GetByIdForCustomerAsync(orderId, accountId, cancellationToken);
        if (order is null)
        {
            var exists = await _uow.Orders.GetByIdAsync(orderId, cancellationToken);
            if (exists is null)
            {
                return Result<CustomerOrderDetailDto>.NotFound("Order", orderId);
            }

            return Result<CustomerOrderDetailDto>.Unauthorized("You are not authorized to view this order.");
        }

        var dto = _mapper.Map<CustomerOrderDetailDto>(order);
        return Result<CustomerOrderDetailDto>.Success(dto);
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    public async Task<Result<CancelOrderCustomerResponseDto>> CancelAsync(
        int orderId,
        int? actorAccountId,
        bool isAdmin,
        string? reason,
        bool restoreCart = false,
        CancellationToken cancellationToken = default)
    {
        var order = await _uow.Orders.GetByIdForUpdateAsync(orderId, cancellationToken);
        if (order is null)
            return Result<CancelOrderCustomerResponseDto>.NotFound("Order", orderId);

        // Ownership check
        if (!isAdmin && order.AccountId != actorAccountId)
            return Result<CancelOrderCustomerResponseDto>.Unauthorized("You are not authorized to cancel this order.");

        // Chỉ hủy khi trạng thái còn Pending hoặc Confirmed
        if (!OrderStatuses.CancellableStatuses.Contains(order.Status.StatusName))
            return Result<CancelOrderCustomerResponseDto>.UnprocessableEntity(
                $"Cannot cancel order in '{order.Status.StatusName}' status.");

        // SHIP COD rule: Chỉ được hủy khi chưa confirmed (tức là chỉ được hủy khi đang Pending)
        if (!isAdmin && order.PaymentMethod == "SHIP_COD" && order.Status.StatusName == OrderStatuses.Confirmed)
        {
            return Result<CancelOrderCustomerResponseDto>.UnprocessableEntity(
                "COD order has been confirmed and cannot be self-cancelled. Please contact support.");
        }

        if (order.CancelledAt.HasValue)
            return Result<CancelOrderCustomerResponseDto>.UnprocessableEntity("Order is already cancelled.");

        // Kiểm tra trạng thái GHN
        var shippingTxn = order.ShippingProviderTransactions.FirstOrDefault();
        if (shippingTxn is not null
            && !string.IsNullOrEmpty(shippingTxn.Status)
            && NonCancellableGhnStatuses.Contains(shippingTxn.Status))
        {
            return Result<CancelOrderCustomerResponseDto>.UnprocessableEntity(
                "Order is out for delivery and cannot be cancelled. Please contact support.");
        }

        // Gọi GHN cancel nếu đã tạo đơn vận chuyển (ngoài transaction DB)
        if (!string.IsNullOrEmpty(order.ShippingOrderCode))
        {
            _logger.LogInformation("Cancelling GHN order {GhnCode} for Order {OrderCode}",
                order.ShippingOrderCode, order.OrderCode);
            // TODO: Call GHN cancel API if available in IGhnClient
        }

        int cancelledByAccountId = actorAccountId ?? (isAdmin ? 0 : order.AccountId);
        bool restoreVoucher = isAdmin || (actorAccountId == null) || restoreCart;
        var result = await _orderLifecycle.CancelOrderInternalAsync(order, reason ?? "Cancelled by customer", cancelledByAccountId, restoreCart, restoreVoucher, cancellationToken);
        if (!result.IsSuccess)
        {
            return Result<CancelOrderCustomerResponseDto>.Failure(result.ErrorCode!, result.ErrorMessage!);
        }

        if (!restoreCart)
        {
            if (!(order.PaymentMethod == "SE_PAY" && order.PaymentStatus != "PAID"))
            {
                var cancelPayload = new { orderId = order.OrderId, orderCode = order.OrderCode, reason = reason };
                await _eventPublisher.PublishAsync("Order", order.OrderId.ToString(), NotificationEventTypes.OrderCancelled,
                    cancelPayload, CancellationToken.None);
            }
        }

        return Result<CancelOrderCustomerResponseDto>.Success(new CancelOrderCustomerResponseDto
        {
            OrderId = order.OrderId,
            OrderCode = order.OrderCode,
            Status = OrderStatuses.Cancelled,
            Message = "Order cancelled successfully."
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
            return Result<OrderTrackingDto>.Unauthorized("You are not authorized to view this order tracking.");

        var shippingTxn = order.ShippingProviderTransactions.FirstOrDefault();

        var dto = new OrderTrackingDto
        {
            OrderId = order.OrderId,
            OrderCode = order.OrderCode,
            ShippingOrderCode = order.ShippingOrderCode,
            CurrentStatus = _statusMapper.NormalizeStatus(shippingTxn?.Status, (OrderStatus)order.StatusId),
            StatusDescription = _statusMapper.GetStatusDescription(shippingTxn?.Status),
            EstimatedDelivery = shippingTxn?.EstimatedDelivery,
            Events = shippingTxn?.ShippingStatusHistories
                .Select(h => new OrderTrackingEventDto
                {
                    Time = h.ProcessedAt,
                    Status = _statusMapper.NormalizeStatus(h.NewStatus, (OrderStatus)order.StatusId),
                    Description = _statusMapper.GetStatusDescription(h.NewStatus)
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
            return Result<OrderPaymentStatusDto>.Unauthorized("You are not authorized to view this order payment status.");

        // Guard: nếu đơn SE_PAY đã bị cancel nhưng PaymentStatus chưa được cập nhật (dữ liệu cũ)
        var effectivePaymentStatus = order.PaymentStatus;
        if (order.PaymentMethod == "SE_PAY"
            && order.CancelledAt.HasValue
            && order.PaymentStatus == "PENDING")
        {
            effectivePaymentStatus = "CANCELLED";
        }

        return Result<OrderPaymentStatusDto>.Success(new OrderPaymentStatusDto
        {
            OrderId = order.OrderId,
            OrderCode = order.OrderCode,
            PaymentStatus = effectivePaymentStatus,
            PaidAt = order.PaidAt,
            ExpiresAt = order.PaymentMethod == "SE_PAY"
                ? DateTime.SpecifyKind(order.CreatedAt, DateTimeKind.Utc)
                    .AddMinutes(_sePayOptions.PaymentTtlMinutes)
                : null
        });
    }

    public async Task<Result<string>> CompleteAsync(int orderId, int accountId, CancellationToken cancellationToken = default)
    {
        var order = await _uow.Orders.GetByIdAsync(orderId, cancellationToken);
        if (order is null) return Result<string>.NotFound("Order", orderId);

        if (order.AccountId != accountId)
            return Result<string>.Unauthorized("You are not authorized to confirm this order.");

        // Chỉ được xác nhận khi đơn ở trạng thái Delivered
        if (order.StatusId != (byte)OrderStatus.Delivered)
        {
            return Result<string>.UnprocessableEntity("Receipt can only be confirmed after the order has been successfully delivered.");
        }

        var result = await _orderLifecycle.CompleteOrderAsync(orderId, cancellationToken);
        if (!result.IsSuccess)
            return Result<string>.Failure(result.ErrorCode!, result.ErrorMessage!);

        // Publish event for notification (if needed, e.g. for points or stats)
        await _eventPublisher.PublishAsync("Order", order.OrderId.ToString(), "order.completed",
            new { orderId = order.OrderId, orderCode = order.OrderCode }, CancellationToken.None);

        return Result<string>.Success("Order receipt confirmed successfully.");
    }

    // ── Payment Info (secure endpoint, không lộ qua URL) ─────────────────────

    public async Task<Result<OrderPaymentInfoDto>> GetPaymentInfoAsync(
        int orderId,
        int accountId,
        CancellationToken cancellationToken = default)
    {
        var order = await _uow.Orders.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
            return Result<OrderPaymentInfoDto>.NotFound("Order", orderId);

        if (order.AccountId != accountId)
            return Result<OrderPaymentInfoDto>.Unauthorized("You are not authorized to access this order's payment info.");

        if (order.PaymentMethod != "SE_PAY")
            return Result<OrderPaymentInfoDto>.BusinessError("This order does not use QR payment.");

        // Lấy attempt đang Pending (hoặc attempt mới nhất nếu không có Pending)
        var latestAttempt = await _db.PaymentGatewayTransactions
            .Where(t => t.OrderId == orderId)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestAttempt is null)
            return Result<OrderPaymentInfoDto>.NotFound("Payment attempt", orderId);

        var qrUrl = BuildVietQrUrl(latestAttempt.RequestId, (long)order.TotalAmount);
        var expiresAt = DateTime.SpecifyKind(order.CreatedAt, DateTimeKind.Utc)
            .AddMinutes(_sePayOptions.PaymentTtlMinutes);

        return Result<OrderPaymentInfoDto>.Success(new OrderPaymentInfoDto
        {
            OrderId = order.OrderId,
            OrderCode = order.OrderCode,
            Amount = order.TotalAmount,
            PaymentAttemptCode = latestAttempt.RequestId,
            QrImageUrl = qrUrl,
            ExpiresAt = expiresAt
        });
    }

    private string BuildVietQrUrl(string attemptCode, long amount)
    {
        var p = new System.Collections.Specialized.NameValueCollection
        {
            ["acc"] = _sePayOptions.AccountNumber,
            ["bank"] = _sePayOptions.BankCode,
            ["amount"] = amount.ToString(),
            ["des"] = attemptCode
        };
        var qs = string.Join("&", p.AllKeys.Select(k => $"{k}={Uri.EscapeDataString(p[k]!)}"));
        return $"https://qr.sepay.vn/img?{qs}";
    }

    private static IReadOnlyCollection<string>? MapCustomerStatusFilter(string? status)
    {
        if (string.IsNullOrWhiteSpace(status) || status.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return status.Trim().ToLowerInvariant() switch
        {
            "pending" => [OrderStatuses.Pending],
            "shipping" => [OrderStatuses.Confirmed, OrderStatuses.Shipped, OrderStatuses.Processing],
            "delivering" => [OrderStatuses.Delivering],
            "completed" => [OrderStatuses.Completed, OrderStatuses.Delivered],
            "cancelled" => [OrderStatuses.Cancelled],
            _ => null
        };
    }
}
