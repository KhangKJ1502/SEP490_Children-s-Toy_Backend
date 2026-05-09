namespace ToyStore.Application.Interfaces.Notifications;

public interface INotificationHandler<TEvent>
{
    Task HandleAsync(TEvent @event, CancellationToken ct = default);
}
