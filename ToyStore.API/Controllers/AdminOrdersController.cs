using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Orders;
using ToyStore.Application.Interfaces.Services;
using ToyStore.API.Controllers;

namespace ToyStore.API.Controllers;

/// <summary>
/// Quan ly don hang phia admin (Staff, Merchandise, Admin).
/// </summary>
[Authorize(Policy = "Orders.Operational")]
[ApiController]
[Route("api/admin/orders")]
public class AdminOrdersController : ControllerBase
{
    private readonly IAdminOrderService _orderService;
    private readonly IOrderCustomerService _customerOrderService;
    private readonly ICurrentUserService _currentUser;
    private readonly IOrderAccessService _orderAccess;

    public AdminOrdersController(
        IAdminOrderService orderService,
        IOrderCustomerService customerOrderService,
        ICurrentUserService currentUser,
        IOrderAccessService orderAccess)
    {
        _orderService = orderService;
        _customerOrderService = customerOrderService;
        _currentUser = currentUser;
        _orderAccess = orderAccess;
    }

    /// <summary>
    /// Danh sach don hang (phan trang, loc theo role).
    /// GET /api/admin/orders
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<AdminOrderListItemDto>>> GetOrders(
        [FromQuery] AdminOrderQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await _orderService.GetListAsync(query, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Chi tiet don hang.
    /// GET /api/admin/orders/{id}
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdminOrderDetailDto>> GetOrderDetail(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _orderService.GetDetailAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Staff xac nhan don hang (Pending → Confirmed).
    /// PATCH /api/admin/orders/{id}/confirm
    /// </summary>
    [HttpPatch("{id:int}/confirm")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<ActionResult<ConfirmOrderResponseDto>> ConfirmOrder(
        int id,
        [FromBody] ConfirmOrderRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _orderService.ConfirmOrderAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Merchandise nhan don va chuyen sang Processing (Confirmed → Processing).
    /// PATCH /api/admin/orders/{id}/process
    /// </summary>
    [HttpPatch("{id:int}/process")]
    [Authorize(Roles = "Merchandise,Admin")]
    public async Task<ActionResult<ProcessOrderResponseDto>> ProcessOrder(
        int id,
        [FromBody] ProcessOrderRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _orderService.ProcessOrderAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Merchandise tao don giao hang voi shipper (Processing → Shipped).
    /// PATCH /api/admin/orders/{id}/ship
    /// </summary>
    [HttpPatch("{id:int}/ship")]
    [Authorize(Roles = "Merchandise,Admin")]
    public async Task<ActionResult<ShipOrderResponseDto>> ShipOrder(
        int id,
        [FromBody] ShipOrderRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _orderService.ShipOrderAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Huy don hang (chi khi Pending hoac Confirmed).
    /// PATCH /api/admin/orders/{id}/cancel
    /// </summary>
    [HttpPatch("{id:int}/cancel")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<ActionResult<CancelOrderResponseDto>> CancelOrder(
        int id,
        [FromBody] CancelOrderRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _orderService.CancelOrderAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Admin assign lai don cho nguoi khac.
    /// PATCH /api/admin/orders/{id}/assign
    /// </summary>
    [HttpPatch("{id:int}/assign")]
    [Authorize(Policy = "Orders.Admin")]
    public async Task<IActionResult> AssignOrder(
        int id,
        [FromBody] AssignOrderRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _orderService.AssignOrderAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Admin tạo đơn vận chuyển GHN thủ công.
    /// Chỉ cho phép khi ShippingOrderCode IS NULL và PaymentStatus IN (PAID, COD_PENDING).
    /// POST /api/admin/orders/{id}/create-shipping
    /// </summary>
    [HttpPost("{id:int}/create-shipping")]
    [Authorize(Roles = "Merchandise,Admin")]
    public async Task<ActionResult<ShipOrderResponseDto>> CreateShipping(
        int id,
        [FromBody] ShipOrderRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _orderService.ShipOrderAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Admin hủy đơn hàng (dùng pipeline đầy đủ: restore stock + voucher + wallet refund).
    /// POST /api/admin/orders/{id}/cancel-full
    /// </summary>
    [HttpPost("{id:int}/cancel-full")]
    [Authorize(Policy = "Orders.Admin")]
    public async Task<ActionResult<CancelOrderCustomerResponseDto>> CancelFull(
        int id,
        [FromBody] CancelOrderCustomerRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _customerOrderService.CancelAsync(
            id,
            _currentUser.AccountId,
            isAdmin: true,
            reason: request.Reason,
            restoreCart: false,
            cancellationToken: cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Admin xem tracking GHN của đơn.
    /// GET /api/admin/orders/{id}/tracking
    /// </summary>
    [HttpGet("{id:int}/tracking")]
    public async Task<ActionResult<OrderTrackingDto>> GetTracking(
        int id,
        CancellationToken cancellationToken)
    {
        var access = await _orderAccess.EnsureCanViewAsync(id, cancellationToken);
        if (access.IsFailure)
        {
            return Result<OrderTrackingDto>.Failure(access.ErrorCode!, access.ErrorMessage!).ToActionResult();
        }

        var result = await _customerOrderService.GetTrackingAsync(
            id,
            _currentUser.AccountId,
            isAdmin: _orderAccess.IsPrivileged(_currentUser.RoleId),
            cancellationToken);
        return result.ToActionResult();
    }
}


