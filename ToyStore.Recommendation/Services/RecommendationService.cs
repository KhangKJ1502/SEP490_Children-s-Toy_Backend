using ToyStore.Application.DTOs;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Enums;
using ToyStore.Recommendation.Models;

namespace ToyStore.Recommendation.Services;

/// <summary>
/// Rule-based and score-based recommendation service implementation.
/// </summary>
public class RecommendationService : IRecommendationService
{
    private readonly IUnitOfWork _unitOfWork;
    
    public RecommendationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    /// <summary>
    /// Gets personalized recommendations based on user behavior history.
    /// </summary>
    public async Task<IReadOnlyList<RecommendationDto>> GetPersonalizedRecommendationsAsync(
        Guid userId,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        // Get user's behavior history
        var behaviors = await _unitOfWork.UserBehaviors.GetRecentBehaviorsAsync(userId, 100, cancellationToken);
        
        // Get user's preferred categories based on behavior
        var categoryPreferences = behaviors
            .GroupBy(b => b.Product?.ToyCategory)
            .Where(g => g.Key.HasValue)
            .OrderByDescending(g => g.Sum(b => GetBehaviorWeight(b.BehaviorType)))
            .Take(3)
            .Select(g => g.Key!.Value)
            .ToList();
            
        // Get user's preferred age ranges
        var agePreferences = behaviors
            .GroupBy(b => b.Product?.AgeRange)
            .Where(g => g.Key.HasValue)
            .OrderByDescending(g => g.Count())
            .Take(2)
            .Select(g => g.Key!.Value)
            .ToList();
            
        // Get products user has already interacted with
        var interactedProductIds = behaviors.Select(b => b.ProductId).Distinct().ToHashSet();
        
        // Get candidate products
        var (products, _) = await _unitOfWork.Products.GetPagedAsync(
            pageNumber: 1,
            pageSize: 100,
            cancellationToken: cancellationToken);
            
        // Score each product
        var scoredProducts = products
            .Where(p => !interactedProductIds.Contains(p.Id))
            .Select(p => ScoreProduct(p, categoryPreferences, agePreferences, behaviors))
            .OrderByDescending(s => s.Score)
            .Take(limit)
            .ToList();
            
        return scoredProducts.Select(MapToDto).ToList();
    }
    
    /// <summary>
    /// Gets recommendations based on specified criteria.
    /// </summary>
    public async Task<IReadOnlyList<RecommendationDto>> GetRecommendationsAsync(
        GetRecommendationsDto request,
        CancellationToken cancellationToken = default)
    {
        // If user ID provided, get personalized recommendations
        if (request.UserId.HasValue)
        {
            return await GetPersonalizedRecommendationsAsync(
                request.UserId.Value, 
                request.Limit, 
                cancellationToken);
        }
        
        // Otherwise, use rule-based recommendations
        var (products, _) = await _unitOfWork.Products.GetPagedAsync(
            pageNumber: 1,
            pageSize: 50,
            toyCategory: request.Categories?.FirstOrDefault(),
            ageRange: request.AgeRange,
            minPrice: request.MinPrice,
            maxPrice: request.MaxPrice,
            sortBy: "popularity",
            cancellationToken: cancellationToken);
            
        var recommendations = products.Take(request.Limit).Select(p => new RecommendationDto
        {
            ProductId = p.Id,
            ProductName = p.Name,
            Slug = p.Slug,
            Price = p.Price,
            SalePrice = p.SalePrice,
            ImageUrl = p.ImageUrl,
            CategoryName = p.Category?.Name ?? "",
            AgeRange = p.AgeRange,
            AverageRating = p.AverageRating,
            Score = CalculateBaseScore(p),
            Reason = GenerateReason(p, request)
        }).ToList();
        
        return recommendations;
    }
    
