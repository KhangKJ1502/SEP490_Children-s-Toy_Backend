using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Infrastructure.Data;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Repositories;

/// <summary>
/// Repository phụ trách truy vấn và thao tác dữ liệu thực thể Voucher với Entity Framework Core.
/// </summary>
public class VoucherRepository : IVoucherRepository
{
    private readonly SEP490ToyStoreContext _context;

    /// <summary>
    /// Khởi tạo VoucherRepository với DbContext.
    /// </summary>
    /// <param name="context">DbContext kết nối cơ sở dữ liệu ToyStore.</param>
    public VoucherRepository(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy danh sách voucher có phân trang, lọc theo từ khóa tìm kiếm (mã, tên, mô tả) và trạng thái, kèm sắp xếp linh hoạt.
    /// </summary>
    /// <param name="pageNumber">Số trang cần lấy (1-based).</param>
    /// <param name="pageSize">Số bản ghi trên mỗi trang.</param>
    /// <param name="sortBy">Tên thuộc tính cần sắp xếp.</param>
    /// <param name="sortDesc">true để sắp xếp giảm dần, false để tăng dần.</param>
    /// <param name="searchTerm">Chuỗi từ khóa tìm kiếm (chứa trong VoucherCode, VoucherName, VoucherDescription).</param>
    /// <param name="status">Trạng thái voucher cần lọc chính xác (Scheduled, Active, Inactive, Expired, Pending, Rejected).</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Đối tượng PaginatedResponse chứa danh sách Voucher và tổng số bản ghi thỏa điều kiện.</returns>
    public async Task<PaginatedResponse<Voucher>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        // Khởi tạo truy vấn AsNoTracking để tối ưu hiệu năng đọc dữ liệu, loại bỏ các bản ghi đã xóa mềm (IsDeleted = true)
        var query = _context.Vouchers
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        // Lọc theo từ khóa tìm kiếm nếu có
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var keyword = searchTerm.Trim();
            query = query.Where(x =>
                x.VoucherCode.Contains(keyword)
                || x.VoucherName.Contains(keyword)
                || x.VoucherDescription.Contains(keyword));
        }

