using ToyStore.Application.Constants;

namespace ToyStore.Application.Features.Notifications;

public static class ShippingStatusMapper
{
    public static byte? ToOrderStatusId(string shippingStatus) => shippingStatus switch
    {
        "delivering" or "money_collect_delivering" => 5,
        "delivered"                                => 6,
        _                                          => null,
    };

    public static string? ToOrderEventType(string shippingStatus) => shippingStatus switch
    {
        "delivering" or "money_collect_delivering" => NotificationEventTypes.OrderDelivering,
        "delivered"                                => NotificationEventTypes.OrderDelivered,
        "delivery_fail"                            => NotificationEventTypes.OrderDeliveryFailed,
        "waiting_to_return" or "returning"         => NotificationEventTypes.OrderReturning,
        "returned"                                 => NotificationEventTypes.OrderReturnCompleted,
        "exception"                                => NotificationEventTypes.SystemShippingWebhookError,
        "damage" or "lost"                         => NotificationEventTypes.SystemShippingDamageLost,
        "picked"                                   => "shipping.picked_up",
        "return_fail"                               => NotificationEventTypes.MerchReturned,
        _                                          => null,
    };
}
