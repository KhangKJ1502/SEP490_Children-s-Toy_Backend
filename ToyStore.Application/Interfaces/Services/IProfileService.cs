using ToyStore.Application.DTOs.Profiles;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Services;

public interface IProfileService
{
    /// <summary>
    /// Lay thong tin profile cua tai khoan dang dang nhap.
    /// </summary>
    Task<Result<ProfileDto>> GetMyProfileAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cap nhat profile cua tai khoan dang dang nhap.
    /// </summary>
    Task<Result<ProfileDto>> UpdateMyProfileAsync(
        UpdateProfileDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay thong tin profile cua customer dang dang nhap.
    /// </summary>
    Task<Result<CustomerProfileDto>> GetMyCustomerProfileAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Doi mat khau cho customer dang dang nhap.
    /// </summary>
    Task<Result> ChangeMyCustomerPasswordAsync(
        ChangeCustomerPasswordDto dto,
        CancellationToken cancellationToken = default);

}
