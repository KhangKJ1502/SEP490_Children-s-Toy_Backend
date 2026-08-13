using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToyStore.Application.Common.Helpers;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Checkouts;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Options;

namespace ToyStore.Infrastructure.Services;

/// <summary>
/// Triển khai toàn bộ luồng checkout: preview → confirm (COD / SE_PAY / WALLET) → retry QR.
/// Tuân theo spec trong prompt_part1..4:
///   - HTTP call GHN nằm ngoài DB transaction.
///   - Atomic UPDATE trừ stock từng sản phẩm.
///   - Voucher UsedQuantity tăng khi tạo order (kể cả SE_PAY Pending).
///   - SE_PAY stock trừ tại webhook PAID (không trừ lúc tạo order) để tránh race condition.
/// </summary>
public class CheckoutService : ICheckoutService
{
    private readonly IUnitOfWork _uow;
    private readonly SEP490ToyStoreContext _db;
    private readonly IGhnClient _ghnClient;
    private readonly GhnOptions _ghnOpts;
    private readonly SePayOptions _sePayOpts;
    private readonly ShopAddressOptions _shopAddr;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ILogger<CheckoutService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly IOrderLifecycleService _orderLifecycle;

    private const string PayMethodCod = "SHIP_COD";
    private const string PayMethodSepay = "SE_PAY";
    private const string PayMethodWallet = "WALLET";
    private const decimal MaxCheckoutSubTotal = 100_000_000m;
    private const int CodRestrictionThreshold = 2;
    private const string StatusNormal = "NORMAL";
    private const string StatusCodRestricted = "COD_RESTRICTED";
    private const string StatusCodProbation = "COD_PROBATION";
    private const string StatusPendingReview = "PENDING_ADMIN_REVIEW";
    private const string StatusManuallyBlocked = "MANUALLY_BLOCKED";
    private const string StatusAppealApprovedStrict = "APPEAL_APPROVED_STRICT";
    private const string StatusPermanentBlocked = "PERMANENT_BLOCKED";
    private static readonly string[] DeliveryAbuseFailCodes =
    {
        "GHN-DFC1A2",
        "GHN-DFC1A7",
        "GHN-DCD1A5",
        "GHN-DCD0A8",
        "GHN-DCD1A1"
    };

    public CheckoutService(
        IUnitOfWork uow,
        SEP490ToyStoreContext db,
        IGhnClient ghnClient,
        IOptions<GhnOptions> ghnOpts,
        IOptions<SePayOptions> sePayOpts,
        IOptions<ShopAddressOptions> shopAddr,
        IDomainEventPublisher eventPublisher,
        ILogger<CheckoutService> logger,
        ITimeProvider timeProvider,
        IOrderLifecycleService orderLifecycle)
    {
        _uow = uow;
        _db = db;
        _ghnClient = ghnClient;
        _ghnOpts = ghnOpts.Value;
        _sePayOpts = sePayOpts.Value;
        _shopAddr = shopAddr.Value;
        _eventPublisher = eventPublisher;
        _logger = logger;
        _timeProvider = timeProvider;
        _orderLifecycle = orderLifecycle;
    }


