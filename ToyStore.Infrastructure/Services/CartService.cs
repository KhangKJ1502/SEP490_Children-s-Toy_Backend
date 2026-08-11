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

/// <summary>
/// Dịch vụ xử lý các nghiệp vụ liên quan đến Giỏ hàng (Cart) của khách hàng.
/// </summary>
public class CartService : ICartService
{
    private const decimal MaxCartSubTotal = 100_000_000m;
    private const string MaxCartSubTotalExceededMessage = "Cart total cannot exceed 100,000,000 VND.";

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICartRealtimeService _cartRealtimeService;
    private readonly IValidator<AddToCartDto> _addToCartValidator;
    private readonly IValidator<UpdateCartItemQuantityDto> _updateQuantityValidator;
    private readonly IMapper _mapper;
    private readonly ILogger<CartService> _logger;
    private readonly ITimeProvider _timeProvider;

    /// <summary>
    /// Khởi tạo CartService với các dịch vụ UnitOfWork, User, SignalR Realtime và Validators.
    /// </summary>
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

    /// <summary>
    /// Thêm sản phẩm mới vào giỏ hàng hoặc tăng số lượng nếu sản phẩm đã tồn tại trong giỏ.
    /// </summary>
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
            return Result<CartDto>.BusinessError("Product is out of quantity.");
        }

        var cart = await EnsureCartAsync(accountId, cancellationToken);
        var existingItem = await _unitOfWork.Carts.GetItemByProductAsync(cart.CartId, dto.ProductId, cancellationToken);
        var cartWithItems = await _unitOfWork.Carts.GetByAccountIdWithItemsAsync(accountId, cancellationToken);
        var now = _timeProvider.UtcNow;
        int checkQty = existingItem != null && existingItem.RemovedAt == null 
            ? (int)(existingItem.Quantity + dto.Quantity) 
            : (int)dto.Quantity;
        var currentPrice = PriceHelper.ResolveCurrentPrice(product, now, checkQty);
        var currentSubTotal = CalculateCartSubTotal(cartWithItems);

        if (existingItem != null)
        {
            var currentQty = existingItem.RemovedAt == null ? existingItem.Quantity : (short)0;
            var mergedQty = (short)(currentQty + dto.Quantity);
            if (mergedQty > product.Quantity)
            {
                return Result<CartDto>.BusinessError(
                    $"Cart quantity has reached the maximum available quantity.");
            }

            var existingActiveItem = cartWithItems?.CartItems.FirstOrDefault(x => x.ProductId == dto.ProductId && x.RemovedAt == null);
            var previousLineTotal = existingActiveItem == null ? 0m : existingActiveItem.CurrentPrice * existingActiveItem.Quantity;
            var projectedSubTotal = currentSubTotal - previousLineTotal + (currentPrice * mergedQty);
            if (IsCartSubTotalExceeded(projectedSubTotal))
            {
                return Result<CartDto>.BusinessError(MaxCartSubTotalExceededMessage);
            }

            existingItem.Quantity = mergedQty;
            existingItem.RemovedAt = null;
            existingItem.UpdatedAt = now;
            existingItem.CurrentPrice = currentPrice;
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
                    $"Cart quantity has reached the maximum available quantity.");
            }

            var projectedSubTotal = currentSubTotal + (currentPrice * dto.Quantity);
            if (IsCartSubTotalExceeded(projectedSubTotal))
            {
                return Result<CartDto>.BusinessError(MaxCartSubTotalExceededMessage);
            }

            var item = new CartItem
            {
                CartId = cart.CartId,
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                PriceAtThatTime = currentPrice,
                CurrentPrice = currentPrice,
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
            "Added to cart successfully.",
            new { dto.ProductId, dto.Quantity },
            cancellationToken);

        return Result<CartDto>.Success(snapshot);
    }

    /// <summary>
    /// Cập nhật số lượng của một sản phẩm cụ thể trong giỏ hàng.
    /// </summary>
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
            return Result<CartDto>.BusinessError("Product is out of quantity.");
        }

        if (dto.Quantity > item.Product.Quantity)
        {
            return Result<CartDto>.BusinessError(
                $"Cart quantity has reached the maximum available quantity.");
        }

        var cartWithItems = await _unitOfWork.Carts.GetByAccountIdWithItemsAsync(accountId, cancellationToken);
        var now = _timeProvider.UtcNow;
        var nextUnitPrice = PriceHelper.ResolveCurrentPrice(item.Product, now, (int)dto.Quantity);
        var currentSubTotal = CalculateCartSubTotal(cartWithItems);
        var previousLineTotal = item.CurrentPrice * item.Quantity;
        var projectedSubTotal = currentSubTotal - previousLineTotal + (nextUnitPrice * dto.Quantity);
        if (IsCartSubTotalExceeded(projectedSubTotal))
        {
            return Result<CartDto>.BusinessError(MaxCartSubTotalExceededMessage);
        }

        item.Quantity = dto.Quantity;
        item.CurrentPrice = nextUnitPrice;
        item.UpdatedAt = now;
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

    /// <summary>
    /// Xóa một sản phẩm ra khỏi giỏ hàng.
    /// </summary>
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

    /// <summary>
    /// Lấy thông tin chi tiết giỏ hàng hiện tại của khách hàng đăng nhập.
    /// </summary>
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

    /// <summary>
    /// Thông báo (Realtime) tới các giỏ hàng chứa sản phẩm bị thay đổi thông tin (giá, trạng thái, tồn kho,...).
    /// </summary>
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

    /// <summary>
    /// Thông báo (Realtime) tới các giỏ hàng bị ảnh hưởng khi chương trình khuyến mãi của sản phẩm thay đổi.
    /// </summary>
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

    /// <summary>
    /// Đảm bảo luôn tồn tại một bản ghi giỏ hàng (Cart) trong database cho tài khoản. Nếu chưa có sẽ tự động khởi tạo.
    /// </summary>
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

    /// <summary>
    /// Tính toán và đồng bộ lại trạng thái giỏ hàng thực tế của khách hàng:
    /// 1. Ẩn/xóa các sản phẩm đã bị xóa khỏi hệ thống (IsDeleted).
    /// 2. Điều chỉnh số lượng sản phẩm trong giỏ hàng theo tồn kho thực tế nếu số lượng yêu cầu vượt quá tồn kho hiện tại (chỉ điều chỉnh khi kho còn sản phẩm > 0).
    /// 3. Cập nhật giá bán mới nhất (bao gồm giá sỉ, giá khuyến mãi hoặc Flash Sale) tại thời điểm hiện tại.
    /// 4. Kiểm tra giới hạn số lượng Flash Sale còn lại để hiển thị cảnh báo Warning cho khách hàng.
    /// 5. Lưu trữ các thay đổi cập nhật vào database và gửi thông báo hết hàng (realtime) qua SignalR nếu có sản phẩm bị xóa khỏi giỏ.
    /// </summary>
    private async Task<CartDto> BuildCartSnapshotAsync(int accountId, CancellationToken cancellationToken)
    {
        // Đảm bảo luôn có bản ghi giỏ hàng trong database cho người dùng trước khi truy xuất
        await EnsureCartAsync(accountId, cancellationToken);
        var cart = await _unitOfWork.Carts.GetByAccountIdWithItemsAsync(accountId, cancellationToken);
        if (cart == null)
        {
            return new CartDto();
        }

        var now = _timeProvider.UtcNow;
        var modified = false;
        var removedAny = false;

        // VÒNG LẶP 1: Duyệt qua từng sản phẩm trong giỏ hàng để đồng bộ giá bán, tồn kho và trạng thái ẩn/xóa thực tế
        foreach (var item in cart.CartItems.ToList())
        {
            var product = item.Product;
            
            // KIỂM TRA 1: Nếu sản phẩm đã bị xóa khỏi hệ thống (IsDeleted = true)
            if (product.IsDeleted)
            {
                // Đánh dấu thời gian sản phẩm bị loại bỏ khỏi giỏ hàng
                item.RemovedAt = now;
                item.UpdatedAt = now;
                _unitOfWork.Carts.UpdateItem(item);
                modified = true;
                removedAny = true;
                continue; // Chuyển sang sản phẩm kế tiếp, bỏ qua các bước đồng bộ bên dưới
            }

            // KIỂM TRA 2: Xác định xem trạng thái sản phẩm có thuộc nhóm "Chỉ đọc" (ngưng bán, hết hàng, không hoạt động) hay không
            var isReadOnlyStatus = IsCartItemReadOnlyStatus(product.ProductStatus);
            
            // Nếu sản phẩm hoạt động bình thường nhưng số lượng yêu cầu trong giỏ lớn hơn tồn kho thực tế
            if (!isReadOnlyStatus && item.Quantity > product.Quantity)
            {
                // Chỉ tự động giảm số lượng trong giỏ xuống bằng tồn kho thực tế nếu trong kho vẫn còn hàng (> 0)
                // (Nếu tồn kho bằng 0, hệ thống không cập nhật số lượng về 0 để tránh vi phạm DB CHECK constraint (Quantity >= 1))
                if (product.Quantity > 0)
                {
                    item.Quantity = (short)product.Quantity;
                    item.UpdatedAt = now;
                    _unitOfWork.Carts.UpdateItem(item);
                    modified = true;
                }
            }

            // KIỂM TRA 3: Tính toán giá mới nhất của sản phẩm ở thời điểm hiện tại
            var latestPrice = isReadOnlyStatus
                ? product.Price // Nếu trạng thái chỉ đọc, giữ nguyên giá gốc của sản phẩm
                : PriceHelper.ResolveCurrentPrice(product, now, (int)item.Quantity); // Tính giá thực tế (áp dụng sỉ/lẻ, Flash Sale)
            
            // Nếu giá cũ trong giỏ hàng khác biệt so với giá mới nhất vừa tính toán
            if (item.CurrentPrice != latestPrice)
            {
                item.CurrentPrice = latestPrice; // Đồng bộ lại giá bán mới nhất vào giỏ
                item.UpdatedAt = now;
                _unitOfWork.Carts.UpdateItem(item);
                modified = true;
            }
        }

        // KIỂM TRA 4: Nếu giỏ hàng có bất kỳ thay đổi nào (về giá, số lượng, hoặc ẩn sản phẩm) thì lưu vào DB
        if (modified)
        {
            cart.UpdatedAt = now;
            _unitOfWork.Carts.UpdateCart(cart);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            // Tải lại giỏ hàng mới nhất từ database sau khi đã đồng bộ dữ liệu thành công
            cart = await _unitOfWork.Carts.GetByAccountIdWithItemsAsync(accountId, cancellationToken);
        }

        // Ánh xạ (Map) dữ liệu giỏ hàng sang đối tượng DTO để trả về cho Client
        var dto = _mapper.Map<CartDto>(cart!);
        // Sắp xếp các vật phẩm trong giỏ theo thứ tự thêm vào giảm dần (mới nhất hiển thị trên cùng)
        dto.Items = dto.Items
            .OrderByDescending(x => x.AddedAt)
            .ThenByDescending(x => x.CartItemId)
            .ToList();

        // VÒNG LẶP 2: Kiểm tra giới hạn số lượng Flash Sale cho từng sản phẩm trong giỏ hàng của khách hàng
        foreach (var itemDto in dto.Items)
        {
            var cartItem = cart.CartItems.First(ci => ci.CartItemId == itemDto.CartItemId);
            var product = cartItem.Product;
            // Lấy đợt Flash Sale đang diễn ra của sản phẩm (nếu có)
            var activeFlashSale = PriceHelper.GetActiveFlashSaleSlot(product, now, 1);
            
            // KIỂM TRA 5: Nếu sản phẩm đang nằm trong chương trình Flash Sale đang hoạt động
            if (activeFlashSale != null)
            {
                // Tính toán số lượng sản phẩm Flash Sale còn lại có thể mua trong đợt này
                int remaining = activeFlashSale.SaleQuantity - (activeFlashSale.SoldQuantity + activeFlashSale.ReservedQuantity);
                
                // Nếu số lượng người dùng yêu cầu lớn hơn giới hạn Flash Sale còn lại
                if (itemDto.Quantity > remaining)
                {
                    // Gán cảnh báo rằng giá bán thông thường sẽ được áp dụng cho toàn bộ số lượng này
                    itemDto.WarningMessage = $"The requested quantity ({itemDto.Quantity}) exceeds the remaining Flash Sale limit of {remaining} items. Standard pricing has been applied to all items.";
                }
            }
        }
        
        // Tính toán các tổng số liệu giỏ hàng để hiển thị lên UI Client
        dto.TotalItem = dto.Items.Count;
        dto.TotalQuantity = dto.Items.Sum(x => x.Quantity);
        dto.SubTotal = dto.Items.Sum(x => x.LineTotal);

        // KIỂM TRA 6: Nếu có sản phẩm bị tự động xóa khỏi giỏ (do bị quản trị viên xóa khỏi hệ thống)
        if (removedAny)
        {
            // Bắn sự kiện thời gian thực (Realtime) qua SignalR Hub báo cho giao diện Client tự động cập nhật ngay lập tức
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

    /// <summary>
    /// Gửi thông điệp cập nhật giỏ hàng thời gian thực qua SignalR Service.
    /// </summary>
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

    private static decimal CalculateCartSubTotal(Cart? cart)
    {
        if (cart == null)
        {
            return 0m;
        }

        return cart.CartItems
            .Where(x => x.RemovedAt == null)
            .Sum(x => x.CurrentPrice * x.Quantity);
    }

    private static bool IsCartSubTotalExceeded(decimal subtotal) => subtotal > MaxCartSubTotal;
}
