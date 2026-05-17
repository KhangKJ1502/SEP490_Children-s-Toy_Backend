using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs.Campaigns;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Centralized campaign lifecycle rules (v3.3).
/// </summary>
public interface ICampaignLifecycleRules
{
    Task<Result?> ValidateCreateDtoAsync(CreateCampaignDto dto, CancellationToken ct = default);

    Task<Result?> ValidateUpdateDtoAsync(UpdateCampaignDto dto, CancellationToken ct = default);

    Task<Result> ValidateSubmitAsync(Campaign campaign, int actorAccountId, CancellationToken ct = default);

    Task<Result> ValidateApproveAsync(
        Campaign campaign,
        int reviewerAccountId,
        bool reviewerIsAdmin = false,
        CancellationToken ct = default);

    Task<Result> ValidateRejectAsync(
        Campaign campaign,
        int reviewerAccountId,
        string? reviewNote,
        bool reviewerIsAdmin = false,
        CancellationToken ct = default);

    Task<Result> ValidateRecallAsync(Campaign campaign, int actorAccountId, CancellationToken ct = default);

    Task<Result> ValidateScheduleAsync(
        Campaign campaign,
        DateTime scheduledAtUtc,
        DateTime? validFromUtc,
        DateTime? validToUtc,
        IList<string> warningCodes,
        CancellationToken ct = default);

    Task<Result> ValidateRescheduleAsync(
        Campaign campaign,
        DateTime newScheduledAtUtc,
        string? reason,
        IList<string> warningCodes,
        CancellationToken ct = default);

    Task<Result> ValidateCancelAsync(Campaign campaign, int actorAccountId, bool actorIsAdmin, CancellationToken ct = default);

    bool ShouldSkipDispatch(Campaign campaign, DateTime nowUtc, out string? logReason);

    Task<Result> ValidateDispatchAsync(Campaign campaign, CancellationToken ct = default);

    /// <summary>Khung giờ gửi hợp lệ (UTC) cho form lên lịch admin — khớp logic ValidateScheduleAsync.</summary>
    Task<Result<CampaignScheduleBoundsDto>> GetScheduleSendWindowBoundsAsync(
        Campaign campaign,
        CancellationToken ct = default);

    Task<CampaignReferenceSnapshot?> BuildLiveReferenceSnapshotAsync(Campaign campaign, CancellationToken ct = default);
}
