using ToyStore.Application.DTOs.Assignments;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

public interface IOrderAssignmentRepository
{
    Task<bool> HasActiveAssignmentsAsync(int orderId, CancellationToken cancellationToken = default);

    Task<List<OrderAssignment>> GetActiveAssignmentsAsync(int orderId, CancellationToken cancellationToken = default);

    Task AddAsync(OrderAssignment assignment, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<OrderAssignment> assignments, CancellationToken cancellationToken = default);

    Task<AssignmentResultDto> AutoAssignAsync(int orderId, int? assignedBy, CancellationToken cancellationToken = default);

    Task<int> ReleaseCapacityAsync(int orderId, CancellationToken cancellationToken = default);

    Task ReassignAsync(
        int orderId,
        byte roleId,
        int newScheduleId,
        int assignedBy,
        string? notes,
        CancellationToken cancellationToken = default);
}
