using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Application.Constants;
using ToyStore.Domain.Entities;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Infrastructure.Options;

namespace ToyStore.Infrastructure.Services;

/// <summary>
/// Xử lý webhook SE_PAY: phân nhánh SPX_ (thanh toán đơn) và WLT_ (nạp ví).
/// Luôn idempotent — SE_PAY có thể gửi lại cùng webhook nhiều lần.
/// </summary>
public class SePayWebhookService : ISePayWebhookService
{
    private readonly IUnitOfWork _uow;
    private readonly IGhnClient _ghnClient;
    private readonly SePayOptions _sePayOpts;
    private readonly ShopAddressOptions _shopAddr;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ILogger<SePayWebhookService> _logger;
    private readonly ITimeProvider _timeProvider;

    public SePayWebhookService(
        IUnitOfWork uow,
        IGhnClient ghnClient,
        IOptions<SePayOptions> sePayOpts,
        IOptions<ShopAddressOptions> shopAddr,
        IDomainEventPublisher eventPublisher,
        ILogger<SePayWebhookService> logger,
        ITimeProvider timeProvider)
    {
        _uow            = uow;
        _ghnClient      = ghnClient;
        _sePayOpts      = sePayOpts.Value;
        _shopAddr       = shopAddr.Value;
        _eventPublisher = eventPublisher;
        _logger         = logger;
        _timeProvider   = timeProvider;
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
            await HandleWalletTopUpAsync(attemptCode, payload.TransferAmount, cancellationToken);
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
            _logger.LogWarning("SPX webhook: Order {Code} is cancelled — cannot pay, manual refund needed", order.OrderCode);
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
            // 5a. Trừ stock (SE_PAY trừ stock tại đây vì không trừ lúc tạo order)
            foreach (var detail in order.OrderDetails)
            {
                // Trừ stock chung
                await _uow.Products.AdjustStockAsync(detail.ProductId, -detail.Quantity, ct);

                // Nếu là Flash Sale: chuyển từ Reserved sang Sold
                if (detail.SlotProductId.HasValue)
                {
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

            await _uow.SaveChangesAsync(ct);
            await _uow.CommitTransactionAsync(ct);
        }
        catch
        {
            await _uow.RollbackTransactionAsync(ct);
            throw;
        }


        _logger.LogInformation("SPX webhook processed: Order {Code} PAID", order.OrderCode);

        await _eventPublisher.PublishAsync("Order", order.OrderId.ToString(),
            NotificationEventTypes.OrderConfirmed,
            new { orderId = order.OrderId, orderCode = order.OrderCode },
            CancellationToken.None);
    }

    // ── WLT_: Nạp ví ─────────────────────────────────────────────────────────

    private async Task HandleWalletTopUpAsync(string idempotencyKey, decimal amount, CancellationToken ct)
    {
        // Idempotency
        var alreadyExists = await _uow.Orders.ExistsWalletTransactionByIdempotencyKeyAsync(idempotencyKey, ct);
        if (alreadyExists)
        {
            _logger.LogInformation("WLT webhook: idempotencyKey '{Key}' already processed", idempotencyKey);
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
        var content = (payload.Content ?? string.Empty).Trim();
        if (IsPrefixed(content, "SPX") || IsPrefixed(content, "WLT"))
        {
            return content;
        }

        var description = (payload.Description ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(description))
        {
            return null;
        }

        return ExtractToken(description, "SPX")
            ?? ExtractToken(description, "WLT");
    }

    private static string? ExtractToken(string text, string prefix)
    {
        var index = text.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return null;
        }

        var end = index;
        while (end < text.Length)
        {
            var ch = text[end];
            if (char.IsWhiteSpace(ch))
            {
                break;
            }

            end++;
        }

        var token = text.Substring(index, end - index).Trim();
        return string.IsNullOrEmpty(token) ? null : token;
    }

}
