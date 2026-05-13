/* =================================================================
   Add Inactive Status to Promotions and PromotionTimeSlots
   Created at: 2026-05-13
================================================================= */

USE [SEP490_ToyStore];
GO

-- Drop existing constraints
ALTER TABLE [dbo].[Promotions] DROP CONSTRAINT [CK_Promotions_Status];
GO

ALTER TABLE [dbo].[PromotionTimeSlots] DROP CONSTRAINT [CK_PromotionTimeSlots_Status];
GO

-- Recreate constraints with 'Inactive' status included
ALTER TABLE [dbo].[Promotions] 
ADD CONSTRAINT [CK_Promotions_Status] 
CHECK ([Status] IN ('Scheduled', 'Active', 'Inactive', 'Expired'));
GO

ALTER TABLE [dbo].[PromotionTimeSlots] 
ADD CONSTRAINT [CK_PromotionTimeSlots_Status] 
CHECK ([Status] IN ('Scheduled', 'Active', 'Inactive', 'Expired'));
GO
