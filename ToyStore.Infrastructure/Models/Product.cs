using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public short CategoryId { get; set; }

    public short? BrandId { get; set; }

    public byte? PriceRangeId { get; set; }

    public string ProductName { get; set; } = null!;

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public string ProductStatus { get; set; } = null!;

    public DateTime? LaunchDate { get; set; }

    public bool IsDeleted { get; set; }

    public short StockThreshold { get; set; }

    public bool LowStockNotificationEnabled { get; set; }

    public DateTime? LastLowStockNotifiedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Brand? Brand { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<ItemSimilarity> ItemSimilaritySimilarProducts { get; set; } = new List<ItemSimilarity>();

    public virtual ICollection<ItemSimilarity> ItemSimilaritySourceProducts { get; set; } = new List<ItemSimilarity>();

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual PriceRange? PriceRange { get; set; }

    public virtual ProductDetail? ProductDetail { get; set; }

    public virtual ICollection<ProductFollower> ProductFollowers { get; set; } = new List<ProductFollower>();

    public virtual ProductImage? ProductImage { get; set; }

    public virtual ICollection<ProductPromotion> ProductPromotions { get; set; } = new List<ProductPromotion>();

    public virtual ICollection<ReviewProduct> ReviewProducts { get; set; } = new List<ReviewProduct>();

    public virtual ICollection<TrendingProduct> TrendingProducts { get; set; } = new List<TrendingProduct>();

    public virtual ICollection<UserProductScore> UserProductScores { get; set; } = new List<UserProductScore>();

    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
}
