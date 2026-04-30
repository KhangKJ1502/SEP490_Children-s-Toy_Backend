using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Brands;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BrandsController : ControllerBase
{
    private readonly IBrandService _brandService;
    private readonly ILogger<BrandsController> _logger;

    public BrandsController(IBrandService brandService, ILogger<BrandsController> logger)
    {
        _brandService = brandService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<BrandListDto>>> GetBrands(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false,
        [FromQuery] string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _brandService.GetBrandsAsync(
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            searchTerm,
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<ActionResult<BrandListDto>> CreateBrand(
        [FromBody] CreateBrandDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _brandService.CreateBrandAsync(dto, cancellationToken);
        if (result.IsSuccess)
        {
            _logger.LogInformation("Created brand {BrandId}", result.Data!.BrandId);
        }

        return result.ToCreatedResult($"api/brands/{result.Data?.BrandId}");
    }

    [HttpPut("{brandId:int}")]
    public async Task<ActionResult<BrandListDto>> UpdateBrand(
        [FromRoute] short brandId,
        [FromBody] UpdateBrandDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _brandService.UpdateBrandAsync(brandId, dto, cancellationToken);
        return result.ToActionResult();
    }
}
