using System.Text.Json;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Constants;
using ToyStore.Application.Services;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Constants;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Enums;
using ToyStore.Application.DTOs.Orders;
using ToyStore.Infrastructure.Mappers;

namespace ToyStore.Infrastructure.Services;

/// <summary>
/// Dịch vụ xử lý Webhook nhận từ đơn vị vận chuyển (ví dụ: GHN):
/// - Phân loại xử lý giữa Đơn hàng thông thường (Order) và Đơn hàng hoàn/trả (Refund).
/// - Cập nhật giao dịch vận chuyển (ShippingProviderTransaction).
/// - Ghi nhận nhật ký lịch sử trạng thái (ShippingStatusHistory).
/// - Đồng bộ trạng thái Đơn hàng hoặc kích hoạt quy trình Trả hàng/Hoàn tiền (Return Flow).
/// - Đồng bộ trạng thái thanh toán đối với đơn COD.
/// - Phát sự kiện thông báo (Domain Events / SignalR Notification).
/// - Giải phóng công suất ca làm việc của nhân viên khi đơn bị hủy/thất bại.
/// </summary>
public class ShippingWebhookService : IShippingWebhookService
{
    private const string WebhookSource = "GHN_WEBHOOK";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ILogger<ShippingWebhookService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly IOrderLifecycleService _orderLifecycle;
    private readonly IShippingReturnFlowService _returnFlow;
    private readonly IShippingStatusMapper _statusMapper;
    private readonly IShiftAssignmentService _shiftAssignmentService;
    private readonly IImageUploadService _imageUploadService;
    private readonly IWalletRefundCreditor _walletRefundCreditor;

