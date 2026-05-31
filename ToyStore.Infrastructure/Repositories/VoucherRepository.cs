using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Infrastructure.Data;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Repositories;

/// <summary>
/// Repository truy vấn dữ liệu voucher.
/// </summary>
public class VoucherRepository : IVoucherRepository
{
    private readonly SEP490ToyStoreContext _context;

    public VoucherRepository(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResponse<Voucher>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Vouchers
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var keyword = searchTerm.Trim();
            query = query.Where(x =>
                x.VoucherCode.Contains(keyword)
                || x.VoucherName.Contains(keyword)
                || x.VoucherDescription.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim();
            query = query.Where(x => x.Status == normalizedStatus);
        }

        var orderedQuery = query.OrderBy(x => x.Status == "Pending" ? 0 : 1);
        var sortedQuery = ApplySorting(orderedQuery, sortBy, sortDesc);

        var totalCount = await sortedQuery.CountAsync(cancellationToken);
        var skip = (pageNumber - 1) * pageSize;

        var entities = await sortedQuery
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<Voucher>(entities, totalCount, pageNumber, pageSize);
    }

    public async Task<Voucher?> GetByIdAsync(int voucherId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Vouchers
            .FirstOrDefaultAsync(x => x.VoucherId == voucherId && !x.IsDeleted, cancellationToken);

        return entity;
    }

    public async Task<Voucher?> GetByCodeAsync(string voucherCode, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Vouchers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.VoucherCode == voucherCode && !x.IsDeleted,
                cancellationToken);

        return entity;
    }

    public async Task<bool> ExistsVoucherCodeAsync(
        string voucherCode,
        int? excludeVoucherId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Vouchers
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.VoucherCode == voucherCode);

        if (excludeVoucherId.HasValue)
        {
            query = query.Where(x => x.VoucherId != excludeVoucherId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Voucher voucher, CancellationToken cancellationToken = default)
    {
        await _context.AddAsync(voucher, cancellationToken);
    }

    public void Update(Voucher voucher)
    {
        _context.Update(voucher);
    }

    public Task<int> CountUsageByAccountAsync(int voucherId, int accountId, CancellationToken cancellationToken = default)
    {
        // Exclude active SE_PAY PENDING orders — voucher is not truly consumed until payment is confirmed.
        // With the max-1-pending guard in CheckoutService, this cannot be abused to stack voucher discounts.
        return _context.VoucherUsageLogs
            .AsNoTracking()
            .Where(x => x.VoucherId == voucherId
                     && x.AccountId == accountId
                     && !(x.Order.PaymentMethod == "SE_PAY"
                          && x.Order.PaymentStatus == "PENDING"
                          && x.Order.CancelledAt == null))
            .CountAsync(cancellationToken);
    }

    private static IQueryable<Voucher> ApplySorting(IOrderedQueryable<Voucher> query, string? sortBy, bool sortDesc)
    {
        var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();

        return normalizedSortBy switch
        {
            "vouchercode" => sortDesc
                ? query.ThenByDescending(x => x.VoucherCode)
                : query.ThenBy(x => x.VoucherCode),
            "vouchername" => sortDesc
                ? query.ThenByDescending(x => x.VoucherName)
                : query.ThenBy(x => x.VoucherName),
            "discountvalue" => sortDesc
                ? query.ThenByDescending(x => x.DiscountValue)
                : query.ThenBy(x => x.DiscountValue),
            "startdate" => sortDesc
                ? query.ThenByDescending(x => x.StartDate)
                : query.ThenBy(x => x.StartDate),
            "enddate" => sortDesc
                ? query.ThenByDescending(x => x.EndDate)
                : query.ThenBy(x => x.EndDate),
            "status" => sortDesc
                ? query.ThenByDescending(x => x.Status)
                : query.ThenBy(x => x.Status),
            _ => sortDesc
                ? query.ThenByDescending(x => x.CreatedAt)
                : query.ThenBy(x => x.CreatedAt)
        };
    }
}
