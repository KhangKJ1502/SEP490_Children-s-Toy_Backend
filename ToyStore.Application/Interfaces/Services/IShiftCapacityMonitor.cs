namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Fires administration notification when shift capacity reaches its maximum load (once per schedule).
/// </summary>
public interface IShiftCapacityMonitor
{
    Task TryNotifyShiftFullAsync(int scheduleId, CancellationToken cancellationToken = default);
}
