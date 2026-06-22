/* =================================================================
   DataSeed FULL v5.3 – SEP490_ToyStore
   Updated for Schema v3.2
================================================================= */

USE [SEP490_ToyStore];
GO
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

PRINT N'================================================================';
PRINT N'  DataSeed v5.3 (Schema v3.2 Compatible) – Starting...';
PRINT N'================================================================';

/* ══════════════════════════════════════════════════════════════
   SECTION 1 – ROLES
══════════════════════════════════════════════════════════════ */
PRINT N'[1] Roles...';
SET IDENTITY_INSERT [dbo].[Roles] ON;
IF NOT EXISTS (SELECT 1
FROM [dbo].[Roles]
WHERE RoleID = 1)
    INSERT INTO [dbo].[Roles]
    (RoleID, RoleName, Description, CreatedAt)
VALUES
    (1, 'Customer', N'Customer', '2024-01-05 08:00:00'),
    (2, 'Admin', N'Administrator', '2024-01-05 08:00:00'),
    (3, 'Staff', N'Sales Staff', '2024-01-05 08:00:00'),
    (4, 'Merchandise', N'Warehouse Staff', '2024-01-05 08:00:00');
SET IDENTITY_INSERT [dbo].[Roles] OFF;
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 2 – SEXES
══════════════════════════════════════════════════════════════ */
PRINT N'[2] Sexes...';
SET IDENTITY_INSERT [dbo].[Sexes] ON;
IF NOT EXISTS (SELECT 1
FROM [dbo].[Sexes]
WHERE SexID = 1)
    INSERT INTO [dbo].[Sexes]
    (SexID, SexName, CreatedAt)
VALUES
    (1, N'Male', '2024-01-05 08:00:00'),
    (2, N'Female', '2024-01-05 08:00:00'),
    (3, N'Other', '2024-01-05 08:00:00');
SET IDENTITY_INSERT [dbo].[Sexes] OFF;
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 3 – ACCOUNTS
══════════════════════════════════════════════════════════════ */
PRINT N'[3] Accounts – Staff & Admin...';
IF NOT EXISTS (SELECT 1
FROM [dbo].[Accounts]
WHERE Email = 'admin@toyhouse.vn')
    INSERT INTO [dbo].[Accounts]
    (RoleID, SexID, EmployeeCode, AccountName, PhoneNumber, Email, DOB, ImageURL, PasswordHash, IsActive, CreatedAt)
VALUES
    (2, 1, 'AD001', N'Michael Admin', '0901000001', 'admin@toyhouse.vn', '1990-03-15', 'https://res.cloudinary.com/datfyxi3f/image/upload/v1780546687/4f2018e8-791f-416c-9bf8-345926edeccd_ytjkb1.jpg', '$2a$11$K3u.z94n.5aM/60s07bZyeUv20a1K6u7m2R5YkM10vQh2h2.2H5K.', 1, '2024-01-05 08:00:00'),
    (3, 2, 'ST001', N'Sarah Miller', '0901000002', 'nhung.st@toyhouse.vn', '1995-07-22', 'https://res.cloudinary.com/datfyxi3f/image/upload/v1780546686/9534bc17-de93-49bd-833f-7ae947019fd8_nkgjz9.jpg', '$2a$11$K3u.z94n.5aM/60s07bZyeUv20a1K6u7m2R5YkM10vQh2h2.2H5K.', 1, '2024-01-05 08:00:00'),
    (3, 1, 'ST002', N'David Johnson', '0901000003', 'dung.st@toyhouse.vn', '1993-11-08', 'https://res.cloudinary.com/datfyxi3f/image/upload/v1780546686/cce0bdb4-1054-4391-9b11-708fb552ad2b_c2mgro.jpg', '$2a$11$K3u.z94n.5aM/60s07bZyeUv20a1K6u7m2R5YkM10vQh2h2.2H5K.', 1, '2024-01-10 08:00:00'),
    (3, 2, 'ST003', N'Emily Davis', '0901000004', 'thao.st@toyhouse.vn', '1997-04-30', 'https://res.cloudinary.com/datfyxi3f/image/upload/v1780546686/26e757ce-03e1-4598-bcfb-bf90d7250672Nu_zecjul.jpg', '$2a$11$K3u.z94n.5aM/60s07bZyeUv20a1K6u7m2R5YkM10vQh2h2.2H5K.', 1, '2024-01-10 08:00:00'),
    (4, 1, 'MC001', N'Robert Wilson', '0901000005', 'bao.kho@toyhouse.vn', '1992-09-18', 'https://res.cloudinary.com/datfyxi3f/image/upload/v1780546686/0ec897fc-0714-4c8c-96f1-4a8b02f4bf12_ncoay0.jpg', '$2a$11$K3u.z94n.5aM/60s07bZyeUv20a1K6u7m2R5YkM10vQh2h2.2H5K.', 1, '2024-01-05 08:00:00'),
    (4, 2, 'MC002', N'Laura Brown', '0901000006', 'lan.kho@toyhouse.vn', '1994-06-25', 'https://res.cloudinary.com/datfyxi3f/image/upload/v1780548037/2ec7bb2c-7ae9-41b7-9c51-bc69fccdf731_lafvlw.jpg', '$2a$11$K3u.z94n.5aM/60s07bZyeUv20a1K6u7m2R5YkM10vQh2h2.2H5K.', 1, '2024-01-10 08:00:00');
GO

PRINT N'[3] Accounts – Customers (5 users)...';
IF NOT EXISTS (SELECT 1
FROM [dbo].[Accounts]
WHERE Email = 'lananh.pham@gmail.com')
    INSERT INTO [dbo].[Accounts]
    (RoleID, SexID, AccountName, PhoneNumber, Email, DOB, ImageURL, PasswordHash, IsActive, CreatedAt)
VALUES
    (1, 2, N'Anna Pham', '0912001001', 'lananh.pham@gmail.com', '1988-03-12', 'https://i.pravatar.cc/300?img=35', '$2a$11$K3u.z94n.5aM/60s07bZyeUv20a1K6u7m2R5YkM10vQh2h2.2H5K.', 1, '2024-03-12 10:15:00'),
    (1, 1, N'Henry Nguyen', '0912001002', 'hung.nguyen88@gmail.com', '1988-06-20', 'https://i.pravatar.cc/300?img=68', '$2a$11$K3u.z94n.5aM/60s07bZyeUv20a1K6u7m2R5YkM10vQh2h2.2H5K.', 1, '2024-04-02 14:30:00'),
    (1, 2, N'Chloe Vu', '0912001003', 'bauchau.vu@gmail.com', '1992-01-15', 'https://i.pravatar.cc/300?img=38', '$2a$11$K3u.z94n.5aM/60s07bZyeUv20a1K6u7m2R5YkM10vQh2h2.2H5K.', 1, '2024-04-20 09:00:00'),
    (1, 1, N'Marcus Do', '0912001004', 'mtuando@outlook.com', '1985-08-10', 'https://i.pravatar.cc/300?img=60', '$2a$11$K3u.z94n.5aM/60s07bZyeUv20a1K6u7m2R5YkM10vQh2h2.2H5K.', 1, '2024-05-08 16:45:00'),
    (1, 2, N'Hannah Hoang', '0912001005', 'thuha.hoang@gmail.com', '1990-12-05', 'https://i.pravatar.cc/300?img=45', '$2a$11$K3u.z94n.5aM/60s07bZyeUv20a1K6u7m2R5YkM10vQh2h2.2H5K.', 1, '2024-06-01 11:20:00');
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 5 – LOOKUP TABLES
══════════════════════════════════════════════════════════════ */
PRINT N'[5] Lookup tables...';

SET IDENTITY_INSERT [dbo].[SuperCategories] ON;
IF NOT EXISTS (SELECT 1
FROM [dbo].[SuperCategories]
WHERE SuperCategoryID = 1)
    INSERT INTO [dbo].[SuperCategories]
    (SuperCategoryID, SuperCategoryName, CreatedAt)
VALUES
    (1, N'Building Toys', '2024-01-05 08:00:00'),
    (2, N'Educational Toys', '2024-01-05 08:00:00'),
    (3, N'Outdoor Activities', '2024-01-05 08:00:00'),
    (4, N'Dolls & Plush Toys', '2024-01-05 08:00:00'),
    (5, N'Figures & Models', '2024-01-05 08:00:00'),
    (6, N'Remote Control Toys', '2024-01-05 08:00:00');
SET IDENTITY_INSERT [dbo].[SuperCategories] OFF;

SET IDENTITY_INSERT [dbo].[Categories] ON;
IF NOT EXISTS (SELECT 1
FROM [dbo].[Categories]
WHERE CategoryID = 1)
    INSERT INTO [dbo].[Categories]
    (CategoryID, SuperCategoryID, CategoryName, CreatedAt)
VALUES
    ( 1, 1, N'Lego', '2024-01-05 08:00:00'),
    ( 2, 1, N'Building Blocks', '2024-01-05 08:00:00'),
    ( 3, 2, N'Flash Cards', '2024-01-05 08:00:00'),
    ( 4, 2, N'Science Toys', '2024-01-05 08:00:00'),
    ( 5, 3, N'Ride-On Toys', '2024-01-05 08:00:00'),
    ( 6, 3, N'Balls', '2024-01-05 08:00:00'),
    ( 7, 3, N'Kites', '2024-01-05 08:00:00'),
    ( 8, 4, N'Teddy Bears & Plush', '2024-01-05 08:00:00'),
    ( 9, 4, N'Fashion Dolls', '2024-01-05 08:00:00'),
    (10, 5, N'Superheroes', '2024-01-05 08:00:00'),
    (11, 5, N'Dinosaurs', '2024-01-05 08:00:00'),
    (12, 6, N'RC Cars', '2024-01-05 08:00:00'),
    (13, 6, N'RC Airplanes', '2024-01-05 08:00:00');
SET IDENTITY_INSERT [dbo].[Categories] OFF;

SET IDENTITY_INSERT [dbo].[Materials] ON;
IF NOT EXISTS (SELECT 1
FROM [dbo].[Materials]
WHERE MaterialID = 1)
    INSERT INTO [dbo].[Materials]
    (MaterialID, MaterialName, CreatedAt)
VALUES
    (1, N'Premium ABS Plastic', '2024-01-05 08:00:00'),
    (2, N'Natural MDF Wood', '2024-01-05 08:00:00'),
    (3, N'Cotton & Velvet Fabric', '2024-01-05 08:00:00'),
    (4, N'Aluminum Alloy', '2024-01-05 08:00:00'),
    (5, N'Natural Rubber', '2024-01-05 08:00:00');
SET IDENTITY_INSERT [dbo].[Materials] OFF;

SET IDENTITY_INSERT [dbo].[Ages] ON;
IF NOT EXISTS (SELECT 1
FROM [dbo].[Ages]
WHERE AgeID = 1)
    INSERT INTO [dbo].[Ages]
    (AgeID, AgeRange, CreatedAt)
VALUES
    (1, N'0-1', '2024-01-05 08:00:00'),
    (2, N'1-3', '2024-01-05 08:00:00'),
    (3, N'3-6', '2024-01-05 08:00:00'),
    (4, N'6-12', '2024-01-05 08:00:00'),
    (5, N'12+', '2024-01-05 08:00:00');
SET IDENTITY_INSERT [dbo].[Ages] OFF;

SET IDENTITY_INSERT [dbo].[Origins] ON;
IF NOT EXISTS (SELECT 1
FROM [dbo].[Origins]
WHERE OriginID = 1)
    INSERT INTO [dbo].[Origins]
    (OriginID, OriginName, CreatedAt)
VALUES
    (1, N'Vietnam', '2024-01-05 08:00:00'),
    (2, N'China', '2024-01-05 08:00:00'),
    (3, N'United States', '2024-01-05 08:00:00'),
    (4, N'Japan', '2024-01-05 08:00:00'),
    (5, N'Denmark', '2024-01-05 08:00:00');
SET IDENTITY_INSERT [dbo].[Origins] OFF;

SET IDENTITY_INSERT [dbo].[Brands] ON;
IF NOT EXISTS (SELECT 1
FROM [dbo].[Brands]
WHERE BrandID = 1)
    INSERT INTO [dbo].[Brands]
    (BrandID, BrandName, CreatedAt)
VALUES
    (1, N'Lego', '2024-01-05 08:00:00'),
    (2, N'Fisher-Price', '2024-01-05 08:00:00'),
    (3, N'Hot Wheels', '2024-01-05 08:00:00'),
    (4, N'Polo', '2024-01-05 08:00:00'),
    (5, N'MyKingdom', '2024-01-05 08:00:00'),
    (6, N'Bandai', '2024-01-05 08:00:00'),
    (7, N'Hasbro', '2024-01-05 08:00:00'),
    (8, N'Mattel', '2024-01-05 08:00:00');
SET IDENTITY_INSERT [dbo].[Brands] OFF;

SET IDENTITY_INSERT [dbo].[PriceRanges] ON;
IF NOT EXISTS (SELECT 1
FROM [dbo].[PriceRanges]
WHERE PriceRangeID = 1)
    INSERT INTO [dbo].[PriceRanges]
    (PriceRangeID, PriceRangeMin, PriceRangeMax, CreatedAt)
VALUES
    (1, 0, 199000, '2024-01-05 08:00:00'),
    (2, 200000, 499000, '2024-01-05 08:00:00'),
    (3, 500000, 999000, '2024-01-05 08:00:00'),
    (4, 1000000, 1999000, '2024-01-05 08:00:00'),
    (5, 2000000, 4999000, '2024-01-05 08:00:00'),
    (6, 5000000, 9999000, '2024-01-05 08:00:00'),
    (7, 10000000, 999999000, '2024-01-05 08:00:00');
SET IDENTITY_INSERT [dbo].[PriceRanges] OFF;

SET IDENTITY_INSERT [dbo].[StatusOrders] ON;
IF NOT EXISTS (SELECT 1
FROM [dbo].[StatusOrders]
WHERE StatusID = 1)
    INSERT INTO [dbo].[StatusOrders]
    (StatusID, StatusName, Description)
VALUES
    (1, 'Pending', N'Waiting for confirmation'),
    (2, 'Confirmed', N'Order confirmed'),
    (3, 'Processing', N'Processing / Packaging'),
    (4, 'Shipped', N'Handed over to carrier'),
    (5, 'Delivering', N'Out for delivery'),
    (6, 'Delivered', N'Delivered successfully'),
    (7, 'Completed', N'Completed'),
    (8, 'Cancelled', N'Cancelled'),
    (9, 'Refunded', N'Refunded'),
    (10, 'Returning', N'Return shipment in progress'),
    (11, 'ReturnCompleted', N'Returned to warehouse, awaiting processing'),
    (12, 'DeliveryFailed', N'Delivery failed, awaiting resolution'),
    (13, 'WaitingReturn', N'Waiting for return shipment'),
    (14, 'ReturnFailed', N'Return shipment failed'),
    (15, 'Lost', N'Package lost during shipping'),
    (16, 'Damaged', N'Package damaged');
SET IDENTITY_INSERT [dbo].[StatusOrders] OFF;

SET IDENTITY_INSERT [dbo].[ReactionTypes] ON;
IF NOT EXISTS (SELECT 1
FROM [dbo].[ReactionTypes]
WHERE ReactionTypeID = 1)
    INSERT INTO [dbo].[ReactionTypes]
    (ReactionTypeID, Code, DisplayName, CreatedAt)
VALUES
    (1, 'like', N'Like', '2024-01-05 08:00:00'),
    (2, 'love', N'Love', '2024-01-05 08:00:00'),
    (3, 'haha', N'Haha', '2024-01-05 08:00:00'),
    (4, 'wow', N'Wow', '2024-01-05 08:00:00'),
    (5, 'sad', N'Sad', '2024-01-05 08:00:00');
SET IDENTITY_INSERT [dbo].[ReactionTypes] OFF;
GO

SET IDENTITY_INSERT [dbo].[StatusRefunds] ON;
IF NOT EXISTS (SELECT 1
FROM [dbo].[StatusRefunds]
WHERE StatusID = 1)
    INSERT INTO [dbo].[StatusRefunds]
    (StatusID, StatusName, Description)
VALUES
    (1, 'RefundRequested', N'Customer requested refund/return'),
    (2, 'RefundApproved', N'Request approved, waiting for return shipment creation'),
    (3, 'RefundRejected', N'Request rejected'),
    (4, 'RefundPickupCreated', N'Return shipment created, waiting for pickup'),
    (5, 'RefundShipping', N'Returned package is on the way to shop'),
    (6, 'RefundReceived', N'Shop received returned package'),
    (7, 'RefundInspectionPending', N'Package quality inspection in progress'),
    (8, 'RefundCompleted', N'Refund completed and quantity updated'),
    (9, 'RefundCancelled', N'Customer cancelled refund request'),
    (10, 'RefundDamage', N'Package damaged or lost during shipping'),
    (11, 'RefundReturnShipmentCreated', N'Return Shipment to Customer Created'),
    (12, 'RefundReturningToCustomer', N'Order Is Being Returned to Customer'),
    (13, 'RefundReturnedToCustomer', N'Order Successfully Returned to Customer'),
    (14, 'RefundReturnToCustomerFailed', N'Failed to Return Order to Customer');
