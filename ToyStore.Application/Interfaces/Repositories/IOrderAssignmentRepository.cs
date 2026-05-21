using ToyStore.Application.DTOs.Assignments;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

public interface IOrderAssignmentRepository
{
    Task<bool> HasActiveAssignmentsAsync(int orderId, CancellationToken cancellationToken = default);

    Task<bool> HasActiveAssignmentAsync(
        int orderId,
        int accountId,
        byte roleId,
        CancellationToken cancellationToken = default);

    Task<List<OrderAssignment>> GetActiveAssignmentsAsync(int orderId, CancellationToken cancellationToken = default);

    Task<List<OrderAssignment>> GetActiveAssignmentsForOrdersAsync(List<int> orderIds, CancellationToken cancellationToken = default);

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

    Task<List<int>> GetPendingOrderIdsByScheduleAsync(int scheduleId, CancellationToken cancellationToken = default);

    Task DeactivateByScheduleAsync(int scheduleId, CancellationToken cancellationToken = default);

    Task<List<OrderAssignment>> GetActiveByScheduleAndRoleForStatusesAsync(
        int scheduleId,
        byte roleId,
        IReadOnlyCollection<string> statusNames,
        CancellationToken cancellationToken = default);

    Task<List<OrderAssignmentTransferItem>> TransferAssignmentsToAccountAsync(
        int scheduleId,
        byte roleId,
        int newAccountId,
        int assignedBy,
        string? note,
        IReadOnlyCollection<string> statusNames,
        CancellationToken cancellationToken = default);

    Task<List<int>> DeactivateByScheduleRoleAndAccountAsync(
        int scheduleId,
        byte roleId,
        int accountId,
        CancellationToken cancellationToken = default);
}
