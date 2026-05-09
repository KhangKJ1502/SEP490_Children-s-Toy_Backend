using ToyStore.Application.DTOs.CustomerChildren;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Services;

public interface ICustomerChildService
{
    /// <summary>
    /// Lấy danh sách bé của customer đang đăng nhập.
    /// </summary>
    Task<Result<List<CustomerChildDto>>> GetMyChildrenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Thêm bé mới cho customer đang đăng nhập.
    /// </summary>
    Task<Result<CustomerChildDto>> CreateChildAsync(CreateChildDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật thông tin bé của customer đang đăng nhập.
    /// </summary>
    Task<Result<CustomerChildDto>> UpdateChildAsync(int childId, UpdateChildDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa mềm bé của customer đang đăng nhập.
    /// </summary>
    Task<Result> DeleteChildAsync(int childId, CancellationToken cancellationToken = default);
}
