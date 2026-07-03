/* =================================================================
   Migration: Add Quantity Breakdown to RefundDetails
   Version  : v1.0
   Date     : 2026-07-01
   Desc     : Bổ sung 2 trường số lượng hàng lỗi vào RefundDetails:
              - FailedCustomerQty: hỏng do lỗi của khách hàng
              - FailedCarrierQty: hỏng do đơn vị vận chuyển
              Đồng thời thêm check constraint ràng buộc tổng số lượng:
              RestorableQuantity + FailedCustomerQty + FailedCarrierQty = Quantity
              Safe to run multiple times (idempotent).
================================================================= */

USE [SEP490_ToyStore];
GO
SET NOCOUNT ON;
PRINT N'[Migration] Add Quantity Breakdown to RefundDetails - Starting...';
GO

/* ══════════════════════════════════════════════════════════════
   STEP 1 — Add FailedCustomerQty and FailedCarrierQty columns
   ══════════════════════════════════════════════════════════════ */
PRINT N'[Step 1] Adding quantity breakdown columns to RefundDetails...';

IF COL_LENGTH('dbo.RefundDetails', 'FailedCustomerQty') IS NULL
BEGIN
    ALTER TABLE [dbo].[RefundDetails]
        ADD [FailedCustomerQty] SMALLINT NOT NULL CONSTRAINT [DF_RefundDetails_FailedCustomerQty] DEFAULT 0;

    PRINT N'  ✓ Column FailedCustomerQty added.';
END
ELSE
    PRINT N'  - Column FailedCustomerQty already exists, skipped.';

IF COL_LENGTH('dbo.RefundDetails', 'FailedCarrierQty') IS NULL
BEGIN
    ALTER TABLE [dbo].[RefundDetails]
        ADD [FailedCarrierQty] SMALLINT NOT NULL CONSTRAINT [DF_RefundDetails_FailedCarrierQty] DEFAULT 0;

    PRINT N'  ✓ Column FailedCarrierQty added.';
END
ELSE
    PRINT N'  - Column FailedCarrierQty already exists, skipped.';
GO

/* ══════════════════════════════════════════════════════════════
   STEP 2 — Thêm Check Constraint ràng buộc tổng số lượng
   ══════════════════════════════════════════════════════════════ */
PRINT N'[Step 2] Creating check constraint CK_RefundDetails_QtySum...';

-- Xóa check cũ nếu có
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_RefundDetails_QtySum' AND parent_object_id = OBJECT_ID('dbo.RefundDetails'))
BEGIN
    ALTER TABLE [dbo].[RefundDetails] DROP CONSTRAINT [CK_RefundDetails_QtySum];
    PRINT N'  - Dropped existing check constraint CK_RefundDetails_QtySum.';
END

ALTER TABLE [dbo].[RefundDetails]
    ADD CONSTRAINT [CK_RefundDetails_QtySum]
    CHECK (
        [FailedCustomerQty] >= 0 AND 
        [FailedCarrierQty] >= 0 AND 
        ([RestorableQuantity] IS NULL OR ([RestorableQuantity] + [FailedCustomerQty] + [FailedCarrierQty] <= [Quantity]))
    );

PRINT N'  ✓ Check constraint CK_RefundDetails_QtySum created.';
GO

/* ══════════════════════════════════════════════════════════════
   STEP 3 — Dọn dẹp cột cũ ItemInspectionResult nếu đã tạo trước đó
   ══════════════════════════════════════════════════════════════ */
PRINT N'[Step 3] Cleaning up old ItemInspectionResult column if exists...';

-- Xóa check constraint cũ của ItemInspectionResult trước
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_RefundDetails_ItemInspectionResult' AND parent_object_id = OBJECT_ID('dbo.RefundDetails'))
BEGIN
    ALTER TABLE [dbo].[RefundDetails] DROP CONSTRAINT [CK_RefundDetails_ItemInspectionResult];
    PRINT N'  ✓ Dropped constraint CK_RefundDetails_ItemInspectionResult.';
END

-- Xóa cột
IF COL_LENGTH('dbo.RefundDetails', 'ItemInspectionResult') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[RefundDetails] DROP COLUMN [ItemInspectionResult];
    PRINT N'  ✓ Column ItemInspectionResult dropped.';
END
ELSE
    PRINT N'  - Column ItemInspectionResult did not exist, skipped.';
GO

/* ══════════════════════════════════════════════════════════════
   STEP 4 — Backfill: đảm bảo RestorableQuantity = Quantity cho các dòng cũ
   ══════════════════════════════════════════════════════════════ */
PRINT N'[Step 4] Ensuring RestorableQuantity matches Quantity for older rows...';

UPDATE [dbo].[RefundDetails]
SET    [RestorableQuantity] = [Quantity]
WHERE  [RestorableQuantity] IS NULL;

PRINT N'  ✓ Backfill completed. Rows updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));
GO

PRINT N'[Migration] Add Quantity Breakdown to RefundDetails - DONE.';
GO
