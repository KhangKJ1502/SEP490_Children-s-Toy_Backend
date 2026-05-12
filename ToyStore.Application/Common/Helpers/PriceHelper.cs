using ToyStore.Domain.Entities;

namespace ToyStore.Application.Common.Helpers;

public static class PriceHelper
{
    public static decimal ResolveCurrentPrice(Product product, DateTime now)
    {
        // 1. Flash Sale
        var activeFlashSale = GetActiveFlashSaleSlot(product, now);
        if (activeFlashSale != null)
        {
            return activeFlashSale.SalePrice;
        }

        // 2. Regular Promotion
        if (product.ProductPromotions != null)
        {
            var bestRegularPromotion = product.ProductPromotions
                .Where(pp => pp.IsActive
                             && pp.Promotion != null
                             && !pp.Promotion.IsDeleted
                             && (string.Equals(pp.Promotion.Status, "Active", StringComparison.OrdinalIgnoreCase)
                                 || string.Equals(pp.Promotion.Status, "Scheduled", StringComparison.OrdinalIgnoreCase))
                             && pp.Promotion.StartDate <= now
                             && pp.Promotion.EndDate >= now
                             && (!pp.SaleQuantity.HasValue || pp.SoldQuantity + pp.ReservedQuantity < pp.SaleQuantity.Value)
                             && IsPromotionSlotActive(pp.Promotion, now))
                .OrderByDescending(pp => pp.Promotion.Priority)
                .ThenBy(pp => pp.SalePrice)
                .FirstOrDefault();

            if (bestRegularPromotion != null)
            {
                return bestRegularPromotion.SalePrice;
            }
        }

        return product.Price;
    }


    public static PromotionProductSlot? GetActiveFlashSaleSlot(Product product, DateTime now)
    {
        if (product.PromotionProductSlots == null) return null;

        return product.PromotionProductSlots
            .Where(pps => pps.IsActive
                         && pps.TimeSlot != null
                         && string.Equals(pps.TimeSlot.Status, "Active", StringComparison.OrdinalIgnoreCase)
                         && pps.TimeSlot.StartAt <= now
                         && pps.TimeSlot.EndAt >= now
                         && pps.TimeSlot.Promotion != null
                         && !pps.TimeSlot.Promotion.IsDeleted
                         && (string.Equals(pps.TimeSlot.Promotion.Status, "Active", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(pps.TimeSlot.Promotion.Status, "Scheduled", StringComparison.OrdinalIgnoreCase))
                         && (pps.SoldQuantity + pps.ReservedQuantity < pps.SaleQuantity))
            .OrderByDescending(pps => pps.TimeSlot.Promotion.Priority)
            .ThenBy(pps => pps.SalePrice)
            .FirstOrDefault();
    }

    private static bool IsPromotionSlotActive(Promotion promotion, DateTime now)
    {
        // Nếu promotion không chia slot (ví dụ Discount thường chạy cả ngày) thì trả về true
        if (promotion.PromotionTimeSlots == null || promotion.PromotionTimeSlots.Count == 0)
        {
            return true;
        }

        // Kiểm tra xem có slot nào đang Active và bao phủ thời gian hiện tại không
        return promotion.PromotionTimeSlots.Any(slot =>
            string.Equals(slot.Status, "Active", StringComparison.OrdinalIgnoreCase)
            && slot.StartAt <= now
            && slot.EndAt >= now);
    }
}
