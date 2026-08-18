using Microsoft.Extensions.Logging;
using ToyStore.Application.Constants;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Constants;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Enums;

namespace ToyStore.Infrastructure.Services;

/// <summary>
/// Service xử lý việc cộng tiền hoàn trả vào Ví (Customer Wallet) của khách hàng một cách an toàn và đảm bảo Idempotency (chống cộng trùng).
/// </summary>
public class WalletRefundCreditorService : IWalletRefundCreditor
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WalletRefundCreditorService> _logger;
    private readonly ITimeProvider _timeProvider;

    /// <summary>
    /// Khởi tạo WalletRefundCreditorService với các dependency cần thiết.
    /// </summary>
    public WalletRefundCreditorService(
        IUnitOfWork unitOfWork,
        ILogger<WalletRefundCreditorService> logger,
        ITimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Thực hiện cộng tiền hoàn trả vào ví của khách hàng:
    /// 1. Kiểm tra số tiền hợp lệ (> 0).
    /// 2. Tạo khóa Idempotency Key chuẩn hóa REFUND_{orderCode} và kiểm tra trùng lặp giao dịch hoàn tiền.
    /// 3. Nếu khách hàng chưa có ví -> tự động tạo ví mới và ghi nhận số dư ban đầu.
    /// 4. Nếu khách hàng đã có ví -> cộng dồn số dư ví hiện tại.
    /// 5. Tạo bản ghi lịch sử giao dịch ví WalletTransaction với trạng thái Completed.
    /// </summary>
    /// <param name="accountId">Mã ID tài khoản khách hàng.</param>
    /// <param name="amount">Số tiền cần hoàn vào ví (VNĐ).</param>
    /// <param name="orderCode">Mã code đơn hàng được hoàn tiền.</param>
    /// <param name="relatedOrderId">Mã ID đơn hàng liên quan.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <param name="idempotencyKey">Khóa Idempotency chống trùng giao dịch (nếu có).</param>
    /// <returns>true nếu cộng tiền thành công hoặc đã hoàn trước đó; false nếu số tiền không hợp lệ.</returns>
    public async Task<bool> CreditRefundAsync(
        int accountId,
        decimal amount,
        string orderCode,
        int? relatedOrderId = null,
        CancellationToken cancellationToken = default,
        string? idempotencyKey = null)
    {
        // 1. Kiểm tra số tiền hoàn phải lớn hơn 0
        if (amount <= 0)
            return false;

        var keyToUse = !string.IsNullOrWhiteSpace(idempotencyKey)
            ? idempotencyKey
            : WalletRefundKeys.ForOrder(orderCode);

        // 2. Nếu không có custom idempotencyKey (nghĩa là luồng hoàn tiền đơn hàng chính):
        // Kiểm tra xem đơn hàng đã từng có giao dịch cộng tiền hoàn hoàn tất trước đó chưa
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            if (relatedOrderId.HasValue
                && await _unitOfWork.Orders.HasCompletedRefundWalletCreditForOrderAsync(relatedOrderId.Value, cancellationToken))
            {
                _logger.LogInformation(
                    "Refund wallet credit skipped — order {OrderId} already has a completed refund txn",
                    relatedOrderId.Value);
                return true;
            }
        }

        // Kiểm tra trùng IdempotencyKey
        if (await _unitOfWork.Orders.ExistsWalletTransactionByIdempotencyKeyAsync(keyToUse, cancellationToken))
        {
            _logger.LogInformation("Refund wallet credit skipped (duplicate key {Key}) for order {OrderCode}", keyToUse, orderCode);
            return true;
        }

        idempotencyKey = keyToUse;

        // 3. Lấy thông tin ví của khách hàng
        var wallet = await _unitOfWork.Wallets.GetByAccountIdAsync(accountId, cancellationToken);
        var now = _timeProvider.UtcNow;

        if (wallet is null)
        {
            // Tự động khởi tạo ví mới nếu khách hàng chưa có ví
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

            // Ghi nhận giao dịch hoàn tiền vào bảng WalletTransaction
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

        // 4. Khách hàng đã có ví: cập nhật cộng số dư ví
        var balanceBefore = wallet.Balance;
        wallet.Balance += amount;
        wallet.LastTransactionAt = now;
        wallet.UpdatedAt = now;
        _unitOfWork.Wallets.UpdateWallet(wallet);

        // Ghi nhận giao dịch hoàn tiền vào bảng WalletTransaction
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
