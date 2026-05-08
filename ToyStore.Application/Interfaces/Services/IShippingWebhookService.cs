namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Xu ly callback tu shipper (GHN, GHTK...).
/// </summary>
public interface IShippingWebhookService
{
    /// <summary>
    /// Xu ly payload webhook tu shipper.
    /// Khong throw exception — ghi log loi va tra ve.
    /// </summary>
    Task HandleAsync(
        string provider,
        string rawPayload,
        CancellationToken cancellationToken = default);
}
