namespace ToyStore.Application.DTOs.Carts;

public static class CartHubEvents
{
    public const string CartUpdated = "CartUpdated";
    public const string CartItemAdded = "CartItemAdded";
    public const string CartItemRemoved = "CartItemRemoved";
    public const string CartQuantityChanged = "CartQuantityChanged";
    public const string CartPromotionChanged = "CartPromotionChanged";
    public const string CartPriceChanged = "CartPriceChanged";
    public const string CartOutOfStock = "CartOutOfStock";
}
