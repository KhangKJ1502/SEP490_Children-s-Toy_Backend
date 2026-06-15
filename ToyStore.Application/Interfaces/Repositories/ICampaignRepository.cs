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
        bool forAdminList = false,
        int viewerAccountId = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dem tong so Campaign theo dieu kien (dung voi query tuong tu GetPagedAsync).
    /// </summary>
    Task<int> CountAsync(
        CampaignQueryDto query,
        bool forAdminList = false,
        int viewerAccountId = 0,
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
    /// Tim Campaign theo ID cho cap nhat (tracking), kem targets, template, schedule, snapshots.
    /// </summary>
    Task<Campaign?> GetForUpdateAsync(int campaignId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay JobID bat ky (FK) dung khi lock CampaignSchedules.
    /// </summary>
    Task<int> GetBackgroundJobIdForCampaignLockAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomic lock: Waiting + chua lock -> Dispatched.
    /// </summary>
    Task<bool> TryAcquireDispatchLockAsync(
        int campaignId,
        int backgroundJobId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Recover stale Dispatched locks (job crash).
    /// </summary>
    Task<int> RecoverStaleDispatchLocksAsync(TimeSpan lockOlderThan, DateTime utcNow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Danh dau snapshot dang live la stale.
    /// </summary>
    Task MarkLiveReferenceSnapshotsStaleAsync(
        int campaignId,
        string reason,
        DateTime staleAt,
        CancellationToken cancellationToken = default);

    void AddReferenceSnapshot(CampaignReferenceSnapshot snapshot);

    /// <summary>
    /// Dispatch thanh cong: schedule Done, campaign Sent, stat.
    /// </summary>
    Task CompleteDispatchAsync(int campaignId, int totalSent, DateTime utcNow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dispatch loi: tang AttemptCount, Failed hoac Waiting+unlock.
    /// </summary>
    Task HandleDispatchFailureAsync(
        int campaignId,
        string error,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task SystemCancelWithAuditAsync(
        int campaignId,
        string approvalLogNote,
        int actorAccountId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task<List<int>> ListApprovedExpiredCampaignIdsAsync(DateTime utcNow, CancellationToken cancellationToken = default);

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

    Task AddCampaignScheduleLogAsync(CampaignScheduleLog log, CancellationToken cancellationToken = default);

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
    /// Recomputes CampaignStats from the Deliveries table (DISTINCT AccountId, WEB_BELL).
    /// Replaces incremental sent++ approach to avoid double-counting.
    /// </summary>
    Task RecomputeCampaignStatsAsync(int campaignId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk insert cac ban ghi Delivery.
    /// </summary>
    Task CreateDeliveriesAsync(List<Delivery> deliveries, CancellationToken cancellationToken = default);

    /// <summary>
    /// Danh dau trang thai entity Campaign la Modified trong DbContext (dung voi IUnitOfWork.SaveChangesAsync).
    /// </summary>
    void Update(Campaign campaign);

    /// <summary>
    /// Xoa mem Campaign (IsDeleted = true). Chi cho phep khi Status la Sent, Cancelled hoac Failed.
    /// Tra ve false neu khong tim thay hoac trang thai khong hop le.
    /// </summary>
    Task<bool> SoftDeleteAsync(int campaignId, CancellationToken cancellationToken = default);
}
