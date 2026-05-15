/* =================================================================
   E-COMMERCE DATABASE SCHEMA (OPTIMIZED FULL VERSION + AI MODERATION)
   Platform: SQL Server | Version: 3.2
  
================================================================= */

USE [master];
GO
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'SEP490_ToyStore')
BEGIN
    CREATE DATABASE [SEP490_ToyStore];
END
GO
USE [SEP490_ToyStore];
GO

/* =============================================
   0. SETUP SCHEMAS & SYSTEM TABLES
============================================= */
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'Notification') EXEC('CREATE SCHEMA [Notification]')
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'Interaction')   EXEC('CREATE SCHEMA [Interaction]')
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'Recommendation') EXEC('CREATE SCHEMA [Recommendation]')
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'System')         EXEC('CREATE SCHEMA [System]')
GO

CREATE TABLE [System].[DomainEventOutbox] (
    [EventID]          UNIQUEIDENTIFIER PRIMARY KEY,
    [AggregateType]    VARCHAR(100) NOT NULL,
    [AggregateId]      VARCHAR(100) NOT NULL,
    [EventType]        VARCHAR(100) NOT NULL,
    [Payload]          NVARCHAR(1000) NOT NULL DEFAULT '{}',
    [OccurredOn]       DATETIME2(0) NOT NULL,
    [ProcessingLockId] UNIQUEIDENTIFIER NULL,
    [ProcessingAt]     DATETIME2(0) NULL,
    [Attempts]         TINYINT NOT NULL DEFAULT 0,
    [LastError]        NVARCHAR(MAX) NULL,
    [ProcessedOn]      DATETIME2(0) NULL
);
GO

CREATE TABLE [System].[BackgroundJobs] (
    [JobID]          INT IDENTITY(1,1) PRIMARY KEY,
    [JobName]        VARCHAR(100) NOT NULL UNIQUE,
    [CronExpression] VARCHAR(50) NULL,
    [IsEnabled]      BIT NOT NULL DEFAULT 1,
    [LastRunTime]    DATETIME2(0) NULL,
    [NextRunTime]    DATETIME2(0) NULL,
    [LastRunStatus]  VARCHAR(20) NULL,
    [LastRunMessage] NVARCHAR(MAX) NULL
);
GO

/* =============================================
   1. USER MANAGEMENT (IAM)
============================================= */
CREATE TABLE [Roles] (
    [RoleID]      TINYINT IDENTITY(1,1) PRIMARY KEY,
    [RoleName]    VARCHAR(50) NOT NULL UNIQUE,
    [Description] NVARCHAR(255) NULL,
    [CreatedAt]   DATETIME2(0) NOT NULL DEFAULT GETDATE()
);
GO

CREATE TABLE [Sexes] (
    [SexID]    TINYINT IDENTITY(1,1) PRIMARY KEY,
    [SexName]  NVARCHAR(4) NOT NULL UNIQUE,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE()
);
GO

CREATE TABLE [Accounts] (
    [AccountID]    INT IDENTITY(1,1) PRIMARY KEY,
    [RoleID]       TINYINT NOT NULL,
    [SexID]        TINYINT NULL,
    [EmployeeCode] VARCHAR(20) NULL,
    [AccountName]  NVARCHAR(100) NOT NULL,
    [PhoneNumber]  VARCHAR(15) NULL,
    [Email]        VARCHAR(100) NOT NULL UNIQUE,
    [DOB]          DATE NULL,
    [ImageURL]     VARCHAR(500) NULL,
    [PasswordHash] VARCHAR(255) NOT NULL,
    [IsActive]     BIT NOT NULL DEFAULT 1,
    [IsDeleted]    BIT NOT NULL DEFAULT 0,
    [Provider]     VARCHAR(20) NULL,
    [CreatedAt]    DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]    DATETIME2(0) NULL,
    CONSTRAINT [FK_Accounts_Roles]    FOREIGN KEY ([RoleID]) REFERENCES [Roles]([RoleID]),
    CONSTRAINT [FK_Accounts_Sexes]    FOREIGN KEY ([SexID])     REFERENCES [dbo].[Sexes]([SexID]),
    CONSTRAINT [CK_Accounts_DOB]      CHECK ([DOB] <= CAST(GETDATE() AS DATE))
);
GO

CREATE TABLE [BlockReasons] (
    [BlockReasonID] TINYINT IDENTITY(1,1) PRIMARY KEY,
    [Content]       NVARCHAR(150) NOT NULL,
    [Description]   NVARCHAR(255) NULL,
    [IsDeleted]     BIT NOT NULL DEFAULT 0,
    [CreatedAt]     DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]     DATETIME2(0) NULL
);
GO

CREATE TABLE [UserBlockHistory] (
    [BlockID]          INT IDENTITY(1,1) PRIMARY KEY,
    [AccountID]        INT NOT NULL,
    [BlockedBy]        INT NULL,
    [BlockReasonID]    TINYINT NOT NULL,
    [Note]             NVARCHAR(500) NULL,
    [UnblockedBy]      INT NULL,
    [UnblockedByJobID] INT NULL,
    [BlockedAt]        DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [BlockedUntil]     DATETIME2(0) NOT NULL,
    [UnblockedAt]      DATETIME2(0) NULL,
    [UpdatedAt]        DATETIME2(0) NULL,
    CONSTRAINT [FK_UserBlockHistory_Account]        FOREIGN KEY ([AccountID])        REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_UserBlockHistory_BlockedBy]      FOREIGN KEY ([BlockedBy])        REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_UserBlockHistory_UnblockedBy]    FOREIGN KEY ([UnblockedBy])      REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_UserBlockHistory_BlockReasons]   FOREIGN KEY ([BlockReasonID])  REFERENCES [BlockReasons]([BlockReasonID]),
    CONSTRAINT [FK_UserBlockHistory_BackgroundJobs] FOREIGN KEY ([UnblockedByJobID]) REFERENCES [System].[BackgroundJobs]([JobID]),
    CONSTRAINT [CK_UserBlockHistory_ValidPeriod]           CHECK ([BlockedUntil] > [BlockedAt]),
    CONSTRAINT [CK_UserBlockHistory_UnblockedAfterBlocked] CHECK ([UnblockedAt] IS NULL OR [UnblockedAt] >= [BlockedAt]),
    CONSTRAINT [CK_UserBlockHistory_OneUnblockActor]       CHECK ([UnblockedBy] IS NULL OR [UnblockedByJobID] IS NULL)
);
GO

/* =============================================
   1.1. ADMINISTRATIVE DIVISIONS
============================================= */
CREATE TABLE Provinces (
    [ProvinceId]   INT PRIMARY KEY,
    [ProvinceName] NVARCHAR(100) NOT NULL,
    [ProvinceCode] VARCHAR(10) NULL,
    [IsActive]     BIT NOT NULL DEFAULT 1,
    [CreatedAt]    DATETIME2(0) DEFAULT GETDATE(),
    [UpdatedAt]    DATETIME2(0) NULL
);
GO

CREATE TABLE Districts (
    [DistrictId]   INT PRIMARY KEY,
    [ProvinceId]   INT NOT NULL,
    [DistrictName] NVARCHAR(100) NOT NULL,
    [IsActive]     BIT NOT NULL DEFAULT 1,
    [CreatedAt]    DATETIME2(0) DEFAULT GETDATE(),
    [UpdatedAt]    DATETIME2(0) NULL,
    CONSTRAINT FK_Districts_Provinces FOREIGN KEY (ProvinceId) REFERENCES Provinces(ProvinceId)
);
GO

CREATE INDEX IX_Districts_ProvinceId ON Districts(ProvinceId);
GO

CREATE TABLE Wards (
    [WardCode]   VARCHAR(20) PRIMARY KEY,
    [DistrictId] INT NOT NULL,
    [WardName]   NVARCHAR(100) NOT NULL,
    [IsActive]   BIT NOT NULL DEFAULT 1,
    [CreatedAt]  DATETIME2(0) DEFAULT GETDATE(),
    CONSTRAINT FK_Wards_Districts FOREIGN KEY (DistrictId) REFERENCES Districts(DistrictId)
);
GO

CREATE INDEX IX_Wards_DistrictId ON Wards(DistrictId);
GO

CREATE TABLE [dbo].[Addresses] (
    [AddressID]     INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [AccountID]     INT           NOT NULL,
    [RecipientName] NVARCHAR(100) NULL,
    [PhoneNumber]   NVARCHAR(20)  NULL,
    [AddressLine]   NVARCHAR(500) NOT NULL,
    [WardCode]      VARCHAR(20)   NULL,
    [DistrictId]    INT           NULL,
    [ProvinceId]    INT           NULL,
    [IsDefault]     BIT           NOT NULL DEFAULT 0,
    [IsDeleted]     BIT           NOT NULL DEFAULT 0,
    [CreatedAt]     DATETIME2(0)  NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]     DATETIME2(0)  NULL,
    CONSTRAINT [FK_Addresses_Accounts]  FOREIGN KEY ([AccountID])  REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_Addresses_Wards]     FOREIGN KEY ([WardCode])   REFERENCES [Wards]([WardCode]),
    CONSTRAINT [FK_Addresses_Districts] FOREIGN KEY ([DistrictId]) REFERENCES [Districts]([DistrictId]),
    CONSTRAINT [FK_Addresses_Provinces] FOREIGN KEY ([ProvinceId]) REFERENCES [Provinces]([ProvinceId])
);
GO

/* =============================================
   2. PRODUCT CATALOG (PIM)
============================================= */
CREATE TABLE [SuperCategories] (
    [SuperCategoryID]   SMALLINT IDENTITY(1,1) PRIMARY KEY,
    [SuperCategoryName] NVARCHAR(25) NOT NULL UNIQUE,
    [IsDeleted]         BIT NOT NULL DEFAULT 0,
    [CreatedAt]         DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]         DATETIME2(0) NULL
);
GO

CREATE TABLE [Categories] (
    [CategoryID]      SMALLINT IDENTITY(1,1) PRIMARY KEY,
    [SuperCategoryID] SMALLINT NOT NULL,
    [CategoryName]    NVARCHAR(25) NOT NULL UNIQUE,
    [IsDeleted]       BIT NOT NULL DEFAULT 0,
    [CreatedAt]       DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]       DATETIME2(0) NULL,
    CONSTRAINT [FK_Categories_SuperCategories] FOREIGN KEY ([SuperCategoryID]) REFERENCES [SuperCategories]([SuperCategoryID])
);
GO

CREATE TABLE [Materials] (
    [MaterialID]   SMALLINT IDENTITY(1,1) PRIMARY KEY,
    [MaterialName] NVARCHAR(25) NOT NULL UNIQUE,
    [IsDeleted]    BIT NOT NULL DEFAULT 0,
    [CreatedAt]    DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]    DATETIME2(0) NULL
);
GO

CREATE TABLE [Ages] (
    [AgeID]    TINYINT IDENTITY(1,1) PRIMARY KEY,
    [AgeRange] VARCHAR(50) NOT NULL UNIQUE,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE()
);
GO

CREATE TABLE [Origins] (
    [OriginID]   TINYINT IDENTITY(1,1) PRIMARY KEY,
    [OriginName] NVARCHAR(100) NOT NULL UNIQUE,
    [CreatedAt]  DATETIME2(0) NOT NULL DEFAULT GETDATE()
);
GO

CREATE TABLE [Brands] (
    [BrandID]   SMALLINT IDENTITY(1,1) PRIMARY KEY,
    [BrandName] NVARCHAR(100) NOT NULL UNIQUE,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(0) NULL
);
GO

CREATE TABLE [PriceRanges] (
    [PriceRangeID]  TINYINT IDENTITY(1,1) PRIMARY KEY,
    [PriceRangeMin] DECIMAL(12,0) NOT NULL CHECK ([PriceRangeMin] >= 0),
    [PriceRangeMax] DECIMAL(12,0) NOT NULL,
    [CreatedAt]     DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [CK_PriceRanges_MinMax] CHECK ([PriceRangeMin] < [PriceRangeMax])
);
GO

CREATE TABLE [Promotions] (
    [PromotionID]   INT IDENTITY(1,1) PRIMARY KEY,
    [CreatedBy]     INT NOT NULL,
    [PromotionName] NVARCHAR(200) NOT NULL,
    [PromotionType] VARCHAR(20) NOT NULL
        CONSTRAINT [CK_Promotions_Type] CHECK ([PromotionType] IN ('FLASH_SALE', 'DISCOUNT')),
    [Description]   NVARCHAR(MAX) NULL,
    [StartDate]     DATETIME2(0) NOT NULL,
    [EndDate]       DATETIME2(0) NOT NULL,
    [Status]        VARCHAR(20) NOT NULL DEFAULT 'Scheduled'
        CONSTRAINT [CK_Promotions_Status] CHECK ([Status] IN ('Scheduled', 'Active', 'Expired', 'Inactive')),
    [Priority]      INT NOT NULL DEFAULT 0,
    [IsDeleted]     BIT NOT NULL DEFAULT 0,
    [CreatedAt]     DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]     DATETIME2(0) NULL,
    CONSTRAINT [FK_Promotions_Accounts]  FOREIGN KEY ([CreatedBy]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [CK_Promotions_DateRange] CHECK ([StartDate] < [EndDate])
);
GO

CREATE INDEX [IX_Promotions_Status_Time] ON [Promotions] ([Status], [StartDate], [EndDate]) INCLUDE ([Priority]);
CREATE INDEX [IX_Promotions_Priority]    ON [Promotions] ([Priority] DESC);
GO

