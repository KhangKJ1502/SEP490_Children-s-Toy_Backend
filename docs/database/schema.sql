/* =================================================================
   E-COMMERCE DATABASE SCHEMA (OPTIMIZED FULL VERSION)
   Platform: SQL Server | Version: 2.0
   Database: SEP490_ToyStore
================================================================= */
USE [master];
GO
-- Tạo database nếu chưa tồn tại
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'SEP409_ToyStore')
BEGIN
    CREATE DATABASE [SEP409_ToyStore];
END
GO
USE [SEP409_ToyStore];
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
    [EventID] UNIQUEIDENTIFIER PRIMARY KEY,
    [AggregateType] VARCHAR(100) NOT NULL,
    [AggregateId] VARCHAR(100) NOT NULL,
    [EventType] VARCHAR(100) NOT NULL,
    [Payload] NVARCHAR(MAX) NOT NULL,
    [OccurredOn] DATETIME2(0) NOT NULL,
    [ProcessingLockId] UNIQUEIDENTIFIER NULL,
    [ProcessingAt] DATETIME2(0) NULL,
    [Attempts] TINYINT NOT NULL DEFAULT 0,
    [LastError] NVARCHAR(MAX) NULL,
    [ProcessedOn] DATETIME2(0) NULL
);




CREATE TABLE [System].[BackgroundJobs] (
    [JobID] INT IDENTITY(1,1) PRIMARY KEY,
    [JobName] VARCHAR(100) NOT NULL UNIQUE,
    [CronExpression] VARCHAR(50) NULL,
    [IsEnabled] BIT NOT NULL DEFAULT 1,
    [LastRunTime] DATETIME2(0) NULL,
    [NextRunTime] DATETIME2(0) NULL,
    [LastRunStatus] VARCHAR(20) NULL,
    [LastRunMessage] NVARCHAR(MAX) NULL
);
GO




/* =============================================
   1. USER MANAGEMENT (IAM)
============================================= */
CREATE TABLE [Roles] (
    [RoleID] TINYINT IDENTITY(1,1) PRIMARY KEY,
    [RoleName] VARCHAR(50) NOT NULL UNIQUE,
    [Description] NVARCHAR(255) NULL,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE()
);




CREATE TABLE [Accounts] (
    [AccountID] INT IDENTITY(1,1) PRIMARY KEY,
    [RoleID] TINYINT NOT NULL,
    [EmployeeCode] VARCHAR(20) NULL,
    [AccountName] NVARCHAR(100) NOT NULL,
    [PhoneNumber] VARCHAR(15) NULL,
    [Email] VARCHAR(255) NOT NULL UNIQUE,
    [Image] VARCHAR(500) NULL,
    [PasswordHash] VARCHAR(255) NOT NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [Provider] VARCHAR(20) NULL,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(0) NULL,
    CONSTRAINT [FK_Accounts_Roles] FOREIGN KEY ([RoleID]) REFERENCES [Roles]([RoleID])
);




CREATE TABLE [BlockReasons] (
    [ReasonID] SMALLINT IDENTITY(1,1) PRIMARY KEY,
    [ReasonCode] VARCHAR(50) NULL,
    [Content] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE()
);




CREATE TABLE [UserBlockHistory] (
    [BlockID] INT IDENTITY(1,1) PRIMARY KEY,
    [AccountID] INT NOT NULL,
    [BlockedBy] INT NULL,
    [ReasonID] SMALLINT NOT NULL,
    [UnblockedBy] INT NULL,
    [UnblockedByJobID] INT NULL,
    [BlockedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [BlockedUntil] DATETIME2(0) NOT NULL,
    [UnblockedAt] DATETIME2(0) NULL,
    CONSTRAINT [FK_UserBlockHistory_Account] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_UserBlockHistory_BlockedBy] FOREIGN KEY ([BlockedBy]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_UserBlockHistory_UnblockedBy] FOREIGN KEY ([UnblockedBy]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_UserBlockHistory_BlockReasons] FOREIGN KEY ([ReasonID]) REFERENCES [BlockReasons]([ReasonID]),
    CONSTRAINT [FK_UserBlockHistory_BackgroundJobs] FOREIGN KEY ([UnblockedByJobID]) REFERENCES [System].[BackgroundJobs]([JobID])
);




CREATE TABLE [Addresses] (
    [AddressID] INT IDENTITY(1,1) PRIMARY KEY,
    [AccountID] INT NOT NULL,
    [RecipientName] NVARCHAR(100) NULL,
    [RecipientPhone] VARCHAR(15) NULL,
    [AddressLine] NVARCHAR(500) NOT NULL,
    [Ward] NVARCHAR(100) NULL,
    [City] NVARCHAR(100) NOT NULL,
    [IsDefault] BIT NOT NULL DEFAULT 0,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(0) NULL,
    CONSTRAINT [FK_Addresses_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID])
);
GO




/* =============================================
   2. PRODUCT CATALOG (PIM)
============================================= */
CREATE TABLE [SuperCategories] (
    [SuperCategoryID] SMALLINT IDENTITY(1,1) PRIMARY KEY,
    [SuperCategoryName] NVARCHAR(100) NOT NULL UNIQUE,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(0) NULL
);




CREATE TABLE [Categories] (
    [CategoryID] SMALLINT IDENTITY(1,1) PRIMARY KEY,
    [SuperCategoryID] SMALLINT NOT NULL,
    [CategoryName] NVARCHAR(100) NOT NULL UNIQUE,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(0) NULL,
    CONSTRAINT [FK_Categories_SuperCategories] FOREIGN KEY ([SuperCategoryID]) REFERENCES [SuperCategories]([SuperCategoryID])
);




CREATE TABLE [Materials] (
    [MaterialID] SMALLINT IDENTITY(1,1) PRIMARY KEY,
    [MaterialName] NVARCHAR(100) NOT NULL UNIQUE,
    [Description] NVARCHAR(500) NULL,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(0) NULL
);




