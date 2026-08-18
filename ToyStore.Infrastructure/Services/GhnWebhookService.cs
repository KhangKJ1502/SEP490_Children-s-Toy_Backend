using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Orders;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Constants;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Enums;
using ToyStore.Application.Services;
using ToyStore.Infrastructure.Mappers;

using System.Net.Http;

namespace ToyStore.Infrastructure.Services;

/// <summary>
/// Dịch vụ chuyên trách xử lý Webhook phản hồi từ Giao Hàng Nhanh (GHN):
/// - Xử lý thông tin hình ảnh chứng từ giao hàng POD (tải dạng URL hoặc dạng luồng Stream nhị phân lên Cloudinary).
/// - Phân loại xử lý riêng nếu mã vận đơn thuộc về Đơn hoàn tiền (Refund).
/// - Khôi phục đơn hàng về trạng thái Processing khi GHN giao hàng/lấy hàng thất bại (Pick Fail).
/// - Đồng bộ phí giao hàng và số tiền COD thực tế thu được từ GHN.
/// - Đồng bộ trạng thái đơn hàng nội bộ và phát sự kiện thông báo (Notification Events).
/// - Đưa tài khoản khách hàng vào danh sách nghi vấn nếu mã thất bại có dấu hiệu cố tình từ chối nhận.
/// </summary>
public class GhnWebhookService : IGhnWebhookService
{
    private const string WebhookSource = "GHN_WEBHOOK";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ILogger<GhnWebhookService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly IOrderLifecycleService _orderLifecycle;
    private readonly IShippingReturnFlowService _returnFlow;
    private readonly IShiftAssignmentService _shiftAssignmentService;
    private readonly IShippingStatusMapper _statusMapper;
    private readonly IShippingWebhookService _shippingWebhookService;
    private readonly IImageUploadService _imageUploadService;
    private readonly IHttpClientFactory _httpClientFactory;

    public GhnWebhookService(
        IUnitOfWork unitOfWork,
        IDomainEventPublisher eventPublisher,
        ILogger<GhnWebhookService> logger,
        ITimeProvider timeProvider,
        IOrderLifecycleService orderLifecycle,
        IShippingReturnFlowService returnFlow,
        IShiftAssignmentService shiftAssignmentService,
        IShippingStatusMapper statusMapper,
        IShippingWebhookService shippingWebhookService,
        IImageUploadService imageUploadService,
        IHttpClientFactory httpClientFactory)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _logger = logger;
        _timeProvider = timeProvider;
        _orderLifecycle = orderLifecycle;
        _returnFlow = returnFlow;
        _shiftAssignmentService = shiftAssignmentService;
        _statusMapper = statusMapper;
        _shippingWebhookService = shippingWebhookService;
        _imageUploadService = imageUploadService;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Tiếp nhận và xử lý payload Webhook từ GHN (không kèm file đính kèm).
    /// </summary>
    public Task ProcessAsync(GhnWebhookPayload payload, CancellationToken cancellationToken = default)
    {
        return ProcessAsync(payload, imageStream: null, fileName: null, cancellationToken);
    }