CREATE TABLE [Products] (
    [ProductID]                   INT           IDENTITY(1,1) PRIMARY KEY,
    [CategoryID]                  SMALLINT      NOT NULL,
    [BrandID]                     SMALLINT      NULL,
    [PriceRangeID]                TINYINT       NULL,
    [ProductName]                 NVARCHAR(255) NOT NULL,
    [Price]                       DECIMAL(12,0) NOT NULL CHECK ([Price] >= 0),
    [Quantity]                    INT           NOT NULL CHECK ([Quantity] >= 0),
    [ProductStatus]               VARCHAR(20)   NOT NULL
        CHECK ([ProductStatus] IN ('Active', 'Inactive', 'OutOfStock', 'Discontinued', 'ComingSoon')),
    [LaunchDate]                  DATETIME2(0)  NULL,
    [IsDeleted]                   BIT           NOT NULL DEFAULT 0,
    [StockThreshold]              SMALLINT      NOT NULL DEFAULT 10,
    [LowStockNotificationEnabled] BIT           NOT NULL DEFAULT 1,
    [LastLowStockNotifiedAt]      DATETIME2(0)  NULL,
    [CreatedAt]                   DATETIME2(0)  NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]                   DATETIME2(0)  NULL,
    CONSTRAINT [FK_Products_Categories]  FOREIGN KEY ([CategoryID])   REFERENCES [Categories]([CategoryID]),
    CONSTRAINT [FK_Products_Brands]      FOREIGN KEY ([BrandID])      REFERENCES [Brands]([BrandID]),
    CONSTRAINT [FK_Products_PriceRanges] FOREIGN KEY ([PriceRangeID]) REFERENCES [PriceRanges]([PriceRangeID])
);
GO

/* -------------------------------------------------------
   PromotionTimeSlots  – chỉ dùng cho FLASH_SALE
   StartAt / EndAt lưu theo UTC (DATETIME2(0))
------------------------------------------------------- */
CREATE TABLE [PromotionTimeSlots] (
    [TimeSlotID]  INT IDENTITY(1,1) PRIMARY KEY,
    [PromotionID] INT NOT NULL,
    [StartAt]     DATETIME2(0) NOT NULL,   -- thời điểm bắt đầu slot (UTC)
    [EndAt]       DATETIME2(0) NOT NULL,   -- thời điểm kết thúc slot (UTC)
    [Status]      VARCHAR(20) NOT NULL DEFAULT 'Scheduled'
        CONSTRAINT [CK_PromotionTimeSlots_Status] CHECK ([Status] IN ('Scheduled', 'Active', 'Expired', 'Inactive')),
    [IsDeleted]     BIT NOT NULL DEFAULT 0,
    [CreatedAt]   DATETIME2(0) NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]   DATETIME2(0) NULL,
    CONSTRAINT [FK_PromotionTimeSlots_Promotions] FOREIGN KEY ([PromotionID]) REFERENCES [Promotions]([PromotionID]),
    CONSTRAINT [CK_PromotionTimeSlots_Range]      CHECK ([StartAt] < [EndAt]),
    CONSTRAINT [UQ_PromotionTimeSlots_UniqueSlot] UNIQUE ([PromotionID], [StartAt], [EndAt])
);
GO

/* Tìm slot đang active nhanh theo khoảng thời gian UTC */
CREATE INDEX [IX_PromotionTimeSlots_Active]
    ON [PromotionTimeSlots] ([Status], [StartAt], [EndAt])
    INCLUDE ([PromotionID]);
CREATE INDEX [IX_PromotionTimeSlots_Promotion]
    ON [PromotionTimeSlots] ([PromotionID], [Status])
    INCLUDE ([StartAt], [EndAt]);
GO

/* -------------------------------------------------------
   ProductPromotions  – chỉ dùng cho DISCOUNT
   Gắn sản phẩm với promotion DISCOUNT, lưu giá sale
   và số lượng ở cấp promotion (không phân slot)
------------------------------------------------------- */
CREATE TABLE [ProductPromotions] (
    [ProductID]        INT           NOT NULL,
    [PromotionID]      INT           NOT NULL,
    [SalePrice]        DECIMAL(12,2) NOT NULL CHECK ([SalePrice] > 0),
    [DiscountPercent]  DECIMAL(5,2)  NULL CHECK ([DiscountPercent] BETWEEN 0 AND 100),
    [SaleQuantity]     INT           NULL,
    [SoldQuantity]     INT           NOT NULL DEFAULT 0,
    [ReservedQuantity] INT           NOT NULL DEFAULT 0,
    [IsActive]         BIT           NOT NULL DEFAULT 1,
    [IsDeleted]     BIT NOT NULL DEFAULT 0,
    [CreatedAt]        DATETIME2(0)  NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]        DATETIME2(0)  NULL,
    CONSTRAINT [PK_ProductPromotions]           PRIMARY KEY ([ProductID], [PromotionID]),
    CONSTRAINT [FK_ProductPromotions_Products]   FOREIGN KEY ([ProductID])   REFERENCES [Products]([ProductID]),
    CONSTRAINT [FK_ProductPromotions_Promotions] FOREIGN KEY ([PromotionID]) REFERENCES [Promotions]([PromotionID]),
    CONSTRAINT [CK_ProductPromotions_Inventory]  CHECK ([SaleQuantity] IS NULL OR ([SoldQuantity] + [ReservedQuantity] <= [SaleQuantity]))
);
GO

CREATE INDEX [IX_ProductPromotions_ProductID_Active]
    ON [ProductPromotions] ([ProductID], [IsActive])
    INCLUDE ([SalePrice], [PromotionID]);
CREATE INDEX [IX_ProductPromotions_PromotionID]
    ON [ProductPromotions] ([PromotionID])
    INCLUDE ([ProductID], [SalePrice], [IsActive]);
GO

/* -------------------------------------------------------
   PromotionProductSlots  – chỉ dùng cho FLASH_SALE
   Liên kết một slot cụ thể với từng sản phẩm tham gia.
   Mỗi dòng quy định giá sale và số lượng riêng cho slot đó.
------------------------------------------------------- */
CREATE TABLE [PromotionProductSlots] (
    [SlotProductID]    INT           IDENTITY(1,1) PRIMARY KEY,
    [TimeSlotID]       INT           NOT NULL,
    [ProductID]        INT           NOT NULL,
    [SalePrice]        DECIMAL(12,2) NOT NULL CHECK ([SalePrice] > 0),
    [DiscountPercent]  DECIMAL(5,2)  NULL CHECK ([DiscountPercent] BETWEEN 0 AND 100),
    [SaleQuantity]     INT           NOT NULL CHECK ([SaleQuantity] > 0),
    [SoldQuantity]     INT           NOT NULL DEFAULT 0,
    [ReservedQuantity] INT           NOT NULL DEFAULT 0,
    [IsActive]         BIT           NOT NULL DEFAULT 1,
    [IsDeleted]     BIT NOT NULL DEFAULT 0,
    [CreatedAt]        DATETIME2(0)  NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]        DATETIME2(0)  NULL,
    CONSTRAINT [UQ_PromotionProductSlots_SlotProduct]   UNIQUE ([TimeSlotID], [ProductID]),
    CONSTRAINT [FK_PromotionProductSlots_TimeSlot]      FOREIGN KEY ([TimeSlotID])  REFERENCES [PromotionTimeSlots]([TimeSlotID]),
    CONSTRAINT [FK_PromotionProductSlots_Products]      FOREIGN KEY ([ProductID])   REFERENCES [Products]([ProductID]),
    CONSTRAINT [CK_PromotionProductSlots_Inventory]     CHECK ([SoldQuantity] + [ReservedQuantity] <= [SaleQuantity])
);
GO

/* Truy vấn sản phẩm flash-sale đang active theo slot */
CREATE INDEX [IX_PromotionProductSlots_Slot_Active]
    ON [PromotionProductSlots] ([TimeSlotID], [IsActive])
    INCLUDE ([ProductID], [SalePrice], [SaleQuantity], [SoldQuantity]);
/* Truy vấn ngược: sản phẩm đang tham gia slot nào */
CREATE INDEX [IX_PromotionProductSlots_Product]
    ON [PromotionProductSlots] ([ProductID], [IsActive])
    INCLUDE ([TimeSlotID], [SalePrice]);
GO

CREATE TABLE [ProductDetails] (
    [ProductID]   INT      NOT NULL PRIMARY KEY,
    [Description] NVARCHAR(1500) NULL,
    [MaterialID]  SMALLINT NULL,
    [AgeID]       TINYINT  NULL,
    [SexID]       TINYINT  NULL,
    [OriginID]    TINYINT  NULL,
    CONSTRAINT [FK_ProductDetails_Products]  FOREIGN KEY ([ProductID])  REFERENCES [Products]([ProductID]),
    CONSTRAINT [FK_ProductDetails_Materials] FOREIGN KEY ([MaterialID]) REFERENCES [Materials]([MaterialID]),
    CONSTRAINT [FK_ProductDetails_Ages]      FOREIGN KEY ([AgeID])      REFERENCES [Ages]([AgeID]),
    CONSTRAINT [FK_ProductDetails_Sexes]     FOREIGN KEY ([SexID])      REFERENCES [Sexes]([SexID]),
    CONSTRAINT [FK_ProductDetails_Origins]   FOREIGN KEY ([OriginID])   REFERENCES [Origins]([OriginID])
);
GO

CREATE TABLE [ProductImages] (
    [ImageID]   INT IDENTITY(1,1) PRIMARY KEY,
    [ProductID] INT NOT NULL,
    [ImageUrl]  VARCHAR(500) NOT NULL,
    [IsMain]    BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(0) NULL,
    CONSTRAINT [FK_ProductImages_Products] FOREIGN KEY ([ProductID]) REFERENCES [Products]([ProductID])
);
GO

CREATE UNIQUE INDEX [UQ_ProductImages_OneMain]
ON [ProductImages]([ProductID])
WHERE ([IsMain] = 1);
GO

/* =============================================
   3. ORDER MANAGEMENT (OMS)
============================================= */
CREATE TABLE [StatusOrders] (
    [StatusID]    TINYINT IDENTITY(1,1) PRIMARY KEY,
    [StatusName]  VARCHAR(50) NOT NULL UNIQUE,
    [Description] NVARCHAR(255) NULL,
    [CreatedAt]   DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]   DATETIME2(0) NULL
);
GO

CREATE TABLE [Orders] (
    [OrderID]               INT           IDENTITY(1,1) PRIMARY KEY,
    [AccountID]             INT           NOT NULL,
    [StatusID]              TINYINT       NOT NULL,
    [AssignedToStaffID]     INT           NULL,
    [OrderCode]             VARCHAR(30)   NOT NULL UNIQUE,
    [ShippingOrderCode]     VARCHAR(50)   NULL,
    [ShippingName]          NVARCHAR(100) NOT NULL,
    [ShippingPhone]         VARCHAR(15)   NOT NULL,
    [ShippingAddress]       NVARCHAR(500) NOT NULL,
    [ShippingWardCode]      VARCHAR(20)   NOT NULL,
    [ShippingWardName]      NVARCHAR(100) NOT NULL,
    [ShippingDistrictId]    INT           NOT NULL,
    [ShippingDistrictName]  NVARCHAR(100) NOT NULL,
    [ShippingProvinceId]    INT           NOT NULL,
    [ShippingProvinceName]  NVARCHAR(100) NOT NULL,
    [OrderDate]             DATETIME2(0)  NOT NULL DEFAULT GETDATE(),
    [ConfirmedAt]           DATETIME2(0)  NULL,
    [ShippedAt]             DATETIME2(0)  NULL,
    [DeliveredAt]           DATETIME2(0)  NULL,
    [CompletedAt]           DATETIME2(0)  NULL,
    [CancelledAt]           DATETIME2(0)  NULL,
    [PaymentMethod] VARCHAR(20) NOT NULL DEFAULT 'SHIP_COD'
             CHECK ([PaymentMethod] IN ('BANK_TRANSFER', 'SHIP_COD', 'SE_PAY', 'WALLET')),
    /* ── v3.2: thêm 'COD_PENDING' ── */
    [PaymentStatus]         VARCHAR(20)   NOT NULL DEFAULT 'PENDING'
        CONSTRAINT [CK_Orders_PaymentStatus]
        CHECK ([PaymentStatus] IN ('PENDING', 'PAID', 'FAILED', 'EXPIRED', 'REFUNDED', 'COD_PENDING')),
    [PaymentCode]           VARCHAR(50)   NULL UNIQUE,
    [PaidAt]                DATETIME2(0)  NULL,
    [SubTotal]              DECIMAL(12,0) NOT NULL,
    [VoucherDiscountAmount] DECIMAL(12,0) NOT NULL DEFAULT 0,
    [EstimatedShippingFee]  DECIMAL(10,0) NOT NULL,
    [ActualShippingFee]     DECIMAL(10,0) NULL,
    [TotalAmount]           DECIMAL(12,0) NOT NULL,
    [CancelReason]          NVARCHAR(500) NULL,
    [CancelledBy]           INT           NULL,
    [Note]                  NVARCHAR(1000) NULL,
    [IsDeleted]             BIT           NOT NULL DEFAULT 0,
    [CreatedAt]             DATETIME2(0)  NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]             DATETIME2(0)  NULL,
    CONSTRAINT FK_Orders_Accounts      FOREIGN KEY ([AccountID])         REFERENCES [Accounts]([AccountID]),
    CONSTRAINT FK_Orders_StatusOrders  FOREIGN KEY ([StatusID])          REFERENCES [StatusOrders]([StatusID]),
    CONSTRAINT FK_Orders_AssignedStaff FOREIGN KEY ([AssignedToStaffID]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT FK_Orders_CancelledBy   FOREIGN KEY ([CancelledBy])       REFERENCES [Accounts]([AccountID]),
    CONSTRAINT CK_Orders_Amounts CHECK (
        [SubTotal] >= 0 AND [EstimatedShippingFee] >= 0
        AND ([ActualShippingFee] IS NULL OR [ActualShippingFee] >= 0)
        AND [TotalAmount] >= 0
    ),
    CONSTRAINT CK_Orders_Timestamps CHECK (
        ([ConfirmedAt]  IS NULL OR [ConfirmedAt]  >= [OrderDate])
        AND ([ShippedAt]   IS NULL OR [ConfirmedAt]  IS NOT NULL)
        AND ([DeliveredAt] IS NULL OR [ShippedAt]    IS NOT NULL)
        AND ([CompletedAt] IS NULL OR [DeliveredAt]  IS NOT NULL)
        AND ([ShippedAt]   IS NULL OR [ShippedAt]    >= [ConfirmedAt])
        AND ([DeliveredAt] IS NULL OR [DeliveredAt]  >= [ShippedAt])
        AND ([CompletedAt] IS NULL OR [CompletedAt]  >= [DeliveredAt])
        AND NOT ([CompletedAt] IS NOT NULL AND [CancelledAt] IS NOT NULL)
    )
);
GO

