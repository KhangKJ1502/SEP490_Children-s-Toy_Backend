using System.Text.Json;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Constants;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Services;

/// <summary>
/// Xu ly webhook tu shipper: cap nhat ShippingProviderTransaction,
/// ghi ShippingStatusHistory, va dong bo trang thai Order neu can.
/// </summary>
public class ShippingWebhookService : IShippingWebhookService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ShippingWebhookService> _logger;

    public ShippingWebhookService(
        IUnitOfWork unitOfWork,
        ILogger<ShippingWebhookService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger     = logger;
    }

    public async Task HandleAsync(
        string provider,
        string rawPayload,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Parse payload lay ProviderOrderCode va NewStatus
            using var doc = JsonDocument.Parse(rawPayload);
            var root = doc.RootElement;

            var providerOrderCode = TryGetString(root, "order_code")
                ?? TryGetString(root, "OrderCode");

            var newStatus = TryGetString(root, "status")
                ?? TryGetString(root, "Status");

            if (string.IsNullOrWhiteSpace(providerOrderCode) || string.IsNullOrWhiteSpace(newStatus))
            {
                _logger.LogWarning(
                    "Shipping webhook from {Provider}: missing order_code or status. Payload: {Payload}",
                    provider, Truncate(rawPayload));
                return;
            }

            // Tim ShippingProviderTransaction
            var tx = await _unitOfWork.Orders.GetShippingTransactionByProviderCodeAsync(
                providerOrderCode, cancellationToken);

            if (tx is null)
            {
                _logger.LogWarning(
                    "Shipping webhook from {Provider}: ProviderOrderCode '{Code}' not found",
                    provider, providerOrderCode);
                return;
            }

            var previousStatus = tx.Status ?? string.Empty;
            var now = DateTime.UtcNow;

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // INSERT ShippingStatusHistory
                await _unitOfWork.Orders.AddShippingStatusHistoryAsync(new ShippingStatusHistory
                {
                    ShippingTxId   = tx.ShippingTransactionId,
                    OrderId        = tx.OrderId,
                    PreviousStatus = previousStatus,
                    NewStatus      = newStatus,
                    Source         = "webhook",
                    RawPayload     = rawPayload,
                    ProcessedAt    = now
                }, cancellationToken);

                // UPDATE ShippingProviderTransaction
                tx.Status    = newStatus;
                tx.UpdatedAt = now;

                // Map sang trang thai don hang
                if (OrderStatuses.GhnStatusMap.TryGetValue(newStatus, out var mappedOrderStatus)
                    && mappedOrderStatus is not null)
                {
                    await UpdateOrderStatusAsync(tx.Order, mappedOrderStatus, now, cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation(
                    "Shipping webhook processed: provider={Provider}, code={Code}, status={Status}",
                    provider, providerOrderCode, newStatus);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex,
                "Shipping webhook from {Provider}: invalid JSON payload", provider);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Shipping webhook from {Provider}: unexpected error while processing", provider);
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task UpdateOrderStatusAsync(
        Order order,
        string targetStatusName,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(cancellationToken);

        if (!statusMap.TryGetValue(targetStatusName, out var targetStatusId))
        {
            _logger.LogWarning("Webhook: status '{Status}' not found in database", targetStatusName);
            return;
        }

        // Khong ghi de neu don da o trang thai do hoac trang thai sau
        if (order.StatusId == targetStatusId)
            return;

        order.StatusId  = targetStatusId;
        order.UpdatedAt = now;

        if (targetStatusName == OrderStatuses.Delivered)
            order.DeliveredAt = now;

        await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
        {
            OrderId   = order.OrderId,
            StatusId  = targetStatusId,
            ChangedBy = null,
            Note      = $"Auto-updated from shipping webhook: {targetStatusName}",
            CreatedAt = now
        }, cancellationToken);
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop)
            && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString();
        }
        return null;
    }

    private static string Truncate(string s, int max = 300)
        => s.Length <= max ? s : s[..max];
}
