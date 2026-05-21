/* Update existing [Notification].[Templates] to English (idempotent). */
BEGIN TRY
    BEGIN TRAN;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Order placed successfully',
        MessageTemplate = N'Your order {{OrderCode}} ({{TotalAmount}} VND) has been received.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'ORDER_PLACED' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Order confirmed: {{OrderCode}}',
        MessageTemplate = N'Your order {{OrderCode}} has been confirmed and is being prepared.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'ORDER_CONFIRMED' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Order {{OrderCode}} is being packed',
        MessageTemplate = N'Your order {{OrderCode}} is being packed by our team.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'ORDER_PACKING' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Order {{OrderCode}} is on the way',
        MessageTemplate = N'Order {{OrderCode}} has been handed to courier {{ShipperName}}. Please keep your phone available.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'ORDER_SHIPPING' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Delivery successful',
        MessageTemplate = N'Order {{OrderCode}} was delivered successfully. Leave a review to unlock rewards!',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'ORDER_DELIVERED' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Order {{OrderCode}} cancelled',
        MessageTemplate = N'Your order {{OrderCode}} was cancelled. Reason: {{CancelReason}}.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'ORDER_CANCELLED' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Delivery failed',
        MessageTemplate = N'Delivery for order {{OrderCode}} failed: {{FailReason}}. Please contact support.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'ORDER_DELIVERY_FAILED' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Order Assigned: {{OrderCode}}',
        MessageTemplate = N'Order {{OrderCode}} from {{CustomerName}} has been assigned to you. Total: {{TotalAmount}} VND. Please process it during your current shift.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'ORDER_ASSIGNED' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Payment successful',
        MessageTemplate = N'You paid {{Amount}} VND for order {{OrderCode}}.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'PAYMENT_SUCCESS' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Payment failed',
        MessageTemplate = N'Payment of {{Amount}} VND for order {{OrderCode}} failed.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'PAYMENT_FAILED' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Wallet top-up successful',
        MessageTemplate = N'{{Amount}} VND was added to your wallet. Current balance: {{Balance}} VND.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'WALLET_TOPUP' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Refund to wallet',
        MessageTemplate = N'{{Amount}} VND was refunded to your wallet for order {{OrderCode}}.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'WALLET_REFUND' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Refund approved',
        MessageTemplate = N'Your refund request for order {{OrderCode}} was approved. {{Amount}} VND will be returned to your wallet.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'REFUND_APPROVED' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Refund rejected',
        MessageTemplate = N'Your refund request for order {{OrderCode}} was rejected. Contact support if you need help.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'REFUND_REJECTED' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Refund completed',
        MessageTemplate = N'{{Amount}} VND from order {{OrderCode}} has been refunded to your wallet.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'REFUND_COMPLETED' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'{{ProductName}} is back in stock',
        MessageTemplate = N'{{ProductName}} on your wishlist is available again at {{Price}} VND. Order before it sells out!',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'PRODUCT_BACK_IN_STOCK' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Price drop: {{ProductName}}',
        MessageTemplate = N'{{ProductName}} in your wishlist is now {{Price}} VND.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'WISHLIST_PRICE_DROP' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Review reply: {{ProductName}}',
        MessageTemplate = N'Staff replied to your review for {{ProductName}}.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'REVIEW_STAFF_REPLIED' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Blog comment reply: {{BlogTitle}}',
        MessageTemplate = N'Someone replied to your comment on {{BlogTitle}}.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'BLOG_COMMENT_REPLIED' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'New order: {{OrderCode}}',
        MessageTemplate = N'New order {{OrderCode}} ({{TotalAmount}} VND). Please process it.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'STAFF_NEW_ORDER' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Cancel request: {{OrderCode}}',
        MessageTemplate = N'Customer {{CustomerName}} requested cancellation for order {{OrderCode}}. Reason: {{Reason}}.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'STAFF_CANCEL_REQUEST' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Refund request: {{OrderCode}}',
        MessageTemplate = N'Customer {{CustomerName}} requested a refund for order {{OrderCode}}.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'STAFF_REFUND_REQUEST' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Review needs moderation',
        MessageTemplate = N'New {{Rating}}-star review for {{ProductName}} needs moderation.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'STAFF_REVIEW_MODERATION' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Low rating alert',
        MessageTemplate = N'{{ProductName}} received a {{Rating}}-star review. Please review and follow up.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'STAFF_LOW_RATING' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Shift {{ShiftName}} started',
        MessageTemplate = N'Your shift {{ShiftName}} has started. Have a productive day!',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'STAFF_SHIFT_STARTED' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Order ready to pack: {{OrderCode}}',
        MessageTemplate = N'Order {{OrderCode}} is ready for packing.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'MERCH_READY_TO_PACK' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Courier picked up order',
        MessageTemplate = N'Courier picked up packages for order {{OrderCode}}.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'MERCH_PICKED_UP' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Returned to warehouse',
        MessageTemplate = N'Order {{OrderCode}} was returned to the warehouse.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'MERCH_RETURNED' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Low stock alert',
        MessageTemplate = N'{{ProductName}} has only {{Quantity}} units left in stock. Restock soon.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'MERCH_LOW_STOCK' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Out of stock',
        MessageTemplate = N'{{ProductName}} is out of stock in the warehouse.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'MERCH_OUT_OF_STOCK' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Payment gateway error',
        MessageTemplate = N'Payment gateway {{GatewayName}} reported an error: {{ErrorMessage}}.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'ADMIN_PAYMENT_ERROR' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Background job failed',
        MessageTemplate = N'Background job {{JobName}} failed. Please check the logs.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'ADMIN_JOB_FAILED' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Outbox event stuck',
        MessageTemplate = N'Outbox event {{EventType}} ({{EventId}}) reached the retry limit. Please check the logs.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'ADMIN_OUTBOX_STUCK' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Shipping sync error',
        MessageTemplate = N'Shipping sync failed for order {{OrderCode}}: {{ErrorMessage}}.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'ADMIN_SHIPPING_ERROR' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Damage or loss reported',
        MessageTemplate = N'Order {{OrderCode}} was reported damaged or lost in transit.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'ADMIN_DAMAGE_LOST' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Blog pending approval: {{BlogTitle}}',
        MessageTemplate = N'Blog post {{BlogTitle}} was submitted and is waiting for your approval.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'ADMIN_BLOG_PENDING' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Order {{OrderCode}} queued for assignment',
        MessageTemplate = N'Order {{OrderCode}} could not be auto-assigned: {{Reason}}. Please assign manually.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'ADMIN_ORDER_QUEUED' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Shift {{ShiftName}} ended with pending orders',
        MessageTemplate = N'Shift {{ShiftName}} (staff #{{AccountId}}) ended with {{CurrentLoad}} order(s) still in progress.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'ADMIN_SHIFT_ENDED_PENDING' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Shift {{ShiftName}} at capacity on {{WorkDate}}',
        MessageTemplate = N'One or more roles on shift {{ShiftName}} ({{WorkDate}}) reached max concurrent orders. Increase MaxLoad or reassign orders manually.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'ADMIN_SHIFT_FULL' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Flash sale started: {{PromotionName}}',
        MessageTemplate = N'{{PromotionName}} is live from {{StartDate}} to {{EndDate}}. Shop now!',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'FLASH_SALE_STARTED' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'New voucher: {{VoucherCode}}',
        MessageTemplate = N'You received voucher {{VoucherCode}} ({{DiscountValue}}, {{DiscountType}}). Use before {{ExpiryDate}}.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'VOUCHER_NEW' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Voucher {{VoucherCode}} expiring soon',
        MessageTemplate = N'Voucher {{VoucherCode}} (save {{DiscountValue}}) expires on {{ExpiryDate}}. Use it soon!',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'VOUCHER_EXPIRING' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Happy birthday, {{CustomerName}}!',
        MessageTemplate = N'Happy birthday! ToyStore has a special gift for you — check your Vouchers.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'BIRTHDAY_CUSTOMER' AND IsDeleted = 0;

    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Happy birthday, {{ChildName}}!',
        MessageTemplate = N'Happy birthday to {{ChildName}}! Pick a favorite toy from ToyStore.',
        UpdatedAt       = GETDATE()
    WHERE TemplateCode = 'BIRTHDAY_CHILD' AND IsDeleted = 0;

    COMMIT TRAN;
    PRINT N'Notification templates updated to English.';
END TRY
BEGIN CATCH
    ROLLBACK TRAN;
    THROW;
END CATCH
GO
