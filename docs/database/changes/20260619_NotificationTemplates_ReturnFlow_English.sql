/* Update return flow notification templates to English (idempotent). */
BEGIN TRY
    BEGIN TRAN;

    -- Update ORDER_DELIVERY_FAILED
    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Delivery unsuccessful',
        MessageTemplate = N'The courier could not reach you. Another delivery attempt will be made. Please keep your phone nearby.',
        UpdatedAt       = GETUTCDATE()
    WHERE TemplateCode = 'ORDER_DELIVERY_FAILED' AND IsDeleted = 0;

    -- Update ORDER_RETURN_REFUND_PENDING
    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Returned to shop',
        MessageTemplate = N'Order {{OrderCode}} has returned to our warehouse. We are processing your refund.',
        UpdatedAt       = GETUTCDATE()
    WHERE TemplateCode = 'ORDER_RETURN_REFUND_PENDING' AND IsDeleted = 0;

    -- Update ORDER_CANCELLED_DELIVERY_FAIL
    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'Order cancelled',
        MessageTemplate = N'Order {{OrderCode}} was cancelled due to failed delivery.',
        UpdatedAt       = GETUTCDATE()
    WHERE TemplateCode = 'ORDER_CANCELLED_DELIVERY_FAIL' AND IsDeleted = 0;

    -- Update ADMIN_RETURN_FAIL
    UPDATE [Notification].[Templates] SET
        TitleTemplate   = N'GHN return failed',
        MessageTemplate = N'Order {{OrderCode}} (GHN: {{ProviderOrderCode}}) reported return_fail. Please process manually.',
        UpdatedAt       = GETUTCDATE()
    WHERE TemplateCode = 'ADMIN_RETURN_FAIL' AND IsDeleted = 0;

    COMMIT TRAN;
    PRINT N'Return flow notification templates updated to English.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    THROW;
END CATCH
GO
