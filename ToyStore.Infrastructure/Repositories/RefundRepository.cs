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

/// <summary>
/// Repository triển khai các phương thức truy vấn, lọc, phân trang và thao tác dữ liệu với thực thể Yêu cầu hoàn tiền (OrderRefund).
/// </summary>
public class RefundRepository : IRefundRepository
{
    private readonly SEP490ToyStoreContext _context;
    private readonly ITimeProvider _timeProvider;

    /// <summary>
    /// Khởi tạo RefundRepository với DbContext và TimeProvider.
    /// </summary>
    public RefundRepository(SEP490ToyStoreContext context, ITimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Lấy danh sách các lý do hoàn tiền đang hoạt động và không phải lý do hệ thống (IsSystem = false).
    /// </summary>
    public async Task<List<OrderRefundReason>> GetActiveReasonsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<OrderRefundReason>()
            .Where(r => !r.IsDeleted && !r.IsSystem) // Chỉ lấy các lý do hoạt động và ẩn lý do Hệ thống (System-only)
            .OrderBy(r => r.RefundReasonId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Tìm lý do hoàn tiền theo nội dung mô tả; nếu chưa tồn tại lý do hệ thống do GHN giao hàng thất bại thì tự động khởi tạo.
    /// </summary>
    public async Task<OrderRefundReason?> GetReasonByContentAsync(string content, CancellationToken cancellationToken = default)
    {
        var reason = await _context.Set<OrderRefundReason>()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Content == content, cancellationToken);

        if (reason is null && content == "Delivery failed / unable to deliver")
        {
            var newReason = new OrderRefundReason
            {
                Content = content,
                Description = "Automatic refund when GHN returns the package to Merchandise due to failed delivery (System-only)",
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

    /// <summary>
    /// Lấy danh sách yêu cầu hoàn tiền của khách hàng có phân trang và lọc theo trạng thái, mã đơn, khoảng ngày.
    /// </summary>
    public async Task<PaginatedResponse<RefundListDto>> GetRefundsAsync(int customerId, RefundFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _context.OrderRefunds
            .Include(r => r.Status)
            .Include(r => r.Order).ThenInclude(o => o.Status)
            .Include(r => r.RefundReason)
            .Include(r => r.Customer)
            .Include(r => r.RequestedByNavigation)
            .Where(r => !r.IsDeleted && r.CustomerId == customerId)
            .AsNoTracking();

        // Lọc theo trạng thái hoàn tiền (hỗ trợ nhiều trạng thái cách nhau bởi dấu phẩy)
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

        // Lọc theo mã đơn hàng
        if (filter.OrderId.HasValue)
            query = query.Where(r => r.OrderId == filter.OrderId.Value);

        // Lọc từ ngày
        if (filter.FromDate.HasValue)
        {
            var startUtc = _timeProvider.ToUtc(filter.FromDate.Value.Date);
            query = query.Where(r => r.CreatedAt >= startUtc);
        }

        // Lọc đến ngày
        if (filter.ToDate.HasValue)
        {
            var endUtc = _timeProvider.ToUtc(filter.ToDate.Value.Date.AddDays(1));
            query = query.Where(r => r.CreatedAt < endUtc);
        }

        var totalItems = await query.CountAsync(cancellationToken);

        // Phân trang và ánh xạ DTO
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
                FinalRefundAmount = r.FinalRefundAmount,
                RefundStatus = r.Status.StatusName,
                CreatedAt = r.CreatedAt,
                ReturnToCustomerFeePaid = r.ReturnToCustomerFeePaid,
                ReturnToCustomerFee = r.ReturnToCustomerFee,
                CustomerResponse = r.CustomerResponse
            })
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<RefundListDto>(items, totalItems, filter.Page, filter.PageSize);
    }

    /// <summary>
    /// Lấy danh sách yêu cầu hoàn tiền cho giao diện Quản trị viên/Nhân viên với bộ lọc nâng cao và phân quyền xem theo phân công.
    /// </summary>
    public async Task<PaginatedResponse<RefundListDto>> GetAdminRefundsAsync(AdminRefundFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _context.OrderRefunds
            .Include(r => r.Status)
            .Include(r => r.Order).ThenInclude(o => o.Status)
            .Include(r => r.RefundReason)
            .Include(r => r.Customer)
            .Include(r => r.RequestedByNavigation)
            .Where(r => !r.IsDeleted)
            .AsNoTracking();

        // Lọc theo trạng thái
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

        // Lọc theo mã đơn hàng
        if (filter.OrderId.HasValue)
            query = query.Where(r => r.OrderId == filter.OrderId.Value);

        // Lọc theo khách hàng
        if (filter.CustomerId.HasValue)
            query = query.Where(r => r.CustomerId == filter.CustomerId.Value);

        // Lọc theo lý do hoàn tiền
        if (filter.RefundReasonId.HasValue)
            query = query.Where(r => r.RefundReasonId == filter.RefundReasonId.Value);

        // Lọc từ ngày
        if (filter.FromDate.HasValue)
        {
            var startUtc = _timeProvider.ToUtc(filter.FromDate.Value.Date);
            query = query.Where(r => r.CreatedAt >= startUtc);
        }

        // Lọc đến ngày
        if (filter.ToDate.HasValue)
        {
            var endUtc = _timeProvider.ToUtc(filter.ToDate.Value.Date.AddDays(1));
            query = query.Where(r => r.CreatedAt < endUtc);
        }

        // Lọc theo phân công nhân viên xử lý
        if (filter.AssignedToMe && filter.AssignedAccountId.HasValue)
        {
            query = query.Where(r => r.Order.AssignedToStaffId == filter.AssignedAccountId.Value ||
                r.Order.AssignedToMerchId == filter.AssignedAccountId.Value ||
                _context.Set<OrderAssignment>().Any(a => a.OrderId == r.OrderId && a.AccountId == filter.AssignedAccountId.Value && a.IsActive));
        }

        // Lọc theo phạm vi tiến độ xử lý (Hoàn thành / Đang xử lý)
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

        // Tìm kiếm từ khóa theo nhiều trường thông tin
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

        // Sắp xếp
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
                FinalRefundAmount = r.FinalRefundAmount,
                RefundStatus = r.Status.StatusName,
                CreatedAt = r.CreatedAt,
                ReturnToCustomerFeePaid = r.ReturnToCustomerFeePaid,
                ReturnToCustomerFee = r.ReturnToCustomerFee,
                CustomerResponse = r.CustomerResponse,
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

    /// <summary>
    /// Lấy chi tiết yêu cầu hoàn tiền theo ID kèm toàn bộ entity liên quan.
    /// </summary>
    public async Task<OrderRefund?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.OrderRefunds
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
            .Include(r => r.RefundDetails).ThenInclude(d => d.Product).ThenInclude(p => p.ProductDetail)
            .Include(r => r.RefundDetails).ThenInclude(d => d.Product).ThenInclude(p => p.Category)
            .Include(r => r.RefundStatusHistories).ThenInclude(h => h.Status)
            .Include(r => r.RefundStatusHistories).ThenInclude(h => h.ChangedByNavigation)
            .FirstOrDefaultAsync(r => r.RefundId == id && !r.IsDeleted, cancellationToken);
    }

    /// <summary>
    /// Lấy yêu cầu hoàn tiền theo mã đơn hàng gốc.
    /// </summary>
    public async Task<OrderRefund?> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return await _context.OrderRefunds
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
            .Include(r => r.RefundDetails).ThenInclude(d => d.Product).ThenInclude(p => p.ProductDetail)
            .Include(r => r.RefundDetails).ThenInclude(d => d.Product).ThenInclude(p => p.Category)
            .Include(r => r.RefundStatusHistories).ThenInclude(h => h.Status)
            .Include(r => r.RefundStatusHistories).ThenInclude(h => h.ChangedByNavigation)
            .FirstOrDefaultAsync(r => r.OrderId == orderId && !r.IsDeleted, cancellationToken);
    }

    /// <summary>
    /// Tìm yêu cầu hoàn tiền theo mã vận đơn chuyển hàng ban đầu của GHN.
    /// </summary>
    public async Task<OrderRefund?> GetByShippingOrderCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.OrderRefunds
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
            .Include(r => r.RefundDetails).ThenInclude(d => d.Product).ThenInclude(p => p.ProductDetail)
            .Include(r => r.RefundDetails).ThenInclude(d => d.Product).ThenInclude(p => p.Category)
            .Include(r => r.RefundStatusHistories).ThenInclude(h => h.Status)
            .Include(r => r.RefundStatusHistories).ThenInclude(h => h.ChangedByNavigation)
            .FirstOrDefaultAsync(r => r.ShippingOrderCode == code && !r.IsDeleted, cancellationToken);
    }

