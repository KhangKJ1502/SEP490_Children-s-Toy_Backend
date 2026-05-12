using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Refunds;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;
using ToyStore.Application.Common.Extensions;

namespace ToyStore.Infrastructure.Repositories;

public class RefundRepository : IRefundRepository
{
    private readonly SEP490ToyStoreContext _context;
    private readonly DbSet<OrderRefund> _dbSet;

    public RefundRepository(SEP490ToyStoreContext context)
    {
        _context = context;
        _dbSet = context.Set<OrderRefund>();
    }

    public async Task<PaginatedResponse<RefundListDto>> GetRefundsAsync(RefundFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(r => r.Order).ThenInclude(o => o.Status)
            .Include(r => r.RefundReason)
            .Include(r => r.Customer)
            .Include(r => r.RequestedByNavigation)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(filter.RefundStatus))
            query = query.Where(r => r.RefundStatus == filter.RefundStatus);

        if (filter.OrderId.HasValue)
            query = query.Where(r => r.OrderId == filter.OrderId.Value);

        if (filter.FromDate.HasValue)
            query = query.Where(r => r.CreatedAt >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(r => r.CreatedAt <= filter.ToDate.Value);

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(r => new RefundListDto
            {
                RefundId = r.RefundId,
                OrderId = r.OrderId,
                OrderCode = r.Order.OrderCode,
                OrderStatus = r.Order.Status.StatusName,
                PaymentStatus = r.Order.PaymentStatus,
                CustomerName = r.Customer.AccountName,
                CustomerPhone = r.Customer.PhoneNumber,
                CustomerEmail = r.Customer.Email,
                RequestedByName = r.RequestedByNavigation != null ? r.RequestedByNavigation.AccountName : null,
                RefundReasonContent = r.RefundReason != null ? r.RefundReason.Content : null,
                ApprovedAmount = r.ApprovedAmount,
                RefundStatus = r.RefundStatus,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<RefundListDto>(items, totalItems, filter.Page, filter.PageSize);
    }

    public async Task<PaginatedResponse<RefundListDto>> GetAdminRefundsAsync(AdminRefundFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(r => r.Order).ThenInclude(o => o.Status)
            .Include(r => r.RefundReason)
            .Include(r => r.Customer)
            .Include(r => r.RequestedByNavigation)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(filter.RefundStatus))
            query = query.Where(r => r.RefundStatus == filter.RefundStatus);

        if (filter.OrderId.HasValue)
            query = query.Where(r => r.OrderId == filter.OrderId.Value);

        if (filter.CustomerId.HasValue)
            query = query.Where(r => r.CustomerId == filter.CustomerId.Value);

        if (filter.RefundReasonId.HasValue)
            query = query.Where(r => r.RefundReasonId == filter.RefundReasonId.Value);

        if (filter.FromDate.HasValue)
            query = query.Where(r => r.CreatedAt >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(r => r.CreatedAt <= filter.ToDate.Value);

        var totalItems = await query.CountAsync(cancellationToken);

        // Sorting
        bool isDesc = string.IsNullOrEmpty(filter.SortDir) || filter.SortDir.ToLower() == "desc";
        
        if (!string.IsNullOrEmpty(filter.SortBy) && filter.SortBy.ToLower() == "approvedamount")
        {
            query = isDesc ? query.OrderByDescending(r => r.ApprovedAmount) : query.OrderBy(r => r.ApprovedAmount);
        }
        else
        {
            query = isDesc ? query.OrderByDescending(r => r.CreatedAt) : query.OrderBy(r => r.CreatedAt);
        }

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(r => new RefundListDto
            {
                RefundId = r.RefundId,
                OrderId = r.OrderId,
                OrderCode = r.Order.OrderCode,
                OrderStatus = r.Order.Status.StatusName,
                PaymentStatus = r.Order.PaymentStatus,
                CustomerName = r.Customer.AccountName,
                CustomerPhone = r.Customer.PhoneNumber,
                CustomerEmail = r.Customer.Email,
                RequestedByName = r.RequestedByNavigation != null ? r.RequestedByNavigation.AccountName : null,
                RefundReasonContent = r.RefundReason != null ? r.RefundReason.Content : null,
                ApprovedAmount = r.ApprovedAmount,
                RefundStatus = r.RefundStatus,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<RefundListDto>(items, totalItems, filter.Page, filter.PageSize);
    }

    public async Task<OrderRefund?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.Order).ThenInclude(o => o.Status)
            .Include(r => r.RefundReason)
            .Include(r => r.Customer)
            .Include(r => r.RequestedByNavigation)
            .Include(r => r.RefundImages.Where(i => !i.IsDeleted))
            .FirstOrDefaultAsync(r => r.RefundId == id, cancellationToken);
    }

    public async Task<OrderRefund?> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.Order).ThenInclude(o => o.Status)
            .Include(r => r.RefundReason)
            .Include(r => r.Customer)
            .Include(r => r.RequestedByNavigation)
            .Include(r => r.RefundImages.Where(i => !i.IsDeleted))
            .FirstOrDefaultAsync(r => r.OrderId == orderId, cancellationToken);
    }

    public async Task<OrderRefund> AddAsync(OrderRefund refund, CancellationToken cancellationToken = default)
    {
        var result = await _dbSet.AddAsync(refund, cancellationToken);
        return result.Entity;
    }

    public void Update(OrderRefund refund)
    {
        _dbSet.Update(refund);
    }
}