SET IDENTITY_INSERT [dbo].[StatusRefunds] OFF;
GO
/* ══════════════════════════════════════════════════════════════
   SECTION 8 – PRODUCTS (30 products)
══════════════════════════════════════════════════════════════ */
PRINT N'[8] Products...';
SET IDENTITY_INSERT [dbo].[Products] ON;

IF NOT EXISTS (SELECT 1
FROM [dbo].[Products]
WHERE ProductID = 1)
    INSERT INTO [dbo].[Products]
    (ProductID,ProductName,Price,Quantity,ProductStatus,CategoryID,BrandID,PriceRangeID,StockThreshold,CreatedAt)
VALUES
    ( 1, N'Lego City Central Police Station - 668 Pieces', 1290000, 45, 'Active', 1, 1, 4, 10, '2024-02-01 08:00:00'),
    ( 2, N'Lego Technic Bugatti Chiron Supercar - 3599 Pieces', 5990000, 8, 'Active', 1, 1, 6, 3, '2024-02-03 08:00:00'),
    ( 3, N'Lego Friends Resort House - 699 Pieces', 1590000, 22, 'Active', 1, 1, 4, 5, '2024-02-05 08:00:00'),
    ( 4, N'Wooden Transportation Puzzle Blocks - 36 Pieces', 420000, 180, 'Active', 2, 5, 2, 20, '2024-02-08 08:00:00'),
    ( 5, N'Wooden Alphabet Blocks - 52 Pieces', 350000, 200, 'Active', 2, 5, 2, 30, '2024-02-10 08:00:00'),
    ( 6, N'4D Smart Flash Cards - Wild Animals (120 Cards)', 145000, 600, 'Active', 3, 2, 1, 50, '2024-02-12 08:00:00'),
    ( 7, N'Math Flash Cards - Numbers 1-100', 185000, 450, 'Active', 3, 5, 1, 50, '2024-02-14 08:00:00'),
    ( 8, N'ScienceMax Microscope 40x-400x', 375000, 75, 'Active', 4, 3, 2, 8, '2024-03-01 08:00:00'),
    ( 9, N'Erupting Volcano Experiment Kit', 280000, 120, 'Active', 4, 5, 2, 15, '2024-03-03 08:00:00'),
    (10, N'Kids Telescope Set 50x/100x', 490000, 55, 'Active', 4, 2, 2, 8, '2024-03-05 08:00:00'),
    (11, N'Donald Duck Ride-On Toy with Lights and Music', 545000, 38, 'Active', 5, 2, 3, 5, '2024-03-10 08:00:00'),
    (12, N'Disney Princess Tricycle with Push Handle', 890000, 18, 'Active', 5, 8, 3, 3, '2024-03-12 08:00:00'),
    (13, N'Boho Pattern Rubber Ball - Size 5', 85000, 320, 'Active', 6, 2, 1, 30, '2024-03-15 08:00:00'),
    (14, N'FIFA Pro Vulcanized Soccer Ball - Size 4', 165000, 250, 'Active', 6, 3, 1, 25, '2024-03-17 08:00:00'),
    (15, N'Large Eagle Kite 1.4 m with 30 m String', 115000, 140, 'Active', 7, 2, 1, 15, '2024-03-20 08:00:00'),
    (16, N'3D Dragon Kite with Carbon Frame', 185000, 90, 'Active', 7, 2, 1, 10, '2024-03-22 08:00:00'),
    (17, N'Green Rex Dinosaur Plush Toy - 80 cm', 840000, 28, 'Active', 8, 5, 3, 4, '2024-04-01 08:00:00'),
    (18, N'Panda Plush Toy - 60 cm (Lying)', 650000, 35, 'Active', 8, 5, 3, 5, '2024-04-03 08:00:00'),
    (19, N'Pastel Long-Eared Bunny Plush Toy - 45 cm', 420000, 60, 'Active', 8, 5, 2, 8, '2024-04-05 08:00:00'),
    (20, N'Barbie Dreamtopia Mermaid Doll with 3 Outfits', 359000, 110, 'Active', 9, 8, 2, 12, '2024-04-08 08:00:00'),
    (21, N'Barbie Fashionista Set - 6 Outfits', 490000, 75, 'Active', 9, 8, 2, 8, '2024-04-10 08:00:00'),
    (22, N'Gao Red Ranger Action Figure - 24 Points', 595000, 22, 'Active', 10, 6, 3, 4, '2024-04-12 08:00:00'),
    (23, N'Kamen Rider Zero-One SHFiguarts Figure', 890000, 15, 'Active', 10, 6, 3, 3, '2024-04-14 08:00:00'),
    (24, N'T-Rex Dinosaur Figure - 1:10 Scale', 395000, 88, 'Active', 11, 3, 2, 10, '2024-04-16 08:00:00'),
    (25, N'Jurassic World Mini Dinosaur Set - 6 Pack', 285000, 150, 'Active', 11, 7, 2, 15, '2024-04-18 08:00:00'),
    (26, N'RC Off-Road Car Traxxas TRX-Mini 4x4', 945000, 55, 'Active', 12, 3, 3, 6, '2024-05-01 08:00:00'),
    (27, N'RC Drift Car - 2.4GHz', 680000, 40, 'Active', 12, 3, 3, 6, '2024-05-03 08:00:00'),
    (28, N'Mini Gyro RC Helicopter - 2.4GHz', 1490000, 12, 'Active', 13, 6, 4, 2, '2024-05-10 08:00:00'),
    (29, N'Play-Doh 24-Color Non-Toxic Modeling Clay', 88000, 980, 'Active', 2, 7, 1, 80, '2024-05-15 08:00:00'),
    (30, N'10-Inch LCD Writing Tablet with Stylus', 175000, 380, 'Active', 3, 2, 1, 40, '2024-05-20 08:00:00');

SET IDENTITY_INSERT [dbo].[Products] OFF;
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 9 – PRODUCT DETAILS
══════════════════════════════════════════════════════════════ */
PRINT N'[9] ProductDetails...';

IF NOT EXISTS (SELECT 1
FROM [dbo].[ProductDetails]
WHERE ProductID = 1)
    INSERT INTO [dbo].[ProductDetails]
    (ProductID, Description, MaterialID, AgeID, SexID, OriginID,
    WeightGram, LengthCm, WidthCm, HeightCm)
VALUES
    -- Lego (1-3)
    ( 1, N'Lego City 668-piece set recreating a 3-story police station. ABS plastic. Suitable for ages 6-12.', 1, 4, 1, 5, 900, 48, 28, 9),
    ( 2, N'Lego Technic 3599-piece Bugatti Chiron supercar 1:8 scale with a simulated W16 engine. Suitable for ages 12+.', 1, 5, 1, 5, 3400, 58, 38, 14),
    ( 3, N'Lego Friends resort house with swimming pool, spa, and 5 characters. Suitable for ages 6-12.', 1, 4, 2, 5, 1100, 53, 37, 10),

    -- Wooden blocks (4-5)
    ( 4, N'36 MDF wooden blocks with water-based paint and rounded 5 mm edges. Suitable for ages 3-6.', 2, 3, 3, 1, 650, 30, 20, 10),
    ( 5, N'52 colorful wooden alphabet blocks A-Z and a-z. Suitable for ages 1-3.', 2, 2, 3, 1, 750, 32, 22, 10),

    -- Flash cards (6-7)
    ( 6, N'120 4D flash cards integrated with AR. Scan the app to view 60 vivid animals.', 3, 3, 3, 2, 300, 22, 15, 5),
    ( 7, N'100 math learning cards from 1-100. Double-sided with counting numbers and exercises. Suitable for ages 3-6.', 3, 3, 3, 1, 250, 21, 14, 4),

    -- Science toys (8-10)
    ( 8, N'ScienceMax microscope with 3 zoom levels (40x/100x/400x), LED light, and 12 slides.', 1, 4, 3, 3, 850, 30, 15, 20),
    ( 9, N'Erupting volcano experiment kit including 12 safe experiments. Suitable for ages 6+.', 1, 4, 3, 3, 420, 26, 20, 8),
    (10, N'Telescope with 2 magnification levels (50x/100x). Includes tripod and star map. Suitable for ages 8+.', 1, 4, 3, 3, 680, 55, 12, 12),

    -- Ride-on / bicycle (11-12)
    (11, N'Donald Duck ride-on toy with LED lights and music. Maximum load 30 kg.', 5, 3, 3, 2, 2800, 55, 35, 42),
    (12, N'Disney Princess tricycle with steel frame. Includes push handle and sunshade. Suitable for ages 3-6.', 4, 3, 2, 2, 4500, 76, 43, 60),

    -- Balls (13-14)
    (13, N'Natural rubber ball size 5 with Boho patterns. Double-layer vulcanized design.', 5, 3, 3, 1, 430, 22, 22, 22),
    (14, N'FIFA Pro size 4 vulcanized rubber soccer ball. Waterproof design.', 5, 4, 1, 3, 390, 20, 20, 20),

    -- Kites (15-16)
    (15, N'Eagle kite with 1.4m wingspan, carbon frame, and 210T polyester fabric.', 2, 4, 1, 1, 280, 140, 10, 5),
    (16, N'3D dragon kite with 1.8 m wingspan and a carbon frame.', 2, 4, 1, 1, 350, 180, 12, 6),

    -- Plush toys (17-19)
    (17, N'Rex dinosaur plush toy 80 cm with soft velvet fabric and PP cotton stuffing.', 3, 2, 2, 2, 900, 80, 35, 40),
    (18, N'Panda plush toy 60 cm in a lying position. Velvet fabric with PP stuffing. Machine washable.', 3, 2, 3, 2, 680, 60, 30, 25),
    (19, N'Pastel long-eared bunny plush toy 45 cm. Soft velvet fabric. Includes gift box.', 3, 2, 2, 2, 480, 45, 20, 20),

    -- Barbie dolls (20-21)
    (20, N'Mattel Barbie Dreamtopia mermaid with purple-pink gradient hair and 3 outfits.', 1, 3, 2, 3, 280, 32, 10, 30),
    (21, N'Barbie Fashionista set with 6 fashionable outfits. Suitable for ages 3+.', 1, 3, 2, 3, 320, 33, 12, 30),

    -- Superhero figures (22-23)
    (22, N'Bandai Gao Red Ranger figure, 18 cm height with 24 articulation points.', 5, 5, 1, 4, 180, 12, 8, 18),
    (23, N'Kamen Rider Zero-One SHFiguarts figure 15 cm tall with 30 joints. Includes 8 interchangeable hands.', 5, 5, 1, 4, 160, 10, 8, 15),

    -- Dinosaurs (24-25)
    (24, N'T-Rex figure 1:10 scale, 25 cm height, 45 cm length. Aluminum alloy and ABS plastic. Spring-loaded jaw.', 4, 4, 1, 3, 520, 45, 18, 25),
    (25, N'Set of 6 mini dinosaur figures, height 8-12 cm. Soft ABS plastic.', 1, 3, 1, 3, 350, 30, 20, 8),

    -- RC cars (26-27)
    (26, N'RC Traxxas TRX-Mini 1:16 with a brushless 2838KV motor, max speed 45 km/h. LiPo battery included.', 4, 4, 1, 3, 680, 36, 22, 14),
    (27, N'RC drift car with CNC aluminum wheels. Speed up to 30 km/h. USB charging in 90 minutes.', 4, 4, 1, 2, 520, 34, 20, 12),

    -- RC helicopter (28)
    (28, N'4-channel 2.4GHz RC gyro helicopter with 6-axis stabilization. Flight time 12-15 minutes.', 1, 5, 1, 2, 280, 32, 32, 12),

    -- Clay / drawing board (29-30)
    (29, N'Hasbro Play-Doh set with 24 colors, each jar 85 g. Non-toxic and gluten-free.', 3, 3, 3, 3, 2040, 35, 24, 8),
    (30, N'10-inch LCD writing and drawing tablet with instant erase. CR2025 battery supports 50,000 uses.', 1, 2, 3, 2, 210, 28, 18, 1);
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 10 – PRODUCT IMAGES
══════════════════════════════════════════════════════════════ */
PRINT N'[10] ProductImages...';

IF NOT EXISTS (SELECT 1
FROM [dbo].[ProductImages]
WHERE ProductID = 1 AND IsMain = 1)
BEGIN
    DECLARE @i INT = 1;

    WHILE @i <= 30
    BEGIN
        DECLARE @seed VARCHAR(30) = 'toy' + CAST(@i AS VARCHAR);

        INSERT INTO [dbo].[ProductImages]
            (ProductID, ImageUrl, IsMain, CreatedAt)
        VALUES
            (@i, 'https://picsum.photos/seed/' + @seed + '-a/400/400', 1, GETDATE()),
            (@i, 'https://picsum.photos/seed/' + @seed + '-b/400/400', 0, GETDATE()),
            (@i, 'https://picsum.photos/seed/' + @seed + '-c/400/400', 0, GETDATE()),
            (@i, 'https://picsum.photos/seed/' + @seed + '-d/400/400', 0, GETDATE());

        SET @i = @i + 1;
    END
END
GO
/* ══════════════════════════════════════════════════════════════
   SECTION 13 – PROMOTIONS
══════════════════════════════════════════════════════════════ */
PRINT N'[13] Promotions...';

DECLARE @admID INT = (
    SELECT TOP 1
    AccountID
FROM Accounts
WHERE Email = 'admin@toyhouse.vn'
);
DECLARE @nowPromo DATETIME2(0) = GETDATE();

IF NOT EXISTS (SELECT 1
FROM [dbo].[Promotions]
WHERE PromotionName = N'Summer Toy Festival 2026')
    INSERT INTO [dbo].[Promotions]
    (CreatedBy, PromotionName, PromotionType, Description,
    StartDate, EndDate, Status, Priority, CreatedAt)
VALUES
    (@admID, N'Summer Toy Festival 2026', 'DISCOUNT',
        N'Massive summer discounts on selected toy collections.',
        @nowPromo, DATEADD(YEAR, 1, @nowPromo), 'Active', 10, @nowPromo),

    (@admID, N'Back To School 2026', 'DISCOUNT',
        N'Special discounts on educational toys and learning cards.',
        @nowPromo, DATEADD(YEAR, 1, @nowPromo), 'Active', 8, @nowPromo),

    (@admID, N'Mega Flash Sale', 'FLASH_SALE',
        N'Limited-time flash sale with discounts up to 50%.',
        @nowPromo, DATEADD(YEAR, 1, @nowPromo), 'Active', 20, @nowPromo),

    (@admID, N'Family Day Flash Sale', 'FLASH_SALE',
        N'Special offers celebrating Family Day.',
        @nowPromo, DATEADD(YEAR, 1, @nowPromo), 'Active', 15, @nowPromo),

    (@admID, N'Christmas & New Year 2026', 'DISCOUNT',
        N'Holiday season discounts up to 40% on figures and dolls.',
        @nowPromo, DATEADD(YEAR, 1, @nowPromo), 'Active', 12, @nowPromo),

    (@admID, N'Lego Builders Week', 'DISCOUNT',
        N'Exclusive discounts on all Lego sets and building blocks.',
        @nowPromo, DATEADD(YEAR, 1, @nowPromo), 'Active', 14, @nowPromo),

    (@admID, N'RC & Racing Expo', 'DISCOUNT',
        N'Save on remote-control cars, drift models, and helicopters.',
        @nowPromo, DATEADD(YEAR, 1, @nowPromo), 'Active', 11, @nowPromo),

    (@admID, N'Plush Toy Carnival', 'DISCOUNT',
        N'Soft toy specials on teddy bears, pandas, and bunny plushies.',
        @nowPromo, DATEADD(YEAR, 1, @nowPromo), 'Active', 9, @nowPromo),

    (@admID, N'Weekend Flash Blitz', 'FLASH_SALE',
        N'48-hour weekend blitz – deep cuts on bestsellers while stocks last.',
        @nowPromo, DATEADD(YEAR, 1, @nowPromo), 'Active', 18, @nowPromo),

    (@admID, N'Black Friday Preview', 'DISCOUNT',
        N'Early Black Friday deals across Lego, Barbie, and action figures.',
        @nowPromo, DATEADD(YEAR, 1, @nowPromo), 'Active', 16, @nowPromo),

    (@admID, N'Outdoor Play Month', 'DISCOUNT',
        N'Discounts on ride-on toys, balls, and kites for active kids.',
        @nowPromo, DATEADD(YEAR, 1, @nowPromo), 'Active', 7, @nowPromo),

    (@admID, N'Science & STEM Sale', 'DISCOUNT',
        N'Microscopes, telescopes, and experiment kits at reduced prices.',
        @nowPromo, DATEADD(YEAR, 1, @nowPromo), 'Active', 6, @nowPromo);
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 14 – PROMOTION TIME SLOTS
══════════════════════════════════════════════════════════════ */
PRINT N'[14] PromotionTimeSlots...';

