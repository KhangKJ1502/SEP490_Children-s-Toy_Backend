using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Wallets;

namespace ToyStore.Application.Interfaces.Services;

public interface IAdminWalletService
{
    Task<Result<PaginatedResponse<AdminWalletListDto>>> GetWalletsAsync(
        AdminWalletQueryDto query,
        CancellationToken cancellationToken = default);

    Task<Result<AdminWalletListDto>> UpdateWalletStatusAsync(
        int walletId,
        UpdateWalletStatusDto dto,
        CancellationToken cancellationToken = default);
}
