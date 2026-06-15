using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Withdrawals;

namespace ToyStore.Application.Interfaces.Services;

public interface IWithdrawalService
{
    Task<Result<WithdrawalDto>> CreateWithdrawalAsync(
        CreateWithdrawalRequestDto dto,
        CancellationToken ct = default);

    Task<Result<WithdrawalDto>> GetWithdrawalAsync(
        int withdrawalId,
        CancellationToken ct = default);

    Task<Result<PaginatedResponse<WithdrawalDto>>> GetMyWithdrawalsAsync(
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<Result> CancelWithdrawalAsync(
        int withdrawalId,
        CancellationToken ct = default);

    Task<Result<PaginatedResponse<AdminWithdrawalListDto>>> GetAdminWithdrawalsAsync(
        AdminWithdrawalFilterDto filter,
        CancellationToken ct = default);

    Task<Result<AdminWithdrawalDetailDto>> AdminGetWithdrawalByIdAsync(
        int id,
        CancellationToken ct = default);
}
