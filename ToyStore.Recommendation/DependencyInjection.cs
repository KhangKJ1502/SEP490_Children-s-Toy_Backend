using Microsoft.Extensions.DependencyInjection;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Recommendation.Services;

namespace ToyStore.Recommendation;

/// <summary>
/// Dependency injection extensions for Recommendation layer.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddRecommendation(this IServiceCollection services)
    {
        services.AddScoped<IRecommendationService, RecommendationService>();
        return services;
    }
}
