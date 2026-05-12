namespace ToyStore.Infrastructure.Options;

/// <summary>
/// Cấu hình SE_PAY / VietQR. Bind từ section "SePay" trong appsettings.
/// </summary>
public sealed class SePayOptions
{
    public const string SectionName = "SePay";

    /// <summary>API Key dùng để verify header Authorization: Apikey API_KEY_CUA_BAN từ webhook.</summary>
    public string ApiKey { get; set; } = string.Empty;

    // ── VietQR / thông tin tài khoản nhận tiền ──────────────────────────────

    /// <summary>BIN ngân hàng (vd: "970422" cho MB Bank).</summary>
    public string BankBin { get; set; } = string.Empty;

    /// <summary>Mã ngắn ngân hàng hiển thị (vd: "MBBank").</summary>
    public string BankCode { get; set; } = string.Empty;

    /// <summary>Số tài khoản nhận tiền.</summary>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>Tên chủ tài khoản (viết không dấu).</summary>
    public string AccountName { get; set; } = string.Empty;

    // ── Kiểm soát luồng ──────────────────────────────────────────────────────

    /// <summary>Số phút tối đa một đơn SE_PAY được phép chờ thanh toán. Mặc định 30.</summary>
    public int PaymentTtlMinutes { get; set; } = 30;

    /// <summary>Số lần retry QR tối đa mỗi đơn. Mặc định 10.</summary>
    public int MaxPaymentAttempts { get; set; } = 10;

    // ── Trọng lượng mặc định khi Products chưa có cột Weight ─────────────────

    /// <summary>Trọng lượng mặc định mỗi sản phẩm tính bằng gram. Mặc định 500.</summary>
    public int DefaultItemWeightGrams { get; set; } = 500;

    /// <summary>Ngưỡng chênh lệch phí ship giữa preview và confirm (đơn vị đồng). Mặc định 5000.</summary>
    public decimal ShippingFeeToleranceVnd { get; set; } = 5000m;
}
