/* =================================================================
   Migration: GHN Return Inspection Flow
   Version  : v1.0
   Date     : 2026-06-30
   Desc     : Mở rộng luồng hoàn tiền GHN thất bại (System Return):
              - Thêm RestorableQuantity vào RefundDetails
                (Merchandise đánh giá SL nhập kho lại theo từng sản phẩm)
              - Thêm CustomerShippingPaid + IncludeShippingInRefund
                vào OrderRefunds
                (Staff chọn có hoàn phí ship hay không khi Complete)
              Safe to run multiple times (idempotent via IF COL_LENGTH).
================================================================= */

USE [SEP490_ToyStore];
GO
SET NOCOUNT ON;
PRINT N'[Migration] GHN Return Inspection Flow - Starting...';
GO

/* ══════════════════════════════════════════════════════════════
   STEP 1 — Add RestorableQuantity to RefundDetails
   Default = Quantity (full restock assumption, Merchandise điều chỉnh)
══════════════════════════════════════════════════════════════ */
PRINT N'[Step 1] Adding RestorableQuantity to RefundDetails...';

IF COL_LENGTH('dbo.RefundDetails', 'RestorableQuantity') IS NULL
BEGIN
    ALTER TABLE [dbo].[RefundDetails]
        ADD [RestorableQuantity] SMALLINT NULL;

    PRINT N'  ✓ Column RestorableQuantity added.';
END
ELSE
    PRINT N'  - Column RestorableQuantity already exists, skipped.';
GO

-- Backfill: set RestorableQuantity = Quantity for existing rows
UPDATE [dbo].[RefundDetails]
SET    [RestorableQuantity] = [Quantity]
WHERE  [RestorableQuantity] IS NULL;

PRINT N'  ✓ Backfill RestorableQuantity = Quantity for existing rows done. Rows: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));
GO

/* ══════════════════════════════════════════════════════════════
   STEP 2 — Add CustomerShippingPaid to OrderRefunds
   Snapshot lúc tạo system refund: tiền ship khách thực trả
══════════════════════════════════════════════════════════════ */
PRINT N'[Step 2] Adding CustomerShippingPaid to OrderRefunds...';

IF COL_LENGTH('dbo.OrderRefunds', 'CustomerShippingPaid') IS NULL
BEGIN
    ALTER TABLE [dbo].[OrderRefunds]
        ADD [CustomerShippingPaid] DECIMAL(12,0) NOT NULL
            CONSTRAINT [DF_OrderRefunds_CustomerShippingPaid] DEFAULT 0;

    PRINT N'  ✓ Column CustomerShippingPaid added.';
END
ELSE
    PRINT N'  - Column CustomerShippingPaid already exists, skipped.';
GO

/* ══════════════════════════════════════════════════════════════
   STEP 3 — Add IncludeShippingInRefund to OrderRefunds
   NULL = chưa xác định (refund chưa Complete hoặc customer return)
   1 = Staff chọn hoàn phí ship
   0 = Staff chọn không hoàn phí ship
══════════════════════════════════════════════════════════════ */
PRINT N'[Step 3] Adding IncludeShippingInRefund to OrderRefunds...';

IF COL_LENGTH('dbo.OrderRefunds', 'IncludeShippingInRefund') IS NULL
BEGIN
    ALTER TABLE [dbo].[OrderRefunds]
        ADD [IncludeShippingInRefund] BIT NULL;

    PRINT N'  ✓ Column IncludeShippingInRefund added.';
END
ELSE
    PRINT N'  - Column IncludeShippingInRefund already exists, skipped.';
GO

/* ══════════════════════════════════════════════════════════════
   STEP 4 — Notification templates for new system return flow
══════════════════════════════════════════════════════════════ */
PRINT N'[Step 4] Upserting notification templates...';

-- Update MERCH_RETURNED to link to refund page instead of order page
IF EXISTS (SELECT 1
FROM [Notification].[Templates]
WHERE TemplateCode = 'MERCH_RETURNED' AND IsDeleted = 0)
BEGIN
    UPDATE [Notification].[Templates]
    SET [TitleTemplate]   = N'GHN package returned to warehouse',
        [MessageTemplate] = N'Order {{OrderCode}} has been returned by GHN. Please confirm receipt and inspect the items.',
        [UpdatedAt]         = GETUTCDATE()
    WHERE TemplateCode = 'MERCH_RETURNED' AND IsDeleted = 0;

    PRINT N'  ✓ MERCH_RETURNED template updated.';
END
ELSE
    PRINT N'  - MERCH_RETURNED template not found, skipped.';

-- New template: notify Staff when Merchandise finishes inspection
IF NOT EXISTS (SELECT 1
FROM [Notification].[Templates]
WHERE TemplateCode = 'STAFF_SYSTEM_REFUND_READY' AND IsDeleted = 0)
BEGIN
    INSERT INTO [Notification].[Templates]
        (TemplateCode, UsageScope, TitleTemplate, MessageTemplate, IsDeleted, CreatedAt)
    VALUES
        ('STAFF_SYSTEM_REFUND_READY', 'SYSTEM',
            N'Inspection complete — refund pending',
            N'Merchandise has inspected the returned items for order {{OrderCode}}. Please confirm the wallet refund.',
            0, GETUTCDATE());

    PRINT N'  ✓ STAFF_SYSTEM_REFUND_READY template inserted.';
END
ELSE
    PRINT N'  - STAFF_SYSTEM_REFUND_READY template already exists, skipped.';
GO

PRINT N'[Migration] GHN Return Inspection Flow - DONE.';
GO
