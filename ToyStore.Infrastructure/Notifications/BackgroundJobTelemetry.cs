using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ToyStore.Infrastructure.Data;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Notifications;

public static class BackgroundJobTelemetry
{
    public static async Task RecordAsync(
        SEP490ToyStoreContext db,
        string jobName,
        bool success,
        string? message,
        ILogger logger,
        CancellationToken ct)
    {
        try
        {
            // Xóa state cũ để tránh trường hợp các Entity bị lỗi trước đó nổ exception lại 
            db.ChangeTracker.Clear();

            var job = await db.BackgroundJobs
                .FirstOrDefaultAsync(j => j.JobName == jobName, ct);

            if (job is null)
            {
                job = new BackgroundJob { JobName = jobName };
                db.BackgroundJobs.Add(job);
            }

            job.LastRunTime = DateTime.UtcNow;
            job.LastRunStatus = success ? "Success" : "Failed";
            job.LastRunMessage = message is not null && message.Length > 500
                ? message[..500]
                : message;

            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to record telemetry for job {JobName}", jobName);
        }
    }
}
