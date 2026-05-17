using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class CampaignApprovalLogRepository : ICampaignApprovalLogRepository
{
    private readonly SEP490ToyStoreContext _context;

    public CampaignApprovalLogRepository(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Them moi mot ban ghi audit log vao DbContext (chua SaveChanges).
    /// </summary>
    public async Task AddAsync(CampaignApprovalLog log, CancellationToken cancellationToken = default)
    {
        await _context.CampaignApprovalLogs.AddAsync(log, cancellationToken);
    }

    /// <summary>
    /// Lay danh sach log theo CampaignID sap xep theo thoi gian tang dan.
    /// </summary>
    public async Task<List<CampaignApprovalLog>> GetByCampaignIdAsync(
        int campaignId,
        CancellationToken cancellationToken = default)
    {
        return await _context.CampaignApprovalLogs
            .AsNoTracking()
            .Where(x => x.CampaignId == campaignId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
