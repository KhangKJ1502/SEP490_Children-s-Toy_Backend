using Microsoft.Extensions.DependencyInjection;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Chatbot.Services;

namespace ToyStore.Chatbot;

/// <summary>
/// Dependency injection extensions for Chatbot layer.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddChatbot(this IServiceCollection services)
    {
        services.AddScoped<IChatbotService, ChatbotService>();
        services.AddScoped<PromptBuilder>();
        return services;
    }
}
