using ToyStore.Domain.Entities;
using ToyStore.Application.DTOs.Assignments;

namespace ToyStore.Application.Interfaces.Services;

public interface IShiftAssignmentService
{
    Task<Result<AssignmentResultDto>> AutoAssignOrderAsync(int orderId, CancellationToken cancellationToken = default);

    Task<Result> ReleaseCapacityAsync(int orderId, CancellationToken cancellationToken = default);

    Task<Result<List<OrderQueueItemDto>>> GetQueueAsync(CancellationToken cancellationToken = default);

    Task<Result> TryAssignOldestQueueAsync(CancellationToken cancellationToken = default);

    Task<Result> AssignQueueAsync(int queueId, AssignQueueOrderRequestDto dto, CancellationToken cancellationToken = default);

    Task<Result> ReassignOrderAsync(int orderId, ReassignOrderRequestDto dto, CancellationToken cancellationToken = default);

    Task<Result> UpdateMaxLoadAsync(int scheduleId, UpdateShiftCapacityDto dto, CancellationToken cancellationToken = default);
}