/* ── v3.2: OrderDetails với PromotionID + SlotProductID ── */
CREATE TABLE [OrderDetails] (
    [OrderDetailID]  INT IDENTITY(1,1) PRIMARY KEY,
    [OrderID]        INT NOT NULL,
    [ProductID]      INT NOT NULL,
    [ProductName]    NVARCHAR(255) NOT NULL,
    [ProductImage]   VARCHAR(500) NULL,
    [Quantity]       SMALLINT NOT NULL CHECK ([Quantity] > 0),
    [UnitPrice]      DECIMAL(12,0) NOT NULL CHECK ([UnitPrice] >= 0),
    [DiscountAmount] DECIMAL(12,0) NOT NULL DEFAULT 0 CHECK ([DiscountAmount] >= 0),
    [LineTotal] AS (IIF([Quantity] * [UnitPrice] - [DiscountAmount] < 0, 0, [Quantity] * [UnitPrice] - [DiscountAmount])) PERSISTED,
    [PromotionID]    INT NULL,   -- FK → Promotions(PromotionID), NULL nếu không áp dụng KM
    [SlotProductID]  INT NULL,   -- FK → PromotionProductSlots(SlotProductID), NULL nếu không phải flash-sale
    [CreatedAt]      DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]      DATETIME2(0) NULL,
    CONSTRAINT [FK_OrderDetails_Orders]                 FOREIGN KEY ([OrderID])        REFERENCES [Orders]([OrderID]),
    CONSTRAINT [FK_OrderDetails_Products]               FOREIGN KEY ([ProductID])      REFERENCES [Products]([ProductID]),
    CONSTRAINT [FK_OrderDetails_Promotions]             FOREIGN KEY ([PromotionID])    REFERENCES [Promotions]([PromotionID]),
    CONSTRAINT [FK_OrderDetails_PromotionProductSlots]  FOREIGN KEY ([SlotProductID])  REFERENCES [PromotionProductSlots]([SlotProductID]),
    CONSTRAINT [UQ_OrderDetails_OrderProduct]           UNIQUE ([OrderID], [ProductID]),
    CONSTRAINT [CK_OrderDetails_DiscountNotExceed]      CHECK ([DiscountAmount] <= [Quantity] * [UnitPrice])
);
GO

CREATE TABLE [ProductFollowers] (
    [FollowerID] INT          IDENTITY(1,1) PRIMARY KEY,
    [ProductID]  INT          NOT NULL,
    [AccountID]  INT          NOT NULL,
    [NotifiedAt] DATETIME2(0) NULL,
    [CreatedAt]  DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [UQ_ProductFollowers_ProductAccount] UNIQUE ([ProductID], [AccountID]),
    CONSTRAINT [FK_ProductFollowers_Products]       FOREIGN KEY ([ProductID]) REFERENCES [Products]([ProductID]),
    CONSTRAINT [FK_ProductFollowers_Accounts]       FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID])
);
GO

CREATE NONCLUSTERED INDEX [IX_ProductFollowers_Pending]
ON [ProductFollowers] ([ProductID], [NotifiedAt])
WHERE [NotifiedAt] IS NULL;
GO

CREATE NONCLUSTERED INDEX [IX_ProductFollowers_Account]
ON [ProductFollowers] ([AccountID])
INCLUDE ([ProductID], [CreatedAt]);
GO

CREATE TABLE [OrderStatusHistory] (
    [HistoryID] INT          IDENTITY(1,1) PRIMARY KEY,
    [OrderID]   INT          NOT NULL,
    [StatusID]  TINYINT      NOT NULL,
    [ChangedBy] INT          NULL,
    [Note]      NVARCHAR(500) NULL,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_OrderStatusHistory_Orders    FOREIGN KEY ([OrderID])   REFERENCES [Orders]([OrderID]),
    CONSTRAINT FK_OrderStatusHistory_Status    FOREIGN KEY ([StatusID])  REFERENCES [StatusOrders]([StatusID]),
    CONSTRAINT FK_OrderStatusHistory_ChangedBy FOREIGN KEY ([ChangedBy]) REFERENCES [Accounts]([AccountID])
);
GO

/* =============================================
   3.1 SHIFT SCHEDULING & ASSIGNMENT
============================================= */
CREATE TABLE [ShiftTemplates] (
    [ShiftTemplateID]   TINYINT       IDENTITY(1,1) PRIMARY KEY,
    [ShiftName]         NVARCHAR(50)  NOT NULL UNIQUE,
    [StartTime]         TIME(0)       NOT NULL,
    [EndTime]           TIME(0)       NOT NULL,
    [MaxOrdersPerShift] SMALLINT      NOT NULL DEFAULT 20,
    [IsActive]          BIT           NOT NULL DEFAULT 1,
    [CreatedAt]         DATETIME2(0)  NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]         DATETIME2(0)  NULL,
    CONSTRAINT [CK_ShiftTemplates_Time] CHECK ([EndTime] > [StartTime])
);
GO

CREATE TABLE [WorkSchedules] (
    [ScheduleID]      INT          IDENTITY(1,1) PRIMARY KEY,
    [AccountID]       INT          NOT NULL,
    [ShiftTemplateID] TINYINT      NOT NULL,
    [WorkDate]        DATE         NOT NULL,
    [Status]          VARCHAR(20)  NOT NULL DEFAULT 'Scheduled'
        CONSTRAINT [CK_WorkSchedules_Status]
        CHECK ([Status] IN ('Scheduled','OnDuty','Completed','Absent','Cancelled')),
    [CreatedBy]       INT          NOT NULL,
    [CreatedAt]       DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]       DATETIME2(0) NULL,
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

CREATE TABLE [StaffShiftCapacity] (
    [CapacityID]  INT          IDENTITY(1,1) PRIMARY KEY,
    [ScheduleID]  INT          NOT NULL UNIQUE,
    [AccountID]   INT          NOT NULL,
    [CurrentLoad] SMALLINT     NOT NULL DEFAULT 0
        CONSTRAINT [CK_SSC_CurrentLoad_Min] CHECK ([CurrentLoad] >= 0),
    [MaxLoad]     SMALLINT     NOT NULL DEFAULT 20,
    [UpdatedAt]   DATETIME2(0) NULL,
    CONSTRAINT [FK_SSC_Schedules] FOREIGN KEY ([ScheduleID]) REFERENCES [WorkSchedules]([ScheduleID]),
    CONSTRAINT [FK_SSC_Accounts]  FOREIGN KEY ([AccountID])  REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [CK_SSC_Load]      CHECK ([CurrentLoad] <= [MaxLoad])
);
GO

CREATE INDEX [IX_SSC_AccountID] ON [StaffShiftCapacity] ([AccountID]);
GO

CREATE OR ALTER TRIGGER [TR_WorkSchedules_CreateCapacity]
ON [WorkSchedules]
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [StaffShiftCapacity] ([ScheduleID], [AccountID], [MaxLoad])
    SELECT
        i.[ScheduleID],
        i.[AccountID],
        st.[MaxOrdersPerShift]
    FROM inserted i
    JOIN [ShiftTemplates] st ON st.[ShiftTemplateID] = i.[ShiftTemplateID];
END;
GO

CREATE TABLE [OrderAssignments] (
    [AssignmentID]  INT           IDENTITY(1,1) PRIMARY KEY,
    [OrderID]       INT           NOT NULL,
    [ScheduleID]    INT           NOT NULL,
    [AccountID]     INT           NOT NULL,
    [RoleID]        TINYINT       NOT NULL,        -- 3=Staff | 4=Merchandise
    [IsActive]      BIT           NOT NULL DEFAULT 1,
    [AssignedAt]    DATETIME2(0)  NOT NULL DEFAULT GETDATE(),
    [AssignedBy]    INT           NULL,
    [Notes]         NVARCHAR(200) NULL,
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

CREATE TABLE [OrderQueue] (
    [QueueID]      INT          IDENTITY(1,1) PRIMARY KEY,
    [OrderID]      INT          NOT NULL UNIQUE,
    [QueuedAt]     DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [Reason]       VARCHAR(50)  NOT NULL
        CONSTRAINT [CK_OQ_Reason] CHECK ([Reason] IN (
            'NO_STAFF_ON_DUTY','ALL_STAFF_FULL','NO_MERCH_ON_DUTY','ALL_MERCH_FULL','BOTH_FULL')),
    [AssignedBy]   INT          NULL,
    [ResolvedAt]   DATETIME2(0) NULL,
    [IsResolved]   BIT          NOT NULL DEFAULT 0,
    CONSTRAINT [FK_OQ_Orders]     FOREIGN KEY ([OrderID])    REFERENCES [Orders]([OrderID]),
    CONSTRAINT [FK_OQ_AssignedBy] FOREIGN KEY ([AssignedBy]) REFERENCES [Accounts]([AccountID])
);
GO

CREATE INDEX [IX_OQ_Unresolved] ON [OrderQueue] ([IsResolved], [QueuedAt] ASC)
    WHERE [IsResolved] = 0;
GO

/* =============================================
   4. SHOPPING (Cart & Wishlist)
============================================= */
CREATE TABLE [Cart] (
    [CartID]    INT IDENTITY(1,1) PRIMARY KEY,
    [AccountID] INT NOT NULL UNIQUE,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(0) NULL,
    CONSTRAINT [FK_Cart_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID])
);
GO

CREATE TABLE [CartItems] (
    [CartItemID]      INT IDENTITY(1,1) PRIMARY KEY,
    [CartID]          INT NOT NULL,
    [ProductID]       INT NOT NULL,
    [Quantity]        SMALLINT NOT NULL CHECK ([Quantity] > 0),
    [PriceAtThatTime] DECIMAL(12,0) NOT NULL,
    [CurrentPrice]    DECIMAL(12,0) NOT NULL,
    [IsSelected]      BIT NOT NULL DEFAULT 1,
    [AddedAt]         DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [RemovedAt]       DATETIME2(0) NULL,
    [UpdatedAt]       DATETIME2(0) NULL,
    CONSTRAINT [FK_CartItems_Cart]        FOREIGN KEY ([CartID])    REFERENCES [Cart]([CartID]),
    CONSTRAINT [FK_CartItems_Products]    FOREIGN KEY ([ProductID]) REFERENCES [Products]([ProductID]),
    CONSTRAINT [UQ_CartItems_CartProduct] UNIQUE ([CartID], [ProductID])
);
GO

CREATE TABLE [Wishlists] (
    [WishlistID] INT IDENTITY(1,1) PRIMARY KEY,
    [AccountID]  INT NOT NULL,
    [ProductID]  INT NOT NULL,
    [CreatedAt]  DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [UQ_Wishlists_AccountProduct] UNIQUE ([AccountID], [ProductID]),
    CONSTRAINT [FK_Wishlists_Accounts]       FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_Wishlists_Products]       FOREIGN KEY ([ProductID]) REFERENCES [Products]([ProductID])
);
GO

/* =============================================
   5. VOUCHERS & PROMOTIONS
============================================= */
CREATE TABLE [Vouchers] (
    [VoucherID]          INT IDENTITY(1,1) PRIMARY KEY,
    [CreatedBy]          INT NULL,
    [VoucherCode]        VARCHAR(30) NOT NULL UNIQUE,
    [VoucherName]        NVARCHAR(255) NOT NULL,
    [VoucherDescription] NVARCHAR(255) NOT NULL,
    [DiscountType]       VARCHAR(10) NOT NULL CHECK ([DiscountType] IN ('FIXED', 'PERCENTAGE')),
    [DiscountValue]      DECIMAL(12,2) NOT NULL CHECK ([DiscountValue] > 0),
    [MaxDiscountCap]     DECIMAL(12,0) NULL,
    [DiscountTarget]     VARCHAR(20) NOT NULL CHECK ([DiscountTarget] IN ('ORDER_TOTAL', 'SHIPPING_FEE')),
    [MinOrderAmount]     DECIMAL(12,0) NULL CHECK ([MinOrderAmount] >= 0),
    [TotalQuantity]      INT NULL CHECK ([TotalQuantity] > 0),
    [UsedQuantity]       INT NOT NULL DEFAULT 0 CHECK ([UsedQuantity] >= 0),
    [MaxUsagePerUser]    SMALLINT NULL DEFAULT 1,
    [StartDate]          DATETIME2(0) NOT NULL,
    [EndDate]            DATETIME2(0) NOT NULL,
    [Status]             VARCHAR(15) NOT NULL CHECK ([Status] IN ('Scheduled', 'Active', 'Inactive', 'Expired')),
    [ImageURL]           VARCHAR(500) NULL,
    [IsDeleted]          BIT NOT NULL DEFAULT 0,
    [CreatedAt]          DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]          DATETIME2(0) NULL,
    CONSTRAINT [FK_Vouchers_Accounts]       FOREIGN KEY ([CreatedBy]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [CK_Vouchers_DiscountPercent] CHECK (
        [DiscountType] <> 'PERCENTAGE' OR ([DiscountValue] > 0 AND [DiscountValue] <= 100)
    ),
    CONSTRAINT [CK_Vouchers_DateRange] CHECK ([StartDate] < [EndDate]),
    CONSTRAINT [CK_Vouchers_Quantity]  CHECK ([TotalQuantity] IS NULL OR [UsedQuantity] <= [TotalQuantity])
);
GO

CREATE TABLE [OrderVouchers] (
    [OrderID]               INT NOT NULL,
    [VoucherID]             INT NOT NULL,
    [DiscountAmountApplied] DECIMAL(12,0) NOT NULL CHECK ([DiscountAmountApplied] >= 0),
    CONSTRAINT [PK_OrderVouchers]          PRIMARY KEY ([OrderID], [VoucherID]),
    CONSTRAINT [FK_OrderVouchers_Orders]   FOREIGN KEY ([OrderID])   REFERENCES [Orders]([OrderID]),
    CONSTRAINT [FK_OrderVouchers_Vouchers] FOREIGN KEY ([VoucherID]) REFERENCES [Vouchers]([VoucherID])
);
GO

