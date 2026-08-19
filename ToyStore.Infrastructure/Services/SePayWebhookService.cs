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
    private readonly IWalletRefundCreditor _walletRefundCreditor;

    public SePayWebhookService(
        IUnitOfWork uow,
        SEP490ToyStoreContext db,
        IGhnClient ghnClient,
        IRedisService redisService,
        IOptions<SePayOptions> sePayOpts,
        IOptions<ShopAddressOptions> shopAddr,
        IDomainEventPublisher eventPublisher,
        ILogger<SePayWebhookService> logger,
        ITimeProvider timeProvider,
        IWalletRefundCreditor walletRefundCreditor)
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
        _walletRefundCreditor = walletRefundCreditor;
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
        // BƯỚC 1: Tìm kiếm giao dịch thanh toán trong hệ thống theo mã đối chiếu (attemptCode)
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

        // BƯỚC 2: Kiểm tra tính trùng lặp (Idempotency Check)
        // Nếu giao dịch này đã được ghi nhận thành công từ trước, bỏ qua để tránh cộng tiền hay xử lý lại.
        if (txn.Status == "Paid")
        {
            _logger.LogInformation("SPX webhook: attempt '{Code}' already Paid — skip", attemptCode);
            return;
        }

        var order = txn.Order;

        // BƯỚC 3: Xử lý Late Payment (Thanh toán trễ khi đơn hàng đã bị hủy do timeout)
        // Nếu đơn hàng đã bị hủy, hệ thống tự động hoàn tiền tương ứng vào ví điện tử của khách hàng và cảnh báo CS.
        if (order.CancelledAt.HasValue)
        {
            _logger.LogWarning(
                "SPX webhook: Order {Code} is cancelled — late payment received, initiating auto refund",
                order.OrderCode);

            await HandleLatePaymentAfterCancelAsync(order, attemptCode, amount, payload, ct);
            return;
        }

        var now = _timeProvider.UtcNow;

        // BƯỚC 4: Kiểm tra số tiền chuyển khoản (Verify amount)
        // Nếu số tiền khách chuyển nhỏ hơn tổng số tiền đơn hàng (cho phép lệch tối đa 1đ), đánh dấu giao dịch thất bại.
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

        // Kiểm tra xem khách có thanh toán dư tiền (overpay) không
        bool isOverpay = amount > order.TotalAmount + 1;

        // BƯỚC 5: Lấy ID trạng thái 'Confirmed' để cập nhật cho đơn hàng
        var statusMap = await _uow.Orders.GetStatusMapAsync(ct);
        statusMap.TryGetValue("Confirmed", out var confirmedStatusId);

        // ── KHỞI ĐẦU DATABASE TRANSACTION ──────────────────────────────────────────
        await _uow.BeginTransactionAsync(ct);
        try
        {
            // BƯỚC 5a: Chuyển đổi tồn kho Flash Sale từ trạng thái tạm giữ (Reserved) sang thực bán (Sold).
            // Do kho chung đã bị trừ và kho Flash sale đã được tạm giữ lúc tạo đơn (CheckoutService.ConfirmAsync),
            // nên lúc này chỉ cần cập nhật kho Flash Sale.
            foreach (var detail in order.OrderDetails)
            {
                if (detail.SlotProductId.HasValue)
                {
                    // Trừ ReservedQuantity và cộng vào SoldQuantity tương ứng
                    await _uow.Orders.AdjustFlashSaleStockAsync(detail.SlotProductId.Value, (int)detail.Quantity, -(int)detail.Quantity, ct);
                }
            }

            // BƯỚC 5b: Cập nhật trạng thái giao dịch cổng thanh toán thành công
            txn.Status = "Paid";
            txn.UpdatedAt = now;
            txn.ResponseCode = "00";
            txn.ResponseMessage = "Success";

            // BƯỚC 5c: Cập nhật trạng thái đơn hàng thành PAID và CONFIRMED
            order.PaymentStatus = "PAID";
            order.PaidAt = now;
            order.UpdatedAt = now;
            if (confirmedStatusId > 0)
            {
                order.StatusId = confirmedStatusId;
                order.ConfirmedAt = now;
            }

            // BƯỚC 5d: Ghi nhận lịch sử thanh toán thành công (PaymentHistory)
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
            await _uow.SaveChangesAsync(ct); // Lấy PaymentHistoryId

            txn.PaymentHistoryId = payHistory.PaymentHistoryId;

            // BƯỚC 5e: Ghi nhận lịch sử chuyển trạng thái đơn hàng
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

            // BƯỚC 5f: Cộng tiền thừa (Overpay) vào ví điện tử khách hàng nếu có
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
                            Direction = "CR", // Credit (Cộng tiền)
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

            // BƯỚC 5g: Xử lý giỏ hàng.
            // Do SE_PAY giữ nguyên giỏ hàng lúc tạo đơn (phòng trường hợp quét QR thất bại hoặc bỏ dở thanh toán),
            // nên bây giờ mới tiến hành xóa các mặt hàng đã mua khỏi giỏ hàng.
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

        // Phát sự kiện xác nhận và sẵn sàng đóng gói đơn hàng
        var orderPayload = new { orderId = order.OrderId, orderCode = order.OrderCode };
        await _eventPublisher.PublishAsync("Order", order.OrderId.ToString(),
            NotificationEventTypes.OrderConfirmed,
            orderPayload,
            CancellationToken.None);

        await _eventPublisher.PublishAsync("Order", order.OrderId.ToString(),
            NotificationEventTypes.MerchReadyToPack,
            orderPayload,
            CancellationToken.None);
    }

    // ── WLT_: Nạp ví ─────────────────────────────────────────────────────────

    /// <summary>
    /// Điều hướng xử lý webhook nạp tiền vào ví (tiền tố WLT).
    /// Thử lần lượt 3 cách: 1. Redis Cache -> 2. Backfill Gateway Txn -> 3. Fallback theo AccountId.
    /// </summary>
    private async Task HandleWalletTopUpAsync(string idempotencyKey, SePayWebhookPayload payload, CancellationToken ct)
    {
        var amount = payload.TransferAmount;

        // 1. Ưu tiên xử lý từ dữ liệu nạp tiền lưu trong Redis Cache (tạo từ QR trong vòng 24h)
        if (await TryHandleWalletTopUpFromAttemptCacheAsync(idempotencyKey, payload, ct))
        {
            return;
        }

        // 2. Nếu đã cộng tiền ví rồi nhưng chưa ghi nhận giao dịch cổng thanh toán thì ghi bổ sung (backfill)
        if (await TryBackfillWalletTopUpGatewayTransactionAsync(idempotencyKey, payload, ct))
        {
            return;
        }

        // 3. Dự phòng khi Redis hết hạn (>24h): Trích xuất AccountId trực tiếp từ mã WLT (ví dụ: WLT_123_abc)
        int? resolvedAccountId = null;
        if (idempotencyKey.Contains('_'))
        {
            var parts = idempotencyKey.Split('_');
            if (parts.Length >= 2 && string.Equals(parts[0], "WLT", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(parts[1], out var accId))
                {
                    resolvedAccountId = accId;
                }
            }
        }
        else if (idempotencyKey.StartsWith("WLT", StringComparison.OrdinalIgnoreCase) && idempotencyKey.Length > 11)
        {
            // Định dạng không gạch dưới: WLT + AccountId + UUID (8 ký tự hex)
            var accIdStr = idempotencyKey[3..^8];
            if (int.TryParse(accIdStr, out var accId))
            {
                resolvedAccountId = accId;
            }
        }

        // Nếu lấy được AccountId, gọi hàm fallback để cộng tiền trực tiếp vào ví
        if (resolvedAccountId.HasValue)
        {
            if (await TryHandleWalletTopUpFallbackAsync(resolvedAccountId.Value, idempotencyKey, payload, ct))
            {
                return;
            }
        }

        _logger.LogWarning("WLT webhook fallback: unable to resolve AccountId from key '{Key}', amount {Amt}", idempotencyKey, amount);
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

    /// <summary>
    /// Xử lý nạp ví từ Redis cache (luồng chính khi quét QR nạp tiền trong 24h).
    /// </summary>
    private async Task<bool> TryHandleWalletTopUpFromAttemptCacheAsync(
        string idempotencyKey,
        SePayWebhookPayload payload,
        CancellationToken ct)
    {
        var amount = payload.TransferAmount;
        var cacheKey = BuildTopUpAttemptKey(idempotencyKey);
        
        // BƯỚC 1: Lấy thông tin yêu cầu nạp tiền từ Redis cache
        var rawAttempt = await _redisService.GetAsync(cacheKey);
        if (string.IsNullOrWhiteSpace(rawAttempt))
        {
            return false; // Không thấy thông tin trong cache -> bỏ qua để sang luồng khác
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

        // BƯỚC 2: Kiểm tra chống lặp (Idempotency) - Nếu đã PAID thì không cộng lại
        if (string.Equals(topUpAttempt.Status, "PAID", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("WLT webhook: attempt '{Key}' already marked as PAID", idempotencyKey);
            return false;
        }

        // BƯỚC 3: Kiểm tra số tiền nhận được có khớp với số tiền tạo lệnh nạp không (cho phép lệch tối đa 1đ)
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

        // BƯỚC 4: Tìm ví của người dùng theo AccountId
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

        // BƯỚC 5: Khởi tạo Database Transaction để cộng tiền và lưu thông tin
        await _uow.BeginTransactionAsync(ct);
        try
        {
            var existingWalletTransaction = await _db.WalletTransactions
                .FirstOrDefaultAsync(wt => wt.IdempotencyKey == idempotencyKey, ct);

            // Tạo giao dịch cổng thanh toán (PaymentGatewayTransaction) và đơn bóng (Shadow Order) nếu chưa có
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

            // Nếu chưa có lịch sử ví -> Cộng tiền vào số dư ví và ghi nhận WalletTransaction (Credit)
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

            // Cập nhật trạng thái cổng thanh toán thành Paid
            paymentGatewayTxn.TransactionNo = paymentGatewayTxn.TransactionNo ?? webhookTxnNo;
            paymentGatewayTxn.ResponseCode = "00";
            paymentGatewayTxn.ResponseMessage = "Transaction Success";
            paymentGatewayTxn.Status = "Paid";
            paymentGatewayTxn.RawCallback = rawCallback;
            paymentGatewayTxn.UpdatedAt = now;

            await _uow.SaveChangesAsync(ct);
            await _uow.CommitTransactionAsync(ct);

            // BƯỚC 6: Cập nhật trạng thái Redis cache thành PAID
            topUpAttempt.Status = "PAID";
            topUpAttempt.WalletTransactionId = walletTransaction?.WalletTransactionId;
            topUpAttempt.CompletedAt = now;
            await _redisService.SetAsync(cacheKey, JsonSerializer.Serialize(topUpAttempt), TimeSpan.FromHours(24));

            // BƯỚC 7: Bắn sự kiện (Event) thông báo cho hệ thống / người dùng nạp tiền thành công
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
            // Xử lý xung đột ghi lặp (Duplicate callback)
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

    /// <summary>
    /// Ghi bổ sung (backfill) giao dịch cổng thanh toán nếu ví đã được cộng tiền thành công trước đó nhưng chưa lưu record cổng thanh toán.
    /// </summary>
    private async Task<bool> TryBackfillWalletTopUpGatewayTransactionAsync(
        string idempotencyKey,
        SePayWebhookPayload payload,
        CancellationToken ct)
    {
        // 1. Kiểm tra xem giao dịch ví đã tồn tại chưa
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
            // 2. Tạo đơn bóng và record giao dịch cổng thanh toán nếu chưa có
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

            // 3. Cập nhật trạng thái cổng thanh toán thành Paid
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

    /// <summary>
    /// Tạo hoặc lấy Đơn hàng bóng (Shadow Order) để liên kết lịch sử nạp ví với bảng cổng thanh toán.
    /// </summary>
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

        // Đơn hàng bóng có thông tin giả định và đánh dấu IsDeleted = true để ẩn khỏi danh sách đơn hàng mua sắm
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

    /// <summary>
    /// Tạo mã đơn hàng bóng chuẩn hóa từ mã nạp ví (Ví dụ: WLT123ABC).
    /// </summary>
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

    private async Task HandleLatePaymentAfterCancelAsync(
        Order order,
        string attemptCode,
        decimal amount,
        SePayWebhookPayload payload,
        CancellationToken ct)
    {
        var idempotencyKey = $"LATE_SEPAY_{attemptCode}";
        var credited = await _walletRefundCreditor.CreditRefundAsync(
            order.AccountId,
            amount,
            order.OrderCode,
            order.OrderId,
            ct,
            idempotencyKey);

        await _eventPublisher.PublishAsync(
            "Payment",
            order.OrderId.ToString(),
            NotificationEventTypes.SystemPaymentGatewayError,
            new
            {
                orderId = order.OrderId,
                orderCode = order.OrderCode,
                attemptCode,
                amount,
                credited,
                note = "Late SE_PAY after order cancelled — auto wallet refund attempted"
            },
            CancellationToken.None);
    }

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

    /// <summary>
    /// Luồng xử lý dự phòng nạp ví khi Redis cache đã hết hạn (>24h).
    /// Tự lấy AccountId từ mã giao dịch để cộng tiền vào ví và lưu thông tin.
    /// </summary>
    private async Task<bool> TryHandleWalletTopUpFallbackAsync(
        int accountId,
        string idempotencyKey,
        SePayWebhookPayload payload,
        CancellationToken ct)
    {
        var amount = payload.TransferAmount;
        var now = _timeProvider.UtcNow;
        var rawCallback = JsonSerializer.Serialize(payload);
        var webhookTxnNo = ResolveWebhookTransactionNo(payload);

        // BƯỚC 1: Tìm ví của tài khoản
        var wallet = await _uow.Orders.GetWalletByAccountIdAsync(accountId, ct);
        if (wallet == null)
        {
            _logger.LogWarning("WLT fallback: wallet not found for AccountId {AccountId}, key '{Key}'", accountId, idempotencyKey);
            return false;
        }

        await _uow.BeginTransactionAsync(ct);
        try
        {
            // BƯỚC 2: Kiểm tra chống lặp (Idempotency) - Nếu đã có giao dịch ví thì không tạo trùng
            var existingWalletTransaction = await _db.WalletTransactions
                .FirstOrDefaultAsync(wt => wt.IdempotencyKey == idempotencyKey, ct);

            var paymentGatewayTxn = await _uow.Orders.GetPaymentTransactionByRequestIdAsync(idempotencyKey, ct);
            if (paymentGatewayTxn == null)
            {
                // Dùng dữ liệu tạm để tạo Đơn bóng (Shadow Order) và Record cổng thanh toán
                var tempAttempt = new WalletTopUpAttemptCache
                {
                    AttemptCode = idempotencyKey,
                    AccountId = accountId,
                    WalletId = wallet.WalletId,
                    Amount = amount,
                    Status = "PAID",
                    CreatedAt = now,
                    CompletedAt = now
                };

                var topUpOrder = await GetOrCreateWalletTopUpShadowOrderAsync(tempAttempt, idempotencyKey, now, ct);
                paymentGatewayTxn = new PaymentGatewayTransaction
                {
                    OrderId = topUpOrder.OrderId,
                    Provider = "SE_PAY",
                    RequestId = idempotencyKey,
                    Amount = amount,
                    Status = "Pending",
                    RetryCount = 0,
                    CreatedAt = now
                };
                await _db.PaymentGatewayTransactions.AddAsync(paymentGatewayTxn, ct);
            }

            WalletTransaction? walletTransaction = existingWalletTransaction;
            var balanceAfter = wallet.Balance;
            var createdWalletTransaction = false;

            // BƯỚC 3: Nếu chưa có giao dịch ví -> Cộng tiền ví và lưu WalletTransaction (Credit)
            if (existingWalletTransaction == null)
            {
                var balanceBefore = wallet.Balance;
                wallet.Balance += amount;
                balanceAfter = wallet.Balance;

                walletTransaction = new WalletTransaction
                {
                    WalletId = wallet.WalletId,
                    AccountId = accountId,
                    TxnType = "TopUp",
                    Direction = "CR",
                    Amount = amount,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = balanceAfter,
                    Method = "BankTransfer",
                    Reason = "Top up via Bank Transfer (SePay fallback)",
                    IdempotencyKey = idempotencyKey,
                    Status = "Completed",
                    CreatedAt = now,
                    CompletedAt = now
                };

                await _uow.Orders.AddWalletTransactionAsync(walletTransaction, ct);
                createdWalletTransaction = true;
            }

            // BƯỚC 4: Cập nhật trạng thái cổng thanh toán thành Paid
            paymentGatewayTxn.TransactionNo = paymentGatewayTxn.TransactionNo ?? webhookTxnNo;
            paymentGatewayTxn.ResponseCode = "00";
            paymentGatewayTxn.ResponseMessage = "Transaction Success";
            paymentGatewayTxn.Status = "Paid";
            paymentGatewayTxn.RawCallback = rawCallback;
            paymentGatewayTxn.UpdatedAt = now;

            await _uow.SaveChangesAsync(ct);
            await _uow.CommitTransactionAsync(ct);

            // BƯỚC 5: Bắn sự kiện (Event) thông báo nạp ví thành công nếu là giao dịch mới
            if (createdWalletTransaction && walletTransaction != null)
            {
                await _eventPublisher.PublishAsync(
                    "Wallet",
                    walletTransaction.WalletTransactionId.ToString(),
                    NotificationEventTypes.WalletTopup,
                    new
                    {
                        accountId = accountId,
                        amount = amount,
                        balanceAfter,
                        walletTransactionId = walletTransaction.WalletTransactionId
                    },
                    CancellationToken.None);
            }

            _logger.LogInformation(
                "WLT fallback processed: key='{Key}', walletTxnId={WalletTxnId}, amount={Amount}",
                idempotencyKey,
                walletTransaction?.WalletTransactionId,
                amount);

            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            await _uow.RollbackTransactionAsync(ct);
            _logger.LogWarning("WLT fallback: unique constraint violation for key '{Key}'", idempotencyKey);
            return true;
        }
        catch (Exception ex)
        {
            await _uow.RollbackTransactionAsync(ct);
            _logger.LogError(ex, "WLT fallback: failed to process fallback top-up for key '{Key}'", idempotencyKey);
            throw;
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
