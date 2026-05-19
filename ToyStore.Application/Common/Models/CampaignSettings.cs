using System;

namespace ToyStore.Application.Common.Models;

/// <summary>
/// Cấu hình thời gian cho các quy tắc vòng đời và lịch trình gửi Campaign.
/// </summary>
public class CampaignSettings
{
    /// <summary>
    /// Thời gian tối thiểu (phút) tính từ thời điểm hiện tại khi lên lịch/đổi lịch gửi Campaign (mặc định: 30 phút).
    /// Campaign phải được lên lịch gửi cách thời điểm hiện tại ít nhất khoảng thời gian này.
    /// </summary>
    public int MinLeadMinutes { get; set; } = 30;

    /// <summary>
    /// Thời gian tối đa (ngày) trong tương lai được phép lên lịch gửi Campaign (mặc định: 90 ngày).
    /// </summary>
    public int MaxFutureDays { get; set; } = 90;

    /// <summary>
    /// Khoảng thời gian đệm (giờ) trước khi Voucher/Khuyến mãi kết thúc mà Campaign phải được gửi xong (mặc định: 2 giờ).
    /// Tránh việc gửi Campaign quảng bá cho Voucher khi Voucher đó chỉ còn dưới 2 giờ là hết hiệu lực.
    /// </summary>
    public int VoucherEndBufferHours { get; set; } = 2;

    /// <summary>
    /// Khoảng thời gian (giờ) tối đa trước khi Khuyến mãi bắt đầu được phép lên lịch gửi Campaign (mặc định: 24 giờ).
    /// Ví dụ: Chỉ cho phép gửi thông báo khuyến mãi trước khi khuyến mãi chạy tối đa 24 giờ.
    /// </summary>
    public int SaleLeadWindowHours { get; set; } = 24;

    /// <summary>
    /// Khoảng thời gian (giờ) trước khi Voucher hết hạn để hệ thống đưa ra cảnh báo sắp hết hạn (mặc định: 24 giờ).
    /// </summary>
    public int VoucherSoonWarnHours { get; set; } = 24;
}
