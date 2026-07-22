using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToyStore.Application.DTOs.Blogs;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Options;

namespace ToyStore.Infrastructure.Services;

public sealed class BlogContentGenerationGateway : IBlogContentGenerationGateway
{
    private const string HttpClientName = "AI_MODERATION";
    private const string GenerateEndpoint = "/moderation/blog-content/generate";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<AiModerationOptions> _options;
    private readonly ILogger<BlogContentGenerationGateway> _logger;

    public BlogContentGenerationGateway(
        IHttpClientFactory httpClientFactory,
        IOptions<AiModerationOptions> options,
        ILogger<BlogContentGenerationGateway> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public async Task<BlogContentGenerationGatewayResult> GenerateAsync(
        PythonBlogGenerateRequest request,
        CancellationToken cancellationToken = default)
    {
        var options = _options.Value;
        if (!options.Enabled)
        {
            return BlogContentGenerationGatewayResult.ServiceUnavailable("AI generation is disabled.");
        }

        try
        {
            using var client = _httpClientFactory.CreateClient(HttpClientName);
            using var aiRequest = new HttpRequestMessage(HttpMethod.Post, GenerateEndpoint)
            {
                Content = JsonContent.Create(request, options: JsonOptions)
            };
            aiRequest.Headers.Add("X-Internal-Key", options.InternalApiKey);

            using var aiResponse = await client.SendAsync(aiRequest, cancellationToken);
            if (!aiResponse.IsSuccessStatusCode)
            {
                var errorBody = await aiResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "AI blog generate failed. Status={StatusCode}, Body={Body}",
                    aiResponse.StatusCode,
                    errorBody);

                return BlogContentGenerationGatewayResult.HttpError((int)aiResponse.StatusCode, errorBody);
            }

            var body = await aiResponse.Content.ReadFromJsonAsync<PythonBlogGenerateResponse>(
                JsonOptions,
                cancellationToken);

            return body == null
                ? BlogContentGenerationGatewayResult.ServiceUnavailable("AI returned an empty response.")
                : BlogContentGenerationGatewayResult.Success(body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI blog generate call threw exception.");
            return BlogContentGenerationGatewayResult.ServiceUnavailable("Unable to call AI service.");
        }
    }
}
