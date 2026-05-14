using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

public interface IWorkScheduleRepository
{
    Task<List<WorkSchedule>> GetListAsync(
        DateTime? workDate,
        string? status,
        byte? roleId,
        CancellationToken cancellationToken = default);

    Task<WorkSchedule?> GetByIdAsync(int scheduleId, CancellationToken cancellationToken = default);

    Task<WorkSchedule?> GetByIdForUpdateAsync(int scheduleId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int accountId, DateTime workDate, byte shiftTemplateId, CancellationToken cancellationToken = default);

    Task<WorkSchedule> CreateAsync(WorkSchedule schedule, CancellationToken cancellationToken = default);
    Task UpdateAsync(WorkSchedule schedule, CancellationToken cancellationToken = default);
    Task DeleteAsync(WorkSchedule schedule, CancellationToken cancellationToken = default);
}
