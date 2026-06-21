using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ToyStore.Application.DTOs.Assignments;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Constants;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Enums;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class OrderAssignmentRepository : IOrderAssignmentRepository
{
    private readonly SEP490ToyStoreContext _context;
    private readonly IShiftCapacityMonitor _shiftCapacityMonitor;
    private readonly ITimeProvider _timeProvider;

    private const byte StaffRoleId = 3;
    private const byte MerchRoleId = 4;

    public OrderAssignmentRepository(
        SEP490ToyStoreContext context,
        IShiftCapacityMonitor shiftCapacityMonitor,
        ITimeProvider timeProvider)
    {
        _context = context;
        _shiftCapacityMonitor = shiftCapacityMonitor;
        _timeProvider = timeProvider;
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

    public Task<bool> HasAssignmentForAccountAsync(
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
                     && x.RoleId == roleId,
                cancellationToken);
    }

    public Task<bool> HasProcessedOrderByAssigneeAsync(
        int orderId,
        int accountId,
        byte assignmentRoleId,
        CancellationToken cancellationToken = default)
    {
        var milestoneStatuses = OrderStatuses.GetProcessedMilestoneStatuses(assignmentRoleId);

        return _context.OrderStatusHistories
            .AsNoTracking()
            .AnyAsync(
                h => h.OrderId == orderId
                     && h.ChangedBy == accountId
                     && h.Status != null
                     && milestoneStatuses.Contains(h.Status.StatusName),
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

    public Task<List<OrderAssignment>> GetAssignmentsByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return _context.OrderAssignments
            .Include(x => x.Account)
            .Where(x => x.OrderId == orderId)
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
        var now = _timeProvider.UtcNow;
        var todayVn = _timeProvider.TodayVn;
        var timeVn = _timeProvider.VnNow.TimeOfDay;

        var ownsTransaction = _context.Database.CurrentTransaction is null;
        IDbContextTransaction? transaction = null;
        if (ownsTransaction)
        {
            transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        try
        {
            var activeAssignments = await _context.OrderAssignments
                .Where(oa => oa.OrderId == orderId && oa.IsActive)
                .ToListAsync(cancellationToken);

            var hasStaff = activeAssignments.Any(a => a.RoleId == StaffRoleId);
            var hasMerch = activeAssignments.Any(a => a.RoleId == MerchRoleId);

            if (hasStaff && hasMerch)
            {
                if (ownsTransaction)
                {
                    await transaction!.RollbackAsync(cancellationToken);
                }

                return new AssignmentResultDto
                {
                    Result = "ALREADY_ASSIGNED",
                    StaffAccountId = activeAssignments.First(a => a.RoleId == StaffRoleId).AccountId,
                    MerchAccountId = activeAssignments.First(a => a.RoleId == MerchRoleId).AccountId
                };
            }

            StaffShiftCapacity? staffCapacity = null;
            StaffShiftCapacity? merchCapacity = null;

            if (!hasStaff)
            {
                staffCapacity = await FindAndClaimCapacityAsync(StaffRoleId, todayVn, timeVn, now, cancellationToken);
            }

            if (!hasMerch)
            {
                merchCapacity = await FindAndClaimCapacityAsync(MerchRoleId, todayVn, timeVn, now, cancellationToken);
            }

            var needStaff = !hasStaff;
            var needMerch = !hasMerch;

            var newAssignments = new List<OrderAssignment>();

            if (needStaff && staffCapacity is not null)
            {
                newAssignments.Add(new OrderAssignment
                {
                    OrderId = orderId,
                    ScheduleId = staffCapacity.ScheduleId,
                    AccountId = staffCapacity.AccountId,
                    RoleId = StaffRoleId,
                    IsActive = true,
                    AssignedBy = assignedBy,
                    AssignedAt = now
                });
            }

            if (needMerch && merchCapacity is not null)
            {
                newAssignments.Add(new OrderAssignment
                {
                    OrderId = orderId,
                    ScheduleId = merchCapacity.ScheduleId,
                    AccountId = merchCapacity.AccountId,
                    RoleId = MerchRoleId,
                    IsActive = true,
                    AssignedBy = assignedBy,
                    AssignedAt = now
                });
            }

            // Nếu không thể phân công được bất kỳ vai trò mới nào và vẫn đang cần phân công
            if (newAssignments.Count == 0 && (needStaff || needMerch))
            {
                var queueReason = await BuildQueueReasonAsync(
                    needStaff, staffCapacity, needMerch, merchCapacity, cancellationToken);
                await UpsertQueueEntryAsync(orderId, queueReason, now, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                if (ownsTransaction)
                {
                    await transaction!.CommitAsync(cancellationToken);
                }

                return new AssignmentResultDto { Result = "QUEUED", Reason = queueReason };
            }

            // Lưu các phân công mới được tạo
            if (newAssignments.Count > 0)
            {
                await _context.OrderAssignments.AddRangeAsync(newAssignments, cancellationToken);
            }

            // Chỉ đánh dấu hoàn thành Queue khi cả 2 vai trò đều được lấp đầy (đã được gán từ trước hoặc mới được gán thêm)
            var hasAllRequired = (hasStaff || staffCapacity is not null) && (hasMerch || merchCapacity is not null);

            if (hasAllRequired)
            {
                var pendingQueue = await _context.OrderQueues
                    .FirstOrDefaultAsync(oq => oq.OrderId == orderId && !oq.IsResolved, cancellationToken);

                if (pendingQueue is not null)
                {
                    pendingQueue.IsResolved = true;
                    pendingQueue.ResolvedAt = now;
                }
            }
            else
            {
                // Vẫn đang thiếu vai trò còn lại, cập nhật Queue với lý do tương ứng
                var missingStaff = !hasStaff && staffCapacity is null;
                var missingMerch = !hasMerch && merchCapacity is null;
                var queueReason = await BuildQueueReasonAsync(
                    missingStaff, staffCapacity, missingMerch, merchCapacity, cancellationToken);
                await UpsertQueueEntryAsync(orderId, queueReason, now, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (needStaff && staffCapacity is not null)
            {
                await _shiftCapacityMonitor.TryNotifyShiftFullAsync(staffCapacity.ScheduleId, cancellationToken);
            }

            if (needMerch && merchCapacity is not null)
            {
                await _shiftCapacityMonitor.TryNotifyShiftFullAsync(merchCapacity.ScheduleId, cancellationToken);
            }

            if (ownsTransaction)
            {
                await transaction!.CommitAsync(cancellationToken);
            }

            var staffAccountId = hasStaff
                ? activeAssignments.First(a => a.RoleId == StaffRoleId).AccountId
                : staffCapacity?.AccountId;
            var merchAccountId = hasMerch
                ? activeAssignments.First(a => a.RoleId == MerchRoleId).AccountId
                : merchCapacity?.AccountId;

            return new AssignmentResultDto
            {
                Result = "ASSIGNED",
                StaffAccountId = staffAccountId,
                MerchAccountId = merchAccountId
            };
        }
        catch
        {
            if (ownsTransaction)
            {
                await transaction!.RollbackAsync(cancellationToken);
            }

            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    public async Task<int> ReleaseCapacityAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var ownsTransaction = _context.Database.CurrentTransaction is null;
        IDbContextTransaction? transaction = null;
        if (ownsTransaction)
        {
            transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        try
        {
            var activeAssignments = await _context.OrderAssignments
                .Where(oa => oa.OrderId == orderId && oa.IsActive)
                .ToListAsync(cancellationToken);

            if (activeAssignments.Count == 0)
            {
                if (ownsTransaction)
                {
                    await transaction!.CommitAsync(cancellationToken);
                }

                return 0;
            }

            foreach (var assignment in activeAssignments)
            {
                assignment.IsActive = false;
            }

            var scheduleIds = activeAssignments.Select(oa => oa.ScheduleId).Distinct().ToList();
            var capacities = await _context.StaffShiftCapacities
                .Where(ssc => scheduleIds.Contains(ssc.ScheduleId))
                .ToListAsync(cancellationToken);

            var now = _timeProvider.UtcNow;
            foreach (var cap in capacities)
            {
                var decrement = activeAssignments.Count(a => a.ScheduleId == cap.ScheduleId);
                if (decrement > 0)
                {
                    cap.CurrentLoad = (short)Math.Max(0, cap.CurrentLoad - decrement);
                }

                cap.UpdatedAt = now;
            }

            var count = await _context.SaveChangesAsync(cancellationToken);

            if (ownsTransaction)
            {
                await transaction!.CommitAsync(cancellationToken);
            }

            return count;
        }
        catch
        {
            if (ownsTransaction)
            {
                await transaction!.RollbackAsync(cancellationToken);
            }

            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    public async Task ReassignAsync(int orderId, byte roleId, int newScheduleId, int assignedBy, string? notes, CancellationToken cancellationToken = default)
    {
        var ownsTransaction = _context.Database.CurrentTransaction is null;
        IDbContextTransaction? transaction = null;
        if (ownsTransaction)
        {
            transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        try
        {
            var oldAssignment = await _context.OrderAssignments
                .FirstOrDefaultAsync(oa => oa.OrderId == orderId && oa.RoleId == roleId && oa.IsActive, cancellationToken);

            if (oldAssignment is null)
            {
                throw new InvalidOperationException("No active assignment found for this Order and Role");
            }

            oldAssignment.IsActive = false;

            await LockCapacityRowAsync(oldAssignment.ScheduleId, cancellationToken);

            var oldCapacity = await _context.StaffShiftCapacities
                .FirstOrDefaultAsync(c => c.ScheduleId == oldAssignment.ScheduleId, cancellationToken);

            if (oldCapacity is not null)
            {
                if (oldCapacity.CurrentLoad > 0)
                {
                    oldCapacity.CurrentLoad--;
                }

                oldCapacity.UpdatedAt = _timeProvider.UtcNow;
            }

            var newSchedule = await _context.WorkSchedules
                .FirstOrDefaultAsync(ws => ws.ScheduleId == newScheduleId, cancellationToken);

            if (newSchedule is null)
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
                AssignedAt = _timeProvider.UtcNow
            };

            await _context.OrderAssignments.AddAsync(newAssignment, cancellationToken);

            await LockCapacityRowAsync(newScheduleId, cancellationToken);

            var newCapacity = await _context.StaffShiftCapacities
                .FirstOrDefaultAsync(c => c.ScheduleId == newScheduleId, cancellationToken);

            if (newCapacity is null)
            {
                throw new InvalidOperationException("Capacity row not found for target schedule");
            }

            if (newCapacity.CurrentLoad >= newCapacity.MaxLoad)
            {
                throw new InvalidOperationException("Target schedule is at full capacity");
            }

            newCapacity.CurrentLoad++;
            newCapacity.UpdatedAt = _timeProvider.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            if (newCapacity is not null)
            {
                await _shiftCapacityMonitor.TryNotifyShiftFullAsync(newScheduleId, cancellationToken);
            }

            if (ownsTransaction)
            {
                await transaction!.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            if (ownsTransaction)
            {
                await transaction!.RollbackAsync(cancellationToken);
            }

            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
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

    public async Task<List<OrderAssignmentWithStatus>> GetActiveByScheduleWithStatusAsync(
        int scheduleId,
        CancellationToken cancellationToken = default)
    {
        return await (
            from oa in _context.OrderAssignments
            join o in _context.Orders on oa.OrderId equals o.OrderId
            join s in _context.StatusOrders on o.StatusId equals s.StatusId
            where oa.ScheduleId == scheduleId
                  && oa.IsActive
                  && !o.IsDeleted
            select new OrderAssignmentWithStatus
            {
                Assignment = oa,
                StatusName = s.StatusName
            }
        ).ToListAsync(cancellationToken);
    }

    public async Task DeactivateAssignmentAsync(OrderAssignment assignment, CancellationToken cancellationToken = default)
    {
        assignment.IsActive = false;

        var capacity = await _context.StaffShiftCapacities
            .FirstOrDefaultAsync(c => c.ScheduleId == assignment.ScheduleId, cancellationToken);

        if (capacity is not null && capacity.CurrentLoad > 0)
        {
            capacity.CurrentLoad--;
            capacity.UpdatedAt = _timeProvider.UtcNow;
        }
    }

    public async Task DeactivateByScheduleAsync(int scheduleId, CancellationToken cancellationToken = default)
    {
        var assignments = await _context.OrderAssignments
            .Where(oa => oa.ScheduleId == scheduleId && oa.IsActive)
            .ToListAsync(cancellationToken);

        if (assignments.Count == 0)
        {
            return;
        }

        var capacity = await _context.StaffShiftCapacities
            .FirstOrDefaultAsync(c => c.ScheduleId == scheduleId, cancellationToken);

        if (capacity is not null)
        {
            capacity.CurrentLoad = (short)Math.Max(0, capacity.CurrentLoad - assignments.Count);
            capacity.UpdatedAt = _timeProvider.UtcNow;
        }

        foreach (var a in assignments)
        {
            a.IsActive = false;
        }
    }

    public async Task<List<OrderAssignment>> GetActiveByScheduleAndRoleForStatusesAsync(
        int scheduleId,
        byte roleId,
        IReadOnlyCollection<string> statusNames,
        CancellationToken cancellationToken = default)
    {
        if (statusNames.Count == 0)
        {
            return [];
        }

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
        {
            return [];
        }

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
        var utcNow = _timeProvider.UtcNow;

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
        {
            return [];
        }

        var capacity = await _context.StaffShiftCapacities
            .FirstOrDefaultAsync(c => c.ScheduleId == scheduleId, cancellationToken);

        if (capacity is not null)
        {
            capacity.CurrentLoad = (short)Math.Max(0, capacity.CurrentLoad - assignments.Count);
            capacity.UpdatedAt = _timeProvider.UtcNow;
        }

        foreach (var a in assignments)
        {
            a.IsActive = false;
        }

        return assignments.Select(a => a.OrderId).Distinct().ToList();
    }

    public async Task<List<int>> GetOrdersMissingFullAssignmentAsync(CancellationToken cancellationToken = default)
    {
        var operationalStatuses = new[]
        {
            OrderStatuses.Pending,
            OrderStatuses.Confirmed,
            OrderStatuses.Processing,
            OrderStatuses.Shipped,
            OrderStatuses.Delivering,
            OrderStatuses.Delivered,
            OrderStatuses.DeliveryFailed
        };

        return await (
            from o in _context.Orders
            join s in _context.StatusOrders on o.StatusId equals s.StatusId
            where !o.IsDeleted
                  && (o.PaymentStatus == "PAID" || o.PaymentStatus == "COD_PENDING")
                  && (operationalStatuses.Contains(s.StatusName)
                      || _context.OrderRefunds.Any(r => r.OrderId == o.OrderId && !r.IsDeleted
                          && r.StatusId != (byte)RefundStatusEnum.RefundCompleted
                          && r.StatusId != (byte)RefundStatusEnum.RefundCancelled
                          && r.StatusId != (byte)RefundStatusEnum.RefundRejected
                          && r.StatusId != (byte)RefundStatusEnum.RefundReturnedToCustomer
                          && r.StatusId != (byte)RefundStatusEnum.RefundReturnToCustomerFailed))
                  && (
                      !_context.OrderAssignments.Any(oa => oa.OrderId == o.OrderId && oa.IsActive && oa.RoleId == StaffRoleId)
                      || !_context.OrderAssignments.Any(oa => oa.OrderId == o.OrderId && oa.IsActive && oa.RoleId == MerchRoleId)
                  )
            select o.OrderId
        ).Distinct()
            .Take(50)
            .ToListAsync(cancellationToken);
    }

    private async Task<StaffShiftCapacity?> FindAndClaimCapacityAsync(
        byte roleId,
        DateTime todayVn,
        TimeSpan timeVn,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var candidateScheduleIds = await (
            from ssc in _context.StaffShiftCapacities.AsNoTracking()
            join ws in _context.WorkSchedules.AsNoTracking() on ssc.ScheduleId equals ws.ScheduleId
            join st in _context.ShiftTemplates.AsNoTracking() on ws.ShiftTemplateId equals st.ShiftTemplateId
            join a in _context.Accounts.AsNoTracking() on ssc.AccountId equals a.AccountId
            where ws.Status == "OnDuty"
                  && ws.WorkDate.Date == todayVn
                  && st.StartTime <= timeVn
                  && st.EndTime >= timeVn
                  && a.RoleId == roleId
                  && a.IsActive
                  && !a.IsDeleted
                  && ssc.CurrentLoad < ssc.MaxLoad
            orderby ssc.CurrentLoad ascending, ssc.ScheduleId ascending
            select ssc.ScheduleId
        ).ToListAsync(cancellationToken);

        foreach (var scheduleId in candidateScheduleIds)
        {
            await LockCapacityRowAsync(scheduleId, cancellationToken);

            var capacity = await _context.StaffShiftCapacities
                .FirstOrDefaultAsync(c => c.ScheduleId == scheduleId, cancellationToken);

            if (capacity is null || capacity.CurrentLoad >= capacity.MaxLoad)
            {
                continue;
            }

            capacity.CurrentLoad++;
            capacity.UpdatedAt = now;
            return capacity;
        }

        return null;
    }

    private async Task RollbackCapacityClaimAsync(int scheduleId, DateTime now, CancellationToken cancellationToken)
    {
        await LockCapacityRowAsync(scheduleId, cancellationToken);

        var capacity = await _context.StaffShiftCapacities
            .FirstOrDefaultAsync(c => c.ScheduleId == scheduleId, cancellationToken);

        if (capacity is null || capacity.CurrentLoad <= 0)
        {
            return;
        }

        capacity.CurrentLoad--;
        capacity.UpdatedAt = now;
    }

    private async Task LockCapacityRowAsync(int scheduleId, CancellationToken cancellationToken)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM [StaffShiftCapacity] WITH (UPDLOCK, ROWLOCK) WHERE [ScheduleID] = {scheduleId}",
            cancellationToken);
    }

    private async Task<string> BuildQueueReasonAsync(
        bool needStaff,
        StaffShiftCapacity? staffCapacity,
        bool needMerch,
        StaffShiftCapacity? merchCapacity,
        CancellationToken cancellationToken)
    {
        if (needStaff && staffCapacity is null && needMerch && merchCapacity is null)
        {
            return "BOTH_FULL";
        }

        if (needStaff && staffCapacity is null)
        {
            var staffOnDuty = await _context.WorkSchedules
                .AnyAsync(ws => ws.Status == "OnDuty" && ws.Account.RoleId == StaffRoleId, cancellationToken);
            return staffOnDuty ? "ALL_STAFF_FULL" : "NO_STAFF_ON_DUTY";
        }

        var merchOnDuty = await _context.WorkSchedules
            .AnyAsync(ws => ws.Status == "OnDuty" && ws.Account.RoleId == MerchRoleId, cancellationToken);
        return merchOnDuty ? "ALL_MERCH_FULL" : "NO_MERCH_ON_DUTY";
    }

    private async Task UpsertQueueEntryAsync(int orderId, string reason, DateTime now, CancellationToken cancellationToken)
    {
        var existing = await _context.OrderQueues
            .FirstOrDefaultAsync(oq => oq.OrderId == orderId && !oq.IsResolved, cancellationToken);

        if (existing is not null)
        {
            existing.Reason = reason;
            return;
        }

        await _context.OrderQueues.AddAsync(new OrderQueue
        {
            OrderId  = orderId,
            Reason   = reason,
            QueuedAt = now
        }, cancellationToken);
    }
}
