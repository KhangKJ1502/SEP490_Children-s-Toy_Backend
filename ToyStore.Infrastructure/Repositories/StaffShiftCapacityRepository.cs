using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class StaffShiftCapacityRepository : IStaffShiftCapacityRepository
{
    private readonly SEP490ToyStoreContext _context;

    public StaffShiftCapacityRepository(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    public Task<StaffShiftCapacity?> GetByScheduleIdAsync(int scheduleId, CancellationToken cancellationToken = default)
    {
        return _context.StaffShiftCapacities
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ScheduleId == scheduleId, cancellationToken);
    }

    public Task<StaffShiftCapacity?> GetByScheduleIdForUpdateAsync(int scheduleId, CancellationToken cancellationToken = default)
    {
        return _context.StaffShiftCapacities
            .FirstOrDefaultAsync(x => x.ScheduleId == scheduleId, cancellationToken);
    }

    public async Task UpdateMaxLoadAsync(int scheduleId, short maxLoad, CancellationToken cancellationToken = default)
    {
        var capacity = await _context.StaffShiftCapacities
            .FirstOrDefaultAsync(x => x.ScheduleId == scheduleId, cancellationToken);

        if (capacity is null) return;

        capacity.MaxLoad = maxLoad;
        capacity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
