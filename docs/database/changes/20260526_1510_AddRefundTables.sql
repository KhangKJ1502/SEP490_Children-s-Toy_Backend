-- Tác giả: Antigravity AI
-- Ngày tạo: 2026-05-26 15:10:00
-- Mô tả: TÁCH TRẠNG THÁI HOÀN TIỀN RA BẢNG RIÊNG (StatusRefunds) và Nâng cấp hệ thống bảng OrderRefunds.

USE [SEP490_ToyStore];
GO

PRINT 'Bắt đầu nâng cấp cấu trúc bảng hoàn tiền và tách trạng thái ra bảng StatusRefunds...';

-- 1. Tạo bảng danh mục trạng thái hoàn tiền (StatusRefunds)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[StatusRefunds]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[StatusRefunds] (
        [StatusID]    TINYINT IDENTITY(1,1) PRIMARY KEY,
        [StatusName]  VARCHAR(50) NOT NULL UNIQUE,
        [Description] NVARCHAR(255) NULL,
        [CreatedAt]   DATETIME2(0) NOT NULL DEFAULT GETDATE(),
        [UpdatedAt]   DATETIME2(0) NULL
    );
    PRINT 'Đã tạo bảng StatusRefunds thành công.';
END
GO

-- Seed dữ liệu trạng thái vào bảng StatusRefunds
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[StatusRefunds]') AND type in (N'U'))
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM [dbo].[StatusRefunds] WHERE [StatusID] = 1)
        INSERT INTO [dbo].[StatusRefunds] (StatusName, Description) VALUES ('RefundRequested', N'Khách hàng yêu cầu hoàn tiền/trả hàng');
    IF NOT EXISTS (SELECT 1 FROM [dbo].[StatusRefunds] WHERE [StatusID] = 2)
        INSERT INTO [dbo].[StatusRefunds] (StatusName, Description) VALUES ('RefundApproved', N'Yêu cầu được chấp nhận, chờ tạo vận đơn thu hồi');
    IF NOT EXISTS (SELECT 1 FROM [dbo].[StatusRefunds] WHERE [StatusID] = 3)
        INSERT INTO [dbo].[StatusRefunds] (StatusName, Description) VALUES ('RefundRejected', N'Yêu cầu bị từ chối');
    IF NOT EXISTS (SELECT 1 FROM [dbo].[StatusRefunds] WHERE [StatusID] = 4)
        INSERT INTO [dbo].[StatusRefunds] (StatusName, Description) VALUES ('RefundPickupCreated', N'Đã tạo đơn thu hồi GHN, chờ shipper lấy hàng');
    IF NOT EXISTS (SELECT 1 FROM [dbo].[StatusRefunds] WHERE [StatusID] = 5)
        INSERT INTO [dbo].[StatusRefunds] (StatusName, Description) VALUES ('RefundShipping', N'Hàng hoàn đang trên đường về kho');
    IF NOT EXISTS (SELECT 1 FROM [dbo].[StatusRefunds] WHERE [StatusID] = 6)
        INSERT INTO [dbo].[StatusRefunds] (StatusName, Description) VALUES ('RefundReceived', N'Kho đã nhận được hàng hoàn');
    IF NOT EXISTS (SELECT 1 FROM [dbo].[StatusRefunds] WHERE [StatusID] = 7)
        INSERT INTO [dbo].[StatusRefunds] (StatusName, Description) VALUES ('RefundInspectionPending', N'Hàng đang được kiểm tra chất lượng tại kho');
    IF NOT EXISTS (SELECT 1 FROM [dbo].[StatusRefunds] WHERE [StatusID] = 8)
        INSERT INTO [dbo].[StatusRefunds] (StatusName, Description) VALUES ('RefundCompleted', N'Đã hoàn tiền cho khách & nhập kho thành công');
    IF NOT EXISTS (SELECT 1 FROM [dbo].[StatusRefunds] WHERE [StatusID] = 9)
        INSERT INTO [dbo].[StatusRefunds] (StatusName, Description) VALUES ('RefundCancelled', N'Khách hàng đã hủy yêu cầu hoàn tiền');
    PRINT 'Đã seed dữ liệu trạng thái hoàn tiền.';
END
GO

-- 2. Drop các check constraint và unique index cũ trên bảng OrderRefunds
IF EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_OrderRefunds_RefundStatus' AND parent_object_id = OBJECT_ID('OrderRefunds'))
BEGIN
    ALTER TABLE [OrderRefunds] DROP CONSTRAINT [CK_OrderRefunds_RefundStatus];
    PRINT 'Đã gỡ check constraint CK_OrderRefunds_RefundStatus cũ.';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_OrderRefunds_OneActivePerOrder' AND object_id = OBJECT_ID('OrderRefunds'))
BEGIN
    DROP INDEX [UQ_OrderRefunds_OneActivePerOrder] ON [OrderRefunds];
    PRINT 'Đã gỡ bỏ unique index UQ_OrderRefunds_OneActivePerOrder cũ.';
END
GO

