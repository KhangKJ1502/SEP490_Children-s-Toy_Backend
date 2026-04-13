using ToyStore.Application.DTOs;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Chatbot.Templates;
using ToyStore.Domain.Enums;

namespace ToyStore.Chatbot.Services;

/// <summary>
/// Chatbot service implementation for toy suggestions.
/// </summary>
public class ChatbotService : IChatbotService
{
    private readonly IRecommendationService _recommendationService;
    private readonly IProductService _productService;
    private readonly PromptBuilder _promptBuilder;
    
    public ChatbotService(
        IRecommendationService recommendationService,
        IProductService productService)
    {
        _recommendationService = recommendationService;
        _productService = productService;
        _promptBuilder = new PromptBuilder();
    }
    
    /// <summary>
    /// Processes a chat message and returns a response with toy suggestions.
    /// </summary>
    public async Task<ChatbotResponseDto> ProcessMessageAsync(
        ChatbotRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // Analyze the user's message
        var analysis = _promptBuilder.AnalyzeMessage(request.Message);
        
        // Merge with explicit request values
        var age = request.ChildAge ?? analysis.ExtractedAge;
        var budget = request.Budget ?? analysis.ExtractedBudget;
        var interests = request.Interests ?? analysis.ExtractedInterests;
        
        // Handle different intents
        return analysis.Intent switch
        {
            "greeting" => CreateGreetingResponse(),
            "goodbye" => CreateGoodbyeResponse(),
            _ => await CreateRecommendationResponse(
                age, budget, interests, request.UserId, cancellationToken)
        };
    }
    
    /// <summary>
    /// Gets toy suggestions based on age, interests, and budget.
    /// </summary>
    public async Task<ChatbotResponseDto> GetToySuggestionsAsync(
        int childAge,
        decimal? budget,
        List<string>? interests,
        CancellationToken cancellationToken = default)
    {
        return await CreateRecommendationResponse(
            childAge, budget, interests ?? new List<string>(), null, cancellationToken);
    }
    
    #region Private Helper Methods
    
    private ChatbotResponseDto CreateGreetingResponse()
    {
        return new ChatbotResponseDto
        {
            Message = PromptTemplates.GreetingTemplate,
            DetectedIntent = "greeting",
            Confidence = 1.0,
            FollowUpQuestions = new List<string>
            {
                "What's the age of the child you're shopping for?",
                "What are they interested in?",
                "Do you have a budget in mind?"
            }
        };
    }
    
    private ChatbotResponseDto CreateGoodbyeResponse()
    {
        return new ChatbotResponseDto
        {
            Message = PromptTemplates.GoodbyeTemplate,
            DetectedIntent = "goodbye",
            Confidence = 1.0
        };
    }
    
    private async Task<ChatbotResponseDto> CreateRecommendationResponse(
        int? age,
        decimal? budget,
        List<string> interests,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var response = new ChatbotResponseDto
        {
            DetectedIntent = "recommendation_request"
        };
        
        // Build recommendation request
        var recRequest = new GetRecommendationsDto
        {
            UserId = userId,
            AgeRange = MapAgeToAgeRange(age),
            Categories = MapInterestsToCategories(interests),
            MaxPrice = budget,
            Limit = 5
        };
        
        // Get recommendations
        var recommendations = await _recommendationService.GetRecommendationsAsync(
            recRequest, cancellationToken);
            
        // Convert to product list DTOs
        var products = new List<ProductListDto>();
        foreach (var rec in recommendations)
        {
            products.Add(new ProductListDto
            {
                Id = rec.ProductId,
                Name = rec.ProductName,
                Slug = rec.Slug,
                Price = rec.Price,
                SalePrice = rec.SalePrice,
                ImageUrl = rec.ImageUrl,
                CategoryName = rec.CategoryName,
                AgeRange = rec.AgeRange,
                AverageRating = rec.AverageRating,
                IsInStock = true
            });
        }
        
        response.SuggestedProducts = products;
        
        // Build response message
        if (products.Any())
        {
            response.Message = _promptBuilder.BuildRecommendationResponse(
                age, interests, budget, products);
            response.Confidence = 0.8;
        }
        else
        {
            response.Message = PromptTemplates.NoResultsTemplate
                .Replace("{criteria}", BuildCriteriaText(age, interests, budget))
                .Replace("{alternatives}", "Try broadening your search criteria.");
            response.Confidence = 0.5;
        }
        
        // Add follow-up questions if we're missing info
        var analysis = new ChatAnalysis
        {
            ExtractedAge = age,
            ExtractedBudget = budget,
            ExtractedInterests = interests
        };
        response.FollowUpQuestions = _promptBuilder.GenerateFollowUpQuestions(analysis);
        
        return response;
    }
    
    private static AgeRange? MapAgeToAgeRange(int? age)
    {
        if (!age.HasValue) return null;
        
        return age.Value switch
        {
            < 1 => AgeRange.Infant,
            < 3 => AgeRange.Toddler,
            < 6 => AgeRange.Preschool,
            < 9 => AgeRange.EarlyChildhood,
            < 13 => AgeRange.Kids,
            _ => AgeRange.Teens
        };
    }
    
    private static List<ToyCategory>? MapInterestsToCategories(List<string> interests)
    {
        if (!interests.Any()) return null;
        
        var mapping = new Dictionary<string, ToyCategory>
        {
            { "building", ToyCategory.Building },
            { "art", ToyCategory.ArtsCrafts },
            { "outdoor", ToyCategory.Outdoor },
            { "science", ToyCategory.Electronic },
            { "music", ToyCategory.Musical },
            { "dolls", ToyCategory.Dolls },
            { "action", ToyCategory.ActionFigures },
            { "puzzles", ToyCategory.Puzzles },
            { "vehicles", ToyCategory.Vehicles },
            { "animals", ToyCategory.Plush },
        };
        
        return interests
            .Where(i => mapping.ContainsKey(i.ToLower()))
            .Select(i => mapping[i.ToLower()])
            .ToList();
    }
    
    private static string BuildCriteriaText(int? age, List<string> interests, decimal? budget)
    {
        var parts = new List<string>();
        
        if (age.HasValue)
            parts.Add($"{age} year old");
        if (interests.Any())
            parts.Add($"interested in {string.Join(", ", interests)}");
        if (budget.HasValue)
            parts.Add($"budget of {budget:N0} VND");
            
        return parts.Any() ? string.Join(", ", parts) : "your criteria";
    }
    
    #endregion
}
