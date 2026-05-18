using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

/// <summary>
/// Repository thao tac du lieu CampaignApprovalLog.
/// </summary>
public interface ICampaignApprovalLogRepository
{
    /// <summary>
    /// Them moi mot ban ghi audit log.
    /// </summary>
    Task AddAsync(CampaignApprovalLog log, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay danh sach log theo CampaignID.
    /// </summary>
    Task<List<CampaignApprovalLog>> GetByCampaignIdAsync(int campaignId, CancellationToken cancellationToken = default);
}
