using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

/// <summary>
/// Defines persistence operations for blog entities.
/// </summary>
public interface IBlogRepository
{
    /// <summary>
    /// Gets paginated blogs with filtering and sorting.
    /// </summary>
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

    /// <summary>
    /// Counts blogs matching filters.
    /// </summary>
    Task<int> CountAsync(
        string? searchTerm = null,
        string? status = null,
        bool featuredOnly = false,
        int? createdByAccountId = null,
        bool onlyPublished = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a blog by id.
    /// </summary>
    Task<BlogPost?> GetByIdAsync(int blogPostId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a blog category exists.
    /// </summary>
    Task<bool> BlogCategoryExistsAsync(short blogCategoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a blog record.
    /// </summary>
    Task<BlogPost> CreateAsync(BlogPost entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a blog record.
    /// </summary>
    Task<BlogPost> UpdateAsync(BlogPost entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes due scheduled blogs based on UTC timestamp.
    /// </summary>
    Task<int> PublishDueScheduledBlogsAsync(DateTime utcNow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hides a blog record.
    /// </summary>
    Task<BlogPost> HideAsync(BlogPost entity, CancellationToken cancellationToken = default);
}
