/* ══════════════════════════════════════════════════════════════
   NOTIFICATION TEMPLATES — Full idempotent seed & update
   Run this script to ensure all required TemplateCodes are 
   properly populated and updated to English in [Notification].[Templates].

   Safe to re-run multiple times (uses MERGE statement).
   Run this script BEFORE launching the application.
   ══════════════════════════════════════════════════════════════ */
BEGIN TRY
    BEGIN TRAN;

    DECLARE @Tpl TABLE (
    TemplateCode VARCHAR(50) NOT NULL PRIMARY KEY,
    UsageScope VARCHAR(10) NOT NULL,
    TitleTemplate NVARCHAR(255) NOT NULL,
    MessageTemplate NVARCHAR(500) NOT NULL
    );

    /* ─── SYSTEM GROUP: automatic, cannot be disabled/deleted ─── */

    -- Orders — customer & staff
    INSERT INTO @Tpl
VALUES
    ('ORDER_PLACED', 'SYSTEM', N'Order placed successfully',
        N'Your order {{OrderCode}} ({{TotalAmount}} VND) has been received.'),
    ('ORDER_CONFIRMED', 'SYSTEM', N'Order confirmed: {{OrderCode}}',
        N'Your order {{OrderCode}} has been confirmed and is being prepared.'),
    ('ORDER_PACKING', 'SYSTEM', N'Order {{OrderCode}} is being packed',
        N'Your order {{OrderCode}} is being packed by our team.'),
    ('ORDER_SHIPPING', 'SYSTEM', N'Order {{OrderCode}} is on the way',
        N'Order {{OrderCode}} has been handed to courier {{ShipperName}}. Please keep your phone available.'),
    ('ORDER_DELIVERED', 'SYSTEM', N'Delivery successful',
        N'Order {{OrderCode}} has been delivered successfully. We would love to hear your feedback.'),
    ('ORDER_COMPLETED', 'SYSTEM', N'Order {{OrderCode}} completed',
        N'Your order {{OrderCode}} has been automatically completed. You have 3 days to request a refund if needed.'),
    ('ORDER_CANCELLED', 'SYSTEM', N'Order {{OrderCode}} cancelled',
        N'Your order {{OrderCode}} was cancelled. Reason: {{CancelReason}}.'),
    ('ORDER_DELIVERY_FAILED', 'SYSTEM', N'Delivery unsuccessful',
        N'The courier could not reach you. Another delivery attempt will be made. Please keep your phone nearby.'),
    ('ORDER_RETURN_REFUND_PENDING', 'SYSTEM', N'Returned to shop',
        N'Order {{OrderCode}} has returned to our shop. We are processing your refund.'),
    ('ORDER_CANCELLED_DELIVERY_FAIL', 'SYSTEM', N'Order cancelled',
        N'Order {{OrderCode}} was cancelled due to failed delivery.'),
    ('ORDER_ASSIGNED', 'SYSTEM', N'Order Assigned: {{OrderCode}}',
        N'Order {{OrderCode}} from {{CustomerName}} has been assigned to you. Total: {{TotalAmount}} VND. Please process it during your current shift.');

    -- Payments & wallet
    INSERT INTO @Tpl
VALUES
    ('PAYMENT_SUCCESS', 'SYSTEM', N'Payment successful',
        N'You paid {{Amount}} VND for order {{OrderCode}}.'),
    ('PAYMENT_FAILED', 'SYSTEM', N'Payment failed',
        N'Payment of {{Amount}} VND for order {{OrderCode}} failed.'),
    ('WALLET_TOPUP', 'SYSTEM', N'Wallet top-up successful',
        N'{{Amount}} VND was added to your wallet. Current balance: {{Balance}} VND.'),
    ('WALLET_REFUND', 'SYSTEM', N'Refund to wallet',
        N'{{Amount}} VND was refunded to your wallet for order {{OrderCode}}.'),
    ('WALLET_WITHDRAWAL_SUCCESS', 'SYSTEM', N'Withdrawal successful',
        N'{{Amount}} VND has been transferred to {{BankName}} - {{AccountNumber}} successfully.'),
    ('WALLET_WITHDRAWAL_FAILED', 'SYSTEM', N'Withdrawal failed',
        N'Withdrawal of {{Amount}} VND failed. Reason: {{FailReason}}. Your wallet balance was not changed.'),
    ('REFUND_APPROVED', 'SYSTEM', N'Refund approved',
        N'Your refund request for order {{OrderCode}} was approved. {{Amount}} VND will be returned to your wallet.'),
    ('REFUND_REJECTED', 'SYSTEM', N'Refund rejected',
        N'Your refund request for order {{OrderCode}} was rejected. Contact support if you need help.'),
    ('REFUND_COMPLETED', 'SYSTEM', N'Refund completed',
        N'{{Amount}} VND from order {{OrderCode}} has been refunded to your wallet.');

    -- Products & Stock
    INSERT INTO @Tpl
