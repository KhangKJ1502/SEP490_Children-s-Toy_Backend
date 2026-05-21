namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Business rules that require schedule queries (beyond simple property validation).
/// </summary>
public interface IWorkScheduleShiftRules
{
    /// <summary>
    /// True when Morning-after-Evening rest is at least 8 hours or rule does not apply.
    /// </summary>
    Task<bool> ValidateConsecutiveShiftAsync(int accountId, byte shiftTemplateId, DateTime workDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when shift would still have ≥1 Staff (sales) and ≥1 Merchandise (warehouse) with Status ≠ Absent.
    /// Pass <paramref name="excludeScheduleId"/> when simulating deletion of that schedule.
    /// Pass <paramref name="accountIdForCreate"/> when simulating insertion of one new schedule row for that account.
    /// When <paramref name="forCreate"/> is true, allows the first role on an empty shift or adding the missing role when the other is already on the shift.
    /// When <paramref name="forDelete"/> is true, allows delete if at least one active person remains (other role can be added back via create).
    /// </summary>
    Task<bool> ValidateMinimumCoverageAsync(
        DateTime workDate,
        byte shiftTemplateId,
        int? excludeScheduleId,
        int? accountIdForCreate,
        bool forCreate = false,
        bool forDelete = false,
        CancellationToken cancellationToken = default);
}
