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

        var now = _timeProvider.UtcNow;
        var today = now.Date;
        var nowTime = now.TimeOfDay;

        var toStart = await db.WorkSchedules
            .Include(ws => ws.ShiftTemplate)
            .Where(ws => ws.WorkDate == today
                         && ws.Status == "Scheduled"
                         && ws.ShiftTemplate.StartTime <= nowTime)
            .ToListAsync(ct);

        if (toStart.Count > 0)
        {
            foreach (var schedule in toStart)
            {
                schedule.Status = "OnDuty";
                schedule.UpdatedAt = now;
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
            .Where(ws => ws.WorkDate == today
                         && ws.Status == "OnDuty"
                         && ws.ShiftTemplate.EndTime < nowTime)
            .ToListAsync(ct);

        if (toClose.Count == 0)
        {
            return;
        }

        foreach (var schedule in toClose)
        {
            schedule.Status = "Completed";
            schedule.UpdatedAt = now;
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
