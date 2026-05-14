using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Products;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IImageUploadService _imageUploadService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IProductService productService, 
        IImageUploadService imageUploadService,
        ILogger<ProductsController> logger)
    {
        _productService = productService;
        _imageUploadService = imageUploadService;
        _logger = logger;
    }

    [HttpPost("upload-image")]
    public async Task<ActionResult<UploadImageResponseDto>> UploadImage(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file was provided." });
        }

        using var stream = file.OpenReadStream();
        var result = await _imageUploadService.UploadImageAsync(stream, file.FileName, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new UploadImageResponseDto { Url = result.Data! });
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ProductListDto>>> GetProducts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false,
        [FromQuery] string? searchTerm = null,
        [FromQuery] short? superCategoryId = null,
        [FromQuery] short? categoryId = null,
        [FromQuery] List<short>? categoryIds = null,
        [FromQuery] List<int>? brandIds = null,
        [FromQuery] List<byte>? priceRangeIds = null,
        [FromQuery] List<short>? materialIds = null,
        [FromQuery] List<byte>? ageIds = null,
        [FromQuery] List<byte>? sexIds = null,
        [FromQuery] List<byte>? originIds = null,
        [FromQuery] int? rating = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _productService.GetProductsAsync(
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            searchTerm,
            superCategoryId,
            categoryId,
            categoryIds,
            brandIds,
            priceRangeIds,
            materialIds,
            ageIds,
            sexIds,
            originIds,
            rating,
            status,
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpGet("{productId:int}")]
    public async Task<ActionResult<ProductDto>> GetProductById(
        [FromRoute] int productId,
        CancellationToken cancellationToken = default)
    {
        var result = await _productService.GetProductByIdAsync(productId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("lookups")]
    public async Task<ActionResult<ProductLookupsDto>> GetProductLookups(CancellationToken cancellationToken = default)
    {
        var result = await _productService.GetProductLookupsAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> CreateProduct(
        [FromBody] CreateProductDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _productService.CreateProductAsync(dto, cancellationToken);
        if (result.IsSuccess)
        {
            _logger.LogInformation("Created product {ProductId}", result.Data!.ProductId);
        }

        return result.ToCreatedResult($"api/products/{result.Data?.ProductId}");
    }

    [HttpPut("{productId:int}")]
    public async Task<ActionResult<ProductDto>> UpdateProduct(
        [FromRoute] int productId,
        [FromBody] UpdateProductDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _productService.UpdateProductAsync(productId, dto, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("search")]
    public async Task<ActionResult<PaginatedResponse<ProductListDto>>> SearchProducts(
        [FromQuery] string searchTerm,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _productService.SearchProductsAsync(
            searchTerm,
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("inventory-report")]
    public async Task<ActionResult<InventoryReportFileDto>> ExportInventoryReport(
        [FromBody] InventoryReportRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var result = await _productService.ExportInventoryReportAsync(request, cancellationToken);
        if (!result.IsSuccess || result.Data == null)
        {
            return result.ToActionResult();
        }

        return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
    }
}