        // Lọc chính xác theo trạng thái nếu có
        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim();
            query = query.Where(x => x.Status == normalizedStatus);
        }

        // Áp dụng sắp xếp theo trường chỉ định hoặc mặc định theo CreatedAt giảm dần
        var sortedQuery = ApplySorting(query, sortBy, sortDesc);

        // Đếm tổng số lượng bản ghi thỏa mãn điều kiện lọc
        var totalCount = await sortedQuery.CountAsync(cancellationToken);
        var skip = (pageNumber - 1) * pageSize;

        // Phân trang bằng Skip và Take
        var entities = await sortedQuery
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<Voucher>(entities, totalCount, pageNumber, pageSize);
    }

    /// <summary>
    /// Tìm voucher theo ID (khóa chính), có theo dõi (tracking) để phục vụ cập nhật trạng thái hoặc dữ liệu.
    /// </summary>
    /// <param name="voucherId">Mã định danh ID của voucher.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thực thể Voucher nếu tồn tại và chưa bị xóa, ngược lại trả về null.</returns>
    public async Task<Voucher?> GetByIdAsync(int voucherId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Vouchers
            .FirstOrDefaultAsync(x => x.VoucherId == voucherId && !x.IsDeleted, cancellationToken);

        return entity;
    }

    /// <summary>
    /// Tìm voucher theo mã VoucherCode duy nhất, không theo dõi (AsNoTracking) để tối ưu hiệu năng.
    /// </summary>
    /// <param name="voucherCode">Mã code voucher (ví dụ: GIAMGIA50K).</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thực thể Voucher nếu tìm thấy, ngược lại trả về null.</returns>
    public async Task<Voucher?> GetByCodeAsync(string voucherCode, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Vouchers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.VoucherCode == voucherCode && !x.IsDeleted,
                cancellationToken);

        return entity;
    }

    /// <summary>
    /// Kiểm tra xem mã voucher đã tồn tại hay chưa (bỏ qua bản ghi đã xóa mềm).
    /// Hỗ trợ loại trừ một ID cụ thể khi kiểm tra trong luồng cập nhật.
    /// </summary>
    /// <param name="voucherCode">Mã code voucher cần kiểm tra.</param>
    /// <param name="excludeVoucherId">ID voucher cần loại trừ (tùy chọn, dùng khi update).</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>true nếu mã đã tồn tại, false nếu chưa.</returns>
    public async Task<bool> ExistsVoucherCodeAsync(
        string voucherCode,
        int? excludeVoucherId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Vouchers
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.VoucherCode == voucherCode);

        // Loại trừ ID của voucher hiện tại nếu đang thực hiện cập nhật
        if (excludeVoucherId.HasValue)
        {
            query = query.Where(x => x.VoucherId != excludeVoucherId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// Thêm một thực thể Voucher mới vào DbSet trong DbContext.
    /// </summary>
    /// <param name="voucher">Thực thể Voucher cần thêm.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    public async Task AddAsync(Voucher voucher, CancellationToken cancellationToken = default)
    {
        await _context.AddAsync(voucher, cancellationToken);
    }

    /// <summary>
    /// Đánh dấu thực thể Voucher đã bị thay đổi để chuẩn bị lưu xuống database.
    /// </summary>
    /// <param name="voucher">Thực thể Voucher cần cập nhật.</param>
    public void Update(Voucher voucher)
    {
        _context.Update(voucher);
    }

    /// <summary>
    /// Đếm số lần mà một tài khoản khách hàng đã sử dụng voucher này.
    /// Có xử lý đặc biệt: Loại trừ các đơn hàng thanh toán chuyển khoản SE_PAY đang ở trạng thái PENDING chưa thanh toán thành công và chưa bị hủy,
    /// giúp khách hàng không bị mất lượt voucher nếu chưa thanh toán hoàn tất.
    /// </summary>
    /// <param name="voucherId">ID của voucher.</param>
    /// <param name="accountId">ID tài khoản khách hàng.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Số lần tài khoản đã sử dụng thành công voucher này.</returns>
    public Task<int> CountUsageByAccountAsync(int voucherId, int accountId, CancellationToken cancellationToken = default)
    {
        // Loại trừ các đơn hàng SE_PAY đang PENDING — voucher chỉ thực sự bị trừ khi thanh toán được xác nhận
        // Nhờ cơ chế chặn tối đa 1 đơn hàng pending trong CheckoutService, người dùng không thể lạm dụng để tích lũy chiết khấu
        return _context.VoucherUsageLogs
            .AsNoTracking()
            .Where(x => x.VoucherId == voucherId
                     && x.AccountId == accountId
                     && !(x.Order.PaymentMethod == "SE_PAY"
                          && x.Order.PaymentStatus == "PENDING"
                          && x.Order.CancelledAt == null))
            .CountAsync(cancellationToken);
    }

    /// <summary>
    /// Hàm tiện ích private để áp dụng biểu thức sắp xếp IQueryable dựa trên tên trường và hướng sắp xếp.
    /// </summary>
    /// <param name="query">IQueryable nguồn của thực thể Voucher.</param>
    /// <param name="sortBy">Tên trường cần sắp xếp.</param>
    /// <param name="sortDesc">true nếu sắp xếp giảm dần, false nếu tăng dần.</param>
    /// <returns>IQueryable đã được áp dụng OrderBy / OrderByDescending.</returns>
    private static IQueryable<Voucher> ApplySorting(IQueryable<Voucher> query, string? sortBy, bool sortDesc)
    {
        var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();

        return normalizedSortBy switch
        {
            "vouchercode" => sortDesc
                ? query.OrderByDescending(x => x.VoucherCode)
                : query.OrderBy(x => x.VoucherCode),
            "vouchername" => sortDesc
                ? query.OrderByDescending(x => x.VoucherName)
                : query.OrderBy(x => x.VoucherName),
            "discountvalue" => sortDesc
                ? query.OrderByDescending(x => x.DiscountValue)
                : query.OrderBy(x => x.DiscountValue),
            "startdate" => sortDesc
                ? query.OrderByDescending(x => x.StartDate)
                : query.OrderBy(x => x.StartDate),
            "enddate" => sortDesc
                ? query.OrderByDescending(x => x.EndDate)
                : query.OrderBy(x => x.EndDate),
            "status" => sortDesc
                ? query.OrderByDescending(x => x.Status)
                : query.OrderBy(x => x.Status),
            // Mặc định sắp xếp theo ngày tạo mới nhất (CreatedAt giảm dần)
            _ => (string.IsNullOrWhiteSpace(sortBy) || sortDesc)
                ? query.OrderByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.CreatedAt)
        };
    }
}