CREATE TABLE [VoucherUsageLogs] (
    [UsageID]   INT IDENTITY(1,1) PRIMARY KEY,
    [VoucherID] INT NOT NULL,
    [AccountID] INT NOT NULL,
    [OrderID]   INT NOT NULL,
    [UsedAt]    DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_VoucherUsageLogs_Vouchers] FOREIGN KEY ([VoucherID]) REFERENCES [Vouchers]([VoucherID]),
    CONSTRAINT [FK_VoucherUsageLogs_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_VoucherUsageLogs_Orders]   FOREIGN KEY ([OrderID])   REFERENCES [Orders]([OrderID])
);
GO

/* =============================================
   6. BLOG & CONTENT
============================================= */
CREATE TABLE [BlogCategories] (
    [BlogCategoryID]      SMALLINT IDENTITY(1,1) PRIMARY KEY,
    [BlogCategoriesName]  NVARCHAR(100) NOT NULL UNIQUE,
    [CreatedAt]           DATETIME2(0) NOT NULL DEFAULT GETDATE()
);
GO

CREATE TABLE [BlogPosts] (
    [BlogPostID]     INT IDENTITY(1,1) PRIMARY KEY,
    [AccountID]      INT NOT NULL,
    [ApprovedBy]     INT NULL,
    [BlogCategoryID] SMALLINT NOT NULL,
    [BlogTitle]      NVARCHAR(255) NOT NULL,
    [BlogContent]    NVARCHAR(MAX) NOT NULL,
    [BlogThumbnail]  VARCHAR(500) NULL,
    [Status] VARCHAR(10) NOT NULL CHECK ([Status] IN (
     'Draft', 'Pending', 'Approved',
     'Rejected', 'Scheduled', 'Published',
     'Hidden'
    )),
    [Reason]         NVARCHAR(500) NULL,
    [IsFeatured]     BIT NOT NULL DEFAULT 0,
    [BlogAt]         DATETIME2(0) NULL,
    [IsDeleted]      BIT NOT NULL DEFAULT 0,
    [CreatedAt]      DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]      DATETIME2(0) NULL,
    CONSTRAINT [FK_BlogPosts_Accounts]       FOREIGN KEY ([AccountID])      REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_BlogPosts_ApprovedBy]     FOREIGN KEY ([ApprovedBy])     REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_BlogPosts_BlogCategories] FOREIGN KEY ([BlogCategoryID]) REFERENCES [BlogCategories]([BlogCategoryID])
);
GO

CREATE TABLE [BlogPostStats] (
    [BlogPostID]    INT NOT NULL PRIMARY KEY,
    [LikeCount]     INT NOT NULL DEFAULT 0,
    [CommentCount]  INT NOT NULL DEFAULT 0,
    [UpdatedAt]     DATETIME2(0) NULL,
    CONSTRAINT [FK_BlogPostStats_BlogPosts]
        FOREIGN KEY ([BlogPostID]) REFERENCES [BlogPosts]([BlogPostID])
);
GO

/* =============================================
   7. REVIEWS & REACTIONS (+ AI MODERATION)
============================================= */
CREATE TABLE [ReactionTypes] (
    [ReactionTypeID] INT IDENTITY(1,1) PRIMARY KEY,
    [Code]           NVARCHAR(20) NOT NULL UNIQUE,
    [DisplayName]    NVARCHAR(50) NOT NULL,
    [CreatedAt]      DATETIME2(0) NOT NULL DEFAULT GETDATE()
);
GO

CREATE TABLE [ReviewProducts] (
    [ReviewID]  INT IDENTITY(1,1) PRIMARY KEY,
    [AccountID] INT NOT NULL,
    [ProductID] INT NOT NULL,
    [OrderID]   INT NOT NULL,
    [Rating]    TINYINT NOT NULL CHECK ([Rating] BETWEEN 1 AND 5),
    [Comment]   NVARCHAR(500) NULL,
    [ModerationStatus] VARCHAR(20) NOT NULL DEFAULT 'Pending'
        CONSTRAINT [CK_ReviewProducts_ModerationStatus] CHECK (
            [ModerationStatus] IN ('Pending', 'Approved', 'Rejected', 'ManualReview')
        ),
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [IsEdited]  BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(0) NULL,
    CONSTRAINT [UQ_Review_Account_Order_Product] UNIQUE ([AccountID], [OrderID], [ProductID]),
    CONSTRAINT [FK_Reviews_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_Reviews_Products] FOREIGN KEY ([ProductID]) REFERENCES [Products]([ProductID]),
    CONSTRAINT [FK_Reviews_Orders]   FOREIGN KEY ([OrderID])   REFERENCES [Orders]([OrderID])
);
GO

CREATE TRIGGER [TR_ReviewProducts_ValidateOrderDetail]
ON [ReviewProducts]
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        WHERE NOT EXISTS (
            SELECT 1
            FROM [OrderDetails] od
            WHERE od.[OrderID]   = i.[OrderID]
              AND od.[ProductID] = i.[ProductID]
        )
    )
    BEGIN
        RAISERROR (
            'Invalid review: This product was not found in the order details.',
            16, 1
        );
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

CREATE TABLE [ReviewProductImages] (
    [ReviewProductImageID] INT IDENTITY(1,1) PRIMARY KEY,
    [ReviewProductID]      INT NOT NULL,
    [ImageURL]             VARCHAR(500) NOT NULL,
    [ModerationStatus] VARCHAR(20) NOT NULL DEFAULT 'Pending'
        CONSTRAINT [CK_ReviewProductImages_ModerationStatus] CHECK (
            [ModerationStatus] IN ('Pending', 'Approved', 'Rejected', 'ManualReview')
        ),
    [IsDeleted]            BIT NOT NULL DEFAULT 0,
    [CreatedAt]            DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]            DATETIME2(0) NULL,
    CONSTRAINT [FK_ReviewProductImages_ReviewProducts]
        FOREIGN KEY ([ReviewProductID]) REFERENCES [ReviewProducts]([ReviewID])
);
GO

CREATE TRIGGER [TR_ReviewProductImages_MaxImages]
ON [ReviewProductImages]
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        WHERE (
            SELECT COUNT(*)
            FROM [ReviewProductImages] rpi
            WHERE rpi.[ReviewProductID] = i.[ReviewProductID]
              AND rpi.[IsDeleted] = 0
        ) > 3
    )
    BEGIN
        RAISERROR (
            'Each review allows a maximum of 3 photos.',
            16, 1
        );
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

CREATE TABLE [dbo].[ReviewModerationLogs] (
    [LogID]        INT IDENTITY(1,1) PRIMARY KEY,
    [TargetType]   VARCHAR(10)    NOT NULL
        CONSTRAINT [CK_ModerationLogs_TargetType]
            CHECK ([TargetType] IN ('Text', 'Image')),
    [ReviewID]     INT            NOT NULL
        CONSTRAINT [FK_ModerationLogs_ReviewProducts]
            REFERENCES [dbo].[ReviewProducts]([ReviewID]),
    [ImageID]      INT            NULL
        CONSTRAINT [FK_ModerationLogs_ReviewProductImages]
            REFERENCES [dbo].[ReviewProductImages]([ReviewProductImageID]),
    [ModeratorType] VARCHAR(10)   NOT NULL
        CONSTRAINT [CK_ModerationLogs_ModeratorType]
            CHECK ([ModeratorType] IN ('AI', 'Staff')),
    [ModeratedBy]  INT            NULL
        CONSTRAINT [FK_ModerationLogs_Accounts]
            REFERENCES [dbo].[Accounts]([AccountID]),
    [Action]       VARCHAR(20)    NOT NULL
        CONSTRAINT [CK_ModerationLogs_Action]
            CHECK ([Action] IN ('Approved', 'Rejected', 'ManualReview', 'Overridden')),
    [AIModelVersion] VARCHAR(100) NULL,
    [ModerationResult] NVARCHAR(MAX) NULL,
    [Reason]       NVARCHAR(500)  NULL,
    [CreatedAt]    DATETIME2(0)   NOT NULL DEFAULT GETDATE(),
    
    CONSTRAINT [CK_ModerationLogs_ImageConsistency]
        CHECK (
            ([TargetType] = 'Image' AND [ImageID] IS NOT NULL) OR
            ([TargetType] = 'Text'  AND [ImageID] IS NULL)
        ),
    CONSTRAINT [CK_ModerationLogs_StaffConsistency]
        CHECK (
            ([ModeratorType] = 'Staff' AND [ModeratedBy] IS NOT NULL) OR
            ([ModeratorType] = 'AI'    AND [ModeratedBy] IS NULL)
        )
);
GO

CREATE NONCLUSTERED INDEX [IX_ModerationLogs_Review]
ON [dbo].[ReviewModerationLogs] ([ReviewID], [CreatedAt] DESC)
INCLUDE ([TargetType], [Action], [ModeratorType]);
GO

CREATE NONCLUSTERED INDEX [IX_ModerationLogs_ManualReview]
ON [dbo].[ReviewModerationLogs] ([Action], [CreatedAt] ASC)
INCLUDE ([ReviewID], [ImageID], [TargetType])
WHERE [Action] = 'ManualReview';
GO

CREATE NONCLUSTERED INDEX [IX_ModerationLogs_AIPerformance]
ON [dbo].[ReviewModerationLogs] ([ModeratorType], [AIModelVersion], [Action])
WHERE [ModeratorType] = 'AI';
GO

CREATE TABLE [StaffReviewProductReplies] (
    [ReplyProductID]  INT IDENTITY(1,1) PRIMARY KEY,
    [ReviewProductID] INT NOT NULL,
    [StaffID]         INT NOT NULL,
    [Content]         NVARCHAR(500) NOT NULL,
    [IsDeleted]       BIT NOT NULL DEFAULT 0,
    [CreatedAt]       DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]       DATETIME2(0) NULL,
    CONSTRAINT [FK_ReviewProductReplies_ReviewProducts]
        FOREIGN KEY ([ReviewProductID]) REFERENCES [ReviewProducts]([ReviewID]),
    CONSTRAINT [FK_ReviewProductReplies_Accounts]
        FOREIGN KEY ([StaffID]) REFERENCES [Accounts]([AccountID])
);
GO

CREATE TABLE [ReviewProductReactions] (
    [ReactionProductID] INT IDENTITY(1,1) PRIMARY KEY,
    [ReviewProductID]   INT NOT NULL,
    [AccountID]         INT NOT NULL,
    [ReactionTypeID]    INT NOT NULL,                       
    [IsDeleted]         BIT NOT NULL DEFAULT 0,
    [CreatedAt]         DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]         DATETIME2(0) NULL,
    CONSTRAINT [FK_ReviewProductReactions_ReviewProducts]
        FOREIGN KEY ([ReviewProductID]) REFERENCES [ReviewProducts]([ReviewID]),
    CONSTRAINT [FK_ReviewProductReactions_Accounts]
        FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_ReviewProductReactions_ReactionTypes]  
        FOREIGN KEY ([ReactionTypeID]) REFERENCES [ReactionTypes]([ReactionTypeID]),
    CONSTRAINT [UQ_ReviewProductReactions_AccountReview]
        UNIQUE ([AccountID], [ReviewProductID])
);
GO

/* =============================================
   7.1. BLOG REVIEWS & INTERACTIONS
============================================= */
CREATE TABLE [ReviewBlogs] (
    [ReviewBlogID] INT IDENTITY(1,1) PRIMARY KEY,
    [BlogPostID]   INT NOT NULL,
    [AccountID]    INT NOT NULL,
    [Comment]      NVARCHAR(500) NULL,
    [IsDeleted]    BIT NOT NULL DEFAULT 0,
    [CreatedAt]    DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]    DATETIME2(0) NULL,
    CONSTRAINT [FK_ReviewBlogs_BlogPosts] FOREIGN KEY ([BlogPostID]) REFERENCES [BlogPosts]([BlogPostID]),
    CONSTRAINT [FK_ReviewBlogs_Accounts]  FOREIGN KEY ([AccountID])  REFERENCES [Accounts]([AccountID])
);
GO

CREATE TABLE [ReviewBlogReplies] (
    [ReplyBlogID]      INT IDENTITY(1,1) PRIMARY KEY,
    [ReviewBlogID]     INT NOT NULL,
    [AccountID]        INT NOT NULL,
    [ParentReplyID]    INT NULL,
    [ReplyToAccountID] INT NULL,
    [Comment]          NVARCHAR(500) NOT NULL,
    [IsDeleted]        BIT NOT NULL DEFAULT 0,
    [CreatedAt]        DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]        DATETIME2(0) NULL,
    CONSTRAINT [FK_ReviewBlogReplies_ReviewBlogs] FOREIGN KEY ([ReviewBlogID])     REFERENCES [ReviewBlogs]([ReviewBlogID]),
    CONSTRAINT [FK_ReviewBlogReplies_Accounts]    FOREIGN KEY ([AccountID])        REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_ReviewBlogReplies_Parent]      FOREIGN KEY ([ParentReplyID])    REFERENCES [ReviewBlogReplies]([ReplyBlogID]),
    CONSTRAINT [FK_ReviewBlogReplies_ReplyTo]     FOREIGN KEY ([ReplyToAccountID]) REFERENCES [Accounts]([AccountID])
);
GO

CREATE TABLE [ReviewBlogReactions] (
    [ReactionBlogID] INT IDENTITY(1,1) PRIMARY KEY,
    [ReviewBlogID]   INT NOT NULL,
    [AccountID]      INT NOT NULL,
    [ReactionTypeID] INT NOT NULL,
    [CreatedAt]      DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_ReviewBlogReactions_ReviewBlogs]   FOREIGN KEY ([ReviewBlogID])   REFERENCES [ReviewBlogs]([ReviewBlogID]),
    CONSTRAINT [FK_ReviewBlogReactions_Accounts]      FOREIGN KEY ([AccountID])      REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_ReviewBlogReactions_ReactionTypes] FOREIGN KEY ([ReactionTypeID]) REFERENCES [ReactionTypes]([ReactionTypeID]),
    CONSTRAINT [UQ_ReviewBlogReactions_AccountReview] UNIQUE ([AccountID], [ReviewBlogID])
);
GO

