using ToyStore.Application.DTOs.Wishlists;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Services;

public interface IWishlistService
{
    Task<Result<List<WishlistItemDto>>> GetMyWishlistAsync(CancellationToken cancellationToken = default);

    Task<Result> AddItemAsync(AddToWishlistDto dto, CancellationToken cancellationToken = default);

    Task<Result> RemoveItemAsync(int productId, CancellationToken cancellationToken = default);
}
