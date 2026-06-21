-- ============================================================
-- Migration: Add RefundType, quality check fields, and return statuses
-- Date    : 2026-06-21
-- ============================================================

-- 1. Insert new status values into StatusRefunds
SET IDENTITY_INSERT [dbo].[StatusRefunds] ON;

IF NOT EXISTS (SELECT 1 FROM [dbo].[StatusRefunds] WHERE StatusID = 11)
    INSERT INTO [dbo].[StatusRefunds] (StatusID, StatusName, Description)
    VALUES (11, 'RefundReturnShipmentCreated', N'Đã tạo vận đơn giao trả hàng cho khách');

IF NOT EXISTS (SELECT 1 FROM [dbo].[StatusRefunds] WHERE StatusID = 12)
    INSERT INTO [dbo].[StatusRefunds] (StatusID, StatusName, Description)
    VALUES (12, 'RefundReturningToCustomer', N'Đơn hàng đang được shipper giao trả lại khách');

IF NOT EXISTS (SELECT 1 FROM [dbo].[StatusRefunds] WHERE StatusID = 13)
    INSERT INTO [dbo].[StatusRefunds] (StatusID, StatusName, Description)
    VALUES (13, 'RefundReturnedToCustomer', N'Khách đã nhận lại hàng giao trả thành công');

IF NOT EXISTS (SELECT 1 FROM [dbo].[StatusRefunds] WHERE StatusID = 14)
    INSERT INTO [dbo].[StatusRefunds] (StatusID, StatusName, Description)
    VALUES (14, 'RefundReturnToCustomerFailed', N'Giao trả hàng cho khách thất bại');

SET IDENTITY_INSERT [dbo].[StatusRefunds] OFF;
GO

-- 2. Add RefundType column to OrderRefunds
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('OrderRefunds') AND name = 'RefundType')
BEGIN
    ALTER TABLE OrderRefunds
    ADD RefundType NVARCHAR(20) NULL;
END
GO

-- 3. Backfill RefundType with default value
UPDATE OrderRefunds
SET RefundType = 'ReturnAndRefund'
WHERE RefundType IS NULL;
GO

-- 4. Alter column to NOT NULL with default constraint
ALTER TABLE OrderRefunds
ALTER COLUMN RefundType NVARCHAR(20) NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE parent_object_id = OBJECT_ID('OrderRefunds') AND name = 'DF_OrderRefunds_RefundType')
BEGIN
    ALTER TABLE OrderRefunds
    ADD CONSTRAINT DF_OrderRefunds_RefundType DEFAULT 'ReturnAndRefund' FOR RefundType;
END
GO

-- 5. Add quality check notes and return shipment tracking columns
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('OrderRefunds') AND name = 'InspectionNote')
BEGIN
    ALTER TABLE OrderRefunds
    ADD InspectionNote NVARCHAR(500) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('OrderRefunds') AND name = 'InspectionPassed')
BEGIN
    ALTER TABLE OrderRefunds
    ADD InspectionPassed BIT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('OrderRefunds') AND name = 'ReturnShippingOrderCode')
BEGIN
    ALTER TABLE OrderRefunds
    ADD ReturnShippingOrderCode VARCHAR(50) NULL;
END
GO

-- 6. Create index for ReturnShippingOrderCode filter
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('OrderRefunds') AND name = 'IX_OrderRefunds_ReturnShippingOrderCode')
BEGIN
    CREATE NONCLUSTERED INDEX IX_OrderRefunds_ReturnShippingOrderCode
        ON OrderRefunds (ReturnShippingOrderCode)
        WHERE ReturnShippingOrderCode IS NOT NULL;
END
GO

PRINT 'Migration complete: RefundType, quality check fields, and return statuses added.';
