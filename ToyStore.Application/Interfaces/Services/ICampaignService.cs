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
    /// Lay danh sach Campaign co phan trang, multi-field search, filter va sort.
    /// </summary>
    Task<Result<PaginatedResponse<CampaignListDto>>> GetCampaignsAsync(
        CampaignQueryDto query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay chi tiet Campaign theo ID, bao gom thong tin doi tuong nghiep vu da resolve.
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

    /// <summary>
    /// Cap nhat Campaign. Chi cho phep khi Status la Draft hoac Scheduled.
    /// </summary>
    Task<Result<CampaignDto>> UpdateCampaignAsync(
        UpdateCampaignDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Huy Campaign. Chi cho phep khi Status la Draft hoac Scheduled.
    /// </summary>
    Task<Result> CancelCampaignAsync(
        int campaignId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tra ve danh sach cac loai doi tuong nghiep vu duoc ho tro cung voi cac placeholder tuong ung.
    /// </summary>
    Task<Result<List<ReferenceTypeDto>>> GetReferenceTypesAsync(
        CancellationToken cancellationToken = default);
}
