using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Refunds;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

public interface IRefundRepository
{
    Task<List<OrderRefundReason>> GetActiveReasonsAsync(CancellationToken cancellationToken = default);
    Task<OrderRefundReason?> GetReasonByContentAsync(string content, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<RefundListDto>> GetRefundsAsync(int customerId, RefundFilterDto filter, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<RefundListDto>> GetAdminRefundsAsync(AdminRefundFilterDto filter, CancellationToken cancellationToken = default);
    Task<OrderRefund?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<OrderRefund?> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default);
    Task<OrderRefund?> GetByShippingOrderCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<OrderRefund?> GetByShippingOrReturnOrderCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<OrderRefund> AddAsync(OrderRefund refund, CancellationToken cancellationToken = default);
    void Update(OrderRefund refund);
}
