using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Reviews;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

/// <summary>
/// Controller xử lý các API liên quan đến Đánh giá sản phẩm (Product Review) phía Khách hàng (Customer) và Khách vãng lai (Guest).
/// Bao gồm: xem đánh giá công khai, tạo đánh giá mới (kèm upload ảnh), sửa đánh giá (giới hạn 1 lần trong 3 ngày), tra cứu sản phẩm chưa đánh giá, và Like/Unlike.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    /// <summary>
    /// Khởi tạo ReviewsController với service đánh giá sản phẩm.
    /// </summary>
    /// <param name="reviewService">Service xử lý logic nghiệp vụ đánh giá.</param>
    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    /// <summary>
    /// Lấy danh sách đánh giá công khai của một sản phẩm (cho phép khách vãng lai và khách hàng xem).
    /// Hỗ trợ lọc theo số sao, có hình ảnh, sắp xếp và phân trang.
    /// Endpoint: GET /api/reviews
    /// </summary>
    /// <param name="query">DTO chứa các tham số truy vấn: ProductId, Rating, HasImage, SortBy, PageNumber, PageSize.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Danh sách phân trang PaginatedResponse chứa các ReviewProductListDto.</returns>
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ReviewProductListDto>>> GetReviews(
        [FromQuery] ReviewQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await _reviewService.GetPublicListAsync(query, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Khách hàng gửi đánh giá mới cho sản phẩm đã mua và hoàn thành đơn hàng.
    /// Hỗ trợ tải lên tối đa 5 hình ảnh minh họa (Multipart Form-Data).
    /// Endpoint: POST /api/reviews
    /// </summary>
    /// <param name="dto">DTO chứa thông tin đánh giá: OrderDetailId, ProductId, Rating, Comment, Images (IFormFile).</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thông tin ReviewProductDto sau khi tạo (tự động phân loại kiểm duyệt APPROVED hoặc PENDING_APPROVAL).</returns>
    [Authorize(Roles = "Customer")]
    [HttpPost]
    public async Task<ActionResult<ReviewProductDto>> CreateReview(
        [FromForm] CreateReviewProductDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _reviewService.CreateReviewAsync(dto, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Khách hàng chỉnh sửa đánh giá của mình (ràng buộc nghiệp vụ: chỉ được sửa 1 lần duy nhất trong vòng 3 ngày kể từ khi tạo).
    /// Endpoint: PUT /api/reviews/{id}
    /// </summary>
    /// <param name="id">Mã ID của đánh giá cần chỉnh sửa.</param>
    /// <param name="dto">DTO chứa thông tin cập nhật: Rating, Comment, KeepImageIds (danh sách ID ảnh giữ lại), NewImages (ảnh mới).</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thông tin ReviewProductDto sau khi cập nhật.</returns>
    [Authorize(Roles = "Customer")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ReviewProductDto>> UpdateReview(
        int id,
        [FromForm] UpdateReviewProductDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _reviewService.UpdateReviewAsync(id, dto, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Lấy danh sách các sản phẩm từ các đơn hàng đã hoàn tất (COMPLETED) của khách hàng mà chưa được đánh giá.
    /// Endpoint: GET /api/reviews/unreviewed
    /// </summary>
    /// <param name="pageNumber">Số trang cần lấy (mặc định 1).</param>
    /// <param name="pageSize">Số bản ghi mỗi trang (mặc định 10).</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Danh sách phân trang UnreviewedProductDto.</returns>
    [Authorize(Roles = "Customer")]
    [HttpGet("unreviewed")]
    public async Task<ActionResult<PaginatedResponse<UnreviewedProductDto>>> GetUnreviewed(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _reviewService.GetUnreviewedProductsAsync(pageNumber, pageSize, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Lấy danh sách toàn bộ các đánh giá mà tài khoản khách hàng đang đăng nhập đã viết (kèm trạng thái kiểm duyệt).
    /// Endpoint: GET /api/reviews/me
    /// </summary>
    /// <param name="query">DTO lọc theo Rating, Status, khoảng thời gian CreatedFrom - CreatedTo, phân trang.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Danh sách phân trang MyReviewDto.</returns>
    [Authorize(Roles = "Customer")]
    [HttpGet("me")]
    public async Task<ActionResult<PaginatedResponse<MyReviewDto>>> GetMyReviews(
        [FromQuery] MyReviewQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var result = await _reviewService.GetMyReviewsAsync(query, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Khách hàng thực hiện Like hoặc Bỏ Like (Toggle) cho một đánh giá sản phẩm.
    /// Endpoint: POST /api/reviews/{id}/like
    /// </summary>
    /// <param name="id">Mã ID của đánh giá sản phẩm.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>DTO ReviewLikeResponseDto chứa trạng thái IsLiked hiện tại và tổng số LikeCount mới nhất.</returns>
    [Authorize(Roles = "Customer")]
    [HttpPost("{id:int}/like")]
    public async Task<ActionResult<ReviewLikeResponseDto>> ToggleLike(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _reviewService.ToggleLikeAsync(id, cancellationToken);
        return result.ToActionResult();
    }
}
