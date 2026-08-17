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
        if (cutoff.HasValue)
        {
            list = list.Where(h => h.CreatedAt <= cutoff.Value).ToList();
        }

        var completedEntry = list
            .Where(h => h.StatusName != null &&
                        (h.StatusName.Equals(OrderStatuses.Completed, StringComparison.OrdinalIgnoreCase) ||
                         h.StatusName.Equals(OrderStatuses.Delivered, StringComparison.OrdinalIgnoreCase) ||
                         h.StatusName.StartsWith("Completed", StringComparison.OrdinalIgnoreCase)))
            .OrderBy(h => h.CreatedAt)
            .FirstOrDefault();

        if (completedEntry != null)
        {
            list = list.Where(h => h.CreatedAt <= completedEntry.CreatedAt).ToList();
        }

        if (string.Equals(internalStatusName, OrderStatuses.Completed, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(internalStatusName, OrderStatuses.Delivered, StringComparison.OrdinalIgnoreCase))
        {
            list = list.Where(h =>
                !string.Equals(h.StatusName, OrderStatuses.Returning, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(h.StatusName, OrderStatuses.WaitingReturn, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(h.StatusName, OrderStatuses.ReturnCompleted, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(h.StatusName, OrderStatuses.ReturnFailed, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(h.StatusName, OrderStatuses.DeliveryFailed, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(h.StatusName, CustomerOrderDisplayStatusMapper.ReturningLabel, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(h.StatusName, CustomerOrderDisplayStatusMapper.ReturnedToWarehouseLabel, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        return list
            .OrderByDescending(h => h.CreatedAt)
            .ToList();
    }

    public static List<OrderTrackingEventDto> FilterShippingEvents(
        IEnumerable<OrderTrackingEventDto> events,
        DateTime? cancelledAt,
        string internalStatusName)
    {
        var list = events.ToList();

        if (IsCancelledOrder(internalStatusName, cancelledAt) && cancelledAt.HasValue)
        {
            return list
                .Where(e => e.Time <= cancelledAt.Value)
                .OrderByDescending(e => e.Time)
                .ToList();
        }

        if (string.Equals(internalStatusName, OrderStatuses.Completed, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(internalStatusName, OrderStatuses.Delivered, StringComparison.OrdinalIgnoreCase))
        {
            list = list.Where(e =>
                !string.Equals(e.Status, OrderStatuses.Returning, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(e.Status, OrderStatuses.WaitingReturn, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(e.Status, OrderStatuses.ReturnCompleted, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(e.Status, OrderStatuses.ReturnFailed, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(e.Status, OrderStatuses.DeliveryFailed, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(e.Status, "returned", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(e.Status, "returning", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(e.Status, "return_transporting", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(e.Status, "return_sorting", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(e.Status, "waiting_to_return", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(e.Status, "delivery_fail", StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        return list
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