    /// <summary>
    /// Tìm yêu cầu hoàn tiền theo mã vận đơn chuyển hàng hoặc mã vận đơn trả hàng.
    /// </summary>
    public async Task<OrderRefund?> GetByShippingOrReturnOrderCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;

        var cleanCode = code.Trim();

        var refund = await _context.OrderRefunds
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
            .Include(r => r.RefundDetails).ThenInclude(d => d.Product).ThenInclude(p => p.ProductDetail)
            .Include(r => r.RefundDetails).ThenInclude(d => d.Product).ThenInclude(p => p.Category)
            .Include(r => r.RefundStatusHistories).ThenInclude(h => h.Status)
            .Include(r => r.RefundStatusHistories).ThenInclude(h => h.ChangedByNavigation)
            .FirstOrDefaultAsync(r => (r.ShippingOrderCode == cleanCode || r.ReturnShippingOrderCode == cleanCode || r.RefundCode == cleanCode) && !r.IsDeleted, cancellationToken);

        if (refund != null) return refund;

        // Bóc tách tiền tố R- hoặc R2- nếu client order code từ GHN gửi dạng R-REF-xxx hoặc R2-REF-xxx
        string strippedCode = cleanCode;
        if (cleanCode.StartsWith("R2-", StringComparison.OrdinalIgnoreCase))
        {
            strippedCode = cleanCode.Substring(3);
        }
        else if (cleanCode.StartsWith("R-", StringComparison.OrdinalIgnoreCase))
        {
            strippedCode = cleanCode.Substring(2);
        }

