using ToyStore.Domain.Entities;

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
        CancellationToken cancellationToken = default);

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
    /// Lay thong tin Account theo ID cho nghiep vu Profile (bao gom PasswordHash).
    /// </summary>
    Task<Account?> GetByIdForProfileAsync(int accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cap nhat thong tin Profile (ImageUrl, PhoneNumber).
    /// </summary>
    Task<Account> UpdateProfileAsync(
        int accountId,
        string? imageUrl,
        string? phoneNumber,
        CancellationToken cancellationToken = default);

}
