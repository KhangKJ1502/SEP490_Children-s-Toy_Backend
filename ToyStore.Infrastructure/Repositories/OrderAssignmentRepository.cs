using Microsoft.EntityFrameworkCore;
using ToyStore.Application.DTOs.Assignments;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Constants;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class OrderAssignmentRepository : IOrderAssignmentRepository
{
    private readonly SEP490ToyStoreContext _context;
    private readonly IShiftCapacityMonitor _shiftCapacityMonitor;

    public OrderAssignmentRepository(SEP490ToyStoreContext context, IShiftCapacityMonitor shiftCapacityMonitor)
    {
        _context = context;
        _shiftCapacityMonitor = shiftCapacityMonitor;
    }

    public Task<bool> HasActiveAssignmentsAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return _context.OrderAssignments
            .AsNoTracking()
            .AnyAsync(x => x.OrderId == orderId && x.IsActive, cancellationToken);
    }

    public Task<bool> HasActiveAssignmentAsync(
        int orderId,
        int accountId,
        byte roleId,
        CancellationToken cancellationToken = default)
    {
        return _context.OrderAssignments
            .AsNoTracking()
            .AnyAsync(
                x => x.OrderId == orderId
                     && x.AccountId == accountId
                     && x.RoleId == roleId
                     && x.IsActive,
                cancellationToken);
    }

    public Task<List<OrderAssignment>> GetActiveAssignmentsAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return _context.OrderAssignments
            .Include(x => x.Account)
            .AsNoTracking()
            .Where(x => x.OrderId == orderId && x.IsActive)
            .ToListAsync(cancellationToken);
    }

    public Task<List<OrderAssignment>> GetActiveAssignmentsForOrdersAsync(List<int> orderIds, CancellationToken cancellationToken = default)
    {
        return _context.OrderAssignments
            .Include(x => x.Account)
            .AsNoTracking()
            .Where(x => orderIds.Contains(x.OrderId) && x.IsActive)
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
        var now = DateTime.UtcNow;
        var nowVn = now.AddHours(7); // Khớp múi giờ VN
        var todayVn = nowVn.Date;
        var timeVn = nowVn.TimeOfDay;

        byte staffRoleId = 3;
        byte merchRoleId = 4;

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // 0. Kiểm tra xem đơn đã được gán chưa (Tránh trùng lặp khi gọi từ nhiều Event)
            var activeAssignments = await _context.OrderAssignments
                .Where(oa => oa.OrderId == orderId && oa.IsActive)
                .ToListAsync(cancellationToken);

            bool hasStaff = activeAssignments.Any(a => a.RoleId == staffRoleId);
            bool hasMerch = activeAssignments.Any(a => a.RoleId == merchRoleId);

            if (hasStaff && hasMerch)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new AssignmentResultDto { Result = "ALREADY_ASSIGNED" };
            }

            // 1. Tìm Staff phù hợp nhất (đang trực, đúng giờ, ít đơn nhất)
            var staffCapacityQuery = from ssc in _context.StaffShiftCapacities
                                     join ws in _context.WorkSchedules on ssc.ScheduleId equals ws.ScheduleId
                                     join st in _context.ShiftTemplates on ws.ShiftTemplateId equals st.ShiftTemplateId
                                     join a in _context.Accounts on ssc.AccountId equals a.AccountId
                                     where ws.Status == "OnDuty"
                                        && ws.WorkDate.Date == todayVn
                                        && st.StartTime <= timeVn
                                        && st.EndTime >= timeVn
                                        && a.RoleId == staffRoleId
                                        && ssc.CurrentLoad < ssc.MaxLoad
                                     orderby ssc.CurrentLoad ascending, ssc.ScheduleId ascending
                                     select ssc;

            var staffResult = await staffCapacityQuery.FirstOrDefaultAsync(cancellationToken);

            // 2. Tìm Merch phù hợp nhất
            var merchCapacityQuery = from ssc in _context.StaffShiftCapacities
                                     join ws in _context.WorkSchedules on ssc.ScheduleId equals ws.ScheduleId
                                     join st in _context.ShiftTemplates on ws.ShiftTemplateId equals st.ShiftTemplateId
                                     join a in _context.Accounts on ssc.AccountId equals a.AccountId
                                     where ws.Status == "OnDuty"
                                        && ws.WorkDate.Date == todayVn
                                        && st.StartTime <= timeVn
                                        && st.EndTime >= timeVn
                                        && a.RoleId == merchRoleId
                                        && ssc.CurrentLoad < ssc.MaxLoad
                                     orderby ssc.CurrentLoad ascending, ssc.ScheduleId ascending
                                     select ssc;

            var merchResult = await merchCapacityQuery.FirstOrDefaultAsync(cancellationToken);

            // 3. Xử lý trường hợp thiếu nhân sự (Vào hàng chờ)
            if (staffResult == null || merchResult == null)
            {
                string queueReason;
                if (staffResult == null && merchResult == null)
                {
                    queueReason = "BOTH_FULL";
                }
                else if (staffResult == null)
                {
                    bool staffOnDuty = await _context.WorkSchedules
                        .AnyAsync(ws => ws.Status == "OnDuty" && ws.Account.RoleId == staffRoleId, cancellationToken);
                    queueReason = staffOnDuty ? "ALL_STAFF_FULL" : "NO_STAFF_ON_DUTY";
                }
                else
                {
                    bool merchOnDuty = await _context.WorkSchedules
                        .AnyAsync(ws => ws.Status == "OnDuty" && ws.Account.RoleId == merchRoleId, cancellationToken);
                    queueReason = merchOnDuty ? "ALL_MERCH_FULL" : "NO_MERCH_ON_DUTY";
                }

                bool alreadyQueued = await _context.OrderQueues
                    .AnyAsync(oq => oq.OrderId == orderId && !oq.IsResolved, cancellationToken);

                if (!alreadyQueued)
                {
                    var queueEntry = new OrderQueue
                    {
                        OrderId = orderId,
                        Reason = queueReason,
                        QueuedAt = now
                    };
                    await _context.OrderQueues.AddAsync(queueEntry, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                return new AssignmentResultDto { Result = "QUEUED", Reason = queueReason };
            }

            // 4. Nếu đủ nhân sự -> Phân đơn
            var assignments = new List<OrderAssignment>
            {
                new OrderAssignment
                {
                    OrderId = orderId,
                    ScheduleId = staffResult.ScheduleId,
                    AccountId = staffResult.AccountId,
                    RoleId = staffRoleId,
                    IsActive = true,
                    AssignedBy = assignedBy,
                    AssignedAt = now
                },
                new OrderAssignment
                {
                    OrderId = orderId,
                    ScheduleId = merchResult.ScheduleId,
                    AccountId = merchResult.AccountId,
                    RoleId = merchRoleId,
                    IsActive = true,
                    AssignedBy = assignedBy,
                    AssignedAt = now
                }
            };

            await _context.OrderAssignments.AddRangeAsync(assignments, cancellationToken);

            // Tăng tải
            staffResult.CurrentLoad++;
            staffResult.UpdatedAt = now;

            merchResult.CurrentLoad++;
            merchResult.UpdatedAt = now;

            await _context.SaveChangesAsync(cancellationToken);

            await _shiftCapacityMonitor.TryNotifyShiftFullAsync(staffResult.ScheduleId, cancellationToken);
            await _shiftCapacityMonitor.TryNotifyShiftFullAsync(merchResult.ScheduleId, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new AssignmentResultDto
            {
                Result = "ASSIGNED",
                StaffAccountId = staffResult.AccountId,
                MerchAccountId = merchResult.AccountId
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<int> ReleaseCapacityAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var activeAssignments = await _context.OrderAssignments
            .Where(oa => oa.OrderId == orderId && oa.IsActive)
            .ToListAsync(cancellationToken);

        if (!activeAssignments.Any()) return 0;

        var scheduleIds = activeAssignments.Select(oa => oa.ScheduleId).Distinct().ToList();

        var capacities = await _context.StaffShiftCapacities
            .Where(ssc => scheduleIds.Contains(ssc.ScheduleId))
            .ToListAsync(cancellationToken);

        if (!capacities.Any()) return 0;

        foreach (var cap in capacities)
        {
            if (cap.CurrentLoad > 0)
            {
                cap.CurrentLoad--;
            }
            cap.UpdatedAt = DateTime.UtcNow;
        }

        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ReassignAsync(int orderId, byte roleId, int newScheduleId, int assignedBy, string? notes, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var oldAssignment = await _context.OrderAssignments
                .FirstOrDefaultAsync(oa => oa.OrderId == orderId && oa.RoleId == roleId && oa.IsActive, cancellationToken);

            if (oldAssignment == null)
            {
                throw new InvalidOperationException("No active assignment found for this Order and Role");
            }

            oldAssignment.IsActive = false;

            var oldCapacity = await _context.StaffShiftCapacities
                .FirstOrDefaultAsync(c => c.ScheduleId == oldAssignment.ScheduleId, cancellationToken);

            if (oldCapacity != null)
            {
                if (oldCapacity.CurrentLoad > 0) oldCapacity.CurrentLoad--;
                oldCapacity.UpdatedAt = DateTime.UtcNow;
            }

            var newSchedule = await _context.WorkSchedules
                .FirstOrDefaultAsync(ws => ws.ScheduleId == newScheduleId, cancellationToken);

            if (newSchedule == null)
            {
                throw new InvalidOperationException("New schedule not found");
            }

            var newAssignment = new OrderAssignment
            {
                OrderId = orderId,
                ScheduleId = newScheduleId,
                AccountId = newSchedule.AccountId,
                RoleId = roleId,
                IsActive = true,
                AssignedBy = assignedBy,
                Notes = notes,
                AssignedAt = DateTime.UtcNow
            };

            await _context.OrderAssignments.AddAsync(newAssignment, cancellationToken);

            var newCapacity = await _context.StaffShiftCapacities
                .FirstOrDefaultAsync(c => c.ScheduleId == newScheduleId, cancellationToken);

            if (newCapacity != null)
            {
                newCapacity.CurrentLoad++;
                newCapacity.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (newCapacity != null)
                await _shiftCapacityMonitor.TryNotifyShiftFullAsync(newScheduleId, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<List<int>> GetPendingOrderIdsByScheduleAsync(int scheduleId, CancellationToken cancellationToken = default)
    {
        var query = from oa in _context.OrderAssignments.AsNoTracking()
                    join o in _context.Orders.AsNoTracking() on oa.OrderId equals o.OrderId
                    join s in _context.StatusOrders.AsNoTracking() on o.StatusId equals s.StatusId
                    where oa.ScheduleId == scheduleId
                       && oa.IsActive
                       && !o.IsDeleted
                       && s.StatusName == OrderStatuses.Pending
                    select o.OrderId;

        return await query.Distinct().ToListAsync(cancellationToken);
    }

    public async Task DeactivateByScheduleAsync(int scheduleId, CancellationToken cancellationToken = default)
    {
        var assignments = await _context.OrderAssignments
            .Where(oa => oa.ScheduleId == scheduleId && oa.IsActive)
            .ToListAsync(cancellationToken);

        if (!assignments.Any())
            return;

        var capacity = await _context.StaffShiftCapacities
            .FirstOrDefaultAsync(c => c.ScheduleId == scheduleId, cancellationToken);

        if (capacity != null)
        {
            var decrement = assignments.Count;
            capacity.CurrentLoad = (short)Math.Max(0, capacity.CurrentLoad - decrement);
            capacity.UpdatedAt = DateTime.UtcNow;
        }

        foreach (var a in assignments)
            a.IsActive = false;
    }

    public async Task<List<OrderAssignment>> GetActiveByScheduleAndRoleForStatusesAsync(
        int scheduleId,
        byte roleId,
        IReadOnlyCollection<string> statusNames,
        CancellationToken cancellationToken = default)
    {
        if (statusNames.Count == 0)
            return [];

        return await (
            from oa in _context.OrderAssignments
            join o in _context.Orders on oa.OrderId equals o.OrderId
            join s in _context.StatusOrders on o.StatusId equals s.StatusId
            where oa.ScheduleId == scheduleId
                  && oa.RoleId == roleId
                  && oa.IsActive
                  && !o.IsDeleted
                  && statusNames.Contains(s.StatusName)
            select oa
        ).ToListAsync(cancellationToken);
    }

    public async Task<List<OrderAssignmentTransferItem>> TransferAssignmentsToAccountAsync(
        int scheduleId,
        byte roleId,
        int newAccountId,
        int assignedBy,
        string? note,
        IReadOnlyCollection<string> statusNames,
        CancellationToken cancellationToken = default)
    {
        if (statusNames.Count == 0)
            return [];

        var rows = await (
            from oa in _context.OrderAssignments
            join o in _context.Orders on oa.OrderId equals o.OrderId
            join s in _context.StatusOrders on o.StatusId equals s.StatusId
            where oa.ScheduleId == scheduleId
                  && oa.RoleId == roleId
                  && oa.IsActive
                  && !o.IsDeleted
                  && statusNames.Contains(s.StatusName)
            select new { Assignment = oa, Order = o, Status = s }
        ).ToListAsync(cancellationToken);

        var transferred = new List<OrderAssignmentTransferItem>();
        var utcNow = DateTime.UtcNow;

        foreach (var row in rows)
        {
            row.Assignment.AccountId = newAccountId;
            row.Assignment.AssignedBy = assignedBy;
            row.Assignment.Notes = note;
            row.Assignment.AssignedAt = utcNow;

            transferred.Add(new OrderAssignmentTransferItem
            {
                OrderId = row.Order.OrderId,
                OrderCode = row.Order.OrderCode,
                StatusName = row.Status.StatusName
            });
        }

        return transferred;
    }

    public async Task<List<int>> DeactivateByScheduleRoleAndAccountAsync(
        int scheduleId,
        byte roleId,
        int accountId,
        CancellationToken cancellationToken = default)
    {
        var assignments = await _context.OrderAssignments
            .Where(oa => oa.ScheduleId == scheduleId
                         && oa.RoleId == roleId
                         && oa.AccountId == accountId
                         && oa.IsActive)
            .ToListAsync(cancellationToken);

        if (assignments.Count == 0)
            return [];

        var capacity = await _context.StaffShiftCapacities
            .FirstOrDefaultAsync(c => c.ScheduleId == scheduleId, cancellationToken);

        if (capacity != null)
        {
            capacity.CurrentLoad = (short)Math.Max(0, capacity.CurrentLoad - assignments.Count);
            capacity.UpdatedAt = DateTime.UtcNow;
        }

        foreach (var a in assignments)
            a.IsActive = false;

        return assignments.Select(a => a.OrderId).Distinct().ToList();
    }
}