CREATE TABLE [Ages] (
    [AgeID] TINYINT IDENTITY(1,1) PRIMARY KEY,
    [AgeRange] VARCHAR(50) NOT NULL UNIQUE,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE()
);




CREATE TABLE [Sexes] (
    [SexID] TINYINT IDENTITY(1,1) PRIMARY KEY,
    [SexName] NVARCHAR(20) NOT NULL UNIQUE,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE()
);




CREATE TABLE [Origins] (
    [OriginID] SMALLINT IDENTITY(1,1) PRIMARY KEY,
    [OriginName] NVARCHAR(100) NOT NULL UNIQUE,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(0) NULL
);




CREATE TABLE [Brands] (
    [BrandID] SMALLINT IDENTITY(1,1) PRIMARY KEY,
    [BrandName] NVARCHAR(100) NOT NULL UNIQUE,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(0) NULL
);




CREATE TABLE [PriceRanges] (
    [PriceRangeID] TINYINT IDENTITY(1,1) PRIMARY KEY,
    [PriceRangeMin] DECIMAL(12,0) NOT NULL CHECK ([PriceRangeMin] >= 0),
    [PriceRangeMax] DECIMAL(12,0) NOT NULL,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(0) NULL,
    CONSTRAINT [CK_PriceRanges_MinMax] CHECK ([PriceRangeMin] < [PriceRangeMax])
);




