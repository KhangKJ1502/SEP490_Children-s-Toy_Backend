using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs.Checkouts;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

/// <summary>
/// Checkout: preview phí ship, đặt hàng, retry QR SE_PAY.
/// </summary>
[ApiController]
[Route("api/checkout")]
[Authorize]
public class CheckoutController : ControllerBase
{
    private readonly ICheckoutService _checkout;
    private readonly ICurrentUserService _currentUser;

    public CheckoutController(ICheckoutService checkout, ICurrentUserService currentUser)
    {
        _checkout    = checkout;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Tính phí ship + tổng đơn trước khi đặt (không tạo Order).
    /// POST /api/checkout/preview
    /// </summary>
    [HttpPost("preview")]
    public async Task<ActionResult<CheckoutPreviewResponseDto>> Preview(
        [FromBody] CheckoutPreviewQueryDto query,
        CancellationToken ct)
    {
        var accountId = _currentUser.AccountId;
        var result = await _checkout.PreviewAsync(accountId, query.AddressId, query.VoucherCode, query.Items, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Đặt hàng — tạo Order, trừ stock, xử lý thanh toán.
    /// POST /api/checkout
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CheckoutConfirmResponseDto>> Confirm(
        [FromBody] CheckoutConfirmRequestDto request,
        CancellationToken ct)
    {
        var accountId = _currentUser.AccountId;
        var result = await _checkout.ConfirmAsync(accountId, request, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Sinh QR mới cho đơn SE_PAY chưa thanh toán.
    /// POST /api/checkout/retry-payment/{orderId}
    /// </summary>
    [HttpPost("retry-payment/{orderId:int}")]
    public async Task<ActionResult<RetryPaymentResponseDto>> RetryPayment(
        int orderId,
        CancellationToken ct)
    {
        var accountId = _currentUser.AccountId;
        var result = await _checkout.RetryPaymentAsync(accountId, orderId, ct);
        return result.ToActionResult();
    }
}

/// <summary>Query DTO cho preview (không cần items — lấy từ cart hiện tại).</summary>
public class CheckoutPreviewQueryDto
{
    public int AddressId { get; set; }
    public string? VoucherCode { get; set; }

    /// <summary>Các dòng đặt (khớp giỏ đã chọn). Null = preview toàn bộ giỏ.</summary>
    public List<CheckoutConfirmItemDto>? Items { get; set; }
}
