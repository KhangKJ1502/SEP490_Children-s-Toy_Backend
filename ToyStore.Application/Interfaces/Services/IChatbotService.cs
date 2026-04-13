using ToyStore.Application.DTOs;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Service interface for chatbot interactions.
/// </summary>
public interface IChatbotService
{
    /// <summary>
    /// Processes a chat message and returns response with toy suggestions.
    /// </summary>
    Task<ChatbotResponseDto> ProcessMessageAsync(
        ChatbotRequestDto request,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets toy suggestions based on criteria.
    /// </summary>
    Task<ChatbotResponseDto> GetToySuggestionsAsync(
        int childAge,
        decimal? budget,
        List<string>? interests,
        CancellationToken cancellationToken = default);
}
