using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Customers;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Service quản lý customer cho admin.
/// </summary>
public interface ICustomerService
{
    /// <summary>
    /// Lấy danh sách customer có phân trang và tìm kiếm.
    /// </summary>
    Task<Result<PaginatedResponse<CustomerListDto>>> GetCustomersAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy chi tiết customer theo account ID.
    /// </summary>
    Task<Result<CustomerDetailDto>> GetCustomerByIdAsync(
        int accountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật thông tin customer.
    /// </summary>
    Task<Result<CustomerDetailDto>> UpdateCustomerAsync(
        int accountId,
        UpdateCustomerDto dto,
        CancellationToken cancellationToken = default);
}
