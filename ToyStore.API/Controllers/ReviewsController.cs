using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Reviews;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    /// <summary>
    /// Xem danh sách review theo sản phẩm (Guest/Customer).
    /// GET /api/reviews
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ReviewProductListDto>>> GetReviews(
        [FromQuery] ReviewQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await _reviewService.GetPublicListAsync(query, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Khách hàng tạo review mới cho sản phẩm.
    /// POST /api/reviews
    /// </summary>
    [Authorize(Roles = "Customer")]
    [HttpPost]
    public async Task<ActionResult<ReviewProductDto>> CreateReview(
        [FromForm] CreateReviewProductDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _reviewService.CreateReviewAsync(dto, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Khách hàng sửa review của mình (chỉ 1 lần, trong 3 ngày).
    /// PUT /api/reviews/{id}
    /// </summary>
    [Authorize(Roles = "Customer")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ReviewProductDto>> UpdateReview(
        int id,
        [FromForm] UpdateReviewProductDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _reviewService.UpdateReviewAsync(id, dto, cancellationToken);
        return result.ToActionResult();
    }


}
