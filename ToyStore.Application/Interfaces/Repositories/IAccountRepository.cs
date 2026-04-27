using ToyStore.Application.Common.Models;

namespace ToyStore.Application.Interfaces.Repositories;

/// <summary>
/// Repository thao tác dữ liệu Account.
/// </summary>
public interface IAccountRepository
{
    /// <summary>
    /// Lấy danh sách Account có phân trang và lọc.
    /// </summary>
    Task<List<AccountModel>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Đếm tổng số Account theo điều kiện lọc.
    /// </summary>
    Task<int> CountAsync(
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tìm Account theo ID.
    /// </summary>
    Task<AccountModel?> GetByIdAsync(int accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra email đã tồn tại chưa.
    /// </summary>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra mã nhân viên đã tồn tại chưa.
    /// </summary>
    Task<bool> ExistsByEmployeeCodeAsync(string employeeCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thông tin role theo ID.
    /// </summary>
    Task<AccountRoleModel?> GetRoleByIdAsync(byte roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tạo mới Account.
    /// </summary>
    Task<AccountModel> CreateAsync(
        byte roleId,
        string? employeeCode,
        string accountName,
        string? phoneNumber,
        string email,
        string passwordHash,
        bool isActive,
        string? provider,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật trạng thái Account.
    /// </summary>
    Task<AccountModel> UpdateStatusAsync(
        int accountId,
        bool isActive,
        CancellationToken cancellationToken = default);
}
