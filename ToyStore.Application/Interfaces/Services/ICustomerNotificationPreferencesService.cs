using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs.Profiles;

namespace ToyStore.Application.Interfaces.Services;

public interface ICustomerNotificationPreferencesService
{
    Task<Result<CustomerNotificationPreferencesDto>> GetMyAsync(CancellationToken cancellationToken = default);

    Task<Result<CustomerNotificationPreferencesDto>> UpdateMyAsync(
        UpdateCustomerNotificationPreferencesDto dto,
        CancellationToken cancellationToken = default);
}
