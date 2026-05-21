/*
  SHIFT FULL NOTIFICATION TRACKING — one admin alert per schedule when CurrentLoad reaches MaxLoad.
  Idempotent ALTER (safe to run if column already exists).
*/
IF COL_LENGTH(N'dbo.StaffShiftCapacity', N'ShiftFullNotifiedAt') IS NULL
BEGIN
    ALTER TABLE dbo.StaffShiftCapacity
        ADD ShiftFullNotifiedAt DATETIME2(3) NULL;
END
GO
