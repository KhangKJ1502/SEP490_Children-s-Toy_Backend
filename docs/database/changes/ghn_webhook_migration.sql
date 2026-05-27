-- 1. Thêm StatusOrders còn thiếu
IF NOT EXISTS (SELECT 1 FROM [dbo].[StatusOrders] WHERE StatusID = 12)
BEGIN
    SET IDENTITY_INSERT [dbo].[StatusOrders] ON;
    INSERT INTO [dbo].[StatusOrders] (StatusID, StatusName, Description) VALUES
    (12, 'DeliveryFailed', N'Giao thất bại, chờ xử lý'),
    (13, 'WaitingReturn',  N'Chờ hoàn hàng về shop'),
    (14, 'ReturnFailed',   N'Hoàn hàng thất bại'),
    (15, 'Lost',           N'Hàng bị mất trong vận chuyển'),
    (16, 'Damaged',        N'Hàng bị hư hỏng');
    SET IDENTITY_INSERT [dbo].[StatusOrders] OFF;
END
GO

-- 2. Thêm cột vào Orders
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[dbo].[Orders]') AND name = 'FailedDeliveryAt')
BEGIN
    ALTER TABLE [dbo].[Orders]
        ADD [FailedDeliveryAt]   DATETIME2(0) NULL,
            [ReturnedAt]         DATETIME2(0) NULL,
            [LastGHNFailCode]    VARCHAR(20)  NULL,
            [DeliveryFailCount]  TINYINT      NOT NULL DEFAULT 0;
END
GO