-- Drop index cũ phụ thuộc vào cột RefundStatus để tránh lỗi khi sửa bảng
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OrderRefunds_Order' AND object_id = OBJECT_ID('OrderRefunds'))
BEGIN
    DROP INDEX [IX_OrderRefunds_Order] ON [OrderRefunds];
    PRINT 'Đã gỡ bỏ index IX_OrderRefunds_Order cũ phụ thuộc vào cột RefundStatus.';
END
GO

-- 3. Bổ sung các cột mới vào bảng OrderRefunds hiện tại
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OrderRefunds]') AND type in (N'U'))
BEGIN
    -- Thêm cột RefundCode
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OrderRefunds') AND name = 'RefundCode')
    BEGIN
        ALTER TABLE [OrderRefunds] ADD [RefundCode] VARCHAR(30) NULL;
        PRINT 'Đã thêm cột RefundCode.';
    END

    -- Thêm cột ShippingOrderCode
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OrderRefunds') AND name = 'ShippingOrderCode')
    BEGIN
        ALTER TABLE [OrderRefunds] ADD [ShippingOrderCode] VARCHAR(50) NULL;
        PRINT 'Đã thêm cột ShippingOrderCode.';
    END

    -- Thêm cột ShippingFee
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OrderRefunds') AND name = 'ShippingFee')
    BEGIN
        ALTER TABLE [OrderRefunds] ADD [ShippingFee] DECIMAL(10,0) NOT NULL DEFAULT 0;
        PRINT 'Đã thêm cột ShippingFee.';
    END

    -- Thêm cột SubTotal
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OrderRefunds') AND name = 'SubTotal')
    BEGIN
        ALTER TABLE [OrderRefunds] ADD [SubTotal] DECIMAL(12,0) NULL;
        PRINT 'Đã thêm cột SubTotal.';
    END

    -- Thêm cột TotalAmount
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OrderRefunds') AND name = 'TotalAmount')
    BEGIN
        ALTER TABLE [OrderRefunds] ADD [TotalAmount] DECIMAL(12,0) NULL;
        PRINT 'Đã thêm cột TotalAmount.';
    END

    -- Thêm cột AdminNote
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OrderRefunds') AND name = 'AdminNote')
    BEGIN
        ALTER TABLE [OrderRefunds] ADD [AdminNote] NVARCHAR(1000) NULL;
        PRINT 'Đã thêm cột AdminNote.';
    END

    -- Thêm cột mốc thời gian chi tiết
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OrderRefunds') AND name = 'ApprovedAt')
        ALTER TABLE [OrderRefunds] ADD [ApprovedAt] DATETIME2(0) NULL;
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OrderRefunds') AND name = 'RejectedAt')
        ALTER TABLE [OrderRefunds] ADD [RejectedAt] DATETIME2(0) NULL;
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OrderRefunds') AND name = 'CompletedAt')
        ALTER TABLE [OrderRefunds] ADD [CompletedAt] DATETIME2(0) NULL;
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OrderRefunds') AND name = 'CancelledAt')
        ALTER TABLE [OrderRefunds] ADD [CancelledAt] DATETIME2(0) NULL;

    -- Thêm cột soft delete (IsDeleted)
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OrderRefunds') AND name = 'IsDeleted')
    BEGIN
        ALTER TABLE [OrderRefunds] ADD [IsDeleted] BIT NOT NULL DEFAULT 0;
        PRINT 'Đã thêm cột IsDeleted.';
    END

    -- Thêm cột StatusID (Liên kết tới bảng StatusRefunds) làm Nullable trước để di chuyển dữ liệu
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OrderRefunds') AND name = 'StatusID')
    BEGIN
        ALTER TABLE [OrderRefunds] ADD [StatusID] TINYINT NULL;
        PRINT 'Đã thêm cột StatusID.';
    END
END
GO

-- 4. Di chuyển dữ liệu trạng thái từ cột RefundStatus cũ sang cột StatusID mới
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OrderRefunds') AND name = 'RefundStatus')
BEGIN
    UPDATE [OrderRefunds] SET [StatusID] = 1 WHERE [RefundStatus] = 'Requested';
    UPDATE [OrderRefunds] SET [StatusID] = 2 WHERE [RefundStatus] = 'Approved';
    UPDATE [OrderRefunds] SET [StatusID] = 3 WHERE [RefundStatus] = 'Rejected';
    UPDATE [OrderRefunds] SET [StatusID] = 8 WHERE [RefundStatus] = 'Completed';
    UPDATE [OrderRefunds] SET [StatusID] = 9 WHERE [RefundStatus] = 'Cancelled';
    -- Đảm bảo không có dòng nào bị NULL StatusID
    UPDATE [OrderRefunds] SET [StatusID] = 1 WHERE [StatusID] IS NULL;
    PRINT 'Đã ánh xạ dữ liệu RefundStatus cũ sang StatusID thành công.';
END
GO