VALUES
    ('PRODUCT_BACK_IN_STOCK', 'SYSTEM', N'Product back in stock: {{ProductName}}',
        N'The product {{ProductName}} you are interested in is back in stock at {{Price}} VND. Get it now before it runs out!'),
    ('WISHLIST_PRICE_DROP', 'SYSTEM', N'Price drop on {{ProductName}}',
        N'{{ProductName}} in your wishlist is now on sale for only {{Price}} VND.');

    -- Reviews & Blogs
    INSERT INTO @Tpl
VALUES
    ('REVIEW_STAFF_REPLIED', 'SYSTEM', N'Reply to review on {{ProductName}}',
        N'A customer service representative has replied to your review of {{ProductName}}.'),
    ('BLOG_COMMENT_REPLIED', 'SYSTEM', N'Blog comment reply: {{BlogTitle}}',
        N'Someone replied to your comment on {{BlogTitle}}.');

    -- Staff Notifications
    INSERT INTO @Tpl
VALUES
    ('STAFF_NEW_ORDER', 'SYSTEM', N'New order received: {{OrderCode}}',
        N'The system registered a new order {{OrderCode}} worth {{TotalAmount}} VND. Please process it.'),
    ('STAFF_CANCEL_REQUEST', 'SYSTEM', N'Cancellation request for {{OrderCode}}',
        N'Customer {{CustomerName}} has requested to cancel order {{OrderCode}}. Reason: {{Reason}}.'),
    ('STAFF_REFUND_REQUEST', 'SYSTEM', N'Refund request for {{OrderCode}}',
        N'Customer {{CustomerName}} requested a refund for order {{OrderCode}}.'),
    ('STAFF_SYSTEM_REFUND_READY', 'SYSTEM', N'Inspection complete — refund pending',
        N'Merchandise has inspected the returned items for order {{OrderCode}}. Please confirm the wallet refund.'),
    ('STAFF_REVIEW_MODERATION', 'SYSTEM', N'Approve new review',
        N'There is a new {{Rating}}-star review for product {{ProductName}} that requires moderation.'),
    ('STAFF_LOW_RATING', 'SYSTEM', N'Low rating warning',
        N'Product {{ProductName}} just received a {{Rating}}-star review. Please check and resolve.'),
    ('STAFF_SHIFT_STARTED', 'SYSTEM', N'Shift {{ShiftName}} started',
        N'Your work shift {{ShiftName}} has started. Have a great shift!');

    -- Merchandise Notifications
    INSERT INTO @Tpl
VALUES
    ('MERCH_READY_TO_PACK', 'SYSTEM', N'Order ready for packing',
        N'Order {{OrderCode}} is ready to be packed.'),
    ('MERCH_PICKED_UP', 'SYSTEM', N'Package picked up by courier',
        N'The courier has successfully picked up the package for order {{OrderCode}}.'),
    ('MERCH_RETURNED', 'SYSTEM', N'GHN package returned to shop',
        N'Order {{OrderCode}} has been returned by GHN. Please confirm receipt and inspect the items.'),
    ('MERCH_LOW_STOCK', 'SYSTEM', N'Low stock warning',
        N'Product {{ProductName}} has only {{Quantity}} units left. Restocking is needed.'),
    ('MERCH_OUT_OF_STOCK', 'SYSTEM', N'Out of stock warning',
        N'Product {{ProductName}} is completely out of stock.');

    -- Admin Notifications
    INSERT INTO @Tpl
