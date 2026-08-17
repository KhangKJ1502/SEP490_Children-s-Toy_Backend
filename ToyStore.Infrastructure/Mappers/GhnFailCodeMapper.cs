using System.Collections.Generic;

namespace ToyStore.Infrastructure.Mappers;

public static class GhnFailCodeMapper
{
    private static readonly HashSet<string> BlacklistCodes = new()
    {
        "GHN-DFC1A2",  // Chặn số
        "GHN-DCD1A5",  // Không có tiền
        "GHN-DCD1A1",  // Báo không đặt hàng (nghi đơn bơm)
        "GHN-DFC1A7",
        "GHN-DCD0A8",
    };

    private static readonly HashSet<string> RefundableCodes = new()
    {
        "GHN-DCD0A6",  // Sai sản phẩm
        "GHN-DCD0A7",  // Sai COD
        "GHN-DCD0A5",  // Hàng hư hỏng
        "GHN-DCD1A3",  // Bể vỡ trong vận chuyển
        "GHN-RFE0A4",  // Hàng hư hỏng khi trả về
    };

    private static readonly HashSet<string> RetryDeliveryCodes = new()
    {
        "GHN-DFC1A0",  // Hẹn lại ngày
        "GHN-DFC1A1",  // Đổi địa chỉ
        "GHN-DCD0A1",  // Sai địa chỉ/SĐT
        "GHN-DFC1A4",  // Không nghe máy (retry tối đa 2 lần)
    };

    private static readonly HashSet<string> GhnFaultCodes = new()
    {
        "GHN-DCD0A5",  // Hàng hư hỏng
        "GHN-DCD1A3",  // Bể vỡ
        "GHN-RFE0A4",  // Hư hỏng khi trả
        "GHN-DFC1A6",  // Nhân viên sự cố
        "GHN-PFA3A2",  // Nhân viên sự cố khi lấy
        "GHN-RFE0A5",  // Nhân viên sự cố khi trả
    };

