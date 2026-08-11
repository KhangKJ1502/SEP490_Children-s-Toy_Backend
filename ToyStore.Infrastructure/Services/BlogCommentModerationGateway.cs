using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Options;

namespace ToyStore.Infrastructure.Services;

/// <summary>
/// Cổng kết nối (Gateway) gọi đến dịch vụ AI Moderation bên ngoài để tự động kiểm duyệt bình luận (Comments) và phản hồi (Replies) của bài viết Blog.
/// </summary>
public sealed class BlogCommentModerationGateway : IBlogCommentModerationGateway
{
    private const string HttpClientName = "AI_MODERATION";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<AiModerationOptions> _options;
    private readonly ILogger<BlogCommentModerationGateway> _logger;

    /// <summary>
    /// Khởi tạo gateway kiểm duyệt bình luận Blog với HttpClientFactory, cấu hình AI Moderation Options và Logger.
    /// </summary>
    public BlogCommentModerationGateway(
        IHttpClientFactory httpClientFactory,
        IOptions<AiModerationOptions> options,
        ILogger<BlogCommentModerationGateway> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Thực hiện kiểm duyệt tự động đối với một bình luận Blog (Review/Comment) cụ thể.
    /// </summary>
    public Task<bool> ModerateCommentAsync(int reviewBlogId, CancellationToken cancellationToken = default)
    {
        // Chuyển tiếp yêu cầu kiểm duyệt với đối tượng đích là "Comment"
        return ModerateAsync("Comment", reviewBlogId, cancellationToken);
    }

    /// <summary>
    /// Thực hiện kiểm duyệt tự động đối với một phản hồi bình luận Blog (Reply) cụ thể.
    /// </summary>
    public Task<bool> ModerateReplyAsync(int replyBlogId, CancellationToken cancellationToken = default)
    {
        // Chuyển tiếp yêu cầu kiểm duyệt với đối tượng đích là "Reply"
        return ModerateAsync("Reply", replyBlogId, cancellationToken);
    }

    /// <summary>
    /// Hàm dùng chung xử lý gửi yêu cầu kiểm duyệt HTTP POST đến Server AI Moderation.
    /// </summary>
    private async Task<bool> ModerateAsync(string targetType, int targetId, CancellationToken cancellationToken)
    {
        var options = _options.Value;
        
        // KIỂM TRA 1: Nếu tính năng kiểm duyệt AI bị tắt (Enabled = false) hoặc ID đối tượng không hợp lệ
        if (!options.Enabled || targetId <= 0)
        {
            return false; // Bỏ qua kiểm duyệt và trả về false
        }

        try
        {
            // 1. Khởi tạo HTTP Client từ Factory theo cấu hình định danh dành riêng cho AI_MODERATION
            using var client = _httpClientFactory.CreateClient(HttpClientName);
            
            // 2. Tạo yêu cầu POST đến endpoint kiểm duyệt một phần tử bình luận
            using var request = new HttpRequestMessage(HttpMethod.Post, "/moderation/blog-comments/moderate-one")
            {
                // Truyền thân yêu cầu (Body) chứa loại đối tượng (Comment/Reply) và ID đối tượng
                Content = JsonContent.Create(new ModerateOneRequest(targetType, targetId))
            };
            
            // 3. Thêm mã khóa API nội bộ (Internal Api Key) vào Header để xác thực yêu cầu
            request.Headers.Add("X-Internal-Key", options.InternalApiKey);

            // 4. Gửi yêu cầu HTTP đến AI Moderation API
            using var response = await client.SendAsync(request, cancellationToken);
            
            // KIỂM TRA 2: Kiểm tra phản hồi HTTP thành công
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "AI moderation call failed for {TargetType} {TargetId}. StatusCode={StatusCode}",
                    targetType,
                    targetId,
                    response.StatusCode);
                return false; // Trả về false nếu cuộc gọi API thất bại
            }

            // 5. Đọc phản hồi JSON kết quả kiểm duyệt từ AI
            var body = await response.Content.ReadFromJsonAsync<ModerateOneResponse>(cancellationToken: cancellationToken);
            
            _logger.LogInformation(
                "AI moderation responded for {TargetType} {TargetId}: Accepted={Accepted}, FinalStatus={FinalStatus}",
                targetType,
                targetId,
                body?.Accepted,
                body?.FinalStatus);
                
            // Trả về true nếu AI xác nhận nội dung hợp lệ/được duyệt (Accepted = true)
            return body?.Accepted == true;
        }
        catch (Exception ex)
        {
            // Ghi log cảnh báo nếu có lỗi kết nối, timeout hoặc lỗi xử lý hệ thống ngoại vi
            _logger.LogWarning(
                ex,
                "AI moderation call error for {TargetType} {TargetId}",
                targetType,
                targetId);
            return false;
        }
    }

    /// <summary>
    /// Record DTO phục vụ gửi payload yêu cầu kiểm duyệt HTTP POST.
    /// </summary>
    private sealed record ModerateOneRequest(string TargetType, int TargetId);

    /// <summary>
    /// Lớp DTO nhận kết quả trả về từ API kiểm duyệt AI.
    /// </summary>
    private sealed class ModerateOneResponse
    {
        /// <summary>
        /// Cho biết nội dung bình luận có được chấp nhận (duyệt hiển thị) hay không.
        /// </summary>
        public bool Accepted { get; set; }
        
        /// <summary>
        /// Trạng thái kiểm duyệt cuối cùng của bình luận (ví dụ: APPROVED, REJECTED...).
        /// </summary>
        [JsonPropertyName("final_status")]
        public string? FinalStatus { get; set; }
    }
}
