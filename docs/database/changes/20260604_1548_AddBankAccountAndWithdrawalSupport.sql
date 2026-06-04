-- ============================================================
-- Người thực hiện: Antigravity
-- Ngày: 2026-06-04
-- Mô tả thay đổi: Thêm bảng BankAccounts, MockBankSystemAccounts,
--                 MockBankSystemTransactions. Cập nhật ràng buộc CHECK
--                 cho WalletPinAttempts.ActionType và WalletTransactions.TxnType
--                 để phục vụ chức năng rút tiền.
-- ============================================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

USE [SEP490_ToyStore];
GO

-- 1. Tạo bảng BankAccounts (Lưu tài khoản ngân hàng liên kết của User)
IF OBJECT_ID('dbo.BankAccounts', 'U') IS NULL
BEGIN
    CREATE TABLE [BankAccounts] (
        [BankAccountId]      INT            IDENTITY(1,1) PRIMARY KEY,
        [AccountId]          INT            NOT NULL,
        [BankName]           NVARCHAR(100)  NOT NULL,
        [BankBin]            VARCHAR(10)    NOT NULL,
        [AccountNumber]      VARCHAR(30)    NOT NULL,
        [AccountHolderName]  NVARCHAR(150)  NOT NULL,
        [IsDefault]          BIT            NOT NULL DEFAULT 0,
        [IsDeleted]          BIT            NOT NULL DEFAULT 0,
        [CreatedAt]          DATETIME2(0)   NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_BankAccounts_Accounts] FOREIGN KEY ([AccountId]) REFERENCES [Accounts]([AccountID])
    );

    CREATE NONCLUSTERED INDEX [IX_BankAccounts_AccountID]
        ON [BankAccounts]([AccountId])
        WHERE [IsDeleted] = 0;
END
GO

-- 2. Tạo bảng MockBankSystemAccounts (Giả lập số dư ngân hàng bên ngoài)
IF OBJECT_ID('dbo.MockBankSystemAccounts', 'U') IS NULL
BEGIN
    CREATE TABLE [MockBankSystemAccounts] (
        [MockBankAccountId]  INT            IDENTITY(1,1) PRIMARY KEY,
        [BankBin]            VARCHAR(10)    NOT NULL,
        [AccountNumber]      VARCHAR(30)    NOT NULL UNIQUE,
        [AccountHolderName]  NVARCHAR(150)  NOT NULL,
        [Balance]            DECIMAL(12,0)  NOT NULL DEFAULT 0 CONSTRAINT [CK_MockBankSystemAccounts_Balance] CHECK ([Balance] >= 0),
        [CreatedAt]          DATETIME2(0)   NOT NULL DEFAULT GETDATE()
    );
END
GO

-- 3. Tạo bảng MockBankSystemTransactions (Giả lập lịch sử giao dịch ngân hàng bên ngoài)
IF OBJECT_ID('dbo.MockBankSystemTransactions', 'U') IS NULL
BEGIN
    CREATE TABLE [MockBankSystemTransactions] (
        [MockBankTxnId]      INT            IDENTITY(1,1) PRIMARY KEY,
        [AccountNumber]      VARCHAR(30)    NOT NULL,
        [Amount]             DECIMAL(12,0)  NOT NULL CONSTRAINT [CK_MockBankSystemTransactions_Amount] CHECK ([Amount] > 0),
        [Direction]          CHAR(2)        NOT NULL CONSTRAINT [CK_MockBankSystemTransactions_Direction] CHECK ([Direction] IN ('CR', 'DR')),
        [Description]        NVARCHAR(255)  NULL,
        [CreatedAt]          DATETIME2(0)   NOT NULL DEFAULT GETDATE()
    );

    CREATE NONCLUSTERED INDEX [IX_MockBankSystemTransactions_AccountNumber]
        ON [MockBankSystemTransactions]([AccountNumber], [CreatedAt] DESC);
END
GO

-- 4. Cập nhật CHECK constraint cho WalletPinAttempts.ActionType (Thêm 'WITHDRAW')
ALTER TABLE [WalletPinAttempts] DROP CONSTRAINT IF EXISTS [CK_WalletPinAttempts_ActionType];
GO

DECLARE @constraintName NVARCHAR(200);
SELECT @constraintName = name
FROM sys.check_constraints
WHERE parent_object_id = OBJECT_ID('[WalletPinAttempts]')
  AND LOWER(definition) LIKE '%actiontype%'
  AND name <> 'CK_WalletPinAttempts_ActionType';
IF @constraintName IS NOT NULL
    EXEC('ALTER TABLE [WalletPinAttempts] DROP CONSTRAINT [' + @constraintName + ']');
GO

ALTER TABLE [WalletPinAttempts]
    ADD CONSTRAINT [CK_WalletPinAttempts_ActionType]
    CHECK ([ActionType] IN ('PAYMENT', 'VIEW_BALANCE', 'TOP_UP', 'WITHDRAW'));
GO

-- 5. Cập nhật CHECK constraint cho WalletTransactions.TxnType (Thêm 'Withdraw', 'Withdraw_Refund')
ALTER TABLE [WalletTransactions] DROP CONSTRAINT IF EXISTS [CK_WalletTransactions_TxnType];
GO

DECLARE @wtConstraintName NVARCHAR(200);
SELECT @wtConstraintName = name
FROM sys.check_constraints
WHERE parent_object_id = OBJECT_ID('[WalletTransactions]')
  AND LOWER(definition) LIKE '%txntype%'
  AND name <> 'CK_WalletTransactions_TxnType';
IF @wtConstraintName IS NOT NULL
    EXEC('ALTER TABLE [WalletTransactions] DROP CONSTRAINT [' + @wtConstraintName + ']');
GO

ALTER TABLE [WalletTransactions]
    ADD CONSTRAINT [CK_WalletTransactions_TxnType]
    CHECK ([TxnType] IN ('TopUp', 'Payment', 'Refund', 'Withdraw', 'Withdraw_Refund'));
GO
