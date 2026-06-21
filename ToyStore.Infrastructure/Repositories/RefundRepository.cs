using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Refunds;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;
using ToyStore.Application.Common.Extensions;

namespace ToyStore.Infrastructure.Repositories;

public class RefundRepository : IRefundRepository
{
    private readonly SEP490ToyStoreContext _context;
    private readonly DbSet<OrderRefund> _dbSet;
    private readonly ITimeProvider _timeProvider;

    public RefundRepository(SEP490ToyStoreContext context, ITimeProvider timeProvider)
    {
        _context = context;
        _dbSet = context.Set<OrderRefund>();
        _timeProvider = timeProvider;
    }

    public async Task<List<OrderRefundReason>> GetActiveReasonsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<OrderRefundReason>()
            .Where(r => !r.IsDeleted && !r.IsSystem) // Chỉ lấy các lý do hoạt động và ẩn lý do Hệ thống (System-only)
            .OrderBy(r => r.RefundReasonId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<OrderRefundReason?> GetReasonByContentAsync(string content, CancellationToken cancellationToken = default)
    {
        var reason = await _context.Set<OrderRefundReason>()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Content == content, cancellationToken);

        if (reason is null && content == "Giao hàng thất bại / không giao được")
        {
            var newReason = new OrderRefundReason
            {
                Content = content,
                Description = "Refund tự động khi GHN trả hàng về kho do giao thất bại",
                IsDeleted = false,
                IsSystem = true,
                CreatedAt = _timeProvider.UtcNow
            };

            await _context.Set<OrderRefundReason>().AddAsync(newReason, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return newReason;
        }

        return reason;
    }

    public async Task<PaginatedResponse<RefundListDto>> GetRefundsAsync(RefundFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(r => r.Status)
            .Include(r => r.Order).ThenInclude(o => o.Status)
            .Include(r => r.RefundReason)
            .Include(r => r.Customer)
            .Include(r => r.RequestedByNavigation)
            .Where(r => !r.IsDeleted)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(filter.RefundStatus))
            query = query.Where(r => r.Status.StatusName == filter.RefundStatus);

        if (filter.OrderId.HasValue)
            query = query.Where(r => r.OrderId == filter.OrderId.Value);

        if (filter.FromDate.HasValue)
        {
            var startUtc = _timeProvider.ToUtc(filter.FromDate.Value.Date);
            query = query.Where(r => r.CreatedAt >= startUtc);
        }

        if (filter.ToDate.HasValue)
        {
            var endUtc = _timeProvider.ToUtc(filter.ToDate.Value.Date.AddDays(1));
            query = query.Where(r => r.CreatedAt < endUtc);
        }

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
                CustomerPhone = r.Order.ShippingPhone,
                CustomerEmail = r.Customer.Email,
                RequestedByName = r.RequestedByNavigation != null ? r.RequestedByNavigation.AccountName : null,
                RefundReasonContent = r.RefundReason != null ? r.RefundReason.Content : null,
                ApprovedAmount = r.ApprovedAmount,
                RefundStatus = r.Status.StatusName,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<RefundListDto>(items, totalItems, filter.Page, filter.PageSize);
    }

    public async Task<PaginatedResponse<RefundListDto>> GetAdminRefundsAsync(AdminRefundFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(r => r.Status)
            .Include(r => r.Order).ThenInclude(o => o.Status)
            .Include(r => r.RefundReason)
            .Include(r => r.Customer)
            .Include(r => r.RequestedByNavigation)
            .Where(r => !r.IsDeleted)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(filter.RefundStatus))
        {
            var statuses = filter.RefundStatus.Split(',').Select(s => s.Trim()).ToList();
            if (statuses.Count > 1)
            {
                query = query.Where(r => statuses.Contains(r.Status.StatusName));
            }
            else
            {
                query = query.Where(r => r.Status.StatusName == filter.RefundStatus);
            }
        }

        if (filter.OrderId.HasValue)
            query = query.Where(r => r.OrderId == filter.OrderId.Value);

        if (filter.CustomerId.HasValue)
            query = query.Where(r => r.CustomerId == filter.CustomerId.Value);

        if (filter.RefundReasonId.HasValue)
            query = query.Where(r => r.RefundReasonId == filter.RefundReasonId.Value);

