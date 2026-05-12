using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

public interface IDeliveryRepository
{
    Task<Delivery?> GetByIdAsync(long deliveryId, CancellationToken ct = default);
    Task<int> CountAsync(int accountId, string channel, string status, CancellationToken ct = default);
    Task<List<Delivery>> GetBellNotificationsAsync(int accountId, string? status, int page, int pageSize, CancellationToken ct = default);
    Task<int> CountBellNotificationsAsync(int accountId, string? status, CancellationToken ct = default);
    Task MarkReadAsync(long deliveryId, int accountId, CancellationToken ct = default);
    Task MarkAllReadAsync(int accountId, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(int accountId, CancellationToken ct = default);
    void Add(Delivery delivery);
    void AddAction(DeliveryAction action);
    Task UpdateAsync(Delivery delivery, CancellationToken ct = default);
    Task<bool> ExistsByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct = default);
    Task IncrementCampaignClickAsync(int campaignId, CancellationToken ct = default);
    Task<bool> HasUserClickedAsync(long deliveryId, int accountId, CancellationToken ct = default);
    Task<List<Delivery>> GetByCampaignPagedAsync(int campaignId, int pageNumber, int pageSize, string? status, CancellationToken ct = default);
    Task<int> CountByCampaignAsync(int campaignId, string? status, CancellationToken ct = default);
    Task MarkDeletedAsync(long deliveryId, int accountId, CancellationToken ct = default);
    Task MarkAllReadAsDeletedAsync(int accountId, CancellationToken ct = default);
}
