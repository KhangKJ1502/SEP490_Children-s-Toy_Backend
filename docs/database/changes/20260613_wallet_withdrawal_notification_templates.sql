-- ============================================================
-- Seed notification templates for withdrawal success/failure
-- Run once against SEP490_ToyStore DB
-- ============================================================

-- WALLET_WITHDRAWAL_SUCCESS
IF NOT EXISTS (
    SELECT 1 FROM [Notification].[Templates]
    WHERE TemplateCode = 'WALLET_WITHDRAWAL_SUCCESS'
)
BEGIN
    INSERT INTO [Notification].[Templates]
        (TemplateCode, Name, TitleTemplate, BodyTemplate, EmailSubjectTemplate, EmailBodyTemplate, IsActive, CreatedAt)
    VALUES (
        'WALLET_WITHDRAWAL_SUCCESS',
        N'Withdrawal successful',
        N'Withdrawal successful',
        N'{{Amount}} VND has been transferred to {{BankName}} - {{AccountNumber}} successfully.',
        N'[ToyStore] Withdrawal successful',
        N'<p>Your withdrawal of <strong>{{Amount}} VND</strong> to <strong>{{BankName}} - {{AccountNumber}}</strong> has been completed.</p>',
        1,
        GETDATE()
    );
END

-- WALLET_WITHDRAWAL_FAILED
IF NOT EXISTS (
    SELECT 1 FROM [Notification].[Templates]
    WHERE TemplateCode = 'WALLET_WITHDRAWAL_FAILED'
)
BEGIN
    INSERT INTO [Notification].[Templates]
        (TemplateCode, Name, TitleTemplate, BodyTemplate, EmailSubjectTemplate, EmailBodyTemplate, IsActive, CreatedAt)
    VALUES (
        'WALLET_WITHDRAWAL_FAILED',
        N'Withdrawal failed',
        N'Withdrawal failed',
        N'Withdrawal of {{Amount}} VND failed. {{FailReason}}. Your wallet balance was not changed.',
        N'[ToyStore] Withdrawal failed',
        N'<p>Your withdrawal of <strong>{{Amount}} VND</strong> was unsuccessful. Reason: {{FailReason}}. Your wallet balance was not changed.</p>',
        1,
        GETDATE()
    );
END
