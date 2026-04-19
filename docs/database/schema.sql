/* =================================================================
   E-COMMERCE DATABASE SCHEMA (OPTIMIZED FULL VERSION)
   Platform: SQL Server | Version: 2.0
================================================================= */
USE [master];
GO
-- Tạo database nếu chưa tồn tại
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
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'Interaction') EXEC('CREATE SCHEMA [Interaction]')
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'Recommendation') EXEC('CREATE SCHEMA [Recommendation]')
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'System') EXEC('CREATE SCHEMA [System]')
GO


-- Outbox Pattern cho Event-Driven Architecture
CREATE TABLE [System].[DomainEventOutbox] (
    [EventID] UNIQUEIDENTIFIER PRIMARY KEY, -- 128-bit UUID
    [AggregateType] VARCHAR(100) NOT NULL, -- max 100 ASCII chars
    [AggregateId] VARCHAR(100) NOT NULL, -- max 100 ASCII chars
    [EventType] VARCHAR(100) NOT NULL, -- max 100 ASCII chars
    [Payload] NVARCHAR(1000) NOT NULL, -- JSON with Unicode support
    [OccurredOn] DATETIME2(0) NOT NULL, -- 1 sec precision, 1900-9999
    [ProcessingLockId] UNIQUEIDENTIFIER NULL, -- 128-bit UUID
    [ProcessingAt] DATETIME2(0) NULL, -- 1 sec precision, 1900-9999
    [Attempts] TINYINT NOT NULL DEFAULT 0, -- 0-255
    [LastError] NVARCHAR(MAX) NULL, -- Unicode text
    [ProcessedOn] DATETIME2(0) NULL -- 1 sec precision, NULL = Chưa xử lý
);
GO

CREATE TABLE [System].[BackgroundJobs] (
    [JobID] INT IDENTITY(1,1) PRIMARY KEY, -- -2,147,483,648 to 2,147,483,647
    [JobName] VARCHAR(100) NOT NULL UNIQUE, -- max 100 ASCII chars
    [CronExpression] VARCHAR(50) NULL, -- max 50 ASCII chars
    [IsEnabled] BIT NOT NULL DEFAULT 1, -- 0-1
    [LastRunTime] DATETIME2(0) NULL, -- 1 sec precision, 1900-9999
    [NextRunTime] DATETIME2(0) NULL, -- 1 sec precision, 1900-9999
    [LastRunStatus] VARCHAR(20) NULL, -- max 20 ASCII chars
    [LastRunMessage] NVARCHAR(MAX) NULL -- Unicode text
);
GO

/* =============================================
   1. USER MANAGEMENT (IAM)
============================================= */
CREATE TABLE [Roles] (
    [RoleID] TINYINT IDENTITY(1,1) PRIMARY KEY, -- 0-255
    [RoleName] VARCHAR(50) NOT NULL UNIQUE, -- max 50 ASCII
    [Description] NVARCHAR(255) NULL, -- max 255 Unicode
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE()
);
GO

CREATE TABLE [Accounts] (
    [AccountID] INT IDENTITY(1,1) PRIMARY KEY, -- -2.1B to 2.1B
    [RoleID] TINYINT NOT NULL, -- 0-255
    [EmployeeCode] VARCHAR(20) NULL, -- max 20 ASCII
    [AccountName] NVARCHAR(100) NOT NULL, -- max 100 Unicode
    [PhoneNumber] VARCHAR(15) NULL, -- max 15 ASCII
    [Email] VARCHAR(100) NOT NULL UNIQUE, -- max 255 ASCII
    [ImageURL] VARCHAR(500) NULL, -- max 500 ASCII (URL)
    [PasswordHash] VARCHAR(255) NOT NULL, -- max 255 ASCII
    [IsActive] BIT NOT NULL DEFAULT 1, -- 0-1
    [IsDeleted] BIT NOT NULL DEFAULT 0, -- 0-1
    [Provider] VARCHAR(20) NULL, -- max 20 ASCII
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(), -- 1 sec precision
    [UpdatedAt] DATETIME2(0) NULL, -- 1 sec precision
    CONSTRAINT [FK_Accounts_Roles] FOREIGN KEY ([RoleID]) REFERENCES [Roles]([RoleID])
);

GO
CREATE TABLE [dbo].[AuditLogs] (
    [AuditID]      BIGINT           IDENTITY(1,1) PRIMARY KEY,
    [EntityType]   VARCHAR(20)   NOT NULL
        CONSTRAINT [CK_AuditLogs_EntityType]
        CHECK ([EntityType] IN ('Product','Order','Payment','Promotion','Voucher')),
    [EntityID]     VARCHAR(30)           NOT NULL,
    [Action]       VARCHAR(30)   NOT NULL
        CONSTRAINT [CK_AuditLogs_Action]
        CHECK ([Action] IN (
            'Create','Update','Delete','Activate','Deactivate',
            'Confirm','Cancel','Assign','Refund','Override','Expire', 'Approve','Reject'   
        )),
    [PerformedBy]  INT           NOT NULL,
    [IPAddress]    VARCHAR(45)   NULL,   -- IPv6 max 45 chars
    [CreatedAt]    DATETIME2(0)  NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_AuditLogs_Accounts]
        FOREIGN KEY ([PerformedBy]) REFERENCES [Accounts]([AccountID])
);
GO


CREATE TABLE [BlockReasons] (
    [BlockReasonID] TINYINT IDENTITY(1,1) PRIMARY KEY, -- 0-255
    [Content] NVARCHAR(150) NOT NULL, -- max 150 Unicode
    [Description] NVARCHAR(255) NULL, -- max 255 Unicode
    [IsDeleted] BIT NOT NULL DEFAULT 0, -- 0-1
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE() -- 1 sec precision
);

GO

CREATE TABLE [UserBlockHistory] (
    [BlockID] INT IDENTITY(1,1) PRIMARY KEY, -- -2.1B to 2.1B
    [AccountID] INT NOT NULL, -- -2.1B to 2.1B
    [BlockedBy] INT NULL, -- NULL = System auto-block
    [BlockReasonID] TINYINT NOT NULL,
    [Note] NVARCHAR(500) NULL,           -- Ghi chú tự do từ Admin
    [UnblockedBy] INT NULL,              -- Admin mở thủ công
    [UnblockedByJobID] INT NULL,         -- BackgroundJob mở tự động
    [BlockedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [BlockedUntil] DATETIME2(0) NOT NULL, -- NULL sẽ bị reject bởi CHECK
    [UnblockedAt] DATETIME2(0) NULL,
    CONSTRAINT [FK_UserBlockHistory_Account] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_UserBlockHistory_BlockedBy] FOREIGN KEY ([BlockedBy]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_UserBlockHistory_UnblockedBy] FOREIGN KEY ([UnblockedBy]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_UserBlockHistory_BlockReasons] FOREIGN KEY ([BlockReasonID]) REFERENCES [BlockReasons]([BlockReasonID]),
    CONSTRAINT [FK_UserBlockHistory_BackgroundJobs] FOREIGN KEY ([UnblockedByJobID]) REFERENCES [System].[BackgroundJobs]([JobID]),
    -- Fix: BlockedUntil phải sau BlockedAt (tránh khóa thời gian âm)
    CONSTRAINT [CK_UserBlockHistory_ValidPeriod] CHECK ([BlockedUntil] > [BlockedAt]),
    -- Fix: UnblockedAt phải sau BlockedAt
    CONSTRAINT [CK_UserBlockHistory_UnblockedAfterBlocked] CHECK ([UnblockedAt] IS NULL OR [UnblockedAt] >= [BlockedAt]),
    -- Fix: Chỉ 1 actor mở khóa (Admin XOR BackgroundJob)
    CONSTRAINT [CK_UserBlockHistory_OneUnblockActor] CHECK ([UnblockedBy] IS NULL OR [UnblockedByJobID] IS NULL)
);
GO

/* =============================================
   1.1. ADMINISTRATIVE DIVISIONS (Tỉnh/Huyện/Xã)
   Chuẩn theo mã GHN / GHTK
============================================= */
CREATE TABLE Provinces (
    ProvinceId   INT PRIMARY KEY,
    ProvinceName NVARCHAR(100) NOT NULL,
    ProvinceCode VARCHAR(10) NULL,

    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE()
);
GO


CREATE TABLE Districts (
    DistrictId   INT PRIMARY KEY,
    ProvinceId   INT NOT NULL,
    DistrictName NVARCHAR(100) NOT NULL,

    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE(),

    CONSTRAINT FK_Districts_Provinces 
        FOREIGN KEY (ProvinceId) REFERENCES Provinces(ProvinceId)
);
GO

CREATE INDEX IX_Districts_ProvinceId ON Districts(ProvinceId);
GO

CREATE TABLE Wards (
    WardCode   VARCHAR(20) PRIMARY KEY,
    DistrictId INT NOT NULL,
    WardName   NVARCHAR(100) NOT NULL,

    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE(),

    CONSTRAINT FK_Wards_Districts 
        FOREIGN KEY (DistrictId) REFERENCES Districts(DistrictId)
);
GO


CREATE INDEX IX_Wards_DistrictId ON Wards(DistrictId);
GO

CREATE TABLE [dbo].[Addresses](
    [AddressID]     INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [AccountID]     INT           NOT NULL,
    [RecipientName] NVARCHAR(100) NULL,
    [PhoneNumber]   NVARCHAR(20)  NULL,
    [AddressLine]   NVARCHAR(500) NOT NULL,  -- Số nhà, tên đường
    [WardCode]      VARCHAR(20)   NULL,       -- FK → Wards
    [DistrictId]    INT           NULL,       -- FK → Districts
    [ProvinceId]    INT           NULL,       -- FK → Provinces
    [IsDefault]     BIT           NOT NULL DEFAULT 0,
    [IsDeleted]     BIT           NOT NULL DEFAULT 0,
    [CreatedAt]     DATETIME2(0)  NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]     DATETIME2(0)  NULL,
    CONSTRAINT [FK_Addresses_Accounts]   FOREIGN KEY ([AccountID])  REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_Addresses_Wards]      FOREIGN KEY ([WardCode])   REFERENCES [Wards]([WardCode]),
    CONSTRAINT [FK_Addresses_Districts]  FOREIGN KEY ([DistrictId]) REFERENCES [Districts]([DistrictId]),
    CONSTRAINT [FK_Addresses_Provinces]  FOREIGN KEY ([ProvinceId]) REFERENCES [Provinces]([ProvinceId])
);
GO

