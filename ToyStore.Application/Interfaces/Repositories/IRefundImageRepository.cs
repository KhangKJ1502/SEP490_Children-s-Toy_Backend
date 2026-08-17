using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

/// <summary>
/// Interface định nghĩa các phương thức thao tác dữ liệu cho hình ảnh bằng chứng hoàn tiền (RefundImage).
/// </summary>
public interface IRefundImageRepository
{
    /// <summary>
    /// Thêm danh sách nhiều hình ảnh bằng chứng vào DbContext theo lô (batch).
    /// </summary>
    /// <param name="images">Tập hợp các thực thể RefundImage.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    Task AddRangeAsync(IEnumerable<RefundImage> images, CancellationToken cancellationToken = default);
}
