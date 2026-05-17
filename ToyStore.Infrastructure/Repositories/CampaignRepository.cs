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
        bool forAdminList = false,
        int viewerAccountId = 0,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = BuildBaseQuery(query, forAdminList, viewerAccountId);
        baseQuery = ApplySort(baseQuery, query.SortBy, query.SortDesc);

        return await baseQuery
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(
        CampaignQueryDto query,
        bool forAdminList = false,
        int viewerAccountId = 0,
        CancellationToken cancellationToken = default)
    {
        return BuildBaseQuery(query, forAdminList, viewerAccountId).CountAsync(cancellationToken);
    }

    public Task<Campaign?> GetByIdAsync(int campaignId, CancellationToken cancellationToken = default)
    {
        return _context.Campaigns
            .AsNoTracking()
            .Include(x => x.CampaignStat)
            .Include(x => x.CampaignTargets)
            .Include(x => x.TemplateCodeNavigation)
            .Include(x => x.CampaignSchedule)
            .Include(x => x.CampaignReferenceSnapshots)
            .Include(x => x.CreatedByAccount)
            .Include(x => x.SubmittedByAccount)
            .Include(x => x.ReviewedByAccount)
            .Where(x => x.CampaignId == campaignId && !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Campaign?> GetByIdWithDetailsAsync(int campaignId, CancellationToken cancellationToken = default)
        => GetByIdAsync(campaignId, cancellationToken);

    public Task<Campaign?> GetForUpdateAsync(int campaignId, CancellationToken cancellationToken = default)
    {
        return _context.Campaigns
            .Include(x => x.CampaignTargets)
            .Include(x => x.TemplateCodeNavigation)
            .Include(x => x.CampaignSchedule)
            .Include(x => x.CampaignReferenceSnapshots)
            .FirstOrDefaultAsync(x => x.CampaignId == campaignId && !x.IsDeleted, cancellationToken);
    }

    public async Task<int> GetBackgroundJobIdForCampaignLockAsync(CancellationToken cancellationToken = default)
    {
        var id = await _context.BackgroundJobs
            .AsNoTracking()
            .OrderBy(j => j.JobId)
            .Select(j => j.JobId)
            .FirstOrDefaultAsync(cancellationToken);

        if (id == 0)
            throw new InvalidOperationException("System.BackgroundJobs has no rows; cannot lock campaign schedule.");

        return id;
    }

    public async Task<bool> TryAcquireDispatchLockAsync(
        int campaignId,
        int backgroundJobId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var n = await _context.CampaignSchedules
            .Where(s => s.CampaignId == campaignId
                     && s.ExecutionStatus == "Waiting"
                     && s.LockedByJobId == null)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(x => x.LockedByJobId, backgroundJobId)
                    .SetProperty(x => x.LockedAt, utcNow)
                    .SetProperty(x => x.ExecutionStatus, "Dispatched")
                    .SetProperty(x => x.UpdatedAt, utcNow),
                cancellationToken);
        return n > 0;
    }

    public Task<int> RecoverStaleDispatchLocksAsync(
        TimeSpan lockOlderThan,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var cutoff = utcNow - lockOlderThan;
        return _context.CampaignSchedules
            .Where(s => s.ExecutionStatus == "Dispatched"
                     && s.LockedAt != null
                     && s.LockedAt <= cutoff)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(x => x.ExecutionStatus, "Waiting")
                    .SetProperty(x => x.LockedByJobId, (int?)null)
                    .SetProperty(x => x.LockedAt, (DateTime?)null)
                    .SetProperty(x => x.UpdatedAt, utcNow),
                cancellationToken);
    }

    public Task MarkLiveReferenceSnapshotsStaleAsync(
        int campaignId,
        string reason,
        DateTime staleAt,
        CancellationToken cancellationToken = default)
    {
        var r = reason.Length > 200 ? reason[..200] : reason;
        return _context.CampaignReferenceSnapshots
            .Where(s => s.CampaignId == campaignId && !s.IsStale)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(x => x.IsStale, true)
                    .SetProperty(x => x.StaleReason, r)
                    .SetProperty(x => x.StaleDetectedAt, staleAt),
                cancellationToken);
    }

    public void AddReferenceSnapshot(CampaignReferenceSnapshot snapshot)
        => _context.CampaignReferenceSnapshots.Add(snapshot);

    public async Task CompleteDispatchAsync(
        int campaignId,
        int totalSent,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        await _context.CampaignSchedules
            .Where(s => s.CampaignId == campaignId)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(x => x.ExecutionStatus, "Done")
                    .SetProperty(x => x.ExecutedAt, utcNow)
                    .SetProperty(x => x.LockedByJobId, (int?)null)
                    .SetProperty(x => x.LockedAt, (DateTime?)null)
                    .SetProperty(x => x.UpdatedAt, utcNow),
                cancellationToken);

        await MarkSentAsync(campaignId, totalSent, cancellationToken);
    }

    public async Task HandleDispatchFailureAsync(
        int campaignId,
        string error,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var schedule = await _context.CampaignSchedules
            .FirstOrDefaultAsync(s => s.CampaignId == campaignId, cancellationToken);

        if (schedule is null) return;

        var msg = string.IsNullOrEmpty(error) ? "Dispatch error" : error;
        if (msg.Length > 500) msg = msg[..500];

        schedule.LastError = msg;
        schedule.AttemptCount++;
        schedule.UpdatedAt = utcNow;

        if (schedule.AttemptCount >= schedule.MaxAttemptCount)
        {
            schedule.ExecutionStatus = "Failed";
            schedule.LockedByJobId = null;
            schedule.LockedAt = null;

            var campaign = await _context.Campaigns.FirstOrDefaultAsync(c => c.CampaignId == campaignId, cancellationToken);
            if (campaign is not null)
            {
                campaign.Status = "Failed";
                campaign.UpdatedAt = utcNow;
            }
        }
        else
        {
            schedule.ExecutionStatus = "Waiting";
            schedule.LockedByJobId = null;
            schedule.LockedAt = null;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SystemCancelWithAuditAsync(
        int campaignId,
        string approvalLogNote,
        int actorAccountId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var campaign = await _context.Campaigns
            .Include(c => c.CampaignSchedule)
            .FirstOrDefaultAsync(c => c.CampaignId == campaignId && !c.IsDeleted, cancellationToken);

        if (campaign is null) return;

        await MarkLiveReferenceSnapshotsStaleAsync(campaignId, "System cancelled", utcNow, cancellationToken);

        campaign.Status = "Cancelled";
        campaign.UpdatedAt = utcNow;

        if (campaign.CampaignSchedule is not null
            && campaign.CampaignSchedule.ExecutionStatus is "Waiting" or "Dispatched")
        {
            campaign.CampaignSchedule.ExecutionStatus = "Cancelled";
            campaign.CampaignSchedule.LockedByJobId = null;
            campaign.CampaignSchedule.LockedAt = null;
            campaign.CampaignSchedule.UpdatedAt = utcNow;
        }

        var note = approvalLogNote.Length > 500 ? approvalLogNote[..500] : approvalLogNote;
        await _context.CampaignApprovalLogs.AddAsync(new CampaignApprovalLog
        {
            CampaignId = campaignId,
            Action = "Cancelled",
            ActorId = actorAccountId,
            Note = note,
            CreatedAt = utcNow
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<List<int>> ListApprovedExpiredCampaignIdsAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        return _context.Campaigns
            .AsNoTracking()
            .Where(c => !c.IsDeleted
                     && c.Status == "Approved"
                     && c.ApprovedExpireAt != null
                     && c.ApprovedExpireAt <= utcNow)
            .Select(c => c.CampaignId)
            .ToListAsync(cancellationToken);
    }

    public Task<Campaign?> GetByEventKeyAsync(string eventKey, CancellationToken cancellationToken = default)
    {
        return _context.Campaigns
            .AsNoTracking()
            .Include(x => x.CampaignStat)
            .Include(x => x.CampaignTargets)
            .Include(x => x.TemplateCodeNavigation)
            .Include(x => x.CampaignSchedule)
            .Where(x => !x.IsDeleted && x.EventKey == eventKey && x.SourceType == "SYSTEM")
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
        // Tao Campaign o trang thai Draft — khong tu dong tao schedule
        var campaign = new Campaign
        {
            CampaignName = dto.CampaignName.Trim(),
            TemplateCode = string.IsNullOrWhiteSpace(dto.TemplateCode) ? null : dto.TemplateCode.Trim(),
            ReferenceType = string.IsNullOrWhiteSpace(dto.ReferenceType) ? null : dto.ReferenceType.Trim().ToUpper(),
            ReferenceId = dto.ReferenceId,
            TitleOverride = string.IsNullOrWhiteSpace(dto.TitleOverride) ? null : dto.TitleOverride.Trim(),
            MessageOverride = string.IsNullOrWhiteSpace(dto.MessageOverride) ? null : dto.MessageOverride.Trim(),
            SourceType = dto.SourceType,
            TargetType = dto.TargetType,
            Status = "Draft",
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

        if (dto.Targets.Count > 0)
        {
            var targets = dto.Targets.Select(t => new CampaignTarget
            {
                CampaignId = campaign.CampaignId,
                TargetType = t.TargetType.Trim(),
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
        campaign.UpdatedAt = DateTime.Now;
        _context.Campaigns.Update(campaign);

        // Replace targets: delete old, insert new
        var oldTargets = await _context.CampaignTargets
            .Where(t => t.CampaignId == campaign.CampaignId)
            .ToListAsync(cancellationToken);

        _context.CampaignTargets.RemoveRange(oldTargets);

        var targets = newTargets.Select(t => new CampaignTarget
        {
            CampaignId = campaign.CampaignId,
            TargetType = t.TargetType.Trim(),
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

    public async Task AddCampaignScheduleLogAsync(CampaignScheduleLog log, CancellationToken cancellationToken = default)
    {
        await _context.CampaignScheduleLogs.AddAsync(log, cancellationToken);
    }

    public async Task<bool> CancelAsync(int campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await _context.Campaigns
            .Include(x => x.CampaignSchedule)
            .Where(x => x.CampaignId == campaignId && !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (campaign is null) return false;

        campaign.Status = "Cancelled";
        campaign.UpdatedAt = DateTime.UtcNow;

        if (campaign.CampaignSchedule != null)
        {
            campaign.CampaignSchedule.ExecutionStatus = "Cancelled";
            campaign.CampaignSchedule.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<List<Campaign>> GetDueCampaignsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Campaigns
            .Include(c => c.CampaignTargets)
            .Include(c => c.TemplateCodeNavigation)
            .Include(c => c.CampaignSchedule)
            .Where(c => !c.IsDeleted
                     && c.Status == "Scheduled"
                     && c.CampaignSchedule != null
                     && c.CampaignSchedule.ScheduledAt <= now)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkSendingAsync(int campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await _context.Campaigns
            .Where(x => x.CampaignId == campaignId)
            .FirstOrDefaultAsync(cancellationToken);

        if (campaign is null) return;

        campaign.Status = "Sending";
        campaign.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkSentAsync(int campaignId, int totalSent, CancellationToken cancellationToken = default)
    {
        var campaign = await _context.Campaigns
            .Where(x => x.CampaignId == campaignId)
            .FirstOrDefaultAsync(cancellationToken);

        if (campaign is null) return;

        campaign.Status = "Sent";
        campaign.UpdatedAt = DateTime.Now;

        // Upsert CampaignStat
        var stat = await _context.CampaignStats
            .Where(s => s.CampaignId == campaignId)
            .FirstOrDefaultAsync(cancellationToken);

        if (stat is null)
        {
            stat = new CampaignStat
            {
                CampaignId = campaignId,
                TotalSent = totalSent,
                TotalRead = 0,
                TotalClicked = 0,
                ComputedAt = DateTime.Now
            };
            await _context.CampaignStats.AddAsync(stat, cancellationToken);
        }
        else
        {
            stat.TotalSent += totalSent;
            stat.ComputedAt = DateTime.Now;
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_CampaignStats_CampaignID") == true)
        {
            // Race condition: another process created the stat record.
            // Detach the failed new stat and try updating the existing one.
            if (stat != null && _context.Entry(stat).State == EntityState.Added)
            {
                _context.Entry(stat).State = EntityState.Detached;
            }

            var existingStat = await _context.CampaignStats
                .Where(s => s.CampaignId == campaignId)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingStat != null)
            {
                existingStat.TotalSent += totalSent;
                existingStat.ComputedAt = DateTime.Now;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }

    public async Task CreateDeliveriesAsync(List<Delivery> deliveries, CancellationToken cancellationToken = default)
    {
        if (deliveries.Count == 0) return;
        await _context.Deliveries.AddRangeAsync(deliveries, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Danh dau Campaign la Modified de DbContext theo doi va luu khi SaveChanges.
    /// </summary>
    public void Update(Campaign campaign)
    {
        _context.Campaigns.Update(campaign);
    }


    private IQueryable<Campaign> BuildBaseQuery(
        CampaignQueryDto query,
        bool forAdminList,
        int viewerAccountId)
    {
        var q = _context.Campaigns
            .AsNoTracking()
            .Include(x => x.CampaignSchedule)
            .Where(x => !x.IsDeleted);

        // Admin: hide other users' Draft campaigns until they submit for review (PendingApproval+).
        if (forAdminList && viewerAccountId > 0)
            q = q.Where(c => c.Status != "Draft" || c.CreatedByAccountId == viewerAccountId);

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim();
            q = q.Where(x =>
                x.CampaignName.Contains(term) ||
                (x.TemplateCode != null && x.TemplateCode.Contains(term)) ||
                (x.EventKey != null && x.EventKey.Contains(term)));
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
}
