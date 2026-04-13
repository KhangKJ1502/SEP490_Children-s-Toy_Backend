using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs;
using ToyStore.Domain.Enums;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Service interface using Result pattern for error handling.
/// This is an alternative to throwing exceptions.
/// </summary>
public interface IProductServiceV2
{
    /// <summary>
    /// Gets a product by ID.
    /// Returns Result.NotFound if product doesn't exist.
    /// </summary>
    Task<Result<ProductDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new product.
    /// Returns Result.ValidationFailure if input is invalid.
    /// Returns Result.Conflict if SKU already exists.
    /// </summary>
    Task<Result<ProductDto>> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing product.
    /// Returns Result.NotFound if product doesn't exist.
    /// Returns Result.ValidationFailure if input is invalid.
    /// </summary>
    Task<Result<ProductDto>> UpdateAsync(Guid id, UpdateProductDto dto, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a product.
    /// Returns Result.NotFound if product doesn't exist.
    /// Returns Result.BusinessError if product cannot be deleted.
    /// </summary>
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
