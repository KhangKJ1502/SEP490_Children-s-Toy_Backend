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

    Task<List<WorkSchedule>> GetByDateRangeAsync(DateTime fromInclusive, DateTime toInclusive, CancellationToken cancellationToken = default);

    Task<List<WorkSchedule>> GetByAccountAndDateAsync(int accountId, DateTime workDate, CancellationToken cancellationToken = default);

    Task<WorkSchedule?> GetByUniqueKeyForUpdateAsync(int accountId, DateTime workDate, byte shiftTemplateId, CancellationToken cancellationToken = default);

    Task<int> CountActiveRoleOnShiftAsync(DateTime workDate, byte shiftTemplateId, byte roleId, int? excludeScheduleId, CancellationToken cancellationToken = default);

    Task<int> CountActiveByShiftTemplateAsync(byte shiftTemplateId, CancellationToken cancellationToken = default);

    Task<Dictionary<byte, int>> CountActiveByShiftTemplateIdsAsync(
        IReadOnlyCollection<byte> shiftTemplateIds,
        CancellationToken cancellationToken = default);

    Task<WorkSchedule> CreateAsync(WorkSchedule schedule, CancellationToken cancellationToken = default);
    Task UpdateAsync(WorkSchedule schedule, CancellationToken cancellationToken = default);
    Task DeleteAsync(WorkSchedule schedule, CancellationToken cancellationToken = default);
}
