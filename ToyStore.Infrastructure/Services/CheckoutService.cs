using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToyStore.Application.DTOs.Checkouts;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Options;
using ToyStore.Application.Common.Helpers;
using ToyStore.Application.Constants;

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
        var cart = await _uow.Carts.GetByAccountIdWithItemsAsync(accountId, cancellationToken);
        if (cart is null || !cart.CartItems.Any(i => i.RemovedAt == null))
            return Result<CheckoutPreviewResponseDto>.BusinessError("Cart is empty.");

        var address = await _uow.Addresses.GetActiveByIdAsync(addressId, cancellationToken);
        if (address is null)
            return Result<CheckoutPreviewResponseDto>.NotFound("Address", addressId);

        var activeAll = cart.CartItems.Where(i => i.RemovedAt == null).ToList();
        List<CartItem> activeItems;
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

        var normalizedPaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? PayMethodCod : paymentMethod;
        normalizedPaymentMethod = normalizedPaymentMethod.Trim().ToUpperInvariant();
        if (normalizedPaymentMethod == PayMethodCod
            && await IsCodRestrictedAsync(accountId, cancellationToken))
        {
            return Result<CheckoutPreviewResponseDto>.BusinessError(
                "Cash on Delivery is temporarily unavailable for your account due to repeated failed COD deliveries. Please choose QR bank transfer or wallet payment.");
        }

        if (!string.IsNullOrWhiteSpace(orderVoucherCode)
            && !string.IsNullOrWhiteSpace(shippingVoucherCode)
            && orderVoucherCode.Equals(shippingVoucherCode, StringComparison.OrdinalIgnoreCase))
        {
            return Result<CheckoutPreviewResponseDto>.BusinessError(
                "Cannot apply the same voucher code for both order and shipping.");
        }

        // Lấy chi tiết cân nặng, chiều dài, rộng, cao thực tế của sản phẩm
        var activeCartItems = activeItems
            .Where(ci => ci.Product.ProductStatus == "Active" && ci.Product.Quantity >= ci.Quantity)
            .ToList();

        var activeProductIds = activeCartItems.Select(ci => ci.ProductId).ToList();

        var productDetailsList = await _db.ProductDetails
            .Where(pd => activeProductIds.Contains(pd.ProductId))
            .Join(_db.Products,
                  pd => pd.ProductId, p => p.ProductId,
                  (pd, p) => new { pd, p })
            .Join(_db.Categories,
                  x => x.p.CategoryId, c => c.CategoryId,
                  (x, c) => new { x.pd, x.p, c })
            .ToListAsync(cancellationToken);

        var detailsMap = productDetailsList.ToDictionary(x => x.p.ProductId);

        var shippingItems = new List<ShippingItem>();
        foreach (var ci in activeCartItems)
        {
            if (detailsMap.TryGetValue(ci.ProductId, out var details))
            {
                var currentPrice = PriceHelper.ResolveCurrentPrice(ci.Product, _timeProvider.UtcNow, (int)ci.Quantity);
                shippingItems.Add(new ShippingItem(
                    ci.ProductId,
                    ci.Product.ProductName,
                    details.c.CategoryName,
                    (int)ci.Quantity,
                    currentPrice,
                    details.pd.WeightGram,
                    details.pd.LengthCm,
                    details.pd.WidthCm,
                    details.pd.HeightCm
                ));
            }
        }

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

        // For COD orders pass subTotal so GHN applies the discounted COD shipping rate
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

        if (normalizedPaymentMethod == PayMethodCod && shippingFee > 0)
        {
            feeReq.CodValue = subTotal;
            feeResult = await _ghnClient.GetFeeAsync(feeReq, cancellationToken);
            if (feeResult.IsSuccess && feeResult.Data!.Fee > 0 && feeResult.Data!.Fee != shippingFee)
            {
                shippingFee = feeResult.Data.Fee;

                if (!string.IsNullOrWhiteSpace(shippingVoucherCode))
                {
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

                if (!string.IsNullOrWhiteSpace(orderVoucherCode) && orderVoucher is not null && string.Equals(orderVoucher.DiscountTarget, "FINAL_PRICE", StringComparison.OrdinalIgnoreCase))
                {
                    decimal baseAmount = subTotal + shippingFee;
                    var voucherResult = await CalculateVoucherDiscountAsync(
                        orderVoucherCode,
                        accountId,
                        subTotal,
                        baseAmount,
                        "FINAL_PRICE",
                        cancellationToken);
                    if (!voucherResult.IsSuccess)
                        return Result<CheckoutPreviewResponseDto>.BusinessError(voucherResult.ErrorMessage ?? "Invalid voucher.");
                    orderDiscount = voucherResult.Data;
                }

                totalBeforeDiscount = subTotal + shippingFee;
                discountAmount = Math.Min(orderDiscount + shippingDiscount, totalBeforeDiscount);
                totalAmount = Math.Max(totalBeforeDiscount - discountAmount, 0m);
            }
        }

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
                var ttl = TimeSpan.FromMinutes(_sePayOpts.PaymentTtlMinutes);
                var isExpired = existingPending.CreatedAt + ttl < _timeProvider.UtcNow;

                if (isExpired)
                {
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
        var products = await _db.Products
            .Include(p => p.ProductImage)
            .Include(p => p.PromotionProductSlots)
                .ThenInclude(pps => pps.TimeSlot)
                    .ThenInclude(ts => ts.Promotion)
            .Include(p => p.ProductPromotions)
                .ThenInclude(pp => pp.Promotion)
                    .ThenInclude(p => p.PromotionTimeSlots)
            .Where(p => productIds.Contains(p.ProductId))
            .ToListAsync(cancellationToken);

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

        // Validate confirm items against active cart (phòng client gửi items không có trong giỏ)
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

        var productDetailsList = await _db.ProductDetails
            .Where(pd => confirmProductIds.Contains(pd.ProductId))
            .Join(_db.Products,
                  pd => pd.ProductId, p => p.ProductId,
                  (pd, p) => new { pd, p })
            .Join(_db.Categories,
                  x => x.p.CategoryId, c => c.CategoryId,
                  (x, c) => new { x.pd, x.p, c })
            .ToListAsync(cancellationToken);

        var detailsMap = productDetailsList.ToDictionary(x => x.p.ProductId);

        var shippingItems = new List<ShippingItem>();
        foreach (var item in request.Items)
        {
            if (detailsMap.TryGetValue(item.ProductId, out var details))
            {
                var p = productMap[item.ProductId];
                var currentPrice = PriceHelper.ResolveCurrentPrice(p, _timeProvider.UtcNow, (int)item.Quantity);
                shippingItems.Add(new ShippingItem(
                    item.ProductId,
                    p.ProductName,
                    details.c.CategoryName,
                    (int)item.Quantity,
                    currentPrice,
                    details.pd.WeightGram,
                    details.pd.LengthCm,
                    details.pd.WidthCm,
                    details.pd.HeightCm
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

        if (payMethodNorm == PayMethodCod && shippingFee > 0)
        {
            feeReq.CodValue = subTotal;
            var feeRetryResult = await _ghnClient.GetFeeAsync(feeReq, cancellationToken);
            if (feeRetryResult.IsSuccess && feeRetryResult.Data!.Fee > 0 && feeRetryResult.Data.Fee != shippingFee)
            {
                shippingFee = feeRetryResult.Data.Fee;

                if (!string.IsNullOrWhiteSpace(shippingVoucherCode) && shippingVoucher is not null)
                    shippingDiscount = Math.Min(CalculateDiscount(shippingVoucher, shippingFee), shippingFee);

                if (!string.IsNullOrWhiteSpace(orderVoucherCode) && orderVoucher is not null && string.Equals(orderVoucher.DiscountTarget, "FINAL_PRICE", StringComparison.OrdinalIgnoreCase))
                {
                    decimal baseAmount = subTotal + shippingFee;
                    orderDiscount = Math.Min(CalculateDiscount(orderVoucher, baseAmount), baseAmount);
                }
            }
        }

        var discountAmount = orderDiscount + shippingDiscount;

        // TotalAmount = SubTotal + ShippingFee - Discount
        var totalAmount = Math.Max(subTotal + shippingFee - discountAmount, 0m);
        var orderCode = GenerateOrderCode();
        var now = _timeProvider.UtcNow;

        var payMethod = payMethodNorm;

        // ── Trong transaction ─────────────────────────────────────────────────
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


            // Insert OrderDetails and handle stock
            foreach (var item in request.Items)
            {
                var p = productMap[item.ProductId];
                var flashSaleSlot = PriceHelper.GetActiveFlashSaleSlot(p, now, (int)item.Quantity);

                await _db.OrderDetails.AddAsync(new OrderDetail
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

                // Trừ stock chung ngay lúc tạo đơn cho cả COD và SE_PAY (reserve tránh oversell)
                // WALLET trừ sau khi kiểm tra số dư ví thành công (bên dưới)
                if (payMethod == PayMethodCod || payMethod == PayMethodSepay)
                {
                    var affected = await _db.Database.ExecuteSqlRawAsync(
                        "UPDATE Products SET Quantity = Quantity - {0} WHERE ProductID = {1} AND Quantity >= {0} AND ProductStatus = 'Active'",
                        new object[] { item.Quantity, item.ProductId },
                        cancellationToken);
                    if (affected == 0)
                    {
                        await _uow.RollbackTransactionAsync(cancellationToken);
                        return Result<CheckoutConfirmResponseDto>.Conflict(
                            $"Product '{p.ProductName}' is out of stock.");
                    }
                }

                // Trừ stock Flash Sale (nếu có)
                if (flashSaleSlot != null)
                {
                    string sql;
                    if (payMethod == PayMethodSepay)
                    {
                        // SE_PAY: Tăng ReservedQuantity
                        sql = "UPDATE PromotionProductSlots SET ReservedQuantity = ReservedQuantity + {0} " +
                              "WHERE SlotProductID = {1} AND SoldQuantity + ReservedQuantity + {0} <= SaleQuantity";
                    }
                    else
                    {
                        // COD/Wallet: Tăng SoldQuantity
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
                // Kiểm tra ví
                var wallet = await _db.Wallets
                    .FirstOrDefaultAsync(w => w.AccountId == accountId, cancellationToken);
                if (wallet is null || wallet.Status != "Active")
                {
                    await _uow.RollbackTransactionAsync(cancellationToken);
                    return Result<CheckoutConfirmResponseDto>.BusinessError("Wallet is not available.");
                }

                var hasActivePin = await _db.WalletPins
                    .AnyAsync(p => p.WalletId == wallet.WalletId && p.IsActive, cancellationToken);
                if (!hasActivePin)
                {
                    await _uow.RollbackTransactionAsync(cancellationToken);
                    return Result<CheckoutConfirmResponseDto>.BusinessError(
                        "Wallet is not activated. Please set up your PIN on the Wallet page.");
                }

                // IMPORTANT: Check available balance = Balance - LockedBalance to prevent double-spending
                // when a withdrawal is in-flight (LockedBalance > 0). Using Balance alone would allow
                // spending funds already reserved for a pending/processing withdrawal, causing
                // the withdrawal CommitAsync to fail with InsufficientAvailable and losing store money.
                var walletAffected = await _db.Database.ExecuteSqlRawAsync(
                    "UPDATE Wallets SET Balance = Balance - {0} WHERE AccountID = {1} AND (Balance - LockedBalance) >= {0} AND Status = 'Active'",
                    new object[] { totalAmount, accountId },
                    cancellationToken);
                if (walletAffected == 0)
                {
                    await _uow.RollbackTransactionAsync(cancellationToken);
                    return Result<CheckoutConfirmResponseDto>.BusinessError("Insufficient wallet balance.");
                }

                var walletAfterBalance = wallet.Balance - totalAmount;
                var walletTxn = new WalletTransaction
                {
                    WalletId = wallet.WalletId,
                    AccountId = accountId,
                    RelatedOrderId = order.OrderId,
                    TxnType = "Payment",
                    Direction = "DR",
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
                await _db.SaveChangesAsync(cancellationToken); // get WalletTransactionId

                // TRỪ STOCK CHUNG (WALLET - chỉ trừ sau khi thanh toán thành công)
                foreach (var item in request.Items)
                {
                    var p = productMap[item.ProductId];
                    var affected = await _db.Database.ExecuteSqlRawAsync(
                        "UPDATE Products SET Quantity = Quantity - {0} WHERE ProductID = {1} AND Quantity >= {0} AND ProductStatus = 'Active'",
                        new object[] { item.Quantity, item.ProductId },
                        cancellationToken);
                    if (affected == 0)
                    {
                        await _uow.RollbackTransactionAsync(cancellationToken);
                        return Result<CheckoutConfirmResponseDto>.Conflict(
                            $"Product '{p.ProductName}' is out of stock.");
                    }
                }

                order.PaymentStatus = "PAID";
                order.PaidAt = now;
                order.StatusId = confirmedStatusId;
                order.ConfirmedAt = now;

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
                // Sinh attempt code + insert PaymentGatewayTransaction
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
                        _db.ChangeTracker.Entries<PaymentGatewayTransaction>()
                            .Where(e => e.State == EntityState.Added)
                            .ToList()
                            .ForEach(e => e.State = EntityState.Detached);
                    }
                }
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

    // ── Retry QR ──────────────────────────────────────────────────────────────

    public async Task<Result<RetryPaymentResponseDto>> RetryPaymentAsync(
        int accountId,
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _db.Orders
            .Include(o => o.OrderDetails)
            .Include(o => o.Status)
            .Include(o => o.PaymentGatewayTransactions)
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.AccountId == accountId && !o.IsDeleted, cancellationToken);

        if (order is null)
            return Result<RetryPaymentResponseDto>.NotFound("Order", orderId);

        if (order.PaymentMethod != PayMethodSepay)
            return Result<RetryPaymentResponseDto>.BusinessError("Order does not use SE_PAY payment.");

        if (order.PaymentStatus == "PAID")
            return Result<RetryPaymentResponseDto>.BusinessError("Order has already been paid.");

        if (order.CancelledAt.HasValue)
            return Result<RetryPaymentResponseDto>.BusinessError("Order has been cancelled, cannot generate new QR.");

        var now = _timeProvider.UtcNow;
        var ttl = TimeSpan.FromMinutes(_sePayOpts.PaymentTtlMinutes);
        if (order.CreatedAt + ttl < now)
        {
            if (order.PaymentStatus == "PENDING" && order.CancelledAt == null)
            {
                _logger.LogInformation("RetryPaymentAsync: Auto-cancelling expired SE_PAY order {OrderId} for account {AccountId}", 
                    order.OrderId, accountId);

                await _orderLifecycle.CancelOrderInternalAsync(
                    order,
                    "SE_PAY payment timeout — auto-cancelled during retry payment attempt",
                    cancelledByAccountId: 0, // system
                    restoreCart: false,
                    restoreVoucher: true,
                    cancellationToken: cancellationToken);
            }
            return Result<RetryPaymentResponseDto>.BusinessError("Order payment window has expired.");
        }

        if (order.PaymentStatus is "EXPIRED" or "CANCELLED" or "FAILED")
            return Result<RetryPaymentResponseDto>.BusinessError($"Order is in {order.PaymentStatus} status, cannot generate new QR.");

        var totalAttempts = order.PaymentGatewayTransactions.Count;
        if (totalAttempts >= _sePayOpts.MaxPaymentAttempts)
            return Result<RetryPaymentResponseDto>.BusinessError(
                $"Exceeded {_sePayOpts.MaxPaymentAttempts} payment attempts for this order.");

        // Cancel các attempt Pending cũ
        var pendingAttempts = order.PaymentGatewayTransactions
            .Where(t => t.Status == "Pending").ToList();
        foreach (var t in pendingAttempts)
        {
            t.Status = "Cancelled";
            t.UpdatedAt = now;
        }

        // Sinh attempt code mới
        string? attemptCode = null;
        for (var retryInsert = 0; retryInsert < 3; retryInsert++)
        {
            attemptCode = BuildAttemptCode(order.OrderCode);
            try
            {
                await _db.PaymentGatewayTransactions.AddAsync(new PaymentGatewayTransaction
                {
                    OrderId = order.OrderId,
                    Provider = "SE_PAY",
                    RequestId = attemptCode,
                    Amount = order.TotalAmount,
                    Status = "Pending",
                    RetryCount = 0,
                    CreatedAt = now
                }, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
                break;
            }
            catch (DbUpdateException dex) when (IsUniqueViolation(dex))
            {
                _logger.LogWarning("RetryPayment UNIQUE violation for attemptCode {Code}", attemptCode);
                _db.ChangeTracker.Entries<PaymentGatewayTransaction>()
                    .Where(e => e.State == EntityState.Added)
                    .ToList()
                    .ForEach(e => e.State = EntityState.Detached);
            }
        }

        var qrUrl = BuildVietQrUrl(attemptCode!, (long)order.TotalAmount);

        return Result<RetryPaymentResponseDto>.Success(new RetryPaymentResponseDto
        {
            PaymentAttemptCode = attemptCode!,
            QrImageUrl = qrUrl,
            TotalAmount = order.TotalAmount
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────


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
        var voucherAffected = await _db.Database.ExecuteSqlRawAsync(
            "UPDATE Vouchers SET UsedQuantity = UsedQuantity + 1 WHERE VoucherID = {0} AND (TotalQuantity IS NULL OR UsedQuantity + 1 <= TotalQuantity)",
            new object[] { voucher.VoucherId },
            ct);
        if (voucherAffected == 0)
            return "Voucher usage limit reached.";

        await _db.VoucherUsageLogs.AddAsync(new VoucherUsageLog
        {
            VoucherId = voucher.VoucherId,
            AccountId = accountId,
            OrderId = orderId,
            UsedAt = _timeProvider.UtcNow
        }, ct);

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
