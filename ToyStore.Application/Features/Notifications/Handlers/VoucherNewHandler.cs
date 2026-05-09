using System.Text.Json;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;

namespace ToyStore.Application.Features.Notifications.Handlers;

/// <summary>
/// Notifies customers when a new voucher campaign is created and distributed.
/// Triggered when CampaignService dispatches a VOUCHER type campaign.
/// </summary>
public class VoucherNewHandler : IOutboxEventHandler
{
    public string EventType => NotificationEventTypes.VoucherNew;

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;
    private readonly IUserPreferenceChecker _prefChecker;

    public VoucherNewHandler(
        IUnitOfWork unitOfWork,
        INotificationDispatcher dispatcher,
        IUserPreferenceChecker prefChecker)
    {
        _unitOfWork  = unitOfWork;
        _dispatcher  = dispatcher;
        _prefChecker = prefChecker;
    }

    public async Task HandleAsync(OutboxEventData ev, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(ev.Payload);
        var root = doc.RootElement;

        var voucherCode  = root.TryGetProperty("voucherCode", out var vc) ? vc.GetString() : "";
        var discountDesc = root.TryGetProperty("discountDesc", out var dd) ? dd.GetString() : "";
        var expiryDate   = root.TryGetProperty("expiryDate", out var ed) ? ed.GetString() : "";
        var accountId    = root.TryGetProperty("accountId", out var aid) ? aid.GetInt32() : 0;

        if (accountId <= 0) return;

        if (!await _prefChecker.CanSendAsync(accountId, PreferenceKeys.Promotions, ct))
            return;

        await _dispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = accountId,
            RecipientType      = RecipientTypes.Customer,
            NotificationType   = NotificationTypes.Promotion,
            Title              = "You have a new voucher!",
            Message            = $"Voucher {voucherCode} — {discountDesc}. Valid until {expiryDate}.",
            SendBell           = true,
            SendEmail          = true,
            ActionTarget       = "/vouchers",
            IdempotencyKey     = $"{EventType}:{ev.AggregateId}:{accountId}",
            Payload            = new Dictionary<string, object>
            {
                ["voucherCode"] = voucherCode ?? "",
            },
        }, ct);
    }
}
