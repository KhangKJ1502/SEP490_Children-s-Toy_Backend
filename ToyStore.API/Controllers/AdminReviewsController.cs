using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Reviews;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/admin/reviews")]
[Authorize(Roles = "Staff,Admin")]
public class AdminReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public AdminReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    /// <summary>
    /// Admin/Staff xem toàn bộ danh sách review.
    /// GET /api/admin/reviews
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<AdminReviewListDto>>> GetReviews(
        [FromQuery] AdminReviewQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await _reviewService.GetAdminListAsync(query, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Admin/Staff xem chi tiết 1 review.
    /// GET /api/admin/reviews/{id}
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdminReviewDetailDto>> GetReviewDetail(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _reviewService.GetAdminDetailAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Admin/Staff cập nhật ModerationStatus thủ công.
    /// PUT /api/admin/reviews/{id}/status
    /// </summary>
    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<AdminReviewDetailDto>> UpdateStatus(
        int id,
        [FromBody] UpdateModerationStatusDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _reviewService.UpdateModerationStatusAsync(id, dto, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Admin/Staff đăng reply.
    /// POST /api/admin/reviews/{id}/reply
    /// </summary>
    [HttpPost("{id:int}/reply")]
    public async Task<ActionResult<StaffReplyDto>> CreateReply(
        int id,
        [FromBody] CreateStaffReplyDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _reviewService.CreateReplyAsync(id, dto, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Admin/Staff sửa reply.
    /// PUT /api/admin/reviews/{id}/reply/{replyId}
    /// </summary>
    [HttpPut("{id:int}/reply/{replyId:int}")]
    public async Task<ActionResult<StaffReplyDto>> UpdateReply(
        int id,
        int replyId,
        [FromBody] UpdateStaffReplyDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _reviewService.UpdateReplyAsync(id, replyId, dto, cancellationToken);
        return result.ToActionResult();
    }
}
