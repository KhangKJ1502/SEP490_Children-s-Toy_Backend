using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Common.Models;
using ToyStore.Application.Constants;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Services;

public class OrderAccessService : IOrderAccessService
{
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SEP490ToyStoreContext _context;

    public OrderAccessService(
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        SEP490ToyStoreContext context)
    {
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public bool IsPrivileged(byte roleId) => roleId == OrderAccessRoles.Admin;

    public byte GetRequiredAssignmentRoleId(byte roleId) =>
        roleId switch
        {
            OrderAccessRoles.Staff => OrderAccessRoles.AssignmentStaff,
            OrderAccessRoles.Merchandise => OrderAccessRoles.AssignmentMerchandise,
            _ => 0
        };

    public async Task<Result> EnsureCanViewAsync(int orderId, CancellationToken cancellationToken = default)
    {
        if (orderId <= 0)
        {
            return Result.Failure("VALIDATION_ERROR", "Order ID must be greater than 0.");
        }

        if (IsPrivileged(_currentUser.RoleId))
        {
            return Result.Success();
        }

        var assignmentRoleId = GetRequiredAssignmentRoleId(_currentUser.RoleId);
        if (assignmentRoleId == 0)
        {
            return Result.Failure("FORBIDDEN", "You are not authorized to view this order.");
        }

        var hasActiveAssignment = await _unitOfWork.OrderAssignments.HasActiveAssignmentAsync(
            orderId,
            _currentUser.AccountId,
            assignmentRoleId,
            cancellationToken);

        if (hasActiveAssignment)
        {
            return Result.Success();
        }

        var hasAnyAssignment = await _unitOfWork.OrderAssignments.HasAssignmentForAccountAsync(
            orderId,
            _currentUser.AccountId,
            assignmentRoleId,
            cancellationToken);

        if (!hasAnyAssignment)
        {
            return Result.Failure("FORBIDDEN", "You are not authorized to view this order.");
        }

        var hasProcessed = await _unitOfWork.OrderAssignments.HasProcessedOrderByAssigneeAsync(
            orderId,
            _currentUser.AccountId,
            assignmentRoleId,
            cancellationToken);

        if (!hasProcessed)
        {
            return Result.Failure("FORBIDDEN", "You are not authorized to view this order.");
        }

        return Result.Success();
    }

    public async Task<Result> EnsureCanMutateAsync(
        int orderId,
        OrderMutation mutation,
        CancellationToken cancellationToken = default)
    {
        if (orderId <= 0)
        {
            return Result.Failure("VALIDATION_ERROR", "Order ID must be greater than 0.");
        }

        if (IsPrivileged(_currentUser.RoleId))
        {
            return Result.Success();
        }

        var assignmentRoleId = GetRequiredAssignmentRoleId(_currentUser.RoleId);
        if (assignmentRoleId == 0)
        {
            return Result.Failure("FORBIDDEN", "You are not authorized to modify this order.");
        }

        var mutationAllowed = mutation switch
        {
            OrderMutation.Confirm or OrderMutation.Cancel => assignmentRoleId == OrderAccessRoles.AssignmentStaff,
            OrderMutation.Process or OrderMutation.Ship => assignmentRoleId == OrderAccessRoles.AssignmentMerchandise,
            _ => false
        };

        if (!mutationAllowed)
        {
            return Result.Failure("FORBIDDEN", "Your role cannot perform this action on orders.");
        }

        var hasAssignment = await _unitOfWork.OrderAssignments.HasActiveAssignmentAsync(
            orderId,
            _currentUser.AccountId,
            assignmentRoleId,
            cancellationToken);

        if (!hasAssignment)
        {
            return Result.Failure("FORBIDDEN", "You do not have an active assignment for this order.");
        }

        return Result.Success();
    }

    public IQueryable<Order> ApplyOrderScope(IQueryable<Order> query, byte roleId, int accountId)
    {
        if (IsPrivileged(roleId))
        {
            return query;
        }

        var assignmentRoleId = GetRequiredAssignmentRoleId(roleId);
        if (assignmentRoleId == 0)
        {
            return query.Where(_ => false);
        }

        return query.Where(o => _context.OrderAssignments
            .Any(oa => oa.OrderId == o.OrderId
                       && oa.AccountId == accountId
                       && oa.RoleId == assignmentRoleId
                       && oa.IsActive));
    }
}