CREATE TABLE [Promotions] (
    [PromotionID] INT IDENTITY(1,1) PRIMARY KEY,
    [PromotionName] NVARCHAR(200) NOT NULL,
    [PromotionCode] VARCHAR(50) NOT NULL UNIQUE,
    [Description] NVARCHAR(MAX) NULL,
    [DiscountPercent] DECIMAL(5,2) NOT NULL CHECK ([DiscountPercent] BETWEEN 0 AND 100),
    [StartDate] DATETIME2(0) NOT NULL,
    [EndDate] DATETIME2(0) NOT NULL,
    [Status] VARCHAR(20) NOT NULL CHECK ([Status] IN ('Scheduled', 'Active', 'Expired', 'Inactive')),
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [CreatedBy] INT NOT NULL,
    CONSTRAINT [FK_Promotions_Accounts] FOREIGN KEY ([CreatedBy]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [CK_Promotions_DateRange] CHECK ([StartDate] < [EndDate])
);




CREATE TABLE [Products] (
    [ProductID] INT IDENTITY(1,1) PRIMARY KEY,
    [ProductName] NVARCHAR(255) NOT NULL,
    [Price] DECIMAL(12,0) NOT NULL CHECK ([Price] >= 0),
    [Quantity] INT NOT NULL CHECK ([Quantity] >= 0),
    [ProductStatus] VARCHAR(20) NOT NULL CHECK ([ProductStatus] IN ('Active', 'Inactive', 'OutOfStock', 'Discontinued')),
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [StockThreshold] SMALLINT NOT NULL DEFAULT 10,
    [LowStockNotificationEnabled] BIT NOT NULL DEFAULT 1,
    [LastLowStockNotifiedAt] DATETIME2(0) NULL,
    [CategoryID] SMALLINT NOT NULL,
    [BrandID] SMALLINT NULL,
    [PriceRangeID] TINYINT NULL,
    [PromotionID] INT NULL,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(0) NULL,
    CONSTRAINT [FK_Products_Categories] FOREIGN KEY ([CategoryID]) REFERENCES [Categories]([CategoryID]),
    CONSTRAINT [FK_Products_Brands] FOREIGN KEY ([BrandID]) REFERENCES [Brands]([BrandID]),
    CONSTRAINT [FK_Products_PriceRanges] FOREIGN KEY ([PriceRangeID]) REFERENCES [PriceRanges]([PriceRangeID]),
    CONSTRAINT [FK_Products_Promotions] FOREIGN KEY ([PromotionID]) REFERENCES [Promotions]([PromotionID])
);




CREATE TABLE [ProductDetails] (
    [ProductID] INT NOT NULL PRIMARY KEY,
    [Description] NVARCHAR(MAX) NULL,
    [MaterialID] SMALLINT NULL,
    [AgeID] TINYINT NULL,
    [SexID] TINYINT NULL,
    [OriginID] SMALLINT NULL,
    CONSTRAINT [FK_ProductDetails_Products] FOREIGN KEY ([ProductID]) REFERENCES [Products]([ProductID]),
    CONSTRAINT [FK_ProductDetails_Materials] FOREIGN KEY ([MaterialID]) REFERENCES [Materials]([MaterialID]),
    CONSTRAINT [FK_ProductDetails_Ages] FOREIGN KEY ([AgeID]) REFERENCES [Ages]([AgeID]),
    CONSTRAINT [FK_ProductDetails_Sexes] FOREIGN KEY ([SexID]) REFERENCES [Sexes]([SexID]),
    CONSTRAINT [FK_ProductDetails_Origins] FOREIGN KEY ([OriginID]) REFERENCES [Origins]([OriginID])
);




CREATE TABLE [ProductImages] (
    [ImageID] INT IDENTITY(1,1) PRIMARY KEY,
    [ProductID] INT NOT NULL,
    [ImageUrl] VARCHAR(500) NOT NULL,
    [IsMain] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(0) NULL,
    CONSTRAINT [FK_ProductImages_Products] FOREIGN KEY ([ProductID]) REFERENCES [Products]([ProductID])
);
GO




/* =============================================
   3. ORDER MANAGEMENT (OMS)
============================================= */
CREATE TABLE [StatusOrders] (
    [StatusID] TINYINT IDENTITY(1,1) PRIMARY KEY,
    [StatusName] VARCHAR(50) NOT NULL UNIQUE,
    [Description] NVARCHAR(255) NULL,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(0) NULL
);




CREATE TABLE [Orders] (
    [OrderID] INT IDENTITY(1,1) PRIMARY KEY,
    [AccountID] INT NOT NULL,
    [StatusID] TINYINT NOT NULL,
    [AssignedToStaffID] INT NULL,
    [OrderCode] VARCHAR(30) NOT NULL UNIQUE,
    [ShippingName] NVARCHAR(100) NOT NULL,
    [ShippingPhone] VARCHAR(15) NOT NULL,
    [ShippingAddress] NVARCHAR(500) NOT NULL,
    [ShippingWard] NVARCHAR(100) NULL,
    [ShippingDistrict] NVARCHAR(100) NOT NULL,
    [ShippingCity] NVARCHAR(100) NOT NULL,
    [OrderDate] DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    [ConfirmedAt] DATETIME2(0) NULL,
    [ShippedAt] DATETIME2(0) NULL,
    [DeliveredAt] DATETIME2(0) NULL,
    [CompletedAt] DATETIME2(0) NULL,
    [CancelledAt] DATETIME2(0) NULL,
    [PaymentMethod] VARCHAR(20) NOT NULL DEFAULT 'BANK_TRANSFER' CHECK ([PaymentMethod] IN ('BANK_TRANSFER', 'MOMO', 'SHIP_CODE', 'ZALOPAY', 'VNPAY', 'WALLET')),
    [PaymentStatus] VARCHAR(20) NOT NULL DEFAULT 'PENDING' CHECK ([PaymentStatus] IN ('PENDING', 'PAID', 'FAILED', 'EXPIRED')),
    [PaymentCode] VARCHAR(50) NOT NULL,
    [PaidAt] DATETIME2(0) NULL,
    [SubTotal] DECIMAL(12,0) NOT NULL,
    [VoucherDiscountAmount] DECIMAL(12,0) NOT NULL DEFAULT 0,
    [EstimatedShippingFee] DECIMAL(10,0) NOT NULL,
    [ActualShippingFee] DECIMAL(10,0) NULL,
    [TotalAmount] DECIMAL(12,0) NOT NULL,
    [CancelReason] NVARCHAR(500) NULL,
    [CancelledBy] INT NULL,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreateAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(0) NULL,
    CONSTRAINT FK_Orders_Accounts FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT FK_Orders_StatusOrders FOREIGN KEY ([StatusID]) REFERENCES [StatusOrders]([StatusID]),
    CONSTRAINT FK_Orders_AssignedStaff FOREIGN KEY ([AssignedToStaffID]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT FK_Orders_CancelledBy FOREIGN KEY ([CancelledBy]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT CK_Orders_Amounts CHECK (
        [SubTotal] >= 0 AND [EstimatedShippingFee] >= 0 AND ([ActualShippingFee] IS NULL OR [ActualShippingFee] >= 0) AND [TotalAmount] >= 0
    ),
    CONSTRAINT CK_Orders_Timestamps CHECK (
        ([ConfirmedAt] IS NULL OR [ConfirmedAt] >= [OrderDate]) AND
        ([ShippedAt] IS NULL OR [ShippedAt] >= [ConfirmedAt]) AND
        ([DeliveredAt] IS NULL OR [DeliveredAt] >= [ShippedAt]) AND
        ([CompletedAt] IS NULL OR [CompletedAt] >= [DeliveredAt])
    )
);




CREATE TABLE [OrderDetails] (
    [OrderDetailID] INT IDENTITY(1,1) PRIMARY KEY,
    [OrderID] INT NOT NULL,
    [ProductID] INT NOT NULL,
    [ProductName] NVARCHAR(255) NOT NULL,
    [ProductImage] VARCHAR(500) NULL,
    [Quantity] SMALLINT NOT NULL CHECK ([Quantity] > 0),
    [UnitPrice] DECIMAL(12,0) NOT NULL CHECK ([UnitPrice] >= 0),
    [DiscountAmount] DECIMAL(12,0) NOT NULL DEFAULT 0 CHECK ([DiscountAmount] >= 0),
    [LineTotal] AS ([Quantity] * [UnitPrice] - [DiscountAmount]) PERSISTED,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_OrderDetails_Orders] FOREIGN KEY ([OrderID]) REFERENCES [Orders]([OrderID]),
    CONSTRAINT [FK_OrderDetails_Products] FOREIGN KEY ([ProductID]) REFERENCES [Products]([ProductID]),
    CONSTRAINT [UQ_OrderDetails_OrderProduct] UNIQUE ([OrderID], [ProductID])
);




CREATE TABLE [OrderStatusHistory] (
    [HistoryID] INT IDENTITY(1,1) PRIMARY KEY,
    [OrderID] INT NOT NULL,
    [StatusID] TINYINT NOT NULL,
    [ChangedBy] INT NULL,
    [Note] NVARCHAR(500) NULL,
    [ChangedAt] DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    [CreateAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(0) NULL,
    CONSTRAINT FK_OrderStatusHistory_Orders FOREIGN KEY ([OrderID]) REFERENCES [Orders]([OrderID]),
    CONSTRAINT FK_OrderStatusHistory_Status FOREIGN KEY ([StatusID]) REFERENCES [StatusOrders]([StatusID]),
    CONSTRAINT FK_OrderStatusHistory_ChangedBy FOREIGN KEY ([ChangedBy]) REFERENCES [Accounts]([AccountID])
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
CREATE TABLE [VoucherTypes] (
    [VoucherTypeID] TINYINT IDENTITY(1,1) PRIMARY KEY,
    [VoucherTypeName] NVARCHAR(100) NOT NULL UNIQUE,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE()
);




CREATE TABLE [Vouchers] (
    [VoucherID] INT IDENTITY(1,1) PRIMARY KEY,
    [VoucherTypeID] TINYINT NOT NULL,
    [CreatedBy] INT NULL,
    [VoucherCode] VARCHAR(30) NOT NULL UNIQUE,
    [VoucherName] NVARCHAR(255) NOT NULL,
    [DiscountAmount] DECIMAL(12,0) NOT NULL CHECK ([DiscountAmount] > 0),
    [VoucherScope] VARCHAR(10) NOT NULL CHECK ([VoucherScope] IN ('Product', 'Shipping')),
    [MinOrderAmount] DECIMAL(12,0) NULL CHECK ([MinOrderAmount] >= 0),
    [Quantity] INT NOT NULL DEFAULT 0 CHECK ([Quantity] >= 0),
    [MaxUsagePerUser] SMALLINT NULL DEFAULT 1,
    [StartDate] DATETIME2(0) NOT NULL,
    [EndDate] DATETIME2(0) NOT NULL,
    [Status] VARCHAR(15) NOT NULL CHECK ([Status] IN ('Scheduled', 'Active', 'Inactive', 'Expired')),
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(0) NULL,
    CONSTRAINT [FK_Vouchers_VoucherTypes] FOREIGN KEY ([VoucherTypeID]) REFERENCES [VoucherTypes]([VoucherTypeID]),
    CONSTRAINT [FK_Vouchers_Accounts] FOREIGN KEY ([CreatedBy]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [CK_Vouchers_DateRange] CHECK ([StartDate] < [EndDate])
);




CREATE TABLE [OrderVouchers] (
    [OrderID] INT NOT NULL,
    [VoucherID] INT NOT NULL,
    [DiscountAmountApplied] DECIMAL(12,0) NOT NULL CHECK ([DiscountAmountApplied] >= 0),
    CONSTRAINT [PK_OrderVouchers] PRIMARY KEY ([OrderID], [VoucherID]),
    CONSTRAINT [FK_OrderVouchers_Orders] FOREIGN KEY ([OrderID]) REFERENCES [Orders]([OrderID]),
    CONSTRAINT [FK_OrderVouchers_Vouchers] FOREIGN KEY ([VoucherID]) REFERENCES [Vouchers]([VoucherID])
);




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
    [Description] NVARCHAR(500) NULL,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE()
);




CREATE TABLE [BlogPosts] (
    [BlogPostID] INT IDENTITY(1,1) PRIMARY KEY,
    [AccountID] INT NOT NULL,
    [ApprovedBy] INT NULL,
    [BlogTitle] NVARCHAR(255) NOT NULL,
    [BlogContent] NVARCHAR(MAX) NOT NULL,
    [BlogThumbnail] VARCHAR(500) NULL,
    [Status] VARCHAR(20) NOT NULL CHECK ([Status] IN ('Draft', 'Pending', 'Approved', 'Rejected', 'Scheduled', 'Published')),
    [Reason] NVARCHAR(500) NULL,
    [IsFeatured] BIT NOT NULL DEFAULT 0,
    [BlogAt] DATETIME2(0) NULL,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(0) NULL,
    CONSTRAINT [FK_BlogPosts_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_BlogPosts_ApprovedBy] FOREIGN KEY ([ApprovedBy]) REFERENCES [Accounts]([AccountID])
);




CREATE TABLE [BlogPostCategories] (
    [BlogPostID] INT NOT NULL,
    [BlogCategoryID] SMALLINT NOT NULL,
    CONSTRAINT [PK_BlogPostCategories] PRIMARY KEY ([BlogPostID], [BlogCategoryID]),
    CONSTRAINT [FK_BlogPostCategories_BlogPosts] FOREIGN KEY ([BlogPostID]) REFERENCES [BlogPosts]([BlogPostID]),
    CONSTRAINT [FK_BlogPostCategories_BlogCategories] FOREIGN KEY ([BlogCategoryID]) REFERENCES [BlogCategories]([BlogCategoryID])
);




CREATE TABLE [Banners] (
    [BannerID] INT IDENTITY(1,1) PRIMARY KEY,
    [BannerName] NVARCHAR(255) NOT NULL,
    [ImageUrl] VARCHAR(500) NOT NULL,
    [LinkUrl] VARCHAR(500) NULL,
    [Position] VARCHAR(20) NOT NULL DEFAULT 'HomePage' CHECK ([Position] IN ('HomePage', 'CategoryPage', 'ProductPage', 'CheckoutPage')),
    [StartDate] DATETIME2(0) NULL,
    [EndDate] DATETIME2(0) NULL,
    [DisplayOrder] TINYINT NOT NULL DEFAULT 0 CHECK ([DisplayOrder] >= 0),
    [IsActive] BIT NOT NULL DEFAULT 1,
    [IsDefault] BIT NOT NULL DEFAULT 0,
    [CreatedBy] INT NOT NULL,
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
    [Comment] NVARCHAR(MAX) NULL,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(0) NULL,
    CONSTRAINT [UQ_Review_Account_Order_Product] UNIQUE ([AccountID], [OrderID], [ProductID]),
    CONSTRAINT [FK_Reviews_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_Reviews_Products] FOREIGN KEY ([ProductID]) REFERENCES [Products]([ProductID]),
    CONSTRAINT [FK_Reviews_Orders] FOREIGN KEY ([OrderID]) REFERENCES [Orders]([OrderID])
);




CREATE TABLE [ReviewProductImages] (
    [ReviewProductImageID] INT IDENTITY(1,1) PRIMARY KEY,
    [ReviewProductID] INT NOT NULL,
    [ImageURL] VARCHAR(500) NOT NULL,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_ReviewProductImages_ReviewProducts] FOREIGN KEY ([ReviewProductID]) REFERENCES [ReviewProducts]([ReviewID])
);




CREATE TABLE [ReviewProductReplies] (
    [ReplyProductID] INT IDENTITY(1,1) PRIMARY KEY,
    [ReviewProductID] INT NOT NULL,
    [AccountID] INT NOT NULL,
    [Content] NVARCHAR(MAX) NOT NULL,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_ReviewProductReplies_ReviewProducts] FOREIGN KEY ([ReviewProductID]) REFERENCES [ReviewProducts]([ReviewID]),
    CONSTRAINT [FK_ReviewProductReplies_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID])
);




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




CREATE TABLE [WalletTransactions] (
    [WalletTransactionID] INT IDENTITY(1,1) PRIMARY KEY,
    [WalletID] INT NOT NULL,
    [AccountID] INT NOT NULL,
    [RelatedOrderID] INT NULL,
    [RelatedPaymentHistoryID] INT NULL,
    [TxnType] VARCHAR(10) NOT NULL CHECK ([TxnType] IN ('TopUp', 'Payment', 'Refund')),
    [Direction] CHAR(2) NOT NULL CHECK ([Direction] IN ('CR', 'DR')),
    [Amount] DECIMAL(12,0) NOT NULL CHECK ([Amount] > 0),
    [BalanceBefore] DECIMAL(12,0) NOT NULL,
    [BalanceAfter] DECIMAL(12,0) NOT NULL,
    [Method] VARCHAR(10) NOT NULL CHECK ([Method] IN ('Bank', 'EWallet', 'Cash', 'Wallet')),
    [ExternalRef] VARCHAR(100) NULL,
    [IdempotencyKey] VARCHAR(100) NULL,
    [Status] VARCHAR(15) NOT NULL DEFAULT 'Pending' CHECK ([Status] IN ('Pending', 'Completed', 'Failed', 'Cancelled')),
    [Reason] NVARCHAR(500) NULL,
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




CREATE TABLE [PaymentHistory] (
    [PaymentHistoryID] INT IDENTITY(1,1) PRIMARY KEY,
    [AccountID] INT NOT NULL,
    [OrderID] INT NOT NULL,
    [WalletTransactionID] INT NULL,
    [PaymentStatus] VARCHAR(20) NOT NULL,
    [PaymentMethod] VARCHAR(20) NOT NULL,
    [TransactionCode] VARCHAR(100) NULL,
    [Amount] DECIMAL(12,0) NOT NULL,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_PaymentHistory_Orders] FOREIGN KEY ([OrderID]) REFERENCES [Orders]([OrderID]),
    CONSTRAINT [FK_PaymentHistory_WalletTransactions] FOREIGN KEY ([WalletTransactionID]) REFERENCES [WalletTransactions]([WalletTransactionID]),
    CONSTRAINT [FK_PaymentHistory_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID])
);




