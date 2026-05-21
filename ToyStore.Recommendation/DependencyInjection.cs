using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ToyStore.Application.DTOs.Tracking;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Application.Validators.Tracking;
using ToyStore.Recommendation.Algorithms;
using ToyStore.Recommendation.Configuration;
using ToyStore.Recommendation.MongoDb;
using ToyStore.Recommendation.Services;
using ToyStore.Recommendation.Tracking;

namespace ToyStore.Recommendation;

/// <summary>
/// Đăng ký toàn bộ service + options cho module Recommendation.
/// Gọi từ API + Worker qua extension AddRecommendation(IConfiguration).
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Đăng ký dịch vụ Recommendation (bao gồm cả MongoDB context, options, services).
    /// Background jobs sẽ được đăng ký trong Worker (qua AddRecommendationJobs).
    /// </summary>
    public static IServiceCollection AddRecommendation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Options binding từ appsettings
        services.Configure<MongoDbOptions>(configuration.GetSection(MongoDbOptions.SectionName));
        services.Configure<RecommendationOptions>(configuration.GetSection(RecommendationOptions.SectionName));

        // MongoDbContext: Singleton vì MongoClient đã có connection pool nội bộ
        services.AddSingleton<MongoDbContext>();

        // Validator riêng cho Tracking (đăng ký rõ ràng để tránh phụ thuộc assembly scan)
        services.AddScoped<IValidator<TrackEventRequestDto>, TrackEventRequestValidator>();

        // Algorithms + BusinessRulesFilter (scoped vì cần DbContext)
        services.AddScoped<TrendingAlgorithm>();
        services.AddScoped<ContentBasedAlgorithm>();
        services.AddScoped<CoPurchaseAlgorithm>();
        services.AddScoped<WeightedScoringAlgorithm>();
        services.AddScoped<BusinessRulesFilter>();

        // Application services
        services.AddScoped<ITrackingService, TrackingService>();
        services.AddScoped<IRecommendationService, RecommendationService>();

        return services;
    }
}
