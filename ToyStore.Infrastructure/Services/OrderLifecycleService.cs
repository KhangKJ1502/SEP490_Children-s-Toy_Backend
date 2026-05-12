using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Common.Models;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Services;

public class OrderLifecycleService : IOrderLifecycleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderLifecycleService> _logger;
    private readonly ITimeProvider _timeProvider;

    public OrderLifecycleService(
        IUnitOfWork unitOfWork,
        ILogger<OrderLifecycleService> logger,
        ITimeProvider timeProvider)
    {
        _unitOfWork   = unitOfWork;
        _logger       = logger;
        _timeProvider = timeProvider;
    }

    public async Task<Result> CancelOrderInternalAsync(Order order, string reason, int cancelledByAccountId, CancellationToken cancellationToken = default)
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
            // Determine if products were already subtracted from main stock
            bool productsSubtracted = (order.PaymentMethod != "SE_PAY" || order.PaymentStatus == "PAID");

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
                    if (order.PaymentStatus == "PAID" || (order.PaymentMethod != "SE_PAY"))
                    {
                        // Deducted from SoldQuantity
                        await _unitOfWork.Orders.AdjustFlashSaleStockAsync(detail.SlotProductId.Value, -detail.Quantity, 0, cancellationToken);
                    }
                    else if (order.PaymentMethod == "SE_PAY" && order.PaymentStatus != "PAID")
                    {
                        // Deducted from ReservedQuantity
                        await _unitOfWork.Orders.AdjustFlashSaleStockAsync(detail.SlotProductId.Value, 0, -detail.Quantity, cancellationToken);
                    }
                }
            }

            // 2. Restore voucher
            await _unitOfWork.Orders.RestoreVoucherAsync(order.OrderId, cancellationToken);

            // 3. Wallet refund
            if (order.PaymentMethod == "WALLET" && order.PaymentStatus == "PAID")
            {
                await _unitOfWork.Orders.RefundWalletAsync(order.AccountId, order.TotalAmount, order.OrderCode, cancellationToken);
                order.PaymentStatus = "REFUNDED";
            }

            // 4. Restore cart items if the order is cancelled before being paid/processed
            if (order.PaymentStatus != "PAID")
            {
                var cart = await _unitOfWork.Carts.GetByAccountIdWithRemovedItemsAsync(order.AccountId, cancellationToken);
                if (cart != null)
                {
                    var productIdsInOrder = order.OrderDetails.Select(d => d.ProductId).ToHashSet();
                    // Restore items that were removed around the time the order was created
                    foreach (var ci in cart.CartItems.Where(i => i.RemovedAt.HasValue && productIdsInOrder.Contains(i.ProductId)))
                    {
                        if (ci.RemovedAt >= order.CreatedAt.AddMinutes(-5)) 
                        {
                            ci.RemovedAt = null;
                        }
                    }
                }
            }

            // 5. Update order status
            order.StatusId     = cancelledId;
            order.CancelledAt  = now;
            order.CancelReason = reason;

            await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
            {
                OrderId   = order.OrderId,
                StatusId  = cancelledId,
                ChangedBy = cancelledByAccountId,
                Note      = $"Order cancelled: {reason}",
                CreatedAt = now
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} cancelled by {AccountId}. Reason: {Reason}", 
                order.OrderId, cancelledByAccountId, reason);

            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to cancel order {OrderId}", order.OrderId);
            throw;
        }
    }
}
