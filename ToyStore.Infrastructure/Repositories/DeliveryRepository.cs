using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Constants;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class DeliveryRepository : IDeliveryRepository
{
    private readonly SEP490ToyStoreContext _db;

    public DeliveryRepository(SEP490ToyStoreContext db)
    {
        _db = db;
    }

    public async Task<Delivery?> GetByIdAsync(long deliveryId, CancellationToken ct = default)
    {
        return await _db.Deliveries.FirstOrDefaultAsync(d => d.DeliveryId == deliveryId, ct);
    }

    public async Task<int> CountAsync(int accountId, string channel, string status, CancellationToken ct = default)
    {
        return await _db.Deliveries
            .CountAsync(d => d.AccountId == accountId 
                          && d.Channel == channel 
                          && d.Status == status, ct);
    }

    public async Task<List<Delivery>> GetBellNotificationsAsync(int accountId, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Deliveries
            .Where(d => d.AccountId == accountId && d.Channel == NotificationChannels.WebBell);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(d => d.Status == status);

        return await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<int> CountBellNotificationsAsync(int accountId, string? status, CancellationToken ct = default)
    {
        var query = _db.Deliveries
            .Where(d => d.AccountId == accountId && d.Channel == NotificationChannels.WebBell);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(d => d.Status == status);

        return await query.CountAsync(ct);
    }

    public async Task MarkReadAsync(long deliveryId, int accountId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        await _db.Deliveries
            .Where(d => d.DeliveryId == deliveryId && d.AccountId == accountId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.Status, NotificationStatuses.Read)
                .SetProperty(d => d.ReadAt, now)
                .SetProperty(d => d.UpdatedAt, now), ct);
    }

    public async Task<int> GetUnreadCountAsync(int accountId, CancellationToken ct = default)
    {
        return await _db.Deliveries
            .CountAsync(d => d.AccountId == accountId
                          && d.Channel   == NotificationChannels.WebBell
                          && d.Status    == NotificationStatuses.Unread, ct);
    }

    public async Task MarkAllReadAsync(int accountId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        await _db.Deliveries
            .Where(d => d.AccountId == accountId 
                     && d.Channel == NotificationChannels.WebBell 
                     && d.Status == NotificationStatuses.Unread)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.Status, NotificationStatuses.Read)
                .SetProperty(d => d.ReadAt, now)
                .SetProperty(d => d.UpdatedAt, now), ct);
    }

    public void Add(Delivery delivery)
    {
        _db.Deliveries.Add(delivery);
    }

    public void AddAction(DeliveryAction action)
    {
        _db.DeliveryActions.Add(action);
    }

    public async Task UpdateAsync(Delivery delivery, CancellationToken ct = default)
    {
        _db.Deliveries.Update(delivery);
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct = default)
    {
        return await _db.Deliveries.AnyAsync(d => d.IdempotencyKey == idempotencyKey, ct);
    }
}
