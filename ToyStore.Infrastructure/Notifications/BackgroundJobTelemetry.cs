using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ToyStore.Infrastructure.Data;

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
            var job = await db.BackgroundJobs
                .FirstOrDefaultAsync(j => j.JobName == jobName, ct);

            if (job is null)
            {
                job = new Models.BackgroundJob { JobName = jobName };
                db.BackgroundJobs.Add(job);
            }

            job.LastRunTime   = DateTime.UtcNow;
            job.LastRunStatus  = success ? "Success" : "Failed";
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
