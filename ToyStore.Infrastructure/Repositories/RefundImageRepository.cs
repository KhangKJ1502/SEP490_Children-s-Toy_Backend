using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

/// <summary>
/// Repository triển khai các thao tác dữ liệu liên quan đến hình ảnh bằng chứng hoàn tiền (RefundImage).
/// </summary>
public class RefundImageRepository : IRefundImageRepository
{
    private readonly SEP490ToyStoreContext _context;

    /// <summary>
    /// Khởi tạo RefundImageRepository với database context SEP490ToyStoreContext.
    /// </summary>
    public RefundImageRepository(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Thêm danh sách nhiều hình ảnh bằng chứng vào DbContext theo lô (batch).
    /// </summary>
    /// <param name="images">Tập hợp các thực thể RefundImage.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    public async Task AddRangeAsync(IEnumerable<RefundImage> images, CancellationToken cancellationToken = default)
    {
        await _context.RefundImages.AddRangeAsync(images, cancellationToken);
    }
}