-- Cập nhật dữ liệu cho RefundCode của các đơn hàng cũ để tránh bị null
UPDATE [OrderRefunds]
SET [RefundCode] = 'REF-OLD-' + CAST([RefundId] AS VARCHAR(10))
WHERE [RefundCode] IS NULL;
GO

-- 5. Cấu hình các ràng buộc chính thức cho cột mới
ALTER TABLE [OrderRefunds] ALTER COLUMN [RefundCode] VARCHAR(30) NOT NULL;
ALTER TABLE [OrderRefunds] ALTER COLUMN [StatusID] TINYINT NOT NULL;

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'UQ_OrderRefunds_RefundCode' AND type = 'UQ')
BEGIN
    ALTER TABLE [OrderRefunds] ADD CONSTRAINT [UQ_OrderRefunds_RefundCode] UNIQUE ([RefundCode]);
END

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_OrderRefunds_StatusRefunds')
BEGIN
    ALTER TABLE [OrderRefunds] ADD CONSTRAINT [FK_OrderRefunds_StatusRefunds] 
        FOREIGN KEY ([StatusID]) REFERENCES [dbo].[StatusRefunds] ([StatusID]);
    PRINT 'Đã cấu hình khóa ngoại liên kết tới bảng StatusRefunds.';
END
GO

-- 6. Xóa bỏ cột RefundStatus cũ (dọn sạch technical debt)
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OrderRefunds') AND name = 'RefundStatus')
BEGIN
    ALTER TABLE [OrderRefunds] DROP COLUMN [RefundStatus];
    PRINT 'Đã xóa bỏ cột RefundStatus chuỗi cũ.';
END
GO

-- 7. Tạo bảng chi tiết sản phẩm hoàn trả (RefundDetails)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RefundDetails]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[RefundDetails] (
        [RefundDetailID]  INT IDENTITY(1,1) PRIMARY KEY,
        [RefundID]        INT NOT NULL,
        [ProductID]       INT NOT NULL,
        [Quantity]        SMALLINT NOT NULL CHECK ([Quantity] > 0),
        [UnitPrice]       DECIMAL(12,0) NOT NULL CHECK ([UnitPrice] >= 0),
        [RefundAmount]    DECIMAL(12,0) NOT NULL CHECK ([RefundAmount] >= 0),
        [CreatedAt]       DATETIME2(0) NOT NULL DEFAULT GETDATE(),
        
        CONSTRAINT [FK_RefundDetails_OrderRefunds] FOREIGN KEY ([RefundID]) REFERENCES [dbo].[OrderRefunds]([RefundID]),
        CONSTRAINT [FK_RefundDetails_Products] FOREIGN KEY ([ProductID]) REFERENCES [dbo].[Products]([ProductID]),
        CONSTRAINT [UQ_RefundDetails_RefundProduct] UNIQUE ([RefundID], [ProductID])
    );
    PRINT 'Đã tạo bảng RefundDetails liên kết tới OrderRefunds.';
END
GO

-- 8. Tạo bảng lịch sử trạng thái yêu cầu hoàn tiền (RefundStatusHistory) dùng StatusID liên kết StatusRefunds
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RefundStatusHistory]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[RefundStatusHistory] (
        [HistoryID] INT IDENTITY(1,1) PRIMARY KEY,
        [RefundID]  INT NOT NULL,
        [StatusID]  TINYINT NOT NULL,
        [ChangedBy] INT NULL,
        [Note]      NVARCHAR(500) NULL,
        [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
        
        CONSTRAINT [FK_RefundStatusHistory_OrderRefunds] FOREIGN KEY ([RefundID]) REFERENCES [dbo].[OrderRefunds]([RefundID]),
        CONSTRAINT [FK_RefundStatusHistory_StatusRefunds] FOREIGN KEY ([StatusID]) REFERENCES [dbo].[StatusRefunds]([StatusID]),
        CONSTRAINT [FK_RefundStatusHistory_ChangedBy] FOREIGN KEY ([ChangedBy]) REFERENCES [dbo].[Accounts]([AccountID])
    );
    PRINT 'Đã tạo bảng RefundStatusHistory liên kết StatusRefunds thành công.';
END
GO

-- 9. Tạo chỉ mục tìm kiếm và khôi phục index cũ
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OrderRefunds_ShippingOrderCode' AND object_id = OBJECT_ID('OrderRefunds'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_OrderRefunds_ShippingOrderCode] 
    ON [OrderRefunds]([ShippingOrderCode]) 
    WHERE [ShippingOrderCode] IS NOT NULL;
    PRINT 'Đã tạo chỉ mục tìm kiếm cho ShippingOrderCode.';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OrderRefunds_Order' AND object_id = OBJECT_ID('OrderRefunds'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_OrderRefunds_Order]
    ON [OrderRefunds]([OrderID])
    INCLUDE ([StatusID], [ApprovedAmount]);
    PRINT 'Đã tái tạo chỉ mục IX_OrderRefunds_Order sử dụng StatusID mới.';
END
GO

PRINT 'Hoàn tất nâng cấp và liên kết bảng hoàn tiền với bảng trạng thái StatusRefunds riêng.';
GO
