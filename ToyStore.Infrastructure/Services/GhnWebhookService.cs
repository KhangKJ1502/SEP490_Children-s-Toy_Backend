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


    public Task ProcessAsync(GhnWebhookPayload payload, CancellationToken cancellationToken = default)
    {
        return ProcessAsync(payload, imageStream: null, fileName: null, cancellationToken);
    }

    public async Task ProcessAsync(
        GhnWebhookPayload payload, 
        Stream? imageStream, 
        string? fileName, 
        CancellationToken cancellationToken = default)
    {
        if (payload == null)
        {
            _logger.LogWarning("GHN Webhook: Null payload received.");
            return;
        }

        var orderCode = (payload.EffectiveOrderCode ?? payload.EffectiveClientOrderCode)?.Trim();
        if (string.IsNullOrWhiteSpace(orderCode))
        {
            _logger.LogWarning("GHN Webhook: Invalid payload or empty OrderCode.");
            return;
        }

        var refund = await _unitOfWork.Refunds.GetByShippingOrReturnOrderCodeAsync(orderCode, cancellationToken);
        if (refund is not null)
        {
            _logger.LogInformation("GHN Webhook: OrderCode '{Code}' matches refund request. Delegating to IShippingWebhookService.", orderCode);
            await _shippingWebhookService.HandleAsync("GHN", JsonSerializer.Serialize(payload), cancellationToken);
            return;
        }

        // Fetch transaction by provider code (OrderCode) or fallback to ClientOrderCode
        var tx = await _unitOfWork.Orders.GetShippingTransactionByProviderCodeAsync(orderCode, cancellationToken);
        if (tx == null && !string.IsNullOrWhiteSpace(payload.EffectiveClientOrderCode))
        {
            tx = await _unitOfWork.Orders.GetShippingTransactionByProviderCodeAsync(payload.EffectiveClientOrderCode.Trim(), cancellationToken);
        }

        if (tx == null)
        {
            _logger.LogWarning("GHN Webhook: Transaction not found for OrderCode '{Code}'", orderCode);
            return;
        }

        var status = payload.EffectiveStatus?.Trim();

        // If status is empty but delivery image is provided (either binary stream or payload string), handle image upload directly
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
                    _logger.LogWarning("GHN Webhook: Empty status and no delivery image in payload for OrderCode '{Code}'.", orderCode);
                }
            }
            return;
        }

        var rawPayload = JsonSerializer.Serialize(payload);

        // Idempotency check:
        if (await _unitOfWork.Orders.ExistsShippingStatusHistoryAsync(tx.ShippingTransactionId, status, rawPayload, cancellationToken))
        {
            _logger.LogInformation("GHN Webhook duplicate skipped: code={Code}, status={Status}", orderCode, status);
            return;
        }

        var previousStatus = tx.Status ?? string.Empty;
        var now = _timeProvider.UtcNow;

        var pendingNotifications = new List<PendingShippingNotification>();
        var releaseCapacity = false;
        var orderIdForCapacity = tx.OrderId;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // 1. Add shipping status history record
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

            // 2. Update transaction status
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

            // 3. Handle Order entity columns & flow based on status
            var statusLower = status.ToLowerInvariant();


            // A. Update POD delivery image: Prefer direct binary stream if available, otherwise check payload string (URL or Base64)
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



            // A2. Update direct tracking columns on the Order
            if (statusLower == "delivery_fail")

            {
                tx.Order.FailedDeliveryAt = now;
                tx.Order.LastGHNFailCode = payload.ReasonCode;
                tx.Order.DeliveryFailCount = (byte)(tx.Order.DeliveryFailCount + 1);
                
                if (!string.IsNullOrEmpty(payload.Reason))
                {
                    tx.Order.CancelReason = payload.Reason;
                }

                _logger.LogInformation("GHN Webhook delivery fail updated: order={OrderCode}, fail count={Count}, reason={Reason}",
                    tx.Order.OrderCode, tx.Order.DeliveryFailCount, payload.ReasonCode);
            }
            else if (statusLower == "returned")
            {
                tx.Order.ReturnedAt = now;
                _logger.LogInformation("GHN Webhook returned updated: order={OrderCode}", tx.Order.OrderCode);
            }

            // B. Resolve return flow or normal delivery action
            var action = _statusMapper.ResolveWebhookAction(status);

            if (action == ShippingWebhookAction.UpdateOrderStatus)
            {
                var targetStatusId = (byte)GhnStatusMapper.ToInternalStatusId(status);
                if (targetStatusId > 0)
                {
                    if (targetStatusId == (byte)OrderStatus.Delivered)
                    {
                        // Use Lifecycle Service for delivery completion (releases shift, processes events)
                        var result = await _orderLifecycle.DeliverOrderAsync(tx.OrderId, cancellationToken);
                        if (!result.IsSuccess)
                        {
                            _logger.LogWarning("Failed to mark order {OrderId} as delivered via lifecycle: {Error}",
                                tx.OrderId, result.ErrorMessage);
                        }
                    }
                    else
                    {
                        // Normal order status progression with out-of-order check
                        bool isTerminal = tx.Order.StatusId == (byte)OrderStatus.Cancelled || 
                                          tx.Order.StatusId == (byte)OrderStatus.Refunded || 
                                          tx.Order.StatusId == (byte)OrderStatus.ReturnCompleted;

                        if (!isTerminal && OrderWebhookTransitionValidator.CanApplyWebhookStatus(tx.Order.StatusId, targetStatusId))
                        {
                            tx.Order.StatusId = targetStatusId;
                            tx.Order.UpdatedAt = now;

                            var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(cancellationToken);
                            var targetStatusName = statusMap.FirstOrDefault(x => x.Value == targetStatusId).Key ?? status;

                            await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
                            {
                                OrderId = tx.OrderId,
                                StatusId = targetStatusId,
                                ChangedBy = null,
                                Note = $"Auto-updated from GHN webhook: {targetStatusName}",
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
                // Process return flow using the standard Return Flow service
                var flowResult = await _returnFlow.ProcessActionAsync(action, tx.Order, tx, status, now, cancellationToken);
                pendingNotifications.AddRange(flowResult.Notifications);
                releaseCapacity = flowResult.ReleaseShiftCapacity;
            }
            else
            {
                _logger.LogWarning("GHN Webhook unknown action for status '{Status}' on order {OrderId}", status, tx.OrderId);
            }

            // C. Synchronize Payment Status
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

                _logger.LogInformation("Order {OrderCode} payment status auto-updated to {PaymentStatus} via GHN webhook",
                    tx.Order.OrderCode, newPaymentStatus);
            }


            // E. Suspect fail code / customer blacklisting triggers
            if (statusLower == "delivery_fail" && GhnFailCodeMapper.ShouldBlacklist(payload.ReasonCode ?? ""))
            {
                // Publish warning event for suspicious delivery failure (staff/admin action requested)
                pendingNotifications.Add(new PendingShippingNotification(
                    NotificationEventTypes.SystemShippingWebhookError,
                    new
                    {
                        orderId = tx.OrderId,
                        orderCode = tx.Order.OrderCode,
                        providerStatus = status,
                        reasonCode = payload.ReasonCode,
                        message = $"Suspicious GHN failure code '{payload.ReasonCode}' detected (potential customer blacklist needed)."
                    }));
            }

            // Save and Commit!
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("GHN Webhook transaction committed successfully for code={Code}, status={Status}",
                payload.OrderCode, status);

            // 4. Publish Event Notifications (Out of transaction for performance and reliability)
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

            // 5. Release shift assignment capacity if requested
            if (releaseCapacity)
            {
                try
                {
                    await _shiftAssignmentService.ReleaseCapacityAsync(orderIdForCapacity, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to release capacity for order {OrderId}", orderIdForCapacity);
                }
            }
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error processing GHN webhook callback for order {Code}", payload.OrderCode);
            throw;
        }
    }

    private static string? ResolveNotificationEventType(string statusLower)
    {
        return statusLower switch
        {
            "delivery_fail"     => NotificationEventTypes.OrderDeliveryFailed,
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
                // Direct server-to-server Cloudinary upload from source URL
                uploadResult = await _imageUploadService.UploadImageFromUrlAsync(
                    imageInput,
                    folder: "order_pods",
                    publicId: order.OrderCode,
                    cancellationToken);
            }
            else
            {
                _logger.LogWarning("Invalid POD image input format for order {OrderCode}", order.OrderCode);
                return;
            }

            if (uploadResult.IsSuccess && !string.IsNullOrEmpty(uploadResult.Data))
            {
                order.DeliveryImageUrl = uploadResult.Data;
                order.UpdatedAt = _timeProvider.UtcNow;
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("GHN Webhook POD image uploaded to Cloudinary: {Url} for order {OrderCode}",
                    uploadResult.Data, order.OrderCode);
            }
            else
            {
                _logger.LogWarning("Failed to upload GHN POD image to Cloudinary for order {OrderCode}: {Error}",
                    order.OrderCode, uploadResult.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading GHN POD image to Cloudinary for order {OrderCode}", order.OrderCode);
        }
    }


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

                _logger.LogInformation("GHN Webhook binary POD image stream uploaded to Cloudinary: {Url} for order {OrderCode}",
                    uploadResult.Data, order.OrderCode);
            }
            else
            {
                _logger.LogWarning("Failed to upload binary GHN POD image stream to Cloudinary for order {OrderCode}: {Error}",
                    order.OrderCode, uploadResult.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while uploading binary POD image stream to Cloudinary for order {OrderCode}", order.OrderCode);
        }
    }
}



