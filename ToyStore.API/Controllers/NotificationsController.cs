using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.Application.Constants;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(IUnitOfWork unitOfWork, ILogger<NotificationsController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger     = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var total = await _unitOfWork.Deliveries.CountBellNotificationsAsync(accountId.Value, status, ct);
        var items = await _unitOfWork.Deliveries.GetBellNotificationsAsync(accountId.Value, status, page, pageSize, ct);

        var resultItems = items.Select(d => new
        {
            d.DeliveryId,
            d.NotificationType,
            d.Title,
            d.Message,
            d.Status,
            d.ImageUrl,
            d.ActionType,
            d.ActionTarget,
            d.CreatedAt,
            d.ReadAt,
        });

        var unreadCount = await _unitOfWork.Deliveries.CountAsync(accountId.Value, NotificationChannels.WebBell, NotificationStatuses.Unread, ct);

        return Ok(new { total, unreadCount, page, pageSize, items = resultItems });
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct = default)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var count = await _unitOfWork.Deliveries.CountAsync(accountId.Value, NotificationChannels.WebBell, NotificationStatuses.Unread, ct);

        return Ok(new { count });
    }

    [HttpPatch("{deliveryId:long}/read")]
    public async Task<IActionResult> MarkRead(long deliveryId, CancellationToken ct = default)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var delivery = await _unitOfWork.Deliveries.GetByIdAsync(deliveryId, ct);

        if (delivery is null || delivery.AccountId != accountId.Value) return NotFound();

        if (delivery.Status == NotificationStatuses.Unread)
        {
            delivery.Status    = NotificationStatuses.Read;
            delivery.ReadAt    = DateTime.UtcNow;
            delivery.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(ct);
        }

        return NoContent();
    }

    [HttpPatch("mark-all-read")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct = default)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        await _unitOfWork.Deliveries.MarkAllReadAsync(accountId.Value, ct);

        return NoContent();
    }

    [HttpPatch("{deliveryId:long}/archive")]
    public async Task<IActionResult> Archive(long deliveryId, CancellationToken ct = default)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var delivery = await _unitOfWork.Deliveries.GetByIdAsync(deliveryId, ct);

        if (delivery is null || delivery.AccountId != accountId.Value) return NotFound();

        delivery.Status    = NotificationStatuses.Archived;
        delivery.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpPost("{deliveryId:long}/click")]
    public async Task<IActionResult> RecordClick(long deliveryId, CancellationToken ct = default)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var delivery = await _unitOfWork.Deliveries.GetByIdAsync(deliveryId, ct);

        if (delivery is null || delivery.AccountId != accountId.Value) return NotFound();

        _unitOfWork.Deliveries.AddAction(new DeliveryAction
        {
            DeliveryId  = deliveryId,
            AccountId   = accountId.Value,
            ActionType  = "Click",
            ActionTarget = delivery.ActionTarget,
            OccurredAt  = DateTime.UtcNow,
        });

        // Update campaign stats if applicable
        if (delivery.CampaignId.HasValue)
        {
            var campaign = await _unitOfWork.Campaigns.GetByIdAsync(delivery.CampaignId.Value, ct);
            if (campaign?.CampaignStat is not null)
            {
                campaign.CampaignStat.TotalClicked++;
                campaign.CampaignStat.ComputedAt = DateTime.UtcNow;
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return NoContent();
    }

    private int? GetAccountId()
    {
        var claim = User.FindFirst("AccountID")?.Value
                 ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
