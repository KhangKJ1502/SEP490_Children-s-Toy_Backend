using Microsoft.AspNetCore.Mvc;
using ToyStore.API.Extensions;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Campaigns;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CampaignsController : ControllerBase
{
    private readonly ICampaignService _campaignService;
    private readonly ILogger<CampaignsController> _logger;

    public CampaignsController(ICampaignService campaignService, ILogger<CampaignsController> logger)
    {
        _campaignService = campaignService;
        _logger          = logger;
    }

    /// <summary>
    /// Lay danh sach Campaign co phan trang voi cac tuy chon loc va sap xep nang cao.
    /// </summary>
    /// <param name="pageNumber">So trang (>=1, mac dinh 1).</param>
    /// <param name="pageSize">So ban ghi moi trang (1-100, mac dinh 10).</param>
    /// <param name="searchTerm">Tim kiem tren CampaignName, TemplateCode, EventKey.</param>
    /// <param name="status">Loc theo trang thai: Draft | Active | Completed.</param>
    /// <param name="sourceType">Loc theo kenh: Email | System | Push.</param>
    /// <param name="startDate">Ngay bat dau (CreatedAt >=, dinh dang yyyy-MM-dd).</param>
    /// <param name="endDate">Ngay ket thuc (CreatedAt <=, dinh dang yyyy-MM-dd).</param>
    /// <param name="sortBy">Truong sap xep: createdAt | name | status (mac dinh: createdAt).</param>
    /// <param name="sortDesc">True = giam dan, False = tang dan (mac dinh: false).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<CampaignListDto>>> GetCampaigns(
        [FromQuery] int pageNumber      = 1,
        [FromQuery] int pageSize        = 10,
        [FromQuery] string? searchTerm  = null,
        [FromQuery] string? status      = null,
        [FromQuery] string? sourceType  = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate   = null,
        [FromQuery] string? sortBy      = null,
        [FromQuery] bool sortDesc       = false,
        CancellationToken cancellationToken = default)
    {
        var query = new CampaignQueryDto
        {
            PageNumber = pageNumber,
            PageSize   = pageSize,
            SearchTerm = searchTerm,
            Status     = status,
            SourceType = sourceType,
            StartDate  = startDate,
            EndDate    = endDate,
            SortBy     = sortBy,
            SortDesc   = sortDesc
        };

        var result = await _campaignService.GetCampaignsAsync(query, cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>
    /// Lay chi tiet Campaign theo ID.
    /// </summary>
    /// <param name="campaignId">ID cua Campaign.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{campaignId:int}")]
    public async Task<ActionResult<CampaignDto>> GetCampaignById(
        [FromRoute] int campaignId,
        CancellationToken cancellationToken = default)
    {
        var result = await _campaignService.GetCampaignByIdAsync(campaignId, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Tao moi Campaign.
    /// </summary>
    /// <param name="dto">Du lieu tao Campaign.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    public async Task<ActionResult<CampaignDto>> CreateCampaign(
        [FromBody] CreateCampaignDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _campaignService.CreateCampaignAsync(dto, cancellationToken);
        return result.ToCreatedResult($"api/campaigns/{result.Data?.CampaignId}");
    }
}
