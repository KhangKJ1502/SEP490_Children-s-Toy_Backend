namespace ToyStore.Recommendation.Configuration;

/// <summary>
/// Bảng EventType — Weight (theo spec):
/// product_view +1.0; product_view_long +2.0; add_to_cart +3.0; add_to_wishlist +2.5;
/// purchase +5.0; search +0.5; category_browse +0.3; review_submit +4.0; remove_from_cart -1.0.
/// </summary>
public static class EventWeights
{
    public const string ProductView = "product_view";
    public const string ProductViewLong = "product_view_long";
    public const string AddToCart = "add_to_cart";
    public const string AddToWishlist = "add_to_wishlist";
    public const string Purchase = "purchase";
    public const string Search = "search";
    public const string CategoryBrowse = "category_browse";
    public const string ReviewSubmit = "review_submit";
    public const string RemoveFromCart = "remove_from_cart";

    /// <summary>Tập hợp tất cả EventType hợp lệ (dùng để validate input từ FE).</summary>
    public static readonly IReadOnlySet<string> AllowedEventTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ProductView, ProductViewLong, AddToCart, AddToWishlist,
        Purchase, Search, CategoryBrowse, ReviewSubmit, RemoveFromCart,
    };

    /// <summary>Lấy trọng số mặc định theo EventType. Trả về 0 nếu không thuộc danh sách.</summary>
    public static decimal GetWeight(string eventType)
    {
        if (string.IsNullOrWhiteSpace(eventType)) return 0m;

        return eventType.ToLowerInvariant() switch
        {
            ProductView => 1.0m,
            ProductViewLong => 2.0m,
            AddToCart => 3.0m,
            AddToWishlist => 2.5m,
            Purchase => 5.0m,
            Search => 0.5m,
            CategoryBrowse => 0.3m,
            ReviewSubmit => 4.0m,
            RemoveFromCart => -1.0m,
            _ => 0m,
        };
    }

    /// <summary>
    /// Time decay: weight × e^(-0.1 × số_ngày_trước).
    /// Ví dụ: event hôm nay nhân 1, event 7 ngày trước nhân ≈ 0.4966.
    /// </summary>
    public static decimal ApplyTimeDecay(decimal baseWeight, double daysAgo)
    {
        if (daysAgo < 0) daysAgo = 0; // an toàn — không xử lý event trong tương lai
        var decay = Math.Exp(-0.1 * daysAgo);
        return baseWeight * (decimal)decay;
    }
}