CREATE TABLE [OrderRefunds] (
    [RefundID] INT IDENTITY(1,1) PRIMARY KEY,
    [OrderID] INT NOT NULL,
    [CustomerID] INT NOT NULL,
    [RequestedBy] INT NULL,
    [ApprovedBy] INT NULL,
    [WalletTransactionID] INT NULL,
    [ImgURL] VARCHAR(200) NOT NULL,
    [Reason] NVARCHAR(500),
    [ApprovedAmount] DECIMAL(12,0) NOT NULL,
    [RefundStatus] VARCHAR(20) NOT NULL DEFAULT 'Requested',
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_OrderRefunds_Orders] FOREIGN KEY ([OrderID]) REFERENCES [Orders]([OrderID]),
    CONSTRAINT [FK_OrderRefunds_WalletTransactions] FOREIGN KEY ([WalletTransactionID]) REFERENCES [WalletTransactions]([WalletTransactionID]),
    CONSTRAINT [FK_OrderRefunds_Customer] FOREIGN KEY ([CustomerID]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_OrderRefunds_RequestedBy] FOREIGN KEY ([RequestedBy]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_OrderRefunds_ApprovedBy] FOREIGN KEY ([ApprovedBy]) REFERENCES [Accounts]([AccountID])
);
GO




/* =============================================
   9. NOTIFICATION & CHAT & INTERACTIONS
============================================= */
CREATE TABLE [Notification].[Templates] (
    [TemplateID] SMALLINT IDENTITY(1,1) PRIMARY KEY,
    [TemplateCode] VARCHAR(100) NOT NULL UNIQUE,
    [TitleTemplate] NVARCHAR(255) NOT NULL,
    [MessageTemplate] NVARCHAR(1000) NOT NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(0) NULL
);