IF NOT EXISTS (SELECT 1
FROM [dbo].[PromotionTimeSlots])
BEGIN
    DECLARE @nowSlot DATETIME2(0) = GETUTCDATE();

    DECLARE @pFlash1 INT = (
        SELECT TOP 1
        PromotionID
    FROM Promotions
    WHERE PromotionName = N'Mega Flash Sale'
    );

    DECLARE @pFlash2 INT = (
        SELECT TOP 1
        PromotionID
    FROM Promotions
    WHERE PromotionName = N'Family Day Flash Sale'
    );

    DECLARE @pFlash3 INT = (
        SELECT TOP 1
        PromotionID
    FROM Promotions
    WHERE PromotionName = N'Weekend Flash Blitz'
    );

    INSERT INTO [dbo].[PromotionTimeSlots]
        (PromotionID, StartAt, EndAt, Status, CreatedAt)
    VALUES
        -- Mega Flash Sale (4 slots)
        (@pFlash1, @nowSlot, DATEADD(HOUR, 3, @nowSlot), 'Active', @nowSlot),
        (@pFlash1, DATEADD(HOUR, 5, @nowSlot), DATEADD(HOUR, 8, @nowSlot), 'Scheduled', @nowSlot),
        (@pFlash1, DATEADD(DAY, 1, @nowSlot), DATEADD(DAY, 1, DATEADD(HOUR, 3, @nowSlot)), 'Scheduled', @nowSlot),
        (@pFlash1, DATEADD(DAY, 1, DATEADD(HOUR, 5, @nowSlot)), DATEADD(DAY, 1, DATEADD(HOUR, 8, @nowSlot)), 'Scheduled', @nowSlot),

        -- Family Day Flash Sale (3 slots)
        (@pFlash2, DATEADD(HOUR, 1, @nowSlot), DATEADD(HOUR, 4, @nowSlot), 'Active', @nowSlot),
        (@pFlash2, DATEADD(DAY, 1, DATEADD(HOUR, 1, @nowSlot)), DATEADD(DAY, 1, DATEADD(HOUR, 4, @nowSlot)), 'Scheduled', @nowSlot),
        (@pFlash2, DATEADD(DAY, 2, @nowSlot), DATEADD(DAY, 2, DATEADD(HOUR, 3, @nowSlot)), 'Scheduled', @nowSlot),

        -- Weekend Flash Blitz (3 slots)
        (@pFlash3, @nowSlot, DATEADD(HOUR, 2, @nowSlot), 'Active', @nowSlot),
        (@pFlash3, DATEADD(HOUR, 6, @nowSlot), DATEADD(HOUR, 9, @nowSlot), 'Scheduled', @nowSlot),
        (@pFlash3, DATEADD(DAY, 1, @nowSlot), DATEADD(DAY, 1, DATEADD(HOUR, 2, @nowSlot)), 'Scheduled', @nowSlot);
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 15 – PRODUCT PROMOTIONS (DISCOUNT)
══════════════════════════════════════════════════════════════ */
PRINT N'[15] ProductPromotions...';

IF NOT EXISTS (SELECT 1
FROM [dbo].[ProductPromotions])
BEGIN
    DECLARE @nowPP DATETIME2(0) = GETUTCDATE();

    DECLARE @pSale INT = (
        SELECT TOP 1
        PromotionID
    FROM Promotions
    WHERE PromotionName = N'Summer Toy Festival 2026'
    );

    DECLARE @pBTS INT = (
        SELECT TOP 1
        PromotionID
    FROM Promotions
    WHERE PromotionName = N'Back To School 2026'
    );

    DECLARE @pXmas INT = (
        SELECT TOP 1
        PromotionID
    FROM Promotions
    WHERE PromotionName = N'Christmas & New Year 2026'
    );

    DECLARE @pLego INT = (
        SELECT TOP 1
        PromotionID
    FROM Promotions
    WHERE PromotionName = N'Lego Builders Week'
    );

    DECLARE @pRC INT = (
        SELECT TOP 1
        PromotionID
    FROM Promotions
    WHERE PromotionName = N'RC & Racing Expo'
    );

    DECLARE @pPlush INT = (
        SELECT TOP 1
        PromotionID
    FROM Promotions
    WHERE PromotionName = N'Plush Toy Carnival'
    );

    DECLARE @pBF INT = (
        SELECT TOP 1
        PromotionID
    FROM Promotions
    WHERE PromotionName = N'Black Friday Preview'
    );

    DECLARE @pOutdoor INT = (
        SELECT TOP 1
        PromotionID
    FROM Promotions
    WHERE PromotionName = N'Outdoor Play Month'
    );

    DECLARE @pSTEM INT = (
        SELECT TOP 1
        PromotionID
    FROM Promotions
    WHERE PromotionName = N'Science & STEM Sale'
    );

    INSERT INTO [dbo].[ProductPromotions]
        (ProductID, PromotionID, SalePrice, DiscountPercent, CreatedAt)
    VALUES
        -- Summer Toy Festival
        ( 1, @pSale, 1099000, 14.88, @nowPP),
        ( 2, @pSale, 4990000, 16.69, @nowPP),
        ( 3, @pSale, 1290000, 18.87, @nowPP),
        ( 4, @pSale, 359000, 14.52, @nowPP),
        (11, @pSale, 449000, 17.61, @nowPP),
        (17, @pSale, 699000, 16.79, @nowPP),
        (20, @pSale, 299000, 16.71, @nowPP),
        (22, @pSale, 499000, 16.13, @nowPP),
        (26, @pSale, 799000, 15.34, @nowPP),

        -- Back To School
        ( 6, @pBTS, 69000, 18.82, @nowPP),
        ( 7, @pBTS, 149000, 18.92, @nowPP),
        ( 8, @pBTS, 299000, 20.27, @nowPP),
        ( 9, @pBTS, 224000, 20.00, @nowPP),
        (10, @pBTS, 399000, 18.57, @nowPP),
        (29, @pBTS, 70000, 20.45, @nowPP),
        (30, @pBTS, 139000, 20.57, @nowPP),

        -- Christmas & New Year (figures & dolls)
        (20, @pXmas, 269000, 25.07, @nowPP),
        (21, @pXmas, 369000, 24.69, @nowPP),
        (22, @pXmas, 449000, 24.54, @nowPP),
        (23, @pXmas, 669000, 24.83, @nowPP),
        (24, @pXmas, 299000, 24.30, @nowPP),
        (25, @pXmas, 214000, 24.91, @nowPP),

        -- Lego Builders Week
        ( 1, @pLego, 1150000, 10.85, @nowPP),
        ( 2, @pLego, 5390000, 10.02, @nowPP),
        ( 3, @pLego, 1420000, 10.69, @nowPP),
        ( 4, @pLego, 378000, 10.00, @nowPP),
        ( 5, @pLego, 315000, 10.00, @nowPP),

        -- RC & Racing Expo
        (26, @pRC, 799000, 15.34, @nowPP),
        (27, @pRC, 578000, 15.00, @nowPP),
        (28, @pRC, 1265000, 15.10, @nowPP),

        -- Plush Toy Carnival
        (17, @pPlush, 672000, 20.00, @nowPP),
        (18, @pPlush, 520000, 20.00, @nowPP),
        (19, @pPlush, 336000, 20.00, @nowPP),

        -- Black Friday Preview
        ( 1, @pBF, 999000, 22.56, @nowPP),
        ( 2, @pBF, 4490000, 25.04, @nowPP),
        (17, @pBF, 588000, 30.00, @nowPP),
        (20, @pBF, 251000, 30.08, @nowPP),
        (26, @pBF, 709000, 25.00, @nowPP),

        -- Outdoor Play Month
        (11, @pOutdoor, 490000, 10.09, @nowPP),
        (12, @pOutdoor, 799000, 10.22, @nowPP),
        (13, @pOutdoor, 76000, 10.59, @nowPP),
        (14, @pOutdoor, 148000, 10.30, @nowPP),
        (15, @pOutdoor, 103000, 10.43, @nowPP),
        (16, @pOutdoor, 166000, 10.27, @nowPP),

        -- Science & STEM Sale
        ( 8, @pSTEM, 319000, 14.93, @nowPP),
        ( 9, @pSTEM, 238000, 15.00, @nowPP),
        (10, @pSTEM, 416000, 15.10, @nowPP);
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 16 – PROMOTION PRODUCT SLOTS (FLASH_SALE)
══════════════════════════════════════════════════════════════ */
PRINT N'[16] PromotionProductSlots...';

IF NOT EXISTS (SELECT 1
FROM [dbo].[PromotionProductSlots])
BEGIN
    DECLARE @nowPPS DATETIME2(0) = GETUTCDATE();

    DECLARE @pFlash1Id INT = (
        SELECT TOP 1
        PromotionID
    FROM Promotions
    WHERE PromotionName = N'Mega Flash Sale'
    );

    DECLARE @pFlash2Id INT = (
        SELECT TOP 1
        PromotionID
    FROM Promotions
    WHERE PromotionName = N'Family Day Flash Sale'
    );

    DECLARE @pFlash3Id INT = (
        SELECT TOP 1
        PromotionID
    FROM Promotions
    WHERE PromotionName = N'Weekend Flash Blitz'
    );

    DECLARE @s1 INT = (SELECT TOP 1
        TimeSlotID
    FROM PromotionTimeSlots
    WHERE PromotionID = @pFlash1Id
    ORDER BY StartAt ASC);
    DECLARE @s2 INT = (SELECT
        TimeSlotID
    FROM PromotionTimeSlots
    WHERE PromotionID = @pFlash1Id
    ORDER BY StartAt ASC OFFSET 1 ROWS FETCH NEXT 1 ROWS ONLY);
    DECLARE @s3 INT = (SELECT
        TimeSlotID
    FROM PromotionTimeSlots
    WHERE PromotionID = @pFlash1Id
    ORDER BY StartAt ASC OFFSET 2 ROWS FETCH NEXT 1 ROWS ONLY);
    DECLARE @s4 INT = (SELECT
        TimeSlotID
    FROM PromotionTimeSlots
    WHERE PromotionID = @pFlash1Id
    ORDER BY StartAt ASC OFFSET 3 ROWS FETCH NEXT 1 ROWS ONLY);

    DECLARE @f1 INT = (SELECT TOP 1
        TimeSlotID
    FROM PromotionTimeSlots
    WHERE PromotionID = @pFlash2Id
    ORDER BY StartAt ASC);
    DECLARE @f2 INT = (SELECT
        TimeSlotID
    FROM PromotionTimeSlots
    WHERE PromotionID = @pFlash2Id
    ORDER BY StartAt ASC OFFSET 1 ROWS FETCH NEXT 1 ROWS ONLY);
    DECLARE @f3 INT = (SELECT
        TimeSlotID
    FROM PromotionTimeSlots
    WHERE PromotionID = @pFlash2Id
    ORDER BY StartAt ASC OFFSET 2 ROWS FETCH NEXT 1 ROWS ONLY);

    DECLARE @w1 INT = (SELECT TOP 1
        TimeSlotID
    FROM PromotionTimeSlots
    WHERE PromotionID = @pFlash3Id
    ORDER BY StartAt ASC);
    DECLARE @w2 INT = (SELECT
        TimeSlotID
    FROM PromotionTimeSlots
    WHERE PromotionID = @pFlash3Id
    ORDER BY StartAt ASC OFFSET 1 ROWS FETCH NEXT 1 ROWS ONLY);
    DECLARE @w3 INT = (SELECT
        TimeSlotID
    FROM PromotionTimeSlots
    WHERE PromotionID = @pFlash3Id
    ORDER BY StartAt ASC OFFSET 2 ROWS FETCH NEXT 1 ROWS ONLY);

    INSERT INTO [dbo].[PromotionProductSlots]
        (TimeSlotID, ProductID, SalePrice, DiscountPercent,
        SaleQuantity, SoldQuantity, ReservedQuantity, CreatedAt)
    VALUES
        -- Mega Flash Sale
        (@s1, 24, 197000, 50.13, 10, 0, 0, @nowPPS),
        (@s1, 13, 39000, 54.12, 50, 0, 0, @nowPPS),
        (@s1, 29, 44000, 50.00, 80, 0, 0, @nowPPS),
        (@s2, 17, 420000, 50.00, 12, 0, 0, @nowPPS),
        (@s2, 18, 325000, 50.00, 15, 0, 0, @nowPPS),
        (@s3, 1, 645000, 50.00, 20, 0, 0, @nowPPS),
        (@s3, 20, 179000, 50.14, 30, 0, 0, @nowPPS),
        (@s3, 25, 142000, 50.18, 40, 0, 0, @nowPPS),
        (@s4, 2, 2995000, 50.00, 5, 0, 0, @nowPPS),
        (@s4, 29, 44000, 50.00, 100, 0, 0, @nowPPS),
        (@s4, 28, 745000, 50.00, 6, 0, 0, @nowPPS),

        -- Family Day Flash Sale
        (@f1, 11, 272000, 50.09, 15, 0, 0, @nowPPS),
        (@f1, 19, 210000, 50.00, 25, 0, 0, @nowPPS),
        (@f2, 6, 72500, 50.00, 60, 0, 0, @nowPPS),
        (@f2, 7, 92500, 50.00, 45, 0, 0, @nowPPS),
        (@f3, 21, 245000, 50.00, 20, 0, 0, @nowPPS),
        (@f3, 22, 297000, 50.08, 12, 0, 0, @nowPPS),

        -- Weekend Flash Blitz
        (@w1, 4, 210000, 50.00, 30, 0, 0, @nowPPS),
        (@w1, 14, 82000, 50.30, 40, 0, 0, @nowPPS),
        (@w2, 26, 472000, 50.05, 10, 0, 0, @nowPPS),
        (@w2, 27, 340000, 50.00, 12, 0, 0, @nowPPS),
        (@w3, 23, 445000, 50.00, 8, 0, 0, @nowPPS),
        (@w3, 3, 795000, 50.00, 10, 0, 0, @nowPPS);
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 17 – VOUCHERS
══════════════════════════════════════════════════════════════ */
PRINT N'[17] Vouchers...';

