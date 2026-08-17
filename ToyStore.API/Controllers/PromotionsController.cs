using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Promotions;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

/// <summary>
/// Controller cung cấp các API endpoint phục vụ quản lý và truy vấn Chương trình khuyến mãi (Promotion), Flash Sale.
/// Mặc định yêu cầu quyền Admin hoặc Staff, ngoại trừ endpoint Flash Sale công khai.
/// </summary>
[Authorize(Roles = "Admin,Staff")]
[ApiController]
[Route("api/[controller]")]
public class PromotionsController : ControllerBase
{
    private readonly IPromotionService _promotionService;
    private readonly ILogger<PromotionsController> _logger;

    /// <summary>
    /// Khởi tạo controller với các service phụ thuộc.
    /// </summary>
    /// <param name="promotionService">Service xử lý nghiệp vụ liên quan đến Promotion.</param>
    /// <param name="logger">Logger ghi log hoạt động của PromotionsController.</param>
    public PromotionsController(IPromotionService promotionService, ILogger<PromotionsController> logger)
    {
        _promotionService = promotionService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách các chương trình Flash Sale đang diễn ra (Active) hoặc sắp diễn ra (Scheduled).
    /// Endpoint công khai (Public) không yêu cầu xác thực đăng nhập để hiển thị trên trang chủ/banner.
    /// </summary>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Danh sách các chương trình Promotion kèm khung giờ (Time Slots) và sản phẩm Flash Sale.</returns>
    [AllowAnonymous]
    [HttpGet("flash-sale")]
    public async Task<ActionResult<List<PromotionDto>>> GetFlashSalePromotions(
        CancellationToken cancellationToken = default)
    {
        var result = await _promotionService.GetFlashSalePromotionsAsync(cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Lấy danh sách chương trình khuyến mãi có phân trang, tìm kiếm theo từ khóa, lọc trạng thái và sắp xếp.
    /// </summary>
    /// <param name="pageNumber">Số trang hiện tại (mặc định là 1).</param>
    /// <param name="pageSize">Số lượng bản ghi trên một trang (mặc định là 10).</param>
    /// <param name="sortBy">Trường cần sắp xếp (PromotionName, StartDate, EndDate, Status, CreatedAt,...).</param>
    /// <param name="sortDesc">true để sắp xếp giảm dần, false để tăng dần.</param>
    /// <param name="searchTerm">Từ khóa tìm kiếm theo tên hoặc mô tả chương trình.</param>
    /// <param name="status">Trạng thái khuyến mãi cần lọc (Scheduled, Active, Inactive, Expired).</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Danh sách phân trang chứa các PromotionListDto.</returns>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<PromotionListDto>>> GetPromotions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _promotionService.GetPromotionsAsync(
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            searchTerm,
            status,
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>
    /// Lấy thông tin chi tiết của một chương trình khuyến mãi theo ID (kèm danh sách sản phẩm hoặc khung giờ flash sale).
    /// </summary>
    /// <param name="promotionId">Mã định danh duy nhất (ID) của chương trình khuyến mãi.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thông tin chi tiết PromotionDto hoặc 404 NotFound nếu không tìm thấy.</returns>
    [HttpGet("{promotionId:int}")]
    public async Task<ActionResult<PromotionDto>> GetPromotionById(
        int promotionId,
        CancellationToken cancellationToken = default)
    {
        var result = await _promotionService.GetPromotionByIdAsync(promotionId, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Tạo mới một chương trình khuyến mãi (hỗ trợ khuyến mãi thông thường hoặc Flash Sale theo khung giờ).
    /// </summary>
    /// <param name="request">DTO chứa thông tin tạo mới chương trình khuyến mãi.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thông tin chi tiết PromotionDto vừa tạo thành công (HTTP 201 Created).</returns>
    [HttpPost]
    public async Task<ActionResult<PromotionDto>> CreatePromotion(
        [FromBody] CreatePromotionDto request,
        CancellationToken cancellationToken = default)
    {
        var result = await _promotionService.CreatePromotionAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Created promotion {PromotionId} via API",
                result.Data!.PromotionId);

            return result.ToCreatedResult($"/api/promotions/{result.Data!.PromotionId}");
        }

        return result.ToActionResult();
    }

    /// <summary>
    /// Cập nhật thông tin chương trình khuyến mãi theo ID (hỗ trợ Partial Update và cập nhật cấu trúc danh sách sản phẩm/khung giờ).
    /// </summary>
    /// <param name="promotionId">Mã ID của chương trình khuyến mãi cần cập nhật.</param>
    /// <param name="request">DTO chứa các trường cần cập nhật.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Thông tin chi tiết PromotionDto sau khi đã cập nhật thành công.</returns>
    [HttpPut("{promotionId:int}")]
    public async Task<ActionResult<PromotionDto>> UpdatePromotion(
        int promotionId,
        [FromBody] UpdatePromotionDto request,
        CancellationToken cancellationToken = default)
    {
        var result = await _promotionService.UpdatePromotionAsync(promotionId, request, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Lấy danh sách các chương trình khuyến mãi đang áp dụng giảm giá trực tiếp cho một sản phẩm cụ thể.
    /// </summary>
    /// <param name="productId">Mã ID của sản phẩm cần tra cứu khuyến mãi.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Danh sách các ProductPromotionInfoDto đang áp dụng cho sản phẩm.</returns>
    [HttpGet("product/{productId:int}")]
    public async Task<ActionResult<List<ProductPromotionInfoDto>>> GetPromotionsByProductId(
        int productId,
        CancellationToken cancellationToken = default)
    {
        var result = await _promotionService.GetPromotionsByProductIdAsync(productId, cancellationToken);
        return result.ToActionResult();
    }
}