CREATE TABLE [Notification].[Deliveries] (
    [DeliveryID] BIGINT IDENTITY(1,1) PRIMARY KEY,
    [AccountID] INT NOT NULL,
    [CreatedByJobID] INT NULL,
    [TemplateCode] VARCHAR(100) NOT NULL,
    [Title] NVARCHAR(255) NOT NULL,
    [Message] NVARCHAR(1000) NOT NULL,
    [Payload] NVARCHAR(MAX) NOT NULL,
    [Status] VARCHAR(10) NOT NULL DEFAULT 'Unread',
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_NotificationDeliveries_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID]),
    CONSTRAINT [FK_NotificationDeliveries_Templates] FOREIGN KEY ([TemplateCode]) REFERENCES [Notification].[Templates]([TemplateCode]),
    CONSTRAINT [FK_NotificationDeliveries_BackgroundJobs] FOREIGN KEY ([CreatedByJobID]) REFERENCES [System].[BackgroundJobs]([JobID])
);




CREATE TABLE [ChatConversations] (
    [ConversationID] INT IDENTITY(1,1) PRIMARY KEY,
    [AccountID] INT NULL,
    [SessionID] VARCHAR(100) NULL,
    [Status] VARCHAR(15) NOT NULL DEFAULT 'BotActive',
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_ChatConversations_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID])
);




CREATE TABLE [ChatMessages] (
    [MessageID] INT IDENTITY(1,1) PRIMARY KEY,
    [ConversationID] INT NOT NULL,
    [SenderType] VARCHAR(10) NOT NULL,
    [Content] NVARCHAR(MAX) NOT NULL,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_ChatMessages_ChatConversations] FOREIGN KEY ([ConversationID]) REFERENCES [ChatConversations]([ConversationID])
);




CREATE TABLE [Interaction].[Events] (
    [EventID] BIGINT IDENTITY(1,1) PRIMARY KEY,
    [AccountID] INT NULL,
    [SessionID] VARCHAR(100) NOT NULL,
    [EventType] VARCHAR(50) NOT NULL,
    [EntityID] VARCHAR(50) NOT NULL,
    [EntityType] VARCHAR(30) NOT NULL,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_Events_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [Accounts]([AccountID])
);




