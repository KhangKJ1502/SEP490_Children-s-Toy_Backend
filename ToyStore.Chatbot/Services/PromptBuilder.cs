using ToyStore.Application.DTOs;
using ToyStore.Domain.Enums;

namespace ToyStore.Chatbot.Services;

/// <summary>
/// Builds dynamic prompts from templates and user input.
/// </summary>
public class PromptBuilder
{
    /// <summary>
    /// Analyzes user message and extracts intent and entities.
    /// </summary>
    public ChatAnalysis AnalyzeMessage(string message)
    {
        var analysis = new ChatAnalysis
        {
            OriginalMessage = message,
            Intent = DetectIntent(message)
        };
        
        // Extract age from message
        analysis.ExtractedAge = ExtractAge(message);
        
        // Extract budget from message
        analysis.ExtractedBudget = ExtractBudget(message);
        
        // Extract interests/categories
        analysis.ExtractedInterests = ExtractInterests(message);
        
        // Determine confidence based on what we extracted
        analysis.Confidence = CalculateConfidence(analysis);
        
        return analysis;
    }
    
    /// <summary>
    /// Builds a recommendation response from template and products.
    /// </summary>
    public string BuildRecommendationResponse(
        int? age,
        List<string> interests,
        decimal? budget,
        List<ProductListDto> products)
    {
        var ageText = age.HasValue ? $"{age.Value}" : "your child";
        var interestsText = interests.Any() 
            ? string.Join(" and ", interests) 
            : "various activities";
        var budgetText = budget.HasValue 
            ? $" within your budget of {FormatCurrency(budget.Value)}" 
            : "";
            
        var productLines = products.Select(p => 
            Templates.PromptTemplates.ProductRecommendationTemplate
                .Replace("{name}", p.Name)
                .Replace("{price}", FormatCurrency(p.SalePrice ?? p.Price))
                .Replace("{age_range}", GetAgeRangeText(p.AgeRange))
                .Replace("{reason}", GetRecommendationReason(p))
        );
        
        return Templates.PromptTemplates.RecommendationTemplate
            .Replace("{age}", ageText)
            .Replace("{interests}", interestsText)
            .Replace("{budget_text}", budgetText)
            .Replace("{recommendations}", string.Join("\n", productLines));
    }
    
    /// <summary>
    /// Generates follow-up questions based on what information is missing.
    /// </summary>
    public List<string> GenerateFollowUpQuestions(ChatAnalysis analysis)
    {
        var questions = new List<string>();
        
        if (!analysis.ExtractedAge.HasValue)
            questions.Add("How old is the child you're shopping for?");
            
        if (!analysis.ExtractedInterests.Any())
            questions.Add("What are they interested in? (e.g., building, art, outdoor play)");
            
        if (!analysis.ExtractedBudget.HasValue)
            questions.Add("Do you have a budget in mind?");
            
        return questions;
    }
    
    #region Private Helper Methods
    
    private static string DetectIntent(string message)
    {
        var lowerMessage = message.ToLower();
        
        if (ContainsAny(lowerMessage, "hi", "hello", "hey", "start"))
            return "greeting";
            
        if (ContainsAny(lowerMessage, "bye", "goodbye", "thanks", "thank you", "done"))
            return "goodbye";
            
        if (ContainsAny(lowerMessage, "recommend", "suggest", "find", "looking for", "want", "need", "help"))
            return "recommendation_request";
            
        if (ContainsAny(lowerMessage, "year", "old", "month", "baby", "toddler", "kid", "teen"))
            return "age_info";
            
        if (ContainsAny(lowerMessage, "vnd", "dong", "budget", "price", "cost", "spend", "under", "around"))
            return "budget_info";
            
        if (ContainsAny(lowerMessage, "like", "love", "interest", "hobby", "enjoy", "play"))
            return "interest_info";
            
        return "general";
    }
    