/* =============================================
   2. PRODUCT CATALOG (PIM)
============================================= */
CREATE TABLE [SuperCategories] (
    [SuperCategoryID] SMALLINT IDENTITY(1,1) PRIMARY KEY, -- -32,768 to 32,767
    [SuperCategoryName] NVARCHAR(25) NOT NULL UNIQUE, -- max 25 Unicode
    [IsDeleted] BIT NOT NULL DEFAULT 0, -- 0-1
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(), -- 1 sec precision
    [UpdatedAt] DATETIME2(0) NULL -- 1 sec precision
);
GO

CREATE TABLE [Categories] (
    [CategoryID] SMALLINT IDENTITY(1,1) PRIMARY KEY, -- -32,768 to 32,767
    [SuperCategoryID] SMALLINT  NOT NULL, -- -32,768 to 32,767
    [CategoryName] NVARCHAR(25) NOT NULL UNIQUE, -- max 25 Unicode
    [IsDeleted] BIT NOT NULL DEFAULT 0, -- 0-1
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(), -- 1 sec precision
    [UpdatedAt] DATETIME2(0) NULL, -- 1 sec precision
    CONSTRAINT [FK_Categories_SuperCategories] FOREIGN KEY ([SuperCategoryID]) REFERENCES [SuperCategories]([SuperCategoryID])
);
GO

CREATE TABLE [Materials] (
    [MaterialID] SMALLINT IDENTITY(1,1) PRIMARY KEY, -- -32,768 to 32,767
    [MaterialName] NVARCHAR(25) NOT NULL UNIQUE, -- max 25 Unicode
    [IsDeleted] BIT NOT NULL DEFAULT 0, -- 0-1
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(), -- 1 sec precision
    [UpdatedAt] DATETIME2(0) NULL -- 1 sec precision
);
GO

CREATE TABLE [Ages] (
    [AgeID] TINYINT IDENTITY(1,1) PRIMARY KEY, -- 0-255
    [AgeRange] VARCHAR(50) NOT NULL UNIQUE, -- max 50 ASCII
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE() -- 1 sec precision
);
GO

CREATE TABLE [Sexes] (
    [SexID] TINYINT IDENTITY(1,1) PRIMARY KEY, -- 0-255
    [SexName] NVARCHAR(4) NOT NULL UNIQUE, -- max 20 Unicode
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE() -- 1 sec precision
);
GO

CREATE TABLE [Origins] (
    [OriginID] TINYINT IDENTITY(1,1) PRIMARY KEY, -- 0-255
    [OriginName] NVARCHAR(100) NOT NULL UNIQUE, -- max 100 Unicode
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE() -- 1 sec precision
);
GO

CREATE TABLE [Brands] (
    [BrandID] SMALLINT IDENTITY(1,1) PRIMARY KEY, -- -32,768 to 32,767
    [BrandName] NVARCHAR(100) NOT NULL UNIQUE, -- max 100 Unicode
    [IsDeleted] BIT NOT NULL DEFAULT 0, -- 0-1
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(), -- 1 sec precision
    [UpdatedAt] DATETIME2(0) NULL -- 1 sec precision
);
GO

CREATE TABLE [PriceRanges] (
    [PriceRangeID] TINYINT IDENTITY(1,1) PRIMARY KEY, -- 0-255
    [PriceRangeMin] DECIMAL(12,0) NOT NULL CHECK ([PriceRangeMin] >= 0), -- -999B to 999B (VND, no decimals)
    [PriceRangeMax] DECIMAL(12,0) NOT NULL, -- -999B to 999B (VND, no decimals)
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(), -- 1 sec precision
    CONSTRAINT [CK_PriceRanges_MinMax] CHECK ([PriceRangeMin] < [PriceRangeMax])
);
GO

CREATE TABLE [Promotions] (    
    [PromotionID] INT IDENTITY(1,1) PRIMARY KEY, -- -2.1B to 2.1B
    [CreatedBy] INT NOT NULL, 
    [PromotionName] NVARCHAR(200) NOT NULL, -- max 200 Unicode
    [PromotionType] VARCHAR(20) NOT NULL
        CONSTRAINT [CK_Promotions_Type] CHECK ([PromotionType] IN ('FLASH_SALE', 'SEASONAL', 'CLEARANCE', 'BUNDLE')),
    [Description] NVARCHAR(MAX) NULL, -- Unicode text (unlimited)
    [DiscountPercent] DECIMAL(5,2) NOT NULL CHECK ([DiscountPercent] BETWEEN 0 AND 100), -- 0-100 (2 decimals)
    [StartDate] DATETIME2(0) NOT NULL, -- 1 sec precision
    [EndDate] DATETIME2(0) NOT NULL, -- 1 sec precision
    [Status] VARCHAR(20) NOT NULL CHECK ([Status] IN ('Scheduled', 'Active', 'Expired', 'Inactive')), -- max 20 ASCII
    [IsDeleted] BIT NOT NULL DEFAULT 0, -- 0-1
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(), -- 1 sec precision
    CONSTRAINT [FK_Promotions_Accounts] FOREIGN KEY ([CreatedBy]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [CK_Promotions_DateRange] CHECK ([StartDate] < [EndDate])
);
GO

CREATE TABLE [Products] (
    [ProductID] INT IDENTITY(1,1) PRIMARY KEY, -- -2.1B to 2.1B
    [ProductName] NVARCHAR(255) NOT NULL, -- max 255 Unicode
    [Price] DECIMAL(12,0) NOT NULL CHECK ([Price] >= 0), -- -999B to 999B VND
    [Quantity] INT NOT NULL CHECK ([Quantity] >= 0), -- -2.1B to 2.1B
    [ProductStatus] VARCHAR(20) NOT NULL CHECK ([ProductStatus] IN ('Active', 'Inactive', 'OutOfStock', 'Discontinued')), -- max 20 ASCII
    [IsDeleted] BIT NOT NULL DEFAULT 0, -- 0-1
    [StockThreshold] SMALLINT NOT NULL DEFAULT 10, -- -32,768 to 32,767
    [LowStockNotificationEnabled] BIT NOT NULL DEFAULT 1, -- 0-1 (toggle notification)
    [LastLowStockNotifiedAt] DATETIME2(0) NULL, -- 1 sec precision (last notification time)
    [CategoryID] SMALLINT NOT NULL, -- -32,768 to 32,767
    [BrandID] SMALLINT NULL, -- -32,768 to 32,767
    [PriceRangeID] TINYINT NULL, -- 0-255
    [PromotionID] INT NULL, -- -2.1B to 2.1B
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(), -- 1 sec precision
    [UpdatedAt] DATETIME2(0) NULL, -- 1 sec precision
    CONSTRAINT [FK_Products_Categories] FOREIGN KEY ([CategoryID]) REFERENCES [Categories]([CategoryID]),
    CONSTRAINT [FK_Products_Brands] FOREIGN KEY ([BrandID]) REFERENCES [Brands]([BrandID]),
    CONSTRAINT [FK_Products_PriceRanges] FOREIGN KEY ([PriceRangeID]) REFERENCES [PriceRanges]([PriceRangeID]),
    CONSTRAINT [FK_Products_Promotions] FOREIGN KEY ([PromotionID]) REFERENCES [Promotions]([PromotionID])
);
GO

-- ProductPromotions: Bảng nhiều-nhiều để gắn nhiều promotion vào 1 sản phẩm (flash sale, theo nhóm)
-- Lưu ý: Products.PromotionID là promotion "mặc định" hiển thị trên giao diện (1-1),
-- còn ProductPromotions dùng để quản lý chi tiết giá sale, số lượng từng đợt promo (1-nhiều)
CREATE TABLE [ProductPromotions] (
    [ProductID]    INT            NOT NULL,
    [PromotionID]  INT            NOT NULL,
    [SalePrice]    DECIMAL(12,0)  NOT NULL CHECK ([SalePrice] > 0),  -- Phải nhỏ hơn giá gốc (enforce ở backend)
    [SaleQuantity] INT            NULL,           -- NULL = không giới hạn
    [SoldQuantity] INT            NOT NULL DEFAULT 0,
    [CreatedAt]    DATETIME2      NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_ProductPromotions] PRIMARY KEY ([ProductID], [PromotionID]),
    CONSTRAINT [FK_ProductPromotions_Products]   FOREIGN KEY ([ProductID])   REFERENCES [Products]([ProductID]),
    CONSTRAINT [FK_ProductPromotions_Promotions] FOREIGN KEY ([PromotionID]) REFERENCES [Promotions]([PromotionID])
);
GO

CREATE TABLE [ProductDetails] (
    [ProductID] INT NOT NULL PRIMARY KEY, -- -2.1B to 2.1B
    [Description] NVARCHAR(1500) NULL,   -- 1500
    [MaterialID] SMALLINT NULL, -- -32,768 to 32,767
    [AgeID] TINYINT NULL, -- 0-255
    [SexID] TINYINT NULL, -- 0-255
    [OriginID] TINYINT NULL, 
    CONSTRAINT [FK_ProductDetails_Products] FOREIGN KEY ([ProductID]) REFERENCES [Products]([ProductID]),
    CONSTRAINT [FK_ProductDetails_Materials] FOREIGN KEY ([MaterialID]) REFERENCES [Materials]([MaterialID]),
    CONSTRAINT [FK_ProductDetails_Ages] FOREIGN KEY ([AgeID]) REFERENCES [Ages]([AgeID]),
    CONSTRAINT [FK_ProductDetails_Sexes] FOREIGN KEY ([SexID]) REFERENCES [Sexes]([SexID]),
    CONSTRAINT [FK_ProductDetails_Origins] FOREIGN KEY ([OriginID]) REFERENCES [Origins]([OriginID])
);
GO

