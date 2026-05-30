using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs.Shifts;

namespace ToyStore.Application.Interfaces.Services;

public interface IWorkScheduleService
{
    Task<Result<WorkScheduleDto>> CreateAsync(CreateWorkScheduleDto dto, CancellationToken cancellationToken = default);

    Task<Result<List<WorkScheduleListDto>>> GetListAsync(WorkScheduleQueryDto query, CancellationToken cancellationToken = default);

    Task<Result<UpdateWorkScheduleResultDto>> UpdateAsync(int scheduleId, UpdateWorkScheduleDto dto, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int scheduleId, CancellationToken cancellationToken = default);

    Task<Result<MarkAbsentResultDto>> MarkAbsentAsync(int scheduleId, CancellationToken cancellationToken = default);

    Task<Result<CloneWeekResultDto>> CloneWeekAsync(DateTime sourceMonday, DateTime targetMonday, CancellationToken cancellationToken = default);

    Task<Result<TransferLoadResultDto>> TransferLoadAsync(
        int sourceScheduleId,
        TransferLoadRequestDto dto,
        CancellationToken cancellationToken = default);
}
