using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ToyStore.Application.DTOs.Assignments;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class OrderAssignmentRepository : IOrderAssignmentRepository
{
    private readonly SEP490ToyStoreContext _context;

    public OrderAssignmentRepository(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    public Task<bool> HasActiveAssignmentsAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return _context.OrderAssignments
            .AsNoTracking()
            .AnyAsync(x => x.OrderId == orderId && x.IsActive, cancellationToken);
    }

    public Task<List<OrderAssignment>> GetActiveAssignmentsAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return _context.OrderAssignments
            .AsNoTracking()
            .Where(x => x.OrderId == orderId && x.IsActive)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(OrderAssignment assignment, CancellationToken cancellationToken = default)
    {
        return _context.OrderAssignments.AddAsync(assignment, cancellationToken).AsTask();
    }

    public Task AddRangeAsync(IEnumerable<OrderAssignment> assignments, CancellationToken cancellationToken = default)
    {
        return _context.OrderAssignments.AddRangeAsync(assignments, cancellationToken);
    }

    public async Task<AssignmentResultDto> AutoAssignAsync(int orderId, int? assignedBy, CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "dbo.sp_AutoAssignOrder";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(new SqlParameter("@OrderID", orderId));
        command.Parameters.Add(new SqlParameter("@AssignedBy", (object?)assignedBy ?? DBNull.Value));

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new AssignmentResultDto
            {
                Result = reader["Result"]?.ToString() ?? string.Empty,
                Reason = reader["Reason"] as string,
                StaffAccountId = reader["StaffAccountID"] == DBNull.Value ? null : Convert.ToInt32(reader["StaffAccountID"]),
                MerchAccountId = reader["MerchAccountID"] == DBNull.Value ? null : Convert.ToInt32(reader["MerchAccountID"])
            };
        }

        return new AssignmentResultDto { Result = "UNKNOWN" };
    }

    public async Task<int> ReleaseCapacityAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "dbo.sp_ReleaseOrderCapacity";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(new SqlParameter("@OrderID", orderId));

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return reader["RowsAffected"] == DBNull.Value ? 0 : Convert.ToInt32(reader["RowsAffected"]);
        }

        return 0;
    }

    public Task ReassignAsync(int orderId, byte roleId, int newScheduleId, int assignedBy, string? notes, CancellationToken cancellationToken = default)
    {
        return _context.Database.ExecuteSqlRawAsync(
            "EXEC dbo.sp_ReassignOrder @OrderID, @RoleID, @NewScheduleID, @AssignedBy, @Notes",
            new object[]
            {
                new SqlParameter("@OrderID", orderId),
                new SqlParameter("@RoleID", roleId),
                new SqlParameter("@NewScheduleID", newScheduleId),
                new SqlParameter("@AssignedBy", assignedBy),
                new SqlParameter("@Notes", (object?)notes ?? DBNull.Value)
            },
            cancellationToken);
    }
}