CREATE TABLE [ProductImages] (
    [ImageID] INT IDENTITY(1,1) PRIMARY KEY, -- -2.1B to 2.1B
    [ProductID] INT NOT NULL, -- -2.1B to 2.1B
    [ImageUrl] VARCHAR(500) NOT NULL, -- max 500 ASCII (URL)
    [IsMain] BIT NOT NULL DEFAULT 0, -- 0-1
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(), -- 1 sec precision
    [UpdatedAt] DATETIME2(0) NULL, -- 1 sec precision
    CONSTRAINT [FK_ProductImages_Products] FOREIGN KEY ([ProductID]) REFERENCES [Products]([ProductID])
);
GO

-- Fix 8: Chỉ cho phép 1 ảnh IsMain = 1 mỗi sản phẩm
-- Tránh duplicate trong SP_GetCart và SP_PlaceOrder khi query WHERE IsMain = 1
CREATE UNIQUE INDEX [UQ_ProductImages_OneMain]
ON [ProductImages]([ProductID])
WHERE ([IsMain] = 1);
GO


/* =============================================
   3. ORDER MANAGEMENT (OMS)
============================================= */
CREATE TABLE [StatusOrders] (
    [StatusID] TINYINT IDENTITY(1,1) PRIMARY KEY, -- 0-255
    [StatusName] VARCHAR(50) NOT NULL UNIQUE, -- max 50 ASCII
    [Description] NVARCHAR(255) NULL, -- max 255 Unicode
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(), -- 1 sec precision
    [UpdatedAt] DATETIME2(0) NULL -- 1 sec precision
);
GO

CREATE TABLE [Orders] (
    [OrderID]              INT           IDENTITY(1,1) PRIMARY KEY,
    [AccountID]            INT           NOT NULL,
    [StatusID]             TINYINT       NOT NULL,
    [AssignedToStaffID]    INT           NULL,
    [OrderCode]            VARCHAR(30)   NOT NULL UNIQUE,
    -- Thông tin người nhận
    [ShippingName]         NVARCHAR(100) NOT NULL,
    [ShippingPhone]        VARCHAR(15)   NOT NULL,
    [ShippingAddress]      NVARCHAR(500) NOT NULL,   -- Số nhà, tên đường
    [ShippingWardCode]     VARCHAR(20)   NOT NULL,   -- FK → Wards
    [ShippingWardName]     NVARCHAR(100) NOT NULL,   -- Snapshot tên xã/phường
    [ShippingDistrictId]   INT           NOT NULL,   -- FK → Districts
    [ShippingDistrictName] NVARCHAR(100) NOT NULL,   -- Snapshot tên quận/huyện
    [ShippingProvinceId]   INT           NOT NULL,   -- FK → Provinces
    [ShippingProvinceName] NVARCHAR(100) NOT NULL,   -- Snapshot tên tỉnh/thành phố
    -- Timeline
    [OrderDate]            DATETIME2(0)  NOT NULL DEFAULT GETDATE(),
    [ConfirmedAt]          DATETIME2(0)  NULL,
    [ShippedAt]            DATETIME2(0)  NULL,
    [DeliveredAt]          DATETIME2(0)  NULL,
    [CompletedAt]          DATETIME2(0)  NULL,
    [CancelledAt]          DATETIME2(0)  NULL,
    -- Thanh toán
    [PaymentMethod]        VARCHAR(20)   NOT NULL DEFAULT 'BANK_TRANSFER' CHECK ([PaymentMethod] IN ('BANK_TRANSFER', 'MOMO', 'SHIP_CODE', 'ZALOPAY', 'VNPAY', 'WALLET')),
    [PaymentStatus]        VARCHAR(20)   NOT NULL DEFAULT 'PENDING' CHECK ([PaymentStatus] IN ('PENDING', 'PAID', 'FAILED', 'EXPIRED', 'REFUNDED')),
    [PaymentCode]          VARCHAR(50)   NULL UNIQUE,    -- NULL khi chưa thanh toán, điền sau khi payment gateway trả về
    [PaidAt]               DATETIME2(0)  NULL,
    -- Tài chính
    [SubTotal]             DECIMAL(12,0) NOT NULL,
    [VoucherDiscountAmount] DECIMAL(12,0) NOT NULL DEFAULT 0,
    [EstimatedShippingFee] DECIMAL(10,0) NOT NULL,
    [ActualShippingFee]    DECIMAL(10,0) NULL,
    [TotalAmount]          DECIMAL(12,0) NOT NULL,
    -- Hủy đơn
    [CancelReason]         NVARCHAR(500) NULL,
    [CancelledBy]          INT           NULL,
    [IsDeleted]            BIT           NOT NULL DEFAULT 0,
    [CreatedAt]            DATETIME2(0)  NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]            DATETIME2(0)  NULL,
    CONSTRAINT FK_Orders_Accounts       FOREIGN KEY ([AccountID])          REFERENCES [Accounts]([AccountID]),
    CONSTRAINT FK_Orders_StatusOrders   FOREIGN KEY ([StatusID])           REFERENCES [StatusOrders]([StatusID]),
    CONSTRAINT FK_Orders_AssignedStaff  FOREIGN KEY ([AssignedToStaffID])  REFERENCES [Accounts]([AccountID]),
    CONSTRAINT FK_Orders_CancelledBy    FOREIGN KEY ([CancelledBy])        REFERENCES [Accounts]([AccountID]),
    CONSTRAINT CK_Orders_Amounts CHECK (
        [SubTotal] >= 0 AND [EstimatedShippingFee] >= 0 AND ([ActualShippingFee] IS NULL OR [ActualShippingFee] >= 0) AND [TotalAmount] >= 0
    ),
    -- Enforce thứ tự lifecycle: bước sau chỉ được set khi bước trước đã tồn tại
    -- Pattern: Order FSM (Finite State Machine) chuẩn OMS
    CONSTRAINT CK_Orders_Timestamps CHECK (
        ([ConfirmedAt]  IS NULL OR [ConfirmedAt]  >= [OrderDate])
        AND ([ShippedAt]    IS NULL OR [ConfirmedAt]  IS NOT NULL)   -- Không thể Ship nếu chưa Confirm
        AND ([DeliveredAt]  IS NULL OR [ShippedAt]    IS NOT NULL)   -- Không thể Delivered nếu chưa Ship
        AND ([CompletedAt]  IS NULL OR [DeliveredAt]  IS NOT NULL)   -- Không thể Complete nếu chưa Delivered
        AND ([ShippedAt]    IS NULL OR [ShippedAt]    >= [ConfirmedAt])
        AND ([DeliveredAt]  IS NULL OR [DeliveredAt]  >= [ShippedAt])
        AND ([CompletedAt]  IS NULL OR [CompletedAt]  >= [DeliveredAt])
        -- Fix 4: Đơn đã Completed không thể cancel trực tiếp (phải qua Refunding)
        AND NOT ([CompletedAt] IS NOT NULL AND [CancelledAt] IS NOT NULL)
    )
);
GO

CREATE TABLE [OrderDetails] (
    [OrderDetailID] INT IDENTITY(1,1) PRIMARY KEY,
    [OrderID] INT NOT NULL,
    [ProductID] INT NOT NULL,
    [ProductName] NVARCHAR(255) NOT NULL,
    [ProductImage] VARCHAR(500) NULL,
    [Quantity] SMALLINT NOT NULL CHECK ([Quantity] > 0),
    [UnitPrice] DECIMAL(12,0) NOT NULL CHECK ([UnitPrice] >= 0),
    [DiscountAmount] DECIMAL(12,0) NOT NULL DEFAULT 0 CHECK ([DiscountAmount] >= 0),
    [LineTotal] AS (IIF([Quantity] * [UnitPrice] - [DiscountAmount] < 0, 0, [Quantity] * [UnitPrice] - [DiscountAmount])) PERSISTED,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_OrderDetails_Orders] FOREIGN KEY ([OrderID]) REFERENCES [Orders]([OrderID]),
    CONSTRAINT [FK_OrderDetails_Products] FOREIGN KEY ([ProductID]) REFERENCES [Products]([ProductID]),
    CONSTRAINT [UQ_OrderDetails_OrderProduct] UNIQUE ([OrderID], [ProductID]),
    CONSTRAINT [CK_OrderDetails_DiscountNotExceedLine] CHECK ([DiscountAmount] <= [Quantity] * [UnitPrice])
);
GO

CREATE TABLE [OrderStatusHistory] (
    [HistoryID]  INT         IDENTITY(1,1) PRIMARY KEY,
    [OrderID]    INT         NOT NULL,
    [StatusID]   TINYINT     NOT NULL,
    [ChangedBy]  INT         NULL,
    [Note]       NVARCHAR(500) NULL,
    [CreatedAt]  DATETIME2(0) NOT NULL DEFAULT GETDATE(),  -- immutable, không có UpdatedAt
    CONSTRAINT FK_OrderStatusHistory_Orders    FOREIGN KEY ([OrderID])   REFERENCES [Orders]([OrderID]),
    CONSTRAINT FK_OrderStatusHistory_Status    FOREIGN KEY ([StatusID])  REFERENCES [StatusOrders]([StatusID]),
    CONSTRAINT FK_OrderStatusHistory_ChangedBy FOREIGN KEY ([ChangedBy]) REFERENCES [Accounts]([AccountID])
);
GO

