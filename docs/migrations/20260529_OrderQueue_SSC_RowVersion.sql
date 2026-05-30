-- BUG-03: Allow re-queue after resolve — filtered unique on unresolved OrderQueue rows
-- BUG-04: Optimistic concurrency on StaffShiftCapacity

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_OQ_OrderID' AND object_id = OBJECT_ID(N'dbo.OrderQueue'))
BEGIN
    ALTER TABLE [dbo].[OrderQueue] DROP CONSTRAINT [UQ_OQ_OrderID];
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_OQ_OrderID_Unresolved' AND object_id = OBJECT_ID(N'dbo.OrderQueue'))
BEGIN
    CREATE UNIQUE INDEX [UQ_OQ_OrderID_Unresolved] ON [dbo].[OrderQueue]([OrderID])
        WHERE [IsResolved] = 0;
END
GO

IF COL_LENGTH('dbo.StaffShiftCapacity', 'RowVersion') IS NULL
BEGIN
    ALTER TABLE [dbo].[StaffShiftCapacity]
        ADD [RowVersion] ROWVERSION NOT NULL;
END
GO
