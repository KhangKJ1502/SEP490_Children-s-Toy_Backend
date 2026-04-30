using Microsoft.EntityFrameworkCore;
using ToyStore.Application.DTOs.Campaigns;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class CampaignRepository : ICampaignRepository
{
    private readonly SEP490ToyStoreContext _context;

    public CampaignRepository(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    public async Task<List<Campaign>> GetPagedAsync(
          CampaignQueryDto query,
          CancellationToken cancellationToken = default)
    {
        var baseQuery = BuildBaseQuery(query);

        baseQuery = ApplySort(baseQuery, query.SortBy, query.SortDesc);

        return await baseQuery
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(
         CampaignQueryDto query,
         CancellationToken cancellationToken = default)
    {
        return BuildBaseQuery(query).CountAsync(cancellationToken);
    }

    public Task<Campaign?> GetByIdAsync(int campaignId, CancellationToken cancellationToken = default)
    {
        return _context.Campaigns
            .AsNoTracking()
            .Include(x => x.CampaignStat)
            .Include(x => x.CampaignTargets)
            .Where(x => x.CampaignId == campaignId && !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Xay dung IQueryable co ban voi tat ca dieu kien WHERE (khong sort, khong phan trang).
    /// Dung chung cho GetPagedAsync va CountAsync de dam bao nhat quan.
    /// </summary>
    private IQueryable<Campaign> BuildBaseQuery(CampaignQueryDto query)
    {
        var q = _context.Campaigns
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim();
            q = q.Where(x =>
                x.CampaignName.Contains(term) ||
                (x.TemplateCode != null && x.TemplateCode.Contains(term)) ||
                (x.EventKey != null && x.EventKey.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            q = q.Where(x => x.Status == query.Status);
        }

        if (!string.IsNullOrWhiteSpace(query.SourceType))
        {
            q = q.Where(x => x.SourceType == query.SourceType);
        }

        if (query.StartDate.HasValue)
        {
            var from = DateTime.SpecifyKind(query.StartDate.Value.Date, DateTimeKind.Utc);
            q = q.Where(x => x.CreatedAt >= from);
        }

        if (query.EndDate.HasValue)
        {
            var to = DateTime.SpecifyKind(query.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            q = q.Where(x => x.CreatedAt <= to);
        }

        return q;
    }

    /// <summary>
    /// Ap dung sort theo sortBy: createdAt | name | status. Mac dinh: createdAt DESC.
    /// </summary>
    private static IQueryable<Campaign> ApplySort(
        IQueryable<Campaign> q,
        string? sortBy,
        bool sortDesc)
    {
        return (sortBy?.Trim().ToLowerInvariant(), sortDesc) switch
        {
            ("name", true) => q.OrderByDescending(x => x.CampaignName),
            ("name", false) => q.OrderBy(x => x.CampaignName),
            ("status", true) => q.OrderByDescending(x => x.Status),
            ("status", false) => q.OrderBy(x => x.Status),
            ("createdat", true) => q.OrderByDescending(x => x.CreatedAt),
            ("createdat", false) => q.OrderBy(x => x.CreatedAt),
            (_, true) => q.OrderByDescending(x => x.CreatedAt),
            _ => q.OrderByDescending(x => x.CreatedAt)
        };
    }

    public Task<bool> ExistsByNameAsync(string campaignName, CancellationToken cancellationToken = default)
    {
        var normalized = campaignName.Trim().ToLower();
        return _context.Campaigns
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .AnyAsync(x => x.CampaignName.ToLower() == normalized, cancellationToken);
    }

    public async Task<Campaign> CreateAsync(
        CreateCampaignDto dto,
        CancellationToken cancellationToken = default)
    {
        var campaign = new Campaign
        {
            CampaignName = dto.CampaignName.Trim(),
            TemplateCode = string.IsNullOrWhiteSpace(dto.TemplateCode) ? null : dto.TemplateCode.Trim(),
            TitleOverride = string.IsNullOrWhiteSpace(dto.TitleOverride) ? null : dto.TitleOverride.Trim(),
            MessageOverride = string.IsNullOrWhiteSpace(dto.MessageOverride) ? null : dto.MessageOverride.Trim(),
            SourceType = dto.SourceType,
            TargetType = dto.TargetType,
            Status = "Draft",
            ScheduledAt = dto.ScheduledAt,
            EventKey = string.IsNullOrWhiteSpace(dto.EventKey) ? null : dto.EventKey.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim(),
            ActionType = string.IsNullOrWhiteSpace(dto.ActionType) ? null : dto.ActionType.Trim(),
            ActionTarget = string.IsNullOrWhiteSpace(dto.ActionTarget) ? null : dto.ActionTarget.Trim(),
            CreatedByAccountId = dto.CreatedByAccountId,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Campaigns.AddAsync(campaign, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var targets = new List<CampaignTarget>();
        if (dto.Targets.Count > 0)
        {
            targets = dto.Targets.Select(t => new CampaignTarget
            {
                CampaignId = campaign.CampaignId,
                TargetType = t.TargetType.Trim(),
                TargetValue = t.TargetValue.Trim()
            }).ToList();

            await _context.CampaignTargets.AddRangeAsync(targets, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        campaign.CampaignTargets = targets;
        return campaign;
    }
}
