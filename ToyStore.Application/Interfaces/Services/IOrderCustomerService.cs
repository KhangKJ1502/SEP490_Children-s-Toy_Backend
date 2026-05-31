using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Orders;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Services;

public interface IOrderCustomerService
{
    /// <summary>Danh sach don hang cua customer.</summary>
    Task<Result<PaginatedResponse<CustomerOrderListItemDto>>> GetListAsync(
        CustomerOrderQueryDto query,
        int accountId,
        CancellationToken cancellationToken = default);

    /// <summary>Chi tiet don hang cua customer.</summary>
    Task<Result<CustomerOrderDetailDto>> GetDetailAsync(
        int orderId,
        int accountId,
        CancellationToken cancellationToken = default);

    /// <summary>Hủy đơn hàng (user hoặc admin).</summary>
    Task<Result<CancelOrderCustomerResponseDto>> CancelAsync(
        int orderId,
        int? actorAccountId,
        bool isAdmin,
        string? reason,
        bool restoreCart = false,
        CancellationToken cancellationToken = default);

    /// <summary>Lấy trạng thái giao hàng từ GHN.</summary>
    Task<Result<OrderTrackingDto>> GetTrackingAsync(
        int orderId,
        int accountId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    /// <summary>Lấy trạng thái thanh toán của đơn hàng.</summary>
    Task<Result<OrderPaymentStatusDto>> GetPaymentStatusAsync(
        int orderId,
        int accountId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    /// <summary>Xác nhận đã nhận hàng (Hoàn thành đơn hàng).</summary>
    Task<Result<string>> CompleteAsync(
        int orderId,
        int accountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thông tin thanh toán nhạy cảm (QR, amount, attemptCode) của đơn SE_PAY.
    /// Chỉ trả dữ liệu cho chủ đơn hàng.
    /// </summary>
    Task<Result<OrderPaymentInfoDto>> GetPaymentInfoAsync(
        int orderId,
        int accountId,
        CancellationToken cancellationToken = default);
}

public class CancelOrderCustomerResponseDto
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class OrderTrackingDto
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string? ShippingOrderCode { get; set; }
    public string? CurrentStatus { get; set; }
    public string? StatusDescription { get; set; }
    public DateTime? EstimatedDelivery { get; set; }
    public List<OrderTrackingEventDto> Events { get; set; } = [];
}

public class OrderTrackingEventDto
{
    public DateTime Time { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Description { get; set; }
}

public class OrderPaymentStatusDto
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Thông tin thanh toán nhạy cảm của đơn SE_PAY.
/// Trả về qua API thay vì lộ trên URL.
/// </summary>
public class OrderPaymentInfoDto
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentAttemptCode { get; set; } = string.Empty;
    public string? QrImageUrl { get; set; }
    public DateTime? ExpiresAt { get; set; }
    /// <summary>
    /// Effective payment status — cho phép FE redirect đúng trang (PAID → success, EXPIRED/CANCELLED → cart).
    /// </summary>
    public string PaymentStatus { get; set; } = string.Empty;
}
