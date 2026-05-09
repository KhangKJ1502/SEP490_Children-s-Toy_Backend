using Microsoft.Extensions.DependencyInjection;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Services.Notifications;
using ToyStore.Application.Features.Notifications.Handlers;

namespace ToyStore.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Notification Services
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
        services.AddScoped<ICampaignNotificationService, CampaignNotificationService>();
        services.AddScoped<IUserPreferenceChecker, UserPreferenceChecker>();

        // Outbox Event Handlers
        services.AddScoped<IOutboxEventHandler, OrderPlacedHandler>();
        services.AddScoped<IOutboxEventHandler, OrderStatusChangedHandler>();
        services.AddScoped<IOutboxEventHandler, ShippingWebhookHandler>();
        services.AddScoped<IOutboxEventHandler, PaymentSuccessHandler>();
        services.AddScoped<IOutboxEventHandler, PaymentFailedHandler>();
        services.AddScoped<IOutboxEventHandler, WalletTopupHandler>();
        services.AddScoped<IOutboxEventHandler, WalletRefundHandler>();
        services.AddScoped<IOutboxEventHandler, PaymentGatewayErrorHandler>();
        services.AddScoped<IOutboxEventHandler, BackgroundJobFailedHandler>();
        services.AddScoped<IOutboxEventHandler, BlogPendingApprovalHandler>();
        services.AddScoped<IOutboxEventHandler, RefundRequestHandler>();
        services.AddScoped<IOutboxEventHandler, ReviewNeedsModerationHandler>();
        services.AddScoped<IOutboxEventHandler, ReviewLowRatingHandler>();
        services.AddScoped<IOutboxEventHandler, ReviewStaffRepliedHandler>();
        services.AddScoped<IOutboxEventHandler, BlogCommentRepliedHandler>();

        return services;
    }
}