IF NOT EXISTS (SELECT 1
FROM [dbo].[Vouchers]
WHERE VoucherCode = 'FREESHIP50')
BEGIN
    DECLARE @nowVoucher DATETIME2(0) = GETDATE();
    DECLARE @creatorID INT = (SELECT TOP 1 AccountID FROM Accounts WHERE Email = 'admin@toyhouse.vn');

    INSERT INTO [dbo].[Vouchers]
        (CreatedBy, VoucherCode, VoucherName, VoucherDescription,
        DiscountType, DiscountValue, MaxDiscountCap,
        DiscountTarget, MinOrderAmount,
        TotalQuantity, UsedQuantity, MaxUsagePerUser,
        StartDate, EndDate, Status, IsDeleted, CreatedAt)
    VALUES
        (
            @creatorID,
            'FREESHIP50',
            N'Free Shipping Up To 50K',
            N'Free shipping up to 50,000 VND for orders from 200,000 VND',
            'FIXED',
            50000,
            50000,
            'SHIPPING_FEE',
            200000,
            1000,
            0,
            1,
            @nowVoucher,
            DATEADD(YEAR, 1, @nowVoucher),
            'Active',
            0,
            @nowVoucher
    ),

        (
            @creatorID,
            'WELCOME15',
            N'15% New Customer Discount',
            N'15% discount for first purchase',
            'PERCENTAGE',
            15,
            250000,
            'ORDER_TOTAL',
            0,
            500,
            0,
            1,
            @nowVoucher,
            DATEADD(YEAR, 1, @nowVoucher),
            'Active',
            0,
            @nowVoucher
    ),

        (
            @creatorID,
            'VIP300K',
            N'VIP Customer 300K Discount',
            N'Applicable for orders from 1,500,000 VND',
            'FIXED',
            300000,
            NULL,
            'ORDER_TOTAL',
            1500000,
            100,
            0,
            1,
            @nowVoucher,
            DATEADD(YEAR, 1, @nowVoucher),
            'Active',
            0,
            @nowVoucher
    ),

        (
            @creatorID,
            'KIDSDAY50',
            N'Kids Day Gift 50K',
            N'50,000 VND discount for orders from 300,000 VND',
            'FIXED',
            50000,
            NULL,
            'ORDER_TOTAL',
            300000,
            1000,
            0,
            1,
            @nowVoucher,
            DATEADD(YEAR, 1, @nowVoucher),
            'Active',
            0,
            @nowVoucher
    ),

        (
            @creatorID,
            'SAVE100K',
            N'Save 100K on Big Orders',
            N'100,000 VND off when you spend 800,000 VND or more',
            'FIXED',
            100000,
            NULL,
            'ORDER_TOTAL',
            800000,
            500,
            12,
            1,
            @nowVoucher,
            DATEADD(YEAR, 1, @nowVoucher),
            'Active',
            0,
            @nowVoucher
    ),

        (
            @creatorID,
            'FLASH20',
            N'Flash 20% Off',
            N'20% discount on order total, capped at 500,000 VND',
            'PERCENTAGE',
            20,
            500000,
            'ORDER_TOTAL',
            150000,
            2000,
            45,
            2,
            @nowVoucher,
            DATEADD(YEAR, 1, @nowVoucher),
            'Active',
            0,
            @nowVoucher
    ),

        (
            @creatorID,
            'SHIPFREE100',
            N'Free Shipping 100K',
            N'Free shipping up to 100,000 VND for orders from 500,000 VND',
            'FIXED',
            100000,
            100000,
            'SHIPPING_FEE',
            500000,
            800,
            8,
            1,
            @nowVoucher,
            DATEADD(YEAR, 1, @nowVoucher),
            'Active',
            0,
            @nowVoucher
    ),

        (
            @creatorID,
            'FINAL10',
            N'Final Price 10% Off',
            N'10% off the final checkout price – no minimum order',
            'PERCENTAGE',
            10,
            200000,
            'FINAL_PRICE',
            0,
            NULL,
            0,
            3,
            @nowVoucher,
            DATEADD(YEAR, 1, @nowVoucher),
            'Active',
            0,
            @nowVoucher
    ),

        (
            @creatorID,
            'BULK5',
            N'Bulk Buyer 5%',
            N'5% off for repeat shoppers – up to 3 uses per account',
            'PERCENTAGE',
            5,
            150000,
            'ORDER_TOTAL',
            100000,
            NULL,
            0,
            3,
            @nowVoucher,
            DATEADD(YEAR, 1, @nowVoucher),
            'Active',
            0,
            @nowVoucher
    ),

        (
            @creatorID,
            'MEMBER200',
            N'Member Exclusive 200K',
            N'200,000 VND off for loyalty members on orders from 1,000,000 VND',
            'FIXED',
            200000,
            NULL,
            'ORDER_TOTAL',
            1000000,
            50,
            5,
            1,
            @nowVoucher,
            DATEADD(YEAR, 1, @nowVoucher),
            'Active',
            0,
            @nowVoucher
    ),

        (
            @creatorID,
            'SUMMER25',
            N'Summer Splash 25%',
            N'25% off summer collection, max discount 400,000 VND',
            'PERCENTAGE',
            25,
            400000,
            'ORDER_TOTAL',
            350000,
            1500,
            22,
            1,
            @nowVoucher,
            DATEADD(YEAR, 1, @nowVoucher),
            'Active',
            0,
            @nowVoucher
    ),

        (
            @creatorID,
            'NEWUSER50K',
            N'New User 50K Bonus',
            N'50,000 VND welcome bonus – 2 uses per new customer',
            'FIXED',
            50000,
            NULL,
            'ORDER_TOTAL',
            150000,
            300,
            18,
            2,
            @nowVoucher,
            DATEADD(YEAR, 1, @nowVoucher),
            'Active',
            0,
            @nowVoucher
    ),

        (
            @creatorID,
            'LEGOWEEK15',
            N'Lego Week 15% Off',
            N'15% off Lego and building sets, capped at 300,000 VND',
            'PERCENTAGE',
            15,
            300000,
            'ORDER_TOTAL',
            200000,
            600,
            31,
            1,
            @nowVoucher,
            DATEADD(YEAR, 1, @nowVoucher),
            'Active',
            0,
            @nowVoucher
    ),

        (
            @creatorID,
            'PLUSH30K',
            N'Plush Lovers 30K',
            N'30,000 VND off plush toy orders from 250,000 VND',
            'FIXED',
            30000,
            NULL,
            'ORDER_TOTAL',
            250000,
            800,
            0,
            2,
            @nowVoucher,
            DATEADD(YEAR, 1, @nowVoucher),
            'Active',
            0,
            @nowVoucher
    ),

        (
            @creatorID,
            'RC10PCT',
            N'RC Toys 10% Off',
            N'10% off remote-control cars and helicopters',
            'PERCENTAGE',
            10,
            350000,
            'FINAL_PRICE',
            400000,
            400,
            0,
            1,
            @nowVoucher,
            DATEADD(YEAR, 1, @nowVoucher),
            'Active',
            0,
            @nowVoucher
    ),

        (
            @creatorID,
            'BTS20K',
            N'Back To School 20K',
            N'20,000 VND off educational toys from 180,000 VND',
            'FIXED',
            20000,
            NULL,
            'ORDER_TOTAL',
            180000,
            2000,
            56,
            1,
            @nowVoucher,
            DATEADD(YEAR, 1, @nowVoucher),
            'Active',
            0,
            @nowVoucher
    ),

        (
            @creatorID,
            'MEGA500K',
            N'Mega Spender 500K',
            N'500,000 VND off orders from 3,000,000 VND – limited to 30 codes',
            'FIXED',
            500000,
            NULL,
            'ORDER_TOTAL',
            3000000,
            30,
            2,
            1,
            @nowVoucher,
            DATEADD(YEAR, 1, @nowVoucher),
            'Active',
            0,
            @nowVoucher
    ),

        (
            @creatorID,
            'SHIP30K',
            N'Shipping Saver 30K',
            N'30,000 VND shipping discount on any order from 120,000 VND',
            'FIXED',
            30000,
            30000,
            'SHIPPING_FEE',
            120000,
            NULL,
            0,
            5,
            @nowVoucher,
            DATEADD(YEAR, 1, @nowVoucher),
            'Active',
            0,
            @nowVoucher
    ),

        (
            @creatorID,
            'FINAL5',
            N'Checkout Bonus 5%',
            N'5% off final price for orders under 500,000 VND',
            'PERCENTAGE',
            5,
            50000,
            'FINAL_PRICE',
            0,
            5000,
            120,
            1,
            @nowVoucher,
            DATEADD(YEAR, 1, @nowVoucher),
            'Active',
            0,
            @nowVoucher
    ),

        (
            @creatorID,
            'TOYHOUSE10',
            N'ToyHouse 10% Storewide',
            N'10% off entire order, max 150,000 VND – no minimum spend',
            'PERCENTAGE',
            10,
            150000,
            'ORDER_TOTAL',
            0,
            NULL,
            0,
            1,
            @nowVoucher,
            DATEADD(YEAR, 1, @nowVoucher),
            'Active',
            0,
            @nowVoucher
    ),

        (
            @creatorID,
            'PENDING50K',
            N'Pending Approval 50K',
            N'Staff-submitted voucher awaiting admin approval',
            'FIXED',
            50000,
            NULL,
            'ORDER_TOTAL',
            200000,
            200,
            0,
            1,
            @nowVoucher,
            DATEADD(YEAR, 1, @nowVoucher),
            'Pending',
            0,
            @nowVoucher
    ),

        (
            @creatorID,
            'INACTIVE_OLD',
            N'Legacy Promo (Inactive)',
            N'Previously active promotion – deactivated for archive testing',
            'PERCENTAGE',
            12,
            100000,
            'ORDER_TOTAL',
            100000,
            100,
            100,
            1,
            DATEADD(MONTH, -2, @nowVoucher),
            DATEADD(MONTH, -1, @nowVoucher),
            'Inactive',
            0,
            DATEADD(MONTH, -2, @nowVoucher)
    );
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 17.1 – DYNAMIC DAILY SEEDING (VOUCHERS, PROMOTIONS, FLASH SALES)
   Generates 1-2 active vouchers, promotions, and flash sales for each day
   from today/June 4, 2026 up to September 30, 2026.
══════════════════════════════════════════════════════════════ */
PRINT N'[17.1] Dynamic Daily Seeding...';

DECLARE @startDate DATE = CAST(GETDATE() AS DATE);
IF @startDate > '2026-09-30'
    SET @startDate = '2026-06-04';
DECLARE @endDate DATE = '2026-09-30';

DECLARE @currentDate DATE = @startDate;
DECLARE @dayNum INT = 1;

DECLARE @creatorID INT = (
    SELECT TOP 1 AccountID FROM Accounts WHERE Email = 'admin@toyhouse.vn'
);

WHILE @currentDate <= @endDate
BEGIN
    DECLARE @dateStr VARCHAR(10) = CONVERT(VARCHAR(10), @currentDate, 120);
    DECLARE @dtStart DATETIME2(0) = CAST(@currentDate AS DATETIME2(0));
    -- End date is 2 days later, so it overlaps and ensures 2 active vouchers/promos on any day
    DECLARE @dtEnd DATETIME2(0) = DATEADD(SECOND, -1, DATEADD(DAY, 2, CAST(@currentDate AS DATETIME2(0))));

    -- -------------------------------------------------------------
    -- 1. DYNAMIC VOUCHERS (1 per day, 2 days duration)
    -- -------------------------------------------------------------
    DECLARE @vCode VARCHAR(30);
    DECLARE @vName NVARCHAR(255);
    DECLARE @vDesc NVARCHAR(255);
    DECLARE @vType VARCHAR(10);
    DECLARE @vVal DECIMAL(12,2);
    DECLARE @vCap DECIMAL(12,0);
    DECLARE @vTarget VARCHAR(20);
    DECLARE @vMinAmt DECIMAL(12,0);

    IF @dayNum % 3 = 1
    BEGIN
        SET @vCode = 'DAILY_TOTAL_' + REPLACE(@dateStr, '-', '');
        SET @vName = N'Daily Order Discount ' + CAST(@currentDate AS NVARCHAR);
        SET @vDesc = N'10% off order total up to 100K';
        SET @vType = 'PERCENTAGE';
        SET @vVal = 10.00;
        SET @vCap = 100000;
        SET @vTarget = 'ORDER_TOTAL';
        SET @vMinAmt = 200000;
    END
    ELSE IF @dayNum % 3 = 2
    BEGIN
        SET @vCode = 'DAILY_SHIP_' + REPLACE(@dateStr, '-', '');
        SET @vName = N'Daily Free Ship ' + CAST(@currentDate AS NVARCHAR);
        SET @vDesc = N'Free shipping up to 30K';
        SET @vType = 'FIXED';
        SET @vVal = 30000;
        SET @vCap = 30000;
        SET @vTarget = 'SHIPPING_FEE';
        SET @vMinAmt = 150000;
    END
    ELSE
    BEGIN
        SET @vCode = 'DAILY_CASH_' + REPLACE(@dateStr, '-', '');
        SET @vName = N'Daily Cash Back ' + CAST(@currentDate AS NVARCHAR);
        SET @vDesc = N'50K off for big orders';
        SET @vType = 'FIXED';
        SET @vVal = 50000;
        SET @vCap = NULL;
        SET @vTarget = 'FINAL_PRICE';
        SET @vMinAmt = 400000;
    END;

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Vouchers] WHERE VoucherCode = @vCode)
    BEGIN
        INSERT INTO [dbo].[Vouchers]
            (CreatedBy, VoucherCode, VoucherName, VoucherDescription,
             DiscountType, DiscountValue, MaxDiscountCap, DiscountTarget, MinOrderAmount,
             TotalQuantity, UsedQuantity, MaxUsagePerUser, StartDate, EndDate, Status, IsDeleted, CreatedAt)
        VALUES
            (@creatorID, @vCode, @vName, @vDesc,
             @vType, @vVal, @vCap, @vTarget, @vMinAmt,
             1000, 0, 1, @dtStart, @dtEnd, 'Active', 0, GETDATE());
    END;

    -- -------------------------------------------------------------
    -- 2. DYNAMIC PROMOTIONS (DISCOUNT) (1 per day, 2 days duration)
    -- -------------------------------------------------------------
    DECLARE @promoName NVARCHAR(200) = N'Daily Discount Promotion ' + CAST(@currentDate AS NVARCHAR);
    DECLARE @promoID INT;

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Promotions] WHERE PromotionName = @promoName)
    BEGIN
        INSERT INTO [dbo].[Promotions]
            (CreatedBy, PromotionName, PromotionType, Description, StartDate, EndDate, Status, Priority, IsDeleted, CreatedAt)
        VALUES
            (@creatorID, @promoName, 'DISCOUNT', N'Special daily discount catalog.', @dtStart, @dtEnd, 'Active', 10, 0, GETDATE());
        
        SET @promoID = SCOPE_IDENTITY();

        -- Link to 3 products (systematically rotating product IDs)
        DECLARE @p1 INT = (@dayNum % 30) + 1;
        DECLARE @p2 INT = ((@dayNum + 7) % 30) + 1;
        DECLARE @p3 INT = ((@dayNum + 13) % 30) + 1;

        -- Get product prices
        DECLARE @price1 DECIMAL(12,0) = (SELECT Price FROM Products WHERE ProductID = @p1);
        DECLARE @price2 DECIMAL(12,0) = (SELECT Price FROM Products WHERE ProductID = @p2);
        DECLARE @price3 DECIMAL(12,0) = (SELECT Price FROM Products WHERE ProductID = @p3);

        IF @price1 IS NOT NULL
            INSERT INTO [dbo].[ProductPromotions] (ProductID, PromotionID, SalePrice, DiscountPercent, IsDeleted, CreatedAt)
            VALUES (@p1, @promoID, @price1 * 0.85, 15.00, 0, GETUTCDATE());
        IF @price2 IS NOT NULL
            INSERT INTO [dbo].[ProductPromotions] (ProductID, PromotionID, SalePrice, DiscountPercent, IsDeleted, CreatedAt)
            VALUES (@p2, @promoID, @price2 * 0.85, 15.00, 0, GETUTCDATE());
        IF @price3 IS NOT NULL
            INSERT INTO [dbo].[ProductPromotions] (ProductID, PromotionID, SalePrice, DiscountPercent, IsDeleted, CreatedAt)
            VALUES (@p3, @promoID, @price3 * 0.85, 15.00, 0, GETUTCDATE());
    END;

    -- -------------------------------------------------------------
    -- 3. DYNAMIC FLASH SALES (1 promotion per day with 2 slots)
    -- -------------------------------------------------------------
    DECLARE @flashPromoName NVARCHAR(200) = N'Daily Flash Sale ' + CAST(@currentDate AS NVARCHAR);
    DECLARE @flashPromoID INT;

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Promotions] WHERE PromotionName = @flashPromoName)
    BEGIN
        INSERT INTO [dbo].[Promotions]
            (CreatedBy, PromotionName, PromotionType, Description, StartDate, EndDate, Status, Priority, IsDeleted, CreatedAt)
        VALUES
            (@creatorID, @flashPromoName, 'FLASH_SALE', N'Limited time daily flash sale.', @dtStart, @dtEnd, 'Active', 20, 0, GETDATE());

        SET @flashPromoID = SCOPE_IDENTITY();

        -- Create Slot 1: 09:00:00 to 13:00:00 (Active status)
        -- Create Slot 2: 15:00:00 to 21:00:00 (Scheduled status)
        -- Note: StartAt and EndAt are stored in UTC. We will use DATETIME2(0) values.
        DECLARE @slot1Start DATETIME2(0) = DATEADD(HOUR, 9, @dtStart);
        DECLARE @slot1End DATETIME2(0) = DATEADD(HOUR, 13, @dtStart);
        DECLARE @slot2Start DATETIME2(0) = DATEADD(HOUR, 15, @dtStart);
        DECLARE @slot2End DATETIME2(0) = DATEADD(HOUR, 21, @dtStart);

        INSERT INTO [dbo].[PromotionTimeSlots] (PromotionID, StartAt, EndAt, Status, IsDeleted, CreatedAt)
        VALUES 
            (@flashPromoID, @slot1Start, @slot1End, 'Active', 0, GETUTCDATE()),
            (@flashPromoID, @slot2Start, @slot2End, 'Scheduled', 0, GETUTCDATE());

        DECLARE @slot1ID INT;
        DECLARE @slot2ID INT;
        
        SET @slot1ID = (SELECT TimeSlotID FROM PromotionTimeSlots WHERE PromotionID = @flashPromoID AND StartAt = @slot1Start);
        SET @slot2ID = (SELECT TimeSlotID FROM PromotionTimeSlots WHERE PromotionID = @flashPromoID AND StartAt = @slot2Start);

        -- Attach products to Slot 1
        DECLARE @fp1 INT = ((@dayNum + 3) % 30) + 1;
        DECLARE @fp2 INT = ((@dayNum + 11) % 30) + 1;
        DECLARE @fprice1 DECIMAL(12,0) = (SELECT Price FROM Products WHERE ProductID = @fp1);
        DECLARE @fprice2 DECIMAL(12,0) = (SELECT Price FROM Products WHERE ProductID = @fp2);

        IF @slot1ID IS NOT NULL
        BEGIN
            IF @fprice1 IS NOT NULL
                INSERT INTO [dbo].[PromotionProductSlots] (TimeSlotID, ProductID, SalePrice, DiscountPercent, SaleQuantity, SoldQuantity, ReservedQuantity, IsDeleted, CreatedAt)
                VALUES (@slot1ID, @fp1, @fprice1 * 0.50, 50.00, 20, 0, 0, 0, GETUTCDATE());
            IF @fprice2 IS NOT NULL
                INSERT INTO [dbo].[PromotionProductSlots] (TimeSlotID, ProductID, SalePrice, DiscountPercent, SaleQuantity, SoldQuantity, ReservedQuantity, IsDeleted, CreatedAt)
                VALUES (@slot1ID, @fp2, @fprice2 * 0.50, 50.00, 20, 0, 0, 0, GETUTCDATE());
        END;

        -- Attach products to Slot 2
        DECLARE @fp3 INT = ((@dayNum + 17) % 30) + 1;
        DECLARE @fp4 INT = ((@dayNum + 23) % 30) + 1;
        DECLARE @fprice3 DECIMAL(12,0) = (SELECT Price FROM Products WHERE ProductID = @fp3);
        DECLARE @fprice4 DECIMAL(12,0) = (SELECT Price FROM Products WHERE ProductID = @fp4);

        IF @slot2ID IS NOT NULL
        BEGIN
            IF @fprice3 IS NOT NULL
                INSERT INTO [dbo].[PromotionProductSlots] (TimeSlotID, ProductID, SalePrice, DiscountPercent, SaleQuantity, SoldQuantity, ReservedQuantity, IsDeleted, CreatedAt)
                VALUES (@slot2ID, @fp3, @fprice3 * 0.50, 50.00, 20, 0, 0, 0, GETUTCDATE());
            IF @fprice4 IS NOT NULL
                INSERT INTO [dbo].[PromotionProductSlots] (TimeSlotID, ProductID, SalePrice, DiscountPercent, SaleQuantity, SoldQuantity, ReservedQuantity, IsDeleted, CreatedAt)
                VALUES (@slot2ID, @fp4, @fprice4 * 0.50, 50.00, 20, 0, 0, 0, GETUTCDATE());
        END;
    END;

    SET @currentDate = DATEADD(DAY, 1, @currentDate);
    SET @dayNum = @dayNum + 1;
