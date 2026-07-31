using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Enums;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class WithdrawalRepository : IWithdrawalRepository
{
    private readonly SEP490ToyStoreContext _context;

    public WithdrawalRepository(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    public Task<WithdrawalRequest?> GetByIdAsync(int withdrawalId, CancellationToken ct = default)
        => _context.WithdrawalRequests.FirstOrDefaultAsync(w => w.WithdrawalId == withdrawalId, ct);

    public Task<WithdrawalRequest?> GetByReferenceIdAsync(string referenceId, CancellationToken ct = default)
        => _context.WithdrawalRequests.FirstOrDefaultAsync(w => w.ReferenceId == referenceId, ct);

    public Task<List<WithdrawalRequest>> GetMyWithdrawalsAsync(int accountId, int page, int pageSize, CancellationToken ct = default)
        => _context.WithdrawalRequests
            .Where(w => w.AccountId == accountId)
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public Task<int> CountMyWithdrawalsAsync(int accountId, CancellationToken ct = default)
        => _context.WithdrawalRequests.CountAsync(w => w.AccountId == accountId, ct);

    public Task<List<WithdrawalRequest>> GetStalePendingAsync(DateTime olderThan, CancellationToken ct = default)
        => _context.WithdrawalRequests
            .Where(w => (w.Status == WithdrawalStatuses.Pending || w.Status == WithdrawalStatuses.Processing)
                        && w.CreatedAt < olderThan)
            .ToListAsync(ct);

    public async Task<(decimal TotalAmount, int Count)> GetDailyStatsAsync(int accountId, DateTime date, CancellationToken ct = default)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        var activeStatuses = new[] { WithdrawalStatuses.Pending, WithdrawalStatuses.Processing, WithdrawalStatuses.Success };

        var rows = await _context.WithdrawalRequests
            .Where(w => w.AccountId == accountId
                        && activeStatuses.Contains(w.Status)
                        && w.CreatedAt >= startOfDay
                        && w.CreatedAt < endOfDay)
            .Select(w => w.Amount)
            .ToListAsync(ct);

        return (rows.Sum(), rows.Count);
    }

    public Task<bool> HasActivePendingAsync(int accountId, CancellationToken ct = default)
        => _context.WithdrawalRequests.AnyAsync(
            w => w.AccountId == accountId
                 && (w.Status == WithdrawalStatuses.Pending || w.Status == WithdrawalStatuses.Processing),
            ct);

    public async Task AddAsync(WithdrawalRequest withdrawal, CancellationToken ct = default)
    {
        await _context.WithdrawalRequests.AddAsync(withdrawal, ct);
    }

    public async Task<List<WithdrawalRequest>> GetAdminWithdrawalsAsync(string? keyword, string? status, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.WithdrawalRequests.Include(w => w.Account).AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            keyword = keyword.Trim().ToLower();
            query = query.Where(w => w.ReferenceId.ToLower().Contains(keyword) || w.Account.AccountName.ToLower().Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(w => w.Status == status);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(w => w.CreatedAt >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(w => w.CreatedAt <= dateTo.Value);
        }

        return await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public Task<int> CountAdminWithdrawalsAsync(string? keyword, string? status, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default)
    {
        var query = _context.WithdrawalRequests.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            keyword = keyword.Trim().ToLower();
            query = query.Where(w => w.ReferenceId.ToLower().Contains(keyword) || w.Account.AccountName.ToLower().Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(w => w.Status == status);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(w => w.CreatedAt >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(w => w.CreatedAt <= dateTo.Value);
        }

        return query.CountAsync(ct);
    }

    public Task<WithdrawalRequest?> GetWithDetailsByIdAsync(int id, CancellationToken ct = default)
        => _context.WithdrawalRequests
            .Include(w => w.Account)
            .Include(w => w.WithdrawalStatusHistories)
            .FirstOrDefaultAsync(w => w.WithdrawalId == id, ct);

    // ── Withdrawal Ledger Operations ─────────────────────────────────────────

    public async Task<WithdrawalRequest?> GetForUpdateAsync(int withdrawalId, CancellationToken ct = default)
    {
        // Issue UPDLOCK hint to prevent concurrent ledger operations on the same row
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM WithdrawalRequests WITH (UPDLOCK, ROWLOCK) WHERE WithdrawalID = {withdrawalId}", ct);
        return await _context.WithdrawalRequests.FirstOrDefaultAsync(w => w.WithdrawalId == withdrawalId, ct);
    }

    public void UpdateAsync(WithdrawalRequest withdrawal)
        => _context.WithdrawalRequests.Update(withdrawal);

    public async Task AddStatusHistoryAsync(WithdrawalStatusHistory history, CancellationToken ct = default)
        => await _context.WithdrawalStatusHistories.AddAsync(history, ct);
}
