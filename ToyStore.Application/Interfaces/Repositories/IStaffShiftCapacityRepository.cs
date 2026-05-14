using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

public interface IStaffShiftCapacityRepository
{
    Task<StaffShiftCapacity?> GetByScheduleIdAsync(int scheduleId, CancellationToken cancellationToken = default);

    Task<StaffShiftCapacity?> GetByScheduleIdForUpdateAsync(int scheduleId, CancellationToken cancellationToken = default);

    Task UpdateMaxLoadAsync(int scheduleId, short maxLoad, CancellationToken cancellationToken = default);
}