    /// <summary>
    /// 10 mã lỗi Lấy hàng thất bại của GHN.
    /// GHN gửi Status="ready_to_pick" kèm một trong các mã này khi shipper không lấy được hàng.
    /// </summary>
    private static readonly HashSet<string> PickFailCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "GHN-PFA1A0",  // Người gửi hẹn lại ngày lấy hàng
        "GHN-PFA2A2",  // Thông tin lấy hàng sai (địa chỉ / SĐT)
        "GHN-PFA2A1",  // Thuê bao không liên lạc được / Máy bận
        "GHN-PFA2A3",  // Người gửi không nghe máy
        "GHN-PFA1A1",  // Người gửi muốn gửi hàng tại bưu cục
        "GHN-PCB0B2",  // Hàng vi phạm quy định khối lượng, kích thước
        "GHN-PFA4A1",  // Hàng vi phạm quy cách đóng gói
        "GHN-PCB0B1",  // Người gửi không muốn gửi hàng nữa
        "GHN-PFA4A2",  // Hàng hóa GHN không vận chuyển
        "GHN-PFA3A2",  // Nhân viên lấy hàng gặp sự cố
    };

    public static bool ShouldBlacklist(string failCode)
        => !string.IsNullOrEmpty(failCode) && BlacklistCodes.Contains(failCode);

    public static bool IsRefundable(string failCode)
        => !string.IsNullOrEmpty(failCode) && RefundableCodes.Contains(failCode);

    public static bool CanRetryDelivery(string failCode)
        => !string.IsNullOrEmpty(failCode) && RetryDeliveryCodes.Contains(failCode);

    public static bool IsGhnFault(string failCode)
        => !string.IsNullOrEmpty(failCode) && GhnFaultCodes.Contains(failCode);

    /// <summary>
    /// Trả về true nếu đây là mã lỗi Lấy hàng thất bại của GHN (GHN-PFA... / GHN-PCB...).
    /// Dùng để phân biệt webhook "ready_to_pick" thông thường với "ready_to_pick sau khi lấy thất bại".
    /// </summary>
    public static bool IsPickFail(string? failCode)
        => !string.IsNullOrWhiteSpace(failCode) && PickFailCodes.Contains(failCode.Trim());

    public static string BuildFailMessage(string orderCode, string? failCode, string? defaultReason)
    {
        var detail = failCode switch
        {
            // Lấy thất bại
            "GHN-PFA1A0" => "Người gửi hẹn lại ngày lấy hàng",
            "GHN-PFA2A2" => "Thông tin lấy hàng sai (địa chỉ / SĐT)",
            "GHN-PFA2A1" => "Thuê bao người gửi không liên lạc được / Máy bận",
            "GHN-PFA2A3" => "Người gửi không nghe máy",
            "GHN-PFA1A1" => "Người gửi muốn gửi hàng tại bưu cục",
            "GHN-PCB0B2" => "Hàng vi phạm quy định khối lượng, kích thước",
            "GHN-PFA4A1" => "Hàng vi phạm quy cách đóng gói",
            "GHN-PCB0B1" => "Người gửi không muốn gửi hàng nữa",
            "GHN-PFA4A2" => "Hàng hóa GHN không vận chuyển",
            "GHN-PFA3A2" => "Nhân viên lấy hàng gặp sự cố",

            // Giao thất bại
            "GHN-DFC1A0" => "Người nhận hẹn lại ngày giao",
            "GHN-DFC1A2" => "Không liên lạc được người nhận / Số điện thoại chặn cuộc gọi",
            "GHN-DFC1A4" => "Người nhận không nghe máy",
            "GHN-DCD0A1" => "Sai thông tin người nhận (địa chỉ / SĐT)",
            "GHN-DFC1A1" => "Người nhận đổi địa chỉ giao hàng",
            "GHN-DFC1A7" => "Người nhận từ chối nhận do không cho xem / thử hàng",
            "GHN-DCD0A6" => "Người nhận từ chối nhận do sai sản phẩm",
            "GHN-DCD0A7" => "Người nhận từ chối nhận do sai số tiền COD",
            "GHN-DCD0A5" => "Người nhận từ chối nhận do hàng hóa hư hỏng",
            "GHN-DCD1A5" => "Người nhận từ chối nhận do không có tiền",
            "GHN-DCD0A8" => "Người nhận đổi ý không mua nữa",
            "GHN-DCD1A1" => "Người nhận báo không đặt hàng (nghi vấn đơn ảo)",
            "GHN-DFC1A6" => "Nhân viên giao hàng gặp sự cố",
            "GHN-DCD1A3" => "Hàng suy suyển, bể vỡ trong quá trình vận chuyển",

            // Trả thất bại
            "GHN-RFE0A0" => "Người gửi hẹn lại ngày trả hàng",
            "GHN-RFE0A1" => "Người gửi đổi địa chỉ trả hàng",
            "GHN-RFE0A6" => "Người gửi không nghe máy",
            "GHN-RFE0A3" => "Người gửi từ chối nhận lại do sai sản phẩm",
            "GHN-RFE0A4" => "Người gửi từ chối nhận lại do hàng hư hỏng",
            "GHN-RFE0A5" => "Nhân viên trả hàng gặp sự cố",

            _            => defaultReason ?? "Không có lý do chi tiết từ đơn vị vận chuyển"
        };

        return $"Đơn #{orderCode} gặp sự cố vận chuyển. Lý do: {detail}";
    }

    public static string GetFriendlyDescription(string? failCode, string? defaultReason = null)
    {
        if (string.IsNullOrWhiteSpace(failCode))
            return defaultReason ?? "Không có lý do chi tiết từ đơn vị vận chuyển";

        var normalizedCode = failCode.Trim().ToUpperInvariant();

        return normalizedCode switch
        {
            // Lấy thất bại
            "GHN-PFA1A0" => "Người gửi hẹn lại ngày lấy hàng",
            "GHN-PFA2A2" => "Thông tin lấy hàng sai (địa chỉ / SĐT)",
            "GHN-PFA2A1" => "Thuê bao người gửi không liên lạc được / Máy bận",
            "GHN-PFA2A3" => "Người gửi không nghe máy",
            "GHN-PFA1A1" => "Người gửi muốn gửi hàng tại bưu cục",
            "GHN-PCB0B2" => "Hàng vi phạm quy định khối lượng, kích thước",
            "GHN-PFA4A1" => "Hàng vi phạm quy cách đóng gói",
            "GHN-PCB0B1" => "Người gửi không muốn gửi hàng nữa",
            "GHN-PFA4A2" => "Hàng hóa GHN không vận chuyển",
            "GHN-PFA3A2" => "Nhân viên lấy hàng gặp sự cố",

            // Giao thất bại
            "GHN-DFC1A0" => "Người nhận hẹn lại ngày giao",
            "GHN-DFC1A2" => "Không liên lạc được người nhận / Số điện thoại chặn cuộc gọi",
            "GHN-DFC1A4" => "Người nhận không nghe máy",
            "GHN-DCD0A1" => "Sai thông tin người nhận (địa chỉ / SĐT)",
            "GHN-DFC1A1" => "Người nhận đổi địa chỉ giao hàng",
            "GHN-DFC1A7" => "Người nhận từ chối nhận do không cho xem / thử hàng",
            "GHN-DCD0A6" => "Người nhận từ chối nhận do sai sản phẩm",
            "GHN-DCD0A7" => "Người nhận từ chối nhận do sai số tiền COD",
            "GHN-DCD0A5" => "Người nhận từ chối nhận do hàng hóa hư hỏng",
            "GHN-DCD1A5" => "Người nhận từ chối nhận do không có tiền",
            "GHN-DCD0A8" => "Người nhận đổi ý không mua nữa",
            "GHN-DCD1A1" => "Người nhận báo không đặt hàng (nghi vấn đơn ảo)",
            "GHN-DFC1A6" => "Nhân viên giao hàng gặp sự cố",
            "GHN-DCD1A3" => "Hàng suy suyển, bể vỡ trong quá trình vận chuyển",

            // Trả thất bại
            "GHN-RFE0A0" => "Người gửi hẹn lại ngày trả hàng",
            "GHN-RFE0A1" => "Người gửi đổi địa chỉ trả hàng",
            "GHN-RFE0A6" => "Người gửi không nghe máy",
            "GHN-RFE0A3" => "Người gửi từ chối nhận lại do sai sản phẩm",
            "GHN-RFE0A4" => "Người gửi từ chối nhận lại do hàng hư hỏng",
            "GHN-RFE0A5" => "Nhân viên trả hàng gặp sự cố",

            _            => defaultReason ?? "Không có lý do chi tiết từ đơn vị vận chuyển"
        };
    }
}