CREATE TABLE [dbo].[BlogPostReactions]
    (
        [ReactionPostID] INT IDENTITY(1,1) PRIMARY KEY,
        [BlogPostID]     INT NOT NULL,
        [AccountID]      INT NOT NULL,
        [ReactionTypeID] INT NOT NULL,
        [CreatedAt]      DATETIME2(0) NOT NULL CONSTRAINT [DF_BlogPostReactions_CreatedAt] DEFAULT (GETDATE()),
        CONSTRAINT [FK_BlogPostReactions_BlogPosts] FOREIGN KEY ([BlogPostID]) REFERENCES [dbo].[BlogPosts]([BlogPostID]),
        CONSTRAINT [FK_BlogPostReactions_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [dbo].[Accounts]([AccountID]),
        CONSTRAINT [FK_BlogPostReactions_ReactionTypes] FOREIGN KEY ([ReactionTypeID]) REFERENCES [dbo].[ReactionTypes]([ReactionTypeID]),
        CONSTRAINT [UQ_BlogPostReactions_AccountPost] UNIQUE ([AccountID], [BlogPostID])
    );

CREATE NONCLUSTERED INDEX [IX_BlogPostReactions_Stats]
    ON [dbo].[BlogPostReactions]([BlogPostID], [ReactionTypeID]);
GO

CREATE TABLE [dbo].[ReviewBlogReplyReactions]
    (
        [ReactionReplyBlogID] INT IDENTITY(1,1) PRIMARY KEY,
        [ReplyBlogID]         INT NOT NULL,
        [AccountID]           INT NOT NULL,
        [ReactionTypeID]      INT NOT NULL,
        [CreatedAt]           DATETIME2(0) NOT NULL CONSTRAINT [DF_ReviewBlogReplyReactions_CreatedAt] DEFAULT (GETDATE()),
        CONSTRAINT [FK_ReviewBlogReplyReactions_ReviewBlogReplies] FOREIGN KEY ([ReplyBlogID]) REFERENCES [dbo].[ReviewBlogReplies]([ReplyBlogID]),
        CONSTRAINT [FK_ReviewBlogReplyReactions_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [dbo].[Accounts]([AccountID]),
        CONSTRAINT [FK_ReviewBlogReplyReactions_ReactionTypes] FOREIGN KEY ([ReactionTypeID]) REFERENCES [dbo].[ReactionTypes]([ReactionTypeID]),
        CONSTRAINT [UQ_ReviewBlogReplyReactions_AccountReply] UNIQUE ([AccountID], [ReplyBlogID])
    );

CREATE NONCLUSTERED INDEX [IX_ReviewBlogReplyReactions_Stats]
    ON [dbo].[ReviewBlogReplyReactions]([ReplyBlogID], [ReactionTypeID]);

GO
CREATE INDEX [IX_BlogPostStats_Score]
ON [BlogPostStats]([LikeCount] DESC, [CommentCount] DESC);
GO

CREATE TRIGGER [trg_BlogReaction_UpdateLikeCount]
ON [ReviewBlogReactions] AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    WITH affected AS (
        SELECT rb.[BlogPostID]
        FROM [ReviewBlogs] rb
        JOIN inserted i ON rb.[ReviewBlogID] = i.[ReviewBlogID]
        UNION
        SELECT rb.[BlogPostID]
        FROM [ReviewBlogs] rb
        JOIN deleted d ON rb.[ReviewBlogID] = d.[ReviewBlogID]
    )
    UPDATE s
    SET
        s.[LikeCount] = (
            SELECT COUNT(*)
            FROM [ReviewBlogReactions] r
            JOIN [ReviewBlogs] rb ON r.[ReviewBlogID] = rb.[ReviewBlogID]
            WHERE rb.[BlogPostID] = s.[BlogPostID]
        ),
        s.[UpdatedAt] = GETDATE()
    FROM [BlogPostStats] s
    WHERE s.[BlogPostID] IN (SELECT [BlogPostID] FROM affected);
END;
GO

CREATE TRIGGER [trg_BlogComment_UpdateCommentCount]
ON [ReviewBlogs] AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    WITH affected AS (
        SELECT [BlogPostID] FROM inserted
        UNION
        SELECT [BlogPostID] FROM deleted
    )
    UPDATE s
    SET
        s.[CommentCount] = (
            SELECT COUNT(*)
            FROM [ReviewBlogs] rb
            WHERE rb.[BlogPostID] = s.[BlogPostID]
            AND rb.[IsDeleted] = 0
        ),
        s.[UpdatedAt] = GETDATE()
    FROM [BlogPostStats] s
    WHERE s.[BlogPostID] IN (SELECT [BlogPostID] FROM affected);
END;
GO


/* =============================================
   TRIGGERS
============================================= */

-- 1. Tự động tạo row BlogPostStats khi có BlogPost mới
CREATE TRIGGER [trg_BlogPost_InitStats]
ON [BlogPosts] AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [BlogPostStats] ([BlogPostID], [LikeCount], [CommentCount], [UpdatedAt])
    SELECT i.[BlogPostID], 0, 0, GETDATE()
    FROM inserted i
    WHERE NOT EXISTS (
        SELECT 1 FROM [BlogPostStats] s
        WHERE s.[BlogPostID] = i.[BlogPostID]
    );
END;
GO

-- 2. Cập nhật LikeCount khi react vào bài blog
CREATE TRIGGER [trg_BlogPostReaction_UpdateLikeCount]
ON [BlogPostReactions] AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    WITH affected AS (
        SELECT [BlogPostID] FROM inserted
        UNION
        SELECT [BlogPostID] FROM deleted
    )
    UPDATE s
    SET
        s.[LikeCount] = (
            SELECT COUNT(*)
            FROM [BlogPostReactions] bpr
            WHERE bpr.[BlogPostID] = s.[BlogPostID]
        ),
        s.[UpdatedAt] = GETDATE()
    FROM [BlogPostStats] s
    WHERE s.[BlogPostID] IN (SELECT [BlogPostID] FROM affected);
END;
GO

-- 3. Cập nhật CommentCount khi có comment/xóa comment
CREATE TRIGGER [trg_ReviewBlog_UpdateCommentCount]
ON [ReviewBlogs] AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    WITH affected AS (
        SELECT [BlogPostID] FROM inserted
        UNION
        SELECT [BlogPostID] FROM deleted
    )
    UPDATE s
    SET
        s.[CommentCount] = (
            SELECT COUNT(*)
            FROM [ReviewBlogs] rb
            WHERE rb.[BlogPostID] = s.[BlogPostID]
              AND rb.[IsDeleted] = 0
        ),
        s.[UpdatedAt] = GETDATE()
    FROM [BlogPostStats] s
    WHERE s.[BlogPostID] IN (SELECT [BlogPostID] FROM affected);
END;
GO

-- 4. Tự động cập nhật IsFeatured top 5 khi BlogPostStats thay đổi
CREATE TRIGGER [trg_BlogPostStats_UpdateFeatured]
ON [BlogPostStats] AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Reset toàn bộ
    UPDATE [BlogPosts]
    SET [IsFeatured] = 0
    WHERE [IsDeleted] = 0 AND [Status] = 'Published';

    -- Set top 5
    WITH TopFeatured AS (
        SELECT TOP 5 bp.[BlogPostID]
        FROM [BlogPosts] bp
        JOIN [BlogPostStats] s ON bp.[BlogPostID] = s.[BlogPostID]
        WHERE bp.[IsDeleted] = 0
          AND bp.[Status] = 'Published'
        ORDER BY
            (s.[LikeCount] + s.[CommentCount]) DESC,
            s.[LikeCount] DESC,
            bp.[CreatedAt] DESC
    )
    UPDATE bp
    SET bp.[IsFeatured] = 1
    FROM [BlogPosts] bp
    JOIN TopFeatured tf ON bp.[BlogPostID] = tf.[BlogPostID];
END;
GO

/* =============================================
   8. PAYMENT & WALLET
============================================= */
CREATE TABLE [Wallets] (
    [WalletID]          INT IDENTITY(1,1) PRIMARY KEY,
    [AccountID]         INT NOT NULL UNIQUE,
    [Currency]          CHAR(3) NOT NULL DEFAULT 'VND',
    [Balance]           DECIMAL(12,0) NOT NULL DEFAULT 0 CHECK ([Balance] >= 0),
    [Status]            VARCHAR(10) NOT NULL DEFAULT 'Active'
        CHECK ([Status] IN ('Active', 'Frozen', 'Closed')),
    [LastTransactionAt] DATETIME2(0) NULL,
    [CreatedAt]         DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]         DATETIME2(0) NULL,
    CONSTRAINT [FK_Wallets_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID])
);
GO
     
CREATE TABLE [WalletTransactions] (
    [WalletTransactionID] INT IDENTITY(1,1) PRIMARY KEY,
    [WalletID]            INT NOT NULL,
    [AccountID]           INT NOT NULL,
    [RelatedOrderID]      INT NULL,
    [TxnType]             VARCHAR(20) NOT NULL CHECK ([TxnType] IN ('TopUp', 'Payment', 'Refund')),
    [Direction]           CHAR(2) NOT NULL CHECK ([Direction] IN ('CR', 'DR')), 
    [Amount]              DECIMAL(12,0) NOT NULL CHECK ([Amount] > 0),
    [BalanceBefore]       DECIMAL(12,0) NOT NULL,
    [BalanceAfter]        DECIMAL(12,0) NOT NULL,
    [Method]              VARCHAR(15) NOT NULL CHECK ([Method] IN ('BankTransfer', 'Internal')),
    [ExternalRef]         VARCHAR(100) NULL, 
    [IdempotencyKey]      VARCHAR(100) NULL, 
    [Status]              VARCHAR(15) NOT NULL DEFAULT 'Pending'
        CHECK ([Status] IN ('Pending', 'Completed', 'Failed', 'Cancelled')),
    [Reason]              NVARCHAR(255) NULL,
    [Metadata]            NVARCHAR(1000) NULL,
    [CreatedAt]           DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [CompletedAt]         DATETIME2(0) NULL,
    CONSTRAINT [FK_WalletTransactions_Wallets]  FOREIGN KEY ([WalletID])       REFERENCES [Wallets]([WalletID]),
    CONSTRAINT [FK_WalletTransactions_Orders]    FOREIGN KEY ([RelatedOrderID]) REFERENCES [Orders]([OrderID]),
    CONSTRAINT [FK_WalletTransactions_Accounts] FOREIGN KEY ([AccountID])      REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [CK_WalletTransactions_BalanceAfter] CHECK (
        ([Direction] = 'CR' AND [BalanceAfter] = [BalanceBefore] + [Amount]) OR
        ([Direction] = 'DR' AND [BalanceAfter] = [BalanceBefore] - [Amount])
    ),
    CONSTRAINT [CK_WalletTransactions_NoNegativeBalance] CHECK ([BalanceAfter] >= 0)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ_WalletTransactions_IdempotencyKey]
ON [WalletTransactions]([IdempotencyKey])
WHERE [IdempotencyKey] IS NOT NULL;
GO

/* ── v3.2: thêm 'COD_PENDING' vào PaymentHistory.PaymentStatus ── */
CREATE TABLE [PaymentHistory] (
    [PaymentHistoryID]    INT IDENTITY(1,1) PRIMARY KEY,
    [AccountID]           INT NOT NULL,
    [OrderID]             INT NOT NULL,
    [WalletTransactionID] INT NULL,
    [PaymentStatus]       VARCHAR(20) NOT NULL
        CONSTRAINT [CK_PaymentHistory_PaymentStatus]
        CHECK ([PaymentStatus] IN ('PENDING', 'PAID', 'FAILED', 'EXPIRED', 'REFUNDED', 'COD_PENDING')),
    [PaymentMethod]       VARCHAR(20) NOT NULL
        CHECK ([PaymentMethod] IN ('SE_PAY', 'WALLET', 'BANK_TRANSFER', 'SHIP_COD')), 
    [TransactionCode]     VARCHAR(100) NULL,
    [Amount]              DECIMAL(12,0) NOT NULL CHECK ([Amount] >= 0),
    [CreatedAt]           DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_PaymentHistory_Orders]              FOREIGN KEY ([OrderID])             REFERENCES [Orders]([OrderID]),
    CONSTRAINT [FK_PaymentHistory_WalletTransactions]  FOREIGN KEY ([WalletTransactionID]) REFERENCES [WalletTransactions]([WalletTransactionID]),
    CONSTRAINT [FK_PaymentHistory_Accounts]            FOREIGN KEY ([AccountID])           REFERENCES [Accounts]([AccountID])
);
GO

/* =============================================
   8.1. PAYMENT GATEWAY TRANSACTIONS
============================================= */
CREATE TABLE [PaymentGatewayTransactions] (
    [PaymentGatewayTxnID] BIGINT        IDENTITY(1,1) NOT NULL,
    [OrderID]             INT           NOT NULL,
    [PaymentHistoryID]    INT           NULL,
    [Provider]            VARCHAR(20)   NOT NULL CHECK ([Provider] IN ('SE_PAY')),
    [RequestID]           VARCHAR(100)  NULL UNIQUE,
    [TransactionNo]       VARCHAR(100)  NULL,
    [Amount]              DECIMAL(12,0) NOT NULL CHECK ([Amount] >= 0),
    [ResponseCode]        VARCHAR(10)   NULL,
    [ResponseMessage]     NVARCHAR(500) NULL,
    [Status]              VARCHAR(20)   NOT NULL DEFAULT 'Pending'
        CHECK ([Status] IN ('Pending', 'Paid', 'Failed', 'Expired', 'Cancelled')),
    [RawCallback]         NVARCHAR(MAX) NULL, 
    [RetryCount]          INT           NOT NULL DEFAULT 0,
    [CreatedAt]           DATETIME2(0)  NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]           DATETIME2(0)  NULL,
    CONSTRAINT [PK_PaymentGatewayTransactions] PRIMARY KEY ([PaymentGatewayTxnID]),
    CONSTRAINT [FK_PayGwTxn_Orders]          FOREIGN KEY ([OrderID])          REFERENCES [Orders]([OrderID]),
    CONSTRAINT [FK_PayGwTxn_PaymentHistory] FOREIGN KEY ([PaymentHistoryID]) REFERENCES [PaymentHistory]([PaymentHistoryID])
);
GO

