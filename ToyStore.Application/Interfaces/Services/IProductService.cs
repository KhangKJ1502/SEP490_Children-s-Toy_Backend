using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Products;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Service quan ly Product.
/// </summary>
public interface IProductService
{
    /// <summary>
    /// Lay danh sach Product co phan trang.
    /// </summary>
    Task<Result<PaginatedResponse<ProductListDto>>> GetProductsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay chi tiet Product theo ID.
    /// </summary>
    Task<Result<ProductDto>> GetProductByIdAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tao moi Product.
    /// </summary>
    Task<Result<ProductDto>> CreateProductAsync(CreateProductDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cap nhat Product.
    /// </summary>
    Task<Result<ProductDto>> UpdateProductAsync(int productId, UpdateProductDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tim kiem Product.
    /// </summary>
    Task<Result<PaginatedResponse<ProductListDto>>> SearchProductsAsync(
        string searchTerm,
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        CancellationToken cancellationToken = default);
}
