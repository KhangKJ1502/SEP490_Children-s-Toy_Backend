using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Orders;
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
    /// Danh sach don hang cua customer.
    /// GET /api/orders
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<CustomerOrderListItemDto>>> GetOrders(
        [FromQuery] CustomerOrderQueryDto query,
        CancellationToken ct)
    {
        var result = await _orderService.GetListAsync(query, _currentUser.AccountId, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Chi tiet don hang cua customer.
    /// GET /api/orders/{orderId}
    /// </summary>
    [HttpGet("{orderId:int}")]
    public async Task<ActionResult<CustomerOrderDetailDto>> GetDetail(
        int orderId,
        CancellationToken ct)
    {
        var result = await _orderService.GetDetailAsync(orderId, _currentUser.AccountId, ct);
        return result.ToActionResult();
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
            restoreCart: request.RestoreCart,
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

    /// <summary>
    /// Xác nhận đã nhận hàng (Hoàn thành đơn hàng).
    /// POST /api/orders/{orderId}/complete
    /// </summary>
    [HttpPost("{orderId:int}/complete")]
    public async Task<ActionResult<string>> Complete(
        int orderId,
        CancellationToken ct)
    {
        var result = await _orderService.CompleteAsync(orderId, _currentUser.AccountId, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Lấy thông tin thanh toán nhạy cảm của đơn SE_PAY (QR URL, amount, attemptCode).
    /// Chỉ trả về cho chủ đơn hàng — không lộ trên URL.
    /// GET /api/orders/{orderId}/payment-info
    /// </summary>
    [HttpGet("{orderId:int}/payment-info")]
    public async Task<ActionResult<OrderPaymentInfoDto>> GetPaymentInfo(
        int orderId,
        CancellationToken ct)
    {
        var result = await _orderService.GetPaymentInfoAsync(orderId, _currentUser.AccountId, ct);
        return result.ToActionResult();
    }
}

public class CancelOrderCustomerRequestDto
{
    public string? Reason { get; set; }

    /// <summary>
    /// true  = khôi phục giỏ hàng sau khi hủy (dùng cho luồng Payment QR).
    /// false = KHÔNG khôi phục giỏ hàng (dùng cho Order Detail / Order History). Mặc định.
    /// </summary>
    public bool RestoreCart { get; set; } = false;
}

