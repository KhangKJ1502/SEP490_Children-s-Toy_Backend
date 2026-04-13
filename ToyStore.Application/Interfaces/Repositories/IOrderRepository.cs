using ToyStore.Domain.Entities;
using ToyStore.Domain.Enums;

namespace ToyStore.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for Order operations.
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// Gets an order by ID.
    /// </summary>
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all orders.
    /// </summary>
    Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds a new order.
    /// </summary>
    Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing order.
    /// </summary>
    void Update(Order order);
    
    /// <summary>
    /// Removes an order.
    /// </summary>
    void Remove(Order order);
    
    /// <summary>
    /// Gets an order by order number.
    /// </summary>
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets an order with its items.
    /// </summary>
    Task<Order?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets orders by user ID.
    /// </summary>
    Task<IReadOnlyList<Order>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets orders by status.
    /// </summary>
    Task<IReadOnlyList<Order>> GetByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets orders in a date range.
    /// </summary>
    Task<IReadOnlyList<Order>> GetByDateRangeAsync(
        DateTime startDate, 
        DateTime endDate, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets recent orders.
    /// </summary>
    Task<IReadOnlyList<Order>> GetRecentOrdersAsync(int limit = 10, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets orders pending shipment.
    /// </summary>
    Task<IReadOnlyList<Order>> GetPendingShipmentAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets paginated orders.
    /// </summary>
    Task<(IReadOnlyList<Order> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Guid? userId = null,
        OrderStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets pending orders for background processing.
    /// </summary>
    Task<IReadOnlyList<Order>> GetPendingOrdersAsync(CancellationToken cancellationToken = default);
}
