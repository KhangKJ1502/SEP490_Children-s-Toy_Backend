using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;

namespace ToyStore.Infrastructure.Notifications;

public class NotificationReadService : INotificationReadService
{
    private readonly IUnitOfWork _unitOfWork;

    public NotificationReadService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task MarkReadAsync(long deliveryId, int accountId, CancellationToken ct = default)
    {
        await _unitOfWork.Deliveries.MarkReadAsync(deliveryId, accountId, ct);
    }

    public async Task<int> GetUnreadCountAsync(int accountId, CancellationToken ct = default)
    {
        return await _unitOfWork.Deliveries.GetUnreadCountAsync(accountId, ct);
    }
}
