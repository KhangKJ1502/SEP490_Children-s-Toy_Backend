using ToyStore.Domain.Enums;

namespace ToyStore.Application.DTOs;

/// <summary>
/// Chatbot request DTO.
/// </summary>
public class ChatbotRequestDto
{
    /// <summary>
    /// User's message/question.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional user ID for personalized suggestions.
    /// </summary>
    public Guid? UserId { get; set; }
    
    /// <summary>
    /// Child's age for recommendations.
    /// </summary>
    public int? ChildAge { get; set; }
    
    /// <summary>
    /// Budget constraint.
    /// </summary>
    public decimal? Budget { get; set; }
    
    /// <summary>
    /// Preferred categories.
    /// </summary>
    public List<ToyCategory>? PreferredCategories { get; set; }
    
    /// <summary>
    /// Child's interests (comma-separated or list).
    /// </summary>
    public List<string>? Interests { get; set; }
    
    /// <summary>
    /// Conversation context for multi-turn conversations.
    /// </summary>
    public List<ChatMessageDto>? ConversationHistory { get; set; }
}

/// <summary>
/// Chat message for conversation history.
/// </summary>
public class ChatMessageDto
{
    public string Role { get; set; } = string.Empty; // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Chatbot response DTO.
/// </summary>
public class ChatbotResponseDto
{
    /// <summary>
    /// Bot's text response.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Suggested products based on the conversation.
    /// </summary>
    public List<ProductListDto> SuggestedProducts { get; set; } = new();
    
    /// <summary>
    /// Follow-up questions to clarify user needs.
    /// </summary>
    public List<string> FollowUpQuestions { get; set; } = new();
    
    /// <summary>
    /// Detected intent from user message.
    /// </summary>
    public string? DetectedIntent { get; set; }
    
    /// <summary>
    /// Confidence score of the response (0-1).
    /// </summary>
    public double Confidence { get; set; }
}
