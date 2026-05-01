using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Products;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IImageUploadService _imageUploadService;
    private readonly SEP490ToyStoreContext _dbContext;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IProductService productService, 
        IImageUploadService imageUploadService,
        SEP490ToyStoreContext dbContext,
        ILogger<ProductsController> logger)
    {
        _productService = productService;
        _imageUploadService = imageUploadService;
        _dbContext = dbContext;
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
        CancellationToken cancellationToken = default)
    {
        var result = await _productService.GetProductsAsync(
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            searchTerm,
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
    public async Task<ActionResult<object>> GetProductLookups(CancellationToken cancellationToken = default)
    {
        var priceRanges = await _dbContext.PriceRanges
            .AsNoTracking()
            .OrderBy(x => x.PriceRangeMin)
            .Select(x => new
            {
                id = x.PriceRangeId,
                label = $"{x.PriceRangeMin:N0} - {x.PriceRangeMax:N0} VND"
            })
            .ToListAsync(cancellationToken);

        var materials = await _dbContext.Materials
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.MaterialName)
            .Select(x => new { id = x.MaterialId, label = x.MaterialName })
            .ToListAsync(cancellationToken);

        var ages = await _dbContext.Ages
            .AsNoTracking()
            .OrderBy(x => x.AgeRange)
            .Select(x => new { id = x.AgeId, label = x.AgeRange })
            .ToListAsync(cancellationToken);

        var sexes = await _dbContext.Sexes
            .AsNoTracking()
            .OrderBy(x => x.SexName)
            .Select(x => new { id = x.SexId, label = x.SexName })
            .ToListAsync(cancellationToken);

        var origins = await _dbContext.Origins
            .AsNoTracking()
            .OrderBy(x => x.OriginName)
            .Select(x => new { id = x.OriginId, label = x.OriginName })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            priceRanges,
            materials,
            ages,
            sexes,
            origins
        });
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
}
