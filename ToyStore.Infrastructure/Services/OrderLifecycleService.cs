using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Common.Models;
using ToyStore.Application.Constants;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Constants;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Services;

public class OrderLifecycleService : IOrderLifecycleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderLifecycleService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly IShiftAssignmentService _shiftAssignmentService;

    public OrderLifecycleService(
        IUnitOfWork unitOfWork,
        ILogger<OrderLifecycleService> logger,
        ITimeProvider timeProvider,
        IShiftAssignmentService shiftAssignmentService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _timeProvider = timeProvider;
        _shiftAssignmentService = shiftAssignmentService;
    }

    public async Task<Result> CancelOrderInternalAsync(Order order, string reason, int cancelledByAccountId, bool restoreCart = false, bool restoreVoucher = true, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.UtcNow;
        var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(cancellationToken);
        if (!statusMap.TryGetValue("Cancelled", out var cancelledId))
        {
            return Result.Failure("INTERNAL_ERROR", "Cancelled status not found.");
        }

        if (order.StatusId == cancelledId)
        {
            return Result.Failure("BUSINESS_ERROR", "Order is already cancelled.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // 1. Restore stock
            // Stock is always deducted at confirm (COD, SE_PAY reserve, WALLET post-debit)
            // Restore when order is not PAID (WALLET PAID already handled separately as REFUNDED)
            bool productsSubtracted = order.PaymentStatus != "PAID";

            foreach (var detail in order.OrderDetails)
            {
                // General Stock restoration
                if (productsSubtracted)
                {
                    await _unitOfWork.Products.AdjustStockAsync(detail.ProductId, detail.Quantity, cancellationToken);
                }

                // Flash Sale Stock restoration
                if (detail.SlotProductId.HasValue)
                {
                    if (order.PaymentStatus == "PAID")
                    {
                        // Deducted from SoldQuantity (webhook moved Reserved→Sold)
                        await _unitOfWork.Orders.AdjustFlashSaleStockAsync(detail.SlotProductId.Value, -detail.Quantity, 0, cancellationToken);
                    }
                    else if (order.PaymentMethod == "SE_PAY")
                    {
                        // SE_PAY: stock reserved but not yet sold — release ReservedQuantity
                        await _unitOfWork.Orders.AdjustFlashSaleStockAsync(detail.SlotProductId.Value, 0, -detail.Quantity, cancellationToken);
                    }
                    else
                    {
                        // COD: deducted from SoldQuantity at confirm
                        await _unitOfWork.Orders.AdjustFlashSaleStockAsync(detail.SlotProductId.Value, -detail.Quantity, 0, cancellationToken);
                    }
                }
            }

            // 2. Restore voucher
            if (restoreVoucher)
            {
                await _unitOfWork.Orders.RestoreVoucherAsync(order.OrderId, cancellationToken);
            }

            // 3. Wallet refund / SE_PAY payment status sync
            if (order.PaymentMethod == "WALLET" && order.PaymentStatus == "PAID")
            {
                await _unitOfWork.Orders.RefundWalletAsync(order.AccountId, order.TotalAmount, order.OrderCode, cancellationToken);
                order.PaymentStatus = "REFUNDED";
            }
            else if (order.PaymentMethod == "SE_PAY" && order.PaymentStatus == "PENDING")
            {
                // System auto-cancel (timeout job) → EXPIRED; user/admin cancel → CANCELLED
                order.PaymentStatus = cancelledByAccountId == 0 ? "EXPIRED" : "CANCELLED";
            }

            // 4. Restore cart items only when explicitly requested (QR payment cancel flow).
            // - Payment QR cancel (restoreCart=true): SE_PAY never removed cart at confirm,
            //   COD/WALLET cart was removed at confirm so we restore if unpaid.
            // - Order Detail / Order History cancel (restoreCart=false): intentional cancel by the user,
            //   we intentionally do NOT put items back into the cart.
            if (restoreCart)
            {
                bool cartWasRemovedAtConfirm = order.PaymentMethod != "SE_PAY";
                if (cartWasRemovedAtConfirm && order.PaymentStatus != "PAID")
                {
                    var cart = await _unitOfWork.Carts.GetByAccountIdWithRemovedItemsAsync(order.AccountId, cancellationToken);
                    if (cart != null)
                    {
                        var productIdsInOrder = order.OrderDetails.Select(d => d.ProductId).ToHashSet();
                        foreach (var ci in cart.CartItems.Where(i => i.RemovedAt.HasValue && productIdsInOrder.Contains(i.ProductId)))
                        {
                            if (ci.RemovedAt >= order.CreatedAt.AddMinutes(-5))
                            {
                                ci.RemovedAt = null;
                            }
                        }
                    }
                }
            }

            // 5. Update order status
            order.StatusId = cancelledId;
            order.CancelledAt = now;
            order.CancelReason = reason;

            await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                StatusId = cancelledId,
                ChangedBy = cancelledByAccountId == 0 ? null : cancelledByAccountId,
                Note = $"Order cancelled: {reason}",
                CreatedAt = now
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} cancelled by {AccountId}. Reason: {Reason}",
                order.OrderId, cancelledByAccountId, reason);

            try
            {
                await _shiftAssignmentService.ReleaseCapacityAsync(order.OrderId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to release capacity for cancelled order {OrderId}", order.OrderId);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to cancel order {OrderId}", order.OrderId);
            throw;
        }
    }

    public async Task<Result> CompleteOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Orders.GetByIdForUpdateAsync(orderId, cancellationToken);
        if (order is null) return Result.NotFound("Order", orderId);

        var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(cancellationToken);
        if (!statusMap.TryGetValue(OrderStatuses.Completed, out var completedId))
            return Result.Failure("INTERNAL_ERROR", "Status 'Completed' not found.");

        if (order.StatusId == completedId) return Result.Success();

        var now = _timeProvider.UtcNow;
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            order.StatusId = completedId;
            order.CompletedAt = now;
            order.UpdatedAt = now;

            // Cập nhật trạng thái thanh toán cho đơn COD
            if (order.PaymentMethod == "SHIP_COD" && order.PaymentStatus != "PAID")
            {
                order.PaymentStatus = "PAID";
                order.PaidAt = now;
            }

            await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                StatusId = completedId,
                ChangedBy = null,
                Note = "Order completed",
                CreatedAt = now
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // LUỒNG B: Release capacity
            await TryReleaseCapacityAsync(orderId, cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to complete order {OrderId}", orderId);
            throw;
        }
    }

    public async Task<Result> DeliverOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Orders.GetByIdForUpdateAsync(orderId, cancellationToken);
        if (order is null) return Result.NotFound("Order", orderId);

        var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(cancellationToken);
        if (!statusMap.TryGetValue(OrderStatuses.Delivered, out var deliveredId))
            return Result.Failure("INTERNAL_ERROR", "Status 'Delivered' not found.");

        if (order.StatusId == deliveredId) return Result.Success();

        var now = _timeProvider.UtcNow;
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            order.StatusId = deliveredId;
            order.DeliveredAt = now;
            order.UpdatedAt = now;

            // Cập nhật trạng thái thanh toán cho đơn COD khi giao thành công
            if (order.PaymentMethod == "SHIP_COD" && order.PaymentStatus != "PAID")
            {
                order.PaymentStatus = "PAID";
                order.PaidAt = now;
            }

            await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                StatusId = deliveredId,
                ChangedBy = null,
                Note = "Order marked as delivered",
                CreatedAt = now
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // LUỒNG B: Release capacity
            await TryReleaseCapacityAsync(orderId, cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to mark order {OrderId} as delivered", orderId);
            throw;
        }
    }

    private async Task TryReleaseCapacityAsync(int orderId, CancellationToken cancellationToken)
    {
        try
        {
            await _shiftAssignmentService.ReleaseCapacityAsync(orderId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to release capacity for order {OrderId}", orderId);
        }
    }
}