CREATE TABLE [Recommendation].[ItemSimilarities] (
    [SimilarityID] INT IDENTITY(1,1) PRIMARY KEY,
    [SourceProductID] INT NOT NULL,
    [SimilarProductID] INT NOT NULL,
    [SimilarityScore] DECIMAL(5,4) NOT NULL,
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_ItemSimilarities_SourceProduct] FOREIGN KEY ([SourceProductID]) REFERENCES [Products]([ProductID]),
    CONSTRAINT [FK_ItemSimilarities_SimilarProduct] FOREIGN KEY ([SimilarProductID]) REFERENCES [Products]([ProductID])
);
GO




/* =============================================
   10. PERFORMANCE INDEXES (OPTIMIZATION)
============================================= */
CREATE NONCLUSTERED INDEX [IX_Vouchers_Worker] ON [Vouchers]([Status], [StartDate], [EndDate]);
CREATE NONCLUSTERED INDEX [IX_Promotions_Worker] ON [Promotions]([Status], [StartDate], [EndDate]);

CREATE NONCLUSTERED INDEX [IX_Products_LowStock_V2]
ON [Products]([ProductStatus], [IsDeleted])
INCLUDE ([Quantity], [StockThreshold])
WHERE ([Quantity] <= 10 AND [IsDeleted] = 0 AND [ProductStatus] = 'Active');

CREATE NONCLUSTERED INDEX [IX_NotificationDeliveries_User]
ON [Notification].[Deliveries]([AccountID], [Status])
INCLUDE ([CreatedAt])
WHERE ([Status] = 'Unread');

CREATE NONCLUSTERED INDEX [IX_Orders_UserHistory] ON [Orders]([AccountID], [OrderDate] DESC);
CREATE NONCLUSTERED INDEX [IX_Wishlists_User] ON [Wishlists]([AccountID]);
CREATE NONCLUSTERED INDEX [IX_ChatMessages_Conversation] ON [ChatMessages]([ConversationID], [CreatedAt] DESC);
CREATE NONCLUSTERED INDEX [IX_Products_FilterSort] ON [Products]([CategoryID], [BrandID], [Price], [IsDeleted]);

CREATE NONCLUSTERED INDEX [IX_InteractionEvents_UserBehavior]
ON [Interaction].[Events]([AccountID], [EventType], [CreatedAt] DESC);

CREATE NONCLUSTERED INDEX [IX_ItemSimilarities_Source]
ON [Recommendation].[ItemSimilarities]([SourceProductID])
INCLUDE ([SimilarProductID], [SimilarityScore]);

CREATE NONCLUSTERED INDEX [IX_Orders_ReportByDate]
ON [Orders]([OrderDate], [PaymentStatus])
INCLUDE ([TotalAmount], [VoucherDiscountAmount]);

CREATE NONCLUSTERED INDEX [IX_OrderDetails_ProductSales]
ON [OrderDetails]([ProductID])
INCLUDE ([Quantity], [LineTotal]);

CREATE NONCLUSTERED INDEX [IX_VoucherUsageLogs_Analytics]
ON [VoucherUsageLogs]([VoucherID], [UsedAt]);

CREATE NONCLUSTERED INDEX [IX_WalletTransactions_Wallet]
ON [WalletTransactions]([WalletID], [CreatedAt] DESC)
INCLUDE ([TxnType], [Amount], [Status]);

CREATE NONCLUSTERED INDEX [IX_WalletTransactions_Account]
ON [WalletTransactions]([AccountID], [CreatedAt] DESC);

CREATE NONCLUSTERED INDEX [IX_WalletTransactions_Order]
ON [WalletTransactions]([RelatedOrderID])
WHERE [RelatedOrderID] IS NOT NULL;

CREATE NONCLUSTERED INDEX [IX_PaymentHistory_Order]
ON [PaymentHistory]([OrderID])
INCLUDE ([PaymentStatus], [Amount], [CreatedAt]);

CREATE NONCLUSTERED INDEX [IX_OrderRefunds_Order]
ON [OrderRefunds]([OrderID])
INCLUDE ([RefundStatus], [ApprovedAmount]);

CREATE NONCLUSTERED INDEX [IX_ReviewProducts_Product]
ON [ReviewProducts]([ProductID], [IsDeleted])
INCLUDE ([Rating], [CreatedAt]);

CREATE UNIQUE NONCLUSTERED INDEX [IX_Accounts_EmployeeCode]
ON [Accounts]([EmployeeCode])
WHERE [EmployeeCode] IS NOT NULL;

CREATE NONCLUSTERED INDEX [IX_DomainEventOutbox_Pending]
ON [System].[DomainEventOutbox]([ProcessedOn])
WHERE [ProcessedOn] IS NULL;

CREATE NONCLUSTERED INDEX [IX_Products_Search]
ON [Products]([ProductName], [CategoryID], [BrandID])
INCLUDE ([Price], [Quantity], [ProductStatus]);

CREATE NONCLUSTERED INDEX [IX_Accounts_Login]
ON [Accounts]([Email], [IsActive], [IsDeleted])
INCLUDE ([PasswordHash], [RoleID]);

CREATE NONCLUSTERED INDEX [IX_CartItems_ActiveCart]
ON [CartItems]([CartID], [RemovedAt])
WHERE [RemovedAt] IS NULL;

CREATE NONCLUSTERED INDEX [IX_Orders_StatusTracking]
ON [Orders]([OrderCode], [PaymentStatus], [StatusID]);
GO




/* =============================================
   11. UNIQUE INDEXES FOR BUSINESS RULES
============================================= */
CREATE UNIQUE NONCLUSTERED INDEX [IX_Addresses_OneDefaultPerUser]
ON [Addresses]([AccountID])
WHERE [IsDefault] = 1 AND [IsDeleted] = 0;

CREATE UNIQUE NONCLUSTERED INDEX [IX_ProductImages_OneMainPerProduct]
ON [ProductImages]([ProductID])
WHERE [IsMain] = 1;
GO




