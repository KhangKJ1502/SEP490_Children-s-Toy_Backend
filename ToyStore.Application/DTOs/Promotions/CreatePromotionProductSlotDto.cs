namespace ToyStore.Application.DTOs.Promotions;

/// <summary>
/// DTO tạo/cập nhật sản phẩm cho một time slot FLASH_SALE.
/// Gửi kèm trong CreatePromotionTimeSlotDto.
/// </summary>
public class CreatePromotionProductSlotDto
{
    public int ProductId { get; set; }

    /// <summary>Giá bán flash-sale. Phải lớn hơn 0.</summary>
    public decimal SalePrice { get; set; }

    /// <summary>% giảm giá. Nếu null thì hệ thống tự tính từ SalePrice vs giá gốc.</summary>
    public decimal? DiscountPercent { get; set; }

    /// <summary>Số lượng tối đa được bán trong slot. Bắt buộc, phải lớn hơn 0.</summary>
    public int SaleQuantity { get; set; }

}
