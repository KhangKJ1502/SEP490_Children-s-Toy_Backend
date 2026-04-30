using ToyStore.Domain.Entities;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Campaigns;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Service quan ly Campaign.
/// </summary>
public interface ICampaignService
{
    /// <summary>
    /// Lay danh sach Campaign co phan trang, multi-field search, filter (status, sourceType,
    /// date range) va sort (createdAt, name, status).
    /// </summary>
    Task<Result<PaginatedResponse<CampaignListDto>>> GetCampaignsAsync(
        CampaignQueryDto query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay chi tiet Campaign theo ID.
    /// </summary>
    Task<Result<CampaignDto>> GetCampaignByIdAsync(
        int campaignId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tao moi Campaign.
    /// </summary>
    Task<Result<CampaignDto>> CreateCampaignAsync(
        CreateCampaignDto dto,
        CancellationToken cancellationToken = default);
}
