-- ============================================================
-- Nguoi thuc hien: System
-- Ngay: 2026-05-11
-- Mo ta thay doi: Them bang WalletPins va WalletPinAttempts
--                 phuc vu quan ly ma PIN vi va tracking lan nhap PIN
-- ============================================================

USE [SEP409_ToyStore];
GO

CREATE TABLE [WalletPins] (
    [WalletPinID]         INT          IDENTITY(1,1) PRIMARY KEY,
    [WalletID]            INT          NOT NULL,
    [PinHash]             VARCHAR(255) NOT NULL,
    [IsActive]            BIT          NOT NULL DEFAULT 1,
    [FailedAttempts]      TINYINT      NOT NULL DEFAULT 0,
    [TotalFailedAttempts] TINYINT      NOT NULL DEFAULT 0,
    [LockedUntil]         DATETIME2(0) NULL,
    [LastChangedAt]       DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [CreatedAt]           DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]           DATETIME2(0) NULL,
    CONSTRAINT [FK_WalletPins_Wallets]        FOREIGN KEY ([WalletID]) REFERENCES [Wallets]([WalletID]),
    CONSTRAINT [CK_WalletPins_FailedAttempts] CHECK ([FailedAttempts] BETWEEN 0 AND 3),
    CONSTRAINT [CK_WalletPins_TotalFailed]    CHECK ([TotalFailedAttempts] BETWEEN 0 AND 6)
);
GO

CREATE UNIQUE INDEX [UQ_WalletPins_WalletID]
ON [WalletPins]([WalletID])
WHERE [IsActive] = 1;
GO

CREATE TABLE [WalletPinAttempts] (
    [AttemptID]  INT          IDENTITY(1,1) PRIMARY KEY,
    [WalletID]   INT          NOT NULL,
    [AccountID]  INT          NOT NULL,
    [ActionType] VARCHAR(20)  NOT NULL
        CONSTRAINT [CK_WalletPinAttempts_ActionType]
            CHECK ([ActionType] IN ('PAYMENT', 'VIEW_BALANCE', 'TOP_UP')),
    [IsSuccess]  BIT          NOT NULL,
    [CreatedAt]  DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_WalletPinAttempts_Wallets]  FOREIGN KEY ([WalletID]) REFERENCES [Wallets]([WalletID]),
    CONSTRAINT [FK_WalletPinAttempts_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID])
);
GO

CREATE NONCLUSTERED INDEX [IX_WalletPinAttempts_Wallet]
ON [WalletPinAttempts]([WalletID], [CreatedAt] DESC);
GO
