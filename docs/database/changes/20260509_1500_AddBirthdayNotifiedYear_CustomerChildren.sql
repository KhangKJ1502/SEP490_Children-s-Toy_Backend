-- ============================================================
-- Migration: Add BirthdayNotifiedYear to [dbo].[CustomerChildren]
-- Date: 2026-05-09
-- Author: SEP490 Team
-- Purpose: Track which year a child's birthday notification was
--          last sent to prevent duplicate sends on job retry.
--          Logic: send only when BirthdayNotifiedYear IS NULL
--                 OR BirthdayNotifiedYear < YEAR(GETDATE()).
-- ============================================================

ALTER TABLE [dbo].[CustomerChildren]
ADD [BirthdayNotifiedYear] SMALLINT NULL;
GO
