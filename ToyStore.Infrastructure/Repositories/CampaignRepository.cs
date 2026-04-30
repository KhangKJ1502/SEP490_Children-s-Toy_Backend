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

    public Task<bool> ExistsByNameAsync(
        string campaignName,
        int excludeId = 0,
        CancellationToken cancellationToken = default)
    {
        var normalized = campaignName.Trim().ToLower();
        return _context.Campaigns
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.CampaignId != excludeId)
            .AnyAsync(x => x.CampaignName.ToLower() == normalized, cancellationToken);
    }

    public async Task<Campaign> CreateAsync(
        CreateCampaignDto dto,
        CancellationToken cancellationToken = default)
    {
        var campaign = new Campaign
        {
            CampaignName      = dto.CampaignName.Trim(),
            TemplateCode      = string.IsNullOrWhiteSpace(dto.TemplateCode)      ? null : dto.TemplateCode.Trim(),
            ReferenceType     = string.IsNullOrWhiteSpace(dto.ReferenceType)     ? null : dto.ReferenceType.Trim().ToUpper(),
            ReferenceId       = dto.ReferenceId,
            TitleOverride     = string.IsNullOrWhiteSpace(dto.TitleOverride)     ? null : dto.TitleOverride.Trim(),
            MessageOverride   = string.IsNullOrWhiteSpace(dto.MessageOverride)   ? null : dto.MessageOverride.Trim(),
            SourceType        = dto.SourceType,
            TargetType        = dto.TargetType,
            Status            = dto.ScheduledAt.HasValue ? "Scheduled" : "Draft",
            ScheduledAt       = dto.ScheduledAt,
            EventKey          = string.IsNullOrWhiteSpace(dto.EventKey)          ? null : dto.EventKey.Trim(),
            ImageUrl          = string.IsNullOrWhiteSpace(dto.ImageUrl)          ? null : dto.ImageUrl.Trim(),
            ActionType        = string.IsNullOrWhiteSpace(dto.ActionType)        ? null : dto.ActionType.Trim(),
            ActionTarget      = string.IsNullOrWhiteSpace(dto.ActionTarget)      ? null : dto.ActionTarget.Trim(),
            CreatedByAccountId = dto.CreatedByAccountId,
            IsDeleted         = false,
            CreatedAt         = DateTime.UtcNow
        };

        await _context.Campaigns.AddAsync(campaign, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        if (dto.Targets.Count > 0)
        {
            var targets = dto.Targets.Select(t => new CampaignTarget
            {
                CampaignId  = campaign.CampaignId,
                TargetType  = t.TargetType.Trim(),
                TargetValue = t.TargetValue.Trim()
            }).ToList();

            await _context.CampaignTargets.AddRangeAsync(targets, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            campaign.CampaignTargets = targets;
        }

        return campaign;
    }

    public async Task<Campaign> UpdateAsync(
        Campaign campaign,
        List<CreateCampaignTargetDto> newTargets,
        CancellationToken cancellationToken = default)
    {
        campaign.UpdatedAt = DateTime.UtcNow;
        _context.Campaigns.Update(campaign);

        // Replace targets: delete old, insert new
        var oldTargets = await _context.CampaignTargets
            .Where(t => t.CampaignId == campaign.CampaignId)
            .ToListAsync(cancellationToken);

        _context.CampaignTargets.RemoveRange(oldTargets);

        var targets = newTargets.Select(t => new CampaignTarget
        {
            CampaignId  = campaign.CampaignId,
            TargetType  = t.TargetType.Trim(),
            TargetValue = t.TargetValue.Trim()
        }).ToList();

        if (targets.Count > 0)
        {
            await _context.CampaignTargets.AddRangeAsync(targets, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        campaign.CampaignTargets = targets;
        return campaign;
    }

    public async Task<bool> CancelAsync(int campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await _context.Campaigns
            .Where(x => x.CampaignId == campaignId && !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (campaign is null) return false;

        campaign.Status    = "Cancelled";
        campaign.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<List<Campaign>> GetDueCampaignsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Campaigns
            .Include(c => c.CampaignTargets)
            .Include(c => c.TemplateCodeNavigation)
            .Where(c => !c.IsDeleted
                     && c.Status == "Scheduled"
                     && c.ScheduledAt.HasValue
                     && c.ScheduledAt.Value <= now)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkSendingAsync(int campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await _context.Campaigns
            .Where(x => x.CampaignId == campaignId)
            .FirstOrDefaultAsync(cancellationToken);

        if (campaign is null) return;

        campaign.Status    = "Sending";
        campaign.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkSentAsync(int campaignId, int totalSent, CancellationToken cancellationToken = default)
    {
        var campaign = await _context.Campaigns
            .Where(x => x.CampaignId == campaignId)
            .FirstOrDefaultAsync(cancellationToken);

        if (campaign is null) return;

        campaign.Status    = "Sent";
        campaign.UpdatedAt = DateTime.UtcNow;

        // Upsert CampaignStat
        var stat = await _context.CampaignStats
            .Where(s => s.CampaignId == campaignId)
            .FirstOrDefaultAsync(cancellationToken);

        if (stat is null)
        {
            stat = new CampaignStat
            {
                CampaignId  = campaignId,
                TotalSent   = totalSent,
                TotalRead   = 0,
                TotalClicked = 0,
                ComputedAt  = DateTime.UtcNow
            };
            await _context.CampaignStats.AddAsync(stat, cancellationToken);
        }
        else
        {
            stat.TotalSent  += totalSent;
            stat.ComputedAt  = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task CreateDeliveriesAsync(List<Delivery> deliveries, CancellationToken cancellationToken = default)
    {
        if (deliveries.Count == 0) return;
        await _context.Deliveries.AddRangeAsync(deliveries, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    // ── Private helpers ────────────────────────────────────────────────────────

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
                (x.EventKey     != null && x.EventKey.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
            q = q.Where(x => x.Status == query.Status);

        if (!string.IsNullOrWhiteSpace(query.SourceType))
            q = q.Where(x => x.SourceType == query.SourceType);

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

    private static IQueryable<Campaign> ApplySort(IQueryable<Campaign> q, string? sortBy, bool sortDesc)
    {
        return (sortBy?.Trim().ToLowerInvariant(), sortDesc) switch
        {
            ("name",      true)  => q.OrderByDescending(x => x.CampaignName),
            ("name",      false) => q.OrderBy(x => x.CampaignName),
            ("status",    true)  => q.OrderByDescending(x => x.Status),
            ("status",    false) => q.OrderBy(x => x.Status),
            ("createdat", true)  => q.OrderByDescending(x => x.CreatedAt),
            ("createdat", false) => q.OrderBy(x => x.CreatedAt),
            (_,           true)  => q.OrderByDescending(x => x.CreatedAt),
            _                    => q.OrderByDescending(x => x.CreatedAt)
        };
    }
}
