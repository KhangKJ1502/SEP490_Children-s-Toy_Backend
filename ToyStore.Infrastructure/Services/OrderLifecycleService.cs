using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Common.Models;
using ToyStore.Application.Constants;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Constants;
using ToyStore.Domain.Enums;
using ToyStore.Domain.Entities;
using ToyStore.Application.DTOs.Refunds;

namespace ToyStore.Infrastructure.Services;

public class OrderLifecycleService : IOrderLifecycleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderLifecycleService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly IShiftAssignmentService _shiftAssignmentService;
    private readonly IWalletRefundCreditor _walletRefundCreditor;

    private static readonly HashSet<string> PrepaidPaymentMethods =
        new(StringComparer.OrdinalIgnoreCase) { "SE_PAY", "WALLET", "BANK_TRANSFER" };

    public OrderLifecycleService(
        IUnitOfWork unitOfWork,
        ILogger<OrderLifecycleService> logger,
        ITimeProvider timeProvider,
        IShiftAssignmentService shiftAssignmentService,
        IWalletRefundCreditor walletRefundCreditor)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _timeProvider = timeProvider;
        _shiftAssignmentService = shiftAssignmentService;
        _walletRefundCreditor = walletRefundCreditor;
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
            // BƯỚC 1: Hoàn trả lại số lượng tồn kho (stock refund)
            foreach (var detail in order.OrderDetails)
            {
                // Cộng lại số lượng tồn kho chung của sản phẩm
                await _unitOfWork.Orders.AdjustStockAsync(detail.ProductId, detail.Quantity, cancellationToken);
                
                // Cộng lại số lượng tồn kho Flash Sale (nếu có áp dụng Flash Sale)
                if (detail.SlotProductId.HasValue)
                {
                    if (order.PaymentStatus == "PAID")
                    {
                        // Nếu đơn đã thanh toán: hoàn trả từ SoldQuantity về lại kho sale khả dụng
                        await _unitOfWork.Orders.AdjustFlashSaleStockAsync(detail.SlotProductId.Value, -detail.Quantity, 0, cancellationToken);
                    }
                    else if (order.PaymentMethod == "SE_PAY")
                    {
                        // Nếu đơn SE_PAY chưa thanh toán (PENDING): hoàn trả từ ReservedQuantity tạm giữ
                        await _unitOfWork.Orders.AdjustFlashSaleStockAsync(detail.SlotProductId.Value, 0, -detail.Quantity, cancellationToken);
                    }
                    else
                    {
                        // Trường hợp khác: giảm lượng bán (SoldQuantity)
                        await _unitOfWork.Orders.AdjustFlashSaleStockAsync(detail.SlotProductId.Value, -detail.Quantity, 0, cancellationToken);
                    }
                }
            }
            
            // BƯỚC 2: Khôi phục Voucher nếu yêu cầu (restoreVoucher = true)
            if (restoreVoucher)
            {
                await _unitOfWork.Orders.RestoreVoucherAsync(order.OrderId, cancellationToken);
            }

            // BƯỚC 3: Xử lý hoàn tiền cho các đơn hàng đã thanh toán trước (Prepaid PAID: SE_PAY / WALLET)
            bool hasCreatedSystemRefund = false;
            if (string.Equals(order.PaymentStatus, PaymentStatuses.Paid, StringComparison.OrdinalIgnoreCase)
                && PrepaidPaymentMethods.Contains(order.PaymentMethod))
            {
                // Nếu trạng thái đơn hàng đã tiến xa (ví dụ: đã gán shipper/đang giao hàng), 
                // không tự động hoàn vào ví nữa mà phải tạo phiếu yêu cầu hoàn tiền thủ công (manual refund request) để admin duyệt.
                if (OrderStatuses.PrepaidCancelRequiresManualRefund(order.StatusId))
                {
                    _logger.LogInformation(
                        "Cancel {OrderCode}: prepaid PAID at status {StatusId} — no auto wallet; use refund management.",
                        order.OrderCode, order.StatusId);
                    await CreateCancelledOrderSystemRefundAsync(order, cancelledByAccountId, reason, cancellationToken);
                    hasCreatedSystemRefund = true;
                }
                // Phòng tránh hoàn tiền trùng lặp (Idempotency Check) bằng cách check lịch sử hoàn tiền hoặc giao dịch ví trùng mã.
                else if (await _unitOfWork.Orders.HasCompletedRefundWalletCreditForOrderAsync(order.OrderId, cancellationToken)
                         || await _unitOfWork.Orders.ExistsWalletTransactionByIdempotencyKeyAsync(
                             WalletRefundKeys.ForOrder(order.OrderCode), cancellationToken))
                {
                    _logger.LogInformation(
                        "Cancel {OrderCode}: refund wallet credit already exists — skipping duplicate.",
                        order.OrderCode);
                    order.PaymentStatus = PaymentStatuses.Refunded;
                }
                // Nếu đơn hàng ở trạng thái mới đặt (ví dụ: Confirmed/Processing), tự động hoàn tiền trực tiếp vào Ví điện tử của khách hàng.
                else
                {
                    await _walletRefundCreditor.CreditRefundAsync(
                        order.AccountId, order.TotalAmount, order.OrderCode, order.OrderId, cancellationToken);
                    order.PaymentStatus = PaymentStatuses.Refunded;
                }
            }
            else if (order.PaymentMethod == "SE_PAY" && order.PaymentStatus == "PENDING")
            {
                // Cập nhật trạng thái thanh toán của SE_PAY: 
                // Nếu do hệ thống tự hủy (timeout job, accountId = 0) -> EXPIRED. 
                // Nếu do User/Admin chủ động hủy -> CANCELLED.
                order.PaymentStatus = cancelledByAccountId == 0 ? PaymentStatuses.Expired : PaymentStatuses.Cancelled;
            }

            // BƯỚC 4: Khôi phục lại giỏ hàng (chỉ áp dụng khi người dùng hủy luồng thanh toán QR)
            // - Đơn hàng COD/WALLET khi nhấn confirm đã bị xóa giỏ hàng, nếu hủy lúc chưa thanh toán thì khôi phục lại các mặt hàng.
            // - Đơn hàng SE_PAY không bị xóa giỏ hàng lúc confirm nên không cần khôi phục.
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
                            // Chỉ phục hồi các mặt hàng vừa mới bị xóa khỏi giỏ trong vòng 5 phút đổ lại để khớp với thời gian tạo đơn
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
            order.CancelledBy = cancelledByAccountId == 0 ? null : cancelledByAccountId;

            await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                StatusId = cancelledId,
                ChangedBy = cancelledByAccountId == 0 ? null : cancelledByAccountId,
                Note = $"Order cancelled: {reason}",
                CreatedAt = now
            }, cancellationToken);

            await _unitOfWork.OrderAssignments.ReleaseCapacityAsync(order.OrderId, cancellationToken);
            await _unitOfWork.OrderQueues.ResolveByOrderIdAsync(order.OrderId, cancellationToken);

            if (hasCreatedSystemRefund)
            {
                await _shiftAssignmentService.AutoAssignOrderAsync(order.OrderId, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} cancelled by {AccountId}. Reason: {Reason}",
                order.OrderId, cancelledByAccountId, reason);

            await TryReleaseCapacityAsync(order.OrderId, cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to cancel order {OrderId}", order.OrderId);
            throw;
        }
    }

    public async Task<Result<Order>> CompleteOrderAsync(int orderId, int? changedByAccountId = null, bool enforceOwnerCheck = false, CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Orders.GetByIdForUpdateAsync(orderId, cancellationToken);
        if (order is null) return Result<Order>.NotFound("Order", orderId);

        if (enforceOwnerCheck && changedByAccountId.HasValue && order.AccountId != changedByAccountId.Value)
            return Result<Order>.Failure("NOT_FOUND", "Order not found or you don't have permission to confirm this order.");

        var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(cancellationToken);
        if (!statusMap.TryGetValue(OrderStatuses.Completed, out var completedId))
            return Result<Order>.Failure("INTERNAL_ERROR", "Status 'Completed' not found.");

        if (order.StatusId == completedId) return Result<Order>.Success(order);

        if (statusMap.TryGetValue(OrderStatuses.Delivered, out var deliveredId) && order.StatusId != deliveredId)
        {
            return Result<Order>.UnprocessableEntity("Receipt can only be confirmed after the order has been successfully delivered.");
        }

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
                ChangedBy = changedByAccountId,
                Note = "Order completed",
                CreatedAt = now
            }, cancellationToken);

            await _unitOfWork.OrderAssignments.ReleaseCapacityAsync(orderId, cancellationToken);
            await _unitOfWork.OrderQueues.ResolveByOrderIdAsync(orderId, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            await TryReleaseCapacityAsync(orderId, cancellationToken);

            return Result<Order>.Success(order);
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

            await _unitOfWork.OrderQueues.ResolveByOrderIdAsync(orderId, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

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
            await _shiftAssignmentService.PublishCapacityFreedAsync(orderId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish capacity freed for order {OrderId}", orderId);
        }
    }

    private async Task CreateCancelledOrderSystemRefundAsync(
        Order order, int cancelledByAccountId, string cancelReason, CancellationToken cancellationToken)
    {
        // 1. Check if an active refund already exists to avoid duplicates
        var existing = await _unitOfWork.Refunds.GetAdminRefundsAsync(
            new AdminRefundFilterDto { OrderId = order.OrderId, PageSize = 10 },
            cancellationToken);

        if (existing.Items.Any(r =>
                r.RefundStatus is RefundStatuses.Requested or RefundStatuses.Approved or RefundStatuses.Completed))
        {
            _logger.LogInformation("Cancel {OrderCode}: an active refund request already exists. Skipping auto system refund.", order.OrderCode);
            return;
        }

        // 2. Resolve refund reason ID
        var reasonEntity = await _unitOfWork.Refunds.GetReasonByContentAsync(RefundReasons.DeliveryFailedGhn, cancellationToken);
        byte reasonId = reasonEntity?.RefundReasonId ?? 1;

        // Reload details if empty to calculate exact refund amounts
        var refundOrder = order;
        if (refundOrder.OrderDetails.Count == 0)
        {
            var reloadedOrder = await _unitOfWork.Orders.GetByIdForUpdateAsync(order.OrderId, cancellationToken);
            if (reloadedOrder != null)
            {
                refundOrder = reloadedOrder;
            }
        }

        // 3. Map refund details
        var discountRatio = refundOrder.SubTotal > 0
            ? (refundOrder.VoucherDiscountAmount / refundOrder.SubTotal)
            : 0m;

        var refundDetails = refundOrder.OrderDetails.Select(od =>
        {
            var netUnitPrice = od.Quantity > 0
                ? od.UnitPrice - (od.DiscountAmount / od.Quantity)
                : od.UnitPrice;
            return new RefundDetail
            {
                ProductId = od.ProductId,
                Quantity = od.Quantity,
                UnitPrice = netUnitPrice,
                RefundAmount = Math.Round(od.Quantity * netUnitPrice * (1 - discountRatio), 0),
                CreatedAt = _timeProvider.UtcNow
            };
        }).ToList();

        var subTotal = refundDetails.Sum(d => d.RefundAmount);
        var shippingFee = refundOrder.ActualShippingFee ?? refundOrder.EstimatedShippingFee;
        // Use the order's TotalAmount (already includes shipping and deducts voucher discount)
        // instead of manually computing subTotal + shippingFee which ignores VoucherDiscountAmount.
        var totalAmount = refundOrder.TotalAmount;
        var now = _timeProvider.UtcNow;

        // 4. Construct OrderRefund entity
        var refund = new OrderRefund
        {
            OrderId = refundOrder.OrderId,
            RefundReasonId = reasonId,
            ReasonDetails = $"Auto-created: Order cancelled by {(cancelledByAccountId == 0 ? "System" : "Admin")} (Reason: {cancelReason})",
            RefundSource = RefundSources.System, // Bypasses customer return windows
            CustomerId = refundOrder.AccountId,
            RequestedBy = cancelledByAccountId == 0 ? null : cancelledByAccountId,
            ApprovedAmount = totalAmount,
            RefundCode = "REF-" + now.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
            SubTotal = subTotal,
            ShippingFee = shippingFee,
            TotalAmount = totalAmount,
            StatusId = (byte)RefundStatusEnum.RefundRequested,
            IsDeleted = false,
            CreatedAt = now
        };

        refund.RefundStatusHistories.Add(new RefundStatusHistory
        {
            StatusId = (byte)RefundStatusEnum.RefundRequested,
            ChangedBy = cancelledByAccountId == 0 ? null : cancelledByAccountId,
            Note = $"Auto-created upon order cancellation: {cancelReason}",
            CreatedAt = now
        });

        foreach (var detail in refundDetails)
        {
            refund.RefundDetails.Add(detail);
        }

        // 5. Save to database
        await _unitOfWork.Refunds.AddAsync(refund, cancellationToken);
        _logger.LogInformation("Cancel {OrderCode}: successfully created auto system refund request {RefundCode} for {TotalAmount} VND.", 
            order.OrderCode, refund.RefundCode, totalAmount);
    }
}
