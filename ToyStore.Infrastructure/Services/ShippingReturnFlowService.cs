using Microsoft.Extensions.Logging;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Refunds;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Constants;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Enums;

namespace ToyStore.Infrastructure.Services;

/// <summary>
/// Service điều phối các hành động nghiệp vụ xử lý luồng Hoàn trả / Chuyển hoàn hàng hóa (Return Flow) được kích hoạt từ Webhook của đơn vị vận chuyển GHN.
/// </summary>
public class ShippingReturnFlowService : IShippingReturnFlowService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRefundService _refundService;
    private readonly ILogger<ShippingReturnFlowService> _logger;

    private static readonly HashSet<string> PrepaidMethods =
        new(StringComparer.OrdinalIgnoreCase) { "SE_PAY", "WALLET", "BANK_TRANSFER" };

    /// <summary>
    /// Khởi tạo ShippingReturnFlowService với UnitOfWork, RefundService và Logger.
    /// </summary>
    public ShippingReturnFlowService(
        IUnitOfWork unitOfWork,
        IRefundService refundService,
        ILogger<ShippingReturnFlowService> logger)
    {
        _unitOfWork = unitOfWork;
        _refundService = refundService;
        _logger = logger;
    }

    /// <summary>
    /// Xử lý hành động nghiệp vụ hoàn trả tương ứng dựa trên sự kiện trạng thái vận đơn từ GHN:
    /// - Giao hàng thất bại (HandleDeliveryFail)
    /// - Chuyển sang chờ hoàn / đang hoàn (SetReturning / HandleReturnStarted / KeepReturning)
    /// - Hoàn hàng thành công về kho (HandleReturnCompleted)
    /// - Hoàn hàng thất bại (HandleReturnFail)
    /// - Hàng hóa bị hỏng hoặc mất trong vận chuyển (HandleDamageLost)
    /// - Đơn vận chuyển bị hủy (HandleGhnCancel)
    /// </summary>
    /// <param name="action">Hành động webhook cần xử lý.</param>
    /// <param name="order">Thực thể đơn hàng tương ứng.</param>
    /// <param name="tx">Giao dịch vận chuyển liên quan.</param>
    /// <param name="ghnStatus">Mã trạng thái trả về từ GHN.</param>
    /// <param name="now">Thời điểm hiện tại (UTC).</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Kết quả xử lý luồng hoàn trả kèm danh sách thông báo cần phát.</returns>
    public async Task<ShippingReturnFlowResult> ProcessActionAsync(
        ShippingWebhookAction action,
        Order order,
        ShippingProviderTransaction tx,
        string ghnStatus,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        if (await IsTerminalOrderAsync(order, action, cancellationToken))
        {
            _logger.LogInformation(
                "Order {OrderId} is terminal (Cancelled/Refunded); skipping return-flow action {Action}",
                order.OrderId, action);
            return new ShippingReturnFlowResult();
        }

        return action switch
        {
            ShippingWebhookAction.HandleDeliveryFail =>
                await HandleDeliveryFailAsync(order, tx, ghnStatus, now, cancellationToken),
            ShippingWebhookAction.SetReturning =>
                await HandleSetReturningAsync(order, ghnStatus, now, cancellationToken),
            ShippingWebhookAction.HandleReturnStarted =>
                await HandleReturnStartedAsync(order, ghnStatus, now, cancellationToken),
            ShippingWebhookAction.KeepReturning =>
                await HandleKeepReturningAsync(order, ghnStatus, now, cancellationToken),
            ShippingWebhookAction.HandleReturnCompleted =>
                await HandleReturnCompletedAsync(
                    order,
                    !string.IsNullOrEmpty(order.CancelReason) && order.CancelReason != OrderCancelReasons.DeliveryFailedGhn
                        ? order.CancelReason
                        : ToyStore.Infrastructure.Mappers.GhnFailCodeMapper.GetFriendlyDescription(order.LastGHNFailCode, OrderCancelReasons.DeliveryFailedGhn),
                    now, cancellationToken),
            ShippingWebhookAction.HandleReturnFail =>
                await HandleReturnFailAsync(order, tx, ghnStatus, now, cancellationToken),
            ShippingWebhookAction.HandleDamageLost =>
                await HandleDamageLostAsync(order, ghnStatus, now, cancellationToken),
            ShippingWebhookAction.HandleGhnCancel =>
                await HandleGhnCancelAsync(order, now, cancellationToken),
            ShippingWebhookAction.HandleException =>
                await HandleExceptionAsync(order, ghnStatus, now, cancellationToken),
            _ => new ShippingReturnFlowResult()
        };
    }

    /// <summary>
    /// Xử lý sự kiện giao hàng thất bại (Delivery Fail) từ Webhook GHN:
    /// 1. Lấy mã trạng thái DeliveryFailed từ cơ sở dữ liệu.
    /// 2. Chuyển đổi mã lỗi GHN (LastGHNFailCode) sang lý do chi tiết dễ hiểu.
    /// 3. Cập nhật trạng thái đơn hàng sang DeliveryFailed và lưu lý do thất bại.
    /// 4. Đếm số lần giao thất bại trong lịch sử vận chuyển (Attempt count) và ghi nhận OrderStatusHistory.
    /// 5. Tạo thông báo sự kiện OrderDeliveryFailed gửi tới khách hàng.
    /// </summary>
    /// <param name="order">Thực thể đơn hàng.</param>
    /// <param name="tx">Giao dịch vận chuyển liên quan.</param>
    /// <param name="ghnStatus">Trạng thái vận chuyển GHN.</param>
    /// <param name="now">Thời gian hiện tại UTC.</param>
    /// <param name="ct">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Kết quả xử lý luồng hoàn trả chứa danh sách thông báo.</returns>
    private async Task<ShippingReturnFlowResult> HandleDeliveryFailAsync(
        Order order, ShippingProviderTransaction tx, string ghnStatus, DateTime now, CancellationToken ct)
    {
        var notifications = new List<PendingShippingNotification>();
        var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(ct);
        var deliveryFailedId = ResolveStatusId(statusMap, OrderStatuses.DeliveryFailed, OrderStatus.DeliveryFailed);

        // 1. Ánh xạ mã lỗi GHN sang mô tả thân thiện
        var friendlyReason = ToyStore.Infrastructure.Mappers.GhnFailCodeMapper.GetFriendlyDescription(
            order.LastGHNFailCode, order.CancelReason);

        if (string.IsNullOrEmpty(order.CancelReason) || order.CancelReason == OrderCancelReasons.DeliveryFailedGhn)
        {
            order.CancelReason = !string.IsNullOrEmpty(friendlyReason) && friendlyReason != OrderCancelReasons.DeliveryFailedGhn
                ? friendlyReason
                : OrderCancelReasons.DeliveryFailedGhn;
        }

        // 2. Cập nhật trạng thái đơn hàng sang DeliveryFailed
        order.StatusId = deliveryFailedId;
        order.UpdatedAt = now;

        var detailedReason = string.IsNullOrEmpty(order.CancelReason) || order.CancelReason == OrderCancelReasons.DeliveryFailedGhn
            ? "No detailed reason from shipping provider"
            : order.CancelReason;

        tx.LastErrorMessage = $"Delivery failed: {detailedReason} (Code: {order.LastGHNFailCode ?? "N/A"})";

        // 3. Đếm số lần giao thất bại trước đó để ghi nhận lần thử (attempt)
        var attempt = await _unitOfWork.Orders.CountShippingStatusHistoryAsync(
            tx.ShippingTransactionId, ShippingStatuses.DeliveryFail, ct);

        await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
        {
            OrderId = order.OrderId,
            StatusId = deliveryFailedId,
            ChangedBy = null,
            Note = $"GHN delivery fail attempt {attempt + 1}.\n Reason: {detailedReason}",
            CreatedAt = now
        }, ct);

        // 4. Phát sự kiện thông báo giao hàng thất bại
        notifications.Add(BuildOrderNotification(
            NotificationEventTypes.OrderDeliveryFailed, order, ghnStatus, tx.ProviderOrderCode));

        return new ShippingReturnFlowResult { Notifications = notifications };
    }

    /// <summary>
    /// Xử lý sự kiện bắt đầu chuyển hoàn hàng về kho (Return Started): Chuyển tiếp tới hàm HandleSetReturningAsync.
    /// </summary>
    private async Task<ShippingReturnFlowResult> HandleReturnStartedAsync(
        Order order, string ghnStatus, DateTime now, CancellationToken ct)
    {
        return await HandleSetReturningAsync(order, ghnStatus, now, ct);
    }

    /// <summary>
    /// Cập nhật trạng thái đơn hàng sang Chờ chuyển hoàn (WaitingReturn):
    /// - Ghi nhận lịch sử trạng thái chờ shipper đến lấy hàng hoàn về shop.
    /// - Phát sự kiện thông báo đơn hàng bắt đầu chuyển hoàn (OrderReturning).
    /// </summary>
    private async Task<ShippingReturnFlowResult> HandleSetReturningAsync(
        Order order, string ghnStatus, DateTime now, CancellationToken ct)
    {
        var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(ct);
        var waitingReturnId = ResolveStatusId(statusMap, OrderStatuses.WaitingReturn, OrderStatus.WaitingReturn);

        if (order.StatusId == waitingReturnId)
            return new ShippingReturnFlowResult();

        order.StatusId = waitingReturnId;
        order.UpdatedAt = now;

        await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
        {
            OrderId = order.OrderId,
            StatusId = waitingReturnId,
            ChangedBy = null,
            Note = $"GHN {ghnStatus}: waiting for courier to pick up return package",
            CreatedAt = now
        }, ct);

        return new ShippingReturnFlowResult
        {
            Notifications =
            [
                BuildOrderNotification(NotificationEventTypes.OrderReturning, order, ghnStatus, null)
            ]
        };
    }

    /// <summary>
    /// Xử lý cập nhật trạng thái khi hàng đang trong quá trình chuyển hoàn về kho (Returning).
    /// </summary>
    private async Task<ShippingReturnFlowResult> HandleKeepReturningAsync(
        Order order, string ghnStatus, DateTime now, CancellationToken ct)
    {
        var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(ct);
        var returningId = ResolveStatusId(statusMap, OrderStatuses.Returning, OrderStatus.Returning);

        if (order.StatusId != returningId)
        {
            order.StatusId = returningId;
            order.UpdatedAt = now;
            await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                StatusId = returningId,
                ChangedBy = null,
                Note = $"GHN {ghnStatus}",
                CreatedAt = now
            }, ct);
        }
        else
        {
            await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                StatusId = order.StatusId,
                ChangedBy = null,
                Note = $"GHN {ghnStatus}",
                CreatedAt = now
            }, ct);
        }

        return new ShippingReturnFlowResult();
    }

    /// <summary>
    /// Xử lý khi kiện hàng đã hoàn về kho shop thành công (Return Completed):
    /// 1. Đối với đơn trả trước (Prepaid: Ví, Chuyển khoản, SePay): Chuyển trạng thái đơn sang Cancelled ngay khi nhận kho và lưu lý do.
    /// 2. Đối với đơn COD: Chuyển sang ReturnCompleted hoặc hủy đơn qua ApplyReturnPaymentBranchAsync.
    /// 3. Kích hoạt phân nhánh thanh toán hoàn trả ApplyReturnPaymentBranchAsync để tạo yêu cầu hoàn trả hệ thống cho Thủ kho kiểm tra.
    /// </summary>
    private async Task<ShippingReturnFlowResult> HandleReturnCompletedAsync(
        Order order, string cancelReason, DateTime now, CancellationToken ct)
    {
        var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(ct);
        var returnCompletedId = ResolveStatusId(statusMap, OrderStatuses.ReturnCompleted, OrderStatus.ReturnCompleted);
        var cancelledId = ResolveStatusId(statusMap, OrderStatuses.Cancelled, OrderStatus.Cancelled);

        // Trường hợp đơn đã ở trạng thái ReturnCompleted trước đó
        if (order.StatusId == returnCompletedId)
        {
            if (PrepaidMethods.Contains(order.PaymentMethod))
            {
                return await ApplyReturnPaymentBranchAsync(order, cancelReason, now, ct, skipReturnCompletedStep: true);
            }

            return new ShippingReturnFlowResult();
        }

        // Trường hợp đơn đã ở trạng thái Cancelled
        if (order.StatusId == cancelledId)
        {
            var existingRefunds = await _unitOfWork.Refunds.GetAdminRefundsAsync(
                new AdminRefundFilterDto { OrderId = order.OrderId, PageSize = 1 },
                ct);

            if (existingRefunds.Items.Any() || !(string.Equals(order.CancelReason, OrderCancelReasons.DeliveryFailedGhn, StringComparison.OrdinalIgnoreCase)
                 && PrepaidMethods.Contains(order.PaymentMethod)))
            {
                return new ShippingReturnFlowResult();
            }
        }

        // Trường hợp đơn thanh toán khi nhận hàng (SHIP_COD)
        if (string.Equals(order.PaymentMethod, "SHIP_COD", StringComparison.OrdinalIgnoreCase))
        {
            return await ApplyReturnPaymentBranchAsync(order, cancelReason, now, ct);
        }

        // Đối với đơn hàng trả trước (Prepaid): Chuyển trạng thái đơn hàng sang Cancelled ngay khi hàng về đến kho
        if (PrepaidMethods.Contains(order.PaymentMethod))
        {
            order.StatusId = cancelledId;
            order.CancelledAt = now;
            order.CompletedAt = null; // Enforce NOT ([CompletedAt] IS NOT NULL AND [CancelledAt] IS NOT NULL)
            // Chỉ ghi đè lý do hủy nếu chưa có lý do cụ thể từ mã lỗi GHN
            if (string.IsNullOrEmpty(order.CancelReason) || order.CancelReason == OrderCancelReasons.DeliveryFailedGhn)
                order.CancelReason = cancelReason;
            order.UpdatedAt = now;

            await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                StatusId = cancelledId,
                ChangedBy = null,
                Note = null,
                CreatedAt = now
            }, ct);
        }
        else
        {
            order.StatusId = returnCompletedId;
            order.UpdatedAt = now;

            await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                StatusId = returnCompletedId,
                ChangedBy = null,
                Note = null,
                CreatedAt = now
            }, ct);
        }

        return await ApplyReturnPaymentBranchAsync(order, cancelReason, now, ct);
    }

    /// <summary>
    /// Xử lý khi quá trình chuyển hoàn hàng về shop bị thất bại (Return Fail):
    /// - Chuyển trạng thái đơn sang ReturnFailed.
    /// - Ghi nhận thông báo lỗi yêu cầu quản trị viên can thiệp xử lý thủ công với đơn vị vận chuyển.
    /// - Phát sự kiện OrderReturnFail.
    /// </summary>
    private async Task<ShippingReturnFlowResult> HandleReturnFailAsync(
        Order order, ShippingProviderTransaction tx, string ghnStatus, DateTime now, CancellationToken ct)
    {
        tx.LastErrorMessage = "GHN return_fail";
        var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(ct);
        var returnFailedId = ResolveStatusId(statusMap, OrderStatuses.ReturnFailed, OrderStatus.ReturnFailed);

        order.StatusId = returnFailedId;
        order.UpdatedAt = now;

        await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
        {
            OrderId = order.OrderId,
            StatusId = returnFailedId,
            ChangedBy = null,
            Note = "GHN return_fail: courier failed to return items. Requires manual admin action.",
            CreatedAt = now
        }, ct);

        return new ShippingReturnFlowResult
        {
            Notifications =
            [
                new PendingShippingNotification(
                    NotificationEventTypes.OrderReturnFail,
                    new
                    {
                        orderId = order.OrderId,
                        orderCode = order.OrderCode,
                        providerOrderCode = tx.ProviderOrderCode ?? "",
                        providerStatus = ghnStatus
                    })
            ]
        };
    }

    /// <summary>
    /// Xử lý sự kiện hàng hóa bị thất lạc (Lost) hoặc hư hỏng (Damage) trong quá trình vận chuyển.
    /// </summary>
    private async Task<ShippingReturnFlowResult> HandleDamageLostAsync(
        Order order, string ghnStatus, DateTime now, CancellationToken ct)
    {
        var cancelReason = ghnStatus.Equals(ShippingStatuses.Lost, StringComparison.OrdinalIgnoreCase)
            ? OrderCancelReasons.LostInTransit
            : OrderCancelReasons.DamagedInTransit;

        return await ApplyDirectCancelReturnAsync(order, cancelReason, ghnStatus, now, ct);
    }

    /// <summary>
    /// Xử lý sự kiện đơn vận chuyển bị hủy bởi GHN (GhnCancel).
    /// </summary>
    private async Task<ShippingReturnFlowResult> HandleGhnCancelAsync(
        Order order, DateTime now, CancellationToken ct)
    {
        var friendlyReason = ToyStore.Infrastructure.Mappers.GhnFailCodeMapper.GetFriendlyDescription(
            order.LastGHNFailCode,
            !string.IsNullOrWhiteSpace(order.CancelReason) && order.CancelReason != OrderCancelReasons.GhnCancelled
                ? order.CancelReason
                : "Shop did not hand over items to courier / Shipment cancelled");

        var cancelReason = !string.IsNullOrEmpty(friendlyReason)
            ? friendlyReason
            : "Shop did not hand over items to courier / Shipment cancelled";

        return await ApplyDirectCancelReturnAsync(
            order, cancelReason, ShippingStatuses.Cancel, now, ct);
    }

    /// <summary>
    /// Hủy đơn hàng trực tiếp và xử lý chuyển tiếp hoàn trả khi gặp sự cố vận chuyển (Lost / Damaged / Cancelled):
    /// 1. Nếu là đơn SHIP_COD: Gọi ApplyCodCancelForDeliveryFailAsync để hủy đơn và giải phóng tải ca trực (ReleaseShiftCapacity = true).
    /// 2. Nếu là đơn trả trước: Cập nhật đơn sang trạng thái Lost/Damaged/Cancelled tương ứng, ghi nhận lịch sử và kích hoạt luồng hoàn tiền.
    /// </summary>
    private async Task<ShippingReturnFlowResult> ApplyDirectCancelReturnAsync(
        Order order, string cancelReason, string ghnStatus, DateTime now, CancellationToken ct)
    {
        if (string.Equals(order.PaymentMethod, "SHIP_COD", StringComparison.OrdinalIgnoreCase))
        {
            await ApplyCodCancelForDeliveryFailAsync(order, cancelReason, now, ct);

            return new ShippingReturnFlowResult
            {
                Notifications =
                [
                    new PendingShippingNotification(
                        NotificationEventTypes.OrderCancelledDeliveryFail,
                        new { orderId = order.OrderId, orderCode = order.OrderCode })
                ],
                ReleaseShiftCapacity = true
            };
        }

        var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(ct);
        
        byte targetStatusId;
        if (ghnStatus.Equals(ShippingStatuses.Lost, StringComparison.OrdinalIgnoreCase))
        {
            targetStatusId = ResolveStatusId(statusMap, OrderStatuses.Lost, OrderStatus.Lost);
        }
        else if (ghnStatus.Equals(ShippingStatuses.Damage, StringComparison.OrdinalIgnoreCase))
        {
            targetStatusId = ResolveStatusId(statusMap, OrderStatuses.Damaged, OrderStatus.Damaged);
        }
        else
        {
            targetStatusId = ResolveStatusId(statusMap, OrderStatuses.Cancelled, OrderStatus.Cancelled);
        }

        order.StatusId = targetStatusId;
        order.CancelReason = cancelReason;
        order.CancelledAt = now;
        order.CompletedAt = null; // Enforce NOT ([CompletedAt] IS NOT NULL AND [CancelledAt] IS NOT NULL)
        order.UpdatedAt = now;

        var noteDetail = !string.IsNullOrWhiteSpace(order.LastGHNFailCode)
            ? $"GHN {ghnStatus} ({order.LastGHNFailCode}): {cancelReason}"
            : $"GHN {ghnStatus}: {cancelReason}";

        await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
        {
            OrderId = order.OrderId,
            StatusId = targetStatusId,
            ChangedBy = null,
            Note = noteDetail,
            CreatedAt = now
        }, ct);

        return await ApplyReturnPaymentBranchAsync(order, cancelReason, now, ct, skipReturnCompletedStep: true);
    }

    /// <summary>
    /// Xử lý các trường hợp ngoại lệ bất thường từ Webhook GHN: Ghi log lịch sử và gửi thông báo cảnh báo hệ thống.
    /// </summary>
    private async Task<ShippingReturnFlowResult> HandleExceptionAsync(
        Order order, string ghnStatus, DateTime now, CancellationToken ct)
    {
        await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
        {
            OrderId = order.OrderId,
            StatusId = order.StatusId,
            ChangedBy = null,
            Note = $"GHN exception: {ghnStatus}: Requires admin review",
            CreatedAt = now
        }, ct);

        return new ShippingReturnFlowResult
        {
            Notifications =
            [
                new PendingShippingNotification(
                    NotificationEventTypes.SystemShippingWebhookError,
                    new
                    {
                        orderId = order.OrderId,
                        orderCode = order.OrderCode,
                        providerStatus = ghnStatus,
                        message = $"GHN exception on order {order.OrderCode}"
                    })
            ]
        };
    }

    /// <summary>
    /// Xử lý phân nhánh nghiệp vụ thanh toán / hoàn tiền khi hàng được chuyển hoàn về kho hoặc bị hỏng/mất:
    /// - Nhánh SHIP_COD:
    ///   + Gọi ApplyCodCancelForDeliveryFailAsync hủy đơn và khôi phục voucher.
    ///   + Tự động tạo bản ghi Hoàn trả Hệ thống (System Refund) thông qua CreateSystemRefundForDeliveryFailAsync để Thủ kho kiểm kê chất lượng.
    ///   + Gửi thông báo MerchReturned cho Thủ kho kiểm tra hàng thực tế.
    /// - Nhánh Trả trước (Prepaid):
    ///   + Lấy lý do hoàn trả DeliveryFailedGhn.
    ///   + Tự động tạo bản ghi Hoàn trả Hệ thống (CreateSystemRefundForDeliveryFailAsync).
    ///   + Gửi thông báo OrderReturnRefundPending cho khách hàng và MerchReturned cho Thủ kho.
    /// </summary>
    private async Task<ShippingReturnFlowResult> ApplyReturnPaymentBranchAsync(
        Order order,
        string cancelReason,
        DateTime now,
        CancellationToken ct,
        bool skipReturnCompletedStep = false)
    {
        var notifications = new List<PendingShippingNotification>();

        // 1. Phân nhánh đơn SHIP_COD
        if (string.Equals(order.PaymentMethod, "SHIP_COD", StringComparison.OrdinalIgnoreCase))
        {
            await ApplyCodCancelForDeliveryFailAsync(order, cancelReason, now, ct, restoreStock: false);

            notifications.Add(new PendingShippingNotification(
                NotificationEventTypes.OrderCancelledDeliveryFail,
                new { orderId = order.OrderId, orderCode = order.OrderCode }));

            var reason = await _unitOfWork.Refunds.GetReasonByContentAsync(RefundReasons.DeliveryFailedGhn, ct);
            if (reason is null)
            {
                _logger.LogError("Refund reason '{Reason}' not found cannot create system refund for COD order {OrderId}",
                    RefundReasons.DeliveryFailedGhn, order.OrderId);
            }
            else
            {
                byte? initialStatusId = null;
                if (string.Equals(cancelReason, OrderCancelReasons.LostInTransit, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(cancelReason, OrderCancelReasons.DamagedInTransit, StringComparison.OrdinalIgnoreCase))
                {
                    initialStatusId = (byte)RefundStatusEnum.RefundDamage;
                }

                // Tạo yêu cầu hoàn tiền hệ thống (System Return) cho đơn COD
                var refund = await _refundService.CreateSystemRefundForDeliveryFailAsync(order, reason.RefundReasonId, initialStatusId, ct);
                if (refund is not null)
                {
                    notifications.Add(new PendingShippingNotification(
                        NotificationEventTypes.OrderReturnRefundPending,
                        new { orderId = order.OrderId, orderCode = order.OrderCode }));
                }

                // Gửi thông báo cho Thủ kho (Merchandise) tiến hành kiểm tra kiện hàng hoàn
                if (!skipReturnCompletedStep)
                {
                    notifications.Add(new PendingShippingNotification(
                        NotificationEventTypes.MerchReturned,
                        new
                        {
                            orderId    = order.OrderId,
                            orderCode  = order.OrderCode,
                            refundId   = refund?.RefundId ?? 0,
                            providerStatus = ShippingStatuses.Returned
                        }));
                }
            }

            return new ShippingReturnFlowResult
            {
                Notifications = notifications,
                ReleaseShiftCapacity = true
            };
        }

        // 2. Phân nhánh đơn thanh toán trả trước (Prepaid: SE_PAY, WALLET, BANK_TRANSFER)
        if (PrepaidMethods.Contains(order.PaymentMethod))
        {
            var reason = await _unitOfWork.Refunds.GetReasonByContentAsync(RefundReasons.DeliveryFailedGhn, ct);
            if (reason is null)
            {
                _logger.LogError("Refund reason '{Reason}' not found cannot create system refund for order {OrderId}",
                    RefundReasons.DeliveryFailedGhn, order.OrderId);
                return new ShippingReturnFlowResult { Notifications = notifications };
            }

            byte? initialStatusId = null;
            if (string.Equals(cancelReason, OrderCancelReasons.LostInTransit, StringComparison.OrdinalIgnoreCase)
                || string.Equals(cancelReason, OrderCancelReasons.DamagedInTransit, StringComparison.OrdinalIgnoreCase))
            {
                initialStatusId = (byte)RefundStatusEnum.RefundDamage;
            }

            // Tạo yêu cầu hoàn tiền hệ thống để nhân viên kiểm tra và hoàn tiền vào ví khách
            var refund = await _refundService.CreateSystemRefundForDeliveryFailAsync(order, reason.RefundReasonId, initialStatusId, ct);
            if (refund is not null)
            {
                notifications.Add(new PendingShippingNotification(
                    NotificationEventTypes.OrderReturnRefundPending,
                    new { orderId = order.OrderId, orderCode = order.OrderCode }));
            }

            // Gửi thông báo cho Thủ kho (Merchandise) kiểm tra hàng thực tế tại kho
            if (!skipReturnCompletedStep)
            {
                notifications.Add(new PendingShippingNotification(
                    NotificationEventTypes.MerchReturned,
                    new
                    {
                        orderId    = order.OrderId,
                        orderCode  = order.OrderCode,
                        refundId   = refund?.RefundId ?? 0,
                        providerStatus = ShippingStatuses.Returned
                    }));
            }
        }

        return new ShippingReturnFlowResult { Notifications = notifications };
    }

    /// <summary>
    /// Thực hiện hủy đơn hàng SHIP_COD do giao hàng không thành công:
    /// 1. Khôi phục số lượng tồn kho sản phẩm (nếu restoreStock = true).
    /// 2. Khôi phục tồn kho Flash Sale tương ứng nếu có sản phẩm trong chương trình Flash Sale.
    /// 3. Khôi phục lại voucher giảm giá đã áp dụng cho đơn hàng.
    /// 4. Đổi trạng thái đơn sang Cancelled, PaymentStatus = CANCELLED và lưu PaymentHistory.
    /// 5. Đồng bộ lại dữ liệu của thực thể order đang được theo dõi.
    /// </summary>
    private async Task ApplyCodCancelForDeliveryFailAsync(
        Order order, string cancelReason, DateTime now, CancellationToken ct, bool restoreStock = true)
    {
        var fullOrder = await _unitOfWork.Orders.GetByIdForUpdateAsync(order.OrderId, ct)
            ?? order;

        var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(ct);
        var cancelledId = ResolveStatusId(statusMap, OrderStatuses.Cancelled, OrderStatus.Cancelled);

        bool productsSubtracted = fullOrder.PaymentStatus != "PAID" && restoreStock;

        // 1. Khôi phục tồn kho chính và flash sale cho từng sản phẩm
        foreach (var detail in fullOrder.OrderDetails)
        {
            if (productsSubtracted)
                await _unitOfWork.Orders.AdjustStockAsync(detail.ProductId, detail.Quantity, ct);

            if (detail.SlotProductId.HasValue && restoreStock)
            {
                if (fullOrder.PaymentStatus == "PAID")
                    await _unitOfWork.Orders.AdjustFlashSaleStockAsync(
                        detail.SlotProductId.Value, -detail.Quantity, 0, ct);
                else
                    await _unitOfWork.Orders.AdjustFlashSaleStockAsync(
                        detail.SlotProductId.Value, -detail.Quantity, 0, ct);
            }
        }

        // 2. Khôi phục voucher của khách hàng
        await _unitOfWork.Orders.RestoreVoucherAsync(fullOrder.OrderId, ct);

        // 3. Cập nhật trạng thái đơn hàng và thanh toán sang CANCELLED
        fullOrder.StatusId = cancelledId;
        fullOrder.CancelledAt = now;
        fullOrder.CompletedAt = null; // Enforce NOT ([CompletedAt] IS NOT NULL AND [CancelledAt] IS NOT NULL)
        fullOrder.CancelReason = cancelReason;
        fullOrder.PaymentStatus = "CANCELLED";
        fullOrder.UpdatedAt = now;

        var noteDetail = !string.IsNullOrWhiteSpace(fullOrder.LastGHNFailCode)
            ? $"Order cancelled ({fullOrder.LastGHNFailCode}): {cancelReason}"
            : $"Order cancelled: {cancelReason}";

        await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
        {
            OrderId = fullOrder.OrderId,
            StatusId = cancelledId,
            ChangedBy = null,
            Note = noteDetail,
            CreatedAt = now
        }, ct);

        await _unitOfWork.Orders.AddPaymentHistoryAsync(new PaymentHistory
        {
            AccountId = fullOrder.AccountId,
            OrderId = fullOrder.OrderId,
            PaymentStatus = "CANCELLED",
            PaymentMethod = fullOrder.PaymentMethod,
            Amount = fullOrder.TotalAmount,
            CreatedAt = now
        }, ct);

        // Đồng bộ dữ liệu tham chiếu
        order.StatusId = fullOrder.StatusId;
        order.CancelledAt = fullOrder.CancelledAt;
        order.CancelReason = fullOrder.CancelReason;
        order.PaymentStatus = fullOrder.PaymentStatus;
        order.UpdatedAt = fullOrder.UpdatedAt;
    }

    /// <summary>
    /// Helper đóng gói đối tượng thông báo vận chuyển (PendingShippingNotification).
    /// </summary>
    private static PendingShippingNotification BuildOrderNotification(
        string eventType, Order order, string providerStatus, string? providerOrderCode)
        => new(eventType, new
        {
            orderId = order.OrderId,
            orderCode = order.OrderCode,
            providerStatus,
            providerOrderCode = providerOrderCode ?? ""
        });

    /// <summary>
    /// Kiểm tra xem đơn hàng đã chuyển sang trạng thái kết thúc (Terminal State) hay chưa.
    /// Các trạng thái kết thúc bao gồm: Refunded, ReturnCompleted, ReturnFailed, Lost, Damaged, Cancelled.
    /// </summary>
    private async Task<bool> IsTerminalOrderAsync(Order order, ShippingWebhookAction action, CancellationToken ct)
    {
        var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(ct);
        var cancelled = ResolveStatusId(statusMap, OrderStatuses.Cancelled, OrderStatus.Cancelled);
        var completed = ResolveStatusId(statusMap, OrderStatuses.Completed, OrderStatus.Completed);
        var refunded = ResolveStatusId(statusMap, OrderStatuses.Refunded, OrderStatus.Refunded);
        var returnCompleted = ResolveStatusId(statusMap, OrderStatuses.ReturnCompleted, OrderStatus.ReturnCompleted);
        var returnFailed = ResolveStatusId(statusMap, OrderStatuses.ReturnFailed, OrderStatus.ReturnFailed);
        var lost = ResolveStatusId(statusMap, OrderStatuses.Lost, OrderStatus.Lost);
        var damaged = ResolveStatusId(statusMap, OrderStatuses.Damaged, OrderStatus.Damaged);

        if (order.StatusId == completed ||
            order.StatusId == refunded || 
            order.StatusId == returnCompleted || 
            order.StatusId == returnFailed || 
            order.StatusId == lost || 
            order.StatusId == damaged)
        {
            return true;
        }

        if (order.StatusId == cancelled)
        {
            var canRecoverFromCancel = action == ShippingWebhookAction.HandleReturnCompleted
                && string.Equals(order.CancelReason, OrderCancelReasons.DeliveryFailedGhn, StringComparison.OrdinalIgnoreCase)
                && PrepaidMethods.Contains(order.PaymentMethod);

            return !canRecoverFromCancel;
        }

        return false;
    }

    /// <summary>
    /// Lấy ID trạng thái đơn hàng từ StatusMap trong cơ sở dữ liệu, nếu không tìm thấy sẽ fallback về enum mặc định.
    /// </summary>
    private byte ResolveStatusId(
        Dictionary<string, byte> statusMap,
        string statusName,
        OrderStatus enumFallback)
    {
        if (statusMap.TryGetValue(statusName, out var id))
            return id;

        _logger.LogError(
            "Status '{Status}' missing from StatusOrders run 20260526_ReturnFlow_StatusAndRefundReason.sql. Using enum fallback {FallbackId}.",
            statusName, (byte)enumFallback);
        return (byte)enumFallback;
    }
}
