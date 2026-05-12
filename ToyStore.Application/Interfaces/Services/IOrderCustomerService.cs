using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Services;

public interface IOrderCustomerService
{
    /// <summary>Hủy đơn hàng (user hoặc admin).</summary>
    Task<Result<CancelOrderCustomerResponseDto>> CancelAsync(
        int orderId,
        int? actorAccountId,
        bool isAdmin,
        string? reason,
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
}
