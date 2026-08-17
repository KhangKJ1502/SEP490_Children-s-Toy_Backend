namespace ToyStore.Domain.Constants;

/// <summary>
/// Định nghĩa các hằng số trạng thái trong vòng đời của Voucher.
/// </summary>
public static class VoucherStatuses
{
    /// <summary>
    /// Đã được lên lịch, chờ đến StartDate để tự động chuyển sang Active.
    /// </summary>
    public const string Scheduled = "Scheduled";

    /// <summary>
    /// Đang hoạt động, khách hàng có thể áp dụng mã giảm giá khi mua hàng.
    /// </summary>
    public const string Active = "Active";

    /// <summary>
    /// Đang tạm ngưng / vô hiệu hóa thủ công bởi quản trị viên.
    /// </summary>
    public const string Inactive = "Inactive";

    /// <summary>
    /// Đã hết hạn (EndDate &lt; thời điểm hiện tại), không còn hiệu lực sử dụng.
    /// </summary>
    public const string Expired = "Expired";

    /// <summary>
    /// Đang chờ phê duyệt từ Admin (thường do Staff tạo hoặc sửa các trường tài chính).
    /// </summary>
    public const string Pending = "Pending";

    /// <summary>
    /// Bị Admin từ chối phê duyệt (kèm lý do trong Reason).
    /// </summary>
    public const string Rejected = "Rejected";
}
