using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Blogs;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Services;

public interface IBlogService
{
    Task<Result<PaginatedResponse<BlogListDto>>> GetBlogsForAdminAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    Task<Result<PaginatedResponse<BlogListDto>>> GetBlogsForStaffAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    Task<Result<PaginatedResponse<BlogListDto>>> SearchPublishedBlogsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    Task<Result<BlogDetailDto>> GetBlogDetailsAsync(int blogPostId, CancellationToken cancellationToken = default);

    Task<Result<BlogDetailDto>> CreateBlogAsync(CreateBlogDto dto, CancellationToken cancellationToken = default);

    Task<Result<BlogDetailDto>> UpdateBlogAsync(int blogPostId, UpdateBlogDto dto, CancellationToken cancellationToken = default);

    Task<Result<BlogDetailDto>> SubmitBlogAsync(int blogPostId, SubmitBlogDto dto, CancellationToken cancellationToken = default);

    Task<Result<BlogDetailDto>> ApproveBlogAsync(int blogPostId, ApproveBlogDto dto, CancellationToken cancellationToken = default);
}
