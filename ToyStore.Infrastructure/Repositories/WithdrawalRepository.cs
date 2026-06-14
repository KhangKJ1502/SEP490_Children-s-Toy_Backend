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
}
