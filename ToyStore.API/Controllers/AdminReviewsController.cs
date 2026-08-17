using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Reviews;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

/// <summary>
/// Controller cung cấp các API dành cho Quản trị viên (Admin) và Nhân viên (Staff) để quản lý đánh giá sản phẩm.
/// Bao gồm: tra cứu danh sách kiểm duyệt, xem chi tiết đánh giá, thay đổi trạng thái kiểm duyệt (Duyệt/Ẩn/Từ chối), và phản hồi đánh giá khách hàng.
/// </summary>
[ApiController]
[Route("api/admin/reviews")]
[Authorize(Roles = "Staff,Admin")]
public class AdminReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    /// <summary>
    /// Khởi tạo AdminReviewsController với service quản lý đánh giá.
    /// </summary>
    /// <param name="reviewService">Service xử lý nghiệp vụ đánh giá sản phẩm.</param>
    public AdminReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    /// <summary>
    /// Admin/Staff lấy danh sách toàn bộ đánh giá sản phẩm có phân trang, lọc theo trạng thái kiểm duyệt, số sao, từ khóa, khoảng ngày.
    /// Endpoint: GET /api/admin/reviews
    /// </summary>
    /// <param name="query">DTO chứa các tham số lọc: ProductId, Rating, ModerationStatus, SearchTerm, CreatedFrom, CreatedTo, PageNumber, PageSize.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Danh sách phân trang PaginatedResponse chứa các AdminReviewListDto.</returns>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<AdminReviewListDto>>> GetReviews(
        [FromQuery] AdminReviewQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await _reviewService.GetAdminListAsync(query, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Admin/Staff xem thông tin chi tiết một đánh giá sản phẩm (bao gồm nhật ký kiểm duyệt và phản hồi của nhân viên).
    /// Endpoint: GET /api/admin/reviews/{id}
    /// </summary>
    /// <param name="id">Mã ID của đánh giá cần xem.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>DTO chi tiết AdminReviewDetailDto.</returns>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdminReviewDetailDto>> GetReviewDetail(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _reviewService.GetAdminDetailAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Admin/Staff cập nhật trạng thái kiểm duyệt thủ công cho một đánh giá (APPROVED, REJECTED, HIDDEN) kèm lý do và ghi nhận log kiểm duyệt.
    /// Endpoint: PUT /api/admin/reviews/{id}/status
    /// </summary>
    /// <param name="id">Mã ID của đánh giá cần thay đổi trạng thái kiểm duyệt.</param>
    /// <param name="dto">DTO chứa trạng thái mới (ModerationStatus) và lý do (Reason).</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>DTO AdminReviewDetailDto sau khi cập nhật.</returns>
    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<AdminReviewDetailDto>> UpdateStatus(
        int id,
        [FromBody] UpdateModerationStatusDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _reviewService.UpdateModerationStatusAsync(id, dto, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Admin/Staff tạo phản hồi chính thức (Official Reply) cho một đánh giá sản phẩm của khách hàng.
    /// Endpoint: POST /api/admin/reviews/{id}/reply
    /// </summary>
    /// <param name="id">Mã ID của đánh giá sản phẩm cần phản hồi.</param>
    /// <param name="dto">DTO chứa nội dung phản hồi (ReplyContent).</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>DTO StaffReplyDto đại diện cho phản hồi vừa tạo.</returns>
    [HttpPost("{id:int}/reply")]
    public async Task<ActionResult<StaffReplyDto>> CreateReply(
        int id,
        [FromBody] CreateStaffReplyDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _reviewService.CreateReplyAsync(id, dto, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Admin/Staff chỉnh sửa nội dung phản hồi đã tạo trước đó.
    /// Endpoint: PUT /api/admin/reviews/{id}/reply/{replyId}
    /// </summary>
    /// <param name="id">Mã ID của đánh giá sản phẩm.</param>
    /// <param name="replyId">Mã ID của bản ghi phản hồi cần sửa.</param>
    /// <param name="dto">DTO chứa nội dung phản hồi mới (ReplyContent).</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>DTO StaffReplyDto sau khi cập nhật.</returns>
    [HttpPut("{id:int}/reply/{replyId:int}")]
    public async Task<ActionResult<StaffReplyDto>> UpdateReply(
        int id,
        int replyId,
        [FromBody] UpdateStaffReplyDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _reviewService.UpdateReplyAsync(id, replyId, dto, cancellationToken);
        return result.ToActionResult();
    }
}
