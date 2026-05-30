using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Constants;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Services;

public class ShiftCapacityMonitor : IShiftCapacityMonitor
{
    private readonly SEP490ToyStoreContext _context;
    private readonly IDomainEventPublisher _eventPublisher;

    public ShiftCapacityMonitor(SEP490ToyStoreContext context, IDomainEventPublisher eventPublisher)
    {
        _context = context;
        _eventPublisher = eventPublisher;
    }

    public async Task TryNotifyShiftFullAsync(int scheduleId, CancellationToken cancellationToken = default)
    {
        var capacity = await _context.StaffShiftCapacities
            .Include(c => c.WorkSchedule)
                .ThenInclude(ws => ws.ShiftTemplate)
            .FirstOrDefaultAsync(c => c.ScheduleId == scheduleId, cancellationToken);

        if (capacity is null || capacity.CurrentLoad < capacity.MaxLoad || capacity.ShiftFullNotifiedAt.HasValue)
        {
            return;
        }

        var utcNow = DateTime.UtcNow;
        capacity.ShiftFullNotifiedAt = utcNow;
        capacity.UpdatedAt = utcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var ws = capacity.WorkSchedule;

        await _eventPublisher.PublishAsync(
            "WorkSchedule",
            scheduleId.ToString(),
            ShiftEventTypes.ShiftFull,
            new
            {
                type = "SHIFT_FULL",
                scheduleId,
                shiftName = ws.ShiftTemplate?.ShiftName ?? "Shift",
                workDate = ws.WorkDate.Date,
                triggeredAt = utcNow
            },
            cancellationToken);
    }
}
