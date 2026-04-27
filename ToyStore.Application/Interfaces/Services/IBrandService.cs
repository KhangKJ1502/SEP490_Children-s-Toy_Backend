using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Brands;

namespace ToyStore.Application.Interfaces.Services;

public interface IBrandService
{
    Task<Result<PaginatedResponse<BrandListDto>>> GetBrandsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    Task<Result<BrandListDto>> CreateBrandAsync(
        CreateBrandDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<BrandListDto>> UpdateBrandAsync(
        short brandId,
        UpdateBrandDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<PaginatedResponse<BrandListDto>>> SearchBrandsAsync(
        string searchTerm,
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        CancellationToken cancellationToken = default);
}
