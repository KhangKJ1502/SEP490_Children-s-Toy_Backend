using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Common.Models.Vouchers;
using ToyStore.Application.DTOs;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Models;

namespace ToyStore.Infrastructure.Repositories;

/// <summary>
/// Repository truy vấn dữ liệu voucher.
/// </summary>
public class VoucherRepository : IVoucherRepository
{
    private readonly SEP490ToyStoreContext _context;
    private readonly DbSet<Voucher> _dbSet;

    public VoucherRepository(SEP490ToyStoreContext context)
    {
        _context = context;
        _dbSet = context.Set<Voucher>();
    }

    public async Task<PaginatedResponse<VoucherModel>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
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

        var items = entities
            .Select(MapToModel)
            .ToList();

        return new PaginatedResponse<VoucherModel>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<VoucherModel?> GetByIdAsync(int voucherId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.VoucherId == voucherId && !x.IsDeleted, cancellationToken);

        return entity is null ? null : MapToModel(entity);
    }

    public async Task<VoucherModel?> GetByCodeAsync(string voucherCode, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.VoucherCode == voucherCode && !x.IsDeleted,
                cancellationToken);

        return entity is null ? null : MapToModel(entity);
    }

    public async Task<bool> ExistsVoucherCodeAsync(
        string voucherCode,
        int? excludeVoucherId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.VoucherCode == voucherCode);

        if (excludeVoucherId.HasValue)
        {
            query = query.Where(x => x.VoucherId != excludeVoucherId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(VoucherModel voucher, CancellationToken cancellationToken = default)
    {
        var entity = MapToEntity(voucher);
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public void Update(VoucherModel voucher)
    {
        var entity = MapToEntity(voucher);
        _dbSet.Update(entity);
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

    private static VoucherModel MapToModel(Voucher entity)
    {
        return new VoucherModel
        {
            VoucherId = entity.VoucherId,
            CreatedBy = entity.CreatedBy,
            VoucherCode = entity.VoucherCode,
            VoucherName = entity.VoucherName,
            VoucherDescription = entity.VoucherDescription,
            DiscountType = entity.DiscountType,
            DiscountValue = entity.DiscountValue,
            MaxDiscountCap = entity.MaxDiscountCap,
            DiscountTarget = entity.DiscountTarget,
            MinOrderAmount = entity.MinOrderAmount,
            TotalQuantity = entity.TotalQuantity,
            UsedQuantity = entity.UsedQuantity,
            MaxUsagePerUser = entity.MaxUsagePerUser,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Status = entity.Status,
            IsDeleted = entity.IsDeleted,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private static Voucher MapToEntity(VoucherModel model)
    {
        return new Voucher
        {
            VoucherId = model.VoucherId,
            CreatedBy = model.CreatedBy,
            VoucherCode = model.VoucherCode,
            VoucherName = model.VoucherName,
            VoucherDescription = model.VoucherDescription,
            DiscountType = model.DiscountType,
            DiscountValue = model.DiscountValue,
            MaxDiscountCap = model.MaxDiscountCap,
            DiscountTarget = model.DiscountTarget,
            MinOrderAmount = model.MinOrderAmount,
            TotalQuantity = model.TotalQuantity,
            UsedQuantity = model.UsedQuantity,
            MaxUsagePerUser = model.MaxUsagePerUser,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            Status = model.Status,
            IsDeleted = model.IsDeleted,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt
        };
    }
}