/* =============================================
   12. TRIGGERS - DATA SYNC & VALIDATION
============================================= */
CREATE OR ALTER TRIGGER [TR_OrderDetails_SyncSubTotal]
ON [OrderDetails]
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @AffectedOrders TABLE (OrderID INT PRIMARY KEY);
    INSERT INTO @AffectedOrders (OrderID)
    SELECT DISTINCT OrderID FROM inserted
    UNION
    SELECT DISTINCT OrderID FROM deleted;

    UPDATE o
    SET o.[SubTotal] = ISNULL(calc.Total, 0),
        o.[UpdatedAt] = GETDATE()
    FROM [Orders] o
    INNER JOIN @AffectedOrders ao ON o.[OrderID] = ao.[OrderID]
    LEFT JOIN (
        SELECT [OrderID], SUM([LineTotal]) AS Total
        FROM [OrderDetails] GROUP BY [OrderID]
    ) calc ON o.[OrderID] = calc.[OrderID]
    WHERE o.[SubTotal] <> ISNULL(calc.Total, 0);
END;
GO


CREATE OR ALTER TRIGGER [TR_OrderVouchers_SyncDiscount]
ON [OrderVouchers]
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @AffectedOrders TABLE (OrderID INT PRIMARY KEY);
    INSERT INTO @AffectedOrders (OrderID)
    SELECT DISTINCT OrderID FROM inserted
    UNION
    SELECT DISTINCT OrderID FROM deleted;

    UPDATE o
    SET o.[VoucherDiscountAmount] = ISNULL(calc.Total, 0),
        o.[UpdatedAt] = GETDATE()
    FROM [Orders] o
    INNER JOIN @AffectedOrders ao ON o.[OrderID] = ao.[OrderID]
    LEFT JOIN (
        SELECT [OrderID], SUM([DiscountAmountApplied]) AS Total
        FROM [OrderVouchers] GROUP BY [OrderID]
    ) calc ON o.[OrderID] = calc.[OrderID];
END;
GO


CREATE OR ALTER TRIGGER [TR_VoucherUsageLogs_DecreaseQuantity]
ON [VoucherUsageLogs]
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE v
    SET v.[Quantity] = v.[Quantity] - u.Cnt,
        v.[UpdatedAt] = GETDATE()
    FROM [Vouchers] v
    INNER JOIN (
        SELECT [VoucherID], COUNT(*) AS Cnt FROM inserted GROUP BY [VoucherID]
    ) u ON v.[VoucherID] = u.[VoucherID];

    IF EXISTS (SELECT 1 FROM [Vouchers] WHERE [Quantity] < 0)
    BEGIN
        RAISERROR('Voucher quantity insufficient.', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;
GO


CREATE OR ALTER TRIGGER [TR_Products_SyncStatusWithQuantity]
ON [Products]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE([Quantity]) RETURN;

    UPDATE p SET p.[ProductStatus] = 'OutOfStock', p.[UpdatedAt] = GETDATE()
    FROM [Products] p
    INNER JOIN inserted i ON p.[ProductID] = i.[ProductID]
    INNER JOIN deleted d ON p.[ProductID] = d.[ProductID]
    WHERE i.[Quantity] = 0 AND d.[Quantity] > 0 AND i.[ProductStatus] = 'Active' AND p.[IsDeleted] = 0;

    UPDATE p SET p.[ProductStatus] = 'Active', p.[UpdatedAt] = GETDATE()
    FROM [Products] p
    INNER JOIN inserted i ON p.[ProductID] = i.[ProductID]
    INNER JOIN deleted d ON p.[ProductID] = d.[ProductID]
    WHERE i.[Quantity] > 0 AND d.[Quantity] = 0 AND i.[ProductStatus] = 'OutOfStock' AND p.[IsDeleted] = 0;
END;
GO


CREATE OR ALTER TRIGGER [TR_Promotions_SyncStatusWithDates]
ON [Promotions]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Now DATETIME2(0) = GETDATE();
    UPDATE p
    SET p.[Status] = CASE
        WHEN i.[Status] = 'Inactive' THEN 'Inactive'
        WHEN i.[StartDate] > @Now THEN 'Scheduled'
        WHEN i.[EndDate] < @Now THEN 'Expired'
        ELSE 'Active'
    END
    FROM [Promotions] p
    INNER JOIN inserted i ON p.[PromotionID] = i.[PromotionID]
    WHERE i.[IsDeleted] = 0;
END;
GO


CREATE OR ALTER TRIGGER [TR_Vouchers_SyncStatusWithDates]
ON [Vouchers]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Now DATETIME2(0) = GETDATE();
    UPDATE v
    SET v.[Status] = CASE
        WHEN i.[Status] = 'Inactive' THEN 'Inactive'
        WHEN i.[Quantity] = 0 THEN 'Inactive'
        WHEN i.[StartDate] > @Now THEN 'Scheduled'
        WHEN i.[EndDate] < @Now THEN 'Expired'
        ELSE 'Active'
    END,
    v.[UpdatedAt] = GETDATE()
    FROM [Vouchers] v
    INNER JOIN inserted i ON v.[VoucherID] = i.[VoucherID]
    WHERE i.[IsDeleted] = 0;
END;
GO


CREATE OR ALTER TRIGGER [TR_Orders_CalculateTotalAmount]
ON [Orders]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF UPDATE(TotalAmount) AND NOT (UPDATE(SubTotal) OR UPDATE(VoucherDiscountAmount) OR UPDATE(ActualShippingFee) OR UPDATE(EstimatedShippingFee))
        RETURN;

    UPDATE o
    SET o.[TotalAmount] = i.[SubTotal] - i.[VoucherDiscountAmount] + ISNULL(i.[ActualShippingFee], i.[EstimatedShippingFee]),
        o.[UpdatedAt] = GETDATE()
    FROM [Orders] o
    INNER JOIN inserted i ON o.[OrderID] = i.[OrderID]
    WHERE ABS(o.[TotalAmount] - (i.[SubTotal] - i.[VoucherDiscountAmount] + ISNULL(i.[ActualShippingFee], i.[EstimatedShippingFee]))) > 0.01;
END;
GO


CREATE OR ALTER TRIGGER [TR_ReviewProducts_ValidateProductInOrder]
ON [ReviewProducts]
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM inserted i
        WHERE NOT EXISTS (
            SELECT 1 FROM [OrderDetails] od
            WHERE od.[OrderID] = i.[OrderID] AND od.[ProductID] = i.[ProductID]
        )
    )
    BEGIN
        RAISERROR('Cannot review product not in this order.', 16, 1);
        RETURN;
    END

    IF EXISTS (
        SELECT 1 FROM inserted i
        INNER JOIN [Orders] o ON i.[OrderID] = o.[OrderID]
        INNER JOIN [StatusOrders] s ON o.[StatusID] = s.[StatusID]
        WHERE s.[StatusName] NOT IN ('Delivered', 'Completed')
    )
    BEGIN
        RAISERROR('Order must be delivered before review.', 16, 1);
        RETURN;
    END

    INSERT INTO [ReviewProducts] ([AccountID], [ProductID], [OrderID], [Rating], [Comment], [IsDeleted], [CreatedAt], [UpdatedAt])
    SELECT [AccountID], [ProductID], [OrderID], [Rating], [Comment], ISNULL([IsDeleted], 0), ISNULL([CreatedAt], GETDATE()), [UpdatedAt]
    FROM inserted;
