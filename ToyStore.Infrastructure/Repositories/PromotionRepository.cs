using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Models;

namespace ToyStore.Infrastructure.Repositories;

/// <summary>
/// Repository triển khai các thao tác với Promotion.
/// </summary>
public class PromotionRepository : IPromotionRepository
{
    private readonly SEP490ToyStoreContext _context;
    private readonly DbSet<Promotion> _dbSet;

    public PromotionRepository(SEP490ToyStoreContext context)
    {
        _context = context;
        _dbSet = context.Set<Promotion>();
    }

    public async Task<PaginatedResponse<PromotionModel>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x => x.PromotionName.Contains(searchTerm) || 
                                     (x.Description != null && x.Description.Contains(searchTerm)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = sortBy?.ToLower() switch
        {
            "name" => sortDesc ? query.OrderByDescending(x => x.PromotionName) : query.OrderBy(x => x.PromotionName),
            "startdate" => sortDesc ? query.OrderByDescending(x => x.StartDate) : query.OrderBy(x => x.StartDate),
            "enddate" => sortDesc ? query.OrderByDescending(x => x.EndDate) : query.OrderBy(x => x.EndDate),
            "priority" => sortDesc ? query.OrderByDescending(x => x.Priority) : query.OrderBy(x => x.Priority),
            _ => sortDesc ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt)
        };

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PromotionModel
            {
                PromotionId = x.PromotionId,
                CreatedBy = x.CreatedBy,
                PromotionName = x.PromotionName,
                PromotionType = x.PromotionType,
                Description = x.Description,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Status = x.Status,
                Priority = x.Priority,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<PromotionModel>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<PromotionModel?> GetByIdAsync(int promotionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(x => !x.IsDeleted && x.PromotionId == promotionId)
            .Select(x => new PromotionModel
            {
                PromotionId = x.PromotionId,
                CreatedBy = x.CreatedBy,
                PromotionName = x.PromotionName,
                PromotionType = x.PromotionType,
                Description = x.Description,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Status = x.Status,
                Priority = x.Priority,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> ExistsPromotionNameAsync(
        string promotionName,
        int? excludePromotionId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(x => !x.IsDeleted && x.PromotionName == promotionName);

        if (excludePromotionId.HasValue)
        {
            query = query.Where(x => x.PromotionId != excludePromotionId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(PromotionModel promotionModel, CancellationToken cancellationToken = default)
    {
        var entity = new Promotion
        {
            CreatedBy = promotionModel.CreatedBy,
            PromotionName = promotionModel.PromotionName,
            PromotionType = promotionModel.PromotionType,
            Description = promotionModel.Description,
            StartDate = promotionModel.StartDate,
            EndDate = promotionModel.EndDate,
            Status = promotionModel.Status,
            Priority = promotionModel.Priority,
            IsDeleted = promotionModel.IsDeleted,
            CreatedAt = promotionModel.CreatedAt,
            UpdatedAt = promotionModel.UpdatedAt
        };

        await _dbSet.AddAsync(entity, cancellationToken);
        
        // This is a hack to get the generated ID back to the model, usually done after SaveChanges
        // but UnitOfWork handles SaveChanges. We will update it in service if needed.
    }

    public void Update(PromotionModel promotionModel)
    {
        var entity = new Promotion
        {
            PromotionId = promotionModel.PromotionId,
            CreatedBy = promotionModel.CreatedBy,
            PromotionName = promotionModel.PromotionName,
            PromotionType = promotionModel.PromotionType,
            Description = promotionModel.Description,
            StartDate = promotionModel.StartDate,
            EndDate = promotionModel.EndDate,
            Status = promotionModel.Status,
            Priority = promotionModel.Priority,
            IsDeleted = promotionModel.IsDeleted,
            CreatedAt = promotionModel.CreatedAt,
            UpdatedAt = promotionModel.UpdatedAt
        };

        // Attach and mark as modified
        _dbSet.Attach(entity);
        _context.Entry(entity).State = EntityState.Modified;
    }
}
