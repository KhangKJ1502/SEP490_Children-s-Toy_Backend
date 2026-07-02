/* ══════════════════════════════════════════════════════════════
   MIGRATION: 20260702_1320_AddFinancialDetails_Refunds
   MÔ TẢ: Bổ sung các cột lưu trữ tài chính và tương tác khách hàng vào bảng OrderRefunds
   ══════════════════════════════════════════════════════════════ */

PRINT N'Starting migration: 20260702_1320_AddFinancialDetails_Refunds...';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.OrderRefunds') AND name = 'ItemApprovedSubTotal')
BEGIN
    ALTER TABLE [dbo].[OrderRefunds] ADD [ItemApprovedSubTotal] DECIMAL(12, 0) NOT NULL DEFAULT 0;
    PRINT N'  ✓ Added column ItemApprovedSubTotal to OrderRefunds.';
END
ELSE
BEGIN
    PRINT N'  - Column ItemApprovedSubTotal already exists, skipped.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.OrderRefunds') AND name = 'ItemRejectedSubTotal')
BEGIN
    ALTER TABLE [dbo].[OrderRefunds] ADD [ItemRejectedSubTotal] DECIMAL(12, 0) NOT NULL DEFAULT 0;
    PRINT N'  ✓ Added column ItemRejectedSubTotal to OrderRefunds.';
END
ELSE
BEGIN
    PRINT N'  - Column ItemRejectedSubTotal already exists, skipped.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.OrderRefunds') AND name = 'ReturnToCustomerFee')
BEGIN
    ALTER TABLE [dbo].[OrderRefunds] ADD [ReturnToCustomerFee] DECIMAL(12, 0) NOT NULL DEFAULT 0;
    PRINT N'  ✓ Added column ReturnToCustomerFee to OrderRefunds.';
END
ELSE
BEGIN
    PRINT N'  - Column ReturnToCustomerFee already exists, skipped.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.OrderRefunds') AND name = 'CustomerResponseDeadline')
BEGIN
    ALTER TABLE [dbo].[OrderRefunds] ADD [CustomerResponseDeadline] DATETIME NULL;
    PRINT N'  ✓ Added column CustomerResponseDeadline to OrderRefunds.';
END
ELSE
BEGIN
    PRINT N'  - Column CustomerResponseDeadline already exists, skipped.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.OrderRefunds') AND name = 'CustomerResponse')
BEGIN
    ALTER TABLE [dbo].[OrderRefunds] ADD [CustomerResponse] NVARCHAR(50) NULL;
    PRINT N'  ✓ Added column CustomerResponse to OrderRefunds.';
END
ELSE
BEGIN
    PRINT N'  - Column CustomerResponse already exists, skipped.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.OrderRefunds') AND name = 'ReturnToCustomerFeePaid')
BEGIN
    ALTER TABLE [dbo].[OrderRefunds] ADD [ReturnToCustomerFeePaid] BIT NOT NULL DEFAULT 0;
    PRINT N'  ✓ Added column ReturnToCustomerFeePaid to OrderRefunds.';
END
ELSE
BEGIN
    PRINT N'  - Column ReturnToCustomerFeePaid already exists, skipped.';
END
GO

PRINT N'Migration 20260702_1320_AddFinancialDetails_Refunds completed successfully.';
GO
