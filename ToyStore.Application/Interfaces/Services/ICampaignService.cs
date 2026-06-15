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
        bool viewerIsAdmin,
        int viewerAccountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay chi tiet Campaign theo ID, bao gom thong tin doi tuong nghiep vu da resolve.
    /// </summary>
    Task<Result<CampaignDto>> GetCampaignByIdAsync(
        int campaignId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay danh sach Delivery (nguoi nhan) theo CampaignID co phan trang.
    /// </summary>
    Task<Result<PaginatedResponse<CampaignDeliveryDto>>> GetCampaignDeliveriesAsync(
        int campaignId,
        int pageNumber,
        int pageSize,
        string? status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tao moi Campaign.
    /// </summary>
    Task<Result<CampaignDto>> CreateCampaignAsync(
        CreateCampaignDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cap nhat Campaign. Chi cho phep khi Status la Draft hoac Rejected.
    /// </summary>
    Task<Result<CampaignDto>> UpdateCampaignAsync(
        UpdateCampaignDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>Huy campaign (Draft, Approved, Scheduled). Staff: chi cua minh; Admin: tat ca.</summary>
    Task<Result> CancelCampaignAsync(
        int campaignId,
        int actorAccountId,
        bool actorIsAdmin,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tra ve danh sach cac loai doi tuong nghiep vu duoc ho tro cung voi cac placeholder tuong ung.
    /// </summary>
    Task<Result<List<ReferenceTypeDto>>> GetReferenceTypesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Staff gui Campaign de Admin xet duyet. Campaign phai dang o trang thai Draft.
    /// </summary>
    Task<Result> SubmitCampaignForReviewAsync(
        int campaignId,
        int accountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin duyet hoac tu choi Campaign. Campaign phai dang o trang thai PendingApproval.
    /// </summary>
    Task<Result> ReviewCampaignAsync(
        int campaignId,
        ReviewCampaignDto dto,
        int accountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Staff dat lich gui Campaign. Campaign phai duoc Admin duyet truoc (Status = Approved).
    /// </summary>
    Task<Result<ScheduleCampaignResultDto>> ScheduleCampaignAsync(
        int campaignId,
        ScheduleCampaignDto dto,
        int accountId,
        CancellationToken cancellationToken = default);

    /// <summary>Staff rut lui gui duyet (PendingApproval -> Draft).</summary>
    Task<Result> RecallCampaignAsync(
        int campaignId,
        int accountId,
        CancellationToken cancellationToken = default);

    /// <summary>Staff doi lich khi da Scheduled.</summary>
    Task<Result<ScheduleCampaignResultDto>> RescheduleCampaignAsync(
        int campaignId,
        RescheduleCampaignDto dto,
        int accountId,
        CancellationToken cancellationToken = default);

    /// <summary>Khung giờ gửi hợp lệ (UTC) để hiển thị trên form schedule.</summary>
    Task<Result<CampaignScheduleBoundsDto>> GetCampaignScheduleBoundsAsync(
        int campaignId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Xoa mem Campaign (IsDeleted = true). Chi duoc phep khi Status la "Sent" (da hoan thanh).
    /// Admin: xoa tat ca; Staff: chi xoa campaign cua minh.
    /// </summary>
    Task<Result> DeleteCampaignAsync(
        int campaignId,
        int actorAccountId,
        bool actorIsAdmin,
        CancellationToken cancellationToken = default);
}