    private static int? ExtractAge(string message)
    {
        var lowerMessage = message.ToLower();
        
        // Match patterns like "5 years", "5 year old", "5yo"
        var patterns = new[]
        {
            @"(\d+)\s*year",
            @"(\d+)\s*yr",
            @"(\d+)\s*yo",
            @"age\s*(\d+)",
            @"(\d+)\s*month",
        };
        
        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(lowerMessage, pattern);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int age))
            {
                // Convert months to years if needed
                if (pattern.Contains("month"))
                    return age < 12 ? 0 : age / 12;
                return age;
            }
        }
        
        // Check for keywords
        if (ContainsAny(lowerMessage, "baby", "infant"))
            return 0;
        if (ContainsAny(lowerMessage, "toddler"))
            return 2;
        if (ContainsAny(lowerMessage, "preschool", "preschooler"))
            return 4;
        if (ContainsAny(lowerMessage, "teenager", "teen"))
            return 13;
            
        return null;
    }
    
    private static decimal? ExtractBudget(string message)
    {
        var lowerMessage = message.ToLower();
        
        // Match patterns for Vietnamese Dong
        var patterns = new[]
        {
            @"(\d+(?:,\d{3})*(?:\.\d+)?)\s*(?:k|nghìn|nghin)",
            @"(\d+(?:,\d{3})*(?:\.\d+)?)\s*(?:vnd|đồng|dong|d)",
            @"(\d+(?:,\d{3})*)\s*(?:triệu|trieu|tr|m)",
            @"under\s*(\d+)",
            @"around\s*(\d+)",
            @"about\s*(\d+)",
        };
        
        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(lowerMessage, pattern);
            if (match.Success)
            {
                var valueStr = match.Groups[1].Value.Replace(",", "");
                if (decimal.TryParse(valueStr, out decimal value))
                {
                    // Handle multipliers
                    if (pattern.Contains("triệu") || pattern.Contains("trieu") || pattern.Contains("m"))
                        return value * 1_000_000;
                    if (pattern.Contains("k") || pattern.Contains("nghìn"))
                        return value * 1_000;
                    return value;
                }
            }
        }
        
        return null;
    }
    
    private static List<string> ExtractInterests(string message)
    {
        var lowerMessage = message.ToLower();
        var interests = new List<string>();
        
        var interestKeywords = new Dictionary<string, string[]>
        {
            { "building", new[] { "build", "lego", "block", "construct", "bricks" } },
            { "art", new[] { "art", "draw", "paint", "craft", "color", "creative" } },
            { "outdoor", new[] { "outdoor", "sport", "active", "run", "bike", "ball" } },
            { "science", new[] { "science", "experiment", "robot", "tech", "stem" } },
            { "music", new[] { "music", "instrument", "sing", "piano", "guitar" } },
            { "dolls", new[] { "doll", "barbie", "pretend", "roleplay" } },
            { "action", new[] { "action", "superhero", "hero", "marvel", "figure" } },
            { "puzzles", new[] { "puzzle", "brain", "logic", "strategy" } },
            { "vehicles", new[] { "car", "truck", "train", "vehicle", "rc", "remote" } },
            { "animals", new[] { "animal", "dinosaur", "pet", "plush", "stuffed" } },
        };
        
        foreach (var (interest, keywords) in interestKeywords)
        {
            if (keywords.Any(k => lowerMessage.Contains(k)))
                interests.Add(interest);
        }
        
        return interests;
    }
    
    private static double CalculateConfidence(ChatAnalysis analysis)
    {
        double confidence = 0.3; // Base confidence
        
        if (analysis.ExtractedAge.HasValue)
            confidence += 0.25;
        if (analysis.ExtractedInterests.Any())
            confidence += 0.25;
        if (analysis.ExtractedBudget.HasValue)
            confidence += 0.2;
            
        return Math.Min(1.0, confidence);
    }
    
    private static bool ContainsAny(string text, params string[] keywords)
    {
        return keywords.Any(k => text.Contains(k));
    }
    
    private static string FormatCurrency(decimal amount)
    {
        if (amount >= 1_000_000)
            return $"{amount / 1_000_000:N1}M VND";
        if (amount >= 1_000)
            return $"{amount / 1_000:N0}K VND";
        return $"{amount:N0} VND";
    }
    
    private static string GetAgeRangeText(AgeRange ageRange)
    {
        return ageRange switch
        {
            AgeRange.Infant => "0-12 months",
            AgeRange.Toddler => "1-3 years",
            AgeRange.Preschool => "3-5 years",
            AgeRange.EarlyChildhood => "6-8 years",
            AgeRange.Kids => "9-12 years",
            AgeRange.Teens => "13+ years",
            _ => "All ages"
        };
    }
    
    private static string GetRecommendationReason(ProductListDto product)
    {
        if (product.AverageRating.HasValue && product.AverageRating >= 4.5m)
            return "Highly rated by parents!";
        if (product.IsFeatured)
            return "Staff favorite!";
        return "Great value for money!";
    }
    
    #endregion
}

/// <summary>
/// Result of analyzing a chat message.
/// </summary>
public class ChatAnalysis
{
    public string OriginalMessage { get; set; } = string.Empty;
    public string Intent { get; set; } = string.Empty;
    public int? ExtractedAge { get; set; }
    public decimal? ExtractedBudget { get; set; }
    public List<string> ExtractedInterests { get; set; } = new();
    public double Confidence { get; set; }
}
