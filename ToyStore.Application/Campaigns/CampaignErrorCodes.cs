namespace ToyStore.Application.Campaigns;

/// <summary>Machine-readable codes for campaign lifecycle (v3.3 spec).</summary>
public static class CampaignErrorCodes
{
    public const string SourceTypeInvalid = "SOURCE_TYPE_INVALID";
    public const string ContentRequired = "CONTENT_REQUIRED";
    public const string TemplateNotFound = "TEMPLATE_NOT_FOUND";
    public const string TargetRequired = "TARGET_REQUIRED";
    public const string TargetAccountInvalid = "TARGET_ACCOUNT_INVALID";
    public const string ReferenceInconsistent = "REFERENCE_INCONSISTENT";
    public const string ReferenceNotFound = "REFERENCE_NOT_FOUND";
    public const string NameRequired = "NAME_REQUIRED";
    public const string Forbidden = "FORBIDDEN";
    public const string InvalidStatusTransition = "INVALID_STATUS_TRANSITION";
    public const string ReferenceExpired = "REFERENCE_EXPIRED";
    public const string ScheduleNotAllowedAtSubmit = "SCHEDULE_NOT_ALLOWED_AT_SUBMIT";
    public const string ScheduleNotAllowedAtCreate = "SCHEDULE_NOT_ALLOWED_AT_CREATE";
    public const string ReviewerCannotBeSubmitter = "REVIEWER_CANNOT_BE_SUBMITTER";
    public const string ReferenceAlreadyExpired = "REFERENCE_ALREADY_EXPIRED";
    public const string TemplateDeactivated = "TEMPLATE_DEACTIVATED";
    public const string ReviewNoteRequired = "REVIEW_NOTE_REQUIRED";
    public const string ApprovedExpired = "APPROVED_EXPIRED";
    public const string ScheduledAtRequired = "SCHEDULED_AT_REQUIRED";
    public const string ScheduledAtTooSoon = "SCHEDULED_AT_TOO_SOON";
    public const string ScheduledAtTooFar = "SCHEDULED_AT_TOO_FAR";
    public const string ValidRangeInvalid = "VALID_RANGE_INVALID";
    public const string ScheduledAtOutOfRange = "SCHEDULED_AT_OUT_OF_RANGE";
    public const string ScheduledBeforeVoucherStart = "SCHEDULED_BEFORE_VOUCHER_START";
    public const string ScheduledTooCloseToVoucherEnd = "SCHEDULED_TOO_CLOSE_TO_VOUCHER_END";
    public const string WarnVoucherExpiringSoon = "WARN_VOUCHER_EXPIRING_SOON";
    public const string ScheduledTooEarlyForSale = "SCHEDULED_TOO_EARLY_FOR_SALE";
    public const string ScheduledAfterSlotStart = "SCHEDULED_AFTER_SLOT_START";
    public const string ScheduledTooCloseToSaleEnd = "SCHEDULED_TOO_CLOSE_TO_SALE_END";
    public const string ScheduledAfterLaunch = "SCHEDULED_AFTER_LAUNCH";
    public const string MaxRescheduleExceeded = "MAX_RESCHEDULE_EXCEEDED";
    public const string CampaignLockedByJob = "CAMPAIGN_LOCKED_BY_JOB";
    public const string SameScheduledAt = "SAME_SCHEDULED_AT";
    public const string ReasonTooLong = "REASON_TOO_LONG";

    private static readonly HashSet<string> DefinedCodes = new(StringComparer.Ordinal)
    {
        SourceTypeInvalid, ContentRequired, TemplateNotFound, TargetRequired, TargetAccountInvalid,
        ReferenceInconsistent, ReferenceNotFound, NameRequired, Forbidden, InvalidStatusTransition,
        ReferenceExpired, ScheduleNotAllowedAtSubmit, ScheduleNotAllowedAtCreate, ReviewerCannotBeSubmitter,
        ReferenceAlreadyExpired, TemplateDeactivated, ReviewNoteRequired, ApprovedExpired,
        ScheduledAtRequired, ScheduledAtTooSoon, ScheduledAtTooFar, ValidRangeInvalid, ScheduledAtOutOfRange,
        ScheduledBeforeVoucherStart, ScheduledTooCloseToVoucherEnd, WarnVoucherExpiringSoon,
        ScheduledTooEarlyForSale, ScheduledAfterSlotStart, ScheduledTooCloseToSaleEnd, ScheduledAfterLaunch,
        MaxRescheduleExceeded, CampaignLockedByJob, SameScheduledAt, ReasonTooLong
    };

    /// <summary>HTTP status for API when <paramref name="code"/> is a campaign v3.3 code; otherwise null.</summary>
    public static int? MapToHttpStatus(string? code)
    {
        if (string.IsNullOrEmpty(code) || !DefinedCodes.Contains(code)) return null;
        return code switch
        {
            TemplateNotFound or ReferenceNotFound or TargetAccountInvalid => 404,
            Forbidden => 403,
            _ => 422
        };
    }
}
