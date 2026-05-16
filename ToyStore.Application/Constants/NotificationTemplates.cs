namespace ToyStore.Application.Constants;

/// <summary>
/// Danh sách các TemplateCode hệ thống — phải khớp đúng với cột TemplateCode trong bảng [Notification].[Templates].
/// Chạy docs/database/changes/notification_templates_full.sql để seed đủ các mã này vào DB trước khi dùng.
/// </summary>
public static class NotificationTemplates
{
    // Đơn hàng — khách hàng
    public const string OrderPlaced          = "ORDER_PLACED";
    public const string OrderConfirmed       = "ORDER_CONFIRMED";
    public const string OrderPacking         = "ORDER_PACKING";
    public const string OrderShipping        = "ORDER_SHIPPING";
    public const string OrderDelivered       = "ORDER_DELIVERED";
    public const string OrderCancelled       = "ORDER_CANCELLED";
    public const string OrderDeliveryFailed  = "ORDER_DELIVERY_FAILED";
    public const string OrderAssigned        = "ORDER_ASSIGNED";

    // Thanh toán & Ví
    public const string PaymentSuccess       = "PAYMENT_SUCCESS";
    public const string PaymentFailed        = "PAYMENT_FAILED";
    public const string WalletTopup          = "WALLET_TOPUP";
    public const string WalletRefund         = "WALLET_REFUND";
    public const string RefundApproved       = "REFUND_APPROVED";
    public const string RefundRejected       = "REFUND_REJECTED";
    public const string RefundCompleted      = "REFUND_COMPLETED";

    // Sản phẩm & Tồn kho
    public const string ProductBackInStock   = "PRODUCT_BACK_IN_STOCK";
    public const string WishlistPriceDrop    = "WISHLIST_PRICE_DROP";

    // Marketing (ADMIN scope — campaign-only)
    public const string FlashSaleStarted     = "FLASH_SALE_STARTED";
    public const string VoucherNew           = "VOUCHER_NEW";
    public const string VoucherExpiring      = "VOUCHER_EXPIRING";
    public const string BirthdayCustomer     = "BIRTHDAY_CUSTOMER";
    public const string BirthdayChild        = "BIRTHDAY_CHILD";

    // Review & Blog
    public const string ReviewStaffReplied   = "REVIEW_STAFF_REPLIED";
    public const string BlogCommentReplied   = "BLOG_COMMENT_REPLIED";

    // Staff
    public const string StaffNewOrder        = "STAFF_NEW_ORDER";
    public const string StaffCancelRequest   = "STAFF_CANCEL_REQUEST";
    public const string StaffRefundRequest   = "STAFF_REFUND_REQUEST";
    public const string StaffReviewModeration = "STAFF_REVIEW_MODERATION";
    public const string StaffLowRating       = "STAFF_LOW_RATING";
    public const string StaffShiftStarted    = "STAFF_SHIFT_STARTED";
    public const string StaffOrderAssigned   = "ORDER_ASSIGNED";  // alias

    // Merchandise
    public const string MerchReadyToPack     = "MERCH_READY_TO_PACK";
    public const string MerchPickedUp        = "MERCH_PICKED_UP";
    public const string MerchReturned        = "MERCH_RETURNED";
    public const string MerchLowStock        = "MERCH_LOW_STOCK";
    public const string MerchOutOfStock      = "MERCH_OUT_OF_STOCK";

    // Admin — system alerts
    public const string AdminPaymentError    = "ADMIN_PAYMENT_ERROR";
    public const string AdminJobFailed       = "ADMIN_JOB_FAILED";
    public const string AdminOutboxStuck     = "ADMIN_OUTBOX_STUCK";
    public const string AdminShippingError   = "ADMIN_SHIPPING_ERROR";
    public const string AdminDamageLost      = "ADMIN_DAMAGE_LOST";
    public const string AdminBlogPending     = "ADMIN_BLOG_PENDING";
    public const string AdminOrderQueued     = "ADMIN_ORDER_QUEUED";
    public const string AdminShiftEndedPending = "ADMIN_SHIFT_ENDED_PENDING";
}