    public async Task<Result<CheckoutPreviewResponseDto>> PreviewAsync(
        int accountId,
        int addressId,
        string paymentMethod,
        string? orderVoucherCode,
        string? shippingVoucherCode,
        IReadOnlyList<CheckoutConfirmItemDto>? itemsSubset,
        CancellationToken cancellationToken = default)
    {
        // BƯỚC 1: Lấy giỏ hàng của người dùng và kiểm tra tính hợp lệ (giỏ hàng có trống không)
        var cart = await _uow.Carts.GetByAccountIdWithItemsAsync(accountId, cancellationToken);
        if (cart is null || !cart.CartItems.Any(i => i.RemovedAt == null))
            return Result<CheckoutPreviewResponseDto>.BusinessError("Cart is empty.");

        // BƯỚC 2: Kiểm tra địa chỉ giao hàng của người dùng
        var address = await _uow.Addresses.GetActiveByIdAsync(addressId, cancellationToken);
        if (address is null)
            return Result<CheckoutPreviewResponseDto>.NotFound("Address", addressId);

        var activeAll = cart.CartItems.Where(i => i.RemovedAt == null).ToList();
        List<CartItem> activeItems;
        
        // BƯỚC 3: Nếu chỉ checkout một tập hợp con các mặt hàng trong giỏ (itemsSubset), 
        // tiến hành kiểm tra xem các mặt hàng đó có tồn tại trong giỏ hàng và đúng số lượng hiện tại không.
        if (itemsSubset is { Count: > 0 })
        {
            activeItems = [];
            foreach (var line in itemsSubset)
            {
                var ci = activeAll.FirstOrDefault(x => x.ProductId == line.ProductId);
                if (ci is null)
                    return Result<CheckoutPreviewResponseDto>.BusinessError(
                        $"Product #{line.ProductId} is not in the cart.");
                if (ci.Quantity != line.Quantity)
                    return Result<CheckoutPreviewResponseDto>.BusinessError(
                        "Cart quantities have changed, please refresh the page.");
                activeItems.Add(ci);
            }
        }
        else
            activeItems = activeAll;
        var itemErrors = new List<CheckoutPreviewItemErrorDto>();
        decimal subTotal = 0;

        foreach (var ci in activeItems)
        {
            var p = ci.Product;
            if (p.ProductStatus != "Active")
                itemErrors.Add(new() { ProductId = p.ProductId, ProductName = p.ProductName, Error = "Product is no longer available." });
            else if (p.Quantity < ci.Quantity)
                itemErrors.Add(new() { ProductId = p.ProductId, ProductName = p.ProductName, Error = $"Only {p.Quantity} items left." });
            else
            {
                var currentPrice = PriceHelper.ResolveCurrentPrice(p, _timeProvider.UtcNow, (int)ci.Quantity);
                subTotal += currentPrice * ci.Quantity;
            }
        }

        if (subTotal >= MaxCheckoutSubTotal)
        {
            return Result<CheckoutPreviewResponseDto>.BusinessError(
                "Cart total must be below 100,000,000 VND to proceed to checkout.");
        }

        // BƯỚC 4: Chuẩn hóa phương thức thanh toán và kiểm tra chính sách hạn chế COD (Delivery Abuse Policy)
        var normalizedPaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? PayMethodCod : paymentMethod;
        normalizedPaymentMethod = normalizedPaymentMethod.Trim().ToUpperInvariant();
        if (normalizedPaymentMethod == PayMethodCod
            && await IsCodRestrictedAsync(accountId, cancellationToken))
        {
            return Result<CheckoutPreviewResponseDto>.BusinessError(
                "Cash on Delivery is temporarily unavailable for your account due to repeated failed COD deliveries. Please choose QR bank transfer or wallet payment.");
        }

        // BƯỚC 5: Kiểm tra ràng buộc Voucher. Không cho phép dùng cùng một mã voucher cho cả đơn hàng và phí vận chuyển.
        if (!string.IsNullOrWhiteSpace(orderVoucherCode)
            && !string.IsNullOrWhiteSpace(shippingVoucherCode)
            && orderVoucherCode.Equals(shippingVoucherCode, StringComparison.OrdinalIgnoreCase))
        {
            return Result<CheckoutPreviewResponseDto>.BusinessError(
                "Cannot apply the same voucher code for both order and shipping.");
        }

        // BƯỚC 6: Lấy thông tin kích thước và trọng lượng thực tế của sản phẩm từ database
        var activeCartItems = activeItems
            .Where(ci => ci.Product.ProductStatus == "Active" && ci.Product.Quantity >= ci.Quantity)
            .ToList();

        var activeProductIds = activeCartItems.Select(ci => ci.ProductId).ToList();

        var productsWithDetails = await _uow.Products.GetProductsWithDetailsAndCategoriesAsync(activeProductIds, cancellationToken);
        var detailsMap = productsWithDetails.ToDictionary(p => p.ProductId);

        var shippingItems = new List<ShippingItem>();
        foreach (var ci in activeCartItems)
        {
            if (detailsMap.TryGetValue(ci.ProductId, out var product))
            {
                var currentPrice = PriceHelper.ResolveCurrentPrice(ci.Product, _timeProvider.UtcNow, (int)ci.Quantity);
                shippingItems.Add(new ShippingItem(
                    ci.ProductId,
                    ci.Product.ProductName,
                    product.Category?.CategoryName ?? "",
                    (int)ci.Quantity,
                    currentPrice,
                    product.ProductDetail?.WeightGram ?? 0,
                    product.ProductDetail?.LengthCm ?? 0,
                    product.ProductDetail?.WidthCm ?? 0,
                    product.ProductDetail?.HeightCm ?? 0
                ));
            }
        }

        // BƯỚC 7: Tính toán kích thước đóng gói tối ưu cho toàn bộ gói hàng (package) dựa trên các mặt hàng
        var package = GhnPackageCalculator.Calculate(
            shippingItems,
            _ghnOpts.DefaultItemWeight,
            _ghnOpts.DefaultLength,
            _ghnOpts.DefaultWidth,
            _ghnOpts.DefaultHeight);
        decimal shippingFee = 0;
        DateTime? estimatedDelivery = null;
        var feeReq = new FeeRequestDTO
        {
            FromDistrictId = _ghnOpts.FromDistrictId,
            FromWardCode = _ghnOpts.FromWardCode,
            ToDistrictId = address.DistrictId ?? 0,
            ToWardCode = address.WardCode ?? string.Empty,
            Weight = Math.Max(package.Weight, 1),
            Length = package.Length,
            Width = package.Width,
            Height = package.Height,
            InsuranceValue = 0m,
            ServiceTypeId = package.ServiceTypeId,
            CodValue = 0m,
            Items = package.Items
        };

        // Với đơn hàng COD, truyền subTotal làm CodValue để GHN áp dụng mức phí bảo hiểm hoặc giảm giá COD thích hợp.
        // Lưu ý: Cuộc gọi HTTP đến API GHN được thực hiện ngoài Database Transaction để tránh block DB connection.
        feeReq.CodValue = normalizedPaymentMethod == PayMethodCod ? subTotal : 0m;

        var feeResult = await _ghnClient.GetFeeAsync(feeReq, cancellationToken);
        if (feeResult.IsSuccess)
        {
            shippingFee = feeResult.Data!.Fee;
        }
        else
        {
            _logger.LogWarning("GHN fee calculation failed for district {DistrictId}: {Error}",
                address.DistrictId, feeResult.ErrorMessage);
        }

        var ldReq = new LeadtimeRequestDTO
        {
            FromDistrictId = _ghnOpts.FromDistrictId,
            FromWardCode = _ghnOpts.FromWardCode,
            ToDistrictId = address.DistrictId ?? 0,
            ToWardCode = address.WardCode ?? string.Empty,
            ServiceTypeId = package.ServiceTypeId
        };
        var ldResult = await _ghnClient.GetLeadtimeAsync(ldReq, cancellationToken);
        if (ldResult.IsSuccess) estimatedDelivery = ldResult.Data!.EstimatedDeliveryTime;

        // Tính voucher discount
        decimal orderDiscount = 0;
        decimal shippingDiscount = 0;

        Voucher? orderVoucher = null;
        if (!string.IsNullOrWhiteSpace(orderVoucherCode))
        {
            orderVoucher = await _uow.Vouchers.GetByCodeAsync(orderVoucherCode, cancellationToken);
            if (orderVoucher is null)
                return Result<CheckoutPreviewResponseDto>.BusinessError("Order voucher code does not exist.");

            string target = orderVoucher.DiscountTarget;
            if (!string.Equals(target, "ORDER_TOTAL", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(target, "FINAL_PRICE", StringComparison.OrdinalIgnoreCase))
            {
                return Result<CheckoutPreviewResponseDto>.BusinessError("Voucher is not applicable for this target.");
            }

            if (string.Equals(target, "FINAL_PRICE", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(shippingVoucherCode))
            {
                return Result<CheckoutPreviewResponseDto>.BusinessError("Cannot apply shipping voucher when a compensation voucher is active.");
            }

            decimal baseAmount = string.Equals(target, "FINAL_PRICE", StringComparison.OrdinalIgnoreCase)
                ? (subTotal + shippingFee)
                : subTotal;

            var voucherResult = await CalculateVoucherDiscountAsync(
                orderVoucherCode,
                accountId,
                subTotal,
                baseAmount,
                target,
                cancellationToken);
            if (!voucherResult.IsSuccess)
                return Result<CheckoutPreviewResponseDto>.BusinessError(voucherResult.ErrorMessage ?? "Invalid voucher.");
            orderDiscount = voucherResult.Data;
        }

        if (!string.IsNullOrWhiteSpace(shippingVoucherCode))
        {
            if (shippingFee <= 0)
                return Result<CheckoutPreviewResponseDto>.BusinessError("Shipping fee is not available for voucher application.");

            var voucherResult = await CalculateVoucherDiscountAsync(
                shippingVoucherCode,
                accountId,
                subTotal,
                shippingFee,
                "SHIPPING_FEE",
                cancellationToken);
            if (!voucherResult.IsSuccess)
                return Result<CheckoutPreviewResponseDto>.BusinessError(voucherResult.ErrorMessage ?? "Invalid voucher.");
            shippingDiscount = voucherResult.Data;
        }

        var totalBeforeDiscount = subTotal + shippingFee;
        var discountAmount = Math.Min(orderDiscount + shippingDiscount, totalBeforeDiscount);
        var totalAmount = Math.Max(totalBeforeDiscount - discountAmount, 0m);



        return Result<CheckoutPreviewResponseDto>.Success(new CheckoutPreviewResponseDto
        {
            SubTotal = subTotal,
            ShippingFee = shippingFee,
            DiscountAmount = discountAmount,
            OrderDiscountAmount = orderDiscount,
            ShippingDiscountAmount = shippingDiscount,
            TotalAmount = totalAmount,
            TotalWeightGrams = package.Weight,
            EstimatedDeliveryTime = estimatedDelivery,
            ItemErrors = itemErrors
        });
    }

    public async Task<Result<CheckoutPaymentOptionsDto>> GetPaymentOptionsAsync(
        int accountId,
        CancellationToken cancellationToken = default)
    {
        var policy = await GetDeliveryAbusePolicyAsync(accountId, cancellationToken);

        return Result<CheckoutPaymentOptionsDto>.Success(new CheckoutPaymentOptionsDto
        {
            IsCodRestricted = policy.IsCodRestricted,
            SuspiciousDeliveryFailOrderCount = policy.SuspiciousOrderCount,
            CodRestrictionReason = policy.IsCodRestricted
                ? "Cash on Delivery is temporarily unavailable for your account due to repeated failed COD deliveries. Please choose QR bank transfer or wallet payment."
                : null
        });
    }

    public async Task<Result<CheckoutConfirmResponseDto>> ConfirmAsync(
        int accountId,
        CheckoutConfirmRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // Validate cơ bản
        if (request.Items.Count == 0)
            return Result<CheckoutConfirmResponseDto>.BusinessError("Cart is empty.");

        // Guard: mỗi user chỉ được có 1 đơn SE_PAY PENDING tại một thời điểm.
        // Nếu đã có đơn pending → trả orderId hiện tại để FE redirect về QR thay vì tạo đơn mới.
        var payMethodNorm = (request.PaymentMethod ?? "").Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(payMethodNorm))
        {
            payMethodNorm = PayMethodCod;
        }

        if (payMethodNorm == PayMethodCod
            && await IsCodRestrictedAsync(accountId, cancellationToken))
        {
            return Result<CheckoutConfirmResponseDto>.BusinessError(
                "Cash on Delivery is temporarily unavailable for your account due to repeated failed COD deliveries. Please choose QR bank transfer or wallet payment.");
        }

        if (payMethodNorm == PayMethodSepay)
        {
            var existingPending = await _db.Orders
                .Include(o => o.OrderDetails)
                .Include(o => o.Status)
                .Where(o => o.AccountId == accountId
                         && o.PaymentMethod == "SE_PAY"
                         && o.PaymentStatus == "PENDING"
                         && o.CancelledAt == null
                         && !o.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingPending is not null)
            {
                // Kiểm tra xem đơn hàng PENDING hiện tại đã hết hạn thanh toán (TTL) chưa.
                var ttl = TimeSpan.FromMinutes(_sePayOpts.PaymentTtlMinutes);
                var isExpired = existingPending.CreatedAt + ttl < _timeProvider.UtcNow;

                if (isExpired)
                {
                    // Nếu đã hết hạn, tiến hành hủy đơn hàng cũ của hệ thống một cách tự động, 
                    // hoàn lại voucher cho người dùng để họ có thể tiến hành checkout đơn mới.
                    _logger.LogInformation("ConfirmAsync: Auto-cancelling expired SE_PAY order {OrderId} for account {AccountId}",
                        existingPending.OrderId, accountId);

                    var cancelResult = await _orderLifecycle.CancelOrderInternalAsync(
                        existingPending,
                        "SE_PAY payment timeout — auto-cancelled during new checkout attempt",
                        cancelledByAccountId: 0, // system
                        restoreCart: false,
                        restoreVoucher: true,
                        cancellationToken: cancellationToken);

                    if (!cancelResult.IsSuccess)
                    {
                        _logger.LogWarning("ConfirmAsync: Failed to auto-cancel expired SE_PAY order {OrderId}: {Err}",
                            existingPending.OrderId, cancelResult.ErrorMessage);

                        return Result<CheckoutConfirmResponseDto>.BusinessError(
                            $"You have an expired pending order #{existingPending.OrderCode} that could not be automatically cancelled: {cancelResult.ErrorMessage}");
                    }
                }
                else
                {
                    // Nếu đơn PENDING cũ vẫn còn hạn thanh toán, chặn không cho tạo đơn mới
                    // và trả về OrderId/OrderCode hiện tại để Frontend chuyển hướng sang trang thanh toán QR.
                    return Result<CheckoutConfirmResponseDto>.Success(new CheckoutConfirmResponseDto
                    {
                        OrderId = existingPending.OrderId,
                        OrderCode = existingPending.OrderCode,
                        PaymentMethod = "SE_PAY",
                        PaymentStatus = "PENDING",
                        HasExistingPendingOrder = true,
                    });
                }
            }
        }

        var address = await _uow.Addresses.GetActiveByIdAsync(request.AddressId, cancellationToken);
        if (address is null)
            return Result<CheckoutConfirmResponseDto>.NotFound("Address", request.AddressId);

        // Lấy ward/district info cho Orders
        var ward = await _uow.Addresses.GetWardByCodeAsync(address.WardCode ?? "", cancellationToken);
        var district = await _uow.Addresses.GetDistrictByIdAsync(address.DistrictId ?? 0, cancellationToken);
        var province = await _uow.Addresses.GetProvinceByIdAsync(address.ProvinceId ?? 0, cancellationToken);

        // Lấy StatusID cho Pending / Confirmed
        var statusMap = await _uow.Orders.GetStatusMapAsync(cancellationToken);
        if (!statusMap.TryGetValue("Pending", out var pendingStatusId))
            return Result<CheckoutConfirmResponseDto>.Failure("CONFIGURATION_ERROR", "Status 'Pending' does not exist in the database.");
        statusMap.TryGetValue("Confirmed", out var confirmedStatusId);

        // Lấy sản phẩm và validate sơ bộ
        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _uow.Products.GetProductsForCheckoutAsync(productIds, cancellationToken);

        var productMap = products.ToDictionary(p => p.ProductId);
        var softErrors = new List<string>();
        foreach (var item in request.Items)
        {
            if (!productMap.TryGetValue(item.ProductId, out var p))
            { softErrors.Add($"Product ID {item.ProductId} does not exist."); continue; }
            if (p.ProductStatus != "Active")
                softErrors.Add($"Product '{p.ProductName}' is no longer available.");
            else if (p.Quantity < item.Quantity)
                softErrors.Add($"Product '{p.ProductName}' only has {p.Quantity} items left.");
        }
        if (softErrors.Count > 0)
            return Result<CheckoutConfirmResponseDto>.BusinessError(string.Join("; ", softErrors));

        // Tính SubTotal từ Products.Price tại thời điểm checkout (có tính Flash Sale/Promotion)
        decimal subTotal = request.Items.Sum(i => PriceHelper.ResolveCurrentPrice(productMap[i.ProductId], _timeProvider.UtcNow, (int)i.Quantity) * (int)i.Quantity);

        if (subTotal >= MaxCheckoutSubTotal)
        {
            return Result<CheckoutConfirmResponseDto>.BusinessError(
                "Cart total must be below 100,000,000 VND to proceed to checkout.");
        }

        // BƯỚC C.2: Đối soát danh sách sản phẩm yêu cầu checkout với giỏ hàng thực tế trong database.
        // Biện pháp bảo mật này giúp ngăn chặn kẻ xấu thao túng request checkout để mua mặt hàng không có trong giỏ 
        // hoặc đặt mua số lượng vượt quá số lượng hợp lệ trong giỏ hàng.
        var activeCart = await _uow.Carts.GetByAccountIdWithItemsAsync(accountId, cancellationToken);
        if (activeCart is not null)
        {
            var cartQtyByProductId = activeCart.CartItems
                .Where(ci => ci.RemovedAt == null)
                .GroupBy(ci => ci.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(ci => (int)ci.Quantity));

            var requestedQtyByProductId = request.Items
                .GroupBy(i => i.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(i => (int)i.Quantity));

            var missingProducts = requestedQtyByProductId.Keys
                .Where(productId => !cartQtyByProductId.ContainsKey(productId))
                .Select(productId => $"ProductId {productId}")
                .ToList();
            if (missingProducts.Count > 0)
            {
                return Result<CheckoutConfirmResponseDto>.BusinessError(
                    $"Products not found in cart: {string.Join(", ", missingProducts)}.");
            }

            var exceededItems = requestedQtyByProductId
                .Where(kvp => kvp.Value > cartQtyByProductId[kvp.Key])
                .Select(kvp => $"ProductId {kvp.Key} (requested {kvp.Value}, in cart {cartQtyByProductId[kvp.Key]})")
                .ToList();
            if (exceededItems.Count > 0)
            {
                return Result<CheckoutConfirmResponseDto>.BusinessError(
                    $"Checkout quantity exceeds cart quantity: {string.Join(", ", exceededItems)}.");
            }
        }

        // Lấy chi tiết cân nặng, chiều dài, rộng, cao thực tế của sản phẩm cho Confirm
        var confirmProductIds = request.Items.Select(i => i.ProductId).Distinct().ToList();

        var productsWithDetails = await _uow.Products.GetProductsWithDetailsAndCategoriesAsync(confirmProductIds, cancellationToken);
        var detailsMap = productsWithDetails.ToDictionary(p => p.ProductId);

        var shippingItems = new List<ShippingItem>();
        foreach (var item in request.Items)
        {
            if (detailsMap.TryGetValue(item.ProductId, out var product))
            {
                var p = productMap[item.ProductId];
                var currentPrice = PriceHelper.ResolveCurrentPrice(p, _timeProvider.UtcNow, (int)item.Quantity);
                shippingItems.Add(new ShippingItem(
                    item.ProductId,
                    p.ProductName,
                    product.Category?.CategoryName ?? "",
                    (int)item.Quantity,
                    currentPrice,
                    product.ProductDetail?.WeightGram ?? 0,
                    product.ProductDetail?.LengthCm ?? 0,
                    product.ProductDetail?.WidthCm ?? 0,
                    product.ProductDetail?.HeightCm ?? 0
                ));
            }
        }

        var package = GhnPackageCalculator.Calculate(
            shippingItems,
            _ghnOpts.DefaultItemWeight,
            _ghnOpts.DefaultLength,
            _ghnOpts.DefaultWidth,
            _ghnOpts.DefaultHeight);

        // Lấy phí ship thật
        var feeReq = new FeeRequestDTO
        {
            FromDistrictId = _ghnOpts.FromDistrictId,
            FromWardCode = _ghnOpts.FromWardCode,
            ToDistrictId = address.DistrictId ?? 0,
            ToWardCode = address.WardCode ?? string.Empty,
            Weight = Math.Max(package.Weight, 1),
            Length = package.Length,
            Width = package.Width,
            Height = package.Height,
            InsuranceValue = 0m,
            ServiceTypeId = package.ServiceTypeId,
            CodValue = 0m,
            Items = package.Items
        };

        // For COD orders pass subTotal so GHN applies the discounted COD shipping rate
        feeReq.CodValue = payMethodNorm == PayMethodCod ? subTotal : 0m;
        var feeResult = await _ghnClient.GetFeeAsync(feeReq, cancellationToken);
        if (!feeResult.IsSuccess)
            return Result<CheckoutConfirmResponseDto>.BusinessError("Could not calculate shipping fee. Please try again.");

        decimal shippingFee = feeResult.Data!.Fee;
        DateTime? estimatedDelivery = null;
        var ldReq = new LeadtimeRequestDTO
        {
            FromDistrictId = _ghnOpts.FromDistrictId,
            FromWardCode = _ghnOpts.FromWardCode,
            ToDistrictId = address.DistrictId ?? 0,
            ToWardCode = address.WardCode ?? string.Empty,
            ServiceTypeId = package.ServiceTypeId
        };
        var ldResult = await _ghnClient.GetLeadtimeAsync(ldReq, cancellationToken);
        if (ldResult.IsSuccess) estimatedDelivery = ldResult.Data!.EstimatedDeliveryTime;

        // Voucher: validate + tính discount (tách order/shipping)
        var orderVoucherCode = request.OrderVoucherCode ?? request.VoucherCode;
        var shippingVoucherCode = request.ShippingVoucherCode;

        if (!string.IsNullOrWhiteSpace(orderVoucherCode)
            && !string.IsNullOrWhiteSpace(shippingVoucherCode)
            && orderVoucherCode.Equals(shippingVoucherCode, StringComparison.OrdinalIgnoreCase))
        {
            return Result<CheckoutConfirmResponseDto>.BusinessError(
                "Cannot apply the same voucher code for both order and shipping.");
        }

        decimal orderDiscount = 0;
        decimal shippingDiscount = 0;
        Voucher? orderVoucher = null;
        Voucher? shippingVoucher = null;

        if (!string.IsNullOrWhiteSpace(orderVoucherCode))
        {
            orderVoucher = await _uow.Vouchers.GetByCodeAsync(orderVoucherCode, cancellationToken);
            if (orderVoucher is null)
                return Result<CheckoutConfirmResponseDto>.BusinessError("Voucher code does not exist.");

            string target = orderVoucher.DiscountTarget;
            if (!string.Equals(target, "ORDER_TOTAL", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(target, "FINAL_PRICE", StringComparison.OrdinalIgnoreCase))
            {
                return Result<CheckoutConfirmResponseDto>.BusinessError("Voucher is not applicable for this target.");
            }

            if (string.Equals(target, "FINAL_PRICE", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(shippingVoucherCode))
            {
                return Result<CheckoutConfirmResponseDto>.BusinessError("Cannot apply shipping voucher when a compensation voucher is active.");
            }

            var voucherCheck = await ValidateVoucherAsync(orderVoucher, accountId, subTotal, target, cancellationToken);
            if (!string.IsNullOrEmpty(voucherCheck))
                return Result<CheckoutConfirmResponseDto>.BusinessError(voucherCheck);

            decimal baseAmount = string.Equals(target, "FINAL_PRICE", StringComparison.OrdinalIgnoreCase)
                ? (subTotal + shippingFee)
                : subTotal;

            orderDiscount = Math.Min(CalculateDiscount(orderVoucher, baseAmount), baseAmount);
        }

        if (!string.IsNullOrWhiteSpace(shippingVoucherCode))
        {
            if (shippingFee <= 0)
                return Result<CheckoutConfirmResponseDto>.BusinessError("Shipping fee is not available for voucher application.");

            shippingVoucher = await _uow.Vouchers.GetByCodeAsync(shippingVoucherCode, cancellationToken);
            if (shippingVoucher is null)
                return Result<CheckoutConfirmResponseDto>.BusinessError("Voucher code does not exist.");

            var voucherCheck = await ValidateVoucherAsync(shippingVoucher, accountId, subTotal, "SHIPPING_FEE", cancellationToken);
            if (!string.IsNullOrEmpty(voucherCheck))
                return Result<CheckoutConfirmResponseDto>.BusinessError(voucherCheck);

            shippingDiscount = Math.Min(CalculateDiscount(shippingVoucher, shippingFee), shippingFee);
        }




        var discountAmount = orderDiscount + shippingDiscount;

        // TotalAmount = SubTotal + ShippingFee - Discount
        var totalAmount = Math.Max(subTotal + shippingFee - discountAmount, 0m);
        var orderCode = GenerateOrderCode();
        var now = _timeProvider.UtcNow;

        var payMethod = payMethodNorm;

        // ── KHỞI ĐẦU DATABASE TRANSACTION ──────────────────────────────────────────
        // Bắt đầu Transaction để đảm bảo tính nguyên tử (Atomicity): 
        // Hoặc tất cả ghi nhận đơn hàng, trừ kho, áp dụng voucher đều thành công, hoặc sẽ rollback toàn bộ.
        await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            var order = new Order
            {
                AccountId = accountId,
                StatusId = pendingStatusId,
                OrderCode = orderCode,
                ShippingName = address.RecipientName ?? address.Account?.AccountName ?? "Customer",
                ShippingPhone = address.PhoneNumber ?? string.Empty,
                ShippingAddress = address.AddressLine,
                ShippingWardCode = address.WardCode ?? string.Empty,
                ShippingWardName = ward?.WardName ?? string.Empty,
                ShippingDistrictId = address.DistrictId ?? 0,
                ShippingDistrictName = district?.DistrictName ?? string.Empty,
                ShippingProvinceId = address.ProvinceId ?? 0,
                ShippingProvinceName = province?.ProvinceName ?? string.Empty,
                PaymentMethod = payMethod,
                PaymentStatus = "PENDING",
                SubTotal = subTotal,
                VoucherDiscountAmount = discountAmount,
                EstimatedShippingFee = shippingFee,
                TotalAmount = totalAmount,
                Note = request.Note,
                OrderDate = now,
                CreatedAt = now
            };
            await _db.Orders.AddAsync(order, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken); // lấy OrderId


            // BƯỚC C.3: Tạo thông tin chi tiết đơn hàng (OrderDetails) và xử lý trừ kho (stock reservation)
            foreach (var item in request.Items)
            {
                var p = productMap[item.ProductId];
                var flashSaleSlot = PriceHelper.GetActiveFlashSaleSlot(p, now, (int)item.Quantity);

                await _uow.Orders.AddOrderDetailAsync(new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    ProductImage = p.ProductImage?.ImageUrl,
                    Quantity = item.Quantity,
                    UnitPrice = PriceHelper.ResolveCurrentPrice(p, now, (int)item.Quantity),
                    DiscountAmount = 0,
                    SlotProductId = flashSaleSlot?.SlotProductId,
                    CreatedAt = now
                }, cancellationToken);

                // QUY TẮC TRỪ KHO CHUNG (General Stock):
                // - COD và SE_PAY: Trừ kho chung ngay lúc tạo đơn hàng (đưa vào trạng thái tạm giữ/reserve) 
                //   để tránh tình trạng oversell khi nhiều người thanh toán cùng lúc.
                // - WALLET: Chỉ thực hiện trừ kho chung sau khi trừ tiền ví thành công (xem ở nhánh WALLET bên dưới).
                if (payMethod == PayMethodCod || payMethod == PayMethodSepay)
                {
                    var success = await _uow.Orders.UpdateProductQuantityAsync(item.ProductId, (int)item.Quantity, cancellationToken);
                    if (!success)
                    {
                        await _uow.RollbackTransactionAsync(cancellationToken);
                        return Result<CheckoutConfirmResponseDto>.Conflict(
                            $"Product '{p.ProductName}' is out of stock.");
                    }
                }

                // QUY TẮC TRỪ KHO FLASH SALE (Slot Promotion Product Stock):
                // Nếu sản phẩm nằm trong chương trình Flash Sale:
                // - SE_PAY: Tăng ReservedQuantity tạm giữ. Khi có Webhook PAID chính thức mới chuyển từ Reserved sang Sold.
                // - COD/Wallet: Tăng trực tiếp SoldQuantity do các giao dịch này hoàn thành hoặc được duyệt ngay.
                if (flashSaleSlot != null)
                {
                    string sql;
                    if (payMethod == PayMethodSepay)
                    {
                        // SE_PAY: Tăng ReservedQuantity tạm giữ để chống quá bán Flash Sale
                        sql = "UPDATE PromotionProductSlots SET ReservedQuantity = ReservedQuantity + {0} " +
                              "WHERE SlotProductID = {1} AND SoldQuantity + ReservedQuantity + {0} <= SaleQuantity";
                    }
                    else
                    {
                        // COD/Wallet: Tăng trực tiếp SoldQuantity
                        sql = "UPDATE PromotionProductSlots SET SoldQuantity = SoldQuantity + {0} " +
                              "WHERE SlotProductID = {1} AND SoldQuantity + ReservedQuantity + {0} <= SaleQuantity";
                    }

                    var flashAffected = await _db.Database.ExecuteSqlRawAsync(sql, new object[] { (int)item.Quantity, flashSaleSlot.SlotProductId }, cancellationToken);
                    if (flashAffected == 0)
                    {
                        await _uow.RollbackTransactionAsync(cancellationToken);
                        return Result<CheckoutConfirmResponseDto>.Conflict(
                            $"Product '{p.ProductName}' has reached its Flash Sale limit.");
                    }
                }
            }

            // Voucher: tăng UsedQuantity + insert VoucherUsageLogs
            if (orderVoucher is not null && orderDiscount > 0)
            {
                var err = await ApplyVoucherUsageAsync(orderVoucher, order.OrderId, accountId, orderDiscount, "ORDER_TOTAL", cancellationToken);
                if (!string.IsNullOrEmpty(err))
                {
                    await _uow.RollbackTransactionAsync(cancellationToken);
                    return Result<CheckoutConfirmResponseDto>.BusinessError(err);
                }
            }

            if (shippingVoucher is not null && shippingDiscount > 0)
            {
                var err = await ApplyVoucherUsageAsync(shippingVoucher, order.OrderId, accountId, shippingDiscount, "SHIPPING_FEE", cancellationToken);
                if (!string.IsNullOrEmpty(err))
                {
                    await _uow.RollbackTransactionAsync(cancellationToken);
                    return Result<CheckoutConfirmResponseDto>.BusinessError(err);
                }
            }

            // Insert OrderStatusHistory
            await _db.OrderStatusHistories.AddAsync(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                StatusId = pendingStatusId,
                ChangedBy = accountId,
                Note = "Order placed successfully",
                CreatedAt = now
            }, cancellationToken);

            // Xử lý CartItems đã checkout
            // SE_PAY: giữ cart đến khi webhook PAID (tránh giỏ trống khi user bỏ QR chưa thanh toán)
            if (payMethod != PayMethodSepay)
            {
                var cart = await _uow.Carts.GetByAccountIdWithItemsAsync(accountId, cancellationToken);
                if (cart is not null)
                {
                    ApplyPurchasedItemsToCart(cart, request.Items, now);
                }
            }

            // ── Nhánh thanh toán ──────────────────────────────────────────

            string? attemptCode = null;
            string? qrImageUrl = null;

            if (payMethod == PayMethodWallet)
            {
                // BƯỚC C.4.a: THANH TOÁN QUA VÍ ĐIỆN TỬ (WALLET METHOD)
                // 1. Kiểm tra sự tồn tại và trạng thái hoạt động của ví
                var wallet = await _uow.Wallets.GetByAccountIdWithActivePinAsync(accountId, cancellationToken);
                if (wallet is null || wallet.Status != "Active")
                {
                    await _uow.RollbackTransactionAsync(cancellationToken);
                    return Result<CheckoutConfirmResponseDto>.BusinessError("Wallet is not available.");
                }

                // 2. Yêu cầu ví phải được thiết lập mã PIN bảo mật trước khi thanh toán
                var hasActivePin = wallet.WalletPins.Any(p => p.IsActive);
                if (!hasActivePin)
                {
                    await _uow.RollbackTransactionAsync(cancellationToken);
                    return Result<CheckoutConfirmResponseDto>.BusinessError(
                        "Wallet is not activated. Please set up your PIN on the Wallet page.");
                }

                // 3. TRÁNH DOUBLE-SPENDING: Đối chiếu số dư khả dụng (Available Balance = Balance - LockedBalance).
                // Không được phép chi tiêu vào phần LockedBalance (đang bị tạm khóa cho lệnh rút tiền đang xử lý).
                // Việc trừ số dư ví phải diễn ra atomically thông qua thủ tục DB để tránh race condition.
                var walletSuccess = await _uow.Orders.DeductWalletBalanceAsync(accountId, totalAmount, cancellationToken);
                if (!walletSuccess)
                {
                    await _uow.RollbackTransactionAsync(cancellationToken);
                    return Result<CheckoutConfirmResponseDto>.BusinessError("Insufficient wallet balance.");
                }

                // 4. Ghi nhận lịch sử giao dịch ví (WalletTransaction)
                var walletAfterBalance = wallet.Balance - totalAmount;
                var walletTxn = new WalletTransaction
                {
                    WalletId = wallet.WalletId,
                    AccountId = accountId,
                    RelatedOrderId = order.OrderId,
                    TxnType = "Payment",
                    Direction = "DR", // Debit (Trừ tiền)
                    Amount = totalAmount,
                    BalanceBefore = wallet.Balance,
                    BalanceAfter = walletAfterBalance,
                    Method = "Internal",
                    Reason = $"Order payment #{orderCode}",
                    IdempotencyKey = $"PAY_{orderCode}",
                    Status = "Completed",
                    CreatedAt = now,
                    CompletedAt = now
                };
                await _db.WalletTransactions.AddAsync(walletTxn, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken); // Lấy ID giao dịch

                // 5. TRỪ STOCK CHUNG: Với WALLET, chỉ thực hiện trừ số lượng tồn kho sản phẩm sau khi ví đã trừ tiền thành công.
                foreach (var item in request.Items)
                {
                    var p = productMap[item.ProductId];
                    var success = await _uow.Orders.UpdateProductQuantityAsync(item.ProductId, (int)item.Quantity, cancellationToken);
                    if (!success)
                    {
                        await _uow.RollbackTransactionAsync(cancellationToken);
                        return Result<CheckoutConfirmResponseDto>.Conflict(
                            $"Product '{p.ProductName}' is out of stock.");
                    }
                }

                // 6. Cập nhật trạng thái đơn hàng thành PAID và CONFIRMED ngay lập tức
                order.PaymentStatus = "PAID";
                order.PaidAt = now;
                order.StatusId = confirmedStatusId;
                order.ConfirmedAt = now;

                // 7. Ghi nhận lịch sử thanh toán chung (PaymentHistory)
                await _db.PaymentHistories.AddAsync(new PaymentHistory
                {
                    AccountId = accountId,
                    OrderId = order.OrderId,
                    WalletTransactionId = walletTxn.WalletTransactionId,
                    PaymentStatus = "PAID",
                    PaymentMethod = "WALLET",
                    TransactionCode = $"PAY_{orderCode}",
                    Amount = totalAmount,
                    CreatedAt = now
                }, cancellationToken);

                // 8. Lưu vết thay đổi trạng thái đơn hàng (OrderStatusHistory)
                await _db.OrderStatusHistories.AddAsync(new OrderStatusHistory
                {
                    OrderId = order.OrderId,
                    StatusId = confirmedStatusId,
                    ChangedBy = null,
                    Note = "Auto-confirmed: Wallet payment",
                    CreatedAt = now
                }, cancellationToken);
            }
            else if (payMethod == PayMethodCod)
            {
                order.PaymentStatus = "COD_PENDING";
            }
            else // SE_PAY
            {
                // BƯỚC C.4.b: THANH TOÁN QUA CỔNG CHUYỂN KHOẢN NGÂN HÀNG (SE_PAY METHOD)
                // Sinh mã đối chiếu giao dịch (attemptCode) duy nhất.
                // Cơ chế thử lại tối đa 3 lần đề phòng trường hợp trùng lặp ngẫu nhiên mã khóa duy nhất (UNIQUE Constraint).
                for (var retryInsert = 0; retryInsert < 3; retryInsert++)
                {
                    attemptCode = BuildAttemptCode(orderCode);
                    try
                    {
                        await _db.PaymentGatewayTransactions.AddAsync(new PaymentGatewayTransaction
                        {
                            OrderId = order.OrderId,
                            Provider = "SE_PAY",
                            RequestId = attemptCode,
                            Amount = totalAmount,
                            Status = "Pending",
                            RetryCount = 0,
                            CreatedAt = now
                        }, cancellationToken);
                        await _db.SaveChangesAsync(cancellationToken);
                        break;
                    }
                    catch (DbUpdateException dex) when (IsUniqueViolation(dex))
                    {
                        _logger.LogWarning("PaymentGatewayTransaction UNIQUE violation for attemptCode {Code}, retrying", attemptCode);
                        // Detach các entity lỗi khỏi change tracker để tránh xung đột khi SaveChanges ở lượt tiếp theo
                        _db.ChangeTracker.Entries<PaymentGatewayTransaction>()
                            .Where(e => e.State == EntityState.Added)
                            .ToList()
                            .ForEach(e => e.State = EntityState.Detached);
                    }
                }
                // Tự động tạo link ảnh mã VietQR với nội dung chuyển khoản là mã attemptCode
                qrImageUrl = BuildVietQrUrl(attemptCode!, (long)totalAmount);
            }

            await _db.SaveChangesAsync(cancellationToken);
            await _uow.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Order {Code} created via {Method}, Status={PS}",
                orderCode, payMethod, order.PaymentStatus);

            if (payMethod != PayMethodSepay)
            {
                await _eventPublisher.PublishAsync("Order", order.OrderId.ToString(),
                    NotificationEventTypes.OrderPlaced,
                    new { orderId = order.OrderId, orderCode, totalAmount },
                    CancellationToken.None);
            }

            if (payMethod == PayMethodWallet)
            {
                var orderPayload = new { orderId = order.OrderId, orderCode };
                await _eventPublisher.PublishAsync("Order", order.OrderId.ToString(),
                    NotificationEventTypes.OrderConfirmed,
                    orderPayload,
                    CancellationToken.None);
                await _eventPublisher.PublishAsync("Order", order.OrderId.ToString(),
                    NotificationEventTypes.MerchReadyToPack,
                    orderPayload,
                    CancellationToken.None);
            }

            return Result<CheckoutConfirmResponseDto>.Success(new CheckoutConfirmResponseDto
            {
                OrderId = order.OrderId,
                OrderCode = orderCode,
                ShippingOrderCode = null,
                ShippingFee = shippingFee,
                EstimatedDeliveryTime = estimatedDelivery,
                TotalAmount = totalAmount,
                PaymentMethod = payMethod,
                PaymentStatus = order.PaymentStatus,
                PaymentAttemptCode = attemptCode,
                QrImageUrl = qrImageUrl
            });
        }
        catch
        {
            await _uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private async Task<Result<decimal>> CalculateVoucherDiscountAsync(
        string voucherCode,
        int accountId,
        decimal orderSubTotal,
        decimal baseAmount,
        string expectedTarget,
        CancellationToken ct)
    {
        var voucher = await _uow.Vouchers.GetByCodeAsync(voucherCode, ct);
        if (voucher is null) return Result<decimal>.NotFound("Voucher");
        var err = await ValidateVoucherAsync(voucher, accountId, orderSubTotal, expectedTarget, ct);
        if (!string.IsNullOrEmpty(err)) return Result<decimal>.BusinessError(err);
        if (baseAmount <= 0) return Result<decimal>.BusinessError("Invalid base amount for voucher application.");
        return Result<decimal>.Success(CalculateDiscount(voucher, baseAmount));
    }

    private async Task<string?> ValidateVoucherAsync(
        Voucher v,
        int accountId,
        decimal orderSubTotal,
        string expectedTarget,
        CancellationToken ct)
    {
        var now = _timeProvider.UtcNow;
        if (v.Status != "Active" || v.IsDeleted) return "Invalid voucher.";
        if (now < v.StartDate) return "Voucher is not yet active.";
        if (now > v.EndDate) return "Voucher has expired.";
        if (!v.DiscountTarget.Equals(expectedTarget, StringComparison.OrdinalIgnoreCase))
            return "Voucher is not applicable for this target.";
        if (v.MinOrderAmount.HasValue && orderSubTotal < v.MinOrderAmount.Value)
            return $"Minimum order of {v.MinOrderAmount:N0} VND required to use this voucher.";
        if (v.TotalQuantity.HasValue && v.UsedQuantity >= v.TotalQuantity.Value)
            return "Voucher usage limit reached.";
        if (v.MaxUsagePerUser.HasValue)
        {
            var usageCount = await _uow.Vouchers.CountUsageByAccountAsync(v.VoucherId, accountId, ct);
            if (usageCount >= v.MaxUsagePerUser.Value)
                return "You have already used this voucher the maximum number of times.";
        }
        return null;
    }

    private static decimal CalculateDiscount(Voucher v, decimal baseAmount)
    {
        if (string.Equals(v.DiscountTarget, "FINAL_PRICE", StringComparison.OrdinalIgnoreCase))
        {
            decimal compDiscount = baseAmount > v.DiscountValue ? baseAmount - v.DiscountValue : 0m;
            if (v.MaxDiscountCap.HasValue) compDiscount = Math.Min(compDiscount, v.MaxDiscountCap.Value);
            return Math.Min(compDiscount, baseAmount);
        }

        decimal discount = v.DiscountType switch
        {
            "PERCENTAGE" => baseAmount * v.DiscountValue / 100m,
            _ => v.DiscountValue
        };
        if (v.MaxDiscountCap.HasValue) discount = Math.Min(discount, v.MaxDiscountCap.Value);
        return Math.Min(discount, baseAmount);
    }

    private async Task<string?> ApplyVoucherUsageAsync(
        Voucher voucher,
        int orderId,
        int accountId,
        decimal discountAmount,
        string voucherTarget,
        CancellationToken ct)
    {
        // Tăng UsedQuantity một cách atomic bằng SQL thô để phòng ngừa tranh chấp dữ liệu (Race Condition) 
        // khi nhiều người dùng cùng áp dụng một voucher có giới hạn số lượng cùng lúc.
        var voucherAffected = await _db.Database.ExecuteSqlRawAsync(
            "UPDATE Vouchers SET UsedQuantity = UsedQuantity + 1 WHERE VoucherID = {0} AND (TotalQuantity IS NULL OR UsedQuantity + 1 <= TotalQuantity)",
            new object[] { voucher.VoucherId },
            ct);
        if (voucherAffected == 0)
            return "Voucher usage limit reached.";

        // Ghi nhận lịch sử sử dụng voucher của khách hàng
        await _db.VoucherUsageLogs.AddAsync(new VoucherUsageLog
        {
            VoucherId = voucher.VoucherId,
            AccountId = accountId,
            OrderId = orderId,
            UsedAt = _timeProvider.UtcNow
        }, ct);

        // Lưu thông tin voucher áp dụng cụ thể cho đơn hàng và giá trị được giảm
        await _db.OrderVouchers.AddAsync(new OrderVoucher
        {
            OrderId = orderId,
            VoucherId = voucher.VoucherId,
            DiscountAmountApplied = discountAmount,
            VoucherTarget = voucherTarget
        }, ct);

        return null;
    }

    private FeeRequestDTO BuildFeeRequest(Address address, int weightGrams, decimal insuranceValue, decimal codValue) => new()
    {
        FromDistrictId = _ghnOpts.FromDistrictId,
        FromWardCode = _ghnOpts.FromWardCode,
        ToDistrictId = address.DistrictId ?? 0,
        ToWardCode = address.WardCode ?? string.Empty,
        Weight = Math.Max(weightGrams, 1),
        Length = _ghnOpts.DefaultLength,
        Width = _ghnOpts.DefaultWidth,
        Height = _ghnOpts.DefaultHeight,
        InsuranceValue = insuranceValue,
        CodValue = codValue
    };

    private string GenerateOrderCode()
        => $"ORD{_timeProvider.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";

    private static string BuildAttemptCode(string orderCode)
    {
        var bytes = new byte[4];
        Random.Shared.NextBytes(bytes);
        var uid = Convert.ToHexString(bytes).ToLowerInvariant();
        return $"SPX{orderCode}{uid}";
    }

    private string BuildVietQrUrl(string attemptCode, long amount)
    {
        var p = new System.Collections.Specialized.NameValueCollection
        {
            ["acc"] = _sePayOpts.AccountNumber,
            ["bank"] = _sePayOpts.BankCode,
            ["amount"] = amount.ToString(),
            ["des"] = attemptCode
        };
        var qs = string.Join("&", p.AllKeys.Select(k => $"{k}={Uri.EscapeDataString(p[k]!)}"));
        return $"https://qr.sepay.vn/img?{qs}";
    }

    private static void ApplyPurchasedItemsToCart(
        Cart cart,
        IEnumerable<CheckoutConfirmItemDto> purchasedItems,
        DateTime now)
    {
        var remainingByProductId = purchasedItems
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(i => (int)i.Quantity));

        foreach (var cartItem in cart.CartItems.Where(i => i.RemovedAt == null))
        {
            if (!remainingByProductId.TryGetValue(cartItem.ProductId, out var remainingQty) || remainingQty <= 0)
            {
                continue;
            }

            var cartQty = (int)cartItem.Quantity;
            if (cartQty > remainingQty)
            {
                cartItem.Quantity = (short)(cartQty - remainingQty);
                cartItem.UpdatedAt = now;
                remainingByProductId[cartItem.ProductId] = 0;
                continue;
            }

            cartItem.RemovedAt = now;
            cartItem.UpdatedAt = now;
            remainingByProductId[cartItem.ProductId] = remainingQty - cartQty;
        }
    }

