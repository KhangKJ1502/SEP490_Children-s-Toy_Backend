using Microsoft.EntityFrameworkCore;
using ToyStore.Application.DTOs.Campaigns;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Services.Resolvers;

/// <summary>
/// Service giải quyết thông tin tham chiếu Voucher (IBusinessObjectResolver) dùng để tự động điền các placeholder
/// (mã voucher, tên voucher, giá trị giảm, hạn dùng) trong các thông báo và chiến dịch email/marketing.
/// </summary>
public class VoucherResolver : IBusinessObjectResolver
{
    private readonly SEP490ToyStoreContext _context;

    /// <summary>
    /// Khởi tạo VoucherResolver với DbContext.
    /// </summary>
    /// <param name="context">DbContext kết nối cơ sở dữ liệu.</param>
    public VoucherResolver(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Loại đối tượng nghiệp vụ tham chiếu (Reference Type) được nhận diện là "VOUCHER".
    /// </summary>
    public string ReferenceType => "VOUCHER";

    /// <summary>
    /// Danh sách các placeholder được hỗ trợ cho Voucher trong các mẫu thông báo (template).
    /// </summary>
    public IReadOnlyList<PlaceholderInfoDto> AvailablePlaceholders =>
    [
        new() { Token = "{{VoucherCode}}",    Description = "Voucher code" },
        new() { Token = "{{DiscountValue}}",  Description = "Discount value (numeric)" },
        new() { Token = "{{DiscountType}}",   Description = "Discount type (PERCENT / AMOUNT)" },
        new() { Token = "{{ExpiryDate}}",     Description = "Expiry (dd/MM/yyyy HH:mm, VN time)" },
        new() { Token = "{{VoucherName}}",    Description = "Voucher name" }
    ];

    /// <summary>
    /// Giải quyết và trích xuất dữ liệu của Voucher theo ID để điền vào các placeholder trong mẫu thông báo.
    /// </summary>
    /// <param name="referenceId">Mã ID của Voucher cần phân giải.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Đối tượng ResolvedReferenceDto chứa tên hiển thị, link điều hướng và bảng tra cứu Placeholder, hoặc null nếu không tìm thấy.</returns>
    public async Task<ResolvedReferenceDto?> ResolveAsync(int referenceId, CancellationToken cancellationToken = default)
    {
        // Truy vấn thông tin cơ bản của voucher từ cơ sở dữ liệu
        var voucher = await _context.Vouchers
            .AsNoTracking()
            .Where(v => v.VoucherId == referenceId && !v.IsDeleted)
            .Select(v => new
            {
                v.VoucherCode,
                v.VoucherName,
                v.DiscountValue,
                v.DiscountType,
                v.EndDate
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (voucher is null) return null;

        // Trả về DTO chứa các placeholder đã được định dạng chuỗi sẵn sàng thay thế vào nội dung thông báo
        return new ResolvedReferenceDto
        {
            DisplayName = voucher.VoucherName,
            DefaultActionTarget = $"/profile/vouchers?code={voucher.VoucherCode}",
            Placeholders = new Dictionary<string, string>
            {
                ["{{VoucherCode}}"] = voucher.VoucherCode,
                ["{{VoucherName}}"] = voucher.VoucherName,
                ["{{DiscountValue}}"] = voucher.DiscountValue.ToString("N0"),
                ["{{DiscountType}}"] = voucher.DiscountType,
                ["{{ExpiryDate}}"] = ReferenceDisplayTime.FormatVietnamDateTime(voucher.EndDate)
            }
        };
    }
}