-- Fix 6: OrderNotes — Ghi chú nội bộ của Staff về đơn hàng
-- (Khác OrderStatusHistory: ghi chú tự do, không ràng buộc vào trạng thái)
CREATE TABLE [OrderNotes] (
    [NoteID]    INT           IDENTITY(1,1) PRIMARY KEY,
    [OrderID]   INT           NOT NULL,
    [StaffID]   INT           NOT NULL,
    [Note]      NVARCHAR(1000) NOT NULL,
    [IsDeleted] BIT           NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(0)  NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_OrderNotes_Orders]  FOREIGN KEY ([OrderID])  REFERENCES [Orders]([OrderID]),
    CONSTRAINT [FK_OrderNotes_Staff]   FOREIGN KEY ([StaffID])  REFERENCES [Accounts]([AccountID])
);
GO

/* =============================================
   4. SHOPPING (Cart & Wishlist)
============================================= */
CREATE TABLE [Cart] (
    [CartID] INT IDENTITY(1,1) PRIMARY KEY,
    [AccountID] INT NOT NULL UNIQUE,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(0) NULL,
    CONSTRAINT [FK_Cart_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID])
);
GO

-- Fix 2: Tự động tạo Cart khi Customer đăng ký
-- RoleID = 2 (Customer) — Guest/Admin/Staff/Merchandise không cần giỏ hàng
CREATE OR ALTER TRIGGER [TR_Accounts_CreateCart]
ON [Accounts]
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [Cart] ([AccountID])
    SELECT [AccountID] FROM inserted
    WHERE [RoleID] = 2;  -- Chỉ Customer mới cần Cart
END;
GO


CREATE TABLE [CartItems] (
    [CartItemID] INT IDENTITY(1,1) PRIMARY KEY,
    [CartID] INT NOT NULL,
    [ProductID] INT NOT NULL,
    [Quantity] SMALLINT NOT NULL CHECK ([Quantity] > 0),
    [PriceAtThatTime] DECIMAL(12,0) NOT NULL,
    [CurrentPrice] DECIMAL(12,0) NOT NULL,
    [IsSelected] BIT NOT NULL DEFAULT 1,
    [AddedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [RemovedAt] DATETIME2(0) NULL,
    CONSTRAINT [FK_CartItems_Cart] FOREIGN KEY ([CartID]) REFERENCES [Cart]([CartID]),
    CONSTRAINT [FK_CartItems_Products] FOREIGN KEY ([ProductID]) REFERENCES [Products]([ProductID]),
    CONSTRAINT [UQ_CartItems_CartProduct] UNIQUE ([CartID], [ProductID])
);
GO

CREATE TABLE [Wishlists] (
    [WishlistID] INT IDENTITY(1,1) PRIMARY KEY,
    [AccountID] INT NOT NULL,
    [ProductID] INT NOT NULL,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [UQ_Wishlists_AccountProduct] UNIQUE ([AccountID], [ProductID]),
    CONSTRAINT [FK_Wishlists_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_Wishlists_Products] FOREIGN KEY ([ProductID]) REFERENCES [Products]([ProductID])
);
GO

/* =============================================
   5. VOUCHERS & PROMOTIONS
============================================= */

CREATE TABLE [Vouchers] (
    [VoucherID] INT IDENTITY(1,1) PRIMARY KEY,
    [CreatedBy] INT NULL,
    [VoucherCode] VARCHAR(30) NOT NULL UNIQUE,
    [VoucherName] NVARCHAR(255) NOT NULL,
    [VoucherDescription] NVARCHAR(255) NOT NULL,
    -- Hỗ trợ 2 loại voucher theo chuẩn Shopify/Magento Promotion Engine
    [DiscountType] VARCHAR(10) NOT NULL CHECK ([DiscountType] IN ('FIXED', 'PERCENTAGE')),
    [DiscountValue]  DECIMAL(5,2) NOT NULL CHECK ([DiscountValue] > 0), -- VND khi FIXED, % khi PERCENT (0–100)
    [MaxDiscountCap] DECIMAL(12,0) NULL,  -- Trần giảm tối đa (chỉ áp dụng khi DiscountType = 'PERCENT')
    [DiscountTarget] VARCHAR(20) NOT NULL CHECK ([DiscountTarget] IN ('ORDER_TOTAL', 'SHIPPING_FEE')),
    [MinOrderAmount] DECIMAL(12,0) NULL CHECK ([MinOrderAmount] >= 0),
    -- 3. CHỈNH SỬA: Tách Quantity thành Total và Used. Cho phép Total NULL để làm mã "Không giới hạn"
    [TotalQuantity] INT NULL CHECK ([TotalQuantity] > 0),
    [UsedQuantity] INT NOT NULL DEFAULT 0 CHECK ([UsedQuantity] >= 0),
    [MaxUsagePerUser] SMALLINT NULL DEFAULT 1,
    [StartDate] DATETIME2(0) NOT NULL,
    [EndDate] DATETIME2(0) NOT NULL,
    [Status] VARCHAR(15) NOT NULL CHECK ([Status] IN ('Scheduled', 'Active', 'Inactive', 'Expired')),
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(0) NULL,
    CONSTRAINT [FK_Vouchers_Accounts] FOREIGN KEY ([CreatedBy]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [CK_Vouchers_DiscountPercent] CHECK (
        [DiscountType] <> 'PERCENTAGE' OR ([DiscountValue] > 0 AND [DiscountValue] <= 100)
    ),
    CONSTRAINT [CK_Vouchers_DateRange] CHECK ([StartDate] < [EndDate]),
    -- Đảm bảo số lượt dùng không vượt quá tổng phát hành (bỏ qua check nếu TotalQuantity là NULL - mã không giới hạn)
    CONSTRAINT [CK_Vouchers_Quantity] CHECK ([TotalQuantity] IS NULL OR [UsedQuantity] <= [TotalQuantity]),
);
GO

CREATE TABLE [OrderVouchers] (
    [OrderID] INT NOT NULL,
    [VoucherID] INT NOT NULL,
    [DiscountAmountApplied] DECIMAL(12,0) NOT NULL CHECK ([DiscountAmountApplied] >= 0),
    CONSTRAINT [PK_OrderVouchers] PRIMARY KEY ([OrderID], [VoucherID]),
    CONSTRAINT [FK_OrderVouchers_Orders] FOREIGN KEY ([OrderID]) REFERENCES [Orders]([OrderID]),
    CONSTRAINT [FK_OrderVouchers_Vouchers] FOREIGN KEY ([VoucherID]) REFERENCES [Vouchers]([VoucherID])
);
GO

CREATE TABLE [VoucherUsageLogs] (
    [UsageID] INT IDENTITY(1,1) PRIMARY KEY,
    [VoucherID] INT NOT NULL,
    [AccountID] INT NOT NULL,
    [OrderID] INT NOT NULL,
    [UsedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_VoucherUsageLogs_Vouchers] FOREIGN KEY ([VoucherID]) REFERENCES [Vouchers]([VoucherID]),
    CONSTRAINT [FK_VoucherUsageLogs_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_VoucherUsageLogs_Orders] FOREIGN KEY ([OrderID]) REFERENCES [Orders]([OrderID])
);
GO
/* =============================================
   6. BLOG & CONTENT
============================================= */
CREATE TABLE [BlogCategories] (
    [BlogCategoryID] SMALLINT IDENTITY(1,1) PRIMARY KEY,
    [BlogCategoriesName] NVARCHAR(100) NOT NULL UNIQUE,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE()
);
GO

CREATE TABLE [BlogPosts] (
    [BlogPostID] INT IDENTITY(1,1) PRIMARY KEY,
    [AccountID] INT NOT NULL,
    [ApprovedBy] INT NULL,
    [BlogTitle] NVARCHAR(255) NOT NULL,
    [BlogContent] NVARCHAR(3000) NOT NULL,        
    [BlogThumbnail] VARCHAR(500) NULL,
    [Status] VARCHAR(10) NOT NULL CHECK ([Status] IN ('Draft', 'Pending', 'Approved', 'Rejected', 'Scheduled', 'Published')),
    [Reason] NVARCHAR(500) NULL,
    [IsFeatured] BIT NOT NULL DEFAULT 0,
    [BlogAt] DATETIME2(0) NULL,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(0) NULL,
    CONSTRAINT [FK_BlogPosts_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_BlogPosts_ApprovedBy] FOREIGN KEY ([ApprovedBy]) REFERENCES [Accounts]([AccountID])
);
GO

CREATE TABLE [BlogPostCategories] (
    [BlogPostID] INT NOT NULL,
    [BlogCategoryID] SMALLINT NOT NULL,
    CONSTRAINT [PK_BlogPostCategories] PRIMARY KEY ([BlogPostID], [BlogCategoryID]),
    CONSTRAINT [FK_BlogPostCategories_BlogPosts] FOREIGN KEY ([BlogPostID]) REFERENCES [BlogPosts]([BlogPostID]),
    CONSTRAINT [FK_BlogPostCategories_BlogCategories] FOREIGN KEY ([BlogCategoryID]) REFERENCES [BlogCategories]([BlogCategoryID])
);
GO

CREATE TABLE [Banners] (
    [BannerID] INT IDENTITY(1,1) PRIMARY KEY,
    [CreatedBy] INT NOT NULL,
    [BannerName] NVARCHAR(255) NOT NULL,
    [ImageUrl] VARCHAR(500) NOT NULL,
    [Position] VARCHAR(20) NOT NULL DEFAULT 'HomePage' CHECK ([Position] IN ('HomePage', 'CategoryPage', 'ProductPage', 'CheckoutPage')),
    [StartDate] DATETIME2(0) NULL,
    [EndDate] DATETIME2(0) NULL,
    [DisplayOrder] TINYINT NOT NULL DEFAULT 0 CHECK ([DisplayOrder] >= 0),
    [IsActive] BIT NOT NULL DEFAULT 1,
    [IsDefault] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(0) NULL,
    CONSTRAINT [FK_Banners_Accounts] FOREIGN KEY ([CreatedBy]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [CK_Banners_DateRange] CHECK ([StartDate] IS NULL OR [EndDate] IS NULL OR [StartDate] < [EndDate])
);
GO

/* =============================================
   7. REVIEWS & REACTIONS
============================================= */
CREATE TABLE [ReviewProducts] (
    [ReviewID] INT IDENTITY(1,1) PRIMARY KEY,
    [AccountID] INT NOT NULL,
    [ProductID] INT NOT NULL,
    [OrderID] INT NOT NULL,
    [Rating] TINYINT NOT NULL CHECK ([Rating] BETWEEN 1 AND 5),
    [Comment] NVARCHAR(500) NULL,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(0) NULL,
    CONSTRAINT [UQ_Review_Account_Order_Product] UNIQUE ([AccountID], [OrderID], [ProductID]),
    CONSTRAINT [FK_Reviews_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_Reviews_Products] FOREIGN KEY ([ProductID]) REFERENCES [Products]([ProductID]),
    CONSTRAINT [FK_Reviews_Orders] FOREIGN KEY ([OrderID]) REFERENCES [Orders]([OrderID])
);
GO

CREATE TABLE [ReviewProductImages] (
    [ReviewProductImageID] INT IDENTITY(1,1) PRIMARY KEY,
    [ReviewProductID] INT NOT NULL,
    [ImageURL] VARCHAR(500) NOT NULL,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_ReviewProductImages_ReviewProducts] FOREIGN KEY ([ReviewProductID]) REFERENCES [ReviewProducts]([ReviewID])
);
GO

CREATE TABLE [StaffReviewProductReplies] (
    [ReplyProductID] INT IDENTITY(1,1) PRIMARY KEY,
    [ReviewProductID] INT NOT NULL,
    [StaffID] INT NOT NULL,
    [Content] NVARCHAR(500) NOT NULL,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(0) NULL,
    CONSTRAINT [FK_ReviewProductReplies_ReviewProducts] FOREIGN KEY ([ReviewProductID]) REFERENCES [ReviewProducts]([ReviewID]),
    CONSTRAINT [FK_ReviewProductReplies_Accounts] FOREIGN KEY ([StaffID]) REFERENCES [Accounts]([AccountID])
);
GO

CREATE TABLE [ReviewProductReactions] (
    [ReactionProductID] INT IDENTITY(1,1) PRIMARY KEY,
    [ReviewProductID] INT NOT NULL,
    [AccountID] INT NOT NULL,
    [ReactionType] VARCHAR(10) NOT NULL CHECK ([ReactionType] IN ('Like', 'Dislike')),
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_ReviewProductReactions_ReviewProducts] FOREIGN KEY ([ReviewProductID]) REFERENCES [ReviewProducts]([ReviewID]),
    CONSTRAINT [FK_ReviewProductReactions_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [UQ_ReviewProductReactions_AccountReview] UNIQUE ([AccountID], [ReviewProductID])
);
GO

/* =============================================
   7.1. BLOG REVIEWS & INTERACTIONS (NEW)
============================================= */
-- 1. Bảng bình luận/đánh giá Blog (ReviewBlogs)
CREATE TABLE [ReviewBlogs] (
    [ReviewBlogID] INT IDENTITY(1,1) PRIMARY KEY,
    [BlogPostID] INT NOT NULL,
    [AccountID] INT NOT NULL,
    [Comment] NVARCHAR(500) NULL,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(0) NULL,
    CONSTRAINT [FK_ReviewBlogs_BlogPosts] FOREIGN KEY ([BlogPostID]) REFERENCES [BlogPosts]([BlogPostID]),
    CONSTRAINT [FK_ReviewBlogs_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID])
);
GO

-- 2. Bảng phản hồi bình luận Blog (ReviewBlogReplies)
CREATE TABLE [ReviewBlogReplies] (
    [ReplyBlogID] INT IDENTITY(1,1) PRIMARY KEY,
    [ReviewBlogID] INT NOT NULL,
    [AccountID] INT NOT NULL, -- Người trả lời (có thể là Admin hoặc User khác)
    [ParentReplyID] INT NULL,
    [ReplyToAccountID] INT NULL,
    [Comment] NVARCHAR(500) NOT NULL,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(0) NULL,
    CONSTRAINT [FK_ReviewBlogReplies_ReviewBlogs] FOREIGN KEY ([ReviewBlogID]) REFERENCES [ReviewBlogs]([ReviewBlogID]),
    CONSTRAINT [FK_ReviewBlogReplies_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_ReviewBlogReplies_Parent] FOREIGN KEY ([ParentReplyID]) REFERENCES [ReviewBlogReplies]([ReplyBlogID]),
    CONSTRAINT [FK_ReviewBlogReplies_ReplyTo] FOREIGN KEY ([ReplyToAccountID]) REFERENCES [Accounts]([AccountID])
);
GO

CREATE TABLE [ReactionTypes] (
    [ReactionTypeID] INT IDENTITY(1,1) PRIMARY KEY,
    [Code] NVARCHAR(20) NOT NULL UNIQUE,   -- like, love, haha, angry...
    [DisplayName] NVARCHAR(50) NOT NULL,   -- Like, Love, Haha...
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE()
);
GO

-- 3. Bảng thả tim/reaction bình luận Blog (ReviewBlogReactions)
CREATE TABLE [ReviewBlogReactions] (
    [ReactionBlogID] INT IDENTITY(1,1) PRIMARY KEY,
    [ReviewBlogID] INT NOT NULL,
    [AccountID] INT NOT NULL,
    [ReactionTypeID] INT NOT NULL,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_ReviewBlogReactions_ReviewBlogs] FOREIGN KEY ([ReviewBlogID]) REFERENCES [ReviewBlogs]([ReviewBlogID]),
    CONSTRAINT [FK_ReviewBlogReactions_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_ReviewBlogReactions_ReactionTypes] FOREIGN KEY ([ReactionTypeID]) REFERENCES [ReactionTypes]([ReactionTypeID]),
    CONSTRAINT [UQ_ReviewBlogReactions_AccountReview] UNIQUE ([AccountID], [ReviewBlogID]) -- Mỗi user chỉ được thả 1 reaction cho 1 comment
);
GO

