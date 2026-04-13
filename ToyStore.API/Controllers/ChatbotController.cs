using Microsoft.AspNetCore.Mvc;
using ToyStore.Application.DTOs;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatbotController : ControllerBase
{
    private readonly IChatbotService _chatbotService;
    private readonly ILogger<ChatbotController> _logger;
    
    public ChatbotController(IChatbotService chatbotService, ILogger<ChatbotController> logger)
    {
        _chatbotService = chatbotService;
        _logger = logger;
    }
    
    /// <summary>
    /// Processes a chat message and returns toy suggestions.
    /// </summary>
    [HttpPost("chat")]
    public async Task<ActionResult<ApiResponse<ChatbotResponseDto>>> Chat(
        [FromBody] ChatbotRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _chatbotService.ProcessMessageAsync(request, cancellationToken);
            return Ok(ApiResponse<ChatbotResponseDto>.Ok(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chatbot message");
            return BadRequest(ApiResponse<ChatbotResponseDto>.Fail("Error processing your message"));
        }
    }
    
    /// <summary>
    /// Gets toy suggestions based on specific criteria.
    /// </summary>
    [HttpGet("suggest")]
    public async Task<ActionResult<ApiResponse<ChatbotResponseDto>>> Suggest(
        [FromQuery] int childAge,
        [FromQuery] decimal? budget = null,
        [FromQuery] string? interests = null,
        CancellationToken cancellationToken = default)
    {
        var interestList = string.IsNullOrWhiteSpace(interests) 
            ? new List<string>() 
            : interests.Split(',').Select(i => i.Trim()).ToList();
            
        var response = await _chatbotService.GetToySuggestionsAsync(
            childAge, budget, interestList, cancellationToken);
            
        return Ok(ApiResponse<ChatbotResponseDto>.Ok(response));
    }
}
