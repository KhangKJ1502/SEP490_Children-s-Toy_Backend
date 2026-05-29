-- Migration: Add EditCount to [dbo].[CustomerChildren]
-- Purpose: Track number of edits per child profile.

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.CustomerChildren', 'EditCount') IS NULL
BEGIN
    ALTER TABLE [dbo].[CustomerChildren]
    ADD [EditCount] INT NOT NULL CONSTRAINT [DF_CustomerChildren_EditCount] DEFAULT (0);
END
GO
