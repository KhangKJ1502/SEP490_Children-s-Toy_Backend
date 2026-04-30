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
        new() { Token = "{{BlogTitle}}",  Description = "Tieu de bai blog" },
        new() { Token = "{{BlogPostId}}", Description = "ID bai blog" }
    ];

    public async Task<ResolvedReferenceDto?> ResolveAsync(int referenceId, CancellationToken cancellationToken = default)
    {
        var post = await _context.BlogPosts
            .AsNoTracking()
            .Where(b => b.BlogPostId == referenceId && !b.IsDeleted)
            .Select(b => new
            {
                b.BlogPostId,
                b.BlogTitle
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (post is null) return null;

        return new ResolvedReferenceDto
        {
            DisplayName = post.BlogTitle,
            DefaultActionTarget = $"/blog/{post.BlogPostId}",
            Placeholders = new Dictionary<string, string>
            {
                ["{{BlogTitle}}"]  = post.BlogTitle,
                ["{{BlogPostId}}"] = post.BlogPostId.ToString()
            }
        };
    }
}
