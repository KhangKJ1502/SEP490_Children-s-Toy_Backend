using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderCustomerService _orderService;
    private readonly ICurrentUserService _currentUser;

    public OrdersController(IOrderCustomerService orderService, ICurrentUserService currentUser)
    {
        _orderService = orderService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Hủy đơn hàng.
    /// POST /api/orders/{orderId}/cancel
    /// </summary>
    [HttpPost("{orderId:int}/cancel")]
    public async Task<ActionResult<CancelOrderCustomerResponseDto>> Cancel(
        int orderId,
        [FromBody] CancelOrderCustomerRequestDto request,
        CancellationToken ct)
    {
        var result = await _orderService.CancelAsync(
            orderId,
            _currentUser.AccountId,
            isAdmin: false,
            request.Reason,
            ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Lấy trạng thái giao hàng GHN.
    /// GET /api/orders/{orderId}/tracking
    /// </summary>
    [HttpGet("{orderId:int}/tracking")]
    public async Task<ActionResult<OrderTrackingDto>> GetTracking(
        int orderId,
        CancellationToken ct)
    {
        var result = await _orderService.GetTrackingAsync(
            orderId,
            _currentUser.AccountId,
            isAdmin: false,
            ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Lấy trạng thái thanh toán.
    /// GET /api/orders/{orderId}/payment-status
    /// </summary>
    [HttpGet("{orderId:int}/payment-status")]
    public async Task<ActionResult<OrderPaymentStatusDto>> GetPaymentStatus(
        int orderId,
        CancellationToken ct)
    {
        var result = await _orderService.GetPaymentStatusAsync(
            orderId,
            _currentUser.AccountId,
            isAdmin: false,
            ct);
        return result.ToActionResult();
    }
}

public class CancelOrderCustomerRequestDto
{
    public string? Reason { get; set; }
}
