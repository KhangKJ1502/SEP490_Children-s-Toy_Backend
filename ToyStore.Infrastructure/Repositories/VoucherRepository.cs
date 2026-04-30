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

        query = ApplySorting(query, sortBy, sortDesc);

        var totalCount = await query.CountAsync(cancellationToken);
        var skip = (pageNumber - 1) * pageSize;

        var entities = await query
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<Voucher>(entities, totalCount, pageNumber, pageSize);
    }

    public async Task<Voucher?> GetByIdAsync(int voucherId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Vouchers
            .AsNoTracking()
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

    private static IQueryable<Voucher> ApplySorting(IQueryable<Voucher> query, string? sortBy, bool sortDesc)
    {
        var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();

        return normalizedSortBy switch
        {
            "vouchercode" => sortDesc
                ? query.OrderByDescending(x => x.VoucherCode)
                : query.OrderBy(x => x.VoucherCode),
            "vouchername" => sortDesc
                ? query.OrderByDescending(x => x.VoucherName)
                : query.OrderBy(x => x.VoucherName),
            "discountvalue" => sortDesc
                ? query.OrderByDescending(x => x.DiscountValue)
                : query.OrderBy(x => x.DiscountValue),
            "startdate" => sortDesc
                ? query.OrderByDescending(x => x.StartDate)
                : query.OrderBy(x => x.StartDate),
            "enddate" => sortDesc
                ? query.OrderByDescending(x => x.EndDate)
                : query.OrderBy(x => x.EndDate),
            "status" => sortDesc
                ? query.OrderByDescending(x => x.Status)
                : query.OrderBy(x => x.Status),
            _ => sortDesc
                ? query.OrderByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.CreatedAt)
        };
    }
}
