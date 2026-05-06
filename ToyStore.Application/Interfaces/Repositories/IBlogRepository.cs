using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

public interface IBlogRepository
{
    Task<List<BlogPost>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        string? status = null,
        bool featuredOnly = false,
        int? createdByAccountId = null,
        bool onlyPublished = false,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        string? searchTerm = null,
        string? status = null,
        bool featuredOnly = false,
        int? createdByAccountId = null,
        bool onlyPublished = false,
        CancellationToken cancellationToken = default);

    Task<BlogPost?> GetByIdAsync(int blogPostId, CancellationToken cancellationToken = default);

    Task<bool> BlogCategoryExistsAsync(short blogCategoryId, CancellationToken cancellationToken = default);

    Task<BlogPost> CreateAsync(BlogPost entity, CancellationToken cancellationToken = default);

    Task<BlogPost> UpdateAsync(BlogPost entity, CancellationToken cancellationToken = default);

    Task<int> PublishDueScheduledBlogsAsync(DateTime utcNow, CancellationToken cancellationToken = default);

    Task<BlogPost> HideAsync(BlogPost entity, CancellationToken cancellationToken = default);
}
