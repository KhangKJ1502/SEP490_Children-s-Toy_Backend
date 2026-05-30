using ToyStore.Domain.Entities;
using ToyStore.Application.DTOs.Shifts;

namespace ToyStore.Application.Interfaces.Services;

public interface IShiftTemplateService
{
    Task<Result<List<ShiftTemplateListDto>>> GetListAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<Result<ShiftTemplateDto>> CreateAsync(CreateShiftTemplateDto dto, CancellationToken cancellationToken = default);

    Task<Result<ShiftTemplateDto>> UpdateAsync(byte shiftTemplateId, UpdateShiftTemplateDto dto, CancellationToken cancellationToken = default);
}