    private async Task<bool> IsCodRestrictedAsync(int accountId, CancellationToken cancellationToken)
    {
        var policy = await GetDeliveryAbusePolicyAsync(accountId, cancellationToken);
        return policy.IsCodRestricted;
    }

    private async Task<DeliveryAbusePolicy> GetDeliveryAbusePolicyAsync(
        int accountId,
        CancellationToken cancellationToken)
    {
        var abuseCase = await _db.CustomerDeliveryAbuseCases
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AccountId == accountId, cancellationToken);

        var countFrom = abuseCase?.CountingFrom;
        var suspiciousOrderCount = await CountSuspiciousCodFailOrdersAsync(accountId, countFrom, cancellationToken);
        var isCodRestricted = abuseCase?.Status is StatusCodProbation
            ? false
            : abuseCase?.Status is StatusCodRestricted
            or StatusPendingReview
            or StatusManuallyBlocked
            or StatusAppealApprovedStrict
            or StatusPermanentBlocked
            || (abuseCase?.Status is not StatusNormal && suspiciousOrderCount >= CodRestrictionThreshold);

        return new DeliveryAbusePolicy(suspiciousOrderCount, isCodRestricted);
    }

    private Task<int> CountSuspiciousCodFailOrdersAsync(
        int accountId,
        DateTime? countFrom,
        CancellationToken cancellationToken)
    {
        return _db.Orders
            .AsNoTracking()
            .CountAsync(o => o.AccountId == accountId
                          && !o.IsDeleted
                          && (!countFrom.HasValue || o.OrderDate >= countFrom.Value)
                          && o.DeliveryFailCount >= 3
                          && o.PaymentMethod == PayMethodCod
                          && o.PaymentStatus != "PAID"
                          && o.LastGHNFailCode != null
                          && DeliveryAbuseFailCodes.Contains(o.LastGHNFailCode),
                cancellationToken);
    }

    private sealed record DeliveryAbusePolicy(int SuspiciousOrderCount, bool IsCodRestricted);

    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true;
}
