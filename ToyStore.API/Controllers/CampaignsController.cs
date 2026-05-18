using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Campaigns;
using ToyStore.Application.DTOs.Products;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CampaignsController : ControllerBase
{
    private readonly ICampaignService _campaignService;
    private readonly IImageUploadService _imageUploadService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CampaignsController> _logger;

    public CampaignsController(
        ICampaignService campaignService,
        IImageUploadService imageUploadService,
        ICurrentUserService currentUserService,
        ILogger<CampaignsController> logger)
    {
        _campaignService = campaignService;
        _imageUploadService = imageUploadService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Lay danh sach Campaign co phan trang voi cac tuy chon loc va sap xep.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<CampaignListDto>>> GetCampaigns(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? status = null,
        [FromQuery] string? sourceType = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false,
        CancellationToken cancellationToken = default)
    {
        var query = new CampaignQueryDto
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            Status = status,
            SourceType = sourceType,
            StartDate = startDate,
            EndDate = endDate,
            SortBy = sortBy,
            SortDesc = sortDesc
        };

        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var viewerIsAdmin = User.IsInRole("Admin");
        var result = await _campaignService.GetCampaignsAsync(query, viewerIsAdmin, accountId.Value, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Lay danh sach cac loai doi tuong nghiep vu duoc ho tro cung cac placeholder.
    /// Dung de frontend hien thi options khi admin tao/sua campaign.
    /// </summary>
    [HttpGet("reference-types")]
    public async Task<ActionResult<List<ReferenceTypeDto>>> GetReferenceTypes(
        CancellationToken cancellationToken = default)
    {
        var result = await _campaignService.GetReferenceTypesAsync(cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Lay chi tiet Campaign theo ID, bao gom thong tin doi tuong nghiep vu da resolve.
    /// </summary>
    [HttpGet("{campaignId:int}")]
    public async Task<ActionResult<CampaignDto>> GetCampaignById(
        [FromRoute] int campaignId,
        CancellationToken cancellationToken = default)
    {
        var result = await _campaignService.GetCampaignByIdAsync(campaignId, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Khung giờ gửi hợp lệ (UTC) cho form lên lịch — tính từ rule hệ thống và voucher/sale/product gắn vào.
    /// </summary>
    [HttpGet("{campaignId:int}/schedule-bounds")]
    public async Task<ActionResult<CampaignScheduleBoundsDto>> GetCampaignScheduleBounds(
        [FromRoute] int campaignId,
        CancellationToken cancellationToken = default)
    {
        var result = await _campaignService.GetCampaignScheduleBoundsAsync(campaignId, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Lay danh sach nguoi nhan (Deliveries) cua mot Campaign.
    /// </summary>
    [HttpGet("{campaignId:int}/deliveries")]
    public async Task<ActionResult<PaginatedResponse<CampaignDeliveryDto>>> GetCampaignDeliveries(
        [FromRoute] int campaignId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _campaignService.GetCampaignDeliveriesAsync(
            campaignId, pageNumber, pageSize, status, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Upload image for campaign content.
    /// </summary>
    [HttpPost("upload-image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<UploadImageResponseDto>> UploadImage(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file was provided." });
        }

        using var stream = file.OpenReadStream();
        var result = await _imageUploadService.UploadImageAsync(stream, file.FileName, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new UploadImageResponseDto { Url = result.Data! });
    }

    /// <summary>
    /// Tao moi Campaign (Status = Draft).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CampaignDto>> CreateCampaign(
        [FromBody] CreateCampaignDto dto,
        CancellationToken cancellationToken = default)
    {
        var accountId = GetAccountId();
        if (accountId.HasValue)
        {
            dto.CreatedByAccountId = accountId.Value;
        }

        var result = await _campaignService.CreateCampaignAsync(dto, cancellationToken);
        return result.ToCreatedResult($"api/campaigns/{result.Data?.CampaignId}");
    }

    /// <summary>
    /// Cap nhat Campaign. Chi cho phep khi Status la Draft hoac Rejected.
    /// </summary>
    [HttpPut("{campaignId:int}")]
    public async Task<ActionResult<CampaignDto>> UpdateCampaign(
        [FromRoute] int campaignId,
        [FromBody] UpdateCampaignDto dto,
        CancellationToken cancellationToken = default)
    {
        dto.CampaignId = campaignId;
        var result = await _campaignService.UpdateCampaignAsync(dto, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Huy Campaign.
    /// </summary>
    [HttpPost("{campaignId:int}/cancel")]
    public async Task<ActionResult> CancelCampaign(
        [FromRoute] int campaignId,
        CancellationToken cancellationToken = default)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var isAdmin = User.IsInRole("Admin");
        var result = await _campaignService.CancelCampaignAsync(
            campaignId, accountId.Value, isAdmin, cancellationToken);
        return result.ToNoContentResult();
    }

    /// <summary>
    /// Staff gui Campaign de Admin xet duyet. Campaign phai dang o trang thai Draft.
    /// </summary>
    [HttpPost("{campaignId:int}/submit")]
    public async Task<ActionResult> SubmitCampaign(
        [FromRoute] int campaignId,
        CancellationToken cancellationToken = default)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var result = await _campaignService.SubmitCampaignForReviewAsync(
            campaignId, accountId.Value, cancellationToken);
        return result.ToNoContentResult();
    }

    /// <summary>
    /// Admin duyet hoac tu choi Campaign. Campaign phai dang o trang thai PendingApproval.
    /// </summary>
    [HttpPost("{campaignId:int}/review")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ReviewCampaign(
        [FromRoute] int campaignId,
        [FromBody] ReviewCampaignDto dto,
        CancellationToken cancellationToken = default)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var result = await _campaignService.ReviewCampaignAsync(
            campaignId, dto, accountId.Value, cancellationToken);
        return result.ToNoContentResult();
    }

    /// <summary>
    /// Staff dat lich gui Campaign. Campaign phai duoc Admin duyet truoc (Status = Approved).
    /// </summary>
    [HttpPost("{campaignId:int}/schedule")]
    public async Task<ActionResult<ScheduleCampaignResultDto>> ScheduleCampaign(
        [FromRoute] int campaignId,
        [FromBody] ScheduleCampaignDto dto,
        CancellationToken cancellationToken = default)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var result = await _campaignService.ScheduleCampaignAsync(
            campaignId, dto, accountId.Value, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Staff rut lui khi dang cho duyet.</summary>
    [HttpPost("{campaignId:int}/recall")]
    public async Task<ActionResult> RecallCampaign(
        [FromRoute] int campaignId,
        CancellationToken cancellationToken = default)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var result = await _campaignService.RecallCampaignAsync(campaignId, accountId.Value, cancellationToken);
        return result.ToNoContentResult();
    }

    /// <summary>Doi lich khi da Scheduled.</summary>
    [HttpPost("{campaignId:int}/reschedule")]
    public async Task<ActionResult<ScheduleCampaignResultDto>> RescheduleCampaign(
        [FromRoute] int campaignId,
        [FromBody] RescheduleCampaignDto dto,
        CancellationToken cancellationToken = default)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var result = await _campaignService.RescheduleCampaignAsync(campaignId, dto, accountId.Value, cancellationToken);
        return result.ToActionResult();
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private int? GetAccountId()
    {
        var claim = User.FindFirst("AccountID")?.Value
                 ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
