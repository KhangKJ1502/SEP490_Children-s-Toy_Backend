-- =================================================================================
-- Description: Thêm trạng thái 'PARTIALLY_REFUNDED' vào CK_Orders_PaymentStatus
-- Date: 2026-05-26
-- Author: Antigravity AI
-- =================================================================================

-- 1. Xóa CHECK constraint cũ trên bảng Orders
IF EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_Orders_PaymentStatus' AND parent_object_id = OBJECT_ID('Orders'))
BEGIN
    ALTER TABLE [Orders] DROP CONSTRAINT [CK_Orders_PaymentStatus];
END
GO

-- 2. Tạo lại CHECK constraint mới bao gồm 'PARTIALLY_REFUNDED'
ALTER TABLE [Orders] ADD CONSTRAINT [CK_Orders_PaymentStatus] 
    CHECK ([PaymentStatus] IN ('PENDING', 'PAID', 'FAILED', 'EXPIRED', 'REFUNDED', 'PARTIALLY_REFUNDED', 'COD_PENDING', 'CANCELLED'));
GO