END;
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 27 – ORDER REFUND REASONS
══════════════════════════════════════════════════════════════ */
PRINT N'[27] OrderRefundReasons...';

IF NOT EXISTS (SELECT 1
FROM [dbo].[OrderRefundReasons])
BEGIN
    INSERT INTO [dbo].[OrderRefundReasons]
        (Content, Description, IsDeleted, IsSystem, CreatedAt)
    VALUES
        (
            N'Product defective from manufacturer',
            N'The delivered product has technical defects',
            0, 0, '2024-01-05 08:00:00'
    ),
        (
            N'Wrong product delivered',
            N'The shop sent the wrong color, size, or model',
            0, 0, '2024-01-05 08:00:00'
    ),
        (
            N'Product not as described',
            N'The actual product does not match the images or description',
            0, 0, '2024-01-05 08:00:00'
    ),
        (
            N'Product damaged during shipping',
            N'The package was dented or broken during delivery',
            0, 0, '2024-01-05 08:00:00'
    ),
        (
            N'Missing accessories',
            N'The package does not include all accessories as described',
            0, 0, '2024-01-05 08:00:00'
    ),
        (
            N'Delivery failed / unable to deliver',
            N'Automatic refund when GHN returns the package to warehouse due to failed delivery (System-only)',
            0, 1, GETDATE()
    );
END
GO


GO
/* ══════════════════════════════════════════════════════════════
   SECTION 28 – DELIVERED ORDERS (for ReviewProducts seeding)
   5 customers × 2 orders each = 10 delivered orders
   Each order has 2 products from the catalog
══════════════════════════════════════════════════════════════ */
PRINT N'[28] Delivered Orders & OrderDetails...';

IF NOT EXISTS (SELECT 1 FROM [dbo].[Orders])
BEGIN
    DECLARE @cust1 INT = (SELECT TOP 1 AccountID FROM Accounts WHERE Email = 'lananh.pham@gmail.com');
    DECLARE @cust2 INT = (SELECT TOP 1 AccountID FROM Accounts WHERE Email = 'hung.nguyen88@gmail.com');
    DECLARE @cust3 INT = (SELECT TOP 1 AccountID FROM Accounts WHERE Email = 'bauchau.vu@gmail.com');
    DECLARE @cust4 INT = (SELECT TOP 1 AccountID FROM Accounts WHERE Email = 'mtuando@outlook.com');
    DECLARE @cust5 INT = (SELECT TOP 1 AccountID FROM Accounts WHERE Email = 'thuha.hoang@gmail.com');

    -- StatusID 6 = Delivered, StatusID 7 = Completed
    SET IDENTITY_INSERT [dbo].[Orders] ON;
    INSERT INTO [dbo].[Orders]
        (OrderID, AccountID, StatusID, OrderCode,
         ShippingName, ShippingPhone, ShippingAddress,
         ShippingWardCode, ShippingWardName, ShippingDistrictId, ShippingDistrictName, ShippingProvinceId, ShippingProvinceName,
         OrderDate, ConfirmedAt, ShippedAt, DeliveredAt,
         PaymentMethod, PaymentStatus, PaidAt,
         SubTotal, VoucherDiscountAmount, EstimatedShippingFee, TotalAmount, IsDeleted, CreatedAt)
    VALUES
        (1, @cust1, 6,'ORD20260115090000101',N'Anna Pham','0912001001',N'12 Nguyễn Huệ','20101',N'Phường Bến Nghé',1442,N'Quận 1',202,N'Hồ Chí Minh','2026-01-15 09:00:00','2026-01-15 11:00:00','2026-01-16 08:00:00','2026-01-17 14:00:00','SHIP_COD','PAID','2026-01-17 14:00:00',1710000,0,30000,1740000,0,'2026-01-15 09:00:00'),
        (2, @cust1, 7,'ORD20260310100000234',N'Anna Pham','0912001001',N'12 Nguyễn Huệ','20101',N'Phường Bến Nghé',1442,N'Quận 1',202,N'Hồ Chí Minh','2026-03-10 10:00:00','2026-03-10 12:00:00','2026-03-11 09:00:00','2026-03-12 15:00:00','SHIP_COD','PAID','2026-03-12 15:00:00',735000,0,30000,765000,0,'2026-03-10 10:00:00'),
        (3, @cust2, 6,'ORD20260120083000312',N'Henry Nguyen','0912001002',N'45 Lê Lợi','20301',N'Phường 1',1444,N'Quận 3',202,N'Hồ Chí Minh','2026-01-20 08:30:00','2026-01-20 10:30:00','2026-01-21 09:00:00','2026-01-22 13:00:00','SHIP_COD','PAID','2026-01-22 13:00:00',6395000,0,30000,6425000,0,'2026-01-20 08:30:00'),
        (4, @cust2, 7,'ORD20260214110000445',N'Henry Nguyen','0912001002',N'45 Lê Lợi','20301',N'Phường 1',1444,N'Quận 3',202,N'Hồ Chí Minh','2026-02-14 11:00:00','2026-02-14 13:00:00','2026-02-15 09:00:00','2026-02-16 14:30:00','SHIP_COD','PAID','2026-02-16 14:30:00',630000,0,30000,660000,0,'2026-02-14 11:00:00'),
        (5, @cust3, 6,'ORD20260201090000523',N'Chloe Vu','0912001003',N'88 Trần Hưng Đạo','20501',N'Phường 1',1447,N'Quận 5',202,N'Hồ Chí Minh','2026-02-01 09:00:00','2026-02-01 11:00:00','2026-02-02 08:30:00','2026-02-03 14:00:00','SHIP_COD','PAID','2026-02-03 14:00:00',1640000,0,30000,1670000,0,'2026-02-01 09:00:00'),
        (6, @cust3, 7,'ORD20260320140000167',N'Chloe Vu','0912001003',N'88 Trần Hưng Đạo','20501',N'Phường 1',1447,N'Quận 5',202,N'Hồ Chí Minh','2026-03-20 14:00:00','2026-03-20 16:00:00','2026-03-21 09:00:00','2026-03-22 14:00:00','SHIP_COD','PAID','2026-03-22 14:00:00',1080000,0,30000,1110000,0,'2026-03-20 14:00:00'),
        (7, @cust4, 6,'ORD20260210100000289',N'Marcus Do','0912001004',N'99 Điện Biên Phủ','21601',N'Phường 1',1462,N'Quận Bình Thạnh',202,N'Hồ Chí Minh','2026-02-10 10:00:00','2026-02-10 12:00:00','2026-02-11 09:00:00','2026-02-12 15:00:00','SHIP_COD','PAID','2026-02-12 15:00:00',870000,0,30000,900000,0,'2026-02-10 10:00:00'),
        (8, @cust4, 7,'ORD20260405093000034',N'Marcus Do','0912001004',N'99 Điện Biên Phủ','21601',N'Phường 1',1462,N'Quận Bình Thạnh',202,N'Hồ Chí Minh','2026-04-05 09:30:00','2026-04-05 11:30:00','2026-04-06 09:00:00','2026-04-07 14:00:00','SHIP_COD','PAID','2026-04-07 14:00:00',980000,0,30000,1010000,0,'2026-04-05 09:30:00'),
        (9, @cust5, 6,'ORD20260301080000751',N'Hannah Hoang','0912001005',N'22 Hoàng Văn Thụ','21701',N'Phường 1',1457,N'Quận Phú Nhuận',202,N'Hồ Chí Minh','2026-03-01 08:00:00','2026-03-01 10:00:00','2026-03-02 09:00:00','2026-03-03 14:00:00','SHIP_COD','PAID','2026-03-03 14:00:00',1290000,0,30000,1320000,0,'2026-03-01 08:00:00'),
        (10,@cust5, 7,'ORD20260415100000418',N'Hannah Hoang','0912001005',N'22 Hoàng Văn Thụ','21701',N'Phường 1',1457,N'Quận Phú Nhuận',202,N'Hồ Chí Minh','2026-04-15 10:00:00','2026-04-15 12:00:00','2026-04-16 09:00:00','2026-04-17 15:00:00','SHIP_COD','PAID','2026-04-17 15:00:00',420000,0,30000,450000,0,'2026-04-15 10:00:00');
    SET IDENTITY_INSERT [dbo].[Orders] OFF;

    -- OrderDetails: each order has 2 products
    INSERT INTO [dbo].[OrderDetails]
        (OrderID, ProductID, ProductName, ProductImage, Quantity, UnitPrice, DiscountAmount, CreatedAt)
    SELECT o.OrderID, p.ProductID, p.ProductName,
        'https://picsum.photos/seed/prod-' + CAST(p.ProductID AS VARCHAR) + '/300/300',
        1, p.Price, 0, o.CreatedAt
    FROM (VALUES
        (1,1),(1,3),
        (2,6),(2,7),
        (3,2),(3,1),
        (4,4),(4,9),
        (5,3),(5,10),
        (6,8),(6,1),
        (7,5),(7,9),
        (8,10),(8,6),
        (9,1),(9,7),
        (10,4),(10,5)
    ) AS v(OID, PID)
    JOIN Orders o ON o.OrderID = v.OID
    JOIN Products p ON p.ProductID = v.PID;
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 28.1 – DYNAMIC PRODUCT REVIEWS
   Seeds ReviewProducts for all delivered/completed orders,
   plus ReviewProductImages, StaffReviewProductReplies,
   ReviewProductReactions, and ReviewModerationLogs.
══════════════════════════════════════════════════════════════ */
PRINT N'[28.1] Dynamic Product Reviews...';

IF NOT EXISTS (SELECT 1 FROM [dbo].[ReviewProducts])
BEGIN
    DECLARE @revStaff1 INT = (SELECT TOP 1 AccountID FROM Accounts WHERE Email = 'nhung.st@toyhouse.vn');
    DECLARE @revAdmin  INT = (SELECT TOP 1 AccountID FROM Accounts WHERE Email = 'admin@toyhouse.vn');

    -- Insert reviews: one per (customer, product, order)
    -- Comment templates keyed on (OrderID % 5)
    INSERT INTO [dbo].[ReviewProducts]
        (AccountID, ProductID, OrderID, Rating, Comment, ModerationStatus, IsDeleted, IsEdited, CreatedAt)
    SELECT
        o.AccountID,
        od.ProductID,
        od.OrderID,
        -- Rating: 4 or 5 stars, varied by order+product
        CAST(4 + (od.ProductID % 2) AS TINYINT),
        CASE (od.OrderID % 5)
            WHEN 0 THEN N'Excellent product! My child plays with it every day. Well worth the price.'
            WHEN 1 THEN N'Great quality, fast shipping. Packaged securely. Very happy with the purchase!'
            WHEN 2 THEN N'Good toy overall. Instructions were a bit unclear but the product itself is solid.'
            WHEN 3 THEN N'My kid loves this! Bought it as a birthday gift and it was a huge hit.'
            ELSE       N'Nice product, true to the description. Will definitely buy again.'
        END,
        'Approved',
        0, 0,
        DATEADD(DAY, 2, o.DeliveredAt)
    FROM [dbo].[Orders] o
    JOIN [dbo].[OrderDetails] od ON od.OrderID = o.OrderID
    WHERE o.StatusID IN (6, 7);   -- Delivered or Completed

    -- ReviewProductImages: attach 1 image per review (50% of reviews)
    INSERT INTO [dbo].[ReviewProductImages]
        (ReviewProductID, ImageURL, ModerationStatus, IsDeleted, CreatedAt)
    SELECT
        rp.ReviewID,
        'https://picsum.photos/seed/rv-' + CAST(rp.ReviewID AS VARCHAR) + '/400/400',
        'Approved',
        0,
        DATEADD(MINUTE, 5, rp.CreatedAt)
    FROM [dbo].[ReviewProducts] rp
    WHERE rp.ReviewID % 2 = 1;  -- odd IDs only → ~50%

    -- StaffReviewProductReplies: staff replies to ~half the reviews
    INSERT INTO [dbo].[StaffReviewProductReplies]
        (ReviewProductID, StaffID, Content, IsDeleted, CreatedAt)
    SELECT
        rp.ReviewID,
        @revStaff1,
        N'Thank you for your feedback! We are glad you enjoyed the product. Please contact us if you need any support.',
        0,
        DATEADD(HOUR, 3, rp.CreatedAt)
    FROM [dbo].[ReviewProducts] rp
    WHERE rp.ReviewID % 2 = 0;  -- even IDs → ~50%

    -- ReviewProductReactions: admin likes each review
    INSERT INTO [dbo].[ReviewProductReactions]
        (ReviewProductID, AccountID, ReactionTypeID, IsDeleted, CreatedAt)
    SELECT
        rp.ReviewID,
        @revAdmin,
        1,  -- ReactionTypeID 1 = Like/Helpful
        0,
        DATEADD(HOUR, 1, rp.CreatedAt)
    FROM [dbo].[ReviewProducts] rp;

    -- ReviewModerationLogs: AI auto-approved all reviews
    INSERT INTO [dbo].[ReviewModerationLogs]
        (TargetType, ReviewID, ImageID, ModeratorType, ModeratedBy,
         Action, AIModelVersion, ModerationResult, Reason, CreatedAt)
    SELECT
        'Text',
        rp.ReviewID,
        NULL,
        'AI',
        NULL,
        'Approved',
        'claude-moderation-v1.0',
        N'{"score":0.01,"categories":{"hate":false,"harassment":false,"spam":false},"passed":true}',
        NULL,
        DATEADD(MINUTE, 2, rp.CreatedAt)
    FROM [dbo].[ReviewProducts] rp;

    -- ReviewModerationLogs for images
    INSERT INTO [dbo].[ReviewModerationLogs]
        (TargetType, ReviewID, ImageID, ModeratorType, ModeratedBy,
         Action, AIModelVersion, ModerationResult, Reason, CreatedAt)
    SELECT
        'Image',
        rpi.ReviewProductID,
        rpi.ReviewProductImageID,
        'AI',
        NULL,
        'Approved',
        'claude-vision-moderation-v1.0',
        N'{"score":0.01,"nudity":false,"violence":false,"passed":true}',
        NULL,
        DATEADD(MINUTE, 3, rpi.CreatedAt)
    FROM [dbo].[ReviewProductImages] rpi;
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 28.2 – ORDER REFUNDS & DETAILS [NEW]
══════════════════════════════════════════════════════════════ */
PRINT N'[28.2-NEW] Order Refunds & Details...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderRefunds])
BEGIN
    DECLARE @refCust3 INT = (
        SELECT TOP 1 AccountID FROM Accounts WHERE Email = 'bauchau.vu@gmail.com'
    );

    DECLARE @refR1 INT = (
        SELECT TOP 1 RefundReasonID FROM OrderRefundReasons WHERE Content = N'Product defective from manufacturer'
    );

    DECLARE @ord3 INT = (
        SELECT TOP 1 OrderID FROM Orders WHERE OrderCode = 'ORD20260201090000523'
    );

    IF @ord3 IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[OrderRefunds]
            (OrderID, RefundReasonID, CustomerID, RequestedBy,
             RefundCode, SubTotal, TotalAmount, ApprovedAmount,
             StatusID, CreatedAt)
        VALUES
            (
                @ord3,
                @refR1,
                @refCust3,
                @refCust3,
                'REF-20260604-47FD81D6',
                490000,
                490000,
                490000,
                1,
                '2026-03-07 09:00:00'
            );

        INSERT INTO [dbo].[RefundDetails]
            (RefundID, ProductID, Quantity, UnitPrice, RefundAmount, CreatedAt)
        SELECT
            RefundID,
            10,
            1,
            490000,
            490000,
            '2026-03-07 09:00:00'
        FROM OrderRefunds;

        INSERT INTO [dbo].[RefundImages]
            (RefundID, ImageURL, CreatedAt)
        SELECT
            RefundID,
            'https://picsum.photos/seed/refund-' + CAST(RefundID AS VARCHAR) + '-a/400/400',
            GETDATE()
        FROM OrderRefunds;
    END
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 31 – BLOG
   NOTE: BlogPostStats is auto-created by trigger
         trg_BlogPost_InitStats on INSERT BlogPosts – no manual seed needed
