using Microsoft.EntityFrameworkCore;
using ToyStore.Application.DTOs.Campaigns;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Services.Resolvers;

public class BlogPostResolver : IBusinessObjectResolver
{
    private readonly SEP490ToyStoreContext _context;

    public BlogPostResolver(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    public string ReferenceType => "BLOG";

    public IReadOnlyList<PlaceholderInfoDto> AvailablePlaceholders =>
    [
        new() { Token = "{{BlogTitle}}",  Description = "Blog post title" },
        new() { Token = "{{BlogPostId}}", Description = "Blog post ID" }
    ];

    public async Task<ResolvedReferenceDto?> ResolveAsync(int referenceId, CancellationToken cancellationToken = default)
    {
        var post = await _context.BlogPosts
            .AsNoTracking()
            .Where(b => b.BlogPostId == referenceId && !b.IsDeleted)
            .Select(b => new
            {
                b.BlogPostId,
                b.BlogTitle,
                b.BlogThumbnail
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (post is null) return null;

        return new ResolvedReferenceDto
        {
            DisplayName = post.BlogTitle,
            ImageUrl = post.BlogThumbnail,
            DefaultActionTarget = $"/blog/{post.BlogPostId}",
            Placeholders = new Dictionary<string, string>
            {
                ["{{BlogTitle}}"]  = post.BlogTitle,
                ["{{BlogPostId}}"] = post.BlogPostId.ToString()
            }
        };
    }
}
