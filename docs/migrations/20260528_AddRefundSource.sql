-- ============================================================
-- Migration: Add RefundSource column to OrderRefunds
-- Purpose : Phân biệt rõ Luồng A (Customer) vs Luồng B (System)
-- Date    : 2026-05-28
-- ============================================================

-- 1. Thêm cột RefundSource (nullable trước để backfill an toàn)
ALTER TABLE OrderRefunds
ADD RefundSource NVARCHAR(20) NULL;
GO

-- 2. Backfill: refund do hệ thống tạo (RequestedBy IS NULL + ReasonDetails chứa "[SYSTEM]" hoặc "Auto-created")
UPDATE OrderRefunds
SET RefundSource = 'System'
WHERE RequestedBy IS NULL
  AND (
      ReasonDetails LIKE '%Auto-created%'
   OR ReasonDetails LIKE '%[SYSTEM]%'
  );
GO

-- 3. Backfill: tất cả còn lại là Customer
UPDATE OrderRefunds
SET RefundSource = 'Customer'
WHERE RefundSource IS NULL;
GO

-- 4. Đổi sang NOT NULL với default
ALTER TABLE OrderRefunds
ALTER COLUMN RefundSource NVARCHAR(20) NOT NULL;
GO

ALTER TABLE OrderRefunds
ADD CONSTRAINT DF_OrderRefunds_RefundSource DEFAULT 'Customer' FOR RefundSource;
GO

-- 5. (Optional) Index để filter nhanh theo RefundSource
CREATE NONCLUSTERED INDEX IX_OrderRefunds_RefundSource
    ON OrderRefunds (RefundSource)
    INCLUDE (OrderId, StatusId, CreatedAt);
GO

PRINT 'Migration complete: RefundSource column added and backfilled.';
