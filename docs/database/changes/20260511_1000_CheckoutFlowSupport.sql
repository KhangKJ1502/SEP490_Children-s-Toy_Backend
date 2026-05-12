-- ============================================================
-- Người thực hiện: Agent
-- Ngày: 2026-05-11
-- Mô tả: Checkout flow support:
--   1. Thêm COD_PENDING vào CHECK constraint Orders.PaymentStatus
--   2. Thêm COD_PENDING vào CHECK constraint PaymentHistory.PaymentStatus
--   3. Thêm cột PromotionID + SlotProductID vào OrderDetails (audit promotion/flash-sale)
-- ============================================================

USE [SEP490_ToyStore];
GO

-- ============================================================
-- 1. Mở rộng CHECK constraint Orders.PaymentStatus
--    Thêm 'COD_PENDING' vào danh sách giá trị hợp lệ
-- ============================================================

-- Xóa constraint cũ
ALTER TABLE [Orders] DROP CONSTRAINT IF EXISTS [CK_Orders_PaymentStatus];
GO

-- (EF scaffold dùng tên có thể khác — xóa cả tên default nếu có)
DECLARE @constraintName NVARCHAR(200);
SELECT @constraintName = name
FROM sys.check_constraints
WHERE parent_object_id = OBJECT_ID('[Orders]')
  AND LOWER(definition) LIKE '%paymentstatus%'
  AND name <> 'CK_Orders_PaymentStatus'; -- tránh xóa lại cái vừa drop
IF @constraintName IS NOT NULL
    EXEC('ALTER TABLE [Orders] DROP CONSTRAINT [' + @constraintName + ']');
GO

ALTER TABLE [Orders]
    ADD CONSTRAINT [CK_Orders_PaymentStatus]
    CHECK ([PaymentStatus] IN ('PENDING', 'PAID', 'FAILED', 'EXPIRED', 'REFUNDED', 'COD_PENDING'));
GO

-- ============================================================
-- 2. Mở rộng CHECK constraint PaymentHistory.PaymentStatus
-- ============================================================

ALTER TABLE [PaymentHistory] DROP CONSTRAINT IF EXISTS [CK_PaymentHistory_PaymentStatus];
GO

DECLARE @phConstraintName NVARCHAR(200);
SELECT @phConstraintName = name
FROM sys.check_constraints
WHERE parent_object_id = OBJECT_ID('[PaymentHistory]')
  AND LOWER(definition) LIKE '%paymentstatus%'
  AND name <> 'CK_PaymentHistory_PaymentStatus';
IF @phConstraintName IS NOT NULL
    EXEC('ALTER TABLE [PaymentHistory] DROP CONSTRAINT [' + @phConstraintName + ']');
GO

ALTER TABLE [PaymentHistory]
    ADD CONSTRAINT [CK_PaymentHistory_PaymentStatus]
    CHECK ([PaymentStatus] IN ('PENDING', 'PAID', 'FAILED', 'EXPIRED', 'REFUNDED', 'COD_PENDING'));
GO

-- ============================================================
-- 3. Thêm cột audit PromotionID và SlotProductID vào OrderDetails
--    Nullable — chỉ set khi sản phẩm đang trong chương trình khuyến mãi/flash-sale
-- ============================================================

ALTER TABLE [OrderDetails] ADD [PromotionID] INT NULL;
GO

ALTER TABLE [OrderDetails] ADD [SlotProductID] INT NULL;
GO

-- FK tới Promotions nếu bảng tồn tại
IF OBJECT_ID('dbo.Promotions', 'U') IS NOT NULL
BEGIN
    ALTER TABLE [OrderDetails]
        ADD CONSTRAINT [FK_OrderDetails_Promotions]
        FOREIGN KEY ([PromotionID]) REFERENCES [Promotions]([PromotionID]);
END
GO

-- FK tới PromotionProductSlots nếu bảng tồn tại
IF OBJECT_ID('dbo.PromotionProductSlots', 'U') IS NOT NULL
BEGIN
    ALTER TABLE [OrderDetails]
        ADD CONSTRAINT [FK_OrderDetails_PromotionProductSlots]
        FOREIGN KEY ([SlotProductID]) REFERENCES [PromotionProductSlots]([SlotProductID]);
END
GO

-- Index hỗ trợ audit theo promotion
CREATE NONCLUSTERED INDEX [IX_OrderDetails_PromotionID]
    ON [OrderDetails]([PromotionID])
    WHERE [PromotionID] IS NOT NULL;
GO
