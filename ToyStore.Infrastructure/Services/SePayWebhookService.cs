using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Application.Constants;
using ToyStore.Domain.Entities;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Options;

namespace ToyStore.Infrastructure.Services;

/// <summary>
/// Xử lý webhook SE_PAY: phân nhánh SPX_ (thanh toán đơn) và WLT_ (nạp ví).
/// Luôn idempotent — SE_PAY có thể gửi lại cùng webhook nhiều lần.
/// </summary>
public class SePayWebhookService : ISePayWebhookService
{
    private const string TopUpAttemptPrefix = "wallet:topup:attempt:";

    private readonly IUnitOfWork _uow;
    private readonly SEP490ToyStoreContext _db;
    private readonly IGhnClient _ghnClient;
    private readonly IRedisService _redisService;
    private readonly SePayOptions _sePayOpts;
    private readonly ShopAddressOptions _shopAddr;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ILogger<SePayWebhookService> _logger;
    private readonly ITimeProvider _timeProvider;

    public SePayWebhookService(
        IUnitOfWork uow,
        SEP490ToyStoreContext db,
        IGhnClient ghnClient,
        IRedisService redisService,
        IOptions<SePayOptions> sePayOpts,
        IOptions<ShopAddressOptions> shopAddr,
        IDomainEventPublisher eventPublisher,
        ILogger<SePayWebhookService> logger,
        ITimeProvider timeProvider)
    {
        _uow = uow;
        _db = db;
        _ghnClient = ghnClient;
        _redisService = redisService;
        _sePayOpts = sePayOpts.Value;
        _shopAddr = shopAddr.Value;
        _eventPublisher = eventPublisher;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task HandleAsync(SePayWebhookPayload payload, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(payload.TransferType)
            && !payload.TransferType.Equals("in", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("SE_PAY webhook ignored: transferType='{Type}'", payload.TransferType);
            return;
        }

        var attemptCode = ResolveAttemptCode(payload);
        if (attemptCode is null)
        {
            var content = (payload.Content ?? payload.Description ?? string.Empty).Trim();
            _logger.LogWarning("SE_PAY webhook: unknown content prefix — content='{Content}'", content);
            return;
        }

        if (IsPrefixed(attemptCode, "SPX"))
        {
            await HandleOrderPaymentAsync(attemptCode, payload.TransferAmount, payload, cancellationToken);
        }
        else if (IsPrefixed(attemptCode, "WLT"))
        {
            await HandleWalletTopUpAsync(attemptCode, payload, cancellationToken);
        }
    }

    // ── SPX_: Thanh toán đơn hàng ────────────────────────────────────────────

    private async Task HandleOrderPaymentAsync(
        string attemptCode,
        decimal amount,
        SePayWebhookPayload payload,
        CancellationToken ct)
    {
        // 1. Lookup
        var txn = await _uow.Orders.GetPaymentTransactionByRequestIdAsync(attemptCode, ct);

        if (txn is null)
        {
            _logger.LogWarning("SPX webhook: attempt code '{Code}' not found — manual reconciliation needed", attemptCode);
            await _eventPublisher.PublishAsync("Payment", "0",
                NotificationEventTypes.SystemPaymentGatewayError,
                new { attemptCode, amount, note = "Attempt code not found" },
                CancellationToken.None);
            return;
        }

        txn.RawCallback = JsonSerializer.Serialize(payload);

        // 2. Idempotency
        if (txn.Status == "Paid")
        {
            _logger.LogInformation("SPX webhook: attempt '{Code}' already Paid — skip", attemptCode);
            return;
        }

        var order = txn.Order;

        // 3. Đơn đã cancel
        if (order.CancelledAt.HasValue)
        {
            _logger.LogWarning("SPX webhook: Order {Code} is cancelled:cannot pay, manual refund needed", order.OrderCode);
            return;
        }

        var now = _timeProvider.UtcNow;

        // 4. Verify amount
        var diff = Math.Abs(amount - order.TotalAmount);
        if (diff > 1 && amount < order.TotalAmount)
        {
            txn.Status = "Failed";
            txn.UpdatedAt = now;
            txn.ResponseMessage = $"Amount mismatch: expected {order.TotalAmount}, received {amount}";
            await _uow.SaveChangesAsync(ct);
            _logger.LogWarning("SPX webhook: amount mismatch for Order {Code}: expected {Exp}, got {Got}",
                order.OrderCode, order.TotalAmount, amount);
            return;
        }

        bool isOverpay = amount > order.TotalAmount + 1;

        // 5. Lấy statusId Confirmed
        var statusMap = await _uow.Orders.GetStatusMapAsync(ct);
        statusMap.TryGetValue("Confirmed", out var confirmedStatusId);

        await _uow.BeginTransactionAsync(ct);
        try
        {
            // 5a. Stock đã được trừ khi tạo đơn (SE_PAY reserve tại confirm); chỉ cần chuyển Flash Sale Reserved → Sold
            foreach (var detail in order.OrderDetails)
            {
                if (detail.SlotProductId.HasValue)
                {
                    // Chuyển từ ReservedQuantity sang SoldQuantity
                    await _uow.Orders.AdjustFlashSaleStockAsync(detail.SlotProductId.Value, (int)detail.Quantity, -(int)detail.Quantity, ct);
                }
            }

            // 5b. Update gateway txn
            txn.Status = "Paid";
            txn.UpdatedAt = now;
            txn.ResponseCode = "00";
            txn.ResponseMessage = "Success";

            // 5c. Update order
            order.PaymentStatus = "PAID";
            order.PaidAt = now;
            order.UpdatedAt = now;
            if (confirmedStatusId > 0)
            {
                order.StatusId = confirmedStatusId;
                order.ConfirmedAt = now;
            }

            // 5d. Insert PaymentHistory
            var payHistory = new PaymentHistory
            {
                AccountId = order.AccountId,
                OrderId = order.OrderId,
                PaymentStatus = "PAID",
                PaymentMethod = "SE_PAY",
                TransactionCode = attemptCode,
                Amount = order.TotalAmount,
                CreatedAt = now
            };
            await _uow.Orders.AddPaymentHistoryAsync(payHistory, ct);
            await _uow.SaveChangesAsync(ct); // get PaymentHistoryId

            txn.PaymentHistoryId = payHistory.PaymentHistoryId;

            // 5e. OrderStatusHistory
            if (confirmedStatusId > 0)
            {
                await _uow.Orders.AddStatusHistoryAsync(new OrderStatusHistory
                {
                    OrderId = order.OrderId,
                    StatusId = confirmedStatusId,
                    ChangedBy = null,
                    Note = "Auto-confirmed: SE_PAY received",
                    CreatedAt = now
                }, ct);
            }

            // 5f. Overpay → ghi vào ví
            if (isOverpay)
            {
                var overpayAmount = amount - order.TotalAmount;
                var idempotencyKey = $"OVERPAY_{attemptCode}";
                var alreadyExists = await _uow.Orders.ExistsWalletTransactionByIdempotencyKeyAsync(idempotencyKey, ct);
                if (!alreadyExists)
                {
                    var wallet = await _uow.Orders.GetWalletByAccountIdAsync(order.AccountId, ct);
                    if (wallet is not null)
                    {
                        var balanceBefore = wallet.Balance;
                        wallet.Balance += overpayAmount;
                        await _uow.Orders.AddWalletTransactionAsync(new WalletTransaction
                        {
                            WalletId = wallet.WalletId,
                            AccountId = order.AccountId,
                            TxnType = "TopUp",
                            Direction = "CR",
                            Amount = overpayAmount,
                            BalanceBefore = balanceBefore,
                            BalanceAfter = wallet.Balance,
                            Method = "BankTransfer",
                            Reason = "Top up via Bank Transfer (SePay overpay)",
                            IdempotencyKey = idempotencyKey,
                            Status = "Completed",
                            CreatedAt = now,
                            CompletedAt = now
                        }, ct);
                        _logger.LogInformation("Overpay {Amount} credited to wallet for Account {Id}, Order {Code}",
                            overpayAmount, order.AccountId, order.OrderCode);
                    }
                }
            }

            // 5g. Xử lý CartItems đã thanh toán (SE_PAY giữ cart đến webhook PAID)
            var cart = await _uow.Carts.GetByAccountIdWithItemsAsync(order.AccountId, ct);
            if (cart is not null)
            {
                ApplyPaidOrderItemsToCart(cart, order.OrderDetails, now);
            }

            await _uow.SaveChangesAsync(ct);
            await _uow.CommitTransactionAsync(ct);
        }
        catch
        {
            await _uow.RollbackTransactionAsync(ct);
            throw;
        }


        _logger.LogInformation("SPX webhook processed: Order {Code} PAID", order.OrderCode);

        var orderPayload = new { orderId = order.OrderId, orderCode = order.OrderCode };
        await _eventPublisher.PublishAsync("Order", order.OrderId.ToString(),
            NotificationEventTypes.OrderConfirmed,
            orderPayload,
            CancellationToken.None);

        // Also notify Merchandise team to start packing
        await _eventPublisher.PublishAsync("Order", order.OrderId.ToString(),
            NotificationEventTypes.MerchReadyToPack,
            orderPayload,
            CancellationToken.None);
    }

    // ── WLT_: Nạp ví ─────────────────────────────────────────────────────────

    private async Task HandleWalletTopUpAsync(string idempotencyKey, SePayWebhookPayload payload, CancellationToken ct)
    {
        var amount = payload.TransferAmount;

        // Ưu tiên xử lý theo metadata topup đã tạo trước đó từ API tạo QR.
        // Nếu không tìm thấy metadata thì fallback về nhánh xử lý thủ công cũ phía dưới.
        if (await TryHandleWalletTopUpFromAttemptCacheAsync(idempotencyKey, payload, ct))
        {
            return;
        }

        if (await TryBackfillWalletTopUpGatewayTransactionAsync(idempotencyKey, payload, ct))
        {
            return;
        }

        // Trích AccountId từ key: WLT_{AccountId}_{uuid} hoặc tìm theo nội dung
        // Format mong đợi: WLT_{anything} — cần lookup ví theo số tài khoản nhận
        // Vì SE_PAY không truyền AccountId trực tiếp, dùng SubAccount nếu có hoặc content parse
        // Tạm thời log warning và skip nếu không xác định được account
        _logger.LogInformation("WLT webhook received with key '{Key}', amount {Amt}", idempotencyKey, amount);

        // TODO: parse accountId từ idempotencyKey hoặc từ payload.SubAccount / ReferenceCode
        // Khi deploy thật, cần thống nhất format WLT_{AccountId}_{uuid8}
        // Tạm thời ghi nhận vào outbox để xử lý thủ công nếu không parse được
        await _eventPublisher.PublishAsync("Wallet", "0",
            NotificationEventTypes.SystemPaymentGatewayError,
            new { idempotencyKey, amount, note = "WLT topup received — manual processing if needed" },
            CancellationToken.None);
    }

    private static bool IsPrefixed(string content, string prefix)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        return content.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            || content.StartsWith(prefix + "_", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ResolveAttemptCode(SePayWebhookPayload payload)
    {
        var fromCode = NormalizeAttemptCode(payload.Code);
        if (!string.IsNullOrWhiteSpace(fromCode))
        {
            return fromCode;
        }

        var fromContent = ExtractAttemptCode(payload.Content);
        if (!string.IsNullOrWhiteSpace(fromContent))
        {
            return fromContent;
        }

        return ExtractAttemptCode(payload.Description);
    }

    private static string? ExtractAttemptCode(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var normalized = text.Trim();
        return ExtractToken(normalized, "SPX")
            ?? ExtractToken(normalized, "WLT");
    }

    private static string? NormalizeAttemptCode(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var value = raw.Trim();
        if (!value.StartsWith("SPX", StringComparison.OrdinalIgnoreCase)
            && !value.StartsWith("WLT", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = ExtractLeadingToken(value);
        return string.IsNullOrWhiteSpace(token) ? null : token;
    }

    private static string ExtractLeadingToken(string text)
    {
        var end = 0;
        while (end < text.Length)
        {
            var ch = text[end];
            if (!(char.IsLetterOrDigit(ch) || ch == '_' || ch == '-'))
            {
                break;
            }

            end++;
        }

        return text[..end];
    }

    private static string? ExtractToken(string text, string prefix)
    {
        var index = text.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return null;
        }

        var token = ExtractLeadingToken(text[index..]);
        return NormalizeAttemptCode(token);
    }

    private async Task<bool> TryHandleWalletTopUpFromAttemptCacheAsync(
        string idempotencyKey,
        SePayWebhookPayload payload,
        CancellationToken ct)
    {
        var amount = payload.TransferAmount;
        var cacheKey = BuildTopUpAttemptKey(idempotencyKey);
        var rawAttempt = await _redisService.GetAsync(cacheKey);
        if (string.IsNullOrWhiteSpace(rawAttempt))
        {
            return false;
        }

        WalletTopUpAttemptCache? topUpAttempt;
        try
        {
            topUpAttempt = JsonSerializer.Deserialize<WalletTopUpAttemptCache>(rawAttempt);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "WLT webhook: invalid top-up cache for key '{Key}'", idempotencyKey);
            return true;
        }

        if (topUpAttempt == null)
        {
            _logger.LogWarning("WLT webhook: top-up attempt '{Key}' cannot be parsed", idempotencyKey);
            return true;
        }

        if (string.Equals(topUpAttempt.Status, "PAID", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("WLT webhook: attempt '{Key}' already marked as PAID", idempotencyKey);
            return false;
        }

        var diff = Math.Abs(amount - topUpAttempt.Amount);
        if (diff > 1)
        {
            topUpAttempt.Status = "FAILED";
            topUpAttempt.CompletedAt = _timeProvider.UtcNow;
            await _redisService.SetAsync(cacheKey, JsonSerializer.Serialize(topUpAttempt), TimeSpan.FromHours(24));

            _logger.LogWarning(
                "WLT webhook: amount mismatch for key '{Key}' expected {Expected}, got {Actual}",
                idempotencyKey,
                topUpAttempt.Amount,
                amount);
            return true;
        }

        var wallet = await _uow.Orders.GetWalletByAccountIdAsync(topUpAttempt.AccountId, ct);
        if (wallet == null || wallet.WalletId != topUpAttempt.WalletId)
        {
            _logger.LogWarning(
                "WLT webhook: wallet not found/mismatch for key '{Key}' account {AccountId}",
                idempotencyKey,
                topUpAttempt.AccountId);
            return true;
        }

        var now = _timeProvider.UtcNow;
        var rawCallback = JsonSerializer.Serialize(payload);
        var webhookTxnNo = ResolveWebhookTransactionNo(payload);
        await _uow.BeginTransactionAsync(ct);
        try
        {
            var existingWalletTransaction = await _db.WalletTransactions
                .FirstOrDefaultAsync(wt => wt.IdempotencyKey == idempotencyKey, ct);

            var paymentGatewayTxn = await _uow.Orders.GetPaymentTransactionByRequestIdAsync(idempotencyKey, ct);
            if (paymentGatewayTxn == null)
            {
                var topUpOrder = await GetOrCreateWalletTopUpShadowOrderAsync(topUpAttempt, idempotencyKey, now, ct);
                paymentGatewayTxn = new PaymentGatewayTransaction
                {
                    OrderId = topUpOrder.OrderId,
                    Provider = "SE_PAY",
                    RequestId = idempotencyKey,
                    Amount = topUpAttempt.Amount,
                    Status = "Pending",
                    RetryCount = 0,
                    CreatedAt = now
                };
                await _db.PaymentGatewayTransactions.AddAsync(paymentGatewayTxn, ct);
            }

            WalletTransaction? walletTransaction = existingWalletTransaction;
            var balanceAfter = wallet.Balance;
            var createdWalletTransaction = false;
            if (existingWalletTransaction == null)
            {
                var balanceBefore = wallet.Balance;
                wallet.Balance += topUpAttempt.Amount;
                balanceAfter = wallet.Balance;

                walletTransaction = new WalletTransaction
                {
                    WalletId = wallet.WalletId,
                    AccountId = topUpAttempt.AccountId,
                    TxnType = "TopUp",
                    Direction = "CR",
                    Amount = topUpAttempt.Amount,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = balanceAfter,
                    Method = "BankTransfer",
                    Reason = "Top up via Bank Transfer (SePay)",
                    IdempotencyKey = idempotencyKey,
                    Status = "Completed",
                    CreatedAt = now,
                    CompletedAt = now
                };

                await _uow.Orders.AddWalletTransactionAsync(walletTransaction, ct);
                createdWalletTransaction = true;
            }

            paymentGatewayTxn.TransactionNo = paymentGatewayTxn.TransactionNo ?? webhookTxnNo;
            paymentGatewayTxn.ResponseCode = "00";
            paymentGatewayTxn.ResponseMessage = "Transaction Success";
            paymentGatewayTxn.Status = "Paid";
            paymentGatewayTxn.RawCallback = rawCallback;
            paymentGatewayTxn.UpdatedAt = now;

            await _uow.SaveChangesAsync(ct);
            await _uow.CommitTransactionAsync(ct);

            topUpAttempt.Status = "PAID";
            topUpAttempt.WalletTransactionId = walletTransaction?.WalletTransactionId;
            topUpAttempt.CompletedAt = now;
            await _redisService.SetAsync(cacheKey, JsonSerializer.Serialize(topUpAttempt), TimeSpan.FromHours(24));

            if (createdWalletTransaction && walletTransaction != null)
            {
                await _eventPublisher.PublishAsync(
                    "Wallet",
                    walletTransaction.WalletTransactionId.ToString(),
                    NotificationEventTypes.WalletTopup,
                    new
                    {
                        accountId = topUpAttempt.AccountId,
                        amount = topUpAttempt.Amount,
                        balanceAfter,
                        walletTransactionId = walletTransaction.WalletTransactionId
                    },
                    CancellationToken.None);
            }

            _logger.LogInformation(
                "WLT webhook processed: key='{Key}', walletTxnId={WalletTxnId}, amount={Amount}",
                idempotencyKey,
                walletTransaction?.WalletTransactionId,
                topUpAttempt.Amount);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            await _uow.RollbackTransactionAsync(ct);

            var existingWalletTxn = await _db.WalletTransactions
                .AsNoTracking()
                .FirstOrDefaultAsync(wt => wt.IdempotencyKey == idempotencyKey, ct);
            if (existingWalletTxn != null)
            {
                topUpAttempt.Status = "PAID";
                topUpAttempt.WalletTransactionId = existingWalletTxn.WalletTransactionId;
                topUpAttempt.CompletedAt = _timeProvider.UtcNow;
                await _redisService.SetAsync(cacheKey, JsonSerializer.Serialize(topUpAttempt), TimeSpan.FromHours(24));
                _logger.LogInformation("WLT webhook: duplicate callback handled for key '{Key}'", idempotencyKey);
                return true;
            }

            throw;
        }
        catch
        {
            await _uow.RollbackTransactionAsync(ct);
            throw;
        }

        return true;
    }

    private async Task<bool> TryBackfillWalletTopUpGatewayTransactionAsync(
        string idempotencyKey,
        SePayWebhookPayload payload,
        CancellationToken ct)
    {
        var walletTransaction = await _db.WalletTransactions
            .FirstOrDefaultAsync(wt => wt.IdempotencyKey == idempotencyKey, ct);
        if (walletTransaction == null)
        {
            return false;
        }

        var now = _timeProvider.UtcNow;
        var rawCallback = JsonSerializer.Serialize(payload);
        var webhookTxnNo = ResolveWebhookTransactionNo(payload);

        await _uow.BeginTransactionAsync(ct);
        try
        {
            var gatewayTxn = await _uow.Orders.GetPaymentTransactionByRequestIdAsync(idempotencyKey, ct);
            if (gatewayTxn == null)
            {
                var fallbackAttempt = new WalletTopUpAttemptCache
                {
                    AttemptCode = idempotencyKey,
                    AccountId = walletTransaction.AccountId,
                    WalletId = walletTransaction.WalletId,
                    Amount = walletTransaction.Amount,
                    Status = "PAID",
                    WalletTransactionId = walletTransaction.WalletTransactionId,
                    CreatedAt = walletTransaction.CreatedAt,
                    CompletedAt = walletTransaction.CompletedAt
                };

                var topUpOrder = await GetOrCreateWalletTopUpShadowOrderAsync(fallbackAttempt, idempotencyKey, now, ct);
                gatewayTxn = new PaymentGatewayTransaction
                {
                    OrderId = topUpOrder.OrderId,
                    Provider = "SE_PAY",
                    RequestId = idempotencyKey,
                    Amount = walletTransaction.Amount,
                    Status = "Pending",
                    RetryCount = 0,
                    CreatedAt = now
                };
                await _db.PaymentGatewayTransactions.AddAsync(gatewayTxn, ct);
            }

            gatewayTxn.TransactionNo = gatewayTxn.TransactionNo ?? webhookTxnNo;
            gatewayTxn.ResponseCode = "00";
            gatewayTxn.ResponseMessage = "Transaction Success";
            gatewayTxn.Status = "Paid";
            gatewayTxn.RawCallback = rawCallback;
            gatewayTxn.UpdatedAt = now;

            await _uow.SaveChangesAsync(ct);
            await _uow.CommitTransactionAsync(ct);

            _logger.LogInformation("WLT webhook backfill: gateway transaction ensured for key '{Key}'", idempotencyKey);
            return true;
        }
        catch
        {
            await _uow.RollbackTransactionAsync(ct);
            throw;
        }
    }

    private async Task<Order> GetOrCreateWalletTopUpShadowOrderAsync(
        WalletTopUpAttemptCache topUpAttempt,
        string idempotencyKey,
        DateTime now,
        CancellationToken ct)
    {
        var orderCode = BuildTopUpShadowOrderCode(idempotencyKey);
        var existingOrder = await _db.Orders
            .FirstOrDefaultAsync(o => o.OrderCode == orderCode, ct);
        if (existingOrder != null)
        {
            return existingOrder;
        }

        var statusMap = await _uow.Orders.GetStatusMapAsync(ct);
        byte statusId = 1;
        if (statusMap.TryGetValue("Confirmed", out var confirmedStatusId))
        {
            statusId = confirmedStatusId;
        }
        else if (statusMap.TryGetValue("Pending", out var pendingStatusId))
        {
            statusId = pendingStatusId;
        }

        var shadowOrder = new Order
        {
            AccountId = topUpAttempt.AccountId,
            StatusId = statusId,
            OrderCode = orderCode,
            ShippingName = "Wallet Top-up",
            ShippingPhone = "0000000000",
            ShippingAddress = "SePay Wallet Top-up",
            ShippingWardCode = "TOPUP",
            ShippingWardName = "TOPUP",
            ShippingDistrictId = 0,
            ShippingDistrictName = "TOPUP",
            ShippingProvinceId = 0,
            ShippingProvinceName = "TOPUP",
            PaymentMethod = "SE_PAY",
            PaymentStatus = "PAID",
            SubTotal = topUpAttempt.Amount,
            VoucherDiscountAmount = 0,
            EstimatedShippingFee = 0,
            TotalAmount = topUpAttempt.Amount,
            Note = $"Wallet top-up via SePay ({idempotencyKey})",
            OrderDate = now,
            PaidAt = now,
            ConfirmedAt = now,
            IsDeleted = true,
            CreatedAt = now
        };

        await _db.Orders.AddAsync(shadowOrder, ct);
        await _uow.SaveChangesAsync(ct);
        return shadowOrder;
    }

    private static string BuildTopUpShadowOrderCode(string idempotencyKey)
    {
        var normalized = new string((idempotencyKey ?? string.Empty)
            .Where(char.IsLetterOrDigit)
            .ToArray())
            .ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = "WLTTOPUP";
        }

        if (!normalized.StartsWith("WLT", StringComparison.OrdinalIgnoreCase))
        {
            normalized = $"WLT{normalized}";
        }

        return normalized.Length <= 30
            ? normalized
            : normalized[..30];
    }

    private static string? ResolveWebhookTransactionNo(SePayWebhookPayload payload)
    {
        if (!string.IsNullOrWhiteSpace(payload.ReferenceCode))
        {
            return payload.ReferenceCode.Trim();
        }

        if (!string.IsNullOrWhiteSpace(payload.Code))
        {
            return payload.Code.Trim();
        }

        return payload.Id?.ToString();
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true;

    private static string BuildTopUpAttemptKey(string attemptCode)
        => $"{TopUpAttemptPrefix}{attemptCode.ToUpperInvariant()}";

    private static void ApplyPaidOrderItemsToCart(
        Cart cart,
        IEnumerable<OrderDetail> paidOrderDetails,
        DateTime now)
    {
        var remainingByProductId = paidOrderDetails
            .GroupBy(d => d.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(d => (int)d.Quantity));

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

    private sealed class WalletTopUpAttemptCache
    {
        public string AttemptCode { get; set; } = string.Empty;
        public int AccountId { get; set; }
        public int WalletId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = "PENDING";
        public int? WalletTransactionId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

}
