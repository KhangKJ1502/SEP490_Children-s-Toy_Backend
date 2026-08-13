using Microsoft.Extensions.Logging;
using ToyStore.Application.Common;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Enums;

namespace ToyStore.Infrastructure.Services;

public class WithdrawalLedgerService : IWithdrawalLedgerService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<WithdrawalLedgerService> _logger;

    public WithdrawalLedgerService(
        IUnitOfWork uow,
        ILogger<WithdrawalLedgerService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────────────────────────
    // LOCK — creates PENDING withdrawal and reserves LockedBalance
    // ────────────────────────────────────────────────────────────────────────────

    public async Task<(WithdrawalErrorCode Code, WithdrawalRequest? Request)> LockAsync(
        LockWithdrawalCommand command,
        CancellationToken ct = default)
    {
        await _uow.BeginTransactionAsync(ct);
        try
        {
            // BƯỚC 1: Đọc ví và thực hiện khóa dòng dữ liệu (UPDLOCK) để tránh các yêu cầu rút tiền / thanh toán đồng thời tranh chấp số dư
            var wallet = await _uow.Wallets.GetForUpdateAsync(command.AccountId, ct);
            if (wallet is null)
            {
                await _uow.RollbackTransactionAsync(ct);
                return (WithdrawalErrorCode.WalletNotFound, null);
            }

            if (wallet.Status == "Frozen")
            {
                await _uow.RollbackTransactionAsync(ct);
                return (WithdrawalErrorCode.WalletFrozen, null);
            }

            // BƯỚC 2: Tính toán số dư khả dụng thực tế (Available Balance = Balance - LockedBalance)
            // và đối chiếu với số tiền rút. Không được phép rút vào phần LockedBalance (đang bị tạm khóa cho lệnh rút khác).
            var available = wallet.Balance - wallet.LockedBalance;
            if (available < command.Amount)
            {
                await _uow.RollbackTransactionAsync(ct);
                return (WithdrawalErrorCode.InsufficientAvailable, null);
            }

            // BƯỚC 3: Tạm khóa số tiền rút atomically bằng cách cộng dồn vào trường LockedBalance trong database
            var referenceId = $"WD{command.AccountId}{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

            var affected = await _uow.Wallets.IncrementLockedBalanceAsync(command.AccountId, command.Amount, ct);
            if (affected == 0)
            {
                await _uow.RollbackTransactionAsync(ct);
                return (WithdrawalErrorCode.InsufficientAvailable, null);
            }

            // BƯỚC 4: Tạo bản ghi yêu cầu rút tiền với trạng thái PENDING
            var withdrawal = new WithdrawalRequest
            {
                WalletId = wallet.WalletId,
                AccountId = command.AccountId,
                ReferenceId = referenceId,
                Amount = command.Amount,
                ToBankBin = command.ToBankBin,
                ToBankName = command.ToBankName,
                ToAccountNumber = command.ToAccountNumber,
                ToAccountName = command.ToAccountName,
                Status = WithdrawalStatuses.Pending,
                RetryCount = 0,
                CreatedAt = DateTime.UtcNow,
            };
            await _uow.Withdrawals.AddAsync(withdrawal, ct);
            await _uow.SaveChangesAsync(ct);

            // BƯỚC 5: Lưu vết lịch sử thay đổi trạng thái giao dịch
            await _uow.Withdrawals.AddStatusHistoryAsync(
                BuildHistory(withdrawal.WithdrawalId, null, WithdrawalStatuses.Pending, WithdrawalHistorySources.User, "Withdrawal requested"),
                ct);
            await _uow.SaveChangesAsync(ct);

            await _uow.CommitTransactionAsync(ct);

            _logger.LogInformation("Withdrawal {RefId} locked {Amount} for account {AccountId}", referenceId, command.Amount, command.AccountId);
            return (WithdrawalErrorCode.Success, withdrawal);
        }
        catch (Exception ex)
        {
            await _uow.RollbackTransactionAsync(ct);
            _logger.LogError(ex, "LockAsync failed for account {AccountId}", command.AccountId);
            return (WithdrawalErrorCode.SystemError, null);
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // COMMIT — deducts Balance after successful PayOS payout
    // ────────────────────────────────────────────────────────────────────────────

    public async Task<WithdrawalErrorCode> CommitAsync(
        CommitWithdrawalCommand command,
        CancellationToken ct = default)
    {
        await _uow.BeginTransactionAsync(ct);
        try
        {
            var withdrawal = await _uow.Withdrawals.GetForUpdateAsync(command.WithdrawalId, ct);
            if (withdrawal is null)
            {
                await _uow.RollbackTransactionAsync(ct);
                return WithdrawalErrorCode.WalletNotFound;
            }

            // ĐẢM BẢO TÍNH ĐỒNG NHẤT (Idempotency): Nếu yêu cầu rút tiền đã được xác nhận thành công trước đó, bỏ qua không xử lý lại
            if (withdrawal.Status == WithdrawalStatuses.Success)
            {
                await _uow.RollbackTransactionAsync(ct);
                return WithdrawalErrorCode.Success;
            }

            if (withdrawal.Status != WithdrawalStatuses.Pending && withdrawal.Status != WithdrawalStatuses.Processing)
            {
                await _uow.RollbackTransactionAsync(ct);
                return WithdrawalErrorCode.InvalidStatus;
            }

            var wallet = await _uow.Wallets.GetForUpdateAsync(withdrawal.AccountId, ct);
            if (wallet is null)
            {
                await _uow.RollbackTransactionAsync(ct);
                return WithdrawalErrorCode.WalletNotFound;
            }

            var balanceBefore = wallet.Balance;
            var balanceAfter  = balanceBefore - withdrawal.Amount;

            // BƯỚC 1: Thực trừ tiền atomically khỏi ví (Trừ cả số dư tổng Balance và số tiền tạm khóa LockedBalance)
            var affected = await _uow.Wallets.CommitDeductionAsync(wallet.WalletId, withdrawal.Amount, ct);
            if (affected == 0)
            {
                await _uow.RollbackTransactionAsync(ct);
                _logger.LogError("Commit failed — insufficient balance for withdrawal {Id}", command.WithdrawalId);
                return WithdrawalErrorCode.InsufficientAvailable;
            }

            // BƯỚC 2: Thêm bản ghi lịch sử giao dịch ví (WalletTransaction) dạng ghi nợ (DR - Debit)
            var txn = new WalletTransaction
            {
                WalletId = wallet.WalletId,
                AccountId = withdrawal.AccountId,
                TxnType = WalletTxnTypes.Withdrawal,
                Direction = "DR",
                Amount = withdrawal.Amount,
                BalanceBefore = balanceBefore,
                BalanceAfter = balanceAfter,
                Method = "BankTransfer",
                ExternalRef = withdrawal.PayosPayoutId,
                IdempotencyKey = withdrawal.ReferenceId,
                Status = "Completed",
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
            };
            await _uow.Wallets.AddTransactionAsync(txn, ct);
            await _uow.SaveChangesAsync(ct);

            // BƯỚC 3: Cập nhật thông tin giao dịch thành công của yêu cầu rút tiền
            var prevStatus = withdrawal.Status;
            withdrawal.WalletTransactionId = txn.WalletTransactionId;
            withdrawal.Status = WithdrawalStatuses.Success;
            withdrawal.PayosTransactionId = command.PayosTransactionId;
            withdrawal.PayosRawResponse = command.PayosRawResponse;
            withdrawal.CompletedAt = DateTime.UtcNow;
            _uow.Withdrawals.UpdateAsync(withdrawal);
            await _uow.SaveChangesAsync(ct);

            // BƯỚC 4: Ghi nhận lịch sử chuyển đổi trạng thái của yêu cầu
            await _uow.Withdrawals.AddStatusHistoryAsync(
                BuildHistory(withdrawal.WithdrawalId, prevStatus, WithdrawalStatuses.Success, WithdrawalHistorySources.Job, null),
                ct);
            await _uow.SaveChangesAsync(ct);

            await _uow.CommitTransactionAsync(ct);

            _logger.LogInformation("Withdrawal {Id} committed — balance deducted {Amount}", command.WithdrawalId, withdrawal.Amount);
            return WithdrawalErrorCode.Success;
        }
        catch (Exception ex)
        {
            await _uow.RollbackTransactionAsync(ct);
            _logger.LogError(ex, "CommitAsync failed for withdrawal {Id}", command.WithdrawalId);
            return WithdrawalErrorCode.SystemError;
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // ROLLBACK — releases LockedBalance on failure / timeout
    // ────────────────────────────────────────────────────────────────────────────

    public async Task<WithdrawalErrorCode> RollbackAsync(
        RollbackWithdrawalCommand command,
        CancellationToken ct = default)
    {
        await _uow.BeginTransactionAsync(ct);
        try
        {
            var withdrawal = await _uow.Withdrawals.GetForUpdateAsync(command.WithdrawalId, ct);
            if (withdrawal is null)
            {
                await _uow.RollbackTransactionAsync(ct);
                return WithdrawalErrorCode.WalletNotFound;
            }

            // ĐẢM BẢO TÍNH ĐỒNG NHẤT (Idempotency): Nếu giao dịch đã kết thúc (thành công/hủy/lỗi), không xử lý lại
            if (withdrawal.Status is WithdrawalStatuses.Failed or WithdrawalStatuses.Cancelled or WithdrawalStatuses.Success)
            {
                await _uow.RollbackTransactionAsync(ct);
                return WithdrawalErrorCode.Success;
            }

            if (withdrawal.Status != WithdrawalStatuses.Pending && withdrawal.Status != WithdrawalStatuses.Processing)
            {
                await _uow.RollbackTransactionAsync(ct);
                return WithdrawalErrorCode.InvalidStatus;
            }

            // BƯỚC 1: Giải phóng số tiền bị khóa (chỉ trừ trường LockedBalance, giữ nguyên tổng Balance của khách hàng)
            await _uow.Wallets.DecrementLockedBalanceAsync(withdrawal.AccountId, withdrawal.Amount, ct);

            // BƯỚC 2: Cập nhật trạng thái lệnh rút tiền thành Failed (Lỗi) và ghi nhận nguyên nhân lỗi
            var prevStatus = withdrawal.Status;
            withdrawal.Status = WithdrawalStatuses.Failed;
            withdrawal.FailReason = command.FailReason;
            withdrawal.CompletedAt = DateTime.UtcNow;
            _uow.Withdrawals.UpdateAsync(withdrawal);
            await _uow.SaveChangesAsync(ct);

            // BƯỚC 3: Ghi nhận nhật ký trạng thái lỗi
            await _uow.Withdrawals.AddStatusHistoryAsync(
                BuildHistory(withdrawal.WithdrawalId, prevStatus, WithdrawalStatuses.Failed, command.Source, command.FailReason),
                ct);
            await _uow.SaveChangesAsync(ct);

            await _uow.CommitTransactionAsync(ct);

            _logger.LogInformation("Withdrawal {Id} rolled back — {Reason}", command.WithdrawalId, command.FailReason);
            return WithdrawalErrorCode.Success;
        }
        catch (Exception ex)
        {
            await _uow.RollbackTransactionAsync(ct);
            _logger.LogError(ex, "RollbackAsync failed for withdrawal {Id}", command.WithdrawalId);
            return WithdrawalErrorCode.SystemError;
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // CANCEL — cancels a PENDING withdrawal that was never sent to PayOS
    // ────────────────────────────────────────────────────────────────────────────

    public async Task<WithdrawalErrorCode> CancelAsync(
        CancelWithdrawalCommand command,
        CancellationToken ct = default)
    {
        await _uow.BeginTransactionAsync(ct);
        try
        {
            var withdrawal = await _uow.Withdrawals.GetForUpdateAsync(command.WithdrawalId, ct);
            if (withdrawal is null)
            {
                await _uow.RollbackTransactionAsync(ct);
                return WithdrawalErrorCode.WalletNotFound;
            }

            // Chỉ chủ tài khoản mới được quyền hủy yêu cầu rút tiền của mình
            if (withdrawal.AccountId != command.RequestingAccountId)
            {
                await _uow.RollbackTransactionAsync(ct);
                return WithdrawalErrorCode.InvalidStatus;
            }

            // Chỉ cho phép hủy khi lệnh rút tiền đang ở trạng thái PENDING và chưa từng gửi yêu cầu sang cổng PayOS
            if (withdrawal.Status != WithdrawalStatuses.Pending || withdrawal.PayosPayoutId is not null)
            {
                await _uow.RollbackTransactionAsync(ct);
                return WithdrawalErrorCode.InvalidStatus;
            }

            // BƯỚC 1: Giải phóng số tiền bị khóa (LockedBalance) của khách hàng
            await _uow.Wallets.DecrementLockedBalanceAsync(withdrawal.AccountId, withdrawal.Amount, ct);

            // BƯỚC 2: Cập nhật trạng thái thành Cancelled (Đã hủy)
            var prevStatus = withdrawal.Status;
            withdrawal.Status = WithdrawalStatuses.Cancelled;
            withdrawal.CancelledAt = DateTime.UtcNow;
            _uow.Withdrawals.UpdateAsync(withdrawal);
            await _uow.SaveChangesAsync(ct);

            await _uow.Withdrawals.AddStatusHistoryAsync(
                BuildHistory(withdrawal.WithdrawalId, prevStatus, WithdrawalStatuses.Cancelled, WithdrawalHistorySources.User, "Cancelled by user"),
                ct);
            await _uow.SaveChangesAsync(ct);

            await _uow.CommitTransactionAsync(ct);

            _logger.LogInformation("Withdrawal {Id} cancelled by account {AccountId}", command.WithdrawalId, command.RequestingAccountId);
            return WithdrawalErrorCode.Success;
        }
        catch (Exception ex)
        {
            await _uow.RollbackTransactionAsync(ct);
            _logger.LogError(ex, "CancelAsync failed for withdrawal {Id}", command.WithdrawalId);
            return WithdrawalErrorCode.SystemError;
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────────────────────────────

    private static WithdrawalStatusHistory BuildHistory(
        int withdrawalId,
        string? fromStatus,
        string toStatus,
        string source,
        string? note)
        => new()
        {
            WithdrawalId = withdrawalId,
            FromStatus   = fromStatus,
            ToStatus     = toStatus,
            Source       = source,
            Note         = note,
            CreatedAt    = DateTime.UtcNow,
        };
}
