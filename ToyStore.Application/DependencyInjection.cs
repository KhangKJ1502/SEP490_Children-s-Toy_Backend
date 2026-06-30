using Microsoft.Extensions.DependencyInjection;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Services.Notifications;
using ToyStore.Application.Features.Notifications.Handlers;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Application.Services;
using ToyStore.Application.Services.Campaigns;

namespace ToyStore.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Notification Services
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
        services.AddScoped<ICampaignNotificationService, CampaignNotificationService>();
        services.AddScoped<IUserPreferenceChecker, UserPreferenceChecker>();
        services.AddScoped<INotificationPreferencesGate, NotificationPreferencesGate>();
        services.AddScoped<ICampaignLifecycleRules, CampaignLifecycleRules>();

        // Shipping status mapper
        services.AddScoped<IShippingStatusMapper, ShippingStatusMapper>();

        // Outbox Event Handlers
        services.AddScoped<IOutboxEventHandler, OrderPlacedHandler>();
        services.AddScoped<IOutboxEventHandler, PaymentSuccessHandler>();
        services.AddScoped<IOutboxEventHandler, PaymentFailedHandler>();
        services.AddScoped<IOutboxEventHandler, WalletTopupHandler>();
        services.AddScoped<IOutboxEventHandler, WalletRefundHandler>();
        services.AddScoped<IOutboxEventHandler, WalletWithdrawalSuccessHandler>();
        services.AddScoped<IOutboxEventHandler, WalletWithdrawalFailedHandler>();
        services.AddScoped<IOutboxEventHandler, PaymentGatewayErrorHandler>();
        services.AddScoped<IOutboxEventHandler, BackgroundJobFailedHandler>();
        services.AddScoped<IOutboxEventHandler, BlogPendingApprovalHandler>();
        services.AddScoped<IOutboxEventHandler, RefundRequestHandler>();
        services.AddScoped<IOutboxEventHandler, ReviewNeedsModerationHandler>();
        services.AddScoped<IOutboxEventHandler, ReviewLowRatingHandler>();
        services.AddScoped<IOutboxEventHandler, ReviewStaffRepliedHandler>();
        services.AddScoped<IOutboxEventHandler, BlogCommentRepliedHandler>();
        services.AddScoped<IOutboxEventHandler, StaffCancelRequestedHandler>();
        services.AddScoped<IOutboxEventHandler, StaffOrderAssignedHandler>();
        services.AddScoped<IOutboxEventHandler, OrderReadyForAssignmentHandler>();
        services.AddScoped<IOutboxEventHandler, OrderAutoAssignedHandler>();
        services.AddScoped<IOutboxEventHandler, OrderQueuedHandler>();
        services.AddScoped<IOutboxEventHandler, CapacityFreedHandler>();
        services.AddScoped<IOutboxEventHandler, ShiftStartedHandler>();
        services.AddScoped<IOutboxEventHandler, ShiftEndedWithPendingOrdersHandler>();
        services.AddScoped<IOutboxEventHandler, ShiftFullHandler>();

        // Merch / order status handlers
        services.AddScoped<IOutboxEventHandler, MerchReadyToPackHandler>();
        services.AddScoped<IOutboxEventHandler, OrderConfirmedHandler>();
        services.AddScoped<IOutboxEventHandler, OrderPackingHandler>();
        services.AddScoped<IOutboxEventHandler, OrderShippedHandler>();
        services.AddScoped<IOutboxEventHandler, OrderCancelledHandler>();

        // Refund lifecycle — customer notifications
        services.AddScoped<IOutboxEventHandler, RefundApprovedHandler>();
        services.AddScoped<IOutboxEventHandler, RefundRejectedHandler>();
        services.AddScoped<IOutboxEventHandler, RefundCompletedHandler>();

        // Shipping webhook granular handlers
        services.AddScoped<IOutboxEventHandler, OrderDeliveringHandler>();
        services.AddScoped<IOutboxEventHandler, OrderDeliveredHandler>();
        services.AddScoped<IOutboxEventHandler, OrderDeliveryFailedHandler>();
        services.AddScoped<IOutboxEventHandler, OrderReturnRefundPendingHandler>();
        services.AddScoped<IOutboxEventHandler, OrderCancelledDeliveryFailHandler>();
        services.AddScoped<IOutboxEventHandler, AdminReturnFailHandler>();
        services.AddScoped<IOutboxEventHandler, MerchPickedUpHandler>();
        services.AddScoped<IOutboxEventHandler, MerchReturnedHandler>();
        services.AddScoped<IOutboxEventHandler, StaffSystemRefundReadyHandler>();

        // Wishlist and voucher handlers
        services.AddScoped<IOutboxEventHandler, WishlistPriceDropHandler>();
        services.AddScoped<IOutboxEventHandler, VoucherNewHandler>();

        return services;
    }
}