CREATE INDEX [IX_PayGwTxn_OrderID]         ON [dbo].[PaymentGatewayTransactions] ([OrderID]);
CREATE INDEX [IX_PayGwTxn_Provider_Status] ON [dbo].[PaymentGatewayTransactions] ([Provider], [Status]);
GO

CREATE TABLE [OrderRefundReasons] (
    [RefundReasonID] TINYINT IDENTITY(1,1) PRIMARY KEY,
    [Content]        NVARCHAR(150) NOT NULL,
    [Description]    NVARCHAR(255) NULL,
    [IsDeleted]      BIT NOT NULL DEFAULT 0,
    [CreatedAt]      DATETIME2(0) NOT NULL DEFAULT GETDATE()
);
GO

CREATE TABLE [OrderRefunds] (
    [RefundID]            INT IDENTITY(1,1) PRIMARY KEY,
    [OrderID]             INT NOT NULL,
    [RefundReasonID]      TINYINT NULL,
    [CustomerID]          INT NOT NULL,
    [RequestedBy]         INT NULL,
    [ApprovedBy]          INT NULL,
    [WalletTransactionID] INT NULL,
    [ReasonDetails]       NVARCHAR(500) NULL,
    [ApprovedAmount]      DECIMAL(12,0) NOT NULL CHECK ([ApprovedAmount] >= 0),
    [RefundStatus]        VARCHAR(20) NOT NULL DEFAULT 'Requested'
        CONSTRAINT [CK_OrderRefunds_RefundStatus]
            CHECK ([RefundStatus] IN ('Requested', 'Approved', 'Rejected', 'Completed', 'Cancelled')),
    [CreatedAt]           DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]           DATETIME2(0) NULL,
    CONSTRAINT [FK_OrderRefunds_Orders]             FOREIGN KEY ([OrderID])             REFERENCES [Orders]([OrderID]),
    CONSTRAINT [FK_OrderRefunds_WalletTransactions] FOREIGN KEY ([WalletTransactionID]) REFERENCES [WalletTransactions]([WalletTransactionID]),
    CONSTRAINT [FK_OrderRefunds_Customer]           FOREIGN KEY ([CustomerID])          REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_OrderRefunds_RequestedBy]        FOREIGN KEY ([RequestedBy])         REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_OrderRefunds_ApprovedBy]         FOREIGN KEY ([ApprovedBy])          REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_OrderRefunds_RefundReasons]      FOREIGN KEY ([RefundReasonID])      REFERENCES [OrderRefundReasons]([RefundReasonID])
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ_OrderRefunds_OneActivePerOrder]
ON [OrderRefunds] ([OrderID])
WHERE [RefundStatus] IN ('Requested', 'Approved');
GO

CREATE TRIGGER [TR_OrderRefunds_ValidateAmount]
ON [OrderRefunds]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        INNER JOIN [Orders] o ON o.[OrderID] = i.[OrderID]
        WHERE i.[ApprovedAmount] > o.[TotalAmount]
    )
    BEGIN
        RAISERROR (
            'The approved amount must not exceed the total amount of the order.',
            16, 1
        );
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

CREATE TABLE [RefundImages] (
    [RefundImageID] INT IDENTITY(1,1) PRIMARY KEY,
    [RefundID]      INT NOT NULL,
    [ImageURL]      VARCHAR(500) NOT NULL,
    [IsDeleted]     BIT NOT NULL DEFAULT 0,
    [CreatedAt]     DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_RefundImages_OrderRefunds] FOREIGN KEY ([RefundID]) REFERENCES [OrderRefunds]([RefundID])
);
GO

CREATE NONCLUSTERED INDEX [IX_RefundImages_RefundID]
ON [RefundImages] ([RefundID])
WHERE [IsDeleted] = 0;
GO

CREATE TRIGGER [TR_RefundImages_MaxImages]
ON [RefundImages]
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        WHERE (
            SELECT COUNT(*)
            FROM [RefundImages] ri
            WHERE ri.[RefundID]  = i.[RefundID]
              AND ri.[IsDeleted] = 0
        ) > 5
    )
    BEGIN
        RAISERROR (
            'Each refund allows a maximum of 5 evidence photos.',
            16, 1
        );
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

CREATE TABLE [dbo].[ShippingProviderTransactions] (
    [ShippingTransactionID] BIGINT IDENTITY(1,1) NOT NULL,
    [OrderID]               INT            NOT NULL,
    [Provider]              VARCHAR(50)    NOT NULL,
    [ProviderOrderCode]     VARCHAR(100)   NULL,
    [TrackingNumber]        VARCHAR(100)   NULL,
    [ServiceType]           NVARCHAR(100)  NULL,
    [Status]                VARCHAR(50)    NULL,
    [ShippingFee]           DECIMAL(12,0)  NULL,
    [CodAmount]             DECIMAL(12,0)  NULL,
    [RowVersion]            ROWVERSION     NOT NULL,
    [RetryCount]            INT            NOT NULL DEFAULT 0,
    [LastErrorMessage]      NVARCHAR(500)  NULL,
    [Metadata]              NVARCHAR(MAX)  NULL,
    [EstimatedDelivery]     DATETIME2(0)   NULL,
    [ActualDelivery]        DATETIME2(0)   NULL,
    [LastPolledAt]          DATETIME2(0)   NULL,
    [CreatedAt]             DATETIME2(0)   NOT NULL CONSTRAINT DF_ShippingTxn_CreatedAt DEFAULT (GETDATE()),
    [UpdatedAt]             DATETIME2(0)   NULL,
    CONSTRAINT [PK__Shipping__F215F69363919D74] PRIMARY KEY ([ShippingTransactionID]),
    CONSTRAINT [FK_ShippingTxn_Orders]    FOREIGN KEY ([OrderID]) REFERENCES [dbo].[Orders]([OrderID]),
    CONSTRAINT [CK_ShippingTxn_CodAmount] CHECK ([CodAmount] IS NULL OR [CodAmount] >= 0),
    CONSTRAINT [CK_ShippingTxn_Status] CHECK ([Status] IN (
        'ready_to_pick', 'picking', 'cancel', 'money_collect_picking',
        'picked', 'storing', 'transporting', 'sorting', 'delivering',
        'money_collect_delivering', 'delivered', 'delivery_fail',
        'waiting_to_return', 'return', 'return_transporting', 'return_sorting',
        'returning', 'return_fail', 'returned', 'exception', 'damage', 'lost'
    ))
);
GO

CREATE INDEX [IX_ShippingTxn_OrderID]         ON [dbo].[ShippingProviderTransactions] ([OrderID]);
CREATE INDEX [IX_ShippingTxn_Provider_Status] ON [dbo].[ShippingProviderTransactions] ([Provider], [Status]);
GO

CREATE TABLE [dbo].[ShippingStatusHistories] (
    [HistoryId]      BIGINT IDENTITY PRIMARY KEY,
    [ShippingTxId]   BIGINT NOT NULL REFERENCES [dbo].[ShippingProviderTransactions]([ShippingTransactionID]),
    [OrderId]        INT NOT NULL REFERENCES [dbo].[Orders]([OrderID]),
    [PreviousStatus] NVARCHAR(50) NOT NULL,
    [NewStatus]      NVARCHAR(50) NOT NULL,
    [Source]         NVARCHAR(20) NOT NULL,
    [RawPayload]     NVARCHAR(MAX) NULL,
    [ProcessedAt]    DATETIME2(0) NOT NULL DEFAULT GETDATE()
);
GO

CREATE INDEX IX_ShippingStatusHistories_ShippingTxId ON ShippingStatusHistories(ShippingTxId);
GO

CREATE INDEX [IX_ShippingProviderTransactions_Polling]
ON [dbo].[ShippingProviderTransactions] ([Provider], [Status], [OrderID])
INCLUDE ([ProviderOrderCode], [UpdatedAt])
WHERE [Status] <> 'delivered'
  AND [Status] <> 'returned'
  AND [Status] <> 'return_fail'
  AND [Status] <> 'exception'
  AND [Status] <> 'damage'
  AND [Status] <> 'lost'
  AND [Status] <> 'cancel';
GO

/* =============================================
   9. NOTIFICATION & CHAT & INTERACTIONS
============================================= */
CREATE TABLE [dbo].[CustomerChildren] (
    [ChildID]   INT IDENTITY(1,1) PRIMARY KEY,
    [AccountID] INT NOT NULL,
    [SexID]     TINYINT NULL,
    [FullName]  NVARCHAR(100) NOT NULL,
    [NickName]  NVARCHAR(50) NULL,
    [DOB]       DATE NOT NULL,
    [BirthdayNotifiedYear] SMALLINT NULL,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(0) NULL,
    CONSTRAINT [FK_CustomerChildren_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [dbo].[Accounts]([AccountID]),
    CONSTRAINT [FK_CustomerChildren_Sexes]    FOREIGN KEY ([SexID])     REFERENCES [dbo].[Sexes]([SexID]),
    CONSTRAINT [CK_CustomerChildren_DOB]      CHECK ([DOB] <= CAST(GETDATE() AS DATE))
);
GO

CREATE NONCLUSTERED INDEX [IX_CustomerChildren_DOB] 
ON [dbo].[CustomerChildren]([DOB]) 
WHERE [IsDeleted] = 0;
GO

CREATE TABLE [Notification].[Templates] (
    [TemplateID]      SMALLINT IDENTITY(1,1) NOT NULL,
    [TemplateCode]    VARCHAR(50)            NOT NULL,
    [UsageScope]      VARCHAR(10)            NOT NULL DEFAULT 'ADMIN',
    [TitleTemplate]   NVARCHAR(255)          NOT NULL,
    [MessageTemplate] NVARCHAR(500)          NOT NULL,
    [IsActive]        BIT                    NOT NULL DEFAULT 1,
    [IsDeleted]       BIT                    NOT NULL DEFAULT 0,
    [CreatedAt]       DATETIME2(0)           NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]       DATETIME2(0)           NULL,
    CONSTRAINT [PK_Templates]              PRIMARY KEY ([TemplateID]),
    CONSTRAINT [UQ_Templates_TemplateCode] UNIQUE ([TemplateCode]),
    CONSTRAINT [CK_Templates_UsageScope]   CHECK ([UsageScope] IN ('SYSTEM', 'ADMIN')),
    CONSTRAINT [CK_Templates_SystemAlwaysActive] CHECK ([UsageScope] <> 'SYSTEM' OR [IsActive] = 1),
    CONSTRAINT [CK_Templates_SystemNotDeleted]   CHECK ([UsageScope] <> 'SYSTEM' OR [IsDeleted] = 0)
);
GO

CREATE TABLE [Notification].[UserPreferences] (
    [PreferenceID] INT IDENTITY(1,1) PRIMARY KEY,
    [AccountID]    INT NOT NULL UNIQUE,
    [EmailOptIn]   BIT NOT NULL DEFAULT 1, 
    [WebPushOptIn] BIT NOT NULL DEFAULT 0, 
    [OrderUpdates] BIT NOT NULL DEFAULT 1, 
    [Promotions]   BIT NOT NULL DEFAULT 1, 
    [StockAlerts]  BIT NOT NULL DEFAULT 1, 
    [BlogAlerts]   BIT NOT NULL DEFAULT 1,
    [UpdatedAt]    DATETIME2(0) NULL,
    CONSTRAINT [FK_UserPreferences_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [dbo].[Accounts]([AccountID])
);
GO

CREATE TRIGGER [TR_Accounts_InitPreferences]
ON [dbo].[Accounts]
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [Notification].[UserPreferences] ([AccountID])
    SELECT [AccountID] FROM inserted;
END;
GO

CREATE TABLE [Notification].[Campaigns] (
    [CampaignID]         INT IDENTITY(1,1) NOT NULL,
    [CampaignName]       NVARCHAR(255)     NOT NULL,
    [TemplateCode]       VARCHAR(50)       NULL,
    [TitleOverride]      NVARCHAR(255)     NULL,
    [MessageOverride]    NVARCHAR(500)     NULL,
    [SourceType]         VARCHAR(10)       NOT NULL DEFAULT 'ADMIN',
    [TargetType]         VARCHAR(10)       NOT NULL DEFAULT 'ALL',
    [ReferenceType]      VARCHAR(20)       NULL,     
    [ReferenceID]        INT               NULL,
    [Status]             VARCHAR(15)       NOT NULL DEFAULT 'Draft',
    [ScheduledAt]        DATETIME2(0)      NULL,
    [EventKey]           VARCHAR(100)      NULL,
    [ImageUrl]           NVARCHAR(500)     NULL,
    [ActionType]         NVARCHAR(20)      NULL,
    [ActionTarget]       NVARCHAR(500)     NULL,
    [IsDeleted]          BIT               NOT NULL DEFAULT 0,
    [CreatedByAccountID] INT               NULL,    
    [CreatedAt]          DATETIME2(0)      NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]          DATETIME2(0)      NULL,
    CONSTRAINT [PK_Campaigns] PRIMARY KEY ([CampaignID]),
    CONSTRAINT [CK_Campaigns_SourceType] CHECK ([SourceType] IN ('ADMIN', 'SYSTEM')),
    CONSTRAINT [CK_Campaigns_TargetType] CHECK ([TargetType] IN ('ALL', 'INDIVIDUAL', 'ROLE')),
    CONSTRAINT [CK_Campaigns_ReferenceType] CHECK ([ReferenceType] IN ('VOUCHER', 'PRODUCT', 'BLOG', 'SALE', 'OTHER')),
    CONSTRAINT [CK_Campaigns_Status] CHECK ([Status] IN ('Draft', 'Scheduled', 'Sending', 'Sent', 'Cancelled', 'Failed')),
    CONSTRAINT [CK_Campaigns_AdminRequiresCreator] CHECK ([SourceType] = 'SYSTEM' OR [CreatedByAccountID] IS NOT NULL),
    CONSTRAINT [CK_Campaigns_ScheduledRequiresTime] CHECK ([Status] <> 'Scheduled' OR [ScheduledAt] IS NOT NULL),
    CONSTRAINT [CK_Campaigns_MustHaveContent] CHECK (
        [TemplateCode] IS NOT NULL OR ([TitleOverride] IS NOT NULL AND [MessageOverride] IS NOT NULL)
    ),
    CONSTRAINT [CK_Campaigns_ReferenceConsistency] CHECK (
        ([ReferenceType] IS NULL AND [ReferenceID] IS NULL) OR ([ReferenceType] IS NOT NULL AND [ReferenceID] IS NOT NULL)
    ),
    CONSTRAINT [FK_Campaigns_Templates] FOREIGN KEY ([TemplateCode])       REFERENCES [Notification].[Templates]([TemplateCode]),
    CONSTRAINT [FK_Campaigns_Accounts]  FOREIGN KEY ([CreatedByAccountID]) REFERENCES [dbo].[Accounts]([AccountID])
);
GO
 