        if (filter.FromDate.HasValue)
        {
            var startUtc = _timeProvider.ToUtc(filter.FromDate.Value.Date);
            query = query.Where(r => r.CreatedAt >= startUtc);
        }

        if (filter.ToDate.HasValue)
        {
            var endUtc = _timeProvider.ToUtc(filter.ToDate.Value.Date.AddDays(1));
            query = query.Where(r => r.CreatedAt < endUtc);
        }

        if (filter.AssignedToMe && filter.AssignedAccountId.HasValue)
        {
            query = query.Where(r => r.Order.AssignedToStaffId == filter.AssignedAccountId.Value ||
                r.Order.AssignedToMerchId == filter.AssignedAccountId.Value ||
                _context.Set<OrderAssignment>().Any(a => a.OrderId == r.OrderId && a.AccountId == filter.AssignedAccountId.Value && a.IsActive));
        }

        if (!string.IsNullOrEmpty(filter.AssignmentScope))
        {
            var scope = filter.AssignmentScope.Trim().ToLower();
            if (scope == "completed")
            {
                query = query.Where(r => r.Status.StatusName == "RefundCompleted" ||
                                         r.Status.StatusName == "RefundCancelled" ||
                                         r.Status.StatusName == "RefundRejected");
            }
            else if (scope == "inprogress")
            {
                query = query.Where(r => r.Status.StatusName != "RefundCompleted" &&
                                         r.Status.StatusName != "RefundCancelled" &&
                                         r.Status.StatusName != "RefundRejected");
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var kw = filter.Keyword.Trim();
            bool isNumeric = int.TryParse(kw, out int orderIdParsed);
            query = query.Where(r =>
                r.RefundCode.Contains(kw) ||
                r.Order.OrderCode.Contains(kw) ||
                r.Customer.AccountName.Contains(kw) ||
                r.Customer.PhoneNumber.Contains(kw) ||
                r.Customer.Email.Contains(kw) ||
                (isNumeric && r.OrderId == orderIdParsed));
        }

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
                CustomerPhone = r.Order.ShippingPhone,
                CustomerEmail = r.Customer.Email,
                RequestedByName = r.RequestedByNavigation != null ? r.RequestedByNavigation.AccountName : null,
                RefundReasonContent = r.RefundReason != null ? r.RefundReason.Content : null,
                ApprovedAmount = r.ApprovedAmount,
                RefundStatus = r.Status.StatusName,
                CreatedAt = r.CreatedAt,
                AssignedToStaffName = _context.Set<OrderAssignment>()
                    .Where(a => a.OrderId == r.OrderId && a.RoleId == 3 && a.IsActive)
                    .Select(a => a.Account.AccountName)
                    .FirstOrDefault()
                    ?? (r.Order.AssignedToStaff != null ? r.Order.AssignedToStaff.AccountName : null),
                AssignedToMerchName = _context.Set<OrderAssignment>()
                    .Where(a => a.OrderId == r.OrderId && a.RoleId == 4 && a.IsActive)
                    .Select(a => a.Account.AccountName)
                    .FirstOrDefault()
                    ?? (r.Order.AssignedToMerch != null ? r.Order.AssignedToMerch.AccountName : null)
            })
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<RefundListDto>(items, totalItems, filter.Page, filter.PageSize);
    }

    public async Task<OrderRefund?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.Status)
            .Include(r => r.Order).ThenInclude(o => o.Status)
            .Include(r => r.Order).ThenInclude(o => o.AssignedToStaff)
            .Include(r => r.Order).ThenInclude(o => o.AssignedToMerch)
            .Include(r => r.Order).ThenInclude(o => o.ShippingProviderTransactions).ThenInclude(t => t.ShippingStatusHistories)
            .Include(r => r.RefundReason)
            .Include(r => r.Customer).ThenInclude(c => c.Address).ThenInclude(a => a.Province)
            .Include(r => r.Customer).ThenInclude(c => c.Address).ThenInclude(a => a.District)
            .Include(r => r.Customer).ThenInclude(c => c.Address).ThenInclude(a => a.WardCodeNavigation)
            .Include(r => r.RequestedByNavigation)
            .Include(r => r.RefundImages.Where(i => !i.IsDeleted))
            .Include(r => r.RefundDetails).ThenInclude(d => d.Product).ThenInclude(p => p.ProductImage)
            .Include(r => r.RefundStatusHistories).ThenInclude(h => h.Status)
            .Include(r => r.RefundStatusHistories).ThenInclude(h => h.ChangedByNavigation)
            .FirstOrDefaultAsync(r => r.RefundId == id && !r.IsDeleted, cancellationToken);
    }

    public async Task<OrderRefund?> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.Status)
            .Include(r => r.Order).ThenInclude(o => o.Status)
            .Include(r => r.Order).ThenInclude(o => o.ShippingProviderTransactions).ThenInclude(t => t.ShippingStatusHistories)
            .Include(r => r.RefundReason)
            .Include(r => r.Customer).ThenInclude(c => c.Address).ThenInclude(a => a.Province)
            .Include(r => r.Customer).ThenInclude(c => c.Address).ThenInclude(a => a.District)
            .Include(r => r.Customer).ThenInclude(c => c.Address).ThenInclude(a => a.WardCodeNavigation)
            .Include(r => r.RequestedByNavigation)
            .Include(r => r.RefundImages.Where(i => !i.IsDeleted))
            .Include(r => r.RefundDetails).ThenInclude(d => d.Product).ThenInclude(p => p.ProductImage)
            .Include(r => r.RefundStatusHistories).ThenInclude(h => h.Status)
            .Include(r => r.RefundStatusHistories).ThenInclude(h => h.ChangedByNavigation)
            .FirstOrDefaultAsync(r => r.OrderId == orderId && !r.IsDeleted, cancellationToken);
    }

    public async Task<OrderRefund?> GetByShippingOrderCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.Status)
            .Include(r => r.Order).ThenInclude(o => o.Status)
            .Include(r => r.Order).ThenInclude(o => o.ShippingProviderTransactions).ThenInclude(t => t.ShippingStatusHistories)
            .Include(r => r.RefundReason)
            .Include(r => r.Customer).ThenInclude(c => c.Address).ThenInclude(a => a.Province)
            .Include(r => r.Customer).ThenInclude(c => c.Address).ThenInclude(a => a.District)
            .Include(r => r.Customer).ThenInclude(c => c.Address).ThenInclude(a => a.WardCodeNavigation)
            .Include(r => r.RequestedByNavigation)
            .Include(r => r.RefundImages.Where(i => !i.IsDeleted))
            .Include(r => r.RefundDetails).ThenInclude(d => d.Product).ThenInclude(p => p.ProductImage)
            .Include(r => r.RefundStatusHistories).ThenInclude(h => h.Status)
            .Include(r => r.RefundStatusHistories).ThenInclude(h => h.ChangedByNavigation)
            .FirstOrDefaultAsync(r => r.ShippingOrderCode == code && !r.IsDeleted, cancellationToken);
    }

    public async Task<OrderRefund?> GetByShippingOrReturnOrderCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.Status)
            .Include(r => r.Order).ThenInclude(o => o.Status)
            .Include(r => r.Order).ThenInclude(o => o.ShippingProviderTransactions).ThenInclude(t => t.ShippingStatusHistories)
            .Include(r => r.RefundReason)
            .Include(r => r.Customer).ThenInclude(c => c.Address).ThenInclude(a => a.Province)
            .Include(r => r.Customer).ThenInclude(c => c.Address).ThenInclude(a => a.District)
            .Include(r => r.Customer).ThenInclude(c => c.Address).ThenInclude(a => a.WardCodeNavigation)
            .Include(r => r.RequestedByNavigation)
            .Include(r => r.RefundImages.Where(i => !i.IsDeleted))
            .Include(r => r.RefundDetails).ThenInclude(d => d.Product).ThenInclude(p => p.ProductImage)
            .Include(r => r.RefundStatusHistories).ThenInclude(h => h.Status)
            .Include(r => r.RefundStatusHistories).ThenInclude(h => h.ChangedByNavigation)
            .FirstOrDefaultAsync(r => (r.ShippingOrderCode == code || r.ReturnShippingOrderCode == code) && !r.IsDeleted, cancellationToken);
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
