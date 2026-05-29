using Microsoft.Extensions.Logging;
using ToyStore.Application.Constants;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Constants;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Enums;

namespace ToyStore.Infrastructure.Services;

public class WalletRefundCreditorService : IWalletRefundCreditor
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WalletRefundCreditorService> _logger;
    private readonly ITimeProvider _timeProvider;

    public WalletRefundCreditorService(
        IUnitOfWork unitOfWork,
        ILogger<WalletRefundCreditorService> logger,
        ITimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<bool> CreditRefundAsync(
        int accountId,
        decimal amount,
        string orderCode,
        int? relatedOrderId = null,
        CancellationToken cancellationToken = default,
        string? idempotencyKey = null)
    {
        if (amount <= 0)
            return false;

        var canonicalKey = WalletRefundKeys.ForOrder(orderCode);
        if (relatedOrderId.HasValue
            && await _unitOfWork.Orders.HasCompletedRefundWalletCreditForOrderAsync(relatedOrderId.Value, cancellationToken))
        {
            _logger.LogInformation(
                "Refund wallet credit skipped — order {OrderId} already has a completed refund txn",
                relatedOrderId.Value);
            return true;
        }

        if (await _unitOfWork.Orders.ExistsWalletTransactionByIdempotencyKeyAsync(canonicalKey, cancellationToken))
        {
            _logger.LogInformation("Refund wallet credit skipped (duplicate key) for order {OrderCode}", orderCode);
            return true;
        }

        if (!string.IsNullOrEmpty(idempotencyKey)
            && !string.Equals(idempotencyKey, canonicalKey, StringComparison.Ordinal)
            && await _unitOfWork.Orders.ExistsWalletTransactionByIdempotencyKeyAsync(idempotencyKey, cancellationToken))
        {
            _logger.LogInformation(
                "Refund wallet credit skipped (legacy key {LegacyKey}) for order {OrderCode}",
                idempotencyKey, orderCode);
            return true;
        }

        idempotencyKey = canonicalKey;

        var wallet = await _unitOfWork.Wallets.GetByAccountIdAsync(accountId, cancellationToken);
        var now = _timeProvider.UtcNow;

        if (wallet is null)
        {
            wallet = new Wallet
            {
                AccountId = accountId,
                Currency = "VND",
                Balance = amount,
                Status = "Active",
                CreatedAt = now,
                LastTransactionAt = now,
                UpdatedAt = now
            };
            await _unitOfWork.Wallets.CreateAsync(wallet, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _unitOfWork.Orders.AddWalletTransactionAsync(new WalletTransaction
            {
                WalletId = wallet.WalletId,
                AccountId = accountId,
                RelatedOrderId = relatedOrderId,
                TxnType = WalletTxnTypes.Refund,
                Direction = WalletTxnDirections.Credit,
                Amount = amount,
                BalanceBefore = 0,
                BalanceAfter = amount,
                Method = "Internal",
                Reason = $"Refund for order {orderCode}",
                IdempotencyKey = idempotencyKey,
                Status = "Completed",
                CreatedAt = now,
                CompletedAt = now
            }, cancellationToken);

            _logger.LogInformation(
                "Created wallet and credited {Amount} for account {AccountId} order {OrderCode}",
                amount, accountId, orderCode);
            return true;
        }

        var balanceBefore = wallet.Balance;
        wallet.Balance += amount;
        wallet.LastTransactionAt = now;
        wallet.UpdatedAt = now;
        _unitOfWork.Wallets.UpdateWallet(wallet);

        await _unitOfWork.Orders.AddWalletTransactionAsync(new WalletTransaction
        {
            WalletId = wallet.WalletId,
            AccountId = accountId,
            RelatedOrderId = relatedOrderId,
            TxnType = WalletTxnTypes.Refund,
            Direction = WalletTxnDirections.Credit,
            Amount = amount,
            BalanceBefore = balanceBefore,
            BalanceAfter = wallet.Balance,
            Method = "Internal",
            Reason = $"Refund for order {orderCode}",
            IdempotencyKey = idempotencyKey,
            Status = "Completed",
            CreatedAt = now,
            CompletedAt = now
        }, cancellationToken);

        _logger.LogInformation(
            "Credited {Amount} to wallet {WalletId} for order {OrderCode}",
            amount, wallet.WalletId, orderCode);
        return true;
    }
}
