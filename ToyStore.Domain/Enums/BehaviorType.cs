namespace ToyStore.Domain.Enums;

/// <summary>
/// Types of user behavior for tracking and recommendations.
/// </summary>
public enum BehaviorType
{
    /// <summary>User viewed a product</summary>
    View = 0,
    
    /// <summary>User added product to cart</summary>
    AddToCart = 1,
    
    /// <summary>User added product to wishlist</summary>
    AddToWishlist = 2,
    
    /// <summary>User purchased a product</summary>
    Purchase = 3,
    
    /// <summary>User rated a product</summary>
    Rate = 4,
    
    /// <summary>User searched for a product</summary>
    Search = 5
}
