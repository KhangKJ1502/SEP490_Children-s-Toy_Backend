using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Notifications;
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

    /// <summary>
    /// Get paginated bell notifications for the current user.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<NotificationListDto>>>> GetNotifications(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        page     = Math.Max(1, page);
        pageSize = Math.Min(50, Math.Max(1, pageSize));

        var total       = await _unitOfWork.Deliveries.CountBellNotificationsAsync(accountId.Value, status, ct);
        var deliveries  = await _unitOfWork.Deliveries.GetBellNotificationsAsync(accountId.Value, status, page, pageSize, ct);
        var unreadCount = await _unitOfWork.Deliveries.GetUnreadCountAsync(accountId.Value, ct);

        var dtos = deliveries.Select(d => new NotificationListDto
        {
            DeliveryId       = d.DeliveryId,
            NotificationType = d.NotificationType,
            Title            = d.Title,
            Message          = d.Message,
            Status           = d.Status,
            ImageUrl         = d.ImageUrl,
            ActionType       = d.ActionType,
            ActionTarget     = d.ActionTarget,
            CreatedAt        = d.CreatedAt,
            ReadAt           = d.ReadAt,
        }).ToList();

        var paginated = new PaginatedResponse<NotificationListDto>(dtos, total, page, pageSize);

        return Ok(ApiResponse<PaginatedResponse<NotificationListDto>>.Ok(paginated,
            $"{total} notifications found. Unread: {unreadCount}"));
    }

    /// <summary>
    /// Get unread notification count.
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<ActionResult<ApiResponse<int>>> GetUnreadCount(CancellationToken ct = default)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var count = await _unitOfWork.Deliveries.GetUnreadCountAsync(accountId.Value, ct);
        return Ok(ApiResponse<int>.Ok(count));
    }

    /// <summary>
    /// Mark a specific notification as read.
    /// </summary>
    [HttpPatch("{deliveryId:long}/read")]
    public async Task<IActionResult> MarkRead(long deliveryId, CancellationToken ct = default)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        await _unitOfWork.Deliveries.MarkReadAsync(deliveryId, accountId.Value, ct);

        return NoContent();
    }

    /// <summary>
    /// Mark all bell notifications as read.
    /// </summary>
    [HttpPatch("mark-all-read")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct = default)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        await _unitOfWork.Deliveries.MarkAllReadAsync(accountId.Value, ct);
        return NoContent();
    }

    /// <summary>
    /// Archive a notification (soft-hide from bell list).
    /// </summary>
    [HttpPatch("{deliveryId:long}/archive")]
    public async Task<IActionResult> Archive(long deliveryId, CancellationToken ct = default)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var delivery = await _unitOfWork.Deliveries.GetByIdAsync(deliveryId, ct);
        if (delivery is null || delivery.AccountId != accountId.Value) return NotFound();

        delivery.Status    = NotificationStatuses.Archived;
        delivery.UpdatedAt = DateTime.Now;
        await _unitOfWork.SaveChangesAsync(ct);

        return NoContent();
    }

    /// <summary>
    /// Record a click action on a notification (for campaign analytics).
    /// </summary>
    [HttpPost("{deliveryId:long}/click")]
    public async Task<IActionResult> RecordClick(long deliveryId, CancellationToken ct = default)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var delivery = await _unitOfWork.Deliveries.GetByIdAsync(deliveryId, ct);
        if (delivery is null || delivery.AccountId != accountId.Value) return NotFound();

        // Check for existing click to prevent double counting
        bool alreadyClicked = await _unitOfWork.Deliveries.HasUserClickedAsync(deliveryId, accountId.Value, ct);

        // Auto-mark as read if not already read (clicking implies reading)
        if (delivery.Status == NotificationStatuses.Unread)
        {
            await _unitOfWork.Deliveries.MarkReadAsync(deliveryId, accountId.Value, ct);
        }

        if (alreadyClicked)
        {
            return NoContent();
        }

        _unitOfWork.Deliveries.AddAction(new DeliveryAction
        {
            DeliveryId   = deliveryId,
            AccountId    = accountId.Value,
            ActionType   = "Click",
            ActionTarget = delivery.ActionTarget,
            OccurredAt   = DateTime.Now,
        });

        if (delivery.CampaignId.HasValue)
        {
            await _unitOfWork.Deliveries.IncrementCampaignClickAsync(delivery.CampaignId.Value, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>
    /// Soft-delete all read notifications.
    /// </summary>
    [HttpDelete("read")]
    public async Task<IActionResult> DeleteRead(CancellationToken ct = default)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        await _unitOfWork.Deliveries.MarkAllReadAsDeletedAsync(accountId.Value, ct);
        return NoContent();
    }

    /// <summary>
    /// Soft-delete a notification.
    /// </summary>
    [HttpDelete("{deliveryId}")]
    public async Task<IActionResult> Delete(long deliveryId, CancellationToken ct = default)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        await _unitOfWork.Deliveries.MarkDeletedAsync(deliveryId, accountId.Value, ct);
        return NoContent();
    }

    private int? GetAccountId()
    {
        var claim = User.FindFirst("AccountID")?.Value
                 ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
