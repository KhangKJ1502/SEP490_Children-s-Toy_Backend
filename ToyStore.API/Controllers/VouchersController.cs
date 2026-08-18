using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Vouchers;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

/// <summary>
/// Controller cung cấp các API endpoint phục vụ quản lý và truy vấn Voucher (mã giảm giá).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class VouchersController : ControllerBase
{
    private readonly IVoucherService _voucherService;
    private readonly ILogger<VouchersController> _logger;

    /// <summary>
    /// Khởi tạo controller với các service phụ thuộc cần thiết.
    /// </summary>
    /// <param name="voucherService">Service xử lý nghiệp vụ liên quan đến Voucher.</param>
    /// <param name="logger">Logger ghi log hoạt động của VouchersController.</param>
    public VouchersController(IVoucherService voucherService, ILogger<VouchersController> logger)
    {
        _voucherService = voucherService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách voucher có hỗ trợ phân trang, tìm kiếm theo từ khóa, lọc theo trạng thái và sắp xếp.
    /// </summary>
    /// <param name="pageNumber">Số thứ tự trang hiện tại (mặc định là 1).</param>
    /// <param name="pageSize">Số lượng bản ghi trên một trang (mặc định là 10, tối đa 100).</param>
    /// <param name="sortBy">Trường cần sắp xếp (ví dụ: VoucherCode, DiscountValue, StartDate, EndDate, Status, CreatedAt).</param>
    /// <param name="sortDesc">Thứ tự sắp xếp: true để giảm dần, false để tăng dần (mặc định là false).</param>
    /// <param name="searchTerm">Từ khóa tìm kiếm theo mã voucher, tên voucher hoặc mô tả.</param>
    /// <param name="status">Trạng thái voucher cần lọc (Scheduled, Active, Inactive, Expired, Pending, Rejected).</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Danh sách phân trang chứa các VoucherListDto.</returns>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<VoucherListDto>>> GetVouchers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        // Gọi tầng Service để lấy danh sách voucher theo các tiêu chí phân trang & bộ lọc
        var result = await _voucherService.GetVouchersAsync(
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            searchTerm,
            status,
            cancellationToken);

        // Chuyển đổi kết quả Service Result sang HTTP ActionResult tương ứng (200 OK hoặc mã lỗi)
        return result.ToActionResult();
    }

    /// <summary>
    /// Tạo mới một voucher trong hệ thống (Yêu cầu quyền Admin hoặc Staff).
    /// </summary>
    /// <param name="request">Thông tin dữ liệu tạo voucher mới.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thông tin chi tiết của voucher vừa được tạo (HTTP 201 Created kèm Header Location).</returns>
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<VoucherDto>> CreateVoucher(
        [FromBody] CreateVoucherDto request,
        CancellationToken cancellationToken = default)
    {
        // Gọi Service thực hiện kiểm tra nghiệp vụ, phân quyền và lưu voucher mới
        var result = await _voucherService.CreateVoucherAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Created voucher {VoucherId} via API",
                result.Data!.VoucherId);

            // Trả về HTTP 201 Created cùng đường dẫn lấy chi tiết voucher vừa tạo
            return result.ToCreatedResult($"/api/vouchers/{result.Data!.VoucherId}");
        }

        // Trường hợp thất bại (Validation error, Conflict, ...), trả về HTTP response tương ứng
        return result.ToActionResult();
    }

    /// <summary>
    /// Cập nhật thông tin voucher theo ID (Yêu cầu quyền Admin hoặc Staff).
    /// Hỗ trợ cập nhật từng phần (Partial Update).
    /// </summary>
    /// <param name="voucherId">Mã định danh duy nhất (ID) của voucher cần cập nhật.</param>
    /// <param name="request">Dữ liệu các trường cần cập nhật của voucher.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thông tin chi tiết voucher sau khi đã cập nhật thành công.</returns>
    [HttpPut("{voucherId:int}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<VoucherDto>> UpdateVoucher(
        int voucherId,
        [FromBody] UpdateVoucherDto request,
        CancellationToken cancellationToken = default)
    {
        // Gọi Service xử lý logic cập nhật, kiểm tra ràng buộc voucher đã dùng/hết hạn, trạng thái duyệt
        var result = await _voucherService.UpdateVoucherAsync(voucherId, request, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Lấy thông tin chi tiết của một voucher theo ID.
    /// </summary>
    /// <param name="voucherId">Mã định danh duy nhất (ID) của voucher.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thông tin chi tiết của voucher (VoucherDto) hoặc 404 NotFound nếu không tìm thấy.</returns>
    [HttpGet("{voucherId:int}")]
    public async Task<ActionResult<VoucherDto>> GetVoucherById(
        int voucherId,
        CancellationToken cancellationToken = default)
    {
        // Gọi Service để tìm nạp thông tin voucher theo ID
        var result = await _voucherService.GetVoucherByIdAsync(voucherId, cancellationToken);
        return result.ToActionResult();
    }
}
