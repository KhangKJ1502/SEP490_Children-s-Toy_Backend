namespace ToyStore.Application.DTOs.Campaigns;

/// <summary>
/// Metadata about a supported reference type — returned by GET /api/campaigns/reference-types
/// so the frontend knows which placeholders each type exposes.
/// </summary>
public class ReferenceTypeDto
{
    public string ReferenceType { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public List<PlaceholderInfoDto> Placeholders { get; set; } = new();
}

public class PlaceholderInfoDto
{
    public string Token { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Resolved data from a business object — embedded inside CampaignDto.
/// </summary>
public class ResolvedReferenceDto
{
    public string? DisplayName { get; set; }

    public string? ImageUrl { get; set; }

    /// <summary>For SALE references: e.g. FLASH_SALE, DISCOUNT — used by admin UI for schedule hints.</summary>
    public string? PromotionType { get; set; }

    public Dictionary<string, string> Placeholders { get; set; } = new();

    public string? DefaultActionTarget { get; set; }

    /// <summary>
    /// Chỉ khi <see cref="PromotionType"/> = FLASH_SALE: các khung giờ (UTC) để admin đối chiếu khi đặt lịch gửi.
    /// </summary>
    public List<ResolvedFlashTimeSlotDto>? FlashTimeSlots { get; set; }
}

/// <summary>
/// Một khung giờ flash sale (thời điểm lưu UTC trên server).
/// </summary>
public sealed class ResolvedFlashTimeSlotDto
{
    public int TimeSlotId { get; set; }

    public DateTime StartAtUtc { get; set; }

    public DateTime EndAtUtc { get; set; }

    public string Status { get; set; } = string.Empty;

    /// <summary>Sản phẩm gắn slot (FLASH_SALE).</summary>
    public List<ResolvedFlashProductLineDto> ProductLines { get; set; } = new();
}

/// <summary>Một dòng sản phẩm trong khung giờ flash.</summary>
public sealed class ResolvedFlashProductLineDto
{
    public int SlotProductId { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public decimal SalePrice { get; set; }

    public decimal? DiscountPercent { get; set; }

    public int SaleQuantity { get; set; }

    public int SoldQuantity { get; set; }

    public int ReservedQuantity { get; set; }

    public bool IsActive { get; set; }
}