        if (!string.Equals(strippedCode, cleanCode, StringComparison.OrdinalIgnoreCase))
        {
            refund = await _context.OrderRefunds
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
                .Include(r => r.RefundDetails).ThenInclude(d => d.Product).ThenInclude(p => p.ProductDetail)
                .Include(r => r.RefundDetails).ThenInclude(d => d.Product).ThenInclude(p => p.Category)
                .Include(r => r.RefundStatusHistories).ThenInclude(h => h.Status)
                .Include(r => r.RefundStatusHistories).ThenInclude(h => h.ChangedByNavigation)
                .FirstOrDefaultAsync(r => (r.RefundCode == strippedCode || r.ShippingOrderCode == strippedCode || r.ReturnShippingOrderCode == strippedCode) && !r.IsDeleted, cancellationToken);
        }

        return refund;
    }

    /// <summary>
    /// Thêm mới một yêu cầu hoàn tiền vào DbContext.
    /// </summary>
    public async Task<OrderRefund> AddAsync(OrderRefund refund, CancellationToken cancellationToken = default)
    {
        var result = await _context.OrderRefunds.AddAsync(refund, cancellationToken);
        return result.Entity;
    }

    /// <summary>
    /// Đánh dấu cập nhật một yêu cầu hoàn tiền.
    /// </summary>
    public void Update(OrderRefund refund)
    {
        _context.OrderRefunds.Update(refund);
    }

    /// <summary>
    /// Lấy danh sách các yêu cầu hoàn tiền quá hạn thanh toán phí gửi trả hàng (48 giờ) để tự động xử lý tiêu hủy.
    /// </summary>
    public async Task<List<OrderRefund>> GetStaleUnpaidRefundsAsync(System.DateTime cutoff, CancellationToken cancellationToken = default)
    {
        return await _context.OrderRefunds
            .Include(r => r.Status)
            .Include(r => r.Order)
            .Include(r => r.RefundDetails).ThenInclude(d => d.Product)
            .Where(r => !r.IsDeleted 
                && r.StatusId == (byte)ToyStore.Domain.Enums.RefundStatusEnum.RefundInspectionPending
                && r.ReturnToCustomerFeePaid == false
                && r.CustomerResponseDeadline.HasValue
                && r.CustomerResponseDeadline.Value < cutoff)
            .ToListAsync(cancellationToken);
    }
}