VALUES
    ('ADMIN_PAYMENT_ERROR', 'SYSTEM', N'Payment gateway error',
        N'Payment gateway {{GatewayName}} reported error: {{ErrorMessage}}.'),
    ('ADMIN_JOB_FAILED', 'SYSTEM', N'Background job failed',
        N'Background Job {{JobName}} failed. Please check the logs.'),
    ('ADMIN_OUTBOX_STUCK', 'SYSTEM', N'Outbox event stuck',
        N'Outbox event {{EventType}} ({{EventId}}) has reached retry limits. Please check logs.'),
    ('ADMIN_SHIPPING_ERROR', 'SYSTEM', N'Shipping sync error',
        N'Error synchronizing shipping status for order {{OrderCode}}: {{ErrorMessage}}.'),
    ('ADMIN_DAMAGE_LOST', 'SYSTEM', N'Items damaged/lost',
        N'Order {{OrderCode}} reported as damaged or lost during delivery.'),
    ('ADMIN_RETURN_FAIL', 'SYSTEM', N'GHN return failed',
        N'Order {{OrderCode}} (GHN: {{ProviderOrderCode}}) reported return_fail. Please process manually.'),
    ('ADMIN_BLOG_PENDING', 'SYSTEM', N'Blog pending approval: {{BlogTitle}}',
        N'Blog post {{BlogTitle}} was submitted and is waiting for your approval.'),
    ('ADMIN_ORDER_QUEUED', 'SYSTEM', N'Order {{OrderCode}} pending assignment',
        N'Order {{OrderCode}} is pending assignment due to: {{Reason}}.'),
    ('ADMIN_SHIFT_ENDED_PENDING', 'SYSTEM', N'Shift ended with pending orders',
        N'Shift {{ShiftName}} (staff #{{AccountId}}) ended with {{CurrentLoad}} orders still pending.'),
    ('ADMIN_SHIFT_FULL', 'SYSTEM', N'Shift capacity reached',
        N'One or both roles in shift {{ShiftName}} reached order capacity on {{WorkDate}}. Please manage manually.');

    -- Birthday Notifications (Automated Background Job)
    INSERT INTO @Tpl
VALUES
    ('BIRTHDAY_CUSTOMER', 'SYSTEM', N'Happy Birthday, {{CustomerName}}!',
        N'Happy birthday to you! ToyStore wishes you a wonderful day filled with joy, health, and happiness!'),
    ('BIRTHDAY_CHILD', 'SYSTEM', N'Happy Birthday, {{ChildName}}!',
        N'Happy birthday to {{ChildName}}! ToyStore wishes them healthy growth and joy. Parents, pick a favorite toy for them!');

    /* ─── ADMIN GROUP: marketing, togglable ─────────────────────── */
    INSERT INTO @Tpl
VALUES
    ('FLASH_SALE_STARTED', 'ADMIN', N'🔥 Flash Sale Alert: {{PromotionName}} is NOW LIVE!',
        N'The wait is over! {{PromotionName}} has officially started ({{StartDate}} – {{EndDate}}). Exclusive deals are waiting — shop your favorites before stock runs out!'),
    ('VOUCHER_NEW', 'ADMIN', N'🎁 Special Gift For You: Claim {{VoucherCode}} Now!',
        N'We''ve unlocked an exclusive {{DiscountValue}} voucher (Code: {{VoucherCode}}) just for you! Valid until {{ExpiryDate}}. Tap to claim and treat your little ones today!'),
    ('VOUCHER_EXPIRING', 'ADMIN', N'⏰ Last Chance: Your {{VoucherCode}} Voucher Expires Soon!',
        N'Don''t miss out on your {{DiscountValue}} discount! Voucher {{VoucherCode}} will expire on {{ExpiryDate}}. Use it now before it''s gone!'),
    ('BLOG_NEW', 'ADMIN', N'📖 New Blog Article: {{BlogTitle}}',
        N'Check out our latest blog post "{{BlogTitle}}"! Discover helpful guides, tips, and fun toy reviews for your family.');

    /* ─── MERGE idempotent insertion & update ─────────────────────── */
    MERGE [Notification].[Templates] AS target
    USING @Tpl AS source
    ON (target.TemplateCode = source.TemplateCode)
    WHEN MATCHED AND (target.IsDeleted = 0) THEN
        UPDATE SET 
            target.UsageScope = source.UsageScope,
            target.TitleTemplate = source.TitleTemplate,
            target.MessageTemplate = source.MessageTemplate,
            target.UpdatedAt = GETUTCDATE()
    WHEN NOT MATCHED THEN
        INSERT (TemplateCode, UsageScope, TitleTemplate, MessageTemplate, IsActive, IsDeleted, CreatedAt)
        VALUES (source.TemplateCode, source.UsageScope, source.TitleTemplate, source.MessageTemplate, 1, 0, GETUTCDATE());

    COMMIT TRAN;
    PRINT N'✅ Notification templates fully unified and translated to English.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'❌ Error: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO
