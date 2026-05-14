-- ============================================================
-- Author: Agent
-- Date: 2026-05-14
-- Description: Shift scheduling + auto assignment (Luong B)
--   - ShiftTemplates, WorkSchedules, StaffShiftCapacity
--   - OrderAssignments, OrderQueue
--   - Trigger to create capacity rows
--   - Stored procedures for auto-assign, release, reassign
-- ============================================================

USE [SEP490_ToyStore];
GO

-- ============================================================
-- 1. ShiftTemplates
-- ============================================================

CREATE TABLE [ShiftTemplates]
(
    [ShiftTemplateID] TINYINT IDENTITY(1,1) PRIMARY KEY,
    [ShiftName] NVARCHAR(50) NOT NULL UNIQUE,
    [StartTime] TIME(0) NOT NULL,
    [EndTime] TIME(0) NOT NULL,
    [MaxOrdersPerShift] SMALLINT NOT NULL DEFAULT 20,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(0) NULL,
    CONSTRAINT [CK_ShiftTemplates_Time] CHECK ([EndTime] > [StartTime])
);
GO

-- Seed default templates (safe insert)
IF NOT EXISTS (SELECT 1
FROM [ShiftTemplates]
WHERE [ShiftName] = N'Ca sang')
    INSERT INTO [ShiftTemplates]
    ([ShiftName], [StartTime], [EndTime])
VALUES
    (N'Ca sang', '07:00', '12:00');
IF NOT EXISTS (SELECT 1
FROM [ShiftTemplates]
WHERE [ShiftName] = N'Ca chieu')
    INSERT INTO [ShiftTemplates]
    ([ShiftName], [StartTime], [EndTime])
VALUES
    (N'Ca chieu', '12:00', '17:00');
IF NOT EXISTS (SELECT 1
FROM [ShiftTemplates]
WHERE [ShiftName] = N'Ca toi')
    INSERT INTO [ShiftTemplates]
    ([ShiftName], [StartTime], [EndTime])
VALUES
    (N'Ca toi', '17:00', '22:00');
GO

-- ============================================================
-- 2. WorkSchedules
-- ============================================================

CREATE TABLE [WorkSchedules]
(
    [ScheduleID] INT IDENTITY(1,1) PRIMARY KEY,
    [AccountID] INT NOT NULL,
    [ShiftTemplateID] TINYINT NOT NULL,
    [WorkDate] DATE NOT NULL,
    [Status] VARCHAR(20) NOT NULL DEFAULT 'Scheduled'
        CONSTRAINT [CK_WorkSchedules_Status]
        CHECK ([Status] IN ('Scheduled','OnDuty','Completed','Absent','Cancelled')),
    [CreatedBy] INT NOT NULL,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(0) NULL,
    CONSTRAINT [UQ_WorkSchedules_StaffShiftDay]
        UNIQUE ([AccountID], [WorkDate], [ShiftTemplateID]),
    CONSTRAINT [FK_WorkSchedules_Accounts]
        FOREIGN KEY ([AccountID])       REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_WorkSchedules_ShiftTemplates]
        FOREIGN KEY ([ShiftTemplateID]) REFERENCES [ShiftTemplates]([ShiftTemplateID]),
    CONSTRAINT [FK_WorkSchedules_CreatedBy]
        FOREIGN KEY ([CreatedBy])       REFERENCES [Accounts]([AccountID])
);
GO

CREATE INDEX [IX_WorkSchedules_Date_Status]
    ON [WorkSchedules] ([WorkDate], [Status])
    INCLUDE ([AccountID], [ShiftTemplateID]);
GO

-- ============================================================
-- 3. StaffShiftCapacity
-- ============================================================

CREATE TABLE [StaffShiftCapacity]
(
    [CapacityID] INT IDENTITY(1,1) PRIMARY KEY,
    [ScheduleID] INT NOT NULL UNIQUE,
    [AccountID] INT NOT NULL,
    [CurrentLoad] SMALLINT NOT NULL DEFAULT 0
        CONSTRAINT [CK_SSC_CurrentLoad_Min] CHECK ([CurrentLoad] >= 0),
    [MaxLoad] SMALLINT NOT NULL DEFAULT 20,
    [UpdatedAt] DATETIME2(0) NULL,
    CONSTRAINT [FK_SSC_Schedules] FOREIGN KEY ([ScheduleID]) REFERENCES [WorkSchedules]([ScheduleID]),
    CONSTRAINT [FK_SSC_Accounts]  FOREIGN KEY ([AccountID])  REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [CK_SSC_Load]      CHECK ([CurrentLoad] <= [MaxLoad])
);
GO

CREATE INDEX [IX_SSC_AccountID] ON [StaffShiftCapacity] ([AccountID]);
GO

-- Trigger: auto-create capacity when a schedule is created
CREATE OR ALTER TRIGGER [TR_WorkSchedules_CreateCapacity]
ON [WorkSchedules]
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [StaffShiftCapacity]
        ([ScheduleID], [AccountID], [MaxLoad])
    SELECT
        i.[ScheduleID],
        i.[AccountID],
        st.[MaxOrdersPerShift]
    FROM inserted i
        JOIN [ShiftTemplates] st ON st.[ShiftTemplateID] = i.[ShiftTemplateID];
END;
GO

-- ============================================================
-- 4. OrderAssignments
-- ============================================================

CREATE TABLE [OrderAssignments]
(
    [AssignmentID] INT IDENTITY(1,1) PRIMARY KEY,
    [OrderID] INT NOT NULL,
    [ScheduleID] INT NOT NULL,
    [AccountID] INT NOT NULL,
    [RoleID] TINYINT NOT NULL,
    -- 3=Staff | 4=Merchandise
    [IsActive] BIT NOT NULL DEFAULT 1,
    [AssignedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [AssignedBy] INT NULL,
    [Notes] NVARCHAR(200) NULL,
    CONSTRAINT [FK_OA_Orders]      FOREIGN KEY ([OrderID])    REFERENCES [Orders]([OrderID]),
    CONSTRAINT [FK_OA_Schedules]   FOREIGN KEY ([ScheduleID]) REFERENCES [WorkSchedules]([ScheduleID]),
    CONSTRAINT [FK_OA_Accounts]    FOREIGN KEY ([AccountID])  REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_OA_AssignedBy]  FOREIGN KEY ([AssignedBy]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_OA_Roles]       FOREIGN KEY ([RoleID])     REFERENCES [Roles]([RoleID]),
    CONSTRAINT [CK_OA_RoleID]      CHECK ([RoleID] IN (3, 4))
);
GO

CREATE UNIQUE INDEX [UQ_OA_ActiveOrderRole]
    ON [OrderAssignments] ([OrderID], [RoleID])
    WHERE [IsActive] = 1;
GO

CREATE INDEX [IX_OA_OrderID]        ON [OrderAssignments] ([OrderID]);
CREATE INDEX [IX_OA_AccountID_Date] ON [OrderAssignments] ([AccountID], [AssignedAt] DESC);
CREATE INDEX [IX_OA_ScheduleID]     ON [OrderAssignments] ([ScheduleID]);
GO

-- ============================================================
-- 5. OrderQueue
-- ============================================================

