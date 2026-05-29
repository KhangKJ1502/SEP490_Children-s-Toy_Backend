/* ══════════════════════════════════════════════════════════════
   GHN Return Flow — StatusOrders 10/11, RefundReason, Templates
   Idempotent — safe to re-run.
══════════════════════════════════════════════════════════════ */
BEGIN TRY
    BEGIN TRAN;

    /* StatusOrders 10 (Returning), 11 (ReturnCompleted) */
    SET IDENTITY_INSERT [dbo].[StatusOrders] ON;

    IF NOT EXISTS (SELECT 1 FROM [dbo].[StatusOrders] WHERE StatusID = 10)
        INSERT INTO [dbo].[StatusOrders] (StatusID, StatusName, Description)
        VALUES (10, 'Returning', N'Hàng đang hoàn về kho');

    IF NOT EXISTS (SELECT 1 FROM [dbo].[StatusOrders] WHERE StatusID = 11)
        INSERT INTO [dbo].[StatusOrders] (StatusID, StatusName, Description)
        VALUES (11, 'ReturnCompleted', N'Hàng đã về kho, chờ xử lý hoàn tiền');

    SET IDENTITY_INSERT [dbo].[StatusOrders] OFF;

    /* Refund reason: giao hàng thất bại GHN */
    IF NOT EXISTS (
        SELECT 1 FROM [dbo].[OrderRefundReasons]
        WHERE Content = N'Giao hàng thất bại / không giao được' AND IsDeleted = 0
    )
        INSERT INTO [dbo].[OrderRefundReasons] (Content, Description, CreatedAt)
        VALUES (
            N'Giao hàng thất bại / không giao được',
            N'GHN hoàn hàng về kho do giao không thành công',
            GETUTCDATE()
        );

    /* Notification templates */
    UPDATE [Notification].[Templates]
    SET MessageTemplate = N'Shipper chưa gặp được bạn, sẽ thử giao lại. Vui lòng để ý điện thoại.',
        TitleTemplate   = N'Giao hàng chưa thành công',
        UpdatedAt       = GETUTCDATE()
    WHERE TemplateCode = 'ORDER_DELIVERY_FAILED' AND IsDeleted = 0;

    IF NOT EXISTS (SELECT 1 FROM [Notification].[Templates] WHERE TemplateCode = 'ORDER_RETURN_REFUND_PENDING' AND IsDeleted = 0)
        INSERT INTO [Notification].[Templates] (TemplateCode, UsageScope, TitleTemplate, MessageTemplate, CreatedAt)
        VALUES (
            'ORDER_RETURN_REFUND_PENDING', 'SYSTEM',
            N'Đơn hàng đã về kho',
            N'Đơn hàng {{OrderCode}} đã về kho, đang xử lý hoàn tiền',
            GETUTCDATE()
        );

    IF NOT EXISTS (SELECT 1 FROM [Notification].[Templates] WHERE TemplateCode = 'ORDER_CANCELLED_DELIVERY_FAIL' AND IsDeleted = 0)
        INSERT INTO [Notification].[Templates] (TemplateCode, UsageScope, TitleTemplate, MessageTemplate, CreatedAt)
        VALUES (
            'ORDER_CANCELLED_DELIVERY_FAIL', 'SYSTEM',
            N'Đơn hàng đã bị huỷ',
            N'Đơn hàng {{OrderCode}} đã bị huỷ do giao hàng thất bại',
            GETUTCDATE()
        );

    IF NOT EXISTS (SELECT 1 FROM [Notification].[Templates] WHERE TemplateCode = 'ADMIN_RETURN_FAIL' AND IsDeleted = 0)
        INSERT INTO [Notification].[Templates] (TemplateCode, UsageScope, TitleTemplate, MessageTemplate, CreatedAt)
        VALUES (
            'ADMIN_RETURN_FAIL', 'SYSTEM',
            N'Hoàn hàng GHN thất bại',
            N'Đơn {{OrderCode}} (GHN: {{ProviderOrderCode}}) báo return_fail. Cần xử lý thủ công.',
            GETUTCDATE()
        );

    COMMIT TRAN;
    PRINT N'[OK] Return flow seed applied.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    THROW;
END CATCH;
GO
