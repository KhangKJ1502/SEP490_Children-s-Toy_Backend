using Microsoft.EntityFrameworkCore;
using ToyStore.Application.DTOs.Campaigns;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Services.Resolvers;

public class VoucherResolver : IBusinessObjectResolver
{
    private readonly SEP490ToyStoreContext _context;

    public VoucherResolver(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    public string ReferenceType => "VOUCHER";

    public IReadOnlyList<PlaceholderInfoDto> AvailablePlaceholders =>
    [
        new() { Token = "{{VoucherCode}}",    Description = "Ma giam gia" },
        new() { Token = "{{DiscountValue}}",  Description = "Gia tri giam (so)" },
        new() { Token = "{{DiscountType}}",   Description = "Kieu giam gia (PERCENT / AMOUNT)" },
        new() { Token = "{{ExpiryDate}}",     Description = "Han su dung (dd/MM/yyyy)" },
        new() { Token = "{{VoucherName}}",    Description = "Ten voucher" }
    ];

    public async Task<ResolvedReferenceDto?> ResolveAsync(int referenceId, CancellationToken cancellationToken = default)
    {
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

        return new ResolvedReferenceDto
        {
            DisplayName = voucher.VoucherName,
            DefaultActionTarget = $"/vouchers/{voucher.VoucherCode}",
            Placeholders = new Dictionary<string, string>
            {
                ["{{VoucherCode}}"]   = voucher.VoucherCode,
                ["{{VoucherName}}"]   = voucher.VoucherName,
                ["{{DiscountValue}}"] = voucher.DiscountValue.ToString("N0"),
                ["{{DiscountType}}"]  = voucher.DiscountType,
                ["{{ExpiryDate}}"]    = voucher.EndDate.ToString("dd/MM/yyyy")
            }
        };
    }
}
