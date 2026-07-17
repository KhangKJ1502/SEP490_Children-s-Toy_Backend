using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Common.Models;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Infrastructure.Data;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Repositories;

public class TemplateRepository : ITemplateRepository
{
    private readonly SEP490ToyStoreContext _context;

    public TemplateRepository(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    public async Task<List<Template>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        bool? isActive = null,
        string? usageScope = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Templates
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x =>
                x.TemplateCode.Contains(searchTerm) ||
                x.TitleTemplate.Contains(searchTerm) ||
                x.MessageTemplate.Contains(searchTerm));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(usageScope))
        {
            var normalizedScope = usageScope.Trim().ToUpperInvariant();
            query = query.Where(x => x.UsageScope == normalizedScope);
        }

        if (startDate.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= endDate.Value);
        }

        query = (sortBy?.Trim().ToLowerInvariant(), sortDesc) switch
        {
            ("templatecode", true) => query.OrderByDescending(x => x.TemplateCode),
            ("templatecode", false) => query.OrderBy(x => x.TemplateCode),
            ("titletemplate", true) => query.OrderByDescending(x => x.TitleTemplate),
            ("titletemplate", false) => query.OrderBy(x => x.TitleTemplate),
            ("isactive", true) => query.OrderByDescending(x => x.IsActive),
            ("isactive", false) => query.OrderBy(x => x.IsActive),
            ("createdat", true) => query.OrderByDescending(x => x.CreatedAt),
            ("createdat", false) => query.OrderBy(x => x.CreatedAt),
            (_, true) => query.OrderByDescending(x => x.TemplateId),
            _ => query.OrderBy(x => x.TemplateId)
        };

        return await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(
        string? searchTerm = null,
        bool? isActive = null,
        string? usageScope = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Templates
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x =>
                x.TemplateCode.Contains(searchTerm) ||
                x.TitleTemplate.Contains(searchTerm) ||
                x.MessageTemplate.Contains(searchTerm));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(usageScope))
        {
            var normalizedScope = usageScope.Trim().ToUpperInvariant();
            query = query.Where(x => x.UsageScope == normalizedScope);
        }

        if (startDate.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= endDate.Value);
        }

        return query.CountAsync(cancellationToken);
    }

    public Task<bool> ExistsByCodeAsync(string templateCode, CancellationToken cancellationToken = default)
    {
        var normalized = templateCode.Trim().ToLower();
        return _context.Templates
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .AnyAsync(x => x.TemplateCode.ToLower() == normalized, cancellationToken);
    }

    public Task<bool> ExistsByCodeExceptIdAsync(
        string templateCode,
        short templateId,
        CancellationToken cancellationToken = default)
    {
        var normalized = templateCode.Trim().ToLower();
        return _context.Templates
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.TemplateId != templateId)
            .AnyAsync(x => x.TemplateCode.ToLower() == normalized, cancellationToken);
    }

    public async Task<bool> IsUsedAsync(string templateCode, CancellationToken cancellationToken = default)
    {
        var ignoredStatuses = new[] { "Draft", "Rejected", "Cancelled" };
        var isUsedInCampaigns = await _context.Campaigns
            .AsNoTracking()
            .AnyAsync(x => x.TemplateCode == templateCode && !ignoredStatuses.Contains(x.Status), cancellationToken);

        if (isUsedInCampaigns) return true;

        var isUsedInDeliveries = await _context.Deliveries
            .AsNoTracking()
            .AnyAsync(x => x.TemplateCode == templateCode, cancellationToken);

        return isUsedInDeliveries;
    }

    public Task<Template?> GetByIdAsync(short templateId, CancellationToken cancellationToken = default)
    {
        return _context.Templates
            .AsNoTracking()
            .Where(x => x.TemplateId == templateId && !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Template> CreateAsync(
        string templateCode,
        string titleTemplate,
        string messageTemplate,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var entity = new Template
        {
            TemplateCode = templateCode,
            TitleTemplate = titleTemplate,
            MessageTemplate = messageTemplate,
            IsActive = isActive,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Templates.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return entity;
    }

    public async Task<Template?> SaveAsync(
        short templateId,
        bool isDeleted,
        string? titleTemplate,
        string? messageTemplate,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Templates
            .Where(x => x.TemplateId == templateId && !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null) return null;

        if (isDeleted)
        {
            entity.IsDeleted = true;
        }
        else
        {
            entity.TitleTemplate = titleTemplate!;
            entity.MessageTemplate = messageTemplate!;
            entity.IsActive = isActive!.Value;
        }

        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return entity;
    }

    public Task<Template?> GetActiveByCodeAsync(string templateCode, CancellationToken cancellationToken = default)
    {
        return _context.Templates
            .AsNoTracking()
            .Where(x => x.TemplateCode == templateCode && x.IsActive && !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }
}