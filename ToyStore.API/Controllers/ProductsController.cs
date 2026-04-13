using Microsoft.AspNetCore.Mvc;
using ToyStore.Application.DTOs;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Enums;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;
    
    public ProductsController(IProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }
    
    /// <summary>
    /// Gets paginated list of products with filtering.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<ProductListDto>>>> GetProducts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] ToyCategory? toyCategory = null,
        [FromQuery] AgeRange? ageRange = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _productService.GetProductsAsync(
            pageNumber, pageSize, categoryId, toyCategory, ageRange,
            minPrice, maxPrice, searchTerm, sortBy, sortDescending, cancellationToken);
            
        return Ok(ApiResponse<PaginatedResponse<ProductListDto>>.Ok(result));
    }
    
    /// <summary>
    /// Gets a product by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetById(
        Guid id, CancellationToken cancellationToken)
    {
        var product = await _productService.GetByIdAsync(id, cancellationToken);
        
        if (product == null)
            return NotFound(ApiResponse<ProductDto>.Fail("Product not found"));
            
        return Ok(ApiResponse<ProductDto>.Ok(product));
    }
    
    /// <summary>
    /// Gets a product by slug.
    /// </summary>
    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetBySlug(
        string slug, CancellationToken cancellationToken)
    {
        var product = await _productService.GetBySlugAsync(slug, cancellationToken);
        
        if (product == null)
            return NotFound(ApiResponse<ProductDto>.Fail("Product not found"));
            
        return Ok(ApiResponse<ProductDto>.Ok(product));
    }
    
    /// <summary>
    /// Gets featured products.
    /// </summary>
    [HttpGet("featured")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProductListDto>>>> GetFeatured(
        [FromQuery] int limit = 10, CancellationToken cancellationToken = default)
    {
        var products = await _productService.GetFeaturedProductsAsync(limit, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ProductListDto>>.Ok(products));
    }
    
    /// <summary>
    /// Gets new arrival products.
    /// </summary>
    [HttpGet("new-arrivals")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProductListDto>>>> GetNewArrivals(
        [FromQuery] int limit = 10, CancellationToken cancellationToken = default)
    {
        var products = await _productService.GetNewArrivalsAsync(limit, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ProductListDto>>.Ok(products));
    }
    
    /// <summary>
    /// Creates a new product.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProductDto>>> Create(
        [FromBody] CreateProductDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var product = await _productService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, 
                ApiResponse<ProductDto>.Ok(product, "Product created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return BadRequest(ApiResponse<ProductDto>.Fail(ex.Message));
        }
    }
    
    /// <summary>
    /// Updates a product.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> Update(
        Guid id, [FromBody] UpdateProductDto dto, CancellationToken cancellationToken)
    {
        var product = await _productService.UpdateAsync(id, dto, cancellationToken);
        
        if (product == null)
            return NotFound(ApiResponse<ProductDto>.Fail("Product not found"));
            
        return Ok(ApiResponse<ProductDto>.Ok(product, "Product updated successfully"));
    }
    
    /// <summary>
    /// Deletes a product (soft delete).
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(
        Guid id, CancellationToken cancellationToken)
    {
        var success = await _productService.DeleteAsync(id, cancellationToken);
        
        if (!success)
            return NotFound(ApiResponse.Fail("Product not found"));
            
        return Ok(ApiResponse.Ok("Product deleted successfully"));
    }
    
    /// <summary>
    /// Updates product stock.
    /// </summary>
    [HttpPatch("{id:guid}/stock")]
    public async Task<ActionResult<ApiResponse>> UpdateStock(
        Guid id, [FromBody] int quantity, CancellationToken cancellationToken)
    {
        var success = await _productService.UpdateStockAsync(id, quantity, cancellationToken);
        
        if (!success)
            return NotFound(ApiResponse.Fail("Product not found"));
            
        return Ok(ApiResponse.Ok("Stock updated successfully"));
    }
}
