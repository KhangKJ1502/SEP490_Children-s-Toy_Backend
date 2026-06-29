/* =================================================================
   Migration: Add Refund Shipping Fee Responsibility
   Version  : v1.0
   Date     : 2026-06-29
   Desc     : Add 5 columns to OrderRefunds, 1 column to
              OrderRefundReasons, and seed new refund reasons
              with ResponsibleParty classification.
              Safe to run multiple times
              (idempotent via IF NOT EXISTS / IF COL_LENGTH).
================================================================= */

USE [SEP490_ToyStore];
GO
SET NOCOUNT ON;
PRINT N'[Migration] Add Refund Shipping Fee Responsibility - Starting...';
GO

/* ══════════════════════════════════════════════════════════════
   STEP 1 — Add ResponsibleParty to OrderRefundReasons
══════════════════════════════════════════════════════════════ */
PRINT N'[Step 1] Adding ResponsibleParty to OrderRefundReasons...';

IF COL_LENGTH('dbo.OrderRefundReasons', 'ResponsibleParty') IS NULL
BEGIN
    ALTER TABLE [dbo].[OrderRefundReasons]
        ADD [ResponsibleParty] NVARCHAR(20) NOT NULL
            CONSTRAINT [DF_OrderRefundReasons_ResponsibleParty] DEFAULT 'Store';

    PRINT N'  ✓ Column ResponsibleParty added.';
END
ELSE
    PRINT N'  - Column ResponsibleParty already exists, skipped.';
GO

/* ══════════════════════════════════════════════════════════════
   STEP 2 — Add 5 columns to OrderRefunds
══════════════════════════════════════════════════════════════ */
PRINT N'[Step 2] Adding columns to OrderRefunds...';

IF COL_LENGTH('dbo.OrderRefunds', 'ReturnShippingFee') IS NULL
BEGIN
    ALTER TABLE [dbo].[OrderRefunds]
        ADD [ReturnShippingFee] DECIMAL(18,2) NOT NULL
            CONSTRAINT [DF_OrderRefunds_ReturnShippingFee] DEFAULT 0;
    PRINT N'  ✓ Column ReturnShippingFee added.';
END
ELSE
    PRINT N'  - Column ReturnShippingFee already exists, skipped.';

IF COL_LENGTH('dbo.OrderRefunds', 'ReturnShippingFeeBy') IS NULL
BEGIN
    ALTER TABLE [dbo].[OrderRefunds]
        ADD [ReturnShippingFeeBy] NVARCHAR(20) NOT NULL
            CONSTRAINT [DF_OrderRefunds_ReturnShippingFeeBy] DEFAULT 'Store';
    PRINT N'  ✓ Column ReturnShippingFeeBy added.';
END
ELSE
    PRINT N'  - Column ReturnShippingFeeBy already exists, skipped.';

IF COL_LENGTH('dbo.OrderRefunds', 'ReturnShippingFeeNote') IS NULL
BEGIN
    ALTER TABLE [dbo].[OrderRefunds]
        ADD [ReturnShippingFeeNote] NVARCHAR(500) NULL;
    PRINT N'  ✓ Column ReturnShippingFeeNote added.';
END
ELSE
    PRINT N'  - Column ReturnShippingFeeNote already exists, skipped.';

IF COL_LENGTH('dbo.OrderRefunds', 'FinalRefundAmount') IS NULL
BEGIN
    ALTER TABLE [dbo].[OrderRefunds]
        ADD [FinalRefundAmount] DECIMAL(18,2) NOT NULL
            CONSTRAINT [DF_OrderRefunds_FinalRefundAmount] DEFAULT 0;
    PRINT N'  ✓ Column FinalRefundAmount added.';
END
ELSE
    PRINT N'  - Column FinalRefundAmount already exists, skipped.';

IF COL_LENGTH('dbo.OrderRefunds', 'DamageResponsibility') IS NULL
BEGIN
    ALTER TABLE [dbo].[OrderRefunds]
        ADD [DamageResponsibility] NVARCHAR(20) NULL;
    PRINT N'  ✓ Column DamageResponsibility added.';
END
ELSE
    PRINT N'  - Column DamageResponsibility already exists, skipped.';
GO

