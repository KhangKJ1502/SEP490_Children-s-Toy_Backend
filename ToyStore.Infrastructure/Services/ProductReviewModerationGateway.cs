using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Options;

namespace ToyStore.Infrastructure.Services;

/// <summary>
/// Triển khai Gateway gọi HTTP API tới AI Sidecar (dịch vụ Python chạy trên port riêng) để kiểm duyệt nội dung đánh giá sản phẩm tự động.
/// </summary>
public sealed class ProductReviewModerationGateway : IProductReviewModerationGateway
{
    private const string HttpClientName = "AI_MODERATION";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<AiModerationOptions> _options;
    private readonly ILogger<ProductReviewModerationGateway> _logger;

    /// <summary>
    /// Khởi tạo ProductReviewModerationGateway với cấu hình và HttpClientFactory.
    /// </summary>
    public ProductReviewModerationGateway(
        IHttpClientFactory httpClientFactory,
        IOptions<AiModerationOptions> options,
        ILogger<ProductReviewModerationGateway> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Gửi request HTTP POST tới AI Sidecar endpoint `/moderation/product-reviews/moderate-one` kèm internal API key để kiểm duyệt đánh giá theo ID.
    /// </summary>
    /// <param name="reviewId">Mã ID đánh giá cần kiểm duyệt.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>true nếu AI Sidecar tiếp nhận và xử lý thành công, false nếu tắt tính năng hoặc xảy ra lỗi.</returns>
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

    /// <summary>
    /// DTO gửi yêu cầu kiểm duyệt 1 đánh giá sang AI Sidecar.
    /// </summary>
    private sealed record ModerateProductReviewRequest(int ReviewId);

    /// <summary>
    /// DTO nhận kết quả phản hồi từ AI Sidecar.
    /// </summary>
    private sealed class ModerateOneResponse
    {
        public bool Accepted { get; set; }
        [JsonPropertyName("final_status")]
        public string? FinalStatus { get; set; }
    }
}