/* =============================================
   8. PAYMENT & WALLET (Double-Entry Bookkeeping)
============================================= */
CREATE TABLE [Wallets] (
    [WalletID] INT IDENTITY(1,1) PRIMARY KEY,
    [AccountID] INT NOT NULL UNIQUE,
    [Currency] CHAR(3) NOT NULL DEFAULT 'VND',
    [Balance] DECIMAL(12,0) NOT NULL DEFAULT 0 CHECK ([Balance] >= 0),
    [Status] VARCHAR(10) NOT NULL DEFAULT 'Active' CHECK ([Status] IN ('Active', 'Frozen', 'Closed')),
    [LastTransactionAt] DATETIME2(0) NULL,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(0) NULL,
    CONSTRAINT [FK_Wallets_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID])
);
GO

CREATE TABLE [WalletTransactions] (
    [WalletTransactionID] INT IDENTITY(1,1) PRIMARY KEY,
    [WalletID] INT NOT NULL,
    [AccountID] INT NOT NULL,
    [RelatedOrderID] INT NULL,
    -- NOTE: RelatedPaymentHistoryID bị loại bỏ để tránh circular FK (PaymentHistory → WalletTransactions ↔ PaymentHistory)
    -- Tra cứu lịch sử thanh toán theo order thông qua RelatedOrderID là đủ.
    [TxnType] VARCHAR(20) NOT NULL CHECK ([TxnType] IN ('TopUp', 'Payment', 'Refund')),
    [Direction] CHAR(2) NOT NULL CHECK ([Direction] IN ('CR', 'DR')), -- Credit/Debit
    [Amount] DECIMAL(12,0) NOT NULL CHECK ([Amount] > 0),
    [BalanceBefore] DECIMAL(12,0) NOT NULL,
    [BalanceAfter] DECIMAL(12,0) NOT NULL,
    [Method] VARCHAR(10) NOT NULL CHECK ([Method] IN ('Bank', 'EWallet', 'Cash', 'Wallet')),
    [ExternalRef] VARCHAR(100) NULL,
    [IdempotencyKey] VARCHAR(100) NULL,
    [Status] VARCHAR(15) NOT NULL DEFAULT 'Pending' CHECK ([Status] IN ('Pending', 'Completed', 'Failed', 'Cancelled')),
    [Reason] NVARCHAR(255) NULL,
    [Metadata] NVARCHAR(1000) NULL,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [CompletedAt] DATETIME2(0) NULL,
    CONSTRAINT [UQ_WalletTransactions_IdempotencyKey] UNIQUE ([IdempotencyKey]),
    CONSTRAINT [FK_WalletTransactions_Wallets] FOREIGN KEY ([WalletID]) REFERENCES [Wallets]([WalletID]),
    CONSTRAINT [FK_WalletTransactions_Orders] FOREIGN KEY ([RelatedOrderID]) REFERENCES [Orders]([OrderID]),
    CONSTRAINT [FK_WalletTransactions_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [CK_WalletTransactions_BalanceAfter] CHECK (
        ([Direction] = 'CR' AND [BalanceAfter] = [BalanceBefore] + [Amount]) OR
        ([Direction] = 'DR' AND [BalanceAfter] = [BalanceBefore] - [Amount])
    ),
    CONSTRAINT [CK_WalletTransactions_NoNegativeBalance] CHECK ([BalanceAfter] >= 0)
);
GO

CREATE TABLE [PaymentHistory] (
    [PaymentHistoryID] INT IDENTITY(1,1) PRIMARY KEY,
    [AccountID] INT NOT NULL,
    [OrderID] INT NOT NULL,
    [WalletTransactionID] INT NULL,
    [PaymentStatus] VARCHAR(20) NOT NULL CHECK ([PaymentStatus] IN ('PENDING', 'PAID', 'FAILED', 'EXPIRED', 'REFUNDED')),
    [PaymentMethod] VARCHAR(20) NOT NULL CHECK ([PaymentMethod] IN ('BANK_TRANSFER', 'MOMO', 'SHIP_CODE', 'ZALOPAY', 'VNPAY', 'WALLET')),
    [TransactionCode] VARCHAR(100) NULL,
    [Amount] DECIMAL(12,0) NOT NULL CHECK ([Amount] >= 0),
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_PaymentHistory_Orders] FOREIGN KEY ([OrderID]) REFERENCES [Orders]([OrderID]),
    CONSTRAINT [FK_PaymentHistory_WalletTransactions] FOREIGN KEY ([WalletTransactionID]) REFERENCES [WalletTransactions]([WalletTransactionID]),
    CONSTRAINT [FK_PaymentHistory_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID])
);
GO

