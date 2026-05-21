using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Options;

namespace ToyStore.Infrastructure.Services;

public sealed class BlogCommentModerationGateway : IBlogCommentModerationGateway
{
    private const string HttpClientName = "AI_MODERATION";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<AiModerationOptions> _options;
    private readonly ILogger<BlogCommentModerationGateway> _logger;

    public BlogCommentModerationGateway(
        IHttpClientFactory httpClientFactory,
        IOptions<AiModerationOptions> options,
        ILogger<BlogCommentModerationGateway> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public Task<bool> ModerateCommentAsync(int reviewBlogId, CancellationToken cancellationToken = default)
    {
        return ModerateAsync("Comment", reviewBlogId, cancellationToken);
    }

    public Task<bool> ModerateReplyAsync(int replyBlogId, CancellationToken cancellationToken = default)
    {
        return ModerateAsync("Reply", replyBlogId, cancellationToken);
    }

    private async Task<bool> ModerateAsync(string targetType, int targetId, CancellationToken cancellationToken)
    {
        var options = _options.Value;
        if (!options.Enabled || targetId <= 0)
        {
            return false;
        }

        try
        {
            using var client = _httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Post, "/moderation/blog-comments/moderate-one")
            {
                Content = JsonContent.Create(new ModerateOneRequest(targetType, targetId))
            };
            request.Headers.Add("X-Internal-Key", options.InternalApiKey);

            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "AI moderation call failed for {TargetType} {TargetId}. StatusCode={StatusCode}",
                    targetType,
                    targetId,
                    response.StatusCode);
                return false;
            }

            var body = await response.Content.ReadFromJsonAsync<ModerateOneResponse>(cancellationToken: cancellationToken);
            _logger.LogInformation(
                "AI moderation responded for {TargetType} {TargetId}: Accepted={Accepted}, FinalStatus={FinalStatus}",
                targetType,
                targetId,
                body?.Accepted,
                body?.FinalStatus);
            return body?.Accepted == true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "AI moderation call error for {TargetType} {TargetId}",
                targetType,
                targetId);
            return false;
        }
    }

    private sealed record ModerateOneRequest(string TargetType, int TargetId);

    private sealed class ModerateOneResponse
    {
        public bool Accepted { get; set; }
        [JsonPropertyName("final_status")]
        public string? FinalStatus { get; set; }
    }
}