    /// <summary>
    /// Xử lý Webhook từ GHN kèm theo luồng dữ liệu hình ảnh POD nhị phân (nếu có).
    /// </summary>
    /// <param name="payload">Đối tượng dữ liệu Webhook từ GHN</param>
    /// <param name="imageStream">Luồng dữ liệu file ảnh nhị phân đính kèm</param>
    /// <param name="fileName">Tên file ảnh</param>
    /// <param name="cancellationToken">Token hủy tác vụ</param>
    public async Task ProcessAsync(
        GhnWebhookPayload payload, 
        Stream? imageStream, 
        string? fileName, 
        CancellationToken cancellationToken = default)
    {
        if (payload == null)
        {
            _logger.LogWarning("GHN Webhook: Nhận payload rỗng (Null).");
            return;
        }

        var orderCode = (payload.EffectiveOrderCode ?? payload.EffectiveClientOrderCode)?.Trim();
        if (string.IsNullOrWhiteSpace(orderCode))
        {
            _logger.LogWarning("GHN Webhook: Payload không hợp lệ hoặc OrderCode trống.");
            return;
        }

        // 1. Kiểm tra xem OrderCode này có thuộc về Đơn hoàn tiền / Trả hàng (Refund) hay không
        var refund = await _unitOfWork.Refunds.GetByShippingOrReturnOrderCodeAsync(orderCode, cancellationToken);
        if (refund is null && !string.IsNullOrWhiteSpace(payload.EffectiveClientOrderCode))
        {
            refund = await _unitOfWork.Refunds.GetByShippingOrReturnOrderCodeAsync(payload.EffectiveClientOrderCode.Trim(), cancellationToken);
        }

        // Nếu thuộc đơn Refund -> Chuyển giao trách nhiệm xử lý cho ShippingWebhookService
        if (refund is not null)
        {
            _logger.LogInformation("GHN Webhook: OrderCode '{Code}' khớp với yêu cầu refund. Đang chuyển tiếp sang IShippingWebhookService.", orderCode);
            await _shippingWebhookService.HandleAsync("GHN", JsonSerializer.Serialize(payload), cancellationToken);
            return;
        }

        // 2. Tìm giao dịch vận chuyển của đơn hàng thường trong cơ sở dữ liệu
        var tx = await _unitOfWork.Orders.GetShippingTransactionByProviderCodeAsync(orderCode, cancellationToken);
        if (tx == null && !string.IsNullOrWhiteSpace(payload.EffectiveClientOrderCode))
        {
            tx = await _unitOfWork.Orders.GetShippingTransactionByProviderCodeAsync(payload.EffectiveClientOrderCode.Trim(), cancellationToken);
        }

        if (tx == null)
        {
            _logger.LogWarning("GHN Webhook: Không tìm thấy giao dịch vận chuyển cho OrderCode '{Code}'", orderCode);
            return;
        }

        // Xác thực chéo cho đơn hàng thường:
        if (!string.IsNullOrWhiteSpace(payload.EffectiveOrderCode) && !string.IsNullOrWhiteSpace(payload.EffectiveClientOrderCode) && tx.Order != null)
        {
            bool matchesOrder = string.Equals(payload.EffectiveOrderCode.Trim(), tx.ProviderOrderCode, StringComparison.OrdinalIgnoreCase)
                             || string.Equals(payload.EffectiveOrderCode.Trim(), tx.Order.ShippingOrderCode, StringComparison.OrdinalIgnoreCase);
            bool matchesClient = string.Equals(payload.EffectiveClientOrderCode.Trim(), tx.Order.OrderCode, StringComparison.OrdinalIgnoreCase)
                              || string.Equals(payload.EffectiveClientOrderCode.Trim(), tx.ProviderOrderCode, StringComparison.OrdinalIgnoreCase);

            if (!matchesOrder || !matchesClient)
            {
                _logger.LogWarning(
                    "GHN Webhook Mismatch cho Order: ClientOrderCode '{ClientCode}' (Order #{OrderCode}) không khớp với OrderCode '{OrderCode}' (Mã GHN trong DB: '{ExpectedCode}'). Bỏ qua cập nhật.",
                    payload.EffectiveClientOrderCode, tx.Order.OrderCode, payload.EffectiveOrderCode, tx.ProviderOrderCode);
                return;
            }
        }

        // Kiểm tra bổ sung an toàn: Nếu tx thuộc về đơn refund thì chuyển tiếp sang ShippingWebhookService
        if (tx.OrderId > 0)
        {
            var existingRefund = await _unitOfWork.Refunds.GetByOrderIdAsync(tx.OrderId, cancellationToken);
            if (existingRefund is not null && string.Equals(existingRefund.RefundSource, RefundSources.Customer, StringComparison.OrdinalIgnoreCase))
            {
                bool isRefundShipment =
                    string.Equals(orderCode, existingRefund.ShippingOrderCode, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(orderCode, existingRefund.ReturnShippingOrderCode, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(tx.ProviderOrderCode, existingRefund.ShippingOrderCode, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(tx.ProviderOrderCode, existingRefund.ReturnShippingOrderCode, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(payload.EffectiveClientOrderCode) &&
                        (payload.EffectiveClientOrderCode.StartsWith("R-", StringComparison.OrdinalIgnoreCase) ||
                         payload.EffectiveClientOrderCode.StartsWith("R2-", StringComparison.OrdinalIgnoreCase) ||
                         payload.EffectiveClientOrderCode.StartsWith("REF-", StringComparison.OrdinalIgnoreCase)));

                if (isRefundShipment)
                {
                    _logger.LogInformation("GHN Webhook: Giao dịch '{Code}' thuộc về đơn refund {RefundId}. Đang chuyển tiếp sang IShippingWebhookService.", orderCode, existingRefund.RefundId);
                    await _shippingWebhookService.HandleAsync("GHN", JsonSerializer.Serialize(payload), cancellationToken);
                    return;
                }
            }
        }

        var status = payload.EffectiveStatus?.Trim();

        // 3. Nếu trạng thái gửi sang bị rỗng nhưng có kèm ảnh minh chứng POD -> Chỉ thực hiện cập nhật ảnh POD
        if (string.IsNullOrWhiteSpace(status))
        {
            if (imageStream != null)
            {
                await UploadDeliveryImageStreamToCloudinaryAsync(tx.Order, imageStream, fileName, cancellationToken);
            }
            else
            {
                var podImageUrl = payload.GetDeliveryImageUrl();
                if (!string.IsNullOrWhiteSpace(podImageUrl))
                {
                    await ProcessDeliveryImageOnlyAsync(tx.Order, podImageUrl, cancellationToken);
                }
                else
                {
                    _logger.LogWarning("GHN Webhook: Trạng thái rỗng và không có ảnh giao hàng trong payload cho OrderCode '{Code}'.", orderCode);
                }
            }
            return;
        }

        var rawPayload = JsonSerializer.Serialize(payload);

        // 4. Kiểm tra Idempotency (Bỏ qua nếu webhook đã được xử lý trước đó)
        if (await _unitOfWork.Orders.ExistsShippingStatusHistoryAsync(tx.ShippingTransactionId, status, rawPayload, cancellationToken))
        {
            _logger.LogInformation("Bỏ qua GHN Webhook trùng lặp: code={Code}, status={Status}", orderCode, status);
            return;
        }

        var previousStatus = tx.Status ?? string.Empty;
        var now = _timeProvider.UtcNow;

        var pendingNotifications = new List<PendingShippingNotification>();
        var releaseCapacity = false;
        var orderIdForCapacity = tx.OrderId;
        // Khi true: Mục B (Action Dispatch) sẽ bị bỏ qua vì Mục A3 đã xử lý xong luồng
        var skipActionDispatch = false;

        // 5. Bắt đầu Database Transaction
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // A1. Thêm bản ghi nhật ký lịch sử trạng thái vận chuyển
            await _unitOfWork.Orders.AddShippingStatusHistoryAsync(new ShippingStatusHistory
            {
                ShippingTxId = tx.ShippingTransactionId,
                OrderId = tx.OrderId,
                PreviousStatus = previousStatus,
                NewStatus = status,
                Source = WebhookSource,
                RawPayload = rawPayload,
                ProcessedAt = now
            }, cancellationToken);

            // A2. Cập nhật trạng thái giao dịch vận chuyển, Phí vận chuyển và Tiền COD
            tx.Status = status;
            tx.UpdatedAt = now;
            if (payload.EffectiveTotalFee > 0)
            {
                tx.ShippingFee = payload.EffectiveTotalFee.Value;
            }
            if (payload.EffectiveCODAmount >= 0)
            {
                tx.CodAmount = payload.EffectiveCODAmount.Value;
            }

            var statusLower = status.ToLowerInvariant();

            // A3. Xử lý tải ảnh minh chứng giao hàng (POD) lên Cloudinary
            if (imageStream != null)
            {
                await UploadDeliveryImageStreamToCloudinaryAsync(tx.Order, imageStream, fileName, cancellationToken);
            }
            else
            {
                var ghnImageUrl = payload.GetDeliveryImageUrl();
                if (!string.IsNullOrWhiteSpace(ghnImageUrl))
                {
                    if (Uri.IsWellFormedUriString(ghnImageUrl, UriKind.Absolute))
                    {
                        tx.Order.DeliveryImageUrl = ghnImageUrl;
                    }

                    await UploadDeliveryImageToCloudinaryAsync(tx.Order, ghnImageUrl, cancellationToken);
                }
            }

            // A4. Cập nhật các cột thông tin giao hàng trực tiếp trên thực thể Order
            if (statusLower == "delivery_fail")
            {
                tx.Order.FailedDeliveryAt = now;
                tx.Order.LastGHNFailCode = payload.ReasonCode;
                tx.Order.DeliveryFailCount = (byte)(tx.Order.DeliveryFailCount + 1);
                
                if (!string.IsNullOrEmpty(payload.Reason))
                {
                    tx.Order.CancelReason = payload.Reason;
                }

                _logger.LogInformation("GHN Webhook cập nhật giao thất bại: order={OrderCode}, số lần thất bại={Count}, lý do={ReasonCode}",
                    tx.Order.OrderCode, tx.Order.DeliveryFailCount, payload.ReasonCode);
            }
            else if (statusLower == "returned")
            {
                tx.Order.ReturnedAt = now;
                _logger.LogInformation("GHN Webhook cập nhật đơn đã hoàn về shop: order={OrderCode}", tx.Order.OrderCode);
            }
            else if (statusLower == "cancel")
            {
                tx.Order.LastGHNFailCode = payload.ReasonCode;
                if (!string.IsNullOrEmpty(payload.Reason))
                {
                    tx.Order.CancelReason = payload.Reason;
                }
                _logger.LogInformation("GHN Webhook cập nhật hủy vận đơn: order={OrderCode}, lý do={ReasonCode} - {Reason}",
                    tx.Order.OrderCode, payload.ReasonCode, payload.Reason);
            }
            else if (statusLower == "ready_to_pick" && GhnFailCodeMapper.IsPickFail(payload.ReasonCode))
            {
                // A5. Lấy hàng thất bại: GHN gửi Status=ready_to_pick kèm mã lỗi lấy hàng GHN-PFA... / GHN-PCB...
                // -> Reset đơn về Processing để nhân viên Merchandise có thể tạo lại vận đơn GHN mới
                var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(cancellationToken);
                if (statusMap.TryGetValue(OrderStatuses.Processing, out var processingId))
                {
                    var friendlyReason = GhnFailCodeMapper.GetFriendlyDescription(payload.ReasonCode);

                    if (tx.Order.StatusId == (byte)OrderStatus.Shipped)
                    {
                        tx.Order.StatusId          = processingId;
                        tx.Order.ShippingOrderCode = null;   // Xóa mã GHN cũ để chuẩn bị tạo đơn mới
                        tx.Order.ShippedAt         = null;
                        tx.Order.DeliveredAt       = null;
                        tx.Order.CompletedAt       = null;
                        tx.Order.LastGHNFailCode   = payload.ReasonCode;
                        tx.Order.UpdatedAt         = now;

                        await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
                        {
                            OrderId   = tx.OrderId,
                            StatusId  = processingId,
                            ChangedBy = null,
                            Note      = $"GHN pickup failed ({payload.ReasonCode}): {friendlyReason}. " +
                                        "Order reset to Processing for redelivery.",
                            CreatedAt = now
                        }, cancellationToken);

                        pendingNotifications.Add(new PendingShippingNotification(
                            NotificationEventTypes.MerchPickFailed,
                            new
                            {
                                orderId      = tx.OrderId,
                                orderCode    = tx.Order.OrderCode,
                                reasonCode   = payload.ReasonCode,
                                reason       = friendlyReason,
                                providerCode = payload.OrderCode
                            }));

                        _logger.LogInformation(
                            "GHN Pick Fail: Đơn hàng {OrderCode} đã được reset về Processing. Lý do: {ReasonCode} - {Reason}",
                            tx.Order.OrderCode, payload.ReasonCode, friendlyReason);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "GHN Pick Fail: Đơn hàng {OrderCode} không ở trạng thái Shipped (StatusId hiện tại={StatusId}), bỏ qua reset.",
                            tx.Order.OrderCode, tx.Order.StatusId);
                    }
                }
                // Vì trường hợp Pick Fail đã xử lý xong ở đây nên đánh dấu bỏ qua Mục B
                skipActionDispatch = true;
            }

            // B. Thực hiện ánh xạ action và cập nhật trạng thái quy trình đơn hàng
            var action = skipActionDispatch
                ? ShippingWebhookAction.Unknown
                : _statusMapper.ResolveWebhookAction(status);

            if (action == ShippingWebhookAction.UpdateOrderStatus)
            {
                var targetStatusId = (byte)GhnStatusMapper.ToInternalStatusId(status);
                if (targetStatusId > 0)
                {
                    if (targetStatusId == (byte)OrderStatus.Delivered)
                    {
                        // Sử dụng Lifecycle Service để chuyển hoàn tất giao hàng (giải phóng ca nhân viên, kích hoạt sự kiện)
                        var result = await _orderLifecycle.DeliverOrderAsync(tx.OrderId, cancellationToken);
                        if (!result.IsSuccess)
                        {
                            _logger.LogWarning("Đánh dấu đơn hàng {OrderId} là delivered thất bại qua lifecycle: {Error}",
                                tx.OrderId, result.ErrorMessage);
                        }
                    }
                    else
                    {
                        // Kiểm tra luồng chuyển trạng thái hợp lệ
                        bool isTerminal = tx.Order.StatusId == (byte)OrderStatus.Cancelled || 
                                          tx.Order.StatusId == (byte)OrderStatus.Refunded || 
                                          tx.Order.StatusId == (byte)OrderStatus.ReturnCompleted;

                        if (!isTerminal && OrderWebhookTransitionValidator.CanApplyWebhookStatus(tx.Order.StatusId, targetStatusId))
                        {
                            tx.Order.StatusId = targetStatusId;

                            // Đảm bảo mốc thời gian thỏa mãn điều kiện ràng buộc DB khi chuyển trạng thái
                            if (targetStatusId == (byte)OrderStatus.Shipped || targetStatusId == (byte)OrderStatus.Delivering)
                            {
                                if (tx.Order.ConfirmedAt == null)
                                {
                                    tx.Order.ConfirmedAt = tx.Order.OrderDate <= now ? tx.Order.OrderDate : now;
                                }
                                if (tx.Order.ShippedAt == null || tx.Order.ShippedAt < tx.Order.ConfirmedAt)
                                {
                                    tx.Order.ShippedAt = now < tx.Order.ConfirmedAt ? tx.Order.ConfirmedAt : now;
                                }
                            }

                            tx.Order.UpdatedAt = now;

                            var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(cancellationToken);
                            var targetStatusName = statusMap.FirstOrDefault(x => x.Value == targetStatusId).Key ?? status;

                            await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
                            {
                                OrderId = tx.OrderId,
                                StatusId = targetStatusId,
                                ChangedBy = null,
                                Note = $"Automatically updated from GHN webhook: {targetStatusName}",
                                CreatedAt = now
                            }, cancellationToken);

                            if (targetStatusId == (byte)OrderStatus.Delivered ||
                                targetStatusId == (byte)OrderStatus.Cancelled ||
                                targetStatusId == (byte)OrderStatus.Completed)
                            {
                                await _unitOfWork.OrderQueues.ResolveByOrderIdAsync(tx.OrderId, cancellationToken);
                            }
                        }
                    }
                }
            }
            else if (action != ShippingWebhookAction.Unknown)
            {
                // Xử lý quy trình trả hàng bằng service Return Flow tiêu chuẩn
                var flowResult = await _returnFlow.ProcessActionAsync(action, tx.Order, tx, status, now, cancellationToken);
                pendingNotifications.AddRange(flowResult.Notifications);
                releaseCapacity = flowResult.ReleaseShiftCapacity;
            }
            else
            {
                _logger.LogWarning("GHN Webhook action không xác định cho trạng thái '{Status}' của đơn hàng {OrderId}", status, tx.OrderId);
            }

            // C. Đồng bộ trạng thái thanh toán đối với đơn COD
            var newPaymentStatus = GhnStatusMapper.ComputePaymentStatus(payload, tx.Order.PaymentMethod, tx.Order.PaymentStatus);
            if (newPaymentStatus != tx.Order.PaymentStatus)
            {
                tx.Order.PaymentStatus = newPaymentStatus;
                tx.Order.UpdatedAt = now;

                await _unitOfWork.Orders.AddPaymentHistoryAsync(new PaymentHistory
                {
                    AccountId = tx.Order.AccountId,
                    OrderId = tx.OrderId,
                    PaymentStatus = newPaymentStatus,
                    PaymentMethod = tx.Order.PaymentMethod,
                    Amount = tx.Order.TotalAmount,
                    CreatedAt = now
                }, cancellationToken);

                _logger.LogInformation("Trạng thái thanh toán của đơn hàng {OrderCode} tự động cập nhật thành {PaymentStatus} qua GHN webhook",
                    tx.Order.OrderCode, newPaymentStatus);
            }

            // D. Kiểm tra mã lỗi giao hàng bất thường để cảnh báo đưa khách vào danh sách đen (Blacklist)
            if (statusLower == "delivery_fail" && GhnFailCodeMapper.ShouldBlacklist(payload.ReasonCode ?? ""))
            {
                pendingNotifications.Add(new PendingShippingNotification(
                    NotificationEventTypes.SystemShippingWebhookError,
                    new
                    {
                        orderId = tx.OrderId,
                        orderCode = tx.Order.OrderCode,
                        providerStatus = status,
                        reasonCode = payload.ReasonCode,
                        message = $"Phát hiện mã giao thất bại nghi vấn '{payload.ReasonCode}' từ GHN (cần xem xét đưa khách vào danh sách đen)."
                    }));
            }

            // E. Lưu và Commit Transaction
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Transaction GHN Webhook đã được commit thành công cho code={Code}, status={Status}",
                payload.OrderCode, status);

