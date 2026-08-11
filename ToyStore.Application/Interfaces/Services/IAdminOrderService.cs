using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Orders;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Xu ly cac nghiep vu quan ly don hang phia admin
/// (Staff, Merchandise, Admin).
/// </summary>
public interface IAdminOrderService
{
    Task<Result<PaginatedResponse<AdminOrderListItemDto>>> GetListAsync(
        AdminOrderQueryDto query,
        CancellationToken cancellationToken = default);

    Task<Result<AdminOrderDetailDto>> GetDetailAsync(
        int orderId,
        CancellationToken cancellationToken = default);

    Task<Result<ConfirmOrderResponseDto>> ConfirmOrderAsync(
        int orderId,
        ConfirmOrderRequestDto request,
        CancellationToken cancellationToken = default);

    Task<Result<ProcessOrderResponseDto>> ProcessOrderAsync(
        int orderId,
        ProcessOrderRequestDto request,
        CancellationToken cancellationToken = default);

    Task<Result<ShipOrderResponseDto>> ShipOrderAsync(
        int orderId,
        ShipOrderRequestDto request,
        CancellationToken cancellationToken = default);

    Task<Result<CancelOrderResponseDto>> CancelOrderAsync(
        int orderId,
        CancelOrderRequestDto request,
        CancellationToken cancellationToken = default);

    Task<Result> AssignOrderAsync(
        int orderId,
        AssignOrderRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tu dong confirm don sau khi thanh toan thanh cong.
    /// Goi tu payment handler SAU KHI commit PaymentStatus=PAID.
    /// Khong nen rollback payment neu method nay loi — chi log.
    /// </summary>
    Task AutoConfirmAfterPaymentAsync(
        int orderId,
        CancellationToken cancellationToken = default);
}


