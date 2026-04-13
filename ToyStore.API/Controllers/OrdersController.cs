using Microsoft.AspNetCore.Mvc;
using ToyStore.Application.DTOs;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Enums;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;
    
    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }
    
    /// <summary>
    /// Gets paginated list of orders with filtering.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<OrderListDto>>>> GetOrders(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? userId = null,
        [FromQuery] OrderStatus? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _orderService.GetOrdersAsync(
            pageNumber, pageSize, userId, status, startDate, endDate, searchTerm, cancellationToken);
            
        return Ok(ApiResponse<PaginatedResponse<OrderListDto>>.Ok(result));
    }
    
    /// <summary>
    /// Gets an order by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> GetById(
        Guid id, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetByIdAsync(id, cancellationToken);
        
        if (order == null)
            return NotFound(ApiResponse<OrderDto>.Fail("Order not found"));
            
        return Ok(ApiResponse<OrderDto>.Ok(order));
    }
    
    /// <summary>
    /// Gets an order by order number.
    /// </summary>
    [HttpGet("number/{orderNumber}")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> GetByOrderNumber(
        string orderNumber, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetByOrderNumberAsync(orderNumber, cancellationToken);
        
        if (order == null)
            return NotFound(ApiResponse<OrderDto>.Fail("Order not found"));
            
        return Ok(ApiResponse<OrderDto>.Ok(order));
    }
    
    /// <summary>
    /// Gets orders for a specific user.
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrderListDto>>>> GetUserOrders(
        Guid userId, CancellationToken cancellationToken)
    {
        var orders = await _orderService.GetUserOrdersAsync(userId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<OrderListDto>>.Ok(orders));
    }
    
    /// <summary>
    /// Creates a new order.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<OrderDto>>> Create(
        [FromBody] CreateOrderDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _orderService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = order.Id },
                ApiResponse<OrderDto>.Ok(order, "Order created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return BadRequest(ApiResponse<OrderDto>.Fail(ex.Message));
        }
    }
    
    /// <summary>
    /// Updates order status.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> UpdateStatus(
        Guid id, [FromBody] UpdateOrderStatusDto dto, CancellationToken cancellationToken)
    {
        var order = await _orderService.UpdateStatusAsync(id, dto, cancellationToken);
        
        if (order == null)
            return NotFound(ApiResponse<OrderDto>.Fail("Order not found"));
            
        return Ok(ApiResponse<OrderDto>.Ok(order, "Order status updated successfully"));
    }
    
    /// <summary>
    /// Cancels an order.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse>> Cancel(
        Guid id, [FromBody] string reason, CancellationToken cancellationToken)
    {
        var success = await _orderService.CancelAsync(id, reason, cancellationToken);
        
        if (!success)
            return NotFound(ApiResponse.Fail("Order not found or cannot be cancelled"));
            
        return Ok(ApiResponse.Ok("Order cancelled successfully"));
    }
}
