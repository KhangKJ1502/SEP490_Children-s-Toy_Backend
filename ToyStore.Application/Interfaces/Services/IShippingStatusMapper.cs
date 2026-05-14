using ToyStore.Domain.Enums;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Service to map shipping provider statuses to internal order statuses.
/// </summary>
public interface IShippingStatusMapper
{
    /// <summary>
    /// Maps a raw provider status to an internal OrderStatus.
    /// </summary>
    /// <param name="providerStatus">The status from GHN/GHTK.</param>
    /// <returns>The corresponding OrderStatus, or null if no mapping exists.</returns>
    OrderStatus? MapToInternalStatus(string? providerStatus);

    /// <summary>
    /// Gets a user-friendly description for a provider status.
    /// </summary>
    string GetStatusDescription(string? providerStatus);

    /// <summary>
    /// Normalizes a provider status for frontend display.
    /// Only returns business-friendly statuses: Pending, Confirmed, Processing, Shipped, Delivering, Delivered, Completed, Cancelled.
    /// </summary>
    string NormalizeStatus(string? providerStatus, OrderStatus currentInternalStatus);
}
