using System.Threading;
using System.Threading.Tasks;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Refunds;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Services;

public interface IRefundService
{
    // Customer
    Task<Result<RefundDto>> CreateRefundAsync(int customerId, CreateRefundDto dto, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<RefundListDto>> GetRefundsAsync(int customerId, RefundFilterDto filter, CancellationToken cancellationToken = default);
    Task<Result<RefundDto>> GetRefundByIdAsync(int customerId, int refundId, CancellationToken cancellationToken = default);
    Task<Result<RefundDto>> CancelRefundAsync(int customerId, int refundId, CancellationToken cancellationToken = default);

    // Admin
    Task<PaginatedResponse<RefundListDto>> GetAdminRefundsAsync(AdminRefundFilterDto filter, CancellationToken cancellationToken = default);
    Task<Result<RefundDto>> AdminGetRefundByIdAsync(int refundId, CancellationToken cancellationToken = default);
    Task<Result<RefundDto>> UpdateRefundStatusAsync(int staffId, int refundId, UpdateRefundStatusDto dto, CancellationToken cancellationToken = default);
}
