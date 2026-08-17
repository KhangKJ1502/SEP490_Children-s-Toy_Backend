using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Refunds;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

/// <summary>
/// Controller xử lý các nghiệp vụ quản trị hoàn tiền/đổi trả dành cho Admin, Staff và Merchandise.
/// Bao gồm: tra cứu danh sách yêu cầu hoàn tiền (phân quyền xem của tôi hoặc toàn bộ), xem chi tiết,
/// cập nhật trạng thái quy trình xử lý (duyệt/từ chối/nhận hàng hoàn/hoàn tiền ví) và phân công lại nhân viên phụ trách.
/// </summary>
[ApiController]
[Route("api/admin/refunds")]
[Authorize(Roles = "Admin,Staff,Merchandise")]
public class AdminRefundsController : ControllerBase
{
    private readonly IRefundService _refundService;
    private readonly ICurrentUserService _currentUserService;

    /// <summary>
    /// Khởi tạo AdminRefundsController với IRefundService và ICurrentUserService.
    /// </summary>
    public AdminRefundsController(IRefundService refundService, ICurrentUserService currentUserService)
    {
        _refundService = refundService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Lấy danh sách yêu cầu hoàn tiền/đổi trả trong hệ thống quản trị.
    /// Nhân viên (Staff/Merchandise) chỉ thấy các đơn được phân công cho mình; Admin có thể xem toàn bộ.
    /// </summary>
    /// <param name="filter">Bộ lọc danh sách quản trị (trạng thái, mã đơn, ngày tạo, gán cho tôi,...).</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Danh sách phân trang yêu cầu hoàn tiền (RefundListDto).</returns>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<RefundListDto>>> GetAdminRefunds(
        [FromQuery] AdminRefundFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        filter.AssignedAccountId = _currentUserService.AccountId;
        if (!User.IsInRole("Admin"))
        {
            filter.AssignedToMe = true;
        }
        var result = await _refundService.GetAdminRefundsAsync(filter, cancellationToken);
        return Ok(ApiResponse<PaginatedResponse<RefundListDto>>.Ok(result));
    }

    /// <summary>
    /// Xem chi tiết một yêu cầu hoàn tiền trong giao diện quản trị (kèm thông tin phân công, hình ảnh, lịch sử trạng thái).
    /// </summary>
    /// <param name="refundId">Mã ID yêu cầu hoàn tiền.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Chi tiết yêu cầu hoàn tiền (RefundDto).</returns>
    [HttpGet("{refundId:int}")]
    public async Task<ActionResult<RefundDto>> GetAdminRefundById(
        [FromRoute] int refundId,
        CancellationToken cancellationToken = default)
    {
        var isAdmin = User.IsInRole("Admin");
        var result = await _refundService.AdminGetRefundByIdAsync(
            refundId, _currentUserService.AccountId, _currentUserService.RoleId, isAdmin, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Cập nhật trạng thái tiến trình xử lý yêu cầu hoàn tiền (ví dụ: Approved, Rejected, Returning, Received, Refunded).
    /// </summary>
    /// <param name="refundId">Mã ID yêu cầu hoàn tiền.</param>
    /// <param name="dto">Thông tin cập nhật trạng thái (trạng thái mới, lý do, bằng chứng,...).</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thông tin yêu cầu hoàn tiền sau khi cập nhật trạng thái.</returns>
    [HttpPatch("{refundId:int}/status")]
    public async Task<ActionResult<RefundDto>> UpdateRefundStatus(
        [FromRoute] int refundId,
        [FromBody] UpdateRefundStatusDto dto,
        CancellationToken cancellationToken = default)
    {
        var isAdmin = User.IsInRole("Admin");
        var result = await _refundService.UpdateRefundStatusAsync(
            _currentUserService.AccountId, _currentUserService.RoleId, refundId, dto, isAdmin, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Quản trị viên (Admin) chuyển giao/phân công lại nhân viên phụ trách xử lý yêu cầu hoàn tiền.
    /// </summary>
    /// <param name="refundId">Mã ID yêu cầu hoàn tiền.</param>
    /// <param name="dto">Thông tin phân công lại (mã nhân viên mới, lý do chuyển giao,...).</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Kết quả thực hiện phân công.</returns>
    [HttpPost("{refundId:int}/reassign")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ReassignRefund(
        [FromRoute] int refundId,
        [FromBody] ToyStore.Application.DTOs.Assignments.ReassignOrderRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _refundService.ReassignRefundAsync(refundId, dto, cancellationToken);
        return result.ToActionResult();
    }
}
