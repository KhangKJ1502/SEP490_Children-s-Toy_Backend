using ToyStore.Domain.Entities;
using ToyStore.Application.DTOs.Customers;

namespace ToyStore.Application.Interfaces.Repositories;

/// <summary>
/// Repository thao tác dữ liệu Account.
/// </summary>
public interface IAccountRepository
{
    /// <summary>
    /// Lấy danh sách Account có phân trang và lọc.
    /// </summary>
    Task<List<Account>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        byte? roleId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Đếm tổng số Account theo điều kiện lọc.
    /// </summary>
    Task<int> CountAsync(
        string? searchTerm = null,
        byte? roleId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tìm Account theo ID.
    /// </summary>
    Task<Account?> GetByIdAsync(int accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra email đã tồn tại chưa.
    /// </summary>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra mã nhân viên đã tồn tại chưa.
    /// </summary>
    Task<bool> ExistsByEmployeeCodeAsync(string employeeCode, CancellationToken cancellationToken = default);

    Task<bool> ExistsByPhoneNumberAsync(
        string phoneNumber,
        int excludeAccountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thông tin role theo ID.
    /// </summary>
    Task<Role?> GetRoleByIdAsync(byte roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tạo mới Account.
    /// </summary>
    Task<Account> CreateAsync(
        byte roleId,
        string? employeeCode,
        string accountName,
        string? phoneNumber,
        string email,
        string passwordHash,
        bool isActive,
        string? provider,
        CancellationToken cancellationToken = default,
        bool hasPassword = true);

    /// <summary>
    /// Cập nhật trạng thái Account.
    /// </summary>
    Task<Account> UpdateStatusAsync(
        int accountId,
        bool isActive,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thông tin Account theo email để xác thực (bao gồm PasswordHash).
    /// </summary>
    Task<Account?> GetByEmailForAuthAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật mật khẩu Account.
    /// </summary>
    Task UpdatePasswordHashAsync(int accountId, string passwordHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cap nhat provider cua account.
    /// </summary>
    Task UpdateProviderAsync(int accountId, string? provider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay thong tin Account theo ID cho nghiep vu Profile (bao gom PasswordHash).
    /// </summary>
    Task<Account?> GetByIdForProfileAsync(int accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cap nhat thong tin Profile (ImageUrl, PhoneNumber, DOB, Sex).
    /// </summary>
    Task<Account> UpdateProfileAsync(
        int accountId,
        string? accountName,
        string? imageUrl,
        string? phoneNumber,
        DateTime? dob,
        byte? sexId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active non-deleted accounts belonging to any of the specified role IDs.
    /// Used by notification handlers to fan out to staff/admin/merchandise roles.
    /// </summary>
    Task<List<Account>> GetByRoleIdsAsync(byte[] roleIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active, non-deleted customer accounts.
    /// </summary>
    Task<List<Account>> GetActiveCustomersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Count customer accounts (non-deleted, active) in the given id set.
    /// </summary>
    Task<int> CountActiveCustomersByIdsAsync(IReadOnlyCollection<int> accountIds, CancellationToken cancellationToken = default);

    Task<List<CustomerDeliveryAbuseSummaryDto>> GetDeliveryAbuseSummariesAsync(
        IReadOnlyCollection<int> accountIds,
        CancellationToken cancellationToken = default);
}
