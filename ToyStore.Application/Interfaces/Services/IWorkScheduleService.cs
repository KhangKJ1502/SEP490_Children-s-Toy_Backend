using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs.Shifts;

namespace ToyStore.Application.Interfaces.Services;

public interface IWorkScheduleService
{
    Task<Result<WorkScheduleDto>> CreateAsync(CreateWorkScheduleDto dto, CancellationToken cancellationToken = default);

    Task<Result<List<WorkScheduleListDto>>> GetListAsync(WorkScheduleQueryDto query, CancellationToken cancellationToken = default);

    Task<Result<WorkScheduleDto>> UpdateAsync(int scheduleId, UpdateWorkScheduleDto dto, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int scheduleId, CancellationToken cancellationToken = default);

    Task<Result> MarkAbsentAsync(int scheduleId, CancellationToken cancellationToken = default);
}
