using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class CampaignScheduleRepository : ICampaignScheduleRepository
{
    private readonly SEP490ToyStoreContext _context;

    public CampaignScheduleRepository(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Them moi mot ban ghi CampaignSchedule vao DbContext (chua SaveChanges).
    /// </summary>
    public async Task AddAsync(CampaignSchedule schedule, CancellationToken cancellationToken = default)
    {
        await _context.CampaignSchedules.AddAsync(schedule, cancellationToken);
    }
}