CREATE TABLE [OrderQueue]
(
    [QueueID] INT IDENTITY(1,1) PRIMARY KEY,
    [OrderID] INT NOT NULL UNIQUE,
    [QueuedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [Reason] VARCHAR(50) NOT NULL
        CONSTRAINT [CK_OQ_Reason] CHECK ([Reason] IN (
            'NO_STAFF_ON_DUTY','ALL_STAFF_FULL','NO_MERCH_ON_DUTY','ALL_MERCH_FULL','BOTH_FULL')),
    [AssignedBy] INT NULL,
    [ResolvedAt] DATETIME2(0) NULL,
    [IsResolved] BIT NOT NULL DEFAULT 0,
    CONSTRAINT [FK_OQ_Orders]     FOREIGN KEY ([OrderID])    REFERENCES [Orders]([OrderID]),
    CONSTRAINT [FK_OQ_AssignedBy] FOREIGN KEY ([AssignedBy]) REFERENCES [Accounts]([AccountID])
);
GO

CREATE INDEX [IX_OQ_Unresolved] ON [OrderQueue] ([IsResolved], [QueuedAt] ASC)
    WHERE [IsResolved] = 0;
GO

-- ============================================================
-- 6. Stored procedures
-- ============================================================

CREATE OR ALTER PROCEDURE [dbo].[sp_AutoAssignOrder]
    @OrderID    INT,
    @AssignedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @StaffScheduleID INT = NULL;
    DECLARE @MerchScheduleID INT = NULL;
    DECLARE @StaffAccountID  INT = NULL;
    DECLARE @MerchAccountID  INT = NULL;
    DECLARE @QueueReason     VARCHAR(50) = NULL;
    DECLARE @Now             DATETIME2(0) = GETDATE();
    DECLARE @Today           DATE    = CAST(@Now AS DATE);
    DECLARE @NowTime         TIME(0) = CAST(@Now AS TIME(0));
    DECLARE @StaffRoleId     TINYINT = 3;
    DECLARE @MerchRoleId     TINYINT = 4;

    BEGIN TRANSACTION;

    SELECT TOP 1
        @StaffScheduleID = ssc.[ScheduleID],
        @StaffAccountID  = ssc.[AccountID]
    FROM [StaffShiftCapacity] ssc WITH (UPDLOCK, ROWLOCK)
        JOIN [WorkSchedules]  ws ON ws.[ScheduleID]      = ssc.[ScheduleID]
        JOIN [ShiftTemplates] st ON st.[ShiftTemplateID] = ws.[ShiftTemplateID]
        JOIN [Accounts]       a ON a.[AccountID]        = ssc.[AccountID]
    WHERE ws.[Status]    = 'OnDuty'
        AND ws.[WorkDate]  = @Today
        AND st.[StartTime] <= @NowTime
        AND st.[EndTime]   >= @NowTime
        AND a.[RoleID]     = @StaffRoleId
        AND ssc.[CurrentLoad] < ssc.[MaxLoad]
    ORDER BY ssc.[CurrentLoad] ASC, ssc.[ScheduleID] ASC;

    SELECT TOP 1
        @MerchScheduleID = ssc.[ScheduleID],
        @MerchAccountID  = ssc.[AccountID]
    FROM [StaffShiftCapacity] ssc WITH (UPDLOCK, ROWLOCK)
        JOIN [WorkSchedules]  ws ON ws.[ScheduleID]      = ssc.[ScheduleID]
        JOIN [ShiftTemplates] st ON st.[ShiftTemplateID] = ws.[ShiftTemplateID]
        JOIN [Accounts]       a ON a.[AccountID]        = ssc.[AccountID]
    WHERE ws.[Status]    = 'OnDuty'
        AND ws.[WorkDate]  = @Today
        AND st.[StartTime] <= @NowTime
        AND st.[EndTime]   >= @NowTime
        AND a.[RoleID]     = @MerchRoleId
        AND ssc.[CurrentLoad] < ssc.[MaxLoad]
    ORDER BY ssc.[CurrentLoad] ASC, ssc.[ScheduleID] ASC;

    IF @StaffScheduleID IS NULL OR @MerchScheduleID IS NULL
    BEGIN
        SET @QueueReason =
            CASE
                WHEN @StaffScheduleID IS NULL AND @MerchScheduleID IS NULL THEN 'BOTH_FULL'
                WHEN @StaffScheduleID IS NULL THEN
                    CASE WHEN EXISTS(
                        SELECT 1
        FROM WorkSchedules ws
            JOIN Accounts a ON a.AccountID = ws.AccountID
        WHERE ws.Status = 'OnDuty' AND a.RoleID = @StaffRoleId
                    ) THEN 'ALL_STAFF_FULL' ELSE 'NO_STAFF_ON_DUTY' END
                ELSE
                    CASE WHEN EXISTS(
                        SELECT 1
        FROM WorkSchedules ws
            JOIN Accounts a ON a.AccountID = ws.AccountID
        WHERE ws.Status = 'OnDuty' AND a.RoleID = @MerchRoleId
                    ) THEN 'ALL_MERCH_FULL' ELSE 'NO_MERCH_ON_DUTY' END
            END;

        IF NOT EXISTS (SELECT 1
        FROM [OrderQueue]
        WHERE [OrderID] = @OrderID AND [IsResolved] = 0)
        BEGIN
            INSERT INTO [OrderQueue]
                ([OrderID], [Reason])
            VALUES
                (@OrderID, @QueueReason);
        END

        COMMIT TRANSACTION;

        SELECT 'QUEUED' AS [Result], @QueueReason AS [Reason], NULL AS [StaffAccountID], NULL AS [MerchAccountID];
        RETURN;
    END;

    INSERT INTO [OrderAssignments]
        ([OrderID], [ScheduleID], [AccountID], [RoleID], [AssignedBy])
    VALUES
        (@OrderID, @StaffScheduleID, @StaffAccountID, @StaffRoleId, @AssignedBy),
        (@OrderID, @MerchScheduleID, @MerchAccountID, @MerchRoleId, @AssignedBy);

    UPDATE [StaffShiftCapacity]
    SET [CurrentLoad] = [CurrentLoad] + 1, [UpdatedAt] = @Now
    WHERE [ScheduleID] IN (@StaffScheduleID, @MerchScheduleID);

    COMMIT TRANSACTION;

    SELECT 'ASSIGNED' AS [Result], NULL AS [Reason], @StaffAccountID AS [StaffAccountID], @MerchAccountID AS [MerchAccountID];
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_ReleaseOrderCapacity]
    @OrderID INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    UPDATE ssc
    SET ssc.[CurrentLoad] = CASE WHEN ssc.[CurrentLoad] > 0 THEN ssc.[CurrentLoad] - 1 ELSE 0 END,
        ssc.[UpdatedAt]   = GETDATE()
    FROM [StaffShiftCapacity] ssc
        JOIN [OrderAssignments] oa ON oa.[ScheduleID] = ssc.[ScheduleID]
    WHERE oa.[OrderID]   = @OrderID
        AND oa.[IsActive]  = 1;

    COMMIT TRANSACTION;

    SELECT @@ROWCOUNT AS [RowsAffected];
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_ReassignOrder]
    @OrderID        INT,
    @RoleID         TINYINT,
    @NewScheduleID  INT,
    @AssignedBy     INT,
    @Notes          NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @OldScheduleID INT;

    BEGIN TRANSACTION;

    SELECT @OldScheduleID = [ScheduleID]
    FROM [OrderAssignments]
    WHERE [OrderID] = @OrderID AND [RoleID] = @RoleID AND [IsActive] = 1;

    IF @OldScheduleID IS NULL
    BEGIN
        ROLLBACK TRANSACTION;
        RAISERROR('No active assignment found for this Order and Role', 16, 1);
        RETURN;
    END;

    UPDATE [OrderAssignments]
    SET [IsActive] = 0
    WHERE [OrderID] = @OrderID AND [RoleID] = @RoleID AND [IsActive] = 1;

    UPDATE [StaffShiftCapacity]
    SET [CurrentLoad] = CASE WHEN [CurrentLoad] > 0 THEN [CurrentLoad] - 1 ELSE 0 END,
        [UpdatedAt]   = GETDATE()
    WHERE [ScheduleID] = @OldScheduleID;

    INSERT INTO [OrderAssignments]
        ([OrderID], [ScheduleID], [AccountID], [RoleID], [AssignedBy], [Notes])
    SELECT @OrderID, @NewScheduleID, ws.[AccountID], @RoleID, @AssignedBy, @Notes
    FROM [WorkSchedules] ws
    WHERE ws.[ScheduleID] = @NewScheduleID;

    UPDATE [StaffShiftCapacity]
    SET [CurrentLoad] = [CurrentLoad] + 1, [UpdatedAt] = GETDATE()
    WHERE [ScheduleID] = @NewScheduleID;

    COMMIT TRANSACTION;
END;
GO
