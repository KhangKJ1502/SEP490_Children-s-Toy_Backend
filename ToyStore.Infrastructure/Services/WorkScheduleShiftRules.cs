using ToyStore.Application.Constants;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.Infrastructure.Services;

public class WorkScheduleShiftRules : IWorkScheduleShiftRules
{
    private readonly IUnitOfWork _unitOfWork;

    private static readonly byte StaffRoleId = 3;
    private static readonly byte MerchRoleId = 4;

    private static readonly TimeSpan MinimumRestBetweenEveningAndMorning = TimeSpan.FromHours(8);

    public WorkScheduleShiftRules(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> ValidateConsecutiveShiftAsync(int accountId, byte shiftTemplateId, DateTime workDate, CancellationToken cancellationToken = default)
    {
        if (shiftTemplateId != ShiftTemplateIds.Morning)
        {
            return true;
        }

        var morningTemplate = await _unitOfWork.ShiftTemplates.GetByIdAsync(ShiftTemplateIds.Morning, cancellationToken);
        var eveningTemplate = await _unitOfWork.ShiftTemplates.GetByIdAsync(ShiftTemplateIds.Evening, cancellationToken);

        if (morningTemplate is null || eveningTemplate is null)
        {
            return true;
        }

        var yesterday = workDate.Date.AddDays(-1);
        var schedulesYesterday = await _unitOfWork.WorkSchedules.GetByAccountAndDateAsync(accountId, yesterday, cancellationToken);

        var hasEveningYesterday = schedulesYesterday.Any(ws =>
            ws.ShiftTemplateId == ShiftTemplateIds.Evening
            && ws.Status != "Cancelled"
            && ws.Status != "Absent");

        if (!hasEveningYesterday)
        {
            return true;
        }

        var morningStartLocal = workDate.Date + morningTemplate.StartTime;
        var eveningEndLocal = yesterday + eveningTemplate.EndTime;
        var rest = morningStartLocal - eveningEndLocal;

        return rest >= MinimumRestBetweenEveningAndMorning;
    }

    public async Task<bool> ValidateMinimumCoverageAsync(
        DateTime workDate,
        byte shiftTemplateId,
        int? excludeScheduleId,
        int? accountIdForCreate,
        bool forCreate = false,
        bool forDelete = false,
        CancellationToken cancellationToken = default)
    {
        var staffCount = await _unitOfWork.WorkSchedules.CountActiveRoleOnShiftAsync(
            workDate, shiftTemplateId, StaffRoleId, excludeScheduleId, cancellationToken);

        var merchCount = await _unitOfWork.WorkSchedules.CountActiveRoleOnShiftAsync(
            workDate, shiftTemplateId, MerchRoleId, excludeScheduleId, cancellationToken);

        if (forCreate && accountIdForCreate.HasValue)
        {
            var account = await _unitOfWork.Accounts.GetByIdAsync(accountIdForCreate.Value, cancellationToken);
            if (account is not null && !account.IsDeleted && account.IsActive)
            {
                // Replenish missing role after absent/delete (e.g. Merch absent, Staff still on shift).
                if (account.RoleId == StaffRoleId && staffCount == 0 && merchCount >= 1)
                {
                    return true;
                }

                if (account.RoleId == MerchRoleId && merchCount == 0 && staffCount >= 1)
                {
                    return true;
                }
            }
        }

        if (accountIdForCreate.HasValue)
        {
            var account = await _unitOfWork.Accounts.GetByIdAsync(accountIdForCreate.Value, cancellationToken);
            if (account is not null && !account.IsDeleted && account.IsActive)
            {
                if (account.RoleId == StaffRoleId)
                {
                    staffCount++;
                }
                else if (account.RoleId == MerchRoleId)
                {
                    merchCount++;
                }
            }
        }

        if (staffCount >= 1 && merchCount >= 1)
        {
            return true;
        }

        // Admin UI creates Staff + Merch in two POSTs; first row may be the only role on the shift so far.
        if (forCreate && staffCount + merchCount >= 1)
        {
            return true;
        }

        // When deleting, allow the shift to become fully empty (0 staff on shift).
        // This unblocks removing the last assignee so the shift can be re-staffed later.
        if (forDelete)
        {
            return true;
        }

        return false;
    }
}
