using Microsoft.EntityFrameworkCore;
using ToyStore.Application.DTOs;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

/// <summary>
/// Repository triển khai các thao tác với Promotion.
/// </summary>
public class PromotionRepository : IPromotionRepository
{
    private readonly SEP490ToyStoreContext _context;

    public PromotionRepository(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResponse<Promotion>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Promotions.Where(x => !x.IsDeleted);

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
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<Promotion>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<Promotion?> GetByIdAsync(int promotionId, CancellationToken cancellationToken = default, string? includeProperties = null)
    {
        IQueryable<Promotion> query = _context.Promotions;

        if (!string.IsNullOrWhiteSpace(includeProperties))
        {
            foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty);
            }
        }

        return await query
            .Where(x => !x.IsDeleted && x.PromotionId == promotionId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> ExistsPromotionNameAsync(
        string promotionName,
        int? excludePromotionId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Promotions.Where(x => !x.IsDeleted && x.PromotionName == promotionName);

        if (excludePromotionId.HasValue)
        {
            query = query.Where(x => x.PromotionId != excludePromotionId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Promotion promotion, CancellationToken cancellationToken = default)
    {
        await _context.AddAsync(promotion, cancellationToken);

        // This is a hack to get the generated ID back to the model, usually done after SaveChanges
        // but UnitOfWork handles SaveChanges. We will update it in service if needed.
    }

    public void Update(Promotion promotion)
    {
        // Attach and mark as modified
        _context.Attach(promotion);
        _context.Entry(promotion).State = EntityState.Modified;
    }

    public void RemoveProductPromotion(ProductPromotion productPromotion)
    {
        _context.ProductPromotions.Remove(productPromotion);
    }

    public void RemovePromotionTimeSlot(PromotionTimeSlot promotionTimeSlot)
    {
        _context.Remove(promotionTimeSlot);
    }

    public async Task<bool> IsProductInActivePromotionAsync(int productId, CancellationToken cancellationToken = default)
    {
        // Check in DISCOUNT (ProductPromotions)
        bool inDiscount = await _context.ProductPromotions
            .Include(pp => pp.Promotion)
            .AnyAsync(pp => pp.ProductId == productId 
                && !pp.IsDeleted
                && !pp.Promotion.IsDeleted 
                && (pp.Promotion.Status == "Active" || pp.Promotion.Status == "Scheduled"), cancellationToken);

        if (inDiscount) return true;

        // Check in FLASH_SALE (PromotionProductSlots)
        bool inFlashSale = await _context.PromotionProductSlots
            .Include(pps => pps.TimeSlot)
            .ThenInclude(ts => ts.Promotion)
            .AnyAsync(pps => pps.ProductId == productId 
                && !pps.IsDeleted
                && !pps.TimeSlot.IsDeleted
                && !pps.TimeSlot.Promotion.IsDeleted
                && (pps.TimeSlot.Promotion.Status == "Active" || pps.TimeSlot.Promotion.Status == "Scheduled"), cancellationToken);

        return inFlashSale;
    }

    public async Task<List<Promotion>> GetFlashSalePromotionsAsync(
        int visibilityDays = 2,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var maxVisibleDate = now.AddDays(visibilityDays);

        return await _context.Promotions
            .Where(p => !p.IsDeleted
                && p.PromotionType == "FLASH_SALE"
                && p.EndDate >= now
                && (p.Status == "Active" || (p.Status == "Scheduled" && p.StartDate <= maxVisibleDate)))
            .Include(p => p.PromotionTimeSlots.Where(ts => !ts.IsDeleted && (ts.Status == "Active" || (ts.Status == "Scheduled" && ts.StartAt <= maxVisibleDate))))
                .ThenInclude(ts => ts.PromotionProductSlots.Where(pps => !pps.IsDeleted))
                    .ThenInclude(pps => pps.Product)
                        .ThenInclude(prod => prod.ProductImage)
            .OrderBy(p => p.Priority)
            .ToListAsync(cancellationToken);
    }
}
