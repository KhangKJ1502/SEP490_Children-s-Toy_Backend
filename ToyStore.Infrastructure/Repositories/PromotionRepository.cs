using Microsoft.EntityFrameworkCore;
using ToyStore.Application.DTOs;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

/// <summary>
/// Repository triển khai các thao tác truy vấn và biến đổi dữ liệu thực thể Promotion, ProductPromotion, PromotionTimeSlot với Entity Framework Core.
/// </summary>
public class PromotionRepository : IPromotionRepository
{
    private readonly SEP490ToyStoreContext _context;

    /// <summary>
    /// Khởi tạo PromotionRepository với DbContext.
    /// </summary>
    /// <param name="context">DbContext kết nối cơ sở dữ liệu ToyStore.</param>
    public PromotionRepository(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy danh sách chương trình khuyến mãi có phân trang, tìm kiếm theo tên/mô tả và lọc theo trạng thái, hỗ trợ sắp xếp linh hoạt.
    /// </summary>
    /// <param name="pageNumber">Số thứ tự trang (1-based).</param>
    /// <param name="pageSize">Số lượng bản ghi mỗi trang.</param>
    /// <param name="sortBy">Tên trường sắp xếp (name, startdate, enddate, priority, createdat).</param>
    /// <param name="sortDesc">true để giảm dần, false để tăng dần.</param>
    /// <param name="searchTerm">Từ khóa tìm kiếm theo tên hoặc mô tả.</param>
    /// <param name="status">Trạng thái cần lọc chính xác (Scheduled, Active, Inactive, Expired).</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Đối tượng PaginatedResponse chứa danh sách Promotion và tổng số lượng bản ghi thỏa điều kiện.</returns>
    public async Task<PaginatedResponse<Promotion>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        // Loại bỏ các bản ghi đã xóa mềm (IsDeleted = true)
        var query = _context.Promotions.Where(x => !x.IsDeleted);

        // Lọc theo từ khóa tìm kiếm trong PromotionName hoặc Description
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x => x.PromotionName.Contains(searchTerm) ||
                                     (x.Description != null && x.Description.Contains(searchTerm)));
        }

        // Lọc chính xác theo trạng thái nếu có
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        // Đếm tổng số bản ghi thỏa điều kiện
        var totalCount = await query.CountAsync(cancellationToken);

        // Sắp xếp động theo trường chỉ định
        query = sortBy?.ToLower() switch
        {
            "name" => sortDesc ? query.OrderByDescending(x => x.PromotionName) : query.OrderBy(x => x.PromotionName),
            "startdate" => sortDesc ? query.OrderByDescending(x => x.StartDate) : query.OrderBy(x => x.StartDate),
            "enddate" => sortDesc ? query.OrderByDescending(x => x.EndDate) : query.OrderBy(x => x.EndDate),
            "priority" => sortDesc ? query.OrderByDescending(x => x.Priority) : query.OrderBy(x => x.Priority),
            _ => sortDesc ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt)
        };

        // Phân trang bằng Skip và Take
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<Promotion>(items, totalCount, pageNumber, pageSize);
    }

    /// <summary>
    /// Lấy thông tin chương trình khuyến mãi theo ID, hỗ trợ nạp kèm động các navigation property (Eager Loading).
    /// </summary>
    /// <param name="promotionId">Mã ID của khuyến mãi.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <param name="includeProperties">Chuỗi tên các navigation property phân tách bằng dấu phẩy.</param>
    /// <returns>Thực thể Promotion hoặc null nếu không tồn tại hoặc đã bị xóa mềm.</returns>
    public async Task<Promotion?> GetByIdAsync(int promotionId, CancellationToken cancellationToken = default, string? includeProperties = null)
    {
        IQueryable<Promotion> query = _context.Promotions;

        // Nạp kèm các navigation properties nếu được truyền vào
        if (!string.IsNullOrWhiteSpace(includeProperties))
        {
            foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty);
            }
        }

        return await query
            .Where(x => !x.IsDeleted && x.PromotionId == promotionId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Kiểm tra xem tên chương trình khuyến mãi đã tồn tại trong database hay chưa (bỏ qua bản ghi đã xóa mềm).
    /// Hỗ trợ loại trừ ID hiện tại khi thực hiện cập nhật.
    /// </summary>
    /// <param name="promotionName">Tên khuyến mãi cần kiểm tra.</param>
    /// <param name="excludePromotionId">ID khuyến mãi cần loại trừ.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>true nếu tên đã tồn tại, false nếu chưa.</returns>
    public async Task<bool> ExistsPromotionNameAsync(
        string promotionName,
        int? excludePromotionId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Promotions.Where(x => !x.IsDeleted && x.PromotionName == promotionName);

        if (excludePromotionId.HasValue)
        {
            query = query.Where(x => x.PromotionId != excludePromotionId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// Thêm thực thể Promotion mới vào DbSet trong DbContext.
    /// </summary>
    /// <param name="promotion">Thực thể Promotion cần thêm.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    public async Task AddAsync(Promotion promotion, CancellationToken cancellationToken = default)
    {
        await _context.AddAsync(promotion, cancellationToken);
    }

    /// <summary>
    /// Đánh dấu thực thể Promotion đã được chỉnh sửa trong DbContext.
    /// </summary>
    /// <param name="promotion">Thực thể Promotion cần cập nhật.</param>
    public void Update(Promotion promotion)
    {
        _context.Attach(promotion);
        _context.Entry(promotion).State = EntityState.Modified;
    }

    /// <summary>
    /// Xóa một bản ghi ProductPromotion khỏi DbSet.
    /// </summary>
    /// <param name="productPromotion">Bản ghi ProductPromotion cần xóa.</param>
    public void RemoveProductPromotion(ProductPromotion productPromotion)
    {
        _context.ProductPromotions.Remove(productPromotion);
    }

    /// <summary>
    /// Xóa một khung giờ PromotionTimeSlot khỏi DbSet.
    /// </summary>
    /// <param name="promotionTimeSlot">Bản ghi PromotionTimeSlot cần xóa.</param>
    public void RemovePromotionTimeSlot(PromotionTimeSlot promotionTimeSlot)
    {
        _context.Remove(promotionTimeSlot);
    }

    /// <summary>
    /// Kiểm tra xem một sản phẩm có đang nằm trong bất kỳ chương trình khuyến mãi nào đang Active hoặc Scheduled hay không.
    /// Quét cả 2 nguồn: Khuyến mãi thông thường (ProductPromotions) và Flash Sale (PromotionProductSlots).
    /// </summary>
    /// <param name="productId">Mã ID sản phẩm cần kiểm tra.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>true nếu sản phẩm đang tham gia khuyến mãi hiệu lực, false nếu chưa.</returns>
    public async Task<bool> IsProductInActivePromotionAsync(int productId, CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra trong khuyến mãi thông thường (DISCOUNT - ProductPromotions)
        bool inDiscount = await _context.ProductPromotions
            .Include(pp => pp.Promotion)
            .AnyAsync(pp => pp.ProductId == productId 
                && !pp.IsDeleted
                && !pp.Promotion.IsDeleted 
                && (pp.Promotion.Status == "Active" || pp.Promotion.Status == "Scheduled"), cancellationToken);

        if (inDiscount) return true;

        // 2. Kiểm tra trong Flash Sale (FLASH_SALE - PromotionProductSlots)
        bool inFlashSale = await _context.PromotionProductSlots
            .Include(pps => pps.TimeSlot)
            .ThenInclude(ts => ts.Promotion)
            .AnyAsync(pps => pps.ProductId == productId 
                && !pps.IsDeleted
                && !pps.TimeSlot.IsDeleted
                && !pps.TimeSlot.Promotion.IsDeleted
                && (pps.TimeSlot.Promotion.Status == "Active" || pps.TimeSlot.Promotion.Status == "Scheduled"), cancellationToken);

        return inFlashSale;
    }

    /// <summary>
    /// Lấy danh sách toàn bộ các chương trình khuyến mãi (cả DISCOUNT thông thường và FLASH_SALE theo khung giờ) đang áp dụng cho một sản phẩm cụ thể.
    /// </summary>
    /// <param name="productId">Mã ID sản phẩm.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Danh sách ProductPromotionInfoDto được sắp xếp theo ngày bắt đầu giảm dần.</returns>
    public async Task<List<ToyStore.Application.DTOs.Promotions.ProductPromotionInfoDto>> GetPromotionsByProductIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // 1. Lấy danh sách từ bảng ProductPromotions (khuyến mãi thông thường)
        var discountPromos = await _context.ProductPromotions
            .Include(pp => pp.Promotion)
            .Where(pp => pp.ProductId == productId && !pp.IsDeleted && !pp.Promotion.IsDeleted && pp.Promotion.Status != "Expired" && pp.Promotion.EndDate >= now)
            .Select(pp => new ToyStore.Application.DTOs.Promotions.ProductPromotionInfoDto
            {
                PromotionId = pp.PromotionId,
                PromotionName = pp.Promotion.PromotionName,
                PromotionType = pp.Promotion.PromotionType,
                StartDate = pp.Promotion.StartDate,
                EndDate = pp.Promotion.EndDate,
                Status = pp.Promotion.Status,
                SalePrice = pp.SalePrice,
                DiscountPercent = pp.DiscountPercent,
                Priority = pp.Promotion.Priority,
                SaleQuantity = null,
                SoldQuantity = null
            })
            .ToListAsync(cancellationToken);

        // 2. Lấy danh sách từ bảng PromotionProductSlots (Flash Sale theo khung giờ)
        var flashSalePromos = await _context.PromotionProductSlots
            .Include(pps => pps.TimeSlot)
            .ThenInclude(ts => ts.Promotion)
            .Where(pps => pps.ProductId == productId && !pps.IsDeleted && !pps.TimeSlot.IsDeleted && !pps.TimeSlot.Promotion.IsDeleted && pps.TimeSlot.Promotion.Status != "Expired" && pps.TimeSlot.EndAt >= now)
            .Select(pps => new ToyStore.Application.DTOs.Promotions.ProductPromotionInfoDto
            {
                PromotionId = pps.TimeSlot.PromotionId,
                PromotionName = pps.TimeSlot.Promotion.PromotionName,
                PromotionType = pps.TimeSlot.Promotion.PromotionType,
                StartDate = pps.TimeSlot.StartAt,
                EndDate = pps.TimeSlot.EndAt,
                Status = pps.TimeSlot.Status,
                SalePrice = pps.SalePrice,
                DiscountPercent = pps.DiscountPercent,
                Priority = pps.TimeSlot.Promotion.Priority,
                SaleQuantity = pps.SaleQuantity,
                SoldQuantity = pps.SoldQuantity
            })
            .ToListAsync(cancellationToken);

        // Hợp nhất 2 danh sách và sắp xếp theo StartDate mới nhất
        return discountPromos.Concat(flashSalePromos)
            .OrderByDescending(p => p.StartDate)
            .ToList();
    }

    /// <summary>
    /// Lấy danh sách các chương trình FLASH_SALE đang Active hoặc Scheduled trong cửa sổ hiển thị (visibilityDays),
    /// nạp kèm đầy đủ thông tin Time Slots, danh sách sản phẩm và ảnh sản phẩm.
    /// </summary>
    /// <param name="visibilityDays">Số ngày giới hạn nhìn thấy trước (mặc định là 2 ngày).</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Danh sách thực thể Promotion thuộc loại Flash Sale.</returns>
    public async Task<List<Promotion>> GetFlashSalePromotionsAsync(
        int visibilityDays = 2,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var maxVisibleDate = now.AddDays(visibilityDays);

        return await _context.Promotions
            .Where(p => !p.IsDeleted
                && p.PromotionType == "FLASH_SALE"
                && p.EndDate >= now
                && (p.Status == "Active" || (p.Status == "Scheduled" && p.StartDate <= maxVisibleDate)))
            .Include(p => p.PromotionTimeSlots.Where(ts => !ts.IsDeleted && (ts.Status == "Active" || (ts.Status == "Scheduled" && ts.StartAt <= maxVisibleDate))))
                .ThenInclude(ts => ts.PromotionProductSlots.Where(pps => !pps.IsDeleted))
                    .ThenInclude(pps => pps.Product)
                        .ThenInclude(prod => prod.ProductImage)
            .OrderBy(p => p.Priority)
            .ToListAsync(cancellationToken);
    }
}
