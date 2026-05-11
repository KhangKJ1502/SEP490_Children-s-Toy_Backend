using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Categories;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

/// <summary>
/// APIs quản lý category.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(ICategoryService categoryService, ILogger<CategoriesController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách category.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<CategoryListDto>>> GetCategories(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false,
        [FromQuery] string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _categoryService.GetCategoriesAsync(
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            searchTerm,
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>
    /// Lấy thông tin chi tiết category theo ID.
    /// </summary>
    [HttpGet("{categoryId:int}")]
    public async Task<ActionResult<CategoryListDto>> GetCategoryById(
        [FromRoute] short categoryId,
        CancellationToken cancellationToken = default)
    {
        var result = await _categoryService.GetCategoryByIdAsync(categoryId, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Tạo mới category.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CategoryListDto>> CreateCategory(
        [FromBody] CreateCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _categoryService.CreateCategoryAsync(dto, cancellationToken);
        if (result.IsSuccess)
        {
            _logger.LogInformation("Created category {CategoryId}", result.Data!.CategoryId);
        }

        return result.ToCreatedResult($"api/categories/{result.Data?.CategoryId}");
    }

    /// <summary>
    /// Cập nhật category.
    /// </summary>
    [HttpPut("{categoryId:int}")]
    public async Task<ActionResult<CategoryListDto>> UpdateCategory(
        [FromRoute] short categoryId,
        [FromBody] UpdateCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _categoryService.UpdateCategoryAsync(categoryId, dto, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Tìm kiếm category.
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<PaginatedResponse<CategoryListDto>>> SearchCategories(
        [FromQuery] string searchTerm,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _categoryService.SearchCategoriesAsync(
            searchTerm,
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            cancellationToken);

        return result.ToActionResult();
    }
}