            // 6. Phát các sự kiện thông báo (Thực hiện ngoài Transaction để tăng hiệu năng)
            var notifEvent = ResolveNotificationEventType(statusLower);
            if (notifEvent != null && pendingNotifications.All(n => n.EventType != notifEvent))
            {
                pendingNotifications.Add(new PendingShippingNotification(notifEvent, new
                {
                    orderId = tx.OrderId,
                    orderCode = tx.Order.OrderCode,
                    providerStatus = status,
                    providerOrderCode = payload.OrderCode
                }));
            }

            foreach (var pending in pendingNotifications)
            {
                await _eventPublisher.PublishAsync(
                    "Order", tx.OrderId.ToString(), pending.EventType, pending.Payload, cancellationToken);
            }

            // 7. Giải phóng công suất ca làm việc của nhân viên nếu đơn hàng hủy/giao thất bại
            if (releaseCapacity)
            {
                try
                {
                    await _shiftAssignmentService.ReleaseCapacityAsync(orderIdForCapacity, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi giải phóng công suất ca làm việc cho đơn hàng {OrderId}", orderIdForCapacity);
                }
            }
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Lỗi khi xử lý callback GHN webhook cho đơn hàng {Code}", payload.OrderCode);
            throw;
        }
    }

    /// <summary>
    /// Ánh xạ trạng thái vận chuyển từ GHN thành loại sự kiện thông báo hệ thống.
    /// </summary>
    private static string? ResolveNotificationEventType(string statusLower)
    {
        return statusLower switch
        {
            "delivery_fail"     => NotificationEventTypes.OrderDeliveryFailed,
            "ready_to_pick"     => null,
            "waiting_to_return" => NotificationEventTypes.OrderReturning,
            "returned"          => NotificationEventTypes.MerchReturned,
            "return_fail"       => NotificationEventTypes.OrderReturnFail,
            "damage" or "lost"  => NotificationEventTypes.SystemShippingDamageLost,
            _                   => null
        };
    }

    private async Task ProcessDeliveryImageOnlyAsync(Order order, string ghnImageUrl, CancellationToken cancellationToken)
    {
        await UploadDeliveryImageToCloudinaryAsync(order, ghnImageUrl, cancellationToken);
    }

    /// <summary>
    /// Tải hình ảnh giao hàng (URL hoặc Base64) lên Cloudinary.
    /// </summary>
    private async Task UploadDeliveryImageToCloudinaryAsync(Order order, string imageInput, CancellationToken cancellationToken)
    {
        try
        {
            Result<string> uploadResult;

            if (imageInput.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) && imageInput.Contains(";base64,"))
            {
                var base64Data = imageInput.Substring(imageInput.IndexOf(";base64,", StringComparison.OrdinalIgnoreCase) + 8);
                var imageBytes = Convert.FromBase64String(base64Data);
                await using var stream = new MemoryStream(imageBytes);

                uploadResult = await _imageUploadService.UploadImageToFolderAsync(
                    stream,
                    $"pod_{order.OrderCode}_{DateTime.UtcNow.Ticks}.jpg",
                    "order_pods",
                    cancellationToken);
            }
            else if (Uri.IsWellFormedUriString(imageInput, UriKind.Absolute))
            {
                uploadResult = await _imageUploadService.UploadImageFromUrlAsync(
                    imageInput,
                    folder: "order_pods",
                    publicId: order.OrderCode,
                    cancellationToken);
            }
            else
            {
                _logger.LogWarning("Định dạng dữ liệu ảnh POD không hợp lệ cho đơn hàng {OrderCode}", order.OrderCode);
                return;
            }

            if (uploadResult.IsSuccess && !string.IsNullOrEmpty(uploadResult.Data))
            {
                order.DeliveryImageUrl = uploadResult.Data;
                order.UpdatedAt = _timeProvider.UtcNow;
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Đã tải thành công ảnh GHN Webhook POD lên Cloudinary: {Url} cho đơn hàng {OrderCode}",
                    uploadResult.Data, order.OrderCode);
            }
            else
            {
                _logger.LogWarning("Tải ảnh GHN POD lên Cloudinary thất bại cho đơn hàng {OrderCode}: {Error}",
                    order.OrderCode, uploadResult.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tải ảnh GHN POD lên Cloudinary cho đơn hàng {OrderCode}", order.OrderCode);
        }
    }

    /// <summary>
    /// Tải luồng ảnh nhị phân (Binary Stream) trực tiếp lên Cloudinary.
    /// </summary>
    private async Task UploadDeliveryImageStreamToCloudinaryAsync(Order order, Stream imageStream, string? fileName, CancellationToken cancellationToken)
    {
        try
        {
            var name = string.IsNullOrWhiteSpace(fileName) 
                ? $"pod_{order.OrderCode}_{DateTime.UtcNow.Ticks}.jpg" 
                : $"pod_{order.OrderCode}_{Path.GetFileName(fileName)}";

            var uploadResult = await _imageUploadService.UploadImageToFolderAsync(
                imageStream,
                name,
                "delivery-proofs",
                cancellationToken);

            if (uploadResult.IsSuccess && !string.IsNullOrEmpty(uploadResult.Data))
            {
                order.DeliveryImageUrl = uploadResult.Data;
                order.UpdatedAt = _timeProvider.UtcNow;
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Đã tải thành công luồng ảnh nhị phân POD lên Cloudinary: {Url} cho đơn hàng {OrderCode}",
                    uploadResult.Data, order.OrderCode);
            }
            else
            {
                _logger.LogWarning("Tải luồng ảnh nhị phân GHN POD lên Cloudinary thất bại cho đơn hàng {OrderCode}: {Error}",
                    order.OrderCode, uploadResult.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi ngoại lệ khi tải luồng ảnh nhị phân POD lên Cloudinary cho đơn hàng {OrderCode}", order.OrderCode);
        }
    }
}



