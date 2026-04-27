using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Accounts;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Service quản lý Account.
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// Lấy danh sách Account có phân trang và lọc.
    /// </summary>
    Task<Result<PaginatedResponse<AccountListDto>>> GetAccountsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy chi tiết Account theo ID.
    /// </summary>
    Task<Result<AccountDto>> GetAccountByIdAsync(
        int accountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tạo mới Account cho Staff hoặc Merchandiser.
    /// </summary>
    Task<Result<AccountDto>> CreateAccountAsync(
        CreateAccountDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật trạng thái Account.
    /// </summary>
    Task<Result<AccountDto>> UpdateAccountStatusAsync(
        int accountId,
        UpdateAccountStatusDto dto,
        CancellationToken cancellationToken = default);
}
