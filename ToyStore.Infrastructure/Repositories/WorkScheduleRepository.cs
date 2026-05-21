using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class WorkScheduleRepository : IWorkScheduleRepository
{
    private readonly SEP490ToyStoreContext _context;

    public WorkScheduleRepository(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    public async Task<List<WorkSchedule>> GetListAsync(
        DateTime? workDate,
        string? status,
        byte? roleId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.WorkSchedules
            .AsNoTracking()
            .Include(x => x.Account)
            .Include(x => x.ShiftTemplate)
            .Include(x => x.StaffShiftCapacity)
            .AsQueryable();

        if (workDate.HasValue)
        {
            var date = workDate.Value.Date;
            query = query.Where(x => x.WorkDate == date);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        if (roleId.HasValue)
        {
            query = query.Where(x => x.Account.RoleId == roleId.Value);
        }

        return await query
            .OrderBy(x => x.WorkDate)
            .ThenBy(x => x.ShiftTemplateId)
            .ToListAsync(cancellationToken);
    }

    public Task<WorkSchedule?> GetByIdAsync(int scheduleId, CancellationToken cancellationToken = default)
    {
        return _context.WorkSchedules
            .AsNoTracking()
            .Include(x => x.Account)
            .Include(x => x.ShiftTemplate)
            .Include(x => x.StaffShiftCapacity)
            .FirstOrDefaultAsync(x => x.ScheduleId == scheduleId, cancellationToken);
    }

    public Task<WorkSchedule?> GetByIdForUpdateAsync(int scheduleId, CancellationToken cancellationToken = default)
    {
        return _context.WorkSchedules
            .Include(x => x.Account)
            .Include(x => x.ShiftTemplate)
            .Include(x => x.StaffShiftCapacity)
            .FirstOrDefaultAsync(x => x.ScheduleId == scheduleId, cancellationToken);
    }

    public Task<bool> ExistsAsync(int accountId, DateTime workDate, byte shiftTemplateId, CancellationToken cancellationToken = default)
    {
        var date = workDate.Date;
        return _context.WorkSchedules
            .AsNoTracking()
            .AnyAsync(x => x.AccountId == accountId
                && x.WorkDate == date
                && x.ShiftTemplateId == shiftTemplateId
                && x.Status != "Absent"
                && x.Status != "Cancelled", cancellationToken);
    }

    public Task<List<WorkSchedule>> GetByDateRangeAsync(
        DateTime fromInclusive,
        DateTime toInclusive,
        CancellationToken cancellationToken = default)
    {
        var from = fromInclusive.Date;
        var to = toInclusive.Date;

        return _context.WorkSchedules
            .AsNoTracking()
            .Where(x => x.WorkDate >= from && x.WorkDate <= to)
            .Include(x => x.Account)
            .Include(x => x.ShiftTemplate)
            .Include(x => x.StaffShiftCapacity)
            .OrderBy(x => x.WorkDate)
            .ThenBy(x => x.AccountId)
            .ToListAsync(cancellationToken);
    }

    public Task<List<WorkSchedule>> GetByAccountAndDateAsync(
        int accountId,
        DateTime workDate,
        CancellationToken cancellationToken = default)
    {
        var date = workDate.Date;
        return _context.WorkSchedules
            .AsNoTracking()
            .Include(x => x.ShiftTemplate)
            .Where(x => x.AccountId == accountId && x.WorkDate == date)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountActiveRoleOnShiftAsync(
        DateTime workDate,
        byte shiftTemplateId,
        byte roleId,
        int? excludeScheduleId,
        CancellationToken cancellationToken = default)
    {
        var date = workDate.Date;

        var q = _context.WorkSchedules.AsNoTracking()
            .Where(ws =>
                ws.WorkDate == date
                && ws.ShiftTemplateId == shiftTemplateId
                && ws.Status != "Absent");

        if (excludeScheduleId.HasValue)
            q = q.Where(ws => ws.ScheduleId != excludeScheduleId.Value);

        return q
            .Join(_context.Accounts.AsNoTracking(), ws => ws.AccountId, a => a.AccountId, (_, a) => a.RoleId)
            .CountAsync(r => r == roleId, cancellationToken);
    }

    public async Task<WorkSchedule> CreateAsync(WorkSchedule schedule, CancellationToken cancellationToken = default)
    {
        schedule.CreatedAt = DateTime.UtcNow;
        await _context.WorkSchedules.AddAsync(schedule, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return schedule;
    }

    public async Task UpdateAsync(WorkSchedule schedule, CancellationToken cancellationToken = default)
    {
        _context.WorkSchedules.Update(schedule);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(WorkSchedule schedule, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var scheduleId = schedule.ScheduleId;

            var assignments = await _context.OrderAssignments
                .Where(a => a.ScheduleId == scheduleId)
                .ToListAsync(cancellationToken);

            if (assignments.Count > 0)
            {
                _context.OrderAssignments.RemoveRange(assignments);
            }

            var capacity = await _context.StaffShiftCapacities
                .FirstOrDefaultAsync(c => c.ScheduleId == scheduleId, cancellationToken);

            if (capacity is not null)
            {
                _context.StaffShiftCapacities.Remove(capacity);
            }

            _context.WorkSchedules.Remove(schedule);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
