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
                && x.ShiftTemplateId == shiftTemplateId, cancellationToken);
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
        _context.WorkSchedules.Remove(schedule);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
