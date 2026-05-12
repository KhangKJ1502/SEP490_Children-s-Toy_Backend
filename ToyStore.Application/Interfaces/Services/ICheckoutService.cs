using ToyStore.Application.DTOs.Checkouts;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Services;

public interface ICheckoutService
{
    /// <summary>
    /// Tính phí ship + tổng đơn trước khi đặt. Validate sơ bộ giỏ hàng.
    /// </summary>
    /// <param name="itemsSubset">Dòng sản phẩm đặt (khớp giỏ đã chọn). Null = dùng toàn bộ dòng giỏ chưa xóa.</param>
    Task<Result<CheckoutPreviewResponseDto>> PreviewAsync(
        int accountId,
        int addressId,
        string? voucherCode,
        IReadOnlyList<CheckoutConfirmItemDto>? itemsSubset,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Đặt hàng: tạo Order, trừ stock, xử lý thanh toán theo PaymentMethod.
    /// </summary>
    Task<Result<CheckoutConfirmResponseDto>> ConfirmAsync(
        int accountId,
        CheckoutConfirmRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sinh QR mới cho đơn SE_PAY chưa thanh toán.
    /// </summary>
    Task<Result<RetryPaymentResponseDto>> RetryPaymentAsync(
        int accountId,
        int orderId,
        CancellationToken cancellationToken = default);
}
