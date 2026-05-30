using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

public interface IShiftTemplateRepository
{
    Task<List<ShiftTemplate>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<List<ShiftTemplate>> GetAllOrderedAsync(CancellationToken cancellationToken = default);

    Task<ShiftTemplate?> GetByIdAsync(byte shiftTemplateId, CancellationToken cancellationToken = default);

    Task<ShiftTemplate?> GetByIdForUpdateAsync(byte shiftTemplateId, CancellationToken cancellationToken = default);

    Task<bool> HasOverlappingActiveTemplateAsync(
        TimeSpan startTime,
        TimeSpan endTime,
        byte? excludeShiftTemplateId = null,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(string shiftName, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameExceptIdAsync(string shiftName, byte shiftTemplateId, CancellationToken cancellationToken = default);

    Task<ShiftTemplate> CreateAsync(ShiftTemplate template, CancellationToken cancellationToken = default);

    Task<ShiftTemplate> UpdateAsync(ShiftTemplate template, CancellationToken cancellationToken = default);
}
