USE [SEP490_ToyStore];
GO

-- Drop indexes that depend on IsActive column
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductPromotions_ProductID_Active' AND object_id = OBJECT_ID('[dbo].[ProductPromotions]'))
    DROP INDEX [IX_ProductPromotions_ProductID_Active] ON [dbo].[ProductPromotions];
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductPromotions_PromotionID' AND object_id = OBJECT_ID('[dbo].[ProductPromotions]'))
    DROP INDEX [IX_ProductPromotions_PromotionID] ON [dbo].[ProductPromotions];
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PromotionProductSlots_Slot_Active' AND object_id = OBJECT_ID('[dbo].[PromotionProductSlots]'))
    DROP INDEX [IX_PromotionProductSlots_Slot_Active] ON [dbo].[PromotionProductSlots];
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PromotionProductSlots_Product' AND object_id = OBJECT_ID('[dbo].[PromotionProductSlots]'))
    DROP INDEX [IX_PromotionProductSlots_Product] ON [dbo].[PromotionProductSlots];
GO

-- Drop column IsActive from ProductPromotions
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[dbo].[ProductPromotions]') AND name = 'IsActive')
BEGIN
    DECLARE @ConstraintName1 NVARCHAR(200);
    SELECT @ConstraintName1 = name FROM sys.default_constraints 
    WHERE parent_object_id = OBJECT_ID('[dbo].[ProductPromotions]') AND parent_column_id = COLUMNPROPERTY(parent_object_id, 'IsActive', 'ColumnId');
    IF @ConstraintName1 IS NOT NULL
        EXEC('ALTER TABLE [dbo].[ProductPromotions] DROP CONSTRAINT [' + @ConstraintName1 + ']');
    
    ALTER TABLE [dbo].[ProductPromotions] DROP COLUMN [IsActive];
END
GO

-- Drop column IsActive from PromotionProductSlots
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[dbo].[PromotionProductSlots]') AND name = 'IsActive')
BEGIN
    DECLARE @ConstraintName2 NVARCHAR(200);
    SELECT @ConstraintName2 = name FROM sys.default_constraints 
    WHERE parent_object_id = OBJECT_ID('[dbo].[PromotionProductSlots]') AND parent_column_id = COLUMNPROPERTY(parent_object_id, 'IsActive', 'ColumnId');
    IF @ConstraintName2 IS NOT NULL
        EXEC('ALTER TABLE [dbo].[PromotionProductSlots] DROP CONSTRAINT [' + @ConstraintName2 + ']');
    
    ALTER TABLE [dbo].[PromotionProductSlots] DROP COLUMN [IsActive];
END
GO

-- Re-create indexes without IsActive column
CREATE NONCLUSTERED INDEX [IX_ProductPromotions_ProductID_Active]
    ON [dbo].[ProductPromotions] ([ProductID])
    INCLUDE ([SalePrice], [PromotionID]);
GO

CREATE NONCLUSTERED INDEX [IX_ProductPromotions_PromotionID]
    ON [dbo].[ProductPromotions] ([PromotionID])
    INCLUDE ([ProductID], [SalePrice]);
GO

CREATE NONCLUSTERED INDEX [IX_PromotionProductSlots_Slot_Active]
    ON [dbo].[PromotionProductSlots] ([TimeSlotID])
    INCLUDE ([ProductID], [SalePrice], [SaleQuantity], [SoldQuantity]);
GO

CREATE NONCLUSTERED INDEX [IX_PromotionProductSlots_Product]
    ON [dbo].[PromotionProductSlots] ([ProductID])
    INCLUDE ([TimeSlotID], [SalePrice]);
GO
