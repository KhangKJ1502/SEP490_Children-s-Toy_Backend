using ToyStore.Application.Common.Models;
using ToyStore.Application.Constants;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Enforces order access using JWT identity and active OrderAssignments.
/// </summary>
public interface IOrderAccessService
{
    bool IsPrivileged(byte roleId);

    /// <summary>
    /// OrderAssignments.RoleID for the current operational role, or 0 if not Staff/Merch.
    /// </summary>
    byte GetRequiredAssignmentRoleId(byte roleId);

    Task<Result> EnsureCanViewAsync(int orderId, CancellationToken cancellationToken = default);

    Task<Result> EnsureCanMutateAsync(
        int orderId,
        OrderMutation mutation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restricts query to orders with an active assignment for the given account/role.
    /// Admin role bypasses (returns query unchanged).
    /// </summary>
    IQueryable<Order> ApplyOrderScope(IQueryable<Order> query, byte roleId, int accountId);
}