CREATE UNIQUE INDEX [UQ_Campaigns_EventKey_Active] ON [Notification].[Campaigns] ([EventKey]) WHERE [EventKey] IS NOT NULL AND [IsDeleted] = 0;
CREATE INDEX [IX_Campaigns_Status_ScheduledAt]     ON [Notification].[Campaigns] ([Status], [ScheduledAt]) WHERE [IsDeleted] = 0 AND [Status] IN ('Scheduled', 'Sending');
GO

CREATE TABLE [Notification].[CampaignStats] (
    [StatID]       INT IDENTITY(1,1) NOT NULL,
    [CampaignID]   INT               NOT NULL,
    [TotalSent]    INT               NOT NULL DEFAULT 0,  
    [TotalRead]    INT               NOT NULL DEFAULT 0, 
    [TotalClicked] INT               NOT NULL DEFAULT 0,  
    [ComputedAt]   DATETIME2(0)      NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_CampaignStats]                 PRIMARY KEY ([StatID]),
    CONSTRAINT [UQ_CampaignStats_CampaignID]      UNIQUE ([CampaignID]),
    CONSTRAINT [FK_CampaignStats_Campaigns]       FOREIGN KEY ([CampaignID]) REFERENCES [Notification].[Campaigns]([CampaignID]),
    CONSTRAINT [CK_CampaignStats_NonNegative]     CHECK ([TotalSent] >= 0 AND [TotalRead] >= 0 AND [TotalClicked] >= 0),
    CONSTRAINT [CK_CampaignStats_ReadNotExceedSent] CHECK ([TotalRead] <= [TotalSent]),
    CONSTRAINT [CK_CampaignStats_ClickedNotExceedSent] CHECK ([TotalClicked] <= [TotalSent])
);
GO

CREATE TABLE [Notification].[CampaignTargets] (
    [CampaignTargetID] INT IDENTITY(1,1) NOT NULL,
    [CampaignID]       INT               NOT NULL,
    [TargetType]       VARCHAR(20)       NOT NULL DEFAULT 'ACCOUNT_ID',
    [TargetValue]      VARCHAR(200)      NOT NULL,
    CONSTRAINT [PK_CampaignTargets]             PRIMARY KEY ([CampaignTargetID]),
    CONSTRAINT [CK_CampaignTargets_TargetType]  CHECK ([TargetType] IN ('ACCOUNT_ID', 'ROLE_ID')),
    CONSTRAINT [UQ_CampaignTargets_NoDuplicate] UNIQUE ([CampaignID], [TargetType], [TargetValue]),
    CONSTRAINT [FK_CampaignTargets_Campaigns]   FOREIGN KEY ([CampaignID]) REFERENCES [Notification].[Campaigns]([CampaignID])
);
GO

CREATE TABLE [Notification].[Deliveries] (
    [DeliveryID]       BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [AccountID]        INT                  NOT NULL,
    [CreatedByJobID]   INT                  NULL,
    [CampaignID]       INT                  NULL,
    [TemplateCode]     VARCHAR(50)          NULL,
    [RecipientType]    VARCHAR(15)          NOT NULL DEFAULT 'CUSTOMER'
        CONSTRAINT [CK_Deliveries_RecipientType] CHECK ([RecipientType] IN ('CUSTOMER', 'ADMIN', 'STAFF', 'MERCHANDISE')),
    [Channel]          VARCHAR(20)          NOT NULL DEFAULT 'WEB_BELL'
        CONSTRAINT [CK_Deliveries_Channel] CHECK ([Channel] IN ('WEB_BELL', 'EMAIL')),
    [ImageUrl]         NVARCHAR(500)        NULL,
    [NotificationType] VARCHAR(20)          NOT NULL DEFAULT 'SYSTEM'
        CONSTRAINT [CK_Deliveries_NotificationType] CHECK ([NotificationType] IN ('ORDER', 'PROMOTION', 'SYSTEM', 'BLOG', 'STOCK')),
    [ActionType]       NVARCHAR(20)         NULL,
    [ActionTarget]     NVARCHAR(500)        NULL,
    [Title]            NVARCHAR(255)        NOT NULL,
    [Message]          NVARCHAR(500)        NOT NULL,
    [Payload]          NVARCHAR(2000)       NOT NULL DEFAULT '{}'
        CONSTRAINT [CK_Deliveries_PayloadIsJson] CHECK (ISJSON([Payload]) = 1),
    [Status]           VARCHAR(10)          NOT NULL DEFAULT 'Unread'
        CONSTRAINT [CK_Deliveries_Status] CHECK ([Status] IN ('Unread', 'Read', 'Archived')),
    [ReadAt]           DATETIME2(0)         NULL,
    [EmailStatus]      VARCHAR(15)          NULL
        CONSTRAINT [CK_Deliveries_EmailStatus] CHECK ([EmailStatus] IN ('Pending', 'Sent', 'Failed')),
    [PushStatus]       VARCHAR(15)          NULL
        CONSTRAINT [CK_Deliveries_PushStatus] CHECK ([PushStatus] IN ('Pending', 'Sent', 'Failed')),
    [IdempotencyKey] VARCHAR(200) UNIQUE NULL,
    [IsDeleted]        BIT                  NOT NULL DEFAULT 0,
    [CreatedAt]        DATETIME2(0)         NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]        DATETIME2(0)         NULL,

    CONSTRAINT [FK_Deliveries_Accounts]  FOREIGN KEY ([AccountID])      REFERENCES [dbo].[Accounts]([AccountID]),
    CONSTRAINT [FK_Deliveries_Templates] FOREIGN KEY ([TemplateCode])   REFERENCES [Notification].[Templates]([TemplateCode]),
    CONSTRAINT [FK_Deliveries_Campaigns] FOREIGN KEY ([CampaignID])     REFERENCES [Notification].[Campaigns]([CampaignID]),
    CONSTRAINT [FK_Deliveries_Jobs]      FOREIGN KEY ([CreatedByJobID]) REFERENCES [System].[BackgroundJobs]([JobID]),

    CONSTRAINT [CK_Deliveries_ReadAtConsistency] CHECK ([Status] <> 'Read' OR [ReadAt] IS NOT NULL)
);
GO
 
CREATE INDEX [IX_Deliveries_AccountID_Status]    ON [Notification].[Deliveries] ([AccountID], [Status]) INCLUDE ([Title], [Message], [CreatedAt], [CampaignID], [NotificationType], [ImageUrl], [ActionType], [ActionTarget]) WHERE [IsDeleted] = 0;
CREATE INDEX [IX_Deliveries_AccountID_CreatedAt] ON [Notification].[Deliveries] ([AccountID], [CreatedAt] DESC) WHERE [IsDeleted] = 0;
CREATE INDEX [IX_Deliveries_PushStatus]          ON [Notification].[Deliveries] ([PushStatus], [CreatedAt]) WHERE [PushStatus] = 'Failed';
GO

CREATE UNIQUE INDEX [UQ_Deliveries_IdempotencyKey]
ON [Notification].[Deliveries]([IdempotencyKey])
WHERE [IdempotencyKey] IS NOT NULL;
GO

CREATE TABLE [Notification].[DeliveryActions] (
    [ActionID]     BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [DeliveryID]   BIGINT               NOT NULL,
    [AccountID]    INT                  NOT NULL,
    [ActionType]   VARCHAR(10)          NOT NULL
        CONSTRAINT [CK_DeliveryActions_ActionType] CHECK ([ActionType] IN ('Read', 'Click', 'Dismiss')),
    [ActionTarget] NVARCHAR(500)        NULL,
    [OccurredAt]   DATETIME2(0)         NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_DeliveryActions_Deliveries] FOREIGN KEY ([DeliveryID]) REFERENCES [Notification].[Deliveries]([DeliveryID]),
    CONSTRAINT [FK_DeliveryActions_Accounts]   FOREIGN KEY ([AccountID])  REFERENCES [dbo].[Accounts]([AccountID])
);
GO
 
CREATE UNIQUE INDEX [UQ_DeliveryActions_OneReadPerDelivery] ON [Notification].[DeliveryActions] ([DeliveryID], [AccountID]) WHERE [ActionType] = 'Read';
CREATE INDEX [IX_DeliveryActions_DeliveryID_OccurredAt]     ON [Notification].[DeliveryActions] ([DeliveryID], [OccurredAt] DESC);
GO

CREATE TABLE [ChatConversations] (
    [ConversationID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [AccountID]      INT NOT NULL,
    [SessionID]      VARCHAR(100) NULL,
    [Status]         VARCHAR(15) NOT NULL DEFAULT 'BotActive',
    [IsDeleted]      BIT NOT NULL DEFAULT 0,
    [CreatedAt]      DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]      DATETIME2(0) NULL,
    CONSTRAINT [FK_ChatConversations_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID])
);
GO

CREATE TABLE [ChatMessages] (
    [MessageID]      INT IDENTITY(1,1) PRIMARY KEY,
    [ConversationID] INT NOT NULL,
    [SenderType]     VARCHAR(10) NOT NULL,
    [Content]        NVARCHAR(1000) NOT NULL,
    [Payload]        NVARCHAR(2000) NULL,
    [CreatedAt]      DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_ChatMessages_ChatConversations]
        FOREIGN KEY ([ConversationID]) REFERENCES [ChatConversations]([ConversationID])
);
GO

CREATE TABLE [Interaction].[Events] (
    [EventID]       BIGINT IDENTITY(1,1) PRIMARY KEY,
    [AccountID]     INT NULL,
    [SessionID]     VARCHAR(100) NOT NULL,
    [EventType]     VARCHAR(50) NOT NULL,
    [EntityID]      VARCHAR(50) NOT NULL,
    [EntityType]    VARCHAR(30) NOT NULL,
    [Source]        VARCHAR(30) NULL,
    [Referrer]      VARCHAR(200) NULL,
    [DeviceType]    VARCHAR(15) NULL,
    [DurationMs]    INT NULL,
    [ScrollDepth]   TINYINT NULL,
    [ClickPosition] VARCHAR(30) NULL,
    [Metadata]      NVARCHAR(500) NULL,
    [CreatedAt]     DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_Events_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID])
);
GO

CREATE TABLE [Recommendation].[UserProductScores] (
    [ScoreID]          BIGINT IDENTITY(1,1) PRIMARY KEY,
    [AccountID]        INT NOT NULL,
    [ProductID]        INT NOT NULL,
    [Score]            DECIMAL(8,4) NOT NULL DEFAULT 0,
    [ViewCount]        SMALLINT NOT NULL DEFAULT 0,
    [CartCount]        TINYINT NOT NULL DEFAULT 0,
    [PurchaseCount]    TINYINT NOT NULL DEFAULT 0,
    [WishlistCount]    TINYINT NOT NULL DEFAULT 0,
    [LastInteractedAt] DATETIME2(0) NOT NULL,
    [ComputedAt]       DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [UQ_UserProductScores] UNIQUE ([AccountID], [ProductID]),
    CONSTRAINT [FK_UPS_Accounts]      FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_UPS_Products]      FOREIGN KEY ([ProductID]) REFERENCES [Products]([ProductID])
);
GO

CREATE TABLE [Recommendation].[ItemSimilarities] (
    [SimilarityID]     INT IDENTITY(1,1) PRIMARY KEY,
    [SourceProductID]  INT NOT NULL,
    [SimilarProductID] INT NOT NULL,
    [SimilarityScore]  DECIMAL(5,4) NOT NULL,
    [AlgorithmType]    VARCHAR(20) NOT NULL DEFAULT 'cf',
    [UpdatedAt]        DATETIME2(0) NULL,
    [CreatedAt]        DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_ItemSimilarities_SourceProduct]  FOREIGN KEY ([SourceProductID])  REFERENCES [Products]([ProductID]),
    CONSTRAINT [FK_ItemSimilarities_SimilarProduct] FOREIGN KEY ([SimilarProductID]) REFERENCES [Products]([ProductID])
);
GO

CREATE TABLE [Recommendation].[Widgets] (
    [WidgetID]     TINYINT IDENTITY(1,1) PRIMARY KEY,
    [WidgetCode]   VARCHAR(50) NOT NULL UNIQUE,
    [WidgetName]   NVARCHAR(100) NOT NULL,
    [Algorithm]    VARCHAR(20) NOT NULL,
    [MaxItems]     TINYINT NOT NULL DEFAULT 10,
    [FallbackAlgo] VARCHAR(20) NULL,
    [IsActive]     BIT NOT NULL DEFAULT 1,
    [Config]       NVARCHAR(500) NULL,
    [CreatedAt]    DATETIME2(0) NOT NULL DEFAULT GETDATE()
);
GO