══════════════════════════════════════════════════════════════ */
PRINT N'[31] Blog...';

SET IDENTITY_INSERT [dbo].[BlogCategories] ON;

IF NOT EXISTS (SELECT 1
FROM [dbo].[BlogCategories]
WHERE BlogCategoryID = 1)
    INSERT INTO [dbo].[BlogCategories]
    (BlogCategoryID, BlogCategoriesName, CreatedAt)
VALUES
    (1, N'News & Promotions', '2024-01-10 08:00:00'),
    (2, N'Parenting Knowledge', '2024-01-10 08:00:00'),
    (3, N'Product Reviews', '2024-01-10 08:00:00'),
    (4, N'Play & Creativity', '2024-01-10 08:00:00'),
    (5, N'Toy Safety', '2024-01-10 08:00:00');

SET IDENTITY_INSERT [dbo].[BlogCategories] OFF;
GO

DECLARE @bst1 INT=(SELECT TOP 1
    AccountID
FROM Accounts
WHERE Email='nhung.st@toyhouse.vn');
DECLARE @bst2 INT=(SELECT TOP 1
    AccountID
FROM Accounts
WHERE Email='dung.st@toyhouse.vn');
DECLARE @badm INT=(SELECT TOP 1
    AccountID
FROM Accounts
WHERE Email='admin@toyhouse.vn');

IF NOT EXISTS (SELECT 1
FROM [dbo].[BlogPosts])
    INSERT INTO [dbo].[BlogPosts]
    (AccountID,ApprovedBy,BlogCategoryID,BlogTitle,BlogContent,BlogThumbnail,Status,IsFeatured,BlogAt,CreatedAt)
VALUES
    (@bst1, @badm, 1, N'Summer Sale – Up To 50% Off Official Toys',
        N'The Summer Sale 2026 event starts now! Lego, Bandai, and Fisher-Price are discounted up to 50%.',
        'https://picsum.photos/seed/blog-sale-he/600/400', 'Published', 1, '2026-03-28 09:00:00', '2026-03-28 09:00:00'),
    (@bst1, @badm, 2, N'7 Golden Rules for Choosing Safe Toys for Children Under 3',
        N'Ages 0–3 are a critical development period. Look for BPA-free materials, rounded edges, and non-toxic water-based paint.',
        'https://picsum.photos/seed/blog-safety/600/400', 'Published', 0, '2026-02-10 10:00:00', '2026-02-10 10:00:00'),
    (@bst2, @badm, 3, N'Hands-On Review: Lego City Police Station After 3 Months',
        N'This 668-piece set holds up well after 3 months. Bricks stay firm and do not break. A 7-year-old can assemble about 80% independently.',
        'https://picsum.photos/seed/blog-review-lego/600/400', 'Published', 1, '2026-03-05 14:00:00', '2026-03-05 14:00:00'),
    (@bst2, @badm, 4, N'Top 10 Active Toys That Build Physical Skills for Ages 3–6',
        N'Active play supports both health and cognitive growth. Here are the 10 most popular movement toys for preschoolers.',
        'https://picsum.photos/seed/blog-activity/600/400', 'Published', 1, '2026-04-01 10:00:00', '2026-04-01 10:00:00'),
    (@bst1, @badm, 1, N'48-Hour Flash Sale – Mega Deals Starting Now',
        N'Only 48 hours! Flash Sale with up to 50% off hundreds of products. Limited stock available!',
        'https://picsum.photos/seed/blog-flash55/600/400', 'Published', 0, GETDATE(), GETDATE());
GO

IF NOT EXISTS (SELECT 1
FROM [dbo].[ReviewBlogs])
BEGIN
    ;WITH
        BlogRanked
        AS
        (
            SELECT BlogPostID, ROW_NUMBER() OVER (ORDER BY BlogPostID) AS Pos
            FROM [dbo].[BlogPosts]
        )
    INSERT INTO [dbo].[ReviewBlogs]
        (BlogPostID, AccountID, Comment, CreatedAt)
    SELECT br.BlogPostID, a.AccountID, v.Comment, v.CreatedAt
    FROM (VALUES
            (1, 'lananh.pham@gmail.com', N'I bought the Lego City set during this sale — 15% off was a great deal! My kid loves it.', '2026-03-29 10:00:00'),
            (1, 'hung.nguyen88@gmail.com', N'Big sale! Just ordered the RC Traxxas and saved a lot.', '2026-03-30 08:00:00'),
            (1, 'bauchau.vu@gmail.com', N'Can I combine the VIP300K voucher with the summer sale?', '2026-03-30 09:30:00'),
            (1, 'mtuando@outlook.com', N'Super fast delivery — ordered in the morning and received by evening. Carefully packed, 5 stars!', '2026-03-31 14:00:00'),
            (1, 'thuha.hoang@gmail.com', N'Bought Barbie Dreamtopia for my daughter — she absolutely loves it. Thanks ToyHouse!', '2026-04-01 09:00:00'),
            (2, 'lananh.pham@gmail.com', N'Very helpful article! I used to buy toys purely on instinct.', '2026-02-11 10:00:00'),
            (2, 'thuha.hoang@gmail.com', N'Thanks for sharing! I forwarded this to my parent group.', '2026-02-12 11:00:00'),
            (3, 'hung.nguyen88@gmail.com', N'Detailed review! I am considering this set for my 8-year-old.', '2026-03-06 11:00:00'),
            (3, 'mtuando@outlook.com', N'I bought it too and agree it is worth it. My 7-year-old assembled about 70% alone.', '2026-03-08 10:00:00'),
            (4, 'lananh.pham@gmail.com', N'We got the Donald Duck ride-on for our 2-year-old — she climbed on and started pedaling right away!', '2026-04-02 09:00:00'),
            (4, 'bauchau.vu@gmail.com', N'Great article! Looking for a birthday gift for my 4-year-old boy.', '2026-04-04 11:00:00'),
            (5, 'hung.nguyen88@gmail.com', N'Set a reminder for the flash sale — cannot miss this one!', '2026-04-21 10:00:00'),
            (5, 'mtuando@outlook.com', N'50% off for real? Bandai figures at half price is an amazing deal!', '2026-04-22 08:00:00')
    ) AS v(BpPos, Email, Comment, CreatedAt)
        JOIN BlogRanked br ON br.Pos = v.BpPos
        JOIN Accounts a ON a.Email = v.Email;

    DECLARE @staffRep INT=(SELECT TOP 1
        AccountID
    FROM Accounts
    WHERE Email='nhung.st@toyhouse.vn');
    INSERT INTO [dbo].[ReviewBlogReplies]
        (ReviewBlogID,AccountID,Comment,CreatedAt)
    SELECT rb.ReviewBlogID, @staffRep,
        N'Thank you for shopping with ToyHouse! Wishing your child joy and healthy development!',
        DATEADD(HOUR,1,rb.CreatedAt)
    FROM ReviewBlogs rb;

    DECLARE @badm2 INT=(SELECT TOP 1
        AccountID
    FROM Accounts
    WHERE Email='admin@toyhouse.vn');
    INSERT INTO [dbo].[ReviewBlogReactions]
        (ReviewBlogID,AccountID,ReactionTypeID,CreatedAt)
    SELECT rb.ReviewBlogID, @badm2, 1, DATEADD(MINUTE,30,rb.CreatedAt)
    FROM ReviewBlogs rb
    WHERE NOT EXISTS (
        SELECT 1
    FROM ReviewBlogReactions r
    WHERE r.ReviewBlogID=rb.ReviewBlogID AND r.AccountID=@badm2);

    INSERT INTO [dbo].[BlogPostReactions]
        (BlogPostID,AccountID,ReactionTypeID,CreatedAt)
    SELECT b.BlogPostID, a.AccountID, v.ReactionTypeID, GETDATE()
    FROM BlogPosts b
    CROSS JOIN (VALUES
            ('lananh.pham@gmail.com', 1),
            ('hung.nguyen88@gmail.com', 2),
            ('bauchau.vu@gmail.com', 1),
            ('thuha.hoang@gmail.com', 1),
            ('mtuando@outlook.com', 2)
    ) AS v(Email,ReactionTypeID)
        JOIN Accounts a ON a.Email=v.Email
    WHERE b.Status='Published'
        AND NOT EXISTS (
          SELECT 1
        FROM BlogPostReactions bpr
        WHERE bpr.BlogPostID=b.BlogPostID AND bpr.AccountID=a.AccountID);
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 31.1 – DYNAMIC BLOGS AND COMMENTS
   Generates realistic blog posts and comments (reviews/reactions)
   from today/June 4, 2026 up to September 30, 2026.
══════════════════════════════════════════════════════════════ */
PRINT N'[31.1] Dynamic Blogs and Comments Seeding...';

DECLARE @bStartDate DATE = CAST(GETDATE() AS DATE);
IF @bStartDate > '2026-09-30'
    SET @bStartDate = '2026-06-04';
DECLARE @bEndDate DATE = '2026-09-30';

DECLARE @bCurrentDate DATE = @bStartDate;
DECLARE @blogNum INT = 1;

DECLARE @staff1 INT = (SELECT TOP 1 AccountID FROM Accounts WHERE Email = 'nhung.st@toyhouse.vn');
DECLARE @staff2 INT = (SELECT TOP 1 AccountID FROM Accounts WHERE Email = 'dung.st@toyhouse.vn');
DECLARE @adminId INT = (SELECT TOP 1 AccountID FROM Accounts WHERE Email = 'admin@toyhouse.vn');

-- Get the customer accounts into a table variable for easy indexing
DECLARE @Customers TABLE (
    Idx INT IDENTITY(1,1) PRIMARY KEY,
    AccID INT NOT NULL,
    Email VARCHAR(100) NOT NULL
);
INSERT INTO @Customers (AccID, Email)
SELECT AccountID, Email FROM Accounts WHERE Email IN (
    'lananh.pham@gmail.com', 'hung.nguyen88@gmail.com', 'bauchau.vu@gmail.com', 'mtuando@outlook.com', 'thuha.hoang@gmail.com'
) ORDER BY Email;

WHILE @bCurrentDate <= @bEndDate
BEGIN
    -- We insert 1 blog post every 8 days (approx. 15 posts total)
    IF @blogNum % 8 = 1
    BEGIN
        DECLARE @catID INT = (@blogNum % 5) + 1;
        DECLARE @title NVARCHAR(255);
        DECLARE @content NVARCHAR(MAX);
        DECLARE @thumb VARCHAR(255) = 'https://picsum.photos/seed/blog-' + CAST(@blogNum AS VARCHAR) + '/600/400';
        DECLARE @authorID INT = CASE WHEN @blogNum % 2 = 1 THEN @staff1 ELSE @staff2 END;
        DECLARE @bDate DATETIME2(0) = CAST(@bCurrentDate AS DATETIME2(0));

        IF @catID = 1
        BEGIN
            SET @title = N'Special Promotions & New Arrivals for ' + CONVERT(NVARCHAR(30), @bCurrentDate, 107);
            SET @content = N'We are excited to announce our new stock of wooden toys, board games, and lego sets for this week. Visit our local store or buy online to get up to 10% off your purchase!';
        END
        ELSE IF @catID = 2
        BEGIN
            SET @title = N'Supporting Your Child''s Mental Development through Play - Vol. ' + CAST(@blogNum AS NVARCHAR);
            SET @content = N'Children learn best when they are actively playing. Sensory toys, blocks, and roleplay toys are perfect tools to build spatial reasoning, language, and early motor skills.';
        END
        ELSE IF @catID = 3
        BEGIN
            SET @title = N'Honest Product Review: Creative Play sets - ' + CONVERT(NVARCHAR(30), @bCurrentDate, 107);
            SET @content = N'This week we review the newest creative coloring and modeling clay kits. Safe for kids, non-sticky, and easy to wash off. Rating: 4.8 out of 5 stars.';
        END
        ELSE IF @catID = 4
        BEGIN
            SET @title = N'10 Easy Indoor Games for Active Kids on Rainy Days';
            SET @content = N'When outdoor playtime is cancelled due to rain, don''t worry! Try these 10 indoor active games like balloon tennis, scavenger hunts, and obstacle courses to keep kids entertained.';
        END
        ELSE
        BEGIN
            SET @title = N'Essential Toy Safety Guidelines for Parents - Update ' + CAST(@blogNum AS NVARCHAR);
            SET @content = N'Safety is always our priority. Keep small parts away from children under 3, verify battery compartments are securely screwed shut, and always buy from certified non-toxic brands.';
        END;

        IF NOT EXISTS (SELECT 1 FROM [dbo].[BlogPosts] WHERE BlogTitle = @title)
        BEGIN
            INSERT INTO [dbo].[BlogPosts]
                (AccountID, ApprovedBy, BlogCategoryID, BlogTitle, BlogContent, BlogThumbnail, Status, IsFeatured, BlogAt, CreatedAt)
            VALUES
                (@authorID, @adminId, @catID, @title, @content, @thumb, 'Published', CASE WHEN @blogNum % 4 = 0 THEN 1 ELSE 0 END, @bDate, @bDate);

            DECLARE @newPostID INT = SCOPE_IDENTITY();

            -- 1. Insert 1-2 Comments (ReviewBlogs)
            DECLARE @commentCount INT = (@blogNum % 2) + 1; -- 1 or 2 comments
            DECLARE @c INT = 1;

            WHILE @c <= @commentCount
            BEGIN
                DECLARE @custIdx INT = ((@blogNum + @c) % 5) + 1;
                DECLARE @custID INT = (SELECT AccID FROM @Customers WHERE Idx = @custIdx);
                DECLARE @commentText NVARCHAR(500);

                IF @catID = 1
                    SET @commentText = CASE WHEN @c = 1 THEN N'Awesome promotions! Just ordered some building blocks for my nephew.' ELSE N'Can I use the discount codes along with other store coupons?' END;
                ELSE IF @catID = 2
                    SET @commentText = CASE WHEN @c = 1 THEN N'Very detailed explanation. My daughter loves sensory sand!' ELSE N'Any suggestions for toys that help with speech therapy?' END;
                ELSE IF @catID = 3
                    SET @commentText = CASE WHEN @c = 1 THEN N'Great review! We bought this modeling clay set and it is very clean indeed.' ELSE N'Is this set suitable for a 4-year-old child?' END;
                ELSE IF @catID = 4
                    SET @commentText = CASE WHEN @c = 1 THEN N'Balloon tennis is a lifesaver! Tried it today and the kids played for hours.' ELSE N'Thanks for these creative ideas, very helpful!' END;
                ELSE
                    SET @commentText = CASE WHEN @c = 1 THEN N'Important tips! I always check the age labels before buying.' ELSE N'BPA-free toys are a must. Thank you for this information.' END;

                DECLARE @commentDate DATETIME2(0) = DATEADD(HOUR, 2 * @c + 1, @bDate);

                INSERT INTO [dbo].[ReviewBlogs] (BlogPostID, AccountID, Comment, ModerationStatus, IsHidden, IsDeleted, CreatedAt)
                VALUES (@newPostID, @custID, @commentText, 'Approved', 0, 0, @commentDate);

                DECLARE @newCommentID INT = SCOPE_IDENTITY();

                -- 2. Staff Reply (ReviewBlogReplies) for 50% of comments
                IF (@blogNum + @c) % 2 = 0
                BEGIN
                    DECLARE @replyText NVARCHAR(500) = N'Thank you for your comment! If you have any further questions, feel free to contact our support hotline.';
                    INSERT INTO [dbo].[ReviewBlogReplies] (ReviewBlogID, AccountID, Comment, CreatedAt)
                    VALUES (@newCommentID, @authorID, @replyText, DATEADD(MINUTE, 30, @commentDate));
                END;

                -- 3. Review Reactions
                INSERT INTO [dbo].[ReviewBlogReactions] (ReviewBlogID, AccountID, ReactionTypeID, CreatedAt)
                VALUES (@newCommentID, @adminId, 1, DATEADD(MINUTE, 45, @commentDate));

                SET @c = @c + 1;
            END;

            -- 4. Blog Post Reactions (Likes/Love from 2-3 users)
            DECLARE @rCust1 INT = (SELECT AccID FROM @Customers WHERE Idx = 1);
            DECLARE @rCust2 INT = (SELECT AccID FROM @Customers WHERE Idx = 2);
            DECLARE @rCust3 INT = (SELECT AccID FROM @Customers WHERE Idx = 3);

            INSERT INTO [dbo].[BlogPostReactions] (BlogPostID, AccountID, ReactionTypeID, CreatedAt)
            VALUES 
                (@newPostID, @rCust1, 1, DATEADD(MINUTE, 10, @bDate)),
                (@newPostID, @rCust2, 2, DATEADD(MINUTE, 25, @bDate));
                
            IF @blogNum % 2 = 0
                INSERT INTO [dbo].[BlogPostReactions] (BlogPostID, AccountID, ReactionTypeID, CreatedAt)
                VALUES (@newPostID, @rCust3, 1, DATEADD(MINUTE, 40, @bDate));
        END;
    END;

    SET @bCurrentDate = DATEADD(DAY, 1, @bCurrentDate);
    SET @blogNum = @blogNum + 1;