/* =============================================
   8.1. PAYMENT GATEWAY TRANSACTIONS (MoMo / VNPay IPN)
   Lưu toàn bộ request/callback từ payment gateway.
   Pattern tương tự ShippingProviderTransactions — tách biệt raw gateway data
   với PaymentHistory (kết quả cuối cùng).
============================================= */
CREATE TABLE [dbo].[PaymentGatewayTransactions] (
    [PaymentGatewayTxnID] BIGINT        IDENTITY(1,1) NOT NULL,
    [OrderID]             INT           NOT NULL,                          -- FK → Orders
    [PaymentHistoryID]    INT           NULL,                              -- FK → PaymentHistory (sau khi xác nhận)
    [Provider]            VARCHAR(20)   NOT NULL,                          -- 'MOMO', 'VNPAY', 'ZALOPAY'
    [RequestID]           VARCHAR(100)  NULL UNIQUE,                       -- ID request gửi lên gateway
    [TransactionNo]       VARCHAR(100)  NULL,                              -- Mã giao dịch từ gateway callback
    [Amount]              DECIMAL(12,0) NOT NULL CHECK ([Amount] >= 0),
    [ResponseCode]        VARCHAR(10)   NULL,                              -- '0'=thành công, các mã khác=lỗi
    [ResponseMessage]     NVARCHAR(500) NULL,
    [Status]              VARCHAR(20)   NOT NULL DEFAULT 'PENDING'
        CONSTRAINT [CK_PayGwTxn_Status] CHECK ([Status] IN ('PENDING', 'PAID', 'FAILED', 'EXPIRED', 'CANCELLED')),
    [RawCallback]         NVARCHAR(MAX) NULL,                              -- JSON toàn bộ IPN/webhook payload
    [RetryCount]          INT           NOT NULL DEFAULT 0,
    [CreatedAt]           DATETIME2(0)  NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]           DATETIME2(0)  NULL,
    CONSTRAINT [PK_PaymentGatewayTransactions] PRIMARY KEY ([PaymentGatewayTxnID]),
    CONSTRAINT [FK_PayGwTxn_Orders]         FOREIGN KEY ([OrderID])         REFERENCES [Orders]([OrderID]),
    CONSTRAINT [FK_PayGwTxn_PaymentHistory] FOREIGN KEY ([PaymentHistoryID]) REFERENCES [PaymentHistory]([PaymentHistoryID])
);
GO
CREATE INDEX [IX_PayGwTxn_OrderID]   ON [dbo].[PaymentGatewayTransactions] ([OrderID]);
CREATE INDEX [IX_PayGwTxn_Provider_Status] ON [dbo].[PaymentGatewayTransactions] ([Provider], [Status]);
GO


CREATE TABLE [OrderRefundReasons] (
    [RefundReasonID] TINYINT IDENTITY(1,1) PRIMARY KEY, -- -1 to 255
    [Content] NVARCHAR(150) NOT NULL, -- max 200 Unicode
    [Description] NVARCHAR(255) NULL, -- max 500 Unicode
    [IsDeleted] BIT NOT NULL DEFAULT 0, -- 0-1
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE() -- 1 sec precision
);
GO

CREATE TABLE [OrderRefunds] (
    [RefundID] INT IDENTITY(1,1) PRIMARY KEY,
    [OrderID] INT NOT NULL,
    [RefundReasonID] TINYINT NULL,
    [CustomerID] INT NOT NULL,
    [RequestedBy] INT NULL,
    [ApprovedBy] INT NULL,
    [WalletTransactionID] INT NULL,
    [ComplaintImageURL] VARCHAR(500) NULL,       -- Nullable: cho phép tạo refund draft trước, upload ảnh sau (VARCHAR(500) đủ cho S3/CDN URL)
    [ReasonDetails] NVARCHAR(500) NULL,
    [ApprovedAmount] DECIMAL(12,0) NOT NULL CHECK ([ApprovedAmount] >= 0),
    [RefundStatus] VARCHAR(20) NOT NULL DEFAULT 'Requested'
        CONSTRAINT [CK_OrderRefunds_RefundStatus] CHECK ([RefundStatus] IN ('Requested', 'Approved', 'Rejected', 'Completed', 'Cancelled')),
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_OrderRefunds_Orders] FOREIGN KEY ([OrderID]) REFERENCES [Orders]([OrderID]),
    CONSTRAINT [FK_OrderRefunds_WalletTransactions] FOREIGN KEY ([WalletTransactionID]) REFERENCES [WalletTransactions]([WalletTransactionID]),
    CONSTRAINT [FK_OrderRefunds_Customer] FOREIGN KEY ([CustomerID]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_OrderRefunds_RequestedBy] FOREIGN KEY ([RequestedBy]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_OrderRefunds_ApprovedBy] FOREIGN KEY ([ApprovedBy]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_OrderRefunds_RefundReasons] FOREIGN KEY ([RefundReasonID]) REFERENCES [OrderRefundReasons]([RefundReasonID])
);
GO

CREATE TABLE [dbo].[ShippingProviderTransactions] (
    [ShippingTransactionID] BIGINT IDENTITY(1,1) NOT NULL,
    [OrderID]              INT            NOT NULL,
    [Provider]             VARCHAR(50)    NOT NULL,           -- 'GHN', 'GHTK', etc.
    [ProviderOrderCode]    VARCHAR(100)   NULL,
    [TrackingNumber]       VARCHAR(100)   NULL,
    [ServiceType]          NVARCHAR(100)  NULL,
    [Status]               VARCHAR(50)    NULL,
    [ShippingFee]          DECIMAL(12,0)  NULL,               -- Đổi từ DECIMAL(12,2) vì VND không có số lẻ
    [CodAmount]            DECIMAL(12,0)  NULL,               -- Số tiền GHN thu hộ (COD). NULL = không phải đơn COD
    [RowVersion]           ROWVERSION     NOT NULL,
    [RetryCount]           INT            NOT NULL DEFAULT 0,
    [LastErrorMessage]     NVARCHAR(500)  NULL,
    [Metadata]             NVARCHAR(MAX)  NULL,
    [EstimatedDelivery]    DATETIME2      NULL,
    [ActualDelivery]       DATETIME2      NULL,
    [LastPolledAt]         DATETIME2      NULL,
    [CreatedAt]            DATETIME2      NOT NULL CONSTRAINT DF_ShippingTxn_CreatedAt DEFAULT (GETDATE()),
    [UpdatedAt]            DATETIME2      NULL,
    CONSTRAINT [PK__Shipping__F215F69363919D74] PRIMARY KEY ([ShippingTransactionID]),
    CONSTRAINT [FK_ShippingTxn_Orders] FOREIGN KEY ([OrderID]) REFERENCES [dbo].[Orders]([OrderID]),
    CONSTRAINT [CK_ShippingTxn_CodAmount] CHECK ([CodAmount] IS NULL OR [CodAmount] >= 0)
);
GO

CREATE INDEX [IX_ShippingTxn_OrderID]          ON [dbo].[ShippingProviderTransactions] ([OrderID]);
CREATE INDEX [IX_ShippingTxn_Provider_Status]  ON [dbo].[ShippingProviderTransactions] ([Provider], [Status]);
GO

CREATE TABLE [dbo].[ShippingStatusHistories] (
    [HistoryId]      BIGINT IDENTITY PRIMARY KEY,
    [ShippingTxId]   BIGINT NOT NULL REFERENCES [dbo].[ShippingProviderTransactions]([ShippingTransactionID]),
    [OrderId]        INT NOT NULL REFERENCES [dbo].[Orders]([OrderID]),
    [PreviousStatus] NVARCHAR(50) NOT NULL,
    [NewStatus]      NVARCHAR(50) NOT NULL,
    [Source]         NVARCHAR(20) NOT NULL,
    [RawPayload]     NVARCHAR(MAX) NULL,
    [ProcessedAt]    DATETIME2 NOT NULL DEFAULT GETDATE()  -- Đồng nhất timezone với toàn hệ thống (GETDATE = local +07:00)
);
GO

CREATE INDEX IX_ShippingStatusHistories_ShippingTxId ON ShippingStatusHistories(ShippingTxId);
GO

CREATE INDEX IX_ShippingProviderTransactions_Polling
ON ShippingProviderTransactions (Provider, Status, OrderId)
INCLUDE (ProviderOrderCode, UpdatedAt)
WHERE Status <> 'delivered'
  AND Status <> 'returned'
  AND Status <> 'exception'
  AND Status <> 'cancel';
