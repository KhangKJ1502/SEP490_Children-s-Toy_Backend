using ToyStore.Application.DTOs.Orders;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Constants;

namespace ToyStore.Application.Services;

/// <summary>
/// Hides order timeline entries after cancellation for customer-facing APIs.
/// </summary>
public static class CustomerOrderTimelineFilter
{
    public static bool IsCancelledOrder(string? internalStatusName, DateTime? cancelledAt)
        => cancelledAt.HasValue
           || string.Equals(internalStatusName, OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase);

    public static DateTime? ResolveCancellationCutoff(
        DateTime? cancelledAt,
        IEnumerable<CustomerOrderStatusHistoryDto> history,
        string? internalStatusName)
    {
        if (!IsCancelledOrder(internalStatusName, cancelledAt))
            return null;

        var cutoff = cancelledAt;

        foreach (var entry in history)
        {
            if (!IsCancellationHistoryEntry(entry))
                continue;

            if (!cutoff.HasValue || entry.CreatedAt < cutoff.Value)
                cutoff = entry.CreatedAt;
        }

        return cutoff;
    }

    public static List<CustomerOrderStatusHistoryDto> FilterStatusHistory(
        IEnumerable<CustomerOrderStatusHistoryDto> history,
        DateTime? cancelledAt,
        string internalStatusName)
    {
        var list = history.ToList();
        var cutoff = ResolveCancellationCutoff(cancelledAt, list, internalStatusName);
        if (!cutoff.HasValue)
            return list;

        return list
            .Where(h => h.CreatedAt <= cutoff.Value)
            .OrderByDescending(h => h.CreatedAt)
            .ToList();
    }

    public static List<OrderTrackingEventDto> FilterShippingEvents(
        IEnumerable<OrderTrackingEventDto> events,
        DateTime? cancelledAt,
        string internalStatusName)
    {
        var list = events.ToList();
        if (!IsCancelledOrder(internalStatusName, cancelledAt))
            return list;

        if (!cancelledAt.HasValue)
            return list;

        return list
            .Where(e => e.Time <= cancelledAt.Value)
            .OrderByDescending(e => e.Time)
            .ToList();
    }

    private static bool IsCancellationHistoryEntry(CustomerOrderStatusHistoryDto entry)
    {
        if (string.Equals(entry.StatusName, CustomerOrderDisplayStatusMapper.CancelledLabel,
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(entry.StatusName, OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var note = entry.Note?.ToLowerInvariant() ?? string.Empty;
        return note.Contains("order cancelled", StringComparison.Ordinal)
               || note.Contains("cancelled", StringComparison.Ordinal)
               || note.Contains("đã hủy", StringComparison.Ordinal)
               || note.Contains("đã huỷ", StringComparison.Ordinal);
    }
}
