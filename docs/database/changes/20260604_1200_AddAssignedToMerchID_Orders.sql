-- Orders: snapshot Merchandise PIC (symmetric with AssignedToStaffID)
IF COL_LENGTH('Orders', 'AssignedToMerchID') IS NULL
BEGIN
    ALTER TABLE [Orders] ADD [AssignedToMerchID] INT NULL;

    ALTER TABLE [Orders] ADD CONSTRAINT [FK_Orders_AssignedMerch]
        FOREIGN KEY ([AssignedToMerchID]) REFERENCES [Accounts]([AccountID]);
END
GO

-- Backfill from latest Merch OrderAssignment, else from status history (Processing/Shipped)
UPDATE o
SET o.[AssignedToMerchID] = src.[AccountId]
FROM [Orders] o
INNER JOIN (
    SELECT oa.[OrderID], oa.[AccountID] AS [AccountId],
           ROW_NUMBER() OVER (PARTITION BY oa.[OrderID] ORDER BY oa.[AssignedAt] DESC) AS rn
    FROM [OrderAssignments] oa
    WHERE oa.[RoleID] = 4
) src ON src.[OrderID] = o.[OrderID] AND src.rn = 1
WHERE o.[AssignedToMerchID] IS NULL;
GO

UPDATE o
SET o.[AssignedToMerchID] = h.[ChangedBy]
FROM [Orders] o
INNER JOIN (
    SELECT h.[OrderID], h.[ChangedBy],
           ROW_NUMBER() OVER (PARTITION BY h.[OrderID] ORDER BY h.[CreatedAt] DESC) AS rn
    FROM [OrderStatusHistories] h
    INNER JOIN [StatusOrders] s ON s.[StatusID] = h.[StatusID]
    WHERE h.[ChangedBy] IS NOT NULL
      AND s.[StatusName] IN ('Processing', 'Shipped')
) h ON h.[OrderID] = o.[OrderID] AND h.rn = 1
WHERE o.[AssignedToMerchID] IS NULL;
GO
