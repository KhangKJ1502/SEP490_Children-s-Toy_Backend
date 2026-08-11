using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToyStore.Application.DTOs.Blogs;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Options;

namespace ToyStore.Infrastructure.Services;

/// <summary>
/// Cổng kết nối (Gateway) gọi đến dịch vụ AI Generation bên ngoài để tự động tạo nội dung bài viết Blog.
/// </summary>
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

    /// <summary>
    /// Khởi tạo gateway tạo nội dung Blog bằng AI với HttpClientFactory, cấu hình AI Options và Logger.
    /// </summary>
    public BlogContentGenerationGateway(
        IHttpClientFactory httpClientFactory,
        IOptions<AiModerationOptions> options,
        ILogger<BlogContentGenerationGateway> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Gửi yêu cầu sinh nội dung Blog tự động bằng AI (HTTP POST đến AI Generation API).
    /// </summary>
    public async Task<BlogContentGenerationGatewayResult> GenerateAsync(
        PythonBlogGenerateRequest request,
        CancellationToken cancellationToken = default)
    {
        var options = _options.Value;
        
        // KIỂM TRA 1: Nếu cấu hình AI Generation bị tắt (Enabled = false)
        if (!options.Enabled)
        {
            // Trả về kết quả dịch vụ không khả dụng
            return BlogContentGenerationGatewayResult.ServiceUnavailable("AI generation is disabled.");
        }

        try
        {
            // 1. Tạo HTTP Client từ Factory theo cấu hình định danh dành riêng cho AI_MODERATION
            using var client = _httpClientFactory.CreateClient(HttpClientName);
            
            // 2. Tạo yêu cầu POST đến endpoint sinh nội dung bài viết Blog
            using var aiRequest = new HttpRequestMessage(HttpMethod.Post, GenerateEndpoint)
            {
                // Truyền thân yêu cầu chứa tham số đầu vào (tiêu đề, từ khóa, ngôn ngữ...) dạng JSON
                Content = JsonContent.Create(request, options: JsonOptions)
            };
            
            // 3. Thêm mã khóa API nội bộ (Internal Api Key) vào Header để xác thực yêu cầu
            aiRequest.Headers.Add("X-Internal-Key", options.InternalApiKey);

            // 4. Gửi yêu cầu HTTP đến AI Generation API
            using var aiResponse = await client.SendAsync(aiRequest, cancellationToken);
            
            // KIỂM TRA 2: Kiểm tra phản hồi HTTP thành công
            if (!aiResponse.IsSuccessStatusCode)
            {
                // Nếu lỗi, đọc nội dung lỗi thô từ Server trả về
                var errorBody = await aiResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "AI blog generate failed. Status={StatusCode}, Body={Body}",
                    aiResponse.StatusCode,
                    errorBody);

                // Trả về lỗi kèm mã HTTP và nội dung lỗi tương ứng
                return BlogContentGenerationGatewayResult.HttpError((int)aiResponse.StatusCode, errorBody);
            }

            // 5. Đọc phản hồi kết quả nội dung sinh ra từ AI dạng JSON
            var body = await aiResponse.Content.ReadFromJsonAsync<PythonBlogGenerateResponse>(
                JsonOptions,
                cancellationToken);

            // Trả về thành công nếu phản hồi hợp lệ, ngược lại báo dịch vụ không phản hồi dữ liệu
            return body == null
                ? BlogContentGenerationGatewayResult.ServiceUnavailable("AI returned an empty response.")
                : BlogContentGenerationGatewayResult.Success(body);
        }
        catch (Exception ex)
        {
            // Ghi log cảnh báo lỗi kết nối hoặc lỗi xử lý hệ thống ngoại vi
            _logger.LogWarning(ex, "AI blog generate call threw exception.");
            return BlogContentGenerationGatewayResult.ServiceUnavailable("Unable to call AI service.");
        }
    }
}
