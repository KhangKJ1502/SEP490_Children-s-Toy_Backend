using ToyStore.Application.DTOs.Carts;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Services;

public interface ICartService
{
    Task<Result<CartDto>> AddItemAsync(AddToCartDto dto, CancellationToken cancellationToken = default);

    Task<Result<CartDto>> UpdateItemQuantityAsync(
        int cartItemId,
        UpdateCartItemQuantityDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<CartDto>> RemoveItemAsync(int cartItemId, CancellationToken cancellationToken = default);

    Task<Result<CartDto>> ClearCartAsync(CancellationToken cancellationToken = default);

    Task<Result<CartDto>> GetMyCartAsync(CancellationToken cancellationToken = default);

    Task NotifyProductChangedAsync(int productId, CancellationToken cancellationToken = default);

    Task NotifyPromotionChangedAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken = default);
}
