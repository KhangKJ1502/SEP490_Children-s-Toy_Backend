using ToyStore.Application.DTOs.Blogs;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Gateway used by backend service to invoke Python AI blog content generation.
/// </summary>
public interface IBlogContentGenerationGateway
{
    Task<BlogContentGenerationGatewayResult> GenerateAsync(
        PythonBlogGenerateRequest request,
        CancellationToken cancellationToken = default);
}
