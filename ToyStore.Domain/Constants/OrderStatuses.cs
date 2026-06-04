namespace ToyStore.Domain.Constants;

/// <summary>
/// Hang so StatusName khop voi bang StatusOrders trong CSDL.
/// Dung de tra cuu StatusID qua IOrderRepository.GetStatusMapAsync()
/// thay vi hardcode byte ID.
/// </summary>
public static class OrderStatuses
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Processing = "Processing";
    public const string Shipped = "Shipped";
    public const string Delivering = "Delivering";
    public const string Delivered = "Delivered";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
    public const string Refunded = "Refunded";
    public const string Returning = "Returning";
    public const string ReturnCompleted = "ReturnCompleted";
    public const string DeliveryFailed = "DeliveryFailed";
    public const string WaitingReturn = "WaitingReturn";
    public const string ReturnFailed = "ReturnFailed";
    public const string Lost = "Lost";
    public const string Damaged = "Damaged";

    /// <summary>
    /// Cac trang thai Staff co the thay duoc theo mac dinh (chua loc bo sung).
    /// </summary>
    public static readonly IReadOnlyCollection<string> StaffVisibleStatuses =
        [Pending, Confirmed];

    /// <summary>
    /// Cac trang thai Merchandise co the thay duoc theo mac dinh.
    /// </summary>
    public static readonly IReadOnlyCollection<string> MerchandiseVisibleStatuses =
        [Confirmed, Processing, Shipped];

    /// <summary>
    /// Admin thay tat ca — khong gioi han (empty = no filter).
    /// </summary>
    public static readonly IReadOnlyCollection<string> AdminVisibleStatuses = [];

    /// <summary>
    /// Trang thai hien thi tab "Da xong" (Staff/Merch) va dieu kien list completed.
    /// </summary>
    public static readonly IReadOnlyCollection<string> StaffMerchCompletedTabStatuses =
    [
        Completed, Cancelled, Refunded, Delivered,
        Delivering, DeliveryFailed, Returning, ReturnCompleted,
        WaitingReturn, ReturnFailed, Lost, Damaged
    ];

    /// <summary>Milestone xu ly cua Staff (OrderAssignments.RoleID = 3).</summary>
    public static readonly IReadOnlyCollection<string> StaffProcessedMilestoneStatuses = [Confirmed];

    /// <summary>Milestone xu ly cua Merchandise (OrderAssignments.RoleID = 4).</summary>
    public static readonly IReadOnlyCollection<string> MerchandiseProcessedMilestoneStatuses =
        [Processing, Shipped];

    public static IReadOnlyCollection<string> GetProcessedMilestoneStatuses(byte assignmentRoleId) =>
        assignmentRoleId == 3
            ? StaffProcessedMilestoneStatuses
            : MerchandiseProcessedMilestoneStatuses;

    /// <summary>
    /// Trang thai cho phep Staff/Admin huy don hang.
    /// </summary>
    public static readonly IReadOnlyCollection<string> CancellableStatuses =
        [Pending, Confirmed];

    /// <summary>
    /// From Shipped onward, prepaid refunds must use refund management (no auto wallet on cancel).
    /// </summary>
    public static bool PrepaidCancelRequiresManualRefund(byte statusId)
        => statusId >= 4;
}