END;
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 33 – SHIFT TEMPLATES & WORK SCHEDULES
══════════════════════════════════════════════════════════════ */
PRINT N'[33] ShiftTemplates...';
SET IDENTITY_INSERT [dbo].[ShiftTemplates] ON;
IF NOT EXISTS (SELECT 1
FROM [dbo].[ShiftTemplates]
WHERE ShiftTemplateID=1)
    INSERT INTO [dbo].[ShiftTemplates]
    (ShiftTemplateID,ShiftName,StartTime,EndTime,MaxOrdersPerShift,IsActive,CreatedAt)
VALUES
    (1, N'Morning Shift', '07:00:00', '12:00:00', 20, 1, '2026-01-01 08:00:00'),
    (2, N'Afternoon Shift', '12:00:00', '17:00:00', 25, 1, '2026-01-01 08:00:00'),
    (3, N'Evening Shift', '17:00:00', '22:00:00', 25, 1, '2026-01-01 08:00:00');
SET IDENTITY_INSERT [dbo].[ShiftTemplates] OFF;
GO
/* ══════════════════════════════════════════════════════════════
   SECTION 35 – NOTIFICATION TEMPLATES, CAMPAIGNS, DELIVERIES
══════════════════════════════════════════════════════════════ */
BEGIN TRY
    BEGIN TRAN;

    DECLARE @Tpl TABLE (
        TemplateCode    VARCHAR(50)   NOT NULL PRIMARY KEY,
        UsageScope      VARCHAR(10)   NOT NULL,
        TitleTemplate   NVARCHAR(255) NOT NULL,
        MessageTemplate NVARCHAR(500) NOT NULL
    );

    /* ─── SYSTEM GROUP: automatic, cannot be disabled/deleted ─── */

    -- Orders — customer & staff
    INSERT INTO @Tpl VALUES
    ('ORDER_PLACED',          'SYSTEM', N'Order placed successfully',
     N'Your order {{OrderCode}} ({{TotalAmount}} VND) has been received.'),
    ('ORDER_CONFIRMED',       'SYSTEM', N'Order confirmed: {{OrderCode}}',
     N'Your order {{OrderCode}} has been confirmed and is being prepared.'),
    ('ORDER_PACKING',         'SYSTEM', N'Order {{OrderCode}} is being packed',
     N'Your order {{OrderCode}} is being packed by our team.'),
    ('ORDER_SHIPPING',        'SYSTEM', N'Order {{OrderCode}} is on the way',
     N'Order {{OrderCode}} has been handed to courier {{ShipperName}}. Please keep your phone available.'),
    ('ORDER_DELIVERED', 'SYSTEM', N'Delivery successful',
     N'Order {{OrderCode}} has been delivered successfully. We would love to hear your feedback.'),
    ('ORDER_CANCELLED',       'SYSTEM', N'Order {{OrderCode}} cancelled',
     N'Your order {{OrderCode}} was cancelled. Reason: {{CancelReason}}.'),
    ('ORDER_DELIVERY_FAILED', 'SYSTEM', N'Delivery unsuccessful',
     N'The courier could not reach you. Another delivery attempt will be made. Please keep your phone nearby.'),
    ('ORDER_RETURN_REFUND_PENDING', 'SYSTEM', N'Returned to shop',
     N'Order {{OrderCode}} has returned to our shop. We are processing your refund.'),
    ('ORDER_CANCELLED_DELIVERY_FAIL', 'SYSTEM', N'Order cancelled',
     N'Order {{OrderCode}} was cancelled due to failed delivery.'),
    ('ORDER_ASSIGNED',        'SYSTEM', N'Order Assigned: {{OrderCode}}',
     N'Order {{OrderCode}} from {{CustomerName}} has been assigned to you. Total: {{TotalAmount}} VND. Please process it during your current shift.');

    -- Payments & wallet
    INSERT INTO @Tpl VALUES
    ('PAYMENT_SUCCESS',  'SYSTEM', N'Payment successful',
     N'You paid {{Amount}} VND for order {{OrderCode}}.'),
    ('PAYMENT_FAILED',   'SYSTEM', N'Payment failed',
     N'Payment of {{Amount}} VND for order {{OrderCode}} failed.'),
    ('WALLET_TOPUP',     'SYSTEM', N'Wallet top-up successful',
     N'{{Amount}} VND was added to your wallet. Current balance: {{Balance}} VND.'),
    ('WALLET_REFUND',    'SYSTEM', N'Refund to wallet',
     N'{{Amount}} VND was refunded to your wallet for order {{OrderCode}}.'),
    ('WALLET_WITHDRAWAL_SUCCESS', 'SYSTEM', N'Withdrawal successful',
     N'{{Amount}} VND has been transferred to {{BankName}} - {{AccountNumber}} successfully.'),
    ('WALLET_WITHDRAWAL_FAILED',  'SYSTEM', N'Withdrawal failed',
     N'Withdrawal of {{Amount}} VND failed. Reason: {{FailReason}}. Your wallet balance was not changed.'),
    ('REFUND_APPROVED',  'SYSTEM', N'Refund approved',
     N'Your refund request for order {{OrderCode}} was approved. {{Amount}} VND will be returned to your wallet.'),
    ('REFUND_REJECTED',  'SYSTEM', N'Refund rejected',
     N'Your refund request for order {{OrderCode}} was rejected. Contact support if you need help.'),
    ('REFUND_COMPLETED', 'SYSTEM', N'Refund completed',
     N'{{Amount}} VND from order {{OrderCode}} has been refunded to your wallet.');

    -- Products & Stock
    INSERT INTO @Tpl VALUES
    ('PRODUCT_BACK_IN_STOCK', 'SYSTEM', N'Product back in stock: {{ProductName}}',
     N'The product {{ProductName}} you are interested in is back in stock at {{Price}} VND. Get it now before it runs out!'),
    ('WISHLIST_PRICE_DROP',   'SYSTEM', N'Price drop on {{ProductName}}',
     N'{{ProductName}} in your wishlist is now on sale for only {{Price}} VND.');

    -- Reviews & Blogs
    INSERT INTO @Tpl VALUES
    ('REVIEW_STAFF_REPLIED', 'SYSTEM', N'Reply to review on {{ProductName}}',
     N'A customer service representative has replied to your review of {{ProductName}}.'),
    ('BLOG_COMMENT_REPLIED', 'SYSTEM', N'Blog comment reply: {{BlogTitle}}',
     N'Someone replied to your comment on {{BlogTitle}}.');

    -- Staff Notifications
    INSERT INTO @Tpl VALUES
    ('STAFF_NEW_ORDER',         'SYSTEM', N'New order received: {{OrderCode}}',
     N'The system registered a new order {{OrderCode}} worth {{TotalAmount}} VND. Please process it.'),
    ('STAFF_CANCEL_REQUEST',    'SYSTEM', N'Cancellation request for {{OrderCode}}',
     N'Customer {{CustomerName}} has requested to cancel order {{OrderCode}}. Reason: {{Reason}}.'),
    ('STAFF_REFUND_REQUEST',    'SYSTEM', N'Refund request for {{OrderCode}}',
     N'Customer {{CustomerName}} requested a refund for order {{OrderCode}}.'),
    ('STAFF_REVIEW_MODERATION', 'SYSTEM', N'Approve new review',
     N'There is a new {{Rating}}-star review for product {{ProductName}} that requires moderation.'),
    ('STAFF_LOW_RATING',        'SYSTEM', N'Low rating warning',
     N'Product {{ProductName}} just received a {{Rating}}-star review. Please check and resolve.'),
    ('STAFF_SHIFT_STARTED',     'SYSTEM', N'Shift {{ShiftName}} started',
     N'Your work shift {{ShiftName}} has started. Have a great shift!');

    -- Merchandise Notifications
    INSERT INTO @Tpl VALUES
    ('MERCH_READY_TO_PACK', 'SYSTEM', N'Order ready for packing',
     N'Order {{OrderCode}} is ready to be packed.'),
    ('MERCH_PICKED_UP',     'SYSTEM', N'Package picked up by courier',
     N'The courier has successfully picked up the package for order {{OrderCode}}.'),
    ('MERCH_RETURNED',      'SYSTEM', N'Package returned to shop',
     N'Order {{OrderCode}} has been returned to the shop.'),
    ('MERCH_LOW_STOCK',     'SYSTEM', N'Low stock warning',
     N'Product {{ProductName}} has only {{Quantity}} units left. Restocking is needed.'),
    ('MERCH_OUT_OF_STOCK',  'SYSTEM', N'Out of stock warning',
     N'Product {{ProductName}} is completely out of stock.');

    -- Admin Notifications
    INSERT INTO @Tpl VALUES
    ('ADMIN_PAYMENT_ERROR',    'SYSTEM', N'Payment gateway error',
     N'Payment gateway {{GatewayName}} reported error: {{ErrorMessage}}.'),
    ('ADMIN_JOB_FAILED',       'SYSTEM', N'Background job failed',
     N'Background Job {{JobName}} failed. Please check the logs.'),
    ('ADMIN_OUTBOX_STUCK',     'SYSTEM', N'Outbox event stuck',
     N'Outbox event {{EventType}} ({{EventId}}) has reached retry limits. Please check logs.'),
    ('ADMIN_SHIPPING_ERROR',   'SYSTEM', N'Shipping sync error',
     N'Error synchronizing shipping status for order {{OrderCode}}: {{ErrorMessage}}.'),
    ('ADMIN_DAMAGE_LOST',      'SYSTEM', N'Items damaged/lost',
     N'Order {{OrderCode}} reported as damaged or lost during delivery.'),
    ('ADMIN_RETURN_FAIL',      'SYSTEM', N'GHN return failed',
     N'Order {{OrderCode}} (GHN: {{ProviderOrderCode}}) reported return_fail. Please process manually.'),
    ('ADMIN_BLOG_PENDING',     'SYSTEM', N'Blog pending approval: {{BlogTitle}}',
     N'Blog post {{BlogTitle}} was submitted and is waiting for your approval.'),
    ('ADMIN_ORDER_QUEUED',     'SYSTEM', N'Order {{OrderCode}} pending assignment',
     N'Order {{OrderCode}} is pending assignment due to: {{Reason}}.'),
    ('ADMIN_SHIFT_ENDED_PENDING', 'SYSTEM', N'Shift ended with pending orders',
     N'Shift {{ShiftName}} (staff #{{AccountId}}) ended with {{CurrentLoad}} orders still pending.'),
    ('ADMIN_SHIFT_FULL',       'SYSTEM', N'Shift capacity reached',
     N'One or both roles in shift {{ShiftName}} reached order capacity on {{WorkDate}}. Please manage manually.');

    /* ─── ADMIN GROUP: marketing, togglable ─────────────────────── */
    INSERT INTO @Tpl VALUES
    ('FLASH_SALE_STARTED', 'ADMIN', N'{{PromotionName}} Started!',
     N'The flash sale {{PromotionName}} has officially started from {{StartDate}} to {{EndDate}}. Grab the deals now!'),
    ('VOUCHER_NEW',        'ADMIN', N'Voucher for you: {{VoucherCode}}',
     N'You received a voucher {{VoucherCode}} for a discount of {{DiscountValue}} ({{DiscountType}}). Apply it before it expires on {{ExpiryDate}}!'),
    ('VOUCHER_EXPIRING',   'ADMIN', N'Voucher {{VoucherCode}} is expiring soon!',
     N'Don''t miss out on voucher {{VoucherCode}} (Discount of {{DiscountValue}}). It expires on {{ExpiryDate}}. Use it now!'),
    ('BIRTHDAY_CUSTOMER',  'ADMIN', N'Happy Birthday, {{CustomerName}}!',
     N'Happy birthday to you! ToyStore has sent you a special gift. Please check your Voucher wallet!'),
    ('BIRTHDAY_CHILD',     'ADMIN', N'Happy Birthday, {{ChildName}}!',
     N'Happy birthday to {{ChildName}}! ToyStore wishes them healthy growth and joy. Parents, pick a favorite toy for them!');

    /* ─── MERGE idempotent insertion & update ─────────────────────── */
    MERGE [Notification].[Templates] AS target
    USING @Tpl AS source
    ON (target.TemplateCode = source.TemplateCode)
    WHEN MATCHED AND (target.IsDeleted = 0) THEN
        UPDATE SET 
            target.UsageScope = source.UsageScope,
            target.TitleTemplate = source.TitleTemplate,
            target.MessageTemplate = source.MessageTemplate,
            target.UpdatedAt = GETUTCDATE()
    WHEN NOT MATCHED THEN
        INSERT (TemplateCode, UsageScope, TitleTemplate, MessageTemplate, IsActive, IsDeleted, CreatedAt)
        VALUES (source.TemplateCode, source.UsageScope, source.TitleTemplate, source.MessageTemplate, 1, 0, GETUTCDATE());

    COMMIT TRAN;
    PRINT N'✅ Notification templates fully unified and translated to English.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'❌ Error: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO

-- ============================================================
-- Campaigns
-- ============================================================
DECLARE @admCamp INT = (SELECT TOP 1
    AccountID
FROM Accounts
WHERE Email = 'admin@toyhouse.vn');

IF NOT EXISTS (SELECT 1
FROM [Notification].[Campaigns])
    INSERT INTO [Notification].[Campaigns]
    (CampaignName, TemplateCode, SourceType, TargetType, Status,
    CreatedByAccountID, IsDeleted, CreatedAt)
VALUES
    (N'Summer Sale 2026 Announcement', 'FLASH_SALE_STARTED', 'ADMIN', 'ALL', 'Sent', @admCamp, 0, '2026-03-28 08:00:00'),
    (N'Welcome Voucher for New Customers', 'VOUCHER_NEW', 'ADMIN', 'ALL', 'Sent', @admCamp, 0, '2026-01-01 08:00:00'),
    (N'Post-Delivery Review Reminder', 'ORDER_DELIVERED', 'SYSTEM', 'INDIVIDUAL', 'Sent', NULL, 0, '2026-04-01 00:00:00');
GO

-- ============================================================
-- Campaign Stats
-- ============================================================
IF NOT EXISTS (SELECT 1
FROM [Notification].[CampaignStats])
    INSERT INTO [Notification].[CampaignStats]
    (CampaignID, TotalSent, TotalRead, TotalClicked, ComputedAt)
SELECT
    CampaignID,
    CASE CampaignName
            WHEN N'Summer Sale 2026 Announcement'     THEN 5
            WHEN N'Welcome Voucher for New Customers' THEN 5
            ELSE 4
        END,
    CASE CampaignName
            WHEN N'Summer Sale 2026 Announcement'     THEN 4
            WHEN N'Welcome Voucher for New Customers' THEN 4
            ELSE 3
        END,
    CASE CampaignName
            WHEN N'Summer Sale 2026 Announcement'     THEN 2
            WHEN N'Welcome Voucher for New Customers' THEN 3
            ELSE 2
        END,
    GETDATE()
FROM [Notification].[Campaigns];
GO

-- ============================================================
-- Deliveries
-- ============================================================
DECLARE @cmpSale INT = (SELECT TOP 1
    CampaignID
FROM [Notification].[Campaigns]
WHERE CampaignName = N'Summer Sale 2026 Announcement');
DECLARE @cmpRv   INT = (SELECT TOP 1
    CampaignID
FROM [Notification].[Campaigns]
WHERE CampaignName = N'Post-Delivery Review Reminder');

-- Order placed notifications
INSERT INTO [Notification].[Deliveries]
    (AccountID, CampaignID, TemplateCode, RecipientType, NotificationType,
    Title, Message, Payload, Status, IdempotencyKey, CreatedAt)
SELECT
    o.AccountID,
    @cmpSale,
    'ORDER_PLACED',
    'CUSTOMER',
    'ORDER',
    N'Order Placed Successfully',
    N'Order ' + o.OrderCode + N' has been confirmed. We are preparing your items!',
    '{"orderId":' + CAST(o.OrderID AS VARCHAR) + ',"orderCode":"' + o.OrderCode + '"}',
    'Unread',
    'DLV-' + o.OrderCode,
    o.OrderDate
FROM Orders o
WHERE NOT EXISTS (
    SELECT 1
FROM [Notification].[Deliveries] d
WHERE d.IdempotencyKey = 'DLV-' + o.OrderCode
);

-- Post-delivery review reminder notifications
INSERT INTO [Notification].[Deliveries]
    (AccountID, CampaignID, TemplateCode, RecipientType, NotificationType,
    Title, Message, Payload, Status, IdempotencyKey, CreatedAt)
SELECT
    o.AccountID,
    @cmpRv,
    'ORDER_DELIVERED',
    'CUSTOMER',
    'ORDER',
    N'Review Your Products',
    N'Order ' + o.OrderCode + N' has been successfully delivered.',
    '{"orderId":' + CAST(o.OrderID AS VARCHAR) + ',"orderCode":"' + o.OrderCode + '"}',
    'Unread',
    'DLV-RV-' + o.OrderCode,
    DATEADD(DAY, 1, o.DeliveredAt)