GO
/* =============================================
   9. NOTIFICATION & CHAT & INTERACTIONS
============================================= */
-- [Notification] Tables
CREATE TABLE [Notification].[Templates](
	[TemplateID] [smallint] IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[TemplateCode] [varchar](50) NOT NULL UNIQUE,
	[TitleTemplate] [nvarchar](255) NOT NULL,
	[MessageTemplate] [nvarchar](500) NOT NULL,
	[IsActive] [bit] NOT NULL DEFAULT 1,
	[CreatedAt] [datetime2](0) NOT NULL DEFAULT GETDATE(),
	[UpdatedAt] [datetime2](0) NULL
);
GO
CREATE TABLE [Notification].[Campaigns](
	[CampaignID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[CampaignName] [nvarchar](255) NOT NULL,
	[TemplateCode] [varchar](50) NULL,
	[TitleOverride] [nvarchar](255) NULL,
	[MessageOverride] [nvarchar](500) NULL,
	[SourceType] [varchar](10) NOT NULL DEFAULT 'ADMIN'
	    CONSTRAINT [CK_Campaigns_SourceType] CHECK ([SourceType] IN ('ADMIN', 'SYSTEM')),
	[TargetType] [varchar](10) NOT NULL DEFAULT 'ALL'
	    CONSTRAINT [CK_Campaigns_TargetType] CHECK ([TargetType] IN ('ALL', 'SEGMENT', 'INDIVIDUAL', 'ROLE')),
	[Status] [varchar](15) NOT NULL DEFAULT 'Draft'
	    CONSTRAINT [CK_Campaigns_Status] CHECK ([Status] IN ('Draft', 'Scheduled', 'Sending', 'Sent', 'Cancelled')),
	[ScheduledAt] [datetime2](0) NULL,
	[EventKey] [varchar](100) NULL,
	[ImageUrl] [nvarchar](500) NULL,
	[ActionType] [nvarchar](20) NULL,
	[ActionTarget] [nvarchar](500) NULL,
	[CreatedByAccountID] [int] NOT NULL,
	[CreatedAt] [datetime2](0) NOT NULL DEFAULT GETDATE(),
	[UpdatedAt] [datetime2](0) NULL,
	CONSTRAINT [FK_Campaigns_Templates] FOREIGN KEY ([TemplateCode]) REFERENCES [Notification].[Templates]([TemplateCode]),
	CONSTRAINT [FK_Campaigns_Accounts] FOREIGN KEY ([CreatedByAccountID]) REFERENCES [Accounts]([AccountID])
);
GO
CREATE TABLE [Notification].[CampaignTargets](
	[CampaignTargetID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[CampaignID] [int] NOT NULL,
	[TargetType] [varchar](20) NOT NULL DEFAULT 'ACCOUNT_ID'
	    CONSTRAINT [CK_CampaignTargets_TargetType] CHECK ([TargetType] IN ('ACCOUNT_ID', 'ROLE_ID', 'SEGMENT')),
	[TargetValue] [varchar](200) NOT NULL,
	CONSTRAINT [FK_CampaignTargets_Campaigns] FOREIGN KEY ([CampaignID]) REFERENCES [Notification].[Campaigns]([CampaignID])
);
GO
CREATE TABLE [Notification].[Deliveries](
	[DeliveryID] [bigint] IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[AccountID] [int] NOT NULL,
	[CreatedByJobID] [int] NULL,
	[TemplateCode] [varchar](50) NOT NULL,
	[RecipientType] [varchar](15) NOT NULL DEFAULT 'CUSTOMER'
	    CONSTRAINT [CK_Deliveries_RecipientType] CHECK ([RecipientType] IN ('CUSTOMER', 'ADMIN', 'STAFF', 'MERCHANDISE')),
	[Title] [nvarchar](255) NOT NULL,
	[Message] [nvarchar](500) NOT NULL,
	[Payload] [nvarchar](1000) NOT NULL,
	[Status] [varchar](10) NOT NULL DEFAULT 'Unread'
	    CONSTRAINT [CK_Deliveries_Status] CHECK ([Status] IN ('Unread', 'Read', 'Archived', 'Deleted')),
	[ReadAt] [datetime2](0) NULL,                              -- NULL = chưa đọc
	[CreatedAt] [datetime2](0) NOT NULL DEFAULT GETDATE(),
	[ImageUrl] [nvarchar](500) NULL,
	[ActionType] [nvarchar](20) NULL,
	[ActionTarget] [nvarchar](500) NULL,
	[CampaignID] [int] NULL,
	CONSTRAINT [FK_Deliveries_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID]),
	CONSTRAINT [FK_Deliveries_Templates] FOREIGN KEY ([TemplateCode]) REFERENCES [Notification].[Templates]([TemplateCode]),
	CONSTRAINT [FK_Deliveries_Campaigns] FOREIGN KEY ([CampaignID]) REFERENCES [Notification].[Campaigns]([CampaignID]),
	CONSTRAINT [FK_Deliveries_Jobs] FOREIGN KEY ([CreatedByJobID]) REFERENCES [System].[BackgroundJobs]([JobID])
);
GO
CREATE TABLE [Notification].[DeliveryActions](
	[ActionID] [bigint] IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[DeliveryID] [bigint] NOT NULL,
	[AccountID] [int] NOT NULL,
	[ActionType] [varchar](10) NOT NULL
	    CONSTRAINT [CK_DeliveryActions_ActionType] CHECK ([ActionType] IN ('Read', 'Click', 'Dismiss')),
	[ActionTarget] [nvarchar](500) NULL,
	[OccurredAt] [datetime2](0) NOT NULL DEFAULT GETDATE(),
	CONSTRAINT [FK_DeliveryActions_Deliveries] FOREIGN KEY ([DeliveryID]) REFERENCES [Notification].[Deliveries]([DeliveryID]),
	CONSTRAINT [FK_DeliveryActions_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID])
);
GO 

CREATE TABLE [ChatConversations] (
    [ConversationID]  INT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [AccountID]       INT          NOT NULL,
    [SessionID]       VARCHAR(100) NULL,
    [Status]          VARCHAR(15)  NOT NULL DEFAULT 'BotActive',
    [CreatedAt]       DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_ChatConversations_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID])
);
GO
CREATE TABLE [ChatMessages] (
    [MessageID] INT IDENTITY(1,1) PRIMARY KEY,
    [ConversationID] INT NOT NULL,
    [SenderType] VARCHAR(10) NOT NULL,
    [Content] NVARCHAR(1000) NOT NULL,
	[Payload] NVARCHAR(2000) NULL,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_ChatMessages_ChatConversations] FOREIGN KEY ([ConversationID]) REFERENCES [ChatConversations]([ConversationID])
);
GO

CREATE TABLE [Interaction].[Events] (
    [EventID]    BIGINT IDENTITY(1,1) PRIMARY KEY,
    [AccountID]  INT            NULL,
    [SessionID]  VARCHAR(100)   NOT NULL,
    [EventType]  VARCHAR(50)    NOT NULL,
    [EntityID]   VARCHAR(50)    NOT NULL,
    [EntityType] VARCHAR(30)    NOT NULL,
	
	-- Context
    [Source]        VARCHAR(30)  NULL,  -- 'homepage', 'search', 'product_detail', 'cart'
    [Referrer]      VARCHAR(200) NULL,  -- URL nguồn dẫn vào
    [DeviceType]    VARCHAR(15)  NULL,  -- 'mobile', 'desktop', 'tablet'
	
	-- Engagement metric
    [DurationMs]    INT          NULL,  -- Thời gian tương tác (milliseconds)
    [ScrollDepth]   TINYINT      NULL,  -- 0–100 (% trang đã scroll)
    [ClickPosition] VARCHAR(30)  NULL,  -- 'top', 'middle', 'bottom', 'sidebar'
	
	-- Extra data
    [Metadata]      NVARCHAR(500) NULL, -- JSON: {"search_query": "...", "filter": {...}}
	
    [CreatedAt]  DATETIME2(0)   NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_Events_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID])
);
GO
CREATE TABLE [Recommendation].[UserProductScores] (
    [ScoreID]          BIGINT IDENTITY(1,1) PRIMARY KEY,
    [AccountID]        INT           NOT NULL,
    [ProductID]        INT           NOT NULL,
    [Score]            DECIMAL(8,4)  NOT NULL DEFAULT 0,  -- Weighted score tổng hợp
    [ViewCount]        SMALLINT      NOT NULL DEFAULT 0,
    [CartCount]        TINYINT       NOT NULL DEFAULT 0,
    [PurchaseCount]    TINYINT       NOT NULL DEFAULT 0,
    [WishlistCount]    TINYINT       NOT NULL DEFAULT 0,
    [LastInteractedAt] DATETIME2(0)  NOT NULL,
    [ComputedAt]       DATETIME2(0)  NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [UQ_UserProductScores] UNIQUE ([AccountID], [ProductID]),
    CONSTRAINT [FK_UPS_Accounts]  FOREIGN KEY ([AccountID])  REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_UPS_Products]  FOREIGN KEY ([ProductID])  REFERENCES [Products]([ProductID])
);
GO
-- Bảng cho Recommendation
CREATE TABLE [Recommendation].[ItemSimilarities] (
    [SimilarityID] INT IDENTITY(1,1) PRIMARY KEY,
    [SourceProductID] INT NOT NULL,
    [SimilarProductID] INT NOT NULL,
    [SimilarityScore] DECIMAL(5,4) NOT NULL,
	
	[AlgorithmType] VARCHAR(20) NOT NULL DEFAULT 'cf', -- 'cf'=Collaborative Filtering, 'cb'=Content-Based, 'hybrid'
    [UpdatedAt]     DATETIME2(0) NULL,
	
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_ItemSimilarities_SourceProduct] FOREIGN KEY ([SourceProductID]) REFERENCES [Products]([ProductID]),
    CONSTRAINT [FK_ItemSimilarities_SimilarProduct] FOREIGN KEY ([SimilarProductID]) REFERENCES [Products]([ProductID])
);
GO

