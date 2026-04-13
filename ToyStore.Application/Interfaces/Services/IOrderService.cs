using ToyStore.Application.DTOs;
using ToyStore.Domain.Enums;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Service interface for order operations.
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Gets an order by ID.
    /// </summary>
    Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets an order by order number.
    /// </summary>
    Task<OrderDto?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all orders for a user.
    /// </summary>
    Task<IReadOnlyList<OrderListDto>> GetUserOrdersAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets paginated orders with filtering.
    /// </summary>
    Task<PaginatedResponse<OrderListDto>> GetOrdersAsync(
        int pageNumber = 1,
        int pageSize = 20,
        Guid? userId = null,
        OrderStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new order.
    /// </summary>
    Task<OrderDto> CreateAsync(CreateOrderDto dto, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates order status.
    /// </summary>
    Task<OrderDto?> UpdateStatusAsync(
        Guid id, 
        UpdateOrderStatusDto dto, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cancels an order.
    /// </summary>
    Task<bool> CancelAsync(Guid id, string reason, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Processes successful payment.
    /// </summary>
    Task<bool> ProcessPaymentSuccessAsync(
        Guid orderId, 
        string transactionId, 
        CancellationToken cancellationToken = default);
}