CREATE TABLE [Recommendation].[TrendingProducts] (
    [TrendingID]    INT IDENTITY(1,1) PRIMARY KEY,
    [ProductID]     INT NOT NULL,
    [Scope]         VARCHAR(20) NOT NULL DEFAULT 'global',
    [Score]         DECIMAL(10,4) NOT NULL,
    [ViewCount]     INT NOT NULL DEFAULT 0,
    [PurchaseCount] INT NOT NULL DEFAULT 0,
    [Rank]          SMALLINT NOT NULL,
    [WindowHours]   TINYINT NOT NULL DEFAULT 24,
    [ComputedAt]    DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [UQ_Trending_Product_Scope_Window] UNIQUE ([ProductID], [Scope], [WindowHours]),
    CONSTRAINT [FK_Trending_Products] FOREIGN KEY ([ProductID]) REFERENCES [Products]([ProductID])
);
GO

CREATE NONCLUSTERED INDEX [IX_Trending_Scope_Rank]
ON [Recommendation].[TrendingProducts]([Scope], [WindowHours], [Rank])
INCLUDE ([ProductID], [Score]);
GO

/* =============================================
   10. PERFORMANCE INDEXES
============================================= */
CREATE NONCLUSTERED INDEX [IX_UserBlockHistory_PendingUnblock]
ON [UserBlockHistory] ([BlockedUntil], [AccountID])
WHERE [UnblockedAt] IS NULL;
GO

CREATE NONCLUSTERED INDEX [IX_UPS_User]
ON [Recommendation].[UserProductScores]([AccountID], [Score] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_ItemSimilarities_Score]
ON [Recommendation].[ItemSimilarities]([SourceProductID], [SimilarityScore] DESC)
INCLUDE ([SimilarProductID], [AlgorithmType]);
GO

CREATE NONCLUSTERED INDEX [IX_Vouchers_Worker]   ON [Vouchers]([Status], [StartDate], [EndDate]);
GO
CREATE NONCLUSTERED INDEX [IX_Promotions_Worker] ON [Promotions]([Status], [StartDate], [EndDate]);
GO

CREATE NONCLUSTERED INDEX [IX_Products_LowStock_V2]
ON [Products]([ProductStatus], [IsDeleted])
INCLUDE ([Quantity], [StockThreshold])
WHERE ([Quantity] <= 10 AND [IsDeleted] = 0 AND [ProductStatus] = 'Active');
GO

CREATE NONCLUSTERED INDEX [IX_NotificationDeliveries_User]
ON [Notification].[Deliveries]([AccountID], [RecipientType], [Status])
INCLUDE ([CreatedAt], [Title])
WHERE ([Status] = 'Unread' AND [RecipientType] = 'CUSTOMER');
GO

CREATE NONCLUSTERED INDEX [IX_NotificationDeliveries_Admin]
ON [Notification].[Deliveries]([RecipientType], [Status], [CreatedAt] DESC)
INCLUDE ([AccountID], [Title])
WHERE [RecipientType] <> 'CUSTOMER';
GO

CREATE NONCLUSTERED INDEX [IX_Orders_UserHistory]        ON [Orders]([AccountID], [OrderDate] DESC);
GO
CREATE NONCLUSTERED INDEX [IX_Wishlists_User]            ON [Wishlists]([AccountID]);
GO
CREATE NONCLUSTERED INDEX [IX_ChatMessages_Conversation] ON [ChatMessages]([ConversationID], [CreatedAt] DESC);
GO
CREATE NONCLUSTERED INDEX [IX_Products_FilterSort]       ON [Products]([CategoryID], [BrandID], [Price], [IsDeleted]);
GO

CREATE NONCLUSTERED INDEX [IX_InteractionEvents_UserBehavior]
ON [Interaction].[Events]([AccountID], [EventType], [CreatedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_ItemSimilarities_Source]
ON [Recommendation].[ItemSimilarities]([SourceProductID])
INCLUDE ([SimilarProductID], [SimilarityScore]);
GO

CREATE NONCLUSTERED INDEX [IX_Orders_ReportByDate]
ON [Orders]([OrderDate], [PaymentStatus])
INCLUDE ([TotalAmount], [VoucherDiscountAmount]);
GO

CREATE NONCLUSTERED INDEX [IX_OrderDetails_ProductSales]
ON [OrderDetails]([ProductID])
INCLUDE ([Quantity], [LineTotal]);
GO

CREATE NONCLUSTERED INDEX [IX_Products_Category_Status]
ON [Products]([CategoryID], [ProductStatus])
INCLUDE ([ProductName], [Price], [Quantity])
WHERE ([IsDeleted] = 0);
GO

CREATE NONCLUSTERED INDEX [IX_Products_Brand_Status]
ON [Products]([BrandID], [ProductStatus])
INCLUDE ([ProductName], [Price])
WHERE ([IsDeleted] = 0 AND [BrandID] IS NOT NULL);
GO

CREATE NONCLUSTERED INDEX [IX_Products_PriceRange_Status]
ON [Products]([PriceRangeID], [ProductStatus])
INCLUDE ([ProductName], [Price])
WHERE ([IsDeleted] = 0 AND [ProductStatus] = 'Active');
GO

CREATE NONCLUSTERED INDEX [IX_VoucherUsageLogs_Analytics] ON [VoucherUsageLogs]([VoucherID], [UsedAt]);
GO

CREATE NONCLUSTERED INDEX [IX_WalletTransactions_Wallet]
ON [WalletTransactions]([WalletID], [CreatedAt] DESC)
INCLUDE ([TxnType], [Amount], [Status]);
GO

CREATE NONCLUSTERED INDEX [IX_WalletTransactions_Account]
ON [WalletTransactions]([AccountID], [CreatedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_WalletTransactions_Order]
ON [WalletTransactions]([RelatedOrderID])
WHERE [RelatedOrderID] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX [IX_PaymentHistory_Order]
ON [PaymentHistory]([OrderID])
INCLUDE ([PaymentStatus], [Amount], [CreatedAt]);
GO

CREATE NONCLUSTERED INDEX [IX_OrderRefunds_Order]
ON [OrderRefunds]([OrderID])
INCLUDE ([RefundStatus], [ApprovedAmount]);
GO

CREATE NONCLUSTERED INDEX [IX_ReviewProducts_Product]
ON [ReviewProducts]([ProductID], [IsDeleted])
INCLUDE ([Rating], [CreatedAt]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_Accounts_EmployeeCode]
ON [Accounts]([EmployeeCode])
WHERE [EmployeeCode] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX [IX_DomainEventOutbox_Pending]
ON [System].[DomainEventOutbox]([OccurredOn])
WHERE [ProcessedOn] IS NULL;
GO

CREATE NONCLUSTERED INDEX [IX_Products_Search]
ON [Products]([ProductName], [CategoryID], [BrandID])
INCLUDE ([Price], [Quantity], [ProductStatus]);
GO

CREATE NONCLUSTERED INDEX [IX_Accounts_Login]
ON [Accounts]([Email], [IsActive], [IsDeleted])
INCLUDE ([PasswordHash], [RoleID]);
GO

CREATE NONCLUSTERED INDEX [IX_CartItems_ActiveCart]
ON [CartItems]([CartID], [RemovedAt])
WHERE [RemovedAt] IS NULL;
GO

CREATE NONCLUSTERED INDEX [IX_Orders_StatusTracking]
ON [Orders]([OrderCode], [PaymentStatus], [StatusID]);
GO

/* =============================================
   10.1. AI MODERATION PERFORMANCE INDEXES
============================================= */
CREATE NONCLUSTERED INDEX [IX_ReviewProducts_ModerationPending]
ON [dbo].[ReviewProducts] ([ModerationStatus], [CreatedAt] ASC)
INCLUDE ([ReviewID], [AccountID], [ProductID], [Comment])
WHERE [ModerationStatus] = 'Pending' AND [IsDeleted] = 0;
GO

CREATE NONCLUSTERED INDEX [IX_ReviewProducts_ManualReview]
ON [dbo].[ReviewProducts] ([ModerationStatus], [CreatedAt] ASC)
INCLUDE ([ReviewID], [AccountID], [ProductID])
WHERE [ModerationStatus] = 'ManualReview' AND [IsDeleted] = 0;
GO

CREATE NONCLUSTERED INDEX [IX_ReviewProductImages_ModerationPending]
ON [dbo].[ReviewProductImages] ([ModerationStatus], [CreatedAt] ASC)
INCLUDE ([ReviewProductImageID], [ReviewProductID], [ImageURL])
WHERE [ModerationStatus] = 'Pending' AND [IsDeleted] = 0;
GO

/* ── v3.2: Index hỗ trợ audit promotion trong OrderDetails ── */
CREATE NONCLUSTERED INDEX [IX_OrderDetails_PromotionID]
ON [OrderDetails]([PromotionID])
WHERE [PromotionID] IS NOT NULL;
GO

/* =============================================
   11. UNIQUE INDEXES FOR BUSINESS RULES
============================================= */
CREATE UNIQUE NONCLUSTERED INDEX [IX_Addresses_OneDefaultPerUser]
ON [Addresses]([AccountID])
WHERE [IsDefault] = 1 AND [IsDeleted] = 0;
GO

CREATE NONCLUSTERED INDEX [IX_BlogPosts_Status_Date]
ON [BlogPosts]([Status], [IsDeleted], [BlogAt] DESC)
INCLUDE ([BlogTitle], [BlogThumbnail]);
GO

CREATE NONCLUSTERED INDEX [IX_BlogPosts_Category] ON [BlogPosts]([BlogCategoryID]);
GO

CREATE NONCLUSTERED INDEX [IX_ReviewBlogs_BlogPost]
ON [ReviewBlogs]([BlogPostID], [IsDeleted])
INCLUDE ([Comment], [CreatedAt]);
GO

/* =============================================
   12. STORED PROCEDURES (SHIFT ASSIGNMENT)
============================================= */
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
    JOIN [WorkSchedules]  ws  ON ws.[ScheduleID]      = ssc.[ScheduleID]
    JOIN [ShiftTemplates] st  ON st.[ShiftTemplateID] = ws.[ShiftTemplateID]
    JOIN [Accounts]       a   ON a.[AccountID]        = ssc.[AccountID]
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
    JOIN [WorkSchedules]  ws  ON ws.[ScheduleID]      = ssc.[ScheduleID]
    JOIN [ShiftTemplates] st  ON st.[ShiftTemplateID] = ws.[ShiftTemplateID]
    JOIN [Accounts]       a   ON a.[AccountID]        = ssc.[AccountID]
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
                        SELECT 1 FROM WorkSchedules ws
                        JOIN Accounts a ON a.AccountID = ws.AccountID
                        WHERE ws.Status = 'OnDuty' AND a.RoleID = @StaffRoleId
                    ) THEN 'ALL_STAFF_FULL' ELSE 'NO_STAFF_ON_DUTY' END
                ELSE
                    CASE WHEN EXISTS(
                        SELECT 1 FROM WorkSchedules ws
                        JOIN Accounts a ON a.AccountID = ws.AccountID
                        WHERE ws.Status = 'OnDuty' AND a.RoleID = @MerchRoleId
                    ) THEN 'ALL_MERCH_FULL' ELSE 'NO_MERCH_ON_DUTY' END
            END;

        IF NOT EXISTS (SELECT 1 FROM [OrderQueue] WHERE [OrderID] = @OrderID AND [IsResolved] = 0)
        BEGIN
            INSERT INTO [OrderQueue] ([OrderID], [Reason]) VALUES (@OrderID, @QueueReason);
        END

        COMMIT TRANSACTION;

        SELECT 'QUEUED' AS [Result], @QueueReason AS [Reason], NULL AS [StaffAccountID], NULL AS [MerchAccountID];
        RETURN;
    END;

    INSERT INTO [OrderAssignments] ([OrderID], [ScheduleID], [AccountID], [RoleID], [AssignedBy])
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

    INSERT INTO [OrderAssignments] ([OrderID], [ScheduleID], [AccountID], [RoleID], [AssignedBy], [Notes])
    SELECT @OrderID, @NewScheduleID, ws.[AccountID], @RoleID, @AssignedBy, @Notes
    FROM [WorkSchedules] ws
    WHERE ws.[ScheduleID] = @NewScheduleID;

    UPDATE [StaffShiftCapacity]
    SET [CurrentLoad] = [CurrentLoad] + 1, [UpdatedAt] = GETDATE()
    WHERE [ScheduleID] = @NewScheduleID;

    COMMIT TRANSACTION;
END;
GO

CREATE NONCLUSTERED INDEX [IX_ReviewBlogReplies_Review]
ON [ReviewBlogReplies]([ReviewBlogID], [IsDeleted]);
GO

CREATE NONCLUSTERED INDEX [IX_ReviewBlogReactions_Stats]
ON [ReviewBlogReactions]([ReviewBlogID], [ReactionTypeID]);
GO

CREATE NONCLUSTERED INDEX [IX_ReviewProductReactions_Stats]
ON [ReviewProductReactions]([ReviewProductID], [ReactionTypeID])
WHERE [IsDeleted] = 0;
GO

CREATE INDEX [IX_PayGwTxn_PaymentHistoryID]
ON [PaymentGatewayTransactions]([PaymentHistoryID])
WHERE [PaymentHistoryID] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX [IX_Products_ComingSoon_Launch]
ON [Products]([ProductStatus], [LaunchDate])
WHERE [ProductStatus] = 'ComingSoon' AND [IsDeleted] = 0;
GO

CREATE NONCLUSTERED INDEX [IX_Deliveries_NotificationType]
ON [Notification].[Deliveries]([AccountID], [NotificationType], [Status])
INCLUDE ([Title], [CreatedAt])
WHERE [IsDeleted] = 0;
GO

/* =============================================
   WALLET PIN MANAGEMENT
============================================= */

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
    CONSTRAINT [CK_WalletPins_FailedAttempts] CHECK ([FailedAttempts]      BETWEEN 0 AND 3),
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
    CONSTRAINT [FK_WalletPinAttempts_Wallets]  FOREIGN KEY ([WalletID])  REFERENCES [Wallets]([WalletID]),
    CONSTRAINT [FK_WalletPinAttempts_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID])
);
GO

CREATE NONCLUSTERED INDEX [IX_WalletPinAttempts_Wallet]
ON [WalletPinAttempts]([WalletID], [CreatedAt] DESC);
GO