FROM Orders o
WHERE o.StatusID IN (6, 7)
    AND o.DeliveredAt IS NOT NULL
    AND NOT EXISTS (
      SELECT 1
    FROM [Notification].[Deliveries] d
    WHERE d.IdempotencyKey = 'DLV-RV-' + o.OrderCode
  );

-- Mark delivered-order notifications as Read
UPDATE d
SET d.Status = 'Read',
    d.ReadAt  = DATEADD(HOUR, 2, d.CreatedAt)
FROM [Notification].[Deliveries] d
    JOIN Orders o ON o.OrderID = CAST(JSON_VALUE(d.Payload, '$.orderId') AS INT)
WHERE o.StatusID IN (6, 7)
    AND d.Status = 'Unread';
GO

-- ============================================================
-- Mega Flash Sale Click Test Campaign [ADDED FOR TESTING]
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM [Notification].[Campaigns] WHERE CampaignName = N'Mega Flash Sale Click Test')
BEGIN
    DECLARE @admCampTest INT = (SELECT TOP 1 AccountID FROM Accounts WHERE Email = 'admin@toyhouse.vn');
    DECLARE @pMega INT = (SELECT TOP 1 PromotionID FROM Promotions WHERE PromotionName = N'Mega Flash Sale');

    IF @admCampTest IS NOT NULL AND @pMega IS NOT NULL
    BEGIN
        PRINT N'Seeding Mega Flash Sale Click Test Campaign...';
        
        INSERT INTO [Notification].[Campaigns]
            (CampaignName, TemplateCode, SourceType, TargetType, Status,
            ReferenceType, ReferenceID, ActionType, ActionTarget, TitleOverride, MessageOverride,
            CreatedByAccountID, IsDeleted, CreatedAt)
        VALUES
            (N'Mega Flash Sale Click Test', 'FLASH_SALE_STARTED', 'ADMIN', 'ALL', 'Sent',
            'SALE', @pMega, 'ROUTE', N'/?flashSale=' + CAST(@pMega AS NVARCHAR(10)),
            N'Mega Flash Sale is Live!', N'Click here to check out amazing toy deals right now!',
            @admCampTest, 0, GETUTCDATE());

        DECLARE @newCmpId INT = SCOPE_IDENTITY();

        -- CampaignStats
        INSERT INTO [Notification].[CampaignStats]
            (CampaignID, TotalSent, TotalRead, TotalClicked, ComputedAt)
        VALUES
            (@newCmpId, 5, 0, 0, GETUTCDATE());

        -- Deliveries for all active customer accounts
        INSERT INTO [Notification].[Deliveries]
            (AccountID, CampaignID, TemplateCode, RecipientType, Channel, NotificationType,
            ActionType, ActionTarget, Title, Message, Payload, Status, IsDeleted, CreatedAt)
        SELECT
            AccountID,
            @newCmpId,
            'FLASH_SALE_STARTED',
            'CUSTOMER',
            'WEB_BELL',
            'PROMOTION',
            'ROUTE',
            N'/?flashSale=' + CAST(@pMega AS NVARCHAR(10)),
            N'Mega Flash Sale is Live!',
            N'Click here to check out amazing toy deals right now!',
            N'{"promotionId":' + CAST(@pMega AS VARCHAR) + N'}',
            'Unread',
            0,
            GETUTCDATE()
        FROM Accounts
        WHERE RoleID = 1 AND IsActive = 1 AND IsDeleted = 0;
    END
END
GO
/* ══════════════════════════════════════════════════════════════
   SECTION 36 – REVIEW MODERATION LOGS  [NEW]
   Logs AI and manual (Staff) moderation results
   for approved text and image reviews.
══════════════════════════════════════════════════════════════ */
PRINT N'[36-NEW] ReviewModerationLogs...';
IF NOT EXISTS (SELECT 1
FROM [dbo].[ReviewModerationLogs])
BEGIN
    /* AI auto-approve for all text reviews */
    INSERT INTO [dbo].[ReviewModerationLogs]
        (TargetType, ReviewID, ImageID, ModeratorType, ModeratedBy,
        Action, AIModelVersion, ModerationResult, Reason, CreatedAt)
    SELECT
        'Text',
        rp.ReviewID,
        NULL,
        'AI',
        NULL,
        'Approved',
        'claude-moderation-v1.0',
        N'{"score":0.02,"categories":{"hate":false,"harassment":false,"spam":false},"passed":true}',
        NULL,
        DATEADD(MINUTE, 2, rp.CreatedAt)
    FROM [dbo].[ReviewProducts] rp
    WHERE rp.ModerationStatus = 'Approved';

    /* AI auto-approve for all review images */
    INSERT INTO [dbo].[ReviewModerationLogs]
        (TargetType, ReviewID, ImageID, ModeratorType, ModeratedBy,
        Action, AIModelVersion, ModerationResult, Reason, CreatedAt)
    SELECT
        'Image',
        rpi.ReviewProductID,
        rpi.ReviewProductImageID,
        'AI',
        NULL,
        'Approved',
        'claude-vision-moderation-v1.0',
        N'{"score":0.01,"nudity":false,"violence":false,"passed":true}',
        NULL,
        DATEADD(MINUTE, 3, rpi.CreatedAt)
    FROM [dbo].[ReviewProductImages] rpi
    WHERE rpi.ModerationStatus = 'Approved';

    /* Staff override: hung's review (rating 4) manually verified and confirmed */
    DECLARE @stfMod INT = (SELECT TOP 1
        AccountID
    FROM Accounts
    WHERE Email='nhung.st@toyhouse.vn');
    DECLARE @rvHung INT = (SELECT TOP 1
        rp.ReviewID
    FROM ReviewProducts rp
        JOIN Accounts a ON a.AccountID = rp.AccountID
    WHERE a.Email = 'hung.nguyen88@gmail.com');
    IF @rvHung IS NOT NULL
        INSERT INTO [dbo].[ReviewModerationLogs]
        (TargetType, ReviewID, ImageID, ModeratorType, ModeratedBy,
        Action, AIModelVersion, ModerationResult, Reason, CreatedAt)
    VALUES
        ('Text', @rvHung, NULL, 'Staff', @stfMod,
            'Overridden', NULL, NULL,
            N'Review flagged for competitor brand mention – manually verified as acceptable content',
            DATEADD(HOUR, 1, (SELECT CreatedAt
            FROM ReviewProducts
            WHERE ReviewID = @rvHung)));
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 37 – CAMPAIGN APPROVAL LOGS  [NEW]
   Notification campaign approval history: Submitted → Approved
══════════════════════════════════════════════════════════════ */
PRINT N'[37-NEW] CampaignApprovalLogs...';
IF NOT EXISTS (SELECT 1
FROM [Notification].[CampaignApprovalLogs])
BEGIN
    DECLARE @admAL  INT = (SELECT TOP 1
        AccountID
    FROM Accounts
    WHERE Email='admin@toyhouse.vn');
    DECLARE @stfAL  INT = (SELECT TOP 1
        AccountID
    FROM Accounts
    WHERE Email='nhung.st@toyhouse.vn');

    -- Match English campaign names inserted above
    DECLARE @cmpS   INT = (SELECT TOP 1
        CampaignID
    FROM [Notification].[Campaigns]
    WHERE CampaignName=N'Summer Sale 2026 Announcement');
    DECLARE @cmpV   INT = (SELECT TOP 1
        CampaignID
    FROM [Notification].[Campaigns]
    WHERE CampaignName=N'Welcome Voucher for New Customers');
    DECLARE @cmpR   INT = (SELECT TOP 1
        CampaignID
    FROM [Notification].[Campaigns]
    WHERE CampaignName=N'Post-Delivery Review Reminder');

    /* Summer Sale campaign: Staff submit → Admin approve */
    INSERT INTO [Notification].[CampaignApprovalLogs]
        (CampaignID,Action,ActorID,Note,CreatedAt)
    VALUES
        (@cmpS, 'Submitted', @stfAL, N'Proposed Summer Sale 2026 campaign to notify all customers', '2026-03-27 14:00:00'),
        (@cmpS, 'Approved', @admAL, N'Content and schedule approved', '2026-03-27 16:30:00'),

        /* Welcome voucher campaign: Staff submit → Admin approve */
        (@cmpV, 'Submitted', @stfAL, N'Welcome voucher campaign for newly registered customers', '2025-12-30 09:00:00'),
        (@cmpV, 'Approved', @admAL, N'Approved – voucher budget confirmed', '2025-12-30 11:00:00'),

        /* Post-delivery review reminder: system campaign, auto-approved */
        (@cmpR, 'Approved', @admAL, N'System campaign – auto-approved', '2026-04-01 00:00:00');
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 39 – DELIVERY ACTIONS  [NEW]
   User read/click actions on notifications
══════════════════════════════════════════════════════════════ */
PRINT N'[39-NEW] DeliveryActions...';
IF NOT EXISTS (SELECT 1
FROM [Notification].[DeliveryActions])
BEGIN
    /* Log Read action for deliveries marked as Read */
    INSERT INTO [Notification].[DeliveryActions]
        (DeliveryID, AccountID, ActionType, ActionTarget, OccurredAt)
    SELECT
        d.DeliveryID,
        d.AccountID,
        'Read',
        NULL,
        d.ReadAt
    FROM [Notification].[Deliveries] d
    WHERE d.Status = 'Read' AND d.ReadAt IS NOT NULL;

    /* Click on some completed-order notifications */
    INSERT INTO [Notification].[DeliveryActions]
        (DeliveryID, AccountID, ActionType, ActionTarget, OccurredAt)
    SELECT
        d.DeliveryID,
        d.AccountID,
        'Click',
        '/orders/' + JSON_VALUE(d.Payload, '$.orderCode'),
        DATEADD(MINUTE, 5, d.ReadAt)
    FROM [Notification].[Deliveries] d
        JOIN Orders o ON o.OrderID = CAST(JSON_VALUE(d.Payload,'$.orderId') AS INT)
    WHERE d.Status = 'Read'
        AND d.ReadAt IS NOT NULL
        AND o.StatusID = 7
        AND NOT EXISTS (
          SELECT 1
        FROM [Notification].[DeliveryActions] da
        WHERE da.DeliveryID = d.DeliveryID AND da.ActionType = 'Click');
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 43 – RECOMMENDATION WIDGETS
══════════════════════════════════════════════════════════════ */
PRINT N'[43] Recommendation.Widgets...';
IF NOT EXISTS (SELECT 1
FROM [Recommendation].[Widgets])
    INSERT INTO [Recommendation].[Widgets]
    ([WidgetCode], [WidgetName], [Algorithm], [MaxItems], [FallbackAlgo], [IsActive])
VALUES
    ('homepage_trending', N'Trending Today', 'trending', 10, 'popular', 1),
    ('pdp_similar', N'Similar Products', 'content_based', 8, 'popular', 1),
    ('pdp_also_bought', N'Customers Also Bought', 'collaborative', 8, 'trending', 1),
    ('after_purchase', N'Buy Next', 'collaborative', 8, 'trending', 1),
    ('homepage_personal', N'Recommended For You', 'weighted_score', 12, 'trending', 1);
GO


/* ══════════════════════════════════════════════════════════════
   SECTION 43.1 – CUSTOMER WALLETS & PINS [NEW]
   Creates active wallets and default wallet PINs for customer accounts.
══════════════════════════════════════════════════════════════ */
PRINT N'[43.1-NEW] Wallets & WalletPins...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[Wallets])
BEGIN
    -- Insert wallets for customers (RoleID = 1)
    INSERT INTO [dbo].[Wallets] (AccountID, Currency, Balance, Status, CreatedAt)
    SELECT AccountID, 'VND', 5000000.00, 'Active', CreatedAt
    FROM Accounts
    WHERE RoleID = 1;

    -- Insert wallet pins for all wallets
    INSERT INTO [dbo].[WalletPins] (WalletID, PinHash, IsActive, FailedAttempts, TotalFailedAttempts, LastChangedAt, CreatedAt)
    SELECT WalletID, '$2a$11$K3u.z94n.5aM/60s07bZyeUv20a1K6u7m2R5YkM10vQh2h2.2H5K.', 1, 0, 0, CreatedAt, CreatedAt
    FROM Wallets;
END
GO


/* ══════════════════════════════════════════════════════════════
   SECTION 44 – WALLET PIN ATTEMPTS  [NEW]
   Log wallet PIN entry attempts by customers
══════════════════════════════════════════════════════════════ */
PRINT N'[44-NEW] WalletPinAttempts...';
IF NOT EXISTS (SELECT 1
FROM [dbo].[WalletPinAttempts])
BEGIN
    INSERT INTO [dbo].[WalletPinAttempts]
        (WalletID, AccountID, ActionType, IsSuccess, CreatedAt)
    SELECT w.WalletID, a.AccountID, v.ActionType, v.IsSuccess, v.AtAt
    FROM Accounts a
        JOIN Wallets w ON w.AccountID = a.AccountID
        JOIN (VALUES
            /* lananh: 2 successful payment attempts */
            ('lananh.pham@gmail.com', 'PAYMENT', 1, '2026-02-14 10:29:00'),
            ('lananh.pham@gmail.com', 'PAYMENT', 1, '2026-04-05 08:59:00'),
            /* hung: 1 failed attempt, then success */
            ('hung.nguyen88@gmail.com', 'PAYMENT', 0, '2026-04-01 13:18:00'),
            ('hung.nguyen88@gmail.com', 'PAYMENT', 1, '2026-04-01 13:19:00'),
            /* thuha: successful payment */
            ('thuha.hoang@gmail.com', 'PAYMENT', 1, '2026-04-01 13:19:30'),
            ('thuha.hoang@gmail.com', 'VIEW_BALANCE', 1, '2026-04-15 09:00:00'),
            /* mtuando: balance check */
            ('mtuando@outlook.com', 'VIEW_BALANCE', 1, '2026-04-18 08:50:00')
    ) AS v(Email, ActionType, IsSuccess, AtAt) ON a.Email = v.Email
    WHERE a.RoleID = 1;
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 38 – SYSTEM TABLES
══════════════════════════════════════════════════════════════ */
PRINT N'[38] BackgroundJobs, DomainEventOutbox...';
IF NOT EXISTS (SELECT 1
FROM [System].[BackgroundJobs])
    INSERT INTO [System].[BackgroundJobs]
    (JobName,CronExpression,IsEnabled,LastRunStatus)
VALUES
    ('SyncGHNStatus', '*/5 * * * *', 1, 'Completed'),
    ('RecalcTrending', '0 * * * *', 1, 'Completed'),
    ('ExpireVouchers', '0 0 * * *', 1, 'Completed'),
    ('ExpirePromotions', '0 0 * * *', 1, 'Completed'),
    ('SendPromoNotif', '0 8 * * *', 1, 'Completed'),
    ('UnblockUsers', '*/15 * * * *', 1, 'Completed'),
    ('LowStockAlert', '0 9 * * *', 1, 'Pending'),
    ('BirthdayNotif', '0 7 * * *', 1, 'Pending'),
    ('CleanExpiredSessions', '0 1 * * *', 1, 'Completed');

INSERT INTO [System].[DomainEventOutbox]
    (EventID,EventType,AggregateType,AggregateId,Payload,OccurredOn)
SELECT NEWID(), 'OrderPlacedEvent', 'Order', CAST(OrderID AS VARCHAR),
    '{"orderId":'+CAST(OrderID AS VARCHAR)+',"orderCode":"'+OrderCode+'","amount":'+CAST(TotalAmount AS VARCHAR)+'}',
    OrderDate
FROM Orders o
WHERE NOT EXISTS (
    SELECT 1
FROM [System].[DomainEventOutbox] eb
WHERE eb.AggregateId=CAST(o.OrderID AS VARCHAR) AND eb.EventType='OrderPlacedEvent');

INSERT INTO [System].[DomainEventOutbox]
    (EventID,EventType,AggregateType,AggregateId,Payload,OccurredOn)
SELECT NEWID(), 'OrderCompletedEvent', 'Order', CAST(OrderID AS VARCHAR),
    '{"orderId":'+CAST(OrderID AS VARCHAR)+',"orderCode":"'+OrderCode+'"}',
    CompletedAt
FROM Orders o
WHERE o.StatusID=7 AND o.CompletedAt IS NOT NULL
    AND NOT EXISTS (
    SELECT 1
    FROM [System].[DomainEventOutbox] eb
    WHERE eb.AggregateId=CAST(o.OrderID AS VARCHAR) AND eb.EventType='OrderCompletedEvent');

INSERT INTO [System].[DomainEventOutbox]
    (EventID,EventType,AggregateType,AggregateId,Payload,OccurredOn)
SELECT NEWID(), 'ProductReviewedEvent', 'Review', CAST(ReviewID AS VARCHAR),
    '{"reviewId":'+CAST(ReviewID AS VARCHAR)+',"productId":'+CAST(ProductID AS VARCHAR)+',"rating":'+CAST(Rating AS VARCHAR)+'}',
    CreatedAt
FROM ReviewProducts
WHERE NOT EXISTS (
    SELECT 1
FROM [System].[DomainEventOutbox] eb
WHERE eb.AggregateId=CAST(ReviewProducts.ReviewID AS VARCHAR)
    AND eb.EventType='ProductReviewedEvent');
GO