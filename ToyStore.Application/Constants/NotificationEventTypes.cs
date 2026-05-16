namespace ToyStore.Application.Constants;

public static class NotificationEventTypes
{
    // Customer — order journey
    public const string OrderPlaced         = "order.placed";
    public const string OrderConfirmed      = "order.confirmed";
    public const string OrderPacking        = "order.packing";
    public const string OrderShipped        = "order.shipped";
    public const string OrderDelivering     = "order.delivering";
    public const string OrderDelivered      = "order.delivered";
    public const string OrderCompleted      = "order.completed";
    public const string OrderCancelled      = "order.cancelled";
    public const string OrderDeliveryFailed = "order.delivery_failed";
    public const string OrderReturning      = "order.returning";
    public const string OrderReturnCompleted = "order.return_completed";

    // Customer — payment & wallet
    public const string PaymentSuccess = "payment.success";
    public const string PaymentFailed  = "payment.failed";
    public const string WalletTopup    = "wallet.topup";
    public const string WalletRefund   = "wallet.refund";

    // Customer — promotions & stock
    public const string ProductBackInStock   = "product.back_in_stock";
    public const string WishlistPriceDrop    = "wishlist.price_drop";
    public const string FlashSaleStarted     = "flash_sale.started";
    public const string VoucherNew           = "voucher.new";
    public const string VoucherExpiring      = "voucher.expiring";

    // Customer — birthdays
    public const string BirthdayCustomer = "birthday.customer";
    public const string BirthdayChild    = "birthday.child";

    // Customer — social
    public const string ReviewStaffReplied   = "review.staff_replied";
    public const string BlogCommentReplied   = "blog.comment_replied";

    // Staff
    public const string StaffNewPendingOrder  = "order.new_pending";
    public const string StaffPaymentOverdue   = "order.payment_overdue";
    public const string StaffCancelRequested  = "order.cancel_requested";
    public const string RefundNewRequest      = "refund.new_request";
    public const string RefundApproved        = "refund.approved";
    public const string RefundRejected        = "refund.rejected";
    public const string RefundCompleted       = "refund.completed";
    public const string ReviewNeedsModeration = "review.needs_moderation";
    public const string ReviewLowRating       = "review.low_rating";
    public const string StaffOrderAssigned    = "order.assigned";

    // Merchandise
    public const string MerchReadyToPack  = "order.ready_to_pack";
    public const string MerchPickedUp     = "shipping.picked_up";
    public const string MerchReturned     = "shipping.returned";
    public const string ProductLowStock   = "product.low_stock";
    public const string ProductOutOfStock = "product.out_of_stock";

    // Admin — system
    public const string SystemPaymentGatewayError = "system.payment_gateway_error";
    public const string SystemBackgroundJobFailed  = "system.background_job_failed";
    public const string SystemOutboxMaxAttempts    = "system.outbox_max_attempts";
    public const string SystemShippingWebhookError = "system.shipping_webhook_error";
    public const string SystemShippingDamageLost   = "system.shipping_damage_lost";
    public const string ContentBlogPendingApproval = "content.blog_pending_approval";
}
