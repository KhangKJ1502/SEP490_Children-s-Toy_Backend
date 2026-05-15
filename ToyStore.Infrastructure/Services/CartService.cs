using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Common.Helpers;
using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs.Carts;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Services;

public class CartService : ICartService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICartRealtimeService _cartRealtimeService;
    private readonly IValidator<AddToCartDto> _addToCartValidator;
    private readonly IValidator<UpdateCartItemQuantityDto> _updateQuantityValidator;
    private readonly IMapper _mapper;
    private readonly ILogger<CartService> _logger;
    private readonly ITimeProvider _timeProvider;

    public CartService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ICartRealtimeService cartRealtimeService,
        IValidator<AddToCartDto> addToCartValidator,
        IValidator<UpdateCartItemQuantityDto> updateQuantityValidator,
        IMapper mapper,
        ILogger<CartService> logger,
        ITimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _cartRealtimeService = cartRealtimeService;
        _addToCartValidator = addToCartValidator;
        _updateQuantityValidator = updateQuantityValidator;
        _mapper = mapper;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<Result<CartDto>> AddItemAsync(AddToCartDto dto, CancellationToken cancellationToken = default)
    {
        var validationResult = await _addToCartValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToResult<CartDto>();
        }

        var accountId = _currentUserService.AccountId;
        if (accountId <= 0)
        {
            return Result<CartDto>.Unauthorized("Please login to manage cart.");
        }

        var product = await _unitOfWork.Products.GetByIdAsync(dto.ProductId, cancellationToken);
        if (product == null || product.IsDeleted)
        {
            return Result<CartDto>.NotFound("Product", dto.ProductId);
        }

        if (!string.Equals(product.ProductStatus, "Active", StringComparison.OrdinalIgnoreCase))
        {
            return Result<CartDto>.BusinessError("Product is inactive and cannot be added to cart.");
        }

        if (product.Quantity <= 0)
        {
            return Result<CartDto>.BusinessError("Product is out of stock.");
        }

        var cart = await EnsureCartAsync(accountId, cancellationToken);
        var existingItem = await _unitOfWork.Carts.GetItemByProductAsync(cart.CartId, dto.ProductId, cancellationToken);
        var now = _timeProvider.UtcNow;
        var currentPrice = PriceHelper.ResolveCurrentPrice(product, now);

        if (existingItem != null)
        {
            var currentQty = existingItem.RemovedAt == null ? existingItem.Quantity : (short)0;
            var mergedQty = (short)(currentQty + dto.Quantity);
            if (mergedQty > product.Quantity)
            {
                return Result<CartDto>.BusinessError(
                    $"Cart quantity has reached the maximum available stock ({product.Quantity}).");
            }

            existingItem.Quantity = mergedQty;
            existingItem.RemovedAt = null;
            existingItem.UpdatedAt = now;
            existingItem.CurrentPrice = currentPrice;
            existingItem.IsSelected = true;
            if (existingItem.PriceAtThatTime <= 0)
            {
                existingItem.PriceAtThatTime = currentPrice;
            }
            _unitOfWork.Carts.UpdateItem(existingItem);
        }
        else
        {
            if (dto.Quantity > product.Quantity)
            {
                return Result<CartDto>.BusinessError(
                    $"Cart quantity has reached the maximum available stock ({product.Quantity}).");
            }

            var item = new CartItem
            {
                CartId = cart.CartId,
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                PriceAtThatTime = currentPrice,
                CurrentPrice = currentPrice,
                IsSelected = true,
                AddedAt = now
            };
            _unitOfWork.Carts.AddItem(item);
        }

        cart.UpdatedAt = now;
        _unitOfWork.Carts.UpdateCart(cart);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var snapshot = await BuildCartSnapshotAsync(accountId, cancellationToken);
        await PublishCartEventAsync(
            accountId,
            CartHubEvents.CartItemAdded,
            snapshot,
            "Item added to cart.",
            new { dto.ProductId, dto.Quantity },
            cancellationToken);

        return Result<CartDto>.Success(snapshot);
    }

    public async Task<Result<CartDto>> UpdateItemQuantityAsync(
        int cartItemId,
        UpdateCartItemQuantityDto dto,
        CancellationToken cancellationToken = default)
    {
        if (cartItemId <= 0)
        {
            return Result<CartDto>.Failure("VALIDATION_ERROR", "Cart item ID must be greater than 0.");
        }

        var validationResult = await _updateQuantityValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToResult<CartDto>();
        }

        var accountId = _currentUserService.AccountId;
        if (accountId <= 0)
        {
            return Result<CartDto>.Unauthorized("Please login to manage cart.");
        }

        var item = await _unitOfWork.Carts.GetItemByIdForAccountAsync(accountId, cartItemId, cancellationToken);
        if (item == null)
        {
            return Result<CartDto>.NotFound("Cart item", cartItemId);
        }

        if (item.Product.IsDeleted || !string.Equals(item.Product.ProductStatus, "Active", StringComparison.OrdinalIgnoreCase))
        {
            return Result<CartDto>.BusinessError("Product is inactive and cannot be updated in cart.");
        }

        if (item.Product.Quantity <= 0)
        {
            return Result<CartDto>.BusinessError("Product is out of stock.");
        }

        if (dto.Quantity > item.Product.Quantity)
        {
            return Result<CartDto>.BusinessError(
                $"Cart quantity has reached the maximum available stock ({item.Product.Quantity}).");
        }

        item.Quantity = dto.Quantity;
        item.CurrentPrice = PriceHelper.ResolveCurrentPrice(item.Product, _timeProvider.UtcNow);
        item.UpdatedAt = _timeProvider.UtcNow;
        _unitOfWork.Carts.UpdateItem(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var snapshot = await BuildCartSnapshotAsync(accountId, cancellationToken);
        await PublishCartEventAsync(
            accountId,
            CartHubEvents.CartQuantityChanged,
            snapshot,
            "Cart item quantity updated.",
            new { cartItemId, quantity = dto.Quantity },
            cancellationToken);

        return Result<CartDto>.Success(snapshot);
    }

    public async Task<Result<CartDto>> RemoveItemAsync(int cartItemId, CancellationToken cancellationToken = default)
    {
        if (cartItemId <= 0)
        {
            return Result<CartDto>.Failure("VALIDATION_ERROR", "Cart item ID must be greater than 0.");
        }

        var accountId = _currentUserService.AccountId;
        if (accountId <= 0)
        {
            return Result<CartDto>.Unauthorized("Please login to manage cart.");
        }

        var item = await _unitOfWork.Carts.GetItemByIdForAccountAsync(accountId, cartItemId, cancellationToken);
        if (item == null)
        {
            return Result<CartDto>.NotFound("Cart item", cartItemId);
        }

        var productId = item.ProductId;
        _unitOfWork.Carts.RemoveItem(item);

        var cart = await _unitOfWork.Carts.GetByAccountIdAsync(accountId, cancellationToken);
        if (cart != null)
        {
            cart.UpdatedAt = _timeProvider.UtcNow;
            _unitOfWork.Carts.UpdateCart(cart);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var snapshot = await BuildCartSnapshotAsync(accountId, cancellationToken);
        await PublishCartEventAsync(
            accountId,
            CartHubEvents.CartItemRemoved,
            snapshot,
            "Item removed from cart.",
            new { cartItemId, productId },
            cancellationToken);

        return Result<CartDto>.Success(snapshot);
    }

    public async Task<Result<CartDto>> ClearCartAsync(CancellationToken cancellationToken = default)
    {
        var accountId = _currentUserService.AccountId;
        if (accountId <= 0)
        {
            return Result<CartDto>.Unauthorized("Please login to manage cart.");
        }

        var cart = await EnsureCartAsync(accountId, cancellationToken);
        var cartWithItems = await _unitOfWork.Carts.GetByAccountIdWithItemsAsync(accountId, cancellationToken);

        if (cartWithItems != null)
        {
            var now = DateTime.UtcNow;
            _unitOfWork.Carts.RemoveItems(cartWithItems.CartItems);

            cart.UpdatedAt = now;
            _unitOfWork.Carts.UpdateCart(cart);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var snapshot = await BuildCartSnapshotAsync(accountId, cancellationToken);

        await PublishCartEventAsync(
            accountId,
            CartHubEvents.CartUpdated,
            snapshot,
            "Cart has been cleared.",
            null,
            cancellationToken);

        return Result<CartDto>.Success(snapshot);
    }

    public async Task<Result<CartDto>> GetMyCartAsync(CancellationToken cancellationToken = default)
    {
        var accountId = _currentUserService.AccountId;
        if (accountId <= 0)
        {
            return Result<CartDto>.Unauthorized("Please login to manage cart.");
        }

        var snapshot = await BuildCartSnapshotAsync(accountId, cancellationToken);
        return Result<CartDto>.Success(snapshot);
    }

    public async Task NotifyProductChangedAsync(int productId, CancellationToken cancellationToken = default)
    {
        if (productId <= 0)
        {
            return;
        }

        var accountIds = await _unitOfWork.Carts.GetAccountIdsByProductIdAsync(productId, cancellationToken);
        _logger.LogInformation(
            "NotifyProductChangedAsync triggered for ProductId={ProductId}. ImpactedAccounts={AccountCount}",
            productId,
            accountIds.Count);

        foreach (var accountId in accountIds)
        {
            var snapshot = await BuildCartSnapshotAsync(accountId, cancellationToken);
            await PublishCartEventAsync(
                accountId,
                CartHubEvents.CartPriceChanged,
                snapshot,
                "Cart updated because product information changed.",
                new { productId },
                cancellationToken);
        }
    }

    public async Task NotifyPromotionChangedAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
        {
            return;
        }

        var accountIds = await _unitOfWork.Carts.GetAccountIdsByProductIdsAsync(productIds, cancellationToken);
        foreach (var accountId in accountIds)
        {
            var snapshot = await BuildCartSnapshotAsync(accountId, cancellationToken);
            await PublishCartEventAsync(
                accountId,
                CartHubEvents.CartPromotionChanged,
                snapshot,
                "Cart updated because promotion changed.",
                new { productIds },
                cancellationToken);
        }
    }

    private async Task<Cart> EnsureCartAsync(int accountId, CancellationToken cancellationToken)
    {
        var existingCart = await _unitOfWork.Carts.GetByAccountIdAsync(accountId, cancellationToken);
        if (existingCart != null)
        {
            return existingCart;
        }

        var createdCart = await _unitOfWork.Carts.CreateAsync(accountId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return createdCart;
    }

    private async Task<CartDto> BuildCartSnapshotAsync(int accountId, CancellationToken cancellationToken)
    {
        await EnsureCartAsync(accountId, cancellationToken);
        var cart = await _unitOfWork.Carts.GetByAccountIdWithItemsAsync(accountId, cancellationToken);
        if (cart == null)
        {
            return new CartDto();
        }

        var now = _timeProvider.UtcNow;
        var modified = false;
        var removedAny = false;

        foreach (var item in cart.CartItems.ToList())
        {
            var product = item.Product;
            if (product.IsDeleted)
            {
                item.RemovedAt = now;
                item.UpdatedAt = now;
                _unitOfWork.Carts.UpdateItem(item);
                modified = true;
                removedAny = true;
                continue;
            }

            var isReadOnlyStatus = IsCartItemReadOnlyStatus(product.ProductStatus);
            if (!isReadOnlyStatus && item.Quantity > product.Quantity)
            {
                // Only adjust quantity if the product still has some stock (> 0).
                // If product.Quantity is 0, we don't update item.Quantity to 0 because:
                // 1. It violates the database CHECK constraint (Quantity >= 1).
                // 2. We want to keep the item in the cart (e.g. during SE_PAY pending payment) 
                //    so the user can see it's out of stock or reserved, rather than it just disappearing.
                if (product.Quantity > 0)
                {
                    item.Quantity = (short)product.Quantity;
                    item.UpdatedAt = now;
                    _unitOfWork.Carts.UpdateItem(item);
                    modified = true;
                }
            }

            var latestPrice = isReadOnlyStatus
                ? product.Price
                : PriceHelper.ResolveCurrentPrice(product, now);
            if (item.CurrentPrice != latestPrice)
            {
                item.CurrentPrice = latestPrice;
                item.UpdatedAt = now;
                _unitOfWork.Carts.UpdateItem(item);
                modified = true;
            }
        }

        if (modified)
        {
            cart.UpdatedAt = now;
            _unitOfWork.Carts.UpdateCart(cart);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            cart = await _unitOfWork.Carts.GetByAccountIdWithItemsAsync(accountId, cancellationToken);
        }

        var dto = _mapper.Map<CartDto>(cart!);
        dto.TotalItem = dto.Items.Count;
        dto.TotalQuantity = dto.Items.Sum(x => x.Quantity);
        dto.SubTotal = dto.Items.Sum(x => x.LineTotal);

        if (removedAny)
        {
            await _cartRealtimeService.PublishAsync(
                accountId,
                CartHubEvents.CartOutOfStock,
                dto,
                "Some out-of-stock or inactive items were removed from your cart.",
                new { hasRemovedItems = true },
                cancellationToken);
        }

        return dto;
    }

    private async Task PublishCartEventAsync(
        int accountId,
        string eventName,
        CartDto cart,
        string message,
        object? payload,
        CancellationToken cancellationToken)
    {
        await _cartRealtimeService.PublishAsync(accountId, eventName, cart, message, payload, cancellationToken);
        if (!string.Equals(eventName, CartHubEvents.CartUpdated, StringComparison.Ordinal))
        {
            await _cartRealtimeService.PublishAsync(accountId, CartHubEvents.CartUpdated, cart, message, payload, cancellationToken);
        }
    }

    private static bool IsCartItemReadOnlyStatus(string? productStatus)
    {
        if (string.IsNullOrWhiteSpace(productStatus))
        {
            return false;
        }

        var normalized = new string(productStatus.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        return normalized is "INACTIVE" or "OUTOFSTOCK" or "DISCONTINUED";
    }
}
