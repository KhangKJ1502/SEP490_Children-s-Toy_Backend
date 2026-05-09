using ToyStore.Domain.Entities;
using ToyStore.Application.DTOs.Campaigns;

namespace ToyStore.Application.Interfaces.Repositories;

/// <summary>
/// Repository thao tac du lieu Campaign.
/// </summary>
public interface ICampaignRepository
{
    /// <summary>
    /// Lay danh sach Campaign co phan trang, multi-field search, filter va sort.
    /// </summary>
    Task<List<Campaign>> GetPagedAsync(
        CampaignQueryDto query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dem tong so Campaign theo dieu kien (dung voi query tuong tu GetPagedAsync).
    /// </summary>
    Task<int> CountAsync(
        CampaignQueryDto query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tim Campaign theo ID, bao gom Stat va Targets.
    /// </summary>
    Task<Campaign?> GetByIdAsync(int campaignId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tim Campaign theo ID voi day du thong tin (bao gom Template, Targets, Stat).
    /// </summary>
    Task<Campaign?> GetByIdWithDetailsAsync(int campaignId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tim SYSTEM Campaign theo EventKey.
    /// </summary>
    Task<Campaign?> GetByEventKeyAsync(string eventKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiem tra ten Campaign da ton tai chua (case-insensitive, chi trong ban ghi chua xoa).
    /// Truyen excludeId de bo qua chinh no khi update.
    /// </summary>
    Task<bool> ExistsByNameAsync(string campaignName, int excludeId = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tao moi Campaign kem danh sach CampaignTarget.
    /// </summary>
    Task<Campaign> CreateAsync(
        CreateCampaignDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cap nhat Campaign (chi cho phep khi Status la Draft hoac Scheduled).
    /// Xoa va tao lai CampaignTargets.
    /// </summary>
    Task<Campaign> UpdateAsync(
        Campaign campaign,
        List<CreateCampaignTargetDto> newTargets,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Huy Campaign (chuyen Status sang Cancelled). Tra ve false neu khong tim thay.
    /// </summary>
    Task<bool> CancelAsync(int campaignId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay danh sach cac Campaign Scheduled da den gio gui (ScheduledAt le now).
    /// Bao gom Template va CampaignTargets.
    /// </summary>
    Task<List<Campaign>> GetDueCampaignsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cap nhat trang thai Campaign sang Sending.
    /// </summary>
    Task MarkSendingAsync(int campaignId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cap nhat trang thai Campaign sang Sent va upsert CampaignStat.
    /// </summary>
    Task MarkSentAsync(int campaignId, int totalSent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk insert cac ban ghi Delivery.
    /// </summary>
    Task CreateDeliveriesAsync(List<Delivery> deliveries, CancellationToken cancellationToken = default);
}