/* ══════════════════════════════════════════════════════════════
   STEP 3 — Backfill FinalRefundAmount for existing records
   Existing refunds: Store covers the shipping fee
   → FinalRefundAmount = ApprovedAmount
══════════════════════════════════════════════════════════════ */
PRINT N'[Step 3] Backfilling FinalRefundAmount for existing records...';

UPDATE [dbo].[OrderRefunds]
SET [FinalRefundAmount] = [ApprovedAmount]
WHERE [FinalRefundAmount] = 0
  AND [ApprovedAmount] > 0;

PRINT N'  ✓ Backfill complete. Rows updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));
GO

/* ══════════════════════════════════════════════════════════════
   STEP 4 — Update existing RefundReason record
   Existing record:
   "Delivery failed / Unable to deliver" → Store
   (already covered by the default value, kept for consistency)
══════════════════════════════════════════════════════════════ */
PRINT N'[Step 4] Updating existing refund reason responsible party...';

UPDATE [dbo].[OrderRefundReasons]
SET [ResponsibleParty] = 'Store'
WHERE [ResponsibleParty] = 'Store'
  AND [Content] = N'Delivery failed / Unable to deliver';
GO

/* ══════════════════════════════════════════════════════════════
   STEP 5 — Seed new RefundReasons with ResponsibleParty classification
   Idempotent: insert only if Content does not already exist
══════════════════════════════════════════════════════════════ */
PRINT N'[Step 5] Seeding new RefundReason records...';

DECLARE @Now5 DATETIME2(0) = GETUTCDATE();

-- Store responsibility
IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderRefundReasons] WHERE [Content] = N'Wrong product delivered')
    INSERT INTO [dbo].[OrderRefundReasons] ([Content], [Description], [ResponsibleParty], [IsSystem], [IsDeleted], [CreatedAt])
    VALUES (N'Wrong product delivered', N'The received product does not match the order.', 'Store', 0, 0, @Now5);

IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderRefundReasons] WHERE [Content] = N'Defective / Damaged product')
    INSERT INTO [dbo].[OrderRefundReasons] ([Content], [Description], [ResponsibleParty], [IsSystem], [IsDeleted], [CreatedAt])
    VALUES (N'Defective / Damaged product', N'The product is defective or damaged due to manufacturing issues.', 'Store', 0, 0, @Now5);

IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderRefundReasons] WHERE [Content] = N'Missing item')
    INSERT INTO [dbo].[OrderRefundReasons] ([Content], [Description], [ResponsibleParty], [IsSystem], [IsDeleted], [CreatedAt])
    VALUES (N'Missing item', N'The quantity received is less than the quantity ordered.', 'Store', 0, 0, @Now5);

IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderRefundReasons] WHERE [Content] = N'Product not as described')
    INSERT INTO [dbo].[OrderRefundReasons] ([Content], [Description], [ResponsibleParty], [IsSystem], [IsDeleted], [CreatedAt])
    VALUES (N'Product not as described', N'The product differs from the images or description on the website.', 'Store', 0, 0, @Now5);

-- Customer responsibility
IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderRefundReasons] WHERE [Content] = N'No longer needed')
    INSERT INTO [dbo].[OrderRefundReasons] ([Content], [Description], [ResponsibleParty], [IsSystem], [IsDeleted], [CreatedAt])
    VALUES (N'No longer needed', N'The customer changed their mind after placing the order.', 'Customer', 0, 0, @Now5);

IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderRefundReasons] WHERE [Content] = N'Ordered the wrong product')
    INSERT INTO [dbo].[OrderRefundReasons] ([Content], [Description], [ResponsibleParty], [IsSystem], [IsDeleted], [CreatedAt])
    VALUES (N'Ordered the wrong product', N'The customer selected the wrong product when placing the order.', 'Customer', 0, 0, @Now5);

IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderRefundReasons] WHERE [Content] = N'Wrong size / color selected')
    INSERT INTO [dbo].[OrderRefundReasons] ([Content], [Description], [ResponsibleParty], [IsSystem], [IsDeleted], [CreatedAt])
    VALUES (N'Wrong size / color selected', N'The customer selected the wrong product variant.', 'Customer', 0, 0, @Now5);

PRINT N'  ✓ Seed complete.';
GO

PRINT N'[Migration] Add Refund Shipping Fee Responsibility - DONE.';
GO