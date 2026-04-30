using ToyStore.Application.DTOs.Campaigns;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Resolver lay du lieu tu mot doi tuong nghiep vu cu the (Voucher, Product, BlogPost, Promotion)
/// va tra ve cac placeholder de render template thong bao.
/// </summary>
public interface IBusinessObjectResolver
{
    /// <summary>
    /// Loai doi tuong nghiep vu ma resolver nay xu ly: VOUCHER | PRODUCT | BLOG | SALE
    /// </summary>
    string ReferenceType { get; }

    /// <summary>
    /// Thong tin cac placeholder ma resolver nay cung cap — dung cho endpoint /reference-types.
    /// </summary>
    IReadOnlyList<PlaceholderInfoDto> AvailablePlaceholders { get; }

    /// <summary>
    /// Lay du lieu cua doi tuong theo ID va tra ve cac placeholder da dien vao.
    /// Tra ve null neu khong tim thay doi tuong.
    /// </summary>
    Task<ResolvedReferenceDto?> ResolveAsync(int referenceId, CancellationToken cancellationToken = default);
}
