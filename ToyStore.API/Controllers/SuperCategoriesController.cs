using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.SuperCategories;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

/// <summary>
/// APIs quản lý super category.
/// </summary>
[ApiController]
[Route("api/categories/super-categories")]
public class SuperCategoriesController : ControllerBase
{
    private readonly ISuperCategoryService _superCategoryService;
    private readonly ILogger<SuperCategoriesController> _logger;

    public SuperCategoriesController(ISuperCategoryService superCategoryService, ILogger<SuperCategoriesController> logger)
    {
        _superCategoryService = superCategoryService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách super category.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<SuperCategoryListDto>>> GetSuperCategories(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false,
        [FromQuery] string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _superCategoryService.GetSuperCategoriesAsync(
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            searchTerm,
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>
    /// Tạo mới super category.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SuperCategoryListDto>> CreateSuperCategory(
        [FromBody] CreateSuperCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _superCategoryService.CreateSuperCategoryAsync(dto, cancellationToken);
        if (result.IsSuccess)
        {
            _logger.LogInformation("Created super category {SuperCategoryId}", result.Data!.SuperCategoryId);
        }

        return result.ToCreatedResult($"api/categories/super-categories/{result.Data?.SuperCategoryId}");
    }

    /// <summary>
    /// Cập nhật super category.
    /// </summary>
    [HttpPut("{superCategoryId:int}")]
    public async Task<ActionResult<SuperCategoryListDto>> UpdateSuperCategory(
        [FromRoute] short superCategoryId,
        [FromBody] UpdateSuperCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _superCategoryService.UpdateSuperCategoryAsync(superCategoryId, dto, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Tìm kiếm super category.
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<PaginatedResponse<SuperCategoryListDto>>> SearchSuperCategories(
        [FromQuery] string searchTerm,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _superCategoryService.SearchSuperCategoriesAsync(
            searchTerm,
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            cancellationToken);

        return result.ToActionResult();
    }
}
