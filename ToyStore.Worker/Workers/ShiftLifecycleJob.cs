using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Constants;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Worker.Workers;

public class ShiftLifecycleJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ShiftLifecycleJob> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(2);

    public ShiftLifecycleJob(
        IServiceProvider services,
        ILogger<ShiftLifecycleJob> logger,
        ITimeProvider timeProvider)
    {
        _services = services;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ShiftLifecycleJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ShiftLifecycleJob error");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ProcessAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SEP490ToyStoreContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IDomainEventPublisher>();

        var utcNow = _timeProvider.UtcNow;
        var nowVn = utcNow.AddHours(7); // Khớp múi giờ VN
        var today = nowVn.Date;
        var nowTime = nowVn.TimeOfDay;

        var toStart = await db.WorkSchedules
            .Include(ws => ws.ShiftTemplate)
            .Where(ws => ws.Status == "Scheduled"
                         && (ws.WorkDate < today || (ws.WorkDate == today && ws.ShiftTemplate.StartTime <= nowTime)))
            .ToListAsync(ct);

        if (toStart.Count > 0)
        {
            foreach (var schedule in toStart)
            {
                schedule.Status = "OnDuty";
                schedule.UpdatedAt = utcNow;
            }

            await db.SaveChangesAsync(ct);

            foreach (var schedule in toStart)
            {
                await publisher.PublishAsync(
                    "Shift",
                    schedule.ScheduleId.ToString(),
                    ShiftEventTypes.ShiftStarted,
                    new
                    {
                        scheduleId = schedule.ScheduleId,
                        accountId = schedule.AccountId,
                        shiftName = schedule.ShiftTemplate.ShiftName
                    },
                    CancellationToken.None);
            }
        }

        var toClose = await db.WorkSchedules
            .Include(ws => ws.ShiftTemplate)
            .Include(ws => ws.StaffShiftCapacity)
            .Where(ws => (ws.Status == "OnDuty" || ws.Status == "Scheduled")
                         && (ws.WorkDate < today || (ws.WorkDate == today && ws.ShiftTemplate.EndTime < nowTime)))
            .ToListAsync(ct);

        if (toClose.Count == 0)
        {
            return;
        }

        foreach (var schedule in toClose)
        {
            // Bug fix: Scheduled shifts that never transitioned to OnDuty 
            // should be marked Absent (employee never showed up), not Completed.
            // Only OnDuty shifts that ran their full duration are truly Completed.
            schedule.Status = schedule.Status == "OnDuty" ? "Completed" : "Absent";
            schedule.UpdatedAt = utcNow;
        }

        await db.SaveChangesAsync(ct);

        foreach (var schedule in toClose)
        {
            var currentLoad = schedule.StaffShiftCapacity?.CurrentLoad ?? 0;
            if (currentLoad <= 0)
            {
                continue;
            }

            await publisher.PublishAsync(
                "Shift",
                schedule.ScheduleId.ToString(),
                ShiftEventTypes.ShiftEndedWithPendingOrders,
                new
                {
                    scheduleId = schedule.ScheduleId,
                    accountId = schedule.AccountId,
                    shiftName = schedule.ShiftTemplate.ShiftName,
                    currentLoad
                },
                CancellationToken.None);
        }
    }
}