END;
GO


CREATE OR ALTER TRIGGER [TR_VoucherUsageLogs_ValidateMaxUsage]
ON [VoucherUsageLogs]
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM inserted i
        INNER JOIN [Vouchers] v ON i.[VoucherID] = v.[VoucherID]
        WHERE v.[MaxUsagePerUser] IS NOT NULL
          AND (SELECT COUNT(*) FROM [VoucherUsageLogs] vul
               WHERE vul.[VoucherID] = i.[VoucherID] AND vul.[AccountID] = i.[AccountID]) >= v.[MaxUsagePerUser]
    )
    BEGIN
        RAISERROR('Max usage per user exceeded.', 16, 1);
        RETURN;
    END

    IF EXISTS (
        SELECT 1 FROM inserted i
        INNER JOIN [Vouchers] v ON i.[VoucherID] = v.[VoucherID]
        WHERE v.[Status] <> 'Active' OR v.[IsDeleted] = 1 OR v.[Quantity] <= 0
           OR v.[StartDate] > GETDATE() OR v.[EndDate] < GETDATE()
    )
    BEGIN
        RAISERROR('Voucher invalid or expired.', 16, 1);
        RETURN;
    END

    INSERT INTO [VoucherUsageLogs] ([VoucherID], [AccountID], [OrderID], [UsedAt])
    SELECT [VoucherID], [AccountID], [OrderID], ISNULL([UsedAt], GETDATE())
    FROM inserted;
END;
GO




/* =============================================
   13. VALIDATION & CONSTRAINTS
============================================= */
ALTER TABLE [Products] ADD CONSTRAINT [CK_Products_PriceQuantity]
CHECK ([Price] >= 0 AND [Quantity] >= 0 AND [StockThreshold] >= 0);

ALTER TABLE [Orders] ADD CONSTRAINT [CK_Orders_AmountLogic]
CHECK ([SubTotal] >= 0 AND [TotalAmount] >= 0 AND [VoucherDiscountAmount] >= 0);

ALTER TABLE [CartItems] ADD CONSTRAINT [CK_CartItems_ValidQuantity]
CHECK ([Quantity] > 0 AND [Quantity] <= 999);
GO


/* =============================================
   14. STORED PROCEDURES
============================================= */
CREATE OR ALTER PROCEDURE [System].[SP_ValidateDataIntegrity]
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ErrorMsg NVARCHAR(MAX) = '';

    IF EXISTS (SELECT 1 FROM [OrderDetails] od LEFT JOIN [Orders] o ON od.OrderID = o.OrderID WHERE o.OrderID IS NULL)
        SET @ErrorMsg = @ErrorMsg + 'Orphaned OrderDetails found. ';

    IF EXISTS (SELECT 1 FROM [Orders] WHERE ABS([TotalAmount] - ([SubTotal] - [VoucherDiscountAmount] + ISNULL([ActualShippingFee], [EstimatedShippingFee]))) > 0.01)
        SET @ErrorMsg = @ErrorMsg + 'Invalid order totals found. ';

    IF @ErrorMsg <> ''
        RAISERROR(@ErrorMsg, 16, 1);
    ELSE
        SELECT 'Data integrity validation passed' AS Result;
END;
GO


CREATE OR ALTER PROCEDURE [System].[SP_SyncPromotionVoucherStatus]
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Now DATETIME2(0) = GETDATE();

    UPDATE [Promotions]
    SET [Status] = CASE
        WHEN [StartDate] <= @Now AND [EndDate] >= @Now THEN 'Active'
        WHEN [EndDate] < @Now THEN 'Expired'
        ELSE [Status]
    END
    WHERE [Status] NOT IN ('Inactive') AND [IsDeleted] = 0
      AND (([Status] = 'Scheduled' AND [StartDate] <= @Now) OR ([Status] = 'Active' AND [EndDate] < @Now));

    UPDATE [Vouchers]
    SET [Status] = CASE
        WHEN [Quantity] = 0 THEN 'Inactive'
        WHEN [StartDate] <= @Now AND [EndDate] >= @Now THEN 'Active'
        WHEN [EndDate] < @Now THEN 'Expired'
        ELSE [Status]
    END, [UpdatedAt] = @Now
    WHERE [Status] NOT IN ('Inactive') AND [IsDeleted] = 0
      AND ([Quantity] = 0 OR ([Status] = 'Scheduled' AND [StartDate] <= @Now) OR ([Status] = 'Active' AND [EndDate] < @Now));

    SELECT @@ROWCOUNT AS RowsUpdated;
END;
GO
