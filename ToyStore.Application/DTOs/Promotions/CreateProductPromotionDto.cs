namespace ToyStore.Application.DTOs.Promotions;

public class CreateProductPromotionDto
{
    public int ProductId { get; set; }

    public decimal SalePrice { get; set; }

    public decimal? DiscountPercent { get; set; }

    public int? SaleQuantity { get; set; }

}