CREATE TABLE [Recommendation].[Widgets] (
    [WidgetID]       TINYINT IDENTITY(1,1) PRIMARY KEY,
    [WidgetCode]     VARCHAR(50)   NOT NULL UNIQUE, -- 'homepage_trending', 'pdp_similar', 'cart_upsell', 'after_purchase'
    [WidgetName]     NVARCHAR(100) NOT NULL,
    [Algorithm]      VARCHAR(20)   NOT NULL, -- 'trending', 'collaborative', 'content_based', 'hybrid', 'popular'
    [MaxItems]       TINYINT       NOT NULL DEFAULT 10,
    [FallbackAlgo]   VARCHAR(20)   NULL,    -- Algo dự phòng khi thiếu data
    [IsActive]       BIT           NOT NULL DEFAULT 1,
    [Config]         NVARCHAR(500) NULL,    -- JSON config bổ sung
    [CreatedAt]      DATETIME2(0)  NOT NULL DEFAULT GETDATE()
);
GO
CREATE TABLE [Recommendation].[TrendingProducts] (
    [TrendingID]   INT IDENTITY(1,1) PRIMARY KEY,
    [ProductID]    INT           NOT NULL,
    [Scope]        VARCHAR(20)   NOT NULL DEFAULT 'global', -- 'global', 'category:{id}', 'brand:{id}'
    [Score]        DECIMAL(10,4) NOT NULL,
    [ViewCount]    INT           NOT NULL DEFAULT 0,
    [PurchaseCount] INT          NOT NULL DEFAULT 0,
    [Rank]         SMALLINT      NOT NULL,
    [WindowHours]  TINYINT       NOT NULL DEFAULT 24, -- 1h, 6h, 24h, 168h
    [ComputedAt]   DATETIME2(0)  NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [UQ_Trending_Product_Scope_Window] UNIQUE ([ProductID], [Scope], [WindowHours]),
    CONSTRAINT [FK_Trending_Products] FOREIGN KEY ([ProductID]) REFERENCES [Products]([ProductID])
);
GO

CREATE NONCLUSTERED INDEX [IX_Trending_Scope_Rank]
ON [Recommendation].[TrendingProducts]([Scope], [WindowHours], [Rank])
INCLUDE ([ProductID], [Score]);


/* =============================================
   10. PERFORMANCE INDEXES (OPTIMIZATION)
============================================= */
-- Fix 5: Index cho BackgroundJob tìm nhanh account hết hạn khóa
CREATE NONCLUSTERED INDEX [IX_UserBlockHistory_PendingUnblock]
ON [UserBlockHistory] ([BlockedUntil], [AccountID])
WHERE [UnblockedAt] IS NULL;
GO

CREATE NONCLUSTERED INDEX [IX_UPS_User]
ON [Recommendation].[UserProductScores]([AccountID], [Score] DESC);
GO

-- Index 
CREATE NONCLUSTERED INDEX [IX_ItemSimilarities_Score]
ON [Recommendation].[ItemSimilarities]([SourceProductID], [SimilarityScore] DESC)
INCLUDE ([SimilarProductID], [AlgorithmType]);
GO

-- Worker/Automation
CREATE NONCLUSTERED INDEX [IX_Vouchers_Worker] ON [Vouchers]([Status], [StartDate], [EndDate]);
GO

CREATE NONCLUSTERED INDEX [IX_Promotions_Worker] ON [Promotions]([Status], [StartDate], [EndDate]);
GO

-- Filtered Index: Sản phẩm sắp hết hàng (Low Stock)
CREATE NONCLUSTERED INDEX [IX_Products_LowStock_V2]
ON [Products]([ProductStatus], [IsDeleted])
INCLUDE ([Quantity], [StockThreshold])
WHERE ([Quantity] <= 10 AND [IsDeleted] = 0 AND [ProductStatus] = 'Active');
GO

-- User-Facing Queries (Customer)
CREATE NONCLUSTERED INDEX [IX_NotificationDeliveries_User]
ON [Notification].[Deliveries]([AccountID], [RecipientType], [Status])
INCLUDE ([CreatedAt], [Title])
WHERE ([Status] = 'Unread' AND [RecipientType] = 'CUSTOMER');
GO

-- Admin/Staff/Merchandise-Facing Queries
CREATE NONCLUSTERED INDEX [IX_NotificationDeliveries_Admin]
ON [Notification].[Deliveries]([RecipientType], [Status], [CreatedAt] DESC)
INCLUDE ([AccountID], [Title])
WHERE ([RecipientType] IN ('ADMIN', 'STAFF', 'MERCHANDISE'));
GO

CREATE NONCLUSTERED INDEX [IX_Orders_UserHistory] ON [Orders]([AccountID], [OrderDate] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_Wishlists_User] ON [Wishlists]([AccountID]);
GO

CREATE NONCLUSTERED INDEX [IX_ChatMessages_Conversation] ON [ChatMessages]([ConversationID], [CreatedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_Products_FilterSort] ON [Products]([CategoryID], [BrandID], [Price], [IsDeleted]);
GO

-- Analytics
CREATE NONCLUSTERED INDEX [IX_InteractionEvents_UserBehavior]
ON [Interaction].[Events]([AccountID], [EventType], [CreatedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_ItemSimilarities_Source]
ON [Recommendation].[ItemSimilarities]([SourceProductID])
INCLUDE ([SimilarProductID], [SimilarityScore]);
GO

-- Report & Statistics
CREATE NONCLUSTERED INDEX [IX_Orders_ReportByDate]
ON [Orders]([OrderDate], [PaymentStatus])
INCLUDE ([TotalAmount], [VoucherDiscountAmount]);
GO

CREATE NONCLUSTERED INDEX [IX_OrderDetails_ProductSales]
ON [OrderDetails]([ProductID])
INCLUDE ([Quantity], [LineTotal]);
GO

-- Fix 6: Product listing/search indexes
CREATE NONCLUSTERED INDEX [IX_Products_Category_Status]
ON [Products]([CategoryID], [ProductStatus])
INCLUDE ([ProductName], [Price], [Quantity], [PromotionID])
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

CREATE NONCLUSTERED INDEX [IX_VoucherUsageLogs_Analytics]
ON [VoucherUsageLogs]([VoucherID], [UsedAt]);
GO

-- Wallet Transactions queries
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

-- Query Payment History by Order
CREATE NONCLUSTERED INDEX [IX_PaymentHistory_Order]
ON [PaymentHistory]([OrderID])
INCLUDE ([PaymentStatus], [Amount], [CreatedAt]);
GO

-- Query Refunds by Order
CREATE NONCLUSTERED INDEX [IX_OrderRefunds_Order]
ON [OrderRefunds]([OrderID])
INCLUDE ([RefundStatus], [ApprovedAmount]);
GO

-- Query Reviews by Product
CREATE NONCLUSTERED INDEX [IX_ReviewProducts_Product]
ON [ReviewProducts]([ProductID], [IsDeleted])
INCLUDE ([Rating], [CreatedAt]);
GO

-- UNIQUE index for EmployeeCode (when NOT NULL)
CREATE UNIQUE NONCLUSTERED INDEX [IX_Accounts_EmployeeCode]
ON [Accounts]([EmployeeCode])
WHERE [EmployeeCode] IS NOT NULL;
GO

-- Index for DomainEventOutbox worker
CREATE NONCLUSTERED INDEX [IX_DomainEventOutbox_Pending]
ON [System].[DomainEventOutbox]([ProcessedOn])
WHERE [ProcessedOn] IS NULL;
GO

-- Critical missing indexes for performance
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
   11. UNIQUE INDEXES FOR BUSINESS RULES
============================================= */

-- Chỉ 1 địa chỉ mặc định per user
CREATE UNIQUE NONCLUSTERED INDEX [IX_Addresses_OneDefaultPerUser]
ON [Addresses]([AccountID])
WHERE [IsDefault] = 1 AND [IsDeleted] = 0;
GO

-- Chỉ 1 ảnh chính per product
CREATE UNIQUE NONCLUSTERED INDEX [IX_ProductImages_OneMainPerProduct]
ON [ProductImages]([ProductID])
WHERE [IsMain] = 1;
GO

-- Lưu ý: Index này giống hệt IX_DomainEventOutbox_Pending ở trên nhưng khác tên. 
-- Nếu hệ thống báo lỗi trùng lặp (already exists), bạn có thể xóa bớt 1 trong 2 nhé.
CREATE NONCLUSTERED INDEX IX_DomainEventOutbox_Worker
ON [System].[DomainEventOutbox]([OccurredOn])
WHERE [ProcessedOn] IS NULL;
GO

-- Indexes cho Blog
CREATE NONCLUSTERED INDEX [IX_BlogPosts_Status_Date] 
ON [BlogPosts]([Status], [IsDeleted], [BlogAt] DESC)
INCLUDE ([BlogTitle], [BlogThumbnail]);
GO

CREATE NONCLUSTERED INDEX [IX_BlogPosts_Category]
ON [BlogPostCategories]([BlogCategoryID]);
GO

-- Indexes cho Tương tác Blog (Load comment nhanh)
CREATE NONCLUSTERED INDEX [IX_ReviewBlogs_BlogPost]
ON [ReviewBlogs]([BlogPostID], [IsDeleted])
INCLUDE ([Comment], [CreatedAt]);
GO

CREATE NONCLUSTERED INDEX [IX_ReviewBlogReplies_Review]
ON [ReviewBlogReplies]([ReviewBlogID], [IsDeleted]);
GO

CREATE NONCLUSTERED INDEX [IX_ReviewBlogReactions_Stats]
ON [ReviewBlogReactions]([ReviewBlogID], [ReactionTypeID]);
GO

CREATE NONCLUSTERED INDEX [IX_AuditLogs_Entity]
ON [AuditLogs]([EntityType], [EntityID], [CreatedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_AuditLogs_Actor]
ON [AuditLogs]([PerformedBy], [CreatedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_AuditLogs_Date]
ON [AuditLogs]([CreatedAt] DESC)
INCLUDE ([EntityType], [Action], [PerformedBy]);
GO

CREATE INDEX [IX_PayGwTxn_PaymentHistoryID] 
ON [PaymentGatewayTransactions]([PaymentHistoryID]) 
WHERE [PaymentHistoryID] IS NOT NULL;