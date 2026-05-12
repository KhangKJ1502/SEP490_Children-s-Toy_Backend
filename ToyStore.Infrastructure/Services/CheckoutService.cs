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

    private const string PayMethodCod = "SHIP_COD";
    private const string PayMethodSepay = "SE_PAY";
    private const string PayMethodWallet = "WALLET";

    public CheckoutService(
        IUnitOfWork uow,
        SEP490ToyStoreContext db,
        IGhnClient ghnClient,
        IOptions<GhnOptions> ghnOpts,
        IOptions<SePayOptions> sePayOpts,
        IOptions<ShopAddressOptions> shopAddr,
        IDomainEventPublisher eventPublisher,
        ILogger<CheckoutService> logger,
        ITimeProvider timeProvider)
    {
        _uow            = uow;
        _db             = db;
        _ghnClient      = ghnClient;
        _ghnOpts        = ghnOpts.Value;
        _sePayOpts      = sePayOpts.Value;
        _shopAddr       = shopAddr.Value;
        _eventPublisher = eventPublisher;
        _logger         = logger;
        _timeProvider   = timeProvider;
    }

    // ── Preview ───────────────────────────────────────────────────────────────

    public async Task<Result<CheckoutPreviewResponseDto>> PreviewAsync(
        int accountId,
        int addressId,
        string? voucherCode,
        IReadOnlyList<CheckoutConfirmItemDto>? itemsSubset,
        CancellationToken cancellationToken = default)
    {
        var cart = await _uow.Carts.GetByAccountIdWithItemsAsync(accountId, cancellationToken);
        if (cart is null || !cart.CartItems.Any(i => i.RemovedAt == null))
            return Result<CheckoutPreviewResponseDto>.BusinessError("Giỏ hàng trống.");

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
                        $"Sản phẩm #{line.ProductId} không có trong giỏ.");
                if (ci.Quantity != line.Quantity)
                    return Result<CheckoutPreviewResponseDto>.BusinessError(
                        "Số lượng trong giỏ đã thay đổi, vui lòng làm mới trang.");
                activeItems.Add(ci);
            }
        }
        else
            activeItems = activeAll;
        var itemErrors = new List<CheckoutPreviewItemErrorDto>();
        decimal subTotal = 0;
        int totalWeightGrams = 0;

        foreach (var ci in activeItems)
        {
            var p = ci.Product;
            if (p.ProductStatus != "Active")
                itemErrors.Add(new() { ProductId = p.ProductId, ProductName = p.ProductName, Error = "Sản phẩm không còn bán." });
            else if (p.Quantity < ci.Quantity)
                itemErrors.Add(new() { ProductId = p.ProductId, ProductName = p.ProductName, Error = $"Chỉ còn {p.Quantity} sản phẩm." });
            else
            {
                var currentPrice = PriceHelper.ResolveCurrentPrice(p, _timeProvider.UtcNow);
                subTotal += currentPrice * ci.Quantity;
                totalWeightGrams += _sePayOpts.DefaultItemWeightGrams * ci.Quantity;
            }
        }

        // Tính voucher discount
        decimal discountAmount = 0;
        if (!string.IsNullOrWhiteSpace(voucherCode))
        {
            var voucherResult = await CalculateVoucherDiscountAsync(voucherCode, accountId, subTotal, cancellationToken);
            if (voucherResult.IsSuccess)
                discountAmount = voucherResult.Data;
        }

        // Gọi GHN fee — không block checkout nếu fail
        decimal shippingFee = 0;
        DateTime? estimatedDelivery = null;
        var feeReq = BuildFeeRequest(address, Math.Max(totalWeightGrams, 1));
        var feeResult = await _ghnClient.GetFeeAsync(feeReq, cancellationToken);
        if (feeResult.IsSuccess)
        {
            shippingFee = feeResult.Data!.Fee;
            var ldReq = new LeadtimeRequestDTO
            {
                FromDistrictId = _ghnOpts.FromDistrictId,
                FromWardCode = _ghnOpts.FromWardCode,
                ToDistrictId = address.DistrictId ?? 0,
                ToWardCode = address.WardCode ?? string.Empty,
                ServiceId = feeResult.Data.ServiceId
            };
            var ldResult = await _ghnClient.GetLeadtimeAsync(ldReq, cancellationToken);
            if (ldResult.IsSuccess) estimatedDelivery = ldResult.Data!.EstimatedDeliveryTime;
        }

        // Discount không vượt subTotal
        discountAmount = Math.Min(discountAmount, subTotal);
        var totalAmount = subTotal + shippingFee - discountAmount;

        return Result<CheckoutPreviewResponseDto>.Success(new CheckoutPreviewResponseDto
        {
            SubTotal = subTotal,
            ShippingFee = shippingFee,
            DiscountAmount = discountAmount,
            TotalAmount = totalAmount,
            TotalWeightGrams = totalWeightGrams,
            EstimatedDeliveryTime = estimatedDelivery,
            ItemErrors = itemErrors
        });
    }

    // ── Confirm ───────────────────────────────────────────────────────────────

    public async Task<Result<CheckoutConfirmResponseDto>> ConfirmAsync(
        int accountId,
        CheckoutConfirmRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // Validate cơ bản
        if (request.Items.Count == 0)
            return Result<CheckoutConfirmResponseDto>.BusinessError("Giỏ hàng trống.");

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
            return Result<CheckoutConfirmResponseDto>.Failure("CONFIGURATION_ERROR", "Status 'Pending' không tồn tại trong DB.");
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
            { softErrors.Add($"Sản phẩm ID {item.ProductId} không tồn tại."); continue; }
            if (p.ProductStatus != "Active")
                softErrors.Add($"Sản phẩm '{p.ProductName}' không còn bán.");
            else if (p.Quantity < item.Quantity)
                softErrors.Add($"Sản phẩm '{p.ProductName}' chỉ còn {p.Quantity} cái.");
        }
        if (softErrors.Count > 0)
            return Result<CheckoutConfirmResponseDto>.BusinessError(string.Join("; ", softErrors));

        // Tính SubTotal từ Products.Price tại thời điểm checkout (có tính Flash Sale/Promotion)
        decimal subTotal = request.Items.Sum(i => PriceHelper.ResolveCurrentPrice(productMap[i.ProductId], _timeProvider.UtcNow) * (int)i.Quantity);
        int totalWeightGrams = request.Items.Sum(i => _sePayOpts.DefaultItemWeightGrams * (int)i.Quantity);

        // Lấy phí ship thật
        var feeReq = BuildFeeRequest(address, Math.Max(totalWeightGrams, 1));
        var feeResult = await _ghnClient.GetFeeAsync(feeReq, cancellationToken);
        decimal shippingFee = feeResult.IsSuccess ? feeResult.Data!.Fee : 0m;
        int resolvedServiceId = feeResult.IsSuccess ? feeResult.Data!.ServiceId : 0;
        DateTime? estimatedDelivery = null;
        if (feeResult.IsSuccess)
        {
            var ldReq = new LeadtimeRequestDTO
            {
                FromDistrictId = _ghnOpts.FromDistrictId,
                FromWardCode = _ghnOpts.FromWardCode,
                ToDistrictId = address.DistrictId ?? 0,
                ToWardCode = address.WardCode ?? string.Empty,
                ServiceId = resolvedServiceId
            };
            var ldResult = await _ghnClient.GetLeadtimeAsync(ldReq, cancellationToken);
            if (ldResult.IsSuccess) estimatedDelivery = ldResult.Data!.EstimatedDeliveryTime;
        }

        // Voucher: validate + tính discount
        decimal discountAmount = Math.Min(request.VoucherDiscountAmount, subTotal);
        Voucher? voucher = null;
        if (!string.IsNullOrWhiteSpace(request.VoucherCode))
        {
            voucher = await _uow.Vouchers.GetByCodeAsync(request.VoucherCode, cancellationToken);
            if (voucher is null)
                return Result<CheckoutConfirmResponseDto>.BusinessError("Mã voucher không tồn tại.");

            var voucherCheck = await ValidateVoucherAsync(voucher, accountId, subTotal, cancellationToken);
            if (!string.IsNullOrEmpty(voucherCheck))
                return Result<CheckoutConfirmResponseDto>.BusinessError(voucherCheck);

            discountAmount = Math.Min(CalculateDiscount(voucher, subTotal), subTotal);
        }

        // TotalAmount = SubTotal + ShippingFee - Discount; floor = ShippingFee
        var totalAmount = Math.Max(subTotal + shippingFee - discountAmount, shippingFee);
        var orderCode = GenerateOrderCode();
        var now = _timeProvider.UtcNow;

        var payMethod = request.PaymentMethod?.ToUpperInvariant() ?? PayMethodCod;

        // ── Trong transaction ─────────────────────────────────────────────────
        await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            var order = new Order
            {
                AccountId = accountId,
                StatusId = pendingStatusId,
                OrderCode = orderCode,
                ShippingName = address.RecipientName ?? address.Account?.AccountName ?? "Khách hàng",
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
                var flashSaleSlot = PriceHelper.GetActiveFlashSaleSlot(p, now);

                await _db.OrderDetails.AddAsync(new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    ProductImage = p.ProductImage?.ImageUrl,
                    Quantity = item.Quantity,
                    UnitPrice = PriceHelper.ResolveCurrentPrice(p, now),
                    DiscountAmount = 0,
                    SlotProductId = flashSaleSlot?.SlotProductId,
                    CreatedAt = now
                }, cancellationToken);

                // Trừ stock chung (chỉ cho COD ngay lúc tạo; WALLET trừ sau khi check ví; SE_PAY trừ khi webhook PAID)
                if (payMethod == PayMethodCod)
                {
                    var affected = await _db.Database.ExecuteSqlRawAsync(
                        "UPDATE Products SET Quantity = Quantity - {0} WHERE ProductID = {1} AND Quantity >= {0} AND ProductStatus = 'Active'",
                        new object[] { item.Quantity, item.ProductId },
                        cancellationToken);
                    if (affected == 0)
                    {
                        await _uow.RollbackTransactionAsync(cancellationToken);
                        return Result<CheckoutConfirmResponseDto>.Conflict(
                            $"Sản phẩm '{p.ProductName}' không đủ hàng.");
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
                              "WHERE SlotProductID = {1} AND SoldQuantity + ReservedQuantity + {0} <= SaleQuantity AND IsActive = 1";
                    }
                    else
                    {
                        // COD/Wallet: Tăng SoldQuantity
                        sql = "UPDATE PromotionProductSlots SET SoldQuantity = SoldQuantity + {0} " +
                              "WHERE SlotProductID = {1} AND SoldQuantity + ReservedQuantity + {0} <= SaleQuantity AND IsActive = 1";
                    }

                    var flashAffected = await _db.Database.ExecuteSqlRawAsync(sql, new object[] { (int)item.Quantity, flashSaleSlot.SlotProductId }, cancellationToken);
                    if (flashAffected == 0)
                    {
                        await _uow.RollbackTransactionAsync(cancellationToken);
                        return Result<CheckoutConfirmResponseDto>.Conflict(
                            $"Sản phẩm '{p.ProductName}' đã hết lượt Flash Sale.");
                    }
                }
            }

            // Voucher: tăng UsedQuantity + insert VoucherUsageLogs
            if (voucher is not null)
            {
                var voucherAffected = await _db.Database.ExecuteSqlRawAsync(
                    "UPDATE Vouchers SET UsedQuantity = UsedQuantity + 1 WHERE VoucherID = {0} AND (TotalQuantity IS NULL OR UsedQuantity + 1 <= TotalQuantity)",
                    new object[] { voucher.VoucherId },
                    cancellationToken);
                if (voucherAffected == 0)
                {
                    await _uow.RollbackTransactionAsync(cancellationToken);
                    return Result<CheckoutConfirmResponseDto>.BusinessError("Voucher đã hết lượt sử dụng.");
                }
                await _db.VoucherUsageLogs.AddAsync(new VoucherUsageLog
                {
                    VoucherId = voucher.VoucherId,
                    AccountId = accountId,
                    OrderId = order.OrderId,
                    UsedAt = now
                }, cancellationToken);
                await _db.OrderVouchers.AddAsync(new OrderVoucher
                {
                    OrderId = order.OrderId,
                    VoucherId = voucher.VoucherId,
                    DiscountAmountApplied = discountAmount
                }, cancellationToken);
            }

            // Insert OrderStatusHistory
            await _db.OrderStatusHistories.AddAsync(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                StatusId = pendingStatusId,
                ChangedBy = accountId,
                Note = "Đơn hàng được tạo",
                CreatedAt = now
            }, cancellationToken);

            // Xóa CartItems đã checkout
            var cart = await _uow.Carts.GetByAccountIdWithItemsAsync(accountId, cancellationToken);
            if (cart is not null)
            {
                var checkedOutProductIds = request.Items.Select(i => i.ProductId).ToHashSet();
                foreach (var ci in cart.CartItems.Where(i => i.RemovedAt == null && checkedOutProductIds.Contains(i.ProductId)))
                    ci.RemovedAt = now;
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
                    return Result<CheckoutConfirmResponseDto>.BusinessError("Ví không khả dụng.");
                }

                var walletAffected = await _db.Database.ExecuteSqlRawAsync(
                    "UPDATE Wallets SET Balance = Balance - {0} WHERE AccountID = {1} AND Balance >= {0} AND Status = 'Active'",
                    new object[] { totalAmount, accountId },
                    cancellationToken);
                if (walletAffected == 0)
                {
                    await _uow.RollbackTransactionAsync(cancellationToken);
                    return Result<CheckoutConfirmResponseDto>.BusinessError("Số dư ví không đủ.");
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
                    Method = "Wallet",
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
                            $"Sản phẩm '{p.ProductName}' không đủ hàng.");
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
                    Note = "Auto-confirmed: wallet payment",
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

            // ── Sau transaction ──────────────────────────────────────────

            _logger.LogInformation("Order {Code} created via {Method}, Status={PS}",
                orderCode, payMethod, order.PaymentStatus);

            await _eventPublisher.PublishAsync("Order", order.OrderId.ToString(),
                "order.created", new { orderId = order.OrderId, orderCode }, CancellationToken.None);

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
            .Include(o => o.PaymentGatewayTransactions)
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.AccountId == accountId && !o.IsDeleted, cancellationToken);

        if (order is null)
            return Result<RetryPaymentResponseDto>.NotFound("Order", orderId);

        if (order.PaymentMethod != PayMethodSepay)
            return Result<RetryPaymentResponseDto>.BusinessError("Đơn hàng không sử dụng thanh toán SE_PAY.");

        if (order.PaymentStatus == "PAID")
            return Result<RetryPaymentResponseDto>.BusinessError("Đơn hàng đã được thanh toán.");

        if (order.PaymentStatus is "EXPIRED" or "CANCELLED" or "FAILED")
            return Result<RetryPaymentResponseDto>.BusinessError($"Đơn hàng ở trạng thái {order.PaymentStatus}, không thể tạo QR mới.");

        var totalAttempts = order.PaymentGatewayTransactions.Count;
        if (totalAttempts >= _sePayOpts.MaxPaymentAttempts)
            return Result<RetryPaymentResponseDto>.BusinessError(
                $"Đã vượt quá {_sePayOpts.MaxPaymentAttempts} lần thử thanh toán cho đơn này.");

        var now = _timeProvider.UtcNow;

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
        string voucherCode, int accountId, decimal subTotal, CancellationToken ct)
    {
        var voucher = await _uow.Vouchers.GetByCodeAsync(voucherCode, ct);
        if (voucher is null) return Result<decimal>.NotFound("Voucher");
        var err = await ValidateVoucherAsync(voucher, accountId, subTotal, ct);
        if (!string.IsNullOrEmpty(err)) return Result<decimal>.BusinessError(err);
        return Result<decimal>.Success(CalculateDiscount(voucher, subTotal));
    }

    private async Task<string?> ValidateVoucherAsync(Voucher v, int accountId, decimal subTotal, CancellationToken ct)
    {
        var now = _timeProvider.UtcNow;
        if (v.Status != "Active" || v.IsDeleted) return "Voucher không hợp lệ.";
        if (now < v.StartDate || now > v.EndDate) return "Voucher hết hạn.";
        if (v.MinOrderAmount.HasValue && subTotal < v.MinOrderAmount.Value)
            return $"Đơn hàng tối thiểu {v.MinOrderAmount:N0}₫ để dùng voucher này.";
        if (v.TotalQuantity.HasValue && v.UsedQuantity >= v.TotalQuantity.Value)
            return "Voucher đã hết lượt sử dụng.";
        if (v.MaxUsagePerUser.HasValue)
        {
            var usageCount = await _uow.Vouchers.CountUsageByAccountAsync(v.VoucherId, accountId, ct);
            if (usageCount >= v.MaxUsagePerUser.Value)
                return "Bạn đã dùng voucher này đủ số lần cho phép.";
        }
        return null;
    }

    private static decimal CalculateDiscount(Voucher v, decimal subTotal)
    {
        decimal discount = v.DiscountType switch
        {
            "PERCENTAGE" => subTotal * v.DiscountValue / 100m,
            _ => v.DiscountValue
        };
        if (v.MaxDiscountCap.HasValue) discount = Math.Min(discount, v.MaxDiscountCap.Value);
        return Math.Min(discount, subTotal);
    }

    private FeeRequestDTO BuildFeeRequest(Address address, int weightGrams) => new()
    {
        FromDistrictId = _ghnOpts.FromDistrictId,
        FromWardCode = _ghnOpts.FromWardCode,
        ToDistrictId = address.DistrictId ?? 0,
        ToWardCode = address.WardCode ?? string.Empty,
        Weight = Math.Max(weightGrams, 1),
        Length = 20,
        Width = 15,
        Height = 10,
        InsuranceValue = 0,
        CodValue = 0
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

    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true;
}
