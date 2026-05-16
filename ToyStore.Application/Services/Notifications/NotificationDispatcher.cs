using System.Text.Json;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;

namespace ToyStore.Application.Services.Notifications;

/// <summary>
/// Orchestrator chính của hệ thống thông báo.
/// Luồng: render template → kiểm tra preferences → idempotency → gửi qua từng channel.
/// </summary>
public class NotificationDispatcher : INotificationDispatcher
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationTemplateRenderer _renderer;
    private readonly INotificationPreferencesGate _prefsGate;
    private readonly IEnumerable<INotificationChannel> _channels;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(
        IUnitOfWork unitOfWork,
        INotificationTemplateRenderer renderer,
        INotificationPreferencesGate prefsGate,
        IEnumerable<INotificationChannel> channels,
        ILogger<NotificationDispatcher> logger)
    {
        _unitOfWork = unitOfWork;
        _renderer   = renderer;
        _prefsGate  = prefsGate;
        _channels   = channels;
        _logger     = logger;
    }

    public async Task DispatchAsync(NotificationContext context, CancellationToken ct = default)
    {
        // Bước 1: Render template — nếu Title đã có (campaign pre-render) thì dùng luôn
        string title;
        string message;

        if (context.Title is not null && context.Message is not null)
        {
            // Campaign path: đã render sẵn, dùng luôn
            title   = context.Title;
            message = context.Message;
        }
        else if (context.TemplateCode is not null)
        {
            try
            {
                (title, message) = await _renderer.RenderAsync(
                    context.TemplateCode, context.Placeholders, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Template render failed. TemplateCode={Code} AccountID={Id}",
                    context.TemplateCode, context.RecipientAccountId);
                return;
            }
        }
        else
        {
            _logger.LogWarning(
                "NotificationContext has neither TemplateCode nor pre-rendered Title/Message. AccountID={Id}",
                context.RecipientAccountId);
            return;
        }

        // Bước 2: Chuẩn bị payload JSON
        var payloadJson = context.Payload is not null
            ? JsonSerializer.Serialize(context.Payload)
            : "{}";

        // Bước 3: Gửi qua từng kênh được bật
        if (context.SendBell)
            await SendViaChannelAsync(
                NotificationChannels.WebBell, context, title, message, payloadJson, ct);

        if (context.SendEmail)
            await SendViaChannelAsync(
                NotificationChannels.Email, context, title, message, payloadJson, ct);
    }

    public async Task DispatchBulkAsync(IEnumerable<NotificationContext> contexts, CancellationToken ct = default)
    {
        foreach (var ctx in contexts)
            await DispatchAsync(ctx, ct);
    }

    private async Task SendViaChannelAsync(
        string channelName,
        NotificationContext context,
        string title,
        string message,
        string payloadJson,
        CancellationToken ct)
    {
        // Kiểm tra UserPreferences
        if (!await _prefsGate.CanSendAsync(context.RecipientAccountId, channelName, context.NotificationType, ct))
            return;

        // Xây dựng IdempotencyKey
        // Ưu tiên: key rõ ràng từ context → key tự động từ ReferenceId → null (không idempotent)
        string? idempotencyKey;
        if (context.IdempotencyKey is not null)
        {
            idempotencyKey = $"{context.IdempotencyKey}:{channelName}";
        }
        else if (context.ReferenceId is not null)
        {
            idempotencyKey = $"{context.TemplateCode}:{context.RecipientAccountId}:{context.ReferenceId}:{channelName}";
        }
        else
        {
            idempotencyKey = null;
        }

        // Kiểm tra idempotency
        if (idempotencyKey is not null &&
            await _unitOfWork.Deliveries.ExistsByIdempotencyKeyAsync(idempotencyKey, ct))
        {
            _logger.LogInformation(
                "Notification already sent (idempotent). Key={Key}", idempotencyKey);
            return;
        }

        // Tìm channel implementation
        var channel = _channels.FirstOrDefault(c =>
            c.Channel.Equals(channelName, StringComparison.OrdinalIgnoreCase));

        if (channel is null)
        {
            _logger.LogWarning("No INotificationChannel registered for '{Channel}'.", channelName);
            return;
        }

        var request = new NotificationDeliveryRequest
        {
            AccountId        = context.RecipientAccountId,
            RecipientType    = context.RecipientType,
            NotificationType = context.NotificationType,
            Channel          = channelName,
            Title            = title,
            Message          = message,
            TemplateCode     = context.TemplateCode,   // may be null for campaign-only path
            IdempotencyKey   = idempotencyKey,
            PayloadJson      = payloadJson,
            ImageUrl         = context.ImageUrl,
            ActionType       = context.ActionType,
            ActionTarget     = context.ActionTarget,
            CampaignId       = context.CampaignId,
        };

        await channel.SendAsync(request, ct);
    }
}
