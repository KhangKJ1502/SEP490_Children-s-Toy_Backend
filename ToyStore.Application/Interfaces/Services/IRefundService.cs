using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Refunds;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Services;

public interface IRefundService
{
    // Customer
    Task<List<RefundReasonDto>> GetRefundReasonsAsync(CancellationToken cancellationToken = default);
    Task<Result<RefundDto>> CreateRefundAsync(int customerId, CreateRefundDto dto, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<RefundListDto>> GetRefundsAsync(int customerId, RefundFilterDto filter, CancellationToken cancellationToken = default);
    Task<Result<RefundDto>> GetRefundByIdAsync(int customerId, int refundId, CancellationToken cancellationToken = default);
    Task<Result<RefundDto>> CancelRefundAsync(int customerId, int refundId, CancellationToken cancellationToken = default);
    Task<Result<RefundDto>> PayReturnFeeAsync(int customerId, int refundId, CancellationToken cancellationToken = default);

    // Admin
    Task<PaginatedResponse<RefundListDto>> GetAdminRefundsAsync(AdminRefundFilterDto filter, CancellationToken cancellationToken = default);
    Task<Result<RefundDto>> AdminGetRefundByIdAsync(int refundId, int currentUserId, byte currentUserRoleId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<Result<RefundDto>> UpdateRefundStatusAsync(int staffId, byte roleId, int refundId, UpdateRefundStatusDto dto, bool isAdmin = false, CancellationToken cancellationToken = default);
    Task<Result<RefundDto>> CreateAdminRefundAsync(int staffId, CreateAdminRefundDto dto, CancellationToken cancellationToken = default);
    Task<Result> ReassignRefundAsync(int refundId, ToyStore.Application.DTOs.Assignments.ReassignOrderRequestDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// System-initiated refund when GHN returns order to warehouse (bypasses customer validations).
    /// </summary>
    Task<OrderRefund?> CreateSystemRefundForDeliveryFailAsync(
        Order order, byte refundReasonId, byte? initialStatusId = null, CancellationToken cancellationToken = default);
}
