using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

/// <summary>
/// Repository thao tac du lieu CampaignSchedule.
/// </summary>
public interface ICampaignScheduleRepository
{
    /// <summary>
    /// Them moi mot ban ghi CampaignSchedule.
    /// </summary>
    Task AddAsync(CampaignSchedule schedule, CancellationToken cancellationToken = default);
}
