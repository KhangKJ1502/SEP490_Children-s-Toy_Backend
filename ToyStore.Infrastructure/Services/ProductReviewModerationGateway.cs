using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Options;

namespace ToyStore.Infrastructure.Services;

public sealed class ProductReviewModerationGateway : IProductReviewModerationGateway
{
    private const string HttpClientName = "AI_MODERATION";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<AiModerationOptions> _options;
    private readonly ILogger<ProductReviewModerationGateway> _logger;

    public ProductReviewModerationGateway(
        IHttpClientFactory httpClientFactory,
        IOptions<AiModerationOptions> options,
        ILogger<ProductReviewModerationGateway> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public async Task<bool> ModerateReviewAsync(int reviewId, CancellationToken cancellationToken = default)
    {
        var options = _options.Value;
        if (!options.Enabled || reviewId <= 0)
        {
            return false;
        }

        try
        {
            using var client = _httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Post, "/moderation/product-reviews/moderate-one")
            {
                Content = JsonContent.Create(new ModerateProductReviewRequest(reviewId))
            };
            request.Headers.Add("X-Internal-Key", options.InternalApiKey);

            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "AI product review moderation call failed for ReviewId {ReviewId}. StatusCode={StatusCode}",
                    reviewId,
                    response.StatusCode);
                return false;
            }

            var body = await response.Content.ReadFromJsonAsync<ModerateOneResponse>(cancellationToken: cancellationToken);
            _logger.LogInformation(
                "AI product review moderation responded for ReviewId {ReviewId}: Accepted={Accepted}, FinalStatus={FinalStatus}",
                reviewId,
                body?.Accepted,
                body?.FinalStatus);
            return body?.Accepted == true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "AI product review moderation call error for ReviewId {ReviewId}",
                reviewId);
            return false;
        }
    }

    private sealed record ModerateProductReviewRequest(int ReviewId);

    private sealed class ModerateOneResponse
    {
        public bool Accepted { get; set; }
        [JsonPropertyName("final_status")]
        public string? FinalStatus { get; set; }
    }
}