    /// <summary>
    /// Gets products similar to the specified product.
    /// </summary>
    public async Task<IReadOnlyList<RecommendationDto>> GetSimilarProductsAsync(
        Guid productId,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(productId, cancellationToken);
        if (product == null)
            return new List<RecommendationDto>();
            
        // Get products in same category and age range
        var (products, _) = await _unitOfWork.Products.GetPagedAsync(
            pageNumber: 1,
            pageSize: 50,
            toyCategory: product.ToyCategory,
            ageRange: product.AgeRange,
            cancellationToken: cancellationToken);
            
        var similar = products
            .Where(p => p.Id != productId)
            .OrderByDescending(p => CalculateSimilarityScore(product, p))
            .Take(limit)
            .Select(p => new RecommendationDto
            {
                ProductId = p.Id,
                ProductName = p.Name,
                Slug = p.Slug,
                Price = p.Price,
                SalePrice = p.SalePrice,
                ImageUrl = p.ImageUrl,
                CategoryName = p.Category?.Name ?? "",
                AgeRange = p.AgeRange,
                AverageRating = p.AverageRating,
                Score = CalculateSimilarityScore(product, p),
                Reason = "Similar to items you're viewing"
            })
            .ToList();
            
        return similar;
    }
    
    /// <summary>
    /// Gets products frequently bought together.
    /// </summary>
    public async Task<IReadOnlyList<RecommendationDto>> GetFrequentlyBoughtTogetherAsync(
        Guid productId,
        int limit = 5,
        CancellationToken cancellationToken = default)
    {
        // Get users who purchased this product
        var purchaseBehaviors = await _unitOfWork.UserBehaviors.GetByProductIdAsync(productId, cancellationToken);
        var purchaserIds = purchaseBehaviors
            .Where(b => b.BehaviorType == BehaviorType.Purchase)
            .Select(b => b.UserId)
            .Distinct()
            .ToList();
            
        // Get other products these users purchased
        var coProducts = new Dictionary<Guid, int>();
        foreach (var userId in purchaserIds.Take(50)) // Limit for performance
        {
            var userPurchases = await _unitOfWork.UserBehaviors.GetByTypeAsync(
                userId, BehaviorType.Purchase, cancellationToken);
                
            foreach (var purchase in userPurchases.Where(p => p.ProductId != productId))
            {
                if (!coProducts.ContainsKey(purchase.ProductId))
                    coProducts[purchase.ProductId] = 0;
                coProducts[purchase.ProductId]++;
            }
        }
        
        // Get top co-purchased products
        var topCoProductIds = coProducts
            .OrderByDescending(kvp => kvp.Value)
            .Take(limit)
            .Select(kvp => kvp.Key)
            .ToList();
            
        var recommendations = new List<RecommendationDto>();
        foreach (var coProductId in topCoProductIds)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(coProductId, cancellationToken);
            if (product != null)
            {
                recommendations.Add(new RecommendationDto
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Slug = product.Slug,
                    Price = product.Price,
                    SalePrice = product.SalePrice,
                    ImageUrl = product.ImageUrl,
                    CategoryName = product.Category?.Name ?? "",
                    AgeRange = product.AgeRange,
                    AverageRating = product.AverageRating,
                    Score = coProducts[coProductId] * 10,
                    Reason = "Frequently bought together"
                });
            }
        }
        
        return recommendations;
    }
    
    /// <summary>
    /// Tracks user behavior for improving recommendations.
    /// </summary>
    public async Task TrackBehaviorAsync(
        TrackBehaviorDto behavior,
        CancellationToken cancellationToken = default)
    {
        var userBehavior = new UserBehavior
        {
            UserId = behavior.UserId,
            ProductId = behavior.ProductId,
            BehaviorType = behavior.BehaviorType,
            SessionId = behavior.SessionId,
            DurationSeconds = behavior.DurationSeconds,
            Metadata = behavior.Metadata,
            OccurredAt = DateTime.UtcNow
        };
        
        await _unitOfWork.UserBehaviors.AddAsync(userBehavior, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        // Update product view/purchase counts
        var product = await _unitOfWork.Products.GetByIdAsync(behavior.ProductId, cancellationToken);
        if (product != null)
        {
            if (behavior.BehaviorType == BehaviorType.View)
                product.ViewCount++;
            else if (behavior.BehaviorType == BehaviorType.Purchase)
                product.PurchaseCount++;
                
            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
    
    /// <summary>
    /// Recalculates recommendation scores (called by background worker).
    /// </summary>
    public async Task RecalculateRecommendationScoresAsync(CancellationToken cancellationToken = default)
    {
        // This could update cached scores, pre-computed recommendations, etc.
        // For now, we recalculate product popularity metrics
        
        var mostViewed = await _unitOfWork.UserBehaviors.GetMostViewedProductsAsync(
            100, DateTime.UtcNow.AddDays(-30), cancellationToken);
            
        var mostPurchased = await _unitOfWork.UserBehaviors.GetMostPurchasedProductsAsync(
            100, DateTime.UtcNow.AddDays(-30), cancellationToken);
            
        // Update product metrics if needed
        // In a production system, you might store these in a cache or separate table
    }
    
    #region Private Helper Methods
    
    private static int GetBehaviorWeight(BehaviorType type)
    {
        return type switch
        {
            BehaviorType.Purchase => 10,
            BehaviorType.AddToCart => 5,
            BehaviorType.AddToWishlist => 4,
            BehaviorType.Rate => 3,
            BehaviorType.View => 1,
            _ => 1
        };
    }
    
    private RecommendationScore ScoreProduct(
        Product product,
        List<ToyCategory> categoryPreferences,
        List<AgeRange> agePreferences,
        IReadOnlyList<UserBehavior> behaviors)
    {
        var components = new ScoreComponents();
        
        // Category match score (0-20)
        if (categoryPreferences.Contains(product.ToyCategory))
        {
            var index = categoryPreferences.IndexOf(product.ToyCategory);
            components.CategoryMatchScore = 20 - (index * 5);
        }
        
        // Age range match score (0-15)
        if (agePreferences.Contains(product.AgeRange))
        {
            components.AgeRangeMatchScore = 15;
        }
        
        // Popularity score (0-10)
        components.PopularityScore = Math.Min(10, product.PurchaseCount / 10.0);
        
        // Rating score (0-10)
        if (product.AverageRating.HasValue)
        {
            components.RatingScore = (double)product.AverageRating.Value * 2;
        }
        
        var score = components.CalculateTotal();
        var reason = GenerateReasonFromComponents(components, product);
        
        return new RecommendationScore
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Score = score,
            Reason = reason,
            Components = components
        };
    }
    
    private static double CalculateBaseScore(Product product)
    {
        double score = 50; // Base score
        
        if (product.IsFeatured) score += 10;
        if (product.IsNewArrival) score += 5;
        if (product.AverageRating.HasValue)
            score += (double)product.AverageRating.Value * 5;
        score += Math.Min(15, product.PurchaseCount / 5.0);
        
        return Math.Min(100, score);
    }
    
    private static double CalculateSimilarityScore(Product source, Product target)
    {
        double score = 0;
        
        if (source.ToyCategory == target.ToyCategory) score += 30;
        if (source.AgeRange == target.AgeRange) score += 25;
        if (source.CategoryId == target.CategoryId) score += 20;
        if (source.Brand == target.Brand) score += 10;
        
        // Price similarity
        var priceDiff = Math.Abs(source.Price - target.Price);
        var priceScore = Math.Max(0, 15 - (double)(priceDiff / source.Price) * 15);
        score += priceScore;
        
        return score;
    }
    
    private static string GenerateReason(Product product, GetRecommendationsDto request)
    {
        if (request.AgeRange.HasValue && product.AgeRange == request.AgeRange.Value)
            return $"Perfect for {request.AgeRange.Value} age group";
            
        if (request.Categories?.Contains(product.ToyCategory) == true)
            return $"Matches your interest in {product.ToyCategory}";
            
        if (product.IsFeatured)
            return "Featured product";
            
        if (product.AverageRating.HasValue && product.AverageRating >= 4)
            return "Highly rated by customers";
            
        return "Popular choice";
    }
    
    private static string GenerateReasonFromComponents(ScoreComponents components, Product product)
    {
        var reasons = new List<string>();
        
        if (components.CategoryMatchScore >= 15)
            reasons.Add("matches your favorite categories");
        if (components.AgeRangeMatchScore >= 10)
            reasons.Add("suitable for your child's age");
        if (components.RatingScore >= 8)
            reasons.Add("highly rated");
        if (components.PopularityScore >= 5)
            reasons.Add("popular choice");
            
        if (reasons.Count == 0)
            return "Recommended for you";
            
        return "This " + string.Join(", ", reasons);
    }
    
    private RecommendationDto MapToDto(RecommendationScore score)
    {
        return new RecommendationDto
        {
            ProductId = score.ProductId,
            ProductName = score.ProductName,
            Score = score.Score,
            Reason = score.Reason
        };
    }
    
    #endregion
}
