using Microsoft.EntityFrameworkCore;
using ToyStore.Application.DTOs.Wishlists;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Services;

public class WishlistService : IWishlistService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITimeProvider _timeProvider;

    public WishlistService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ITimeProvider timeProvider)
    {
        _unitOfWork         = unitOfWork;
        _currentUserService = currentUserService;
        _timeProvider       = timeProvider;
    }

    public async Task<Result<List<WishlistItemDto>>> GetMyWishlistAsync(CancellationToken cancellationToken = default)
    {
        var accountId = _currentUserService.AccountId;
        if (accountId <= 0)
        {
            return Result<List<WishlistItemDto>>.Unauthorized("Please login to view wishlist.");
        }

        var wishlists = await _unitOfWork.Wishlists.GetByAccountIdAsync(accountId, cancellationToken);
        var items = wishlists
            .Select(x => new WishlistItemDto
            {
                ProductId = x.ProductId,
                CreatedAt = x.CreatedAt
            })
            .ToList();

        return Result<List<WishlistItemDto>>.Success(items);
    }

    public async Task<Result> AddItemAsync(AddToWishlistDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.ProductId <= 0)
        {
            return Result.Failure("VALIDATION_ERROR", "Product ID must be greater than 0.");
        }

        var accountId = _currentUserService.AccountId;
        if (accountId <= 0)
        {
            return Result.Failure("UNAUTHORIZED", "Please login to add wishlist.");
        }

        var product = await _unitOfWork.Products.GetByIdAsync(dto.ProductId, cancellationToken);
        if (product == null || product.IsDeleted)
        {
            return Result.NotFound("Product", dto.ProductId);
        }

        var existing = await _unitOfWork.Wishlists.GetByAccountAndProductAsync(accountId, dto.ProductId, cancellationToken);
        if (existing != null)
        {
            return Result.Conflict("Product already exists in wishlist.");
        }

        await _unitOfWork.Wishlists.AddAsync(new Wishlist
        {
            AccountId = accountId,
            ProductId = dto.ProductId,
            CreatedAt = _timeProvider.UtcNow
        }, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException)
        {
            return Result.Conflict("Product already exists in wishlist.");
        }
    }

    public async Task<Result> RemoveItemAsync(int productId, CancellationToken cancellationToken = default)
    {
        if (productId <= 0)
        {
            return Result.Failure("VALIDATION_ERROR", "Product ID must be greater than 0.");
        }

        var accountId = _currentUserService.AccountId;
        if (accountId <= 0)
        {
            return Result.Failure("UNAUTHORIZED", "Please login to remove wishlist.");
        }

        var product = await _unitOfWork.Products.GetByIdAsync(productId, cancellationToken);
        if (product == null || product.IsDeleted)
        {
            return Result.NotFound("Product", productId);
        }

        var existing = await _unitOfWork.Wishlists.GetByAccountAndProductAsync(accountId, productId, cancellationToken);
        if (existing == null)
        {
            return Result.NotFound("Wishlist item", productId);
        }

        _unitOfWork.Wishlists.Remove(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
