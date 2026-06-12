using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Promotions;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

/// <summary>
/// API quản lý promotion.
/// </summary>
[Authorize(Roles = "Admin,Staff")]
[ApiController]
[Route("api/[controller]")]
public class PromotionsController : ControllerBase
{
    private readonly IPromotionService _promotionService;
    private readonly ILogger<PromotionsController> _logger;

    public PromotionsController(IPromotionService promotionService, ILogger<PromotionsController> logger)
    {
        _promotionService = promotionService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách Flash Sale đang active/scheduled (public, không cần đăng nhập).
    /// </summary>
    [AllowAnonymous]
    [HttpGet("flash-sale")]
    public async Task<ActionResult<List<PromotionDto>>> GetFlashSalePromotions(
        CancellationToken cancellationToken = default)
    {
        var result = await _promotionService.GetFlashSalePromotionsAsync(cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Lấy danh sách promotion có phân trang, tìm kiếm và sắp xếp.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<PromotionListDto>>> GetPromotions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _promotionService.GetPromotionsAsync(
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            searchTerm,
            status,
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>
    /// Lấy promotion theo ID.
    /// </summary>
    [HttpGet("{promotionId:int}")]
    public async Task<ActionResult<PromotionDto>> GetPromotionById(
        int promotionId,
        CancellationToken cancellationToken = default)
    {
        var result = await _promotionService.GetPromotionByIdAsync(promotionId, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Tạo mới promotion.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PromotionDto>> CreatePromotion(
        [FromBody] CreatePromotionDto request,
        CancellationToken cancellationToken = default)
    {
        var result = await _promotionService.CreatePromotionAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Created promotion {PromotionId} via API",
                result.Data!.PromotionId);

            return result.ToCreatedResult($"/api/promotions/{result.Data!.PromotionId}");
        }

        return result.ToActionResult();
    }

    /// <summary>
    /// Cập nhật promotion theo ID.
    /// </summary>
    [HttpPut("{promotionId:int}")]
    public async Task<ActionResult<PromotionDto>> UpdatePromotion(
        int promotionId,
        [FromBody] UpdatePromotionDto request,
        CancellationToken cancellationToken = default)
    {
        var result = await _promotionService.UpdatePromotionAsync(promotionId, request, cancellationToken);
        return result.ToActionResult();
    }
}
