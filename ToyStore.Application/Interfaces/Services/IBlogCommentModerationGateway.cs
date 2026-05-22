namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Gateway used by backend service to invoke Python AI moderation for blog comments/replies.
/// </summary>
public interface IBlogCommentModerationGateway
{
    Task<bool> ModerateCommentAsync(int reviewBlogId, CancellationToken cancellationToken = default);

    Task<bool> ModerateReplyAsync(int replyBlogId, CancellationToken cancellationToken = default);
}