    public ShippingWebhookService(
        IUnitOfWork unitOfWork,
        IDomainEventPublisher eventPublisher,
        ILogger<ShippingWebhookService> logger,
        ITimeProvider timeProvider,
        IOrderLifecycleService orderLifecycle,
        IShippingReturnFlowService returnFlow,
        IShippingStatusMapper statusMapper,
        IShiftAssignmentService shiftAssignmentService,
        IImageUploadService imageUploadService,
        IWalletRefundCreditor walletRefundCreditor)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _logger = logger;
        _timeProvider = timeProvider;
        _orderLifecycle = orderLifecycle;
        _returnFlow = returnFlow;
        _statusMapper = statusMapper;
        _shiftAssignmentService = shiftAssignmentService;
        _imageUploadService = imageUploadService;
        _walletRefundCreditor = walletRefundCreditor;
    }

    /// <summary>
    /// Phương thức chính tiếp nhận và xử lý payload Webhook từ đơn vị vận chuyển.
    /// </summary>
    /// <param name="provider">Tên nhà vận chuyển (ví dụ: GHN)</param>
    /// <param name="rawPayload">Chuỗi JSON payload thô gửi từ nhà vận chuyển</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ</param>
    public async Task HandleAsync(
        string provider,
        string rawPayload,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Phân tích dữ liệu JSON payload thô
            using var doc = JsonDocument.Parse(rawPayload);
            var root = doc.RootElement;

            // Trích xuất mã vận đơn từ đối tác (OrderCode hoặc order_code)
            var providerOrderCode = TryGetString(root, "OrderCode")
                ?? TryGetString(root, "order_code");

            // Trích xuất mã đơn hàng của hệ thống mình gửi sang (ClientOrderCode hoặc client_order_code)
            var clientOrderCode = TryGetString(root, "ClientOrderCode")
                ?? TryGetString(root, "client_order_code");

            // Trích xuất trạng thái vận chuyển mới (Status hoặc status)
            var newStatus = TryGetString(root, "Status")
                ?? TryGetString(root, "status");

            // Kiểm tra tính hợp lệ của thông tin bắt buộc
            if (string.IsNullOrWhiteSpace(providerOrderCode) || string.IsNullOrWhiteSpace(newStatus))
            {
                _logger.LogWarning(
                    "Shipping webhook from {Provider}: missing order_code or status. Payload: {Payload}",
                    provider, Truncate(rawPayload));
                return;
            }

            providerOrderCode = providerOrderCode.Trim();
            newStatus = newStatus.Trim();
            clientOrderCode = clientOrderCode?.Trim();

            // 2. Kiểm tra xem mã vận chuyển này có thuộc về yêu cầu Trả hàng / Hoàn tiền (Refund) hay không
            var refund = await _unitOfWork.Refunds.GetByShippingOrReturnOrderCodeAsync(providerOrderCode, cancellationToken);
            if (refund is null && !string.IsNullOrWhiteSpace(clientOrderCode))
            {
                refund = await _unitOfWork.Refunds.GetByShippingOrReturnOrderCodeAsync(clientOrderCode, cancellationToken);
            }

            // Nếu đây là mã vận chuyển của đơn Refund -> Thực hiện xác thực chéo (Cross-Validation) tính hợp lệ giữa OrderCode và ClientOrderCode
            if (refund is not null)
            {
                bool isR2Client = !string.IsNullOrWhiteSpace(clientOrderCode) && clientOrderCode.StartsWith("R2-", StringComparison.OrdinalIgnoreCase);
                bool isR1Client = !string.IsNullOrWhiteSpace(clientOrderCode) && (clientOrderCode.StartsWith("R-", StringComparison.OrdinalIgnoreCase) || clientOrderCode.StartsWith("REF-", StringComparison.OrdinalIgnoreCase));

                // Nếu webhook gửi CẢ providerOrderCode VÀ clientOrderCode:
                if (!string.IsNullOrWhiteSpace(providerOrderCode) && !string.IsNullOrWhiteSpace(clientOrderCode))
                {
                    if (isR2Client)
                    {
                        // Chiều giao lại hàng cho khách: OrderCode từ GHN phải khớp với ReturnShippingOrderCode trong DB
                        if (!string.Equals(providerOrderCode, refund.ReturnShippingOrderCode, StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogWarning(
                                "GHN Webhook Mismatch cho Refund (chiều Shop->Khách): ClientOrderCode '{ClientCode}' (Refund #{RefundCode}) không khớp với OrderCode '{OrderCode}' (Mã ReturnShippingOrderCode trong DB: '{ExpectedCode}'). Bỏ qua cập nhật.",
                                clientOrderCode, refund.RefundCode, providerOrderCode, refund.ReturnShippingOrderCode);
                            return;
                        }
                    }
                    else if (isR1Client)
                    {
                        // Chiều thu hồi hàng từ khách: OrderCode từ GHN phải khớp với ShippingOrderCode trong DB
                        if (!string.Equals(providerOrderCode, refund.ShippingOrderCode, StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogWarning(
                                "GHN Webhook Mismatch cho Refund (chiều Khách->Shop): ClientOrderCode '{ClientCode}' (Refund #{RefundCode}) không khớp với OrderCode '{OrderCode}' (Mã ShippingOrderCode trong DB: '{ExpectedCode}'). Bỏ qua cập nhật.",
                                clientOrderCode, refund.RefundCode, providerOrderCode, refund.ShippingOrderCode);
                            return;
                        }
                    }
                    else
                    {
                        // Không có tiền tố R-/R2-, kiểm tra xem providerOrderCode có khớp 1 trong 2 mã vận đơn của refund không
                        bool matchesRefund = string.Equals(providerOrderCode, refund.ShippingOrderCode, StringComparison.OrdinalIgnoreCase)
                                          || string.Equals(providerOrderCode, refund.ReturnShippingOrderCode, StringComparison.OrdinalIgnoreCase);
                        if (!matchesRefund)
                        {
                            _logger.LogWarning(
                                "GHN Webhook Mismatch cho Refund: OrderCode '{OrderCode}' không khớp với bất kỳ mã vận đơn nào của Refund #{RefundCode} (Shipping='{ShipCode}', Return='{ReturnCode}'). Bỏ qua cập nhật.",
                                providerOrderCode, refund.RefundCode, refund.ShippingOrderCode, refund.ReturnShippingOrderCode);
                            return;
                        }
                    }
                }

                await HandleRefundWebhookAsync(refund, providerOrderCode, clientOrderCode, newStatus, rawPayload, cancellationToken);
                return;
            }

            // 3. Tìm giao dịch vận chuyển của đơn hàng thường trong cơ sở dữ liệu
            var tx = await _unitOfWork.Orders.GetShippingTransactionByProviderCodeAsync(
                providerOrderCode, cancellationToken);

            if (tx is null && !string.IsNullOrWhiteSpace(clientOrderCode))
            {
                tx = await _unitOfWork.Orders.GetShippingTransactionByProviderCodeAsync(clientOrderCode, cancellationToken);
            }

            if (tx is null)
            {
                _logger.LogWarning(
                    "Shipping webhook from {Provider}: ProviderOrderCode '{Code}' / ClientOrderCode '{ClientCode}' not found in shipping transactions or refund requests",
                    provider, providerOrderCode, clientOrderCode);
                return;
            }

            // Xác thực chéo cho đơn hàng thường:
            // Nếu có cả providerOrderCode và clientOrderCode, kiểm tra xem providerOrderCode có khớp với mã vận chuyển đã lưu của đơn này không
            if (!string.IsNullOrWhiteSpace(providerOrderCode) && !string.IsNullOrWhiteSpace(clientOrderCode) && tx.Order != null)
            {
                bool matchesOrder = string.Equals(providerOrderCode, tx.ProviderOrderCode, StringComparison.OrdinalIgnoreCase)
                                 || string.Equals(providerOrderCode, tx.Order.ShippingOrderCode, StringComparison.OrdinalIgnoreCase);
                bool matchesClient = string.Equals(clientOrderCode, tx.Order.OrderCode, StringComparison.OrdinalIgnoreCase)
                                  || string.Equals(clientOrderCode, tx.ProviderOrderCode, StringComparison.OrdinalIgnoreCase);

                if (!matchesOrder || !matchesClient)
                {
                    _logger.LogWarning(
                        "GHN Webhook Mismatch cho Order: ClientOrderCode '{ClientCode}' (Order #{OrderCode}) không khớp với OrderCode '{OrderCode}' (Mã GHN trong DB: '{ExpectedCode}'). Bỏ qua cập nhật.",
                        clientOrderCode, tx.Order.OrderCode, providerOrderCode, tx.ProviderOrderCode);
                    return;
                }
            }

            // Kiểm tra bổ sung an toàn: Nếu trước đó chưa tìm thấy refund theo mã vận đơn, kiểm tra xem giao dịch tx hoặc clientOrderCode có thuộc về yêu cầu Refund hay không
            if (refund is null && tx != null)
            {
                if (!string.IsNullOrWhiteSpace(tx.ProviderOrderCode))
                {
                    refund = await _unitOfWork.Refunds.GetByShippingOrReturnOrderCodeAsync(tx.ProviderOrderCode, cancellationToken);
                }

                if (refund is null && tx.OrderId > 0)
                {
                    var existingRefund = await _unitOfWork.Refunds.GetByOrderIdAsync(tx.OrderId, cancellationToken);
                    if (existingRefund is not null && string.Equals(existingRefund.RefundSource, RefundSources.Customer, StringComparison.OrdinalIgnoreCase))
                    {
                        var isOrderCompleted = tx.Order != null && tx.Order.Status != null &&
                            (string.Equals(tx.Order.Status.StatusName, OrderStatuses.Completed, StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(tx.Order.Status.StatusName, OrderStatuses.Delivered, StringComparison.OrdinalIgnoreCase));

                        var isReturnGhnStatus = newStatus.Equals("returned", StringComparison.OrdinalIgnoreCase) ||
                                                newStatus.Equals("returning", StringComparison.OrdinalIgnoreCase) ||
                                                newStatus.Equals("return", StringComparison.OrdinalIgnoreCase) ||
                                                newStatus.Equals("return_transporting", StringComparison.OrdinalIgnoreCase) ||
                                                newStatus.Equals("return_sorting", StringComparison.OrdinalIgnoreCase) ||
                                                newStatus.Equals("waiting_to_return", StringComparison.OrdinalIgnoreCase) ||
                                                newStatus.Equals("delivery_fail", StringComparison.OrdinalIgnoreCase) ||
                                                newStatus.Equals("return_fail", StringComparison.OrdinalIgnoreCase);

                        bool isRefundShipment =
                            isOrderCompleted ||
                            isReturnGhnStatus ||
                            string.Equals(providerOrderCode, existingRefund.ShippingOrderCode, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(providerOrderCode, existingRefund.ReturnShippingOrderCode, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(tx.ProviderOrderCode, existingRefund.ShippingOrderCode, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(tx.ProviderOrderCode, existingRefund.ReturnShippingOrderCode, StringComparison.OrdinalIgnoreCase) ||
                            (!string.IsNullOrWhiteSpace(clientOrderCode) &&
                                (clientOrderCode.StartsWith("R-", StringComparison.OrdinalIgnoreCase) ||
                                 clientOrderCode.StartsWith("R2-", StringComparison.OrdinalIgnoreCase) ||
                                 clientOrderCode.StartsWith("REF-", StringComparison.OrdinalIgnoreCase)));

                        if (isRefundShipment)
                        {
                            refund = existingRefund;
                        }
                    }
                }
            }

            if (refund is not null)
            {
                await HandleRefundWebhookAsync(refund, providerOrderCode, clientOrderCode, newStatus, rawPayload, cancellationToken);
                return;
            }

            // Guard cho đơn hàng đã Completed/Delivered: Không cho phép trạng thái trả hàng/giao thất bại đè lên đơn chính khi không có Refund record
            var orderIsFinished = tx.Order != null && tx.Order.Status != null &&
                (string.Equals(tx.Order.Status.StatusName, OrderStatuses.Completed, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(tx.Order.Status.StatusName, OrderStatuses.Delivered, StringComparison.OrdinalIgnoreCase));

            var ghnIsReturn = newStatus.Equals("returned", StringComparison.OrdinalIgnoreCase) ||
                              newStatus.Equals("returning", StringComparison.OrdinalIgnoreCase) ||
                              newStatus.Equals("return", StringComparison.OrdinalIgnoreCase) ||
                              newStatus.Equals("return_transporting", StringComparison.OrdinalIgnoreCase) ||
                              newStatus.Equals("return_sorting", StringComparison.OrdinalIgnoreCase) ||
                              newStatus.Equals("waiting_to_return", StringComparison.OrdinalIgnoreCase) ||
                              newStatus.Equals("delivery_fail", StringComparison.OrdinalIgnoreCase) ||
                              newStatus.Equals("return_fail", StringComparison.OrdinalIgnoreCase);

            if (orderIsFinished && ghnIsReturn)
            {
                _logger.LogWarning(
                    "Bỏ qua Webhook hoàn trả '{Status}' cho đơn hàng đã hoàn thành/đã giao: OrderId={OrderId}, ProviderCode={Code}",
                    newStatus, tx.OrderId, providerOrderCode);
                return;
            }

            // Safety Guard: Nếu webhook mang tiền tố Refund (R-, R2-, REF-) mà không tìm thấy record OrderRefund tương ứng trong DB
            // -> Tuyệt đối ngắt luồng, KHÔNG ĐƯỢC rơi xuống luồng đơn hàng gốc làm ghi đè Order.StatusId hoặc OrderStatusHistories!
            if (!string.IsNullOrWhiteSpace(clientOrderCode) &&
                (clientOrderCode.StartsWith("R-", StringComparison.OrdinalIgnoreCase) ||
                 clientOrderCode.StartsWith("R2-", StringComparison.OrdinalIgnoreCase) ||
                 clientOrderCode.StartsWith("REF-", StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning(
                    "Shipping webhook Refund '{ClientOrderCode}' (ProviderCode: {ProviderCode}) không tìm thấy trong CSDL OrderRefunds. Ngắt luồng để bảo vệ đơn hàng gốc.",
                    clientOrderCode, providerOrderCode);
                return;
            }

            // 4. Kiểm tra Idempotency (Tránh xử lý lặp lại webhook bị gửi trùng)
            if (await _unitOfWork.Orders.ExistsShippingStatusHistoryAsync(
                    tx.ShippingTransactionId, newStatus, rawPayload, cancellationToken))
            {
                _logger.LogInformation(
                    "Bỏ qua Shipping webhook trùng lặp: code={Code}, status={Status}",
                    providerOrderCode, newStatus);
                return;
            }

            var previousStatus = tx.Status ?? string.Empty;
            var now = _timeProvider.UtcNow;
            var pendingNotifications = new List<PendingShippingNotification>();
            var releaseCapacity = false;
            var orderIdForCapacity = tx.OrderId;

            // 5. Bắt đầu Database Transaction để đảm bảo tính toàn vẹn dữ liệu
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // A. Ghi lại lịch sử cập nhật trạng thái vận chuyển
                await _unitOfWork.Orders.AddShippingStatusHistoryAsync(new ShippingStatusHistory
                {
                    ShippingTxId = tx.ShippingTransactionId,
                    OrderId = tx.OrderId,
                    PreviousStatus = previousStatus,
                    NewStatus = newStatus,
                    Source = WebhookSource,
                    RawPayload = rawPayload,
                    ProcessedAt = now
                }, cancellationToken);

                // B. Cập nhật trạng thái giao dịch vận chuyển
                tx.Status = newStatus;
                tx.UpdatedAt = now;

                if (tx.Order != null)
                {
                    var rCode = TryGetString(root, "ReasonCode") ?? TryGetString(root, "reason_code");
                    var rDesc = TryGetString(root, "Reason") ?? TryGetString(root, "reason");
                    if (!string.IsNullOrWhiteSpace(rCode))
                    {
                        tx.Order.LastGHNFailCode = rCode;
                    }
                    if (!string.IsNullOrWhiteSpace(rDesc))
                    {
                        tx.Order.CancelReason = rDesc;
                    }
                }

                // C. Phân tích hành động tương ứng với trạng thái vận chuyển mới
                var action = _statusMapper.ResolveWebhookAction(newStatus);

                if (action == ShippingWebhookAction.UpdateOrderStatus)
                {
                    // Cập nhật trạng thái nội bộ của Đơn hàng (ví dụ: Shipped, Delivering, Delivered)
                    var targetStatus = _statusMapper.MapToInternalStatus(newStatus);
                    if (targetStatus.HasValue)
                        await UpdateOrderStatusAsync(tx.Order, targetStatus.Value, now, cancellationToken);
                }
                else if (action != ShippingWebhookAction.Unknown)
                {
                    // Xử lý các quy trình trả hàng / sự cố vận chuyển qua ReturnFlow
                    var flowResult = await _returnFlow.ProcessActionAsync(
                        action, tx.Order, tx, newStatus, now, cancellationToken);
                    pendingNotifications.AddRange(flowResult.Notifications);
                    releaseCapacity = flowResult.ReleaseShiftCapacity;
                }
                else
                {
                    _logger.LogWarning(
                        "Shipping webhook trạng thái không xác định '{Status}' cho đơn hàng {OrderId}",
                        newStatus, tx.OrderId);
                }

                // D. Tự động chuyển trạng thái thanh toán thành PAID đối với đơn SHIP_COD khi giao hàng thành công
                if (tx.Order.PaymentMethod == "SHIP_COD" && tx.Order.PaymentStatus != "PAID")
                {
                    if (newStatus.Equals(ShippingStatuses.Delivered, StringComparison.OrdinalIgnoreCase))
                    {
                        tx.Order.PaymentStatus = "PAID";
                        tx.Order.PaidAt = now;
                        _logger.LogInformation(
                            "Trạng thái thanh toán của đơn hàng {OrderCode} đã được cập nhật thành PAID qua webhook: {Status}",
                            tx.Order.OrderCode, newStatus);
                    }
                }

                // E. Lưu thay đổi và Commit Transaction
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation(
                    "Đã xử lý Shipping webhook: provider={Provider}, code={Code}, status={Status}, action={Action}",
                    provider, providerOrderCode, newStatus, action);

                // F. Xác định sự kiện thông báo cần phát đi (Notification Event)
                var notifEvent = _statusMapper.ResolveNotificationEventType(newStatus);
                if (notifEvent is not null
                    && pendingNotifications.All(n => n.EventType != notifEvent))
                {
                    pendingNotifications.Add(new PendingShippingNotification(notifEvent, new
                    {
                        orderId = tx.OrderId,
                        orderCode = tx.Order?.OrderCode ?? "",
                        providerStatus = newStatus,
                        providerOrderCode
                    }));
                }

                // G. Phát các sự kiện thông báo (thực hiện sau khi commit DB thành công)
                foreach (var pending in pendingNotifications)
                {
                    await _eventPublisher.PublishAsync(
                        "Order", tx.OrderId.ToString(), pending.EventType, pending.Payload,
                        cancellationToken);
                }

                // H. Giải phóng tải/công suất ca làm việc của nhân viên nếu đơn hủy/giao thất bại
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
            catch
            {
                // Rollback transaction nếu xảy ra lỗi trong quá trình xử lý DB
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex,
                "Shipping webhook từ {Provider}: Chuỗi JSON payload không hợp lệ", provider);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Shipping webhook từ {Provider}: Xảy ra lỗi bất ngờ trong quá trình xử lý", provider);
        }
    }

    /// <summary>
    /// Hàm phụ trợ cập nhật trạng thái đơn hàng nội bộ dựa trên webhook vận chuyển.
    /// </summary>
    private async Task UpdateOrderStatusAsync(
        Order order,
        OrderStatus targetStatus,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // Nếu là trạng thái Giao hàng thành công (Delivered) -> Gọi service Lifecycle để hoàn tất đơn hàng
        if (targetStatus == OrderStatus.Delivered)
        {
            var result = await _orderLifecycle.DeliverOrderAsync(order.OrderId, cancellationToken);
            if (!result.IsSuccess)
            {
                _logger.LogWarning(
                    "Lỗi khi đánh dấu đơn hàng {OrderId} là Delivered qua lifecycle service: {Error}",
                    order.OrderId, result.ErrorMessage);
            }
            return;
        }

        var targetStatusName = targetStatus.ToString();
        var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(cancellationToken);

        if (!statusMap.TryGetValue(targetStatusName, out var targetStatusId))
        {
            _logger.LogWarning("Webhook: Không tìm thấy trạng thái '{Status}' trong cơ sở dữ liệu", targetStatusName);
            return;
        }

        // Kiểm tra hợp lệ luồng chuyển trạng thái đơn hàng từ Webhook
        if (!OrderWebhookTransitionValidator.CanApplyWebhookStatus(order.StatusId, targetStatusId))
            return;

        order.StatusId = targetStatusId;
        order.UpdatedAt = now;

        // Lưu lịch sử thay đổi trạng thái đơn hàng
        await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
        {
            OrderId = order.OrderId,
            StatusId = targetStatusId,
            ChangedBy = null,
            Note = $"Automatically updated from shipping webhook: {targetStatusName}",
            CreatedAt = now
        }, cancellationToken);
    }

    /// <summary>
    /// Thử lấy giá trị chuỗi của một thuộc tính từ JsonElement.
    /// </summary>
    private static string? TryGetString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop)
            && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString();
        }
        return null;
    }

    /// <summary>
    /// Cắt ngắn chuỗi log nếu quá dài để tránh phình dung lượng log file.
    /// </summary>
    private static string Truncate(string s, int max = 300)
        => s.Length <= max ? s : s[..max];

    /// <summary>
    /// Xử lý Webhook riêng biệt dành cho các đơn hàng đang trong quy trình Hoàn tiền / Trả hàng (Refund).
    /// </summary>
    /// <param name="refund">Thực thể đơn hoàn tiền (OrderRefund)</param>
    /// <param name="providerOrderCode">Mã vận đơn gửi từ đối tác (ShippingOrderCode hoặc ReturnShippingOrderCode)</param>
    /// <param name="newStatus">Trạng thái vận chuyển mới từ GHN</param>
    /// <param name="rawPayload">Payload thô gửi từ GHN</param>
    /// <param name="cancellationToken">Token hủy tác vụ</param>
    private async Task HandleRefundWebhookAsync(
        OrderRefund refund,
        string providerOrderCode,
        string? clientOrderCode,
        string newStatus,
        string rawPayload,
        CancellationToken cancellationToken)
    {
        try
        {
            // Kiểm tra đây là chiều giao lại hàng cho khách (ReturnToCustomer) hay chiều thu hồi hàng trả về Shop
            bool isReturnToCustomer = (!string.IsNullOrWhiteSpace(clientOrderCode) && clientOrderCode.StartsWith("R2-", StringComparison.OrdinalIgnoreCase))
                || string.Equals(providerOrderCode, refund.ReturnShippingOrderCode, StringComparison.OrdinalIgnoreCase);

            _logger.LogInformation(
                "Đang định tuyến Refund webhook: RefundId={RefundId}, ProviderCode={Code}, ClientOrderCode={ClientCode}, ShippingOrderCode={ShipCode}, ReturnShippingOrderCode={ReturnCode}, isReturnToCustomer={IsReturn}",
                refund.RefundId, providerOrderCode, clientOrderCode, refund.ShippingOrderCode, refund.ReturnShippingOrderCode, isReturnToCustomer);

            var now = _timeProvider.UtcNow;

            // Phân tích mã lý do (ReasonCode) và mô tả lý do (Reason) từ payload GHN để ghi log chi tiết
            var ghnReasonCode = TryGetString(JsonDocument.Parse(rawPayload).RootElement, "ReasonCode")
                ?? TryGetString(JsonDocument.Parse(rawPayload).RootElement, "reason_code");
            var ghnReason = TryGetString(JsonDocument.Parse(rawPayload).RootElement, "Reason")
                ?? TryGetString(JsonDocument.Parse(rawPayload).RootElement, "reason");

            // Tải ảnh bằng chứng giao hàng POD (nếu có) lên Cloudinary
            string? uploadedImageUrl = null;
            var ghnImageUrl = TryGetGhnImageUrl(rawPayload);
            if (!string.IsNullOrWhiteSpace(ghnImageUrl) && Uri.IsWellFormedUriString(ghnImageUrl, UriKind.Absolute))
            {
                var folderName = isReturnToCustomer ? "refund-to-customer-proofs" : "refund-to-shop-proofs";
                var publicId = isReturnToCustomer 
                    ? $"refund_to_customer_{refund.RefundCode ?? refund.RefundId.ToString()}_{now.Ticks}"
                    : $"refund_to_shop_{refund.RefundCode ?? refund.RefundId.ToString()}_{now.Ticks}";

                var uploadResult = await _imageUploadService.UploadImageFromUrlAsync(
                    ghnImageUrl,
                    folderName,
                    publicId,
                    cancellationToken);

                if (uploadResult.IsSuccess && !string.IsNullOrEmpty(uploadResult.Data))
                {
                    uploadedImageUrl = uploadResult.Data;
                    _logger.LogInformation("Đã tải ảnh GHN Webhook POD lên Cloudinary thành công: {Url} cho refund ID {RefundId}",
                        uploadedImageUrl, refund.RefundId);
                }
                else
                {
                    _logger.LogWarning("Tải ảnh GHN POD lên Cloudinary thất bại cho refund ID {RefundId}: {Error}",
                        refund.RefundId, uploadResult.ErrorMessage);
                }
            }

            // Ánh xạ trạng thái từ vận chuyển GHN sang trạng thái Refund nội bộ hệ thống
            byte? targetRefundStatusId;
            if (isReturnToCustomer)
            {
                // Luồng giao lại hàng từ Shop tới Khách hàng
                targetRefundStatusId = newStatus.ToLowerInvariant() switch
                {
                    ShippingStatuses.ReadyToPick or ShippingStatuses.Storing => (byte)RefundStatusEnum.RefundReturnShipmentCreated,
                    
                    ShippingStatuses.Picking or ShippingStatuses.Picked or 
                    ShippingStatuses.Transporting or ShippingStatuses.Sorting or 
                    ShippingStatuses.Delivering or ShippingStatuses.MoneyCollectDelivering or
                    ShippingStatuses.ReturnTransporting or ShippingStatuses.ReturnSorting or 
                    ShippingStatuses.Returning => (byte)RefundStatusEnum.RefundReturningToCustomer,
                    
                    ShippingStatuses.Delivered => (byte)RefundStatusEnum.RefundReturnedToCustomer,
                    
                    ShippingStatuses.Cancel => (byte)RefundStatusEnum.RefundCancelled,

                    ShippingStatuses.DeliveryFail or ShippingStatuses.ReturnFail or ShippingStatuses.Returned or 
                    ShippingStatuses.Exception => (byte)RefundStatusEnum.RefundReturnToCustomerFailed,

                    ShippingStatuses.Lost or ShippingStatuses.Damage => (byte)RefundStatusEnum.RefundDamage,
                    
                    _ => null
                };
            }
            else
            {
                // Luồng lấy hàng trả từ Khách hàng về Shop
                targetRefundStatusId = newStatus.ToLowerInvariant() switch
                {
                    ShippingStatuses.ReadyToPick or ShippingStatuses.Storing => (byte)RefundStatusEnum.RefundPickupCreated,
                    
                    ShippingStatuses.Picking or ShippingStatuses.Picked or 
                    ShippingStatuses.Transporting or ShippingStatuses.Sorting or 
                    ShippingStatuses.Delivering or ShippingStatuses.MoneyCollectDelivering or
                    ShippingStatuses.ReturnTransporting or ShippingStatuses.ReturnSorting or 
                    ShippingStatuses.Returning or
                    ShippingStatuses.Delivered or ShippingStatuses.Returned => (byte)RefundStatusEnum.RefundShipping,
                    
                    ShippingStatuses.Cancel or ShippingStatuses.DeliveryFail or ShippingStatuses.ReturnFail or 
                    ShippingStatuses.Exception => (byte)RefundStatusEnum.RefundCancelled,

                    ShippingStatuses.Lost or ShippingStatuses.Damage => (byte)RefundStatusEnum.RefundDamage,
                    
                    _ => null
                };
            }

            // 1. Xác định mã vận đơn của Refund (ưu tiên mã refund, không dùng mã gốc của đơn hàng chính)
            var payloadRoot = JsonDocument.Parse(rawPayload).RootElement;
            clientOrderCode ??= TryGetString(payloadRoot, "ClientOrderCode") ?? TryGetString(payloadRoot, "client_order_code");

            string refundTxCode = providerOrderCode;
            if (!string.IsNullOrWhiteSpace(clientOrderCode) &&
                (clientOrderCode.StartsWith("R-", StringComparison.OrdinalIgnoreCase) ||
                 clientOrderCode.StartsWith("R2-", StringComparison.OrdinalIgnoreCase) ||
                 clientOrderCode.StartsWith("REF-", StringComparison.OrdinalIgnoreCase)))
            {
                refundTxCode = clientOrderCode.Trim();
            }
            else if (!string.IsNullOrWhiteSpace(refund.ShippingOrderCode) &&
                     string.Equals(providerOrderCode, refund.ShippingOrderCode, StringComparison.OrdinalIgnoreCase))
            {
                refundTxCode = refund.ShippingOrderCode.Trim();
            }
            else if (!string.IsNullOrWhiteSpace(refund.ReturnShippingOrderCode) &&
                     string.Equals(providerOrderCode, refund.ReturnShippingOrderCode, StringComparison.OrdinalIgnoreCase))
            {
                refundTxCode = refund.ReturnShippingOrderCode.Trim();
            }
            else if (!string.IsNullOrWhiteSpace(refund.RefundCode))
            {
                refundTxCode = refund.RefundCode.Trim();
            }

            var tx = await _unitOfWork.Orders.GetShippingTransactionByProviderCodeAsync(refundTxCode, cancellationToken);

            // Nếu tx tìm được lại chính là giao dịch vận chuyển gốc của đơn hàng chính (không chứa tiền tố R-, R2-, REF- và không khớp mã refund)
            // -> Đặt tx = null để tạo một ShippingProviderTransaction hoàn toàn MỚI cho Refund, bảo vệ 100% giao dịch gốc của đơn hàng!
            if (tx is not null &&
                !string.Equals(tx.ProviderOrderCode, refund.ShippingOrderCode, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(tx.ProviderOrderCode, refund.ReturnShippingOrderCode, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(tx.ProviderOrderCode, refund.RefundCode, StringComparison.OrdinalIgnoreCase) &&
                !tx.ProviderOrderCode.StartsWith("R-", StringComparison.OrdinalIgnoreCase) &&
                !tx.ProviderOrderCode.StartsWith("R2-", StringComparison.OrdinalIgnoreCase) &&
                !tx.ProviderOrderCode.StartsWith("REF-", StringComparison.OrdinalIgnoreCase))
            {
                tx = null;
            }

            // Bug Fix: Nếu tx tìm được là transaction của đơn hàng GỐC (RefundId == null),
            // tức là GHN đang dùng cùng mã vận đơn cho cả đơn thường lẫn vận đơn pickup refund,
            // phải tạo tx MỚI cho refund thay vì modify tx gốc.
            // Nếu để nhánh else gán RefundId và ghi đè Status lên tx gốc, hàm
            // GetOriginalOrderShippingTransaction() sẽ lọc bỏ tx gốc (vì RefundId != null)
            // và trả về status "returning"/"returned" của refund cho đơn hàng checkout → hiển thị sai!
            if (tx is not null && tx.RefundId == null)
            {
                _logger.LogWarning(
                    "Refund webhook dùng cùng mã vận đơn với đơn hàng gốc (RefundId=null). " +
                    "Tạo tx mới để bảo vệ transaction gốc. RefundId={RefundId}, ProviderCode={Code}",
                    refund.RefundId, providerOrderCode);
                tx = null;
            }

            var previousStatus = tx?.Status ?? string.Empty;

            // Kiểm tra trùng lặp webhook cho refund shipping
            if (tx is not null && await _unitOfWork.Orders.ExistsShippingStatusHistoryAsync(
                    tx.ShippingTransactionId, newStatus, rawPayload, cancellationToken))
            {
                _logger.LogInformation(
                    "Bỏ qua Refund shipping webhook trùng lặp: code={Code}, status={Status}",
                    providerOrderCode, newStatus);
                return;
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // 2. Đảm bảo ShippingProviderTransaction và ShippingStatusHistory tồn tại riêng biệt cho Refund
                if (tx is null)
                {
                    tx = new ShippingProviderTransaction
                    {
                        OrderId = refund.OrderId,
                        RefundId = refund.RefundId,
                        Provider = "GHN",
                        ProviderOrderCode = refundTxCode,
                        Status = newStatus,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    await _unitOfWork.Orders.AddShippingTransactionAsync(tx, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    // tx này đã có RefundId != null (là tx của refund, không phải tx gốc)
                    tx.RefundId = refund.RefundId;
                    tx.Status = newStatus;
                    tx.UpdatedAt = now;
                }

                // Ghi nhật ký lịch sử vận chuyển cho refund
                await _unitOfWork.Orders.AddShippingStatusHistoryAsync(new ShippingStatusHistory
                {
                    ShippingTxId = tx.ShippingTransactionId,
                    OrderId = tx.OrderId,
                    PreviousStatus = previousStatus,
                    NewStatus = newStatus,
                    Source = WebhookSource,
                    RawPayload = rawPayload,
                    ProcessedAt = now
                }, cancellationToken);

                // 2. Xử lý chuyển đổi trạng thái Yêu cầu Hoàn tiền (Refund Status)
                if (targetRefundStatusId is not null && refund.StatusId != targetRefundStatusId.Value)
                {
                    // Kiểm tra quy tắc CanTransition đầy đủ, bao gồm trường hợp ngoại lệ
                    // GHN báo hư/mất hàng (RefundDamage) sau khi vận đơn bị hủy (RefundCancelled).
                    var isSystemReturn = string.Equals(refund.RefundSource, RefundSources.System, StringComparison.OrdinalIgnoreCase);
                    var canTransition = RefundStatusTransitionValidator.CanTransition(
                        refund.StatusId,
                        targetRefundStatusId.Value,
                        refund.RefundType ?? string.Empty,
                        isSystemReturn,
                        isAdmin: false);

                    var isLostDamageOverride =
                        refund.StatusId == (byte)RefundStatusEnum.RefundCancelled &&
                        targetRefundStatusId.Value == (byte)RefundStatusEnum.RefundDamage;

                    if (canTransition || isLostDamageOverride)
                    {
                        var previousRefundStatusId = refund.StatusId;
                        refund.StatusId = targetRefundStatusId.Value;
                        refund.UpdatedAt = now;

                        // Trường hợp vận chuyển bị HỦY (RefundCancelled)
                        if (targetRefundStatusId.Value == (byte)RefundStatusEnum.RefundCancelled)
                        {
                            refund.CancelledAt = now;
                            await _unitOfWork.OrderAssignments.ReleaseCapacityAsync(refund.OrderId, cancellationToken);

                            if (isReturnToCustomer)
                            {
                                // Hoàn lại phí ReturnToCustomerFee mà khách đã trả trước đó khi shop không bàn giao hàng cho shipper
                                if (refund.ReturnToCustomerFeePaid && refund.ReturnToCustomerFee > 0)
                                {
                                    var order = await _unitOfWork.Orders.GetByIdForUpdateAsync(refund.OrderId, cancellationToken);
                                    if (order != null)
                                    {
                                        var returnFeeRefundKey = $"REFUND_RETURN_FEE_{refund.RefundCode ?? refund.RefundId.ToString()}";
                                        var credited = await _walletRefundCreditor.CreditRefundAsync(
                                            order.AccountId,
                                            refund.ReturnToCustomerFee,
                                            order.OrderCode,
                                            order.OrderId,
                                            cancellationToken,
                                            returnFeeRefundKey);

                                        if (credited)
                                        {
                                            _logger.LogInformation(
                                                "Phí giao lại cho khách {Amount} đã được hoàn vào ví cho RefundId={RefundId} (GHN cancel - shop không giao hàng cho shipper)",
                                                refund.ReturnToCustomerFee, refund.RefundId);
                                        }
                                    }
                                }
                            }
                        }
                        // Trường hợp hàng hóa bị HƯ HỎNG / THẤT LẠC (RefundDamage)
                        else if (targetRefundStatusId.Value == (byte)RefundStatusEnum.RefundDamage)
                        {
                            refund.CancelledAt = null;
                            refund.AdminNote = "Goods damaged/lost in transit (GHN updated Damage/Lost). No quality inspection required.";

                            if (isReturnToCustomer)
                            {
                                // Hoàn lại phí ReturnToCustomerFee mà khách đã trả trước đó vì ĐVVC GHN làm hư hỏng / bể vỡ / thất lạc hàng trên đường giao lại cho khách
                                if (refund.ReturnToCustomerFeePaid && refund.ReturnToCustomerFee > 0)
                                {
                                    var order = await _unitOfWork.Orders.GetByIdForUpdateAsync(refund.OrderId, cancellationToken);
                                    if (order != null)
                                    {
                                        var returnFeeRefundKey = $"REFUND_RETURN_FEE_{refund.RefundCode ?? refund.RefundId.ToString()}";
                                        var credited = await _walletRefundCreditor.CreditRefundAsync(
                                            order.AccountId,
                                            refund.ReturnToCustomerFee,
                                            order.OrderCode,
                                            order.OrderId,
                                            cancellationToken,
                                            returnFeeRefundKey);

                                        if (credited)
                                        {
                                            _logger.LogInformation(
                                                "Phí giao lại cho khách {Amount} đã được hoàn vào ví cho RefundId={RefundId} (GHN damage/lost trên đường giao lại hàng cho khách)",
                                                refund.ReturnToCustomerFee, refund.RefundId);
                                        }
                                    }
                                }
                            }

                            if (previousRefundStatusId == (byte)RefundStatusEnum.RefundCancelled)
                            {
                                // Phục hồi lại phân công ca làm việc của nhân viên và tăng lại khối lượng công việc
                                var orderAssignments = await _unitOfWork.OrderAssignments.GetAssignmentsByOrderIdAsync(refund.OrderId, cancellationToken);
                                foreach (var oa in orderAssignments)
                                {
                                    oa.IsActive = true;

                                    var capacity = await _unitOfWork.StaffShiftCapacities.GetByScheduleIdForUpdateAsync(oa.ScheduleId, cancellationToken);
                                    if (capacity is not null)
                                    {
                                        capacity.CurrentLoad++;
                                        capacity.UpdatedAt = now;
                                    }
                                }
                            }
                        }

                        // Xây dựng ghi chú lịch sử chi tiết bao gồm nguyên nhân từ GHN (giữ nguyên reason từ GHN)
                        var friendlyReason = GhnFailCodeMapper.GetFriendlyDescription(ghnReasonCode, ghnReason);
                        var detailedNote = !string.IsNullOrWhiteSpace(friendlyReason) && friendlyReason != ghnReason
                            ? $"GHN {newStatus} ({ghnReasonCode ?? "N/A"}): {friendlyReason}"
                            : !string.IsNullOrWhiteSpace(ghnReason)
                                ? $"GHN {newStatus}: {ghnReason}"
                                : $"Automatically updated from GHN shipping webhook: {newStatus}";

                        // Bổ sung bối cảnh đối với trường hợp hủy hoặc giao thất bại
                        if (targetRefundStatusId.Value == (byte)RefundStatusEnum.RefundCancelled)
                        {
                            if (isReturnToCustomer)
                            {
                                detailedNote += ". Return-to-customer delivery cancelled: shop did not hand over items to courier.";
                                if (refund.ReturnToCustomerFeePaid && refund.ReturnToCustomerFee > 0)
                                    detailedNote += $" Return shipping fee of {refund.ReturnToCustomerFee:N0} VND was refunded to customer wallet.";
                            }
                            else
                            {
                                detailedNote += ". Return pickup shipment cancelled: items were not handed over to courier or shipment was cancelled. Refund request cancelled.";
                            }
                        }
                        else if (targetRefundStatusId.Value == (byte)RefundStatusEnum.RefundDamage)
                        {
                            if (isReturnToCustomer)
                            {
                                detailedNote += ". Returned items to customer were damaged/lost in transit by courier.";
                                if (refund.ReturnToCustomerFeePaid && refund.ReturnToCustomerFee > 0)
                                    detailedNote += $" Return shipping fee of {refund.ReturnToCustomerFee:N0} VND was refunded to customer wallet.";
                            }
                        }
                        else if (targetRefundStatusId.Value == (byte)RefundStatusEnum.RefundReturnToCustomerFailed)
                        {
                            // Lỗi DO KHÁCH: Shipper đã đến giao nhưng khách không nhận hàng / không nghe máy.
                            // → KHÔNG hoàn lại phí giao trả (ReturnToCustomerFee) — khách chịu trách nhiệm.
                            // Chỉ hoàn phí ship khi là lỗi do Shop (không bàn giao hàng cho shipper → RefundCancelled).
                            detailedNote += ". Delivery of rejected items back to customer failed (customer was unreachable or refused package). Return shipping fee is non-refundable.";
                        }

                        // Lưu nhật ký chuyển đổi trạng thái refund
                        refund.RefundStatusHistories.Add(new RefundStatusHistory
                        {
                            StatusId = targetRefundStatusId.Value,
                            ChangedBy = null,
                            Note = detailedNote,
                            CreatedAt = now
                        });

                        _unitOfWork.Refunds.Update(refund);
                    }
                }

                // Cập nhật đường dẫn ảnh POD nếu có
                if (uploadedImageUrl != null)
                {
                    if (isReturnToCustomer)
                    {
                        refund.ReturnToCustomerImageUrl = uploadedImageUrl;
                    }
                    else
                    {
                        refund.ReturnDeliveryImageUrl = uploadedImageUrl;
                    }
                    refund.UpdatedAt = now;
                    _unitOfWork.Refunds.Update(refund);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation(
                    "Đã xử lý Refund webhook: RefundId={RefundId}, đã chuyển sang StatusId={StatusId} thông qua trạng thái từ provider={ProviderStatus}",
                    refund.RefundId, targetRefundStatusId?.ToString() ?? "none", newStatus);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Transaction thất bại khi cập nhật Refund ID {RefundId} qua webhook", refund.RefundId);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi trong quá trình xử lý Refund webhook cho mã {Code}", providerOrderCode);
        }
    }

    /// <summary>
    /// Thử lấy URL hình ảnh từ payload GHN để xử lý minh chứng giao hàng (POD).
    /// </summary>
    private string? TryGetGhnImageUrl(string rawPayload)
    {
        try
        {
            var ghnPayload = JsonSerializer.Deserialize<GhnWebhookPayload>(rawPayload);
            return ghnPayload?.GetDeliveryImageUrl();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi giải mã payload GHN khi trích xuất hình ảnh cho refund webhook.");
            return null;
        }
    }
}
