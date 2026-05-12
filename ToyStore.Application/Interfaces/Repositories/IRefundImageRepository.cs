using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

public interface IRefundImageRepository
{
    Task AddRangeAsync(IEnumerable<RefundImage> images, CancellationToken cancellationToken = default);
}
