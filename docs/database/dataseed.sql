/* =================================================================
   DataSeed FULL v5.4 – SEP490_ToyStore
   Updated for Schema v3.2
   All business datetimes stored as UTC. English content only.
================================================================= */

USE [SEP490_ToyStore];
GO
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

PRINT N'================================================================';
PRINT N'  DataSeed v5.4 (Schema v3.2) – UTC timeline seed – Starting...';
PRINT N'================================================================';

/* ══════════════════════════════════════════════════════════════
   SECTION 0 – UTC TIME ANCHOR (persists across batches)
   VN wall-clock → UTC: CAST((date+time AT TIME ZONE 'SE Asia Standard Time')
                           AT TIME ZONE 'UTC' AS DATETIME2(0))
══════════════════════════════════════════════════════════════ */
IF OBJECT_ID('tempdb..#SeedTime') IS NOT NULL DROP TABLE #SeedTime;
CREATE TABLE #SeedTime
(
    UtcNow DATETIME2(0) NOT NULL,
    UtcToday DATE NOT NULL
);
INSERT INTO #SeedTime
    (UtcNow, UtcToday)
VALUES
    (CAST('2026-08-01 00:00:00' AS DATETIME2(0)), CAST('2026-08-01' AS DATE));
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 1 – ROLES
══════════════════════════════════════════════════════════════ */
PRINT N'[1] Roles...';
DECLARE @UtcNow1 DATETIME2(0) = (SELECT UtcNow
FROM #SeedTime);
DECLARE @LookupCreated1 DATETIME2(0) = DATEADD(MONTH, -6, @UtcNow1);

SET IDENTITY_INSERT [dbo].[Roles] ON;
IF NOT EXISTS (SELECT 1
FROM [dbo].[Roles]
WHERE RoleID = 1)
    INSERT INTO [dbo].[Roles]
    (RoleID, RoleName, Description, CreatedAt)
VALUES
    (1, 'Customer', N'Customer', @LookupCreated1),
    (2, 'Admin', N'Administrator', @LookupCreated1),
    (3, 'Staff', N'Sales Staff', @LookupCreated1),
    (4, 'Merchandise', N'Warehouse Staff', @LookupCreated1);
SET IDENTITY_INSERT [dbo].[Roles] OFF;
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 2 – SEXES
══════════════════════════════════════════════════════════════ */
PRINT N'[2] Sexes...';
DECLARE @LookupCreated2 DATETIME2(0) = DATEADD(MONTH, -6, (SELECT UtcNow
FROM #SeedTime));

SET IDENTITY_INSERT [dbo].[Sexes] ON;
IF NOT EXISTS (SELECT 1
FROM [dbo].[Sexes]
WHERE SexID = 1)
    INSERT INTO [dbo].[Sexes]
    (SexID, SexName, CreatedAt)
VALUES
    (1, N'Male', @LookupCreated2),
    (2, N'Female', @LookupCreated2),
    (3, N'Other', @LookupCreated2);
SET IDENTITY_INSERT [dbo].[Sexes] OFF;
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 3 – ACCOUNTS
══════════════════════════════════════════════════════════════ */
PRINT N'[3] Accounts – Staff & Admin...';
DECLARE @UtcNow3 DATETIME2(0) = (SELECT UtcNow
FROM #SeedTime);
DECLARE @StaffCreated DATETIME2(0) = DATEADD(MONTH, -6, @UtcNow3);
DECLARE @StaffCreated2 DATETIME2(0) = DATEADD(MONTH, -5, @UtcNow3);

IF NOT EXISTS (SELECT 1
FROM [dbo].[Accounts]
WHERE Email = 'admintoystore@gmail.com')
    INSERT INTO [dbo].[Accounts]
    (RoleID, SexID, EmployeeCode, AccountName, PhoneNumber, Email, DOB, ImageURL, PasswordHash, HasPassword, IsActive, CreatedAt)
VALUES
    (2, 1, 'AD001', N'James Admin', '0901000001', 'admintoystore@gmail.com', '1990-03-15', 'https://res.cloudinary.com/datfyxi3f/image/upload/v1780546687/4f2018e8-791f-416c-9bf8-345926edeccd_ytjkb1.jpg', '$2a$11$K3u.z94n.5aM/60s07bZyeUv20a1K6u7m2R5YkM10vQh2h2.2H5K.', 1, 1, @StaffCreated),
    (3, 2, 'ST001', N'Nhung Tran', '0901000002', 'nhung.st@toyhouse.vn', '1995-07-22', 'https://res.cloudinary.com/datfyxi3f/image/upload/v1780546686/9534bc17-de93-49bd-833f-7ae947019fd8_nkgjz9.jpg', '$2a$11$K3u.z94n.5aM/60s07bZyeUv20a1K6u7m2R5YkM10vQh2h2.2H5K.', 1, 1, @StaffCreated),
    (3, 1, 'ST002', N'Dung Le', '0901000003', 'dung.st@toyhouse.vn', '1993-11-08', 'https://res.cloudinary.com/datfyxi3f/image/upload/v1780546686/cce0bdb4-1054-4391-9b11-708fb552ad2b_c2mgro.jpg', '$2a$11$K3u.z94n.5aM/60s07bZyeUv20a1K6u7m2R5YkM10vQh2h2.2H5K.', 1, 1, @StaffCreated2),
    (3, 2, 'ST003', N'Thao Nguyen', '0901000004', 'thao.st@toyhouse.vn', '1997-04-30', 'https://res.cloudinary.com/datfyxi3f/image/upload/v1780546686/26e757ce-03e1-4598-bcfb-bf90d7250672Nu_zecjul.jpg', '$2a$11$K3u.z94n.5aM/60s07bZyeUv20a1K6u7m2R5YkM10vQh2h2.2H5K.', 1, 1, @StaffCreated2),
    (4, 1, 'MC001', N'Bao Nguyen', '0901000005', 'bao.kho@toyhouse.vn', '1992-09-18', 'https://res.cloudinary.com/datfyxi3f/image/upload/v1780546686/0ec897fc-0714-4c8c-96f1-4a8b02f4bf12_ncoay0.jpg', '$2a$11$K3u.z94n.5aM/60s07bZyeUv20a1K6u7m2R5YkM10vQh2h2.2H5K.', 1, 1, @StaffCreated),
    (4, 2, 'MC002', N'Lan Hoang', '0901000006', 'lan.kho@toyhouse.vn', '1994-06-25', 'https://res.cloudinary.com/datfyxi3f/image/upload/v1780548037/2ec7bb2c-7ae9-41b7-9c51-bc69fccdf731_lafvlw.jpg', '$2a$11$K3u.z94n.5aM/60s07bZyeUv20a1K6u7m2R5YkM10vQh2h2.2H5K.', 1, 1, @StaffCreated2);
GO

PRINT N'[3] Accounts – Customers (5 users)...';
DECLARE @UtcNow3b DATETIME2(0) = (SELECT UtcNow
FROM #SeedTime);
DECLARE @CustPwdHash VARCHAR(64) = UPPER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'Customer@123456'), 2));

IF NOT EXISTS (SELECT 1
FROM [dbo].[Accounts]
WHERE Email = 'lananh.pham@gmail.com')
    INSERT INTO [dbo].[Accounts]
    (RoleID, SexID, AccountName, PhoneNumber, Email, DOB, ImageURL, PasswordHash, HasPassword, IsActive, CreatedAt)
VALUES
    (1, 2, N'Lan Anh Pham', '0912001001', 'lananh.pham@gmail.com', '1988-03-12', 'https://i.pravatar.cc/300?img=35', @CustPwdHash, 1, 1, DATEADD(MONTH, -4, @UtcNow3b)),
    (1, 1, N'Hung Nguyen', '0912001002', 'hung.nguyen88@gmail.com', '1988-06-20', 'https://i.pravatar.cc/300?img=68', @CustPwdHash, 1, 1, DATEADD(MONTH, -3, @UtcNow3b)),
    (1, 2, N'Bau Chau Vu', '0912001003', 'bauchau.vu@gmail.com', '1992-01-15', 'https://i.pravatar.cc/300?img=38', @CustPwdHash, 1, 1, DATEADD(MONTH, -3, @UtcNow3b)),
    (1, 1, N'Minh Tuan Do', '0912001004', 'mtuando@outlook.com', '1985-08-10', 'https://i.pravatar.cc/300?img=60', @CustPwdHash, 1, 1, DATEADD(MONTH, -2, @UtcNow3b)),
    (1, 2, N'Thu Ha Hoang', '0912001005', 'thuha.hoang@gmail.com', '1990-12-05', 'https://i.pravatar.cc/300?img=45', @CustPwdHash, 1, 1, DATEADD(MONTH, -2, @UtcNow3b));

IF NOT EXISTS (SELECT 1 FROM [dbo].[Accounts] WHERE Email = 'khoalmce181686@fpt.edu.vn')
    INSERT INTO [dbo].[Accounts]
    (RoleID, SexID, AccountName, PhoneNumber, Email, DOB, ImageURL, PasswordHash, HasPassword, IsActive, CreatedAt)
VALUES
    (1, 1, N'Le Minh Khoa', '0912001999', 'khoalmce181686@fpt.edu.vn', '2000-01-01', 'https://i.pravatar.cc/300?img=12', @CustPwdHash, 1, 1, DATEADD(MONTH, -4, @UtcNow3b));
GO

PRINT N'[3] Accounts – New Customers (40 users, Jul-Aug 2026)...';
DECLARE @PwdHash3c VARCHAR(64) = UPPER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'Customer@123456'), 2));

IF NOT EXISTS (SELECT 1
FROM [dbo].[Accounts]
WHERE Email = 'quynhanh.le@gmail.com')
    INSERT INTO [dbo].[Accounts]
    (RoleID, SexID, AccountName, PhoneNumber, Email, DOB, ImageURL, PasswordHash, HasPassword, IsActive, CreatedAt)
SELECT
    1, v.SexID, v.AccountName, v.PhoneNumber, v.Email, v.DOB,
    'https://i.pravatar.cc/300?img=' + CAST(v.AvatarSeed AS VARCHAR),
    @PwdHash3c, 1, 1, v.CreatedAt
FROM (VALUES
    -- ── July 2026 (20 accounts) ──────────────────────────────────
    (2, N'Quynh Anh Le',      '0912002001', 'quynhanh.le@gmail.com',      '1996-02-14', 21, CAST('2026-07-01 02:15:00' AS DATETIME2(0))),
    (1, N'Duc Thanh Pham',    '0912002002', 'ducthanh.pham@gmail.com',    '1991-05-09', 22, CAST('2026-07-02 03:40:00' AS DATETIME2(0))),
    (2, N'Hai Yen Vu',        '0912002003', 'haiyen.vu@gmail.com',        '1998-11-23', 23, CAST('2026-07-03 07:05:00' AS DATETIME2(0))),
    (1, N'Van Long Nguyen',   '0912002004', 'vanlong.nguyen@gmail.com',   '1989-08-17', 24, CAST('2026-07-04 09:20:00' AS DATETIME2(0))),
    (2, N'Thi Mai Tran',      '0912002005', 'thimai.tran@gmail.com',      '1994-01-30', 25, CAST('2026-07-05 11:10:00' AS DATETIME2(0))),
    (1, N'Quoc Bao Hoang',    '0912002006', 'quocbao.hoang@gmail.com',    '1993-06-12', 26, CAST('2026-07-06 13:45:00' AS DATETIME2(0))),
    (2, N'Ngoc Han Do',       '0912002007', 'ngochan.do@gmail.com',       '1997-03-25', 27, CAST('2026-07-07 15:00:00' AS DATETIME2(0))),
    (1, N'Thanh Tung Le',     '0912002008', 'thanhtung.le@gmail.com',     '1990-09-08', 28, CAST('2026-07-08 16:30:00' AS DATETIME2(0))),
    (2, N'Kim Ngan Vo',       '0912002009', 'kimngan.vo@gmail.com',       '1999-04-19', 29, CAST('2026-07-09 18:50:00' AS DATETIME2(0))),
    (1, N'Xuan Truong Bui',   '0912002010', 'xuantruong.bui@gmail.com',   '1988-12-05', 30, CAST('2026-07-10 20:10:00' AS DATETIME2(0))),
    (2, N'Phuong Thao Dang',  '0912002011', 'phuongthao.dang@gmail.com',  '1995-07-27', 31, CAST('2026-07-12 08:05:00' AS DATETIME2(0))),
    (1, N'Anh Khoa Ly',       '0912002012', 'anhkhoa.ly@gmail.com',       '1992-10-14', 32, CAST('2026-07-13 09:35:00' AS DATETIME2(0))),
    (2, N'Bich Ngoc Trinh',   '0912002013', 'bichngoc.trinh@gmail.com',   '1996-02-02', 33, CAST('2026-07-14 10:55:00' AS DATETIME2(0))),
    (1, N'Minh Quan Ta',      '0912002014', 'minhquan.ta@gmail.com',      '1991-11-11', 34, CAST('2026-07-16 12:20:00' AS DATETIME2(0))),
    (2, N'Thu Trang Chau',    '0912002015', 'thutrang.chau@gmail.com',    '1997-08-06', 35, CAST('2026-07-18 14:00:00' AS DATETIME2(0))),
    (1, N'Gia Huy Phan',      '0912002016', 'giahuy.phan@gmail.com',      '1990-05-21', 36, CAST('2026-07-20 16:15:00' AS DATETIME2(0))),
    (2, N'Diem My Huynh',     '0912002017', 'diemmy.huynh@gmail.com',     '1998-01-09', 37, CAST('2026-07-22 17:45:00' AS DATETIME2(0))),
    (1, N'Cong Danh Vuong',   '0912002018', 'congdanh.vuong@outlook.com', '1989-09-30', 38, CAST('2026-07-25 19:05:00' AS DATETIME2(0))),
    (2, N'Le Na Duong',       '0912002019', 'lena.duong@gmail.com',       '1994-04-16', 39, CAST('2026-07-28 20:40:00' AS DATETIME2(0))),
    (1, N'Trong Nhan Mai',    '0912002020', 'trongnhan.mai@outlook.com',  '1993-06-03', 40, CAST('2026-07-31 22:00:00' AS DATETIME2(0))),

    -- ── August 1–7, 2026 (20 accounts, before Aug 8) ─────────────
    (2, N'Bao Ngoc Nguyen',   '0912002021', 'baongoc.nguyen@gmail.com',   '1997-02-18', 41, CAST('2026-08-01 00:30:00' AS DATETIME2(0))),
    (1, N'Hoang Yen Pham',    '0912002022', 'hoangyen.pham@gmail.com',    '1992-07-24', 42, CAST('2026-08-01 05:15:00' AS DATETIME2(0))),
    (2, N'Duy Khang Tran',    '0912002023', 'duykhang.tran@gmail.com',    '1996-03-11', 43, CAST('2026-08-01 10:00:00' AS DATETIME2(0))),
    (1, N'Thuy Duong Le',     '0912002024', 'thuyduong.le@gmail.com',     '1990-10-05', 44, CAST('2026-08-01 14:45:00' AS DATETIME2(0))),
    (2, N'Nhat Minh Vo',      '0912002025', 'nhatminh.vo@gmail.com',      '1995-12-27', 45, CAST('2026-08-02 02:20:00' AS DATETIME2(0))),
    (1, N'Cam Tu Hoang',      '0912002026', 'camtu.hoang@gmail.com',      '1991-04-08', 46, CAST('2026-08-02 07:50:00' AS DATETIME2(0))),
    (2, N'Viet Anh Do',       '0912002027', 'vietanh.do@gmail.com',       '1998-08-19', 47, CAST('2026-08-02 13:10:00' AS DATETIME2(0))),
    (1, N'Kieu Trinh Bui',    '0912002028', 'kieutrinh.bui@gmail.com',    '1989-01-22', 48, CAST('2026-08-02 18:30:00' AS DATETIME2(0))),
    (2, N'Thanh Nhan Dang',   '0912002029', 'thanhnhan.dang@gmail.com',   '1994-05-14', 49, CAST('2026-08-03 03:00:00' AS DATETIME2(0))),
    (1, N'Huu Phuoc Ly',      '0912002030', 'huuphuoc.ly@outlook.com',    '1993-09-02', 50, CAST('2026-08-03 09:25:00' AS DATETIME2(0))),
    (2, N'My Linh Trinh',     '0912002031', 'mylinh.trinh@gmail.com',     '1997-11-16', 51, CAST('2026-08-03 15:40:00' AS DATETIME2(0))),
    (1, N'Dinh Khoi Ta',      '0912002032', 'dinhkhoi.ta@gmail.com',      '1990-06-29', 52, CAST('2026-08-04 01:10:00' AS DATETIME2(0))),
    (2, N'Ai Nhi Chau',       '0912002033', 'ainhi.chau@gmail.com',       '1999-02-07', 53, CAST('2026-08-04 06:35:00' AS DATETIME2(0))),
    (1, N'Van Hao Phan',      '0912002034', 'vanhao.phan@gmail.com',      '1988-10-20', 54, CAST('2026-08-04 12:00:00' AS DATETIME2(0))),
    (2, N'Tuong Vi Huynh',    '0912002035', 'tuongvi.huynh@gmail.com',    '1996-07-13', 55, CAST('2026-08-05 04:20:00' AS DATETIME2(0))),
    (1, N'Ich Tam Vuong',     '0912002036', 'ichtam.vuong@gmail.com',     '1992-03-04', 56, CAST('2026-08-05 11:45:00' AS DATETIME2(0))),
    (1, N'Bao Tran Duong',    '0912002037', 'baotran.duong@outlook.com',  '1991-12-31', 57, CAST('2026-08-06 08:15:00' AS DATETIME2(0))),
    (2, N'Khanh Linh Mai',    '0912002038', 'khanhlinh.mai@gmail.com',    '1995-09-09', 58, CAST('2026-08-06 16:50:00' AS DATETIME2(0))),
    (1, N'Duc Anh Nguyen',    '0912002039', 'ducanh.nguyen2@gmail.com',   '1990-01-25', 59, CAST('2026-08-07 05:05:00' AS DATETIME2(0))),
    (2, N'Yen Nhi Pham',      '0912002040', 'yennhi.pham2@gmail.com',     '1997-06-18', 60, CAST('2026-08-07 21:30:00' AS DATETIME2(0)))
) AS v(SexID, AccountName, PhoneNumber, Email, DOB, AvatarSeed, CreatedAt);
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 5 – LOOKUP TABLES
══════════════════════════════════════════════════════════════ */
PRINT N'[5] Lookup tables...';
DECLARE @LookupCreated5 DATETIME2(0) = DATEADD(MONTH, -6, (SELECT UtcNow
FROM #SeedTime));

SET IDENTITY_INSERT [dbo].[SuperCategories] ON;
IF NOT EXISTS (SELECT 1
FROM [dbo].[SuperCategories]
WHERE SuperCategoryID = 1)
    INSERT INTO [dbo].[SuperCategories]
    (SuperCategoryID, SuperCategoryName, CreatedAt)
VALUES
    (1, N'Building Toys', @LookupCreated5),
    (2, N'Educational Toys', @LookupCreated5),
    (3, N'Outdoor Activities', @LookupCreated5),
    (4, N'Dolls & Plush Toys', @LookupCreated5),
    (5, N'Figures & Models', @LookupCreated5),
    (6, N'Remote Control Toys', @LookupCreated5);
SET IDENTITY_INSERT [dbo].[SuperCategories] OFF;

SET IDENTITY_INSERT [dbo].[Categories] ON;
IF NOT EXISTS (SELECT 1
FROM [dbo].[Categories]
WHERE CategoryID = 1)
    INSERT INTO [dbo].[Categories]
    (CategoryID, SuperCategoryID, CategoryName, CreatedAt)
VALUES
    ( 1, 1, N'Lego', @LookupCreated5),
    ( 2, 1, N'Building Blocks', @LookupCreated5),
    ( 3, 2, N'Flash Cards', @LookupCreated5),
    ( 4, 2, N'Science Toys', @LookupCreated5),
    ( 5, 3, N'Ride-On Toys', @LookupCreated5),
    ( 6, 3, N'Balls', @LookupCreated5),
    ( 7, 3, N'Kites', @LookupCreated5),
    ( 8, 4, N'Teddy Bears & Plush', @LookupCreated5),
    ( 9, 4, N'Fashion Dolls', @LookupCreated5),
    (10, 5, N'Superheroes', @LookupCreated5),
    (11, 5, N'Dinosaurs', @LookupCreated5),
    (12, 6, N'RC Cars', @LookupCreated5),
    (13, 6, N'RC Airplanes', @LookupCreated5);
SET IDENTITY_INSERT [dbo].[Categories] OFF;

SET IDENTITY_INSERT [dbo].[Materials] ON;
IF NOT EXISTS (SELECT 1
FROM [dbo].[Materials]
WHERE MaterialID = 1)
    INSERT INTO [dbo].[Materials]
    (MaterialID, MaterialName, CreatedAt)
VALUES
    (1, N'Premium ABS Plastic', @LookupCreated5),
    (2, N'Natural MDF Wood', @LookupCreated5),
    (3, N'Cotton & Velvet Fabric', @LookupCreated5),
    (4, N'Aluminum Alloy', @LookupCreated5),
    (5, N'Natural Rubber', @LookupCreated5);
SET IDENTITY_INSERT [dbo].[Materials] OFF;

SET IDENTITY_INSERT [dbo].[Ages] ON;
IF NOT EXISTS (SELECT 1
FROM [dbo].[Ages]
WHERE AgeID = 1)
    INSERT INTO [dbo].[Ages]
    (AgeID, AgeRange, CreatedAt)
VALUES
    (1, N'0-1', @LookupCreated5),
    (2, N'1-3', @LookupCreated5),
    (3, N'3-6', @LookupCreated5),
    (4, N'6-12', @LookupCreated5),
    (5, N'12+', @LookupCreated5);
SET IDENTITY_INSERT [dbo].[Ages] OFF;

SET IDENTITY_INSERT [dbo].[Origins] ON;
IF NOT EXISTS (SELECT 1
FROM [dbo].[Origins]
WHERE OriginID = 1)
    INSERT INTO [dbo].[Origins]
    (OriginID, OriginName, CreatedAt)
VALUES
    (1, N'Vietnam', @LookupCreated5),
    (2, N'China', @LookupCreated5),
    (3, N'United States', @LookupCreated5),
    (4, N'Japan', @LookupCreated5),
    (5, N'Denmark', @LookupCreated5);
SET IDENTITY_INSERT [dbo].[Origins] OFF;

SET IDENTITY_INSERT [dbo].[Brands] ON;
IF NOT EXISTS (SELECT 1
FROM [dbo].[Brands]
WHERE BrandID = 1)
    INSERT INTO [dbo].[Brands]
    (BrandID, BrandName, CreatedAt)
VALUES
    (1, N'Lego', @LookupCreated5),
    (2, N'Fisher-Price', @LookupCreated5),
    (3, N'Hot Wheels', @LookupCreated5),
    (4, N'Polo', @LookupCreated5),
    (5, N'MyKingdom', @LookupCreated5),
    (6, N'Bandai', @LookupCreated5),
    (7, N'Hasbro', @LookupCreated5),
    (8, N'Mattel', @LookupCreated5);
SET IDENTITY_INSERT [dbo].[Brands] OFF;

SET IDENTITY_INSERT [dbo].[PriceRanges] ON;
IF NOT EXISTS (SELECT 1
FROM [dbo].[PriceRanges]
WHERE PriceRangeID = 1)
    INSERT INTO [dbo].[PriceRanges]
    (PriceRangeID, PriceRangeMin, PriceRangeMax, CreatedAt)
VALUES
    (1, 0, 199000, @LookupCreated5),
    (2, 200000, 499000, @LookupCreated5),
    (3, 500000, 999000, @LookupCreated5),
    (4, 1000000, 1999000, @LookupCreated5),
    (5, 2000000, 4999000, @LookupCreated5),
    (6, 5000000, 9999000, @LookupCreated5),
    (7, 10000000, 999999000, @LookupCreated5);
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
    (1, 'like', N'Like', @LookupCreated5),
    (2, 'love', N'Love', @LookupCreated5),
    (3, 'haha', N'Haha', @LookupCreated5),
    (4, 'wow', N'Wow', @LookupCreated5),
    (5, 'sad', N'Sad', @LookupCreated5);
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
   SECTION 7.1 – ORDER REFUND REASONS
══════════════════════════════════════════════════════════════ */
PRINT N'[7.1] OrderRefundReasons...';
DECLARE @NowRefundReasons DATETIME2(0) = GETUTCDATE();

IF NOT EXISTS (SELECT 1
FROM [dbo].[OrderRefundReasons]
WHERE [Content] = N'Delivery failed / Unable to deliver')
    INSERT INTO [dbo].[OrderRefundReasons]
    ([Content], [Description], [ResponsibleParty], [IsSystem], [IsDeleted], [CreatedAt])
VALUES
    (N'Delivery failed / Unable to deliver', N'GHN failed to deliver the package to the customer.', 'Store', 1, 0, @NowRefundReasons);

IF NOT EXISTS (SELECT 1
FROM [dbo].[OrderRefundReasons]
WHERE [Content] = N'Wrong product delivered')
    INSERT INTO [dbo].[OrderRefundReasons]
    ([Content], [Description], [ResponsibleParty], [IsSystem], [IsDeleted], [CreatedAt])
VALUES
    (N'Wrong product delivered', N'The received product does not match the order.', 'Store', 0, 0, @NowRefundReasons);

IF NOT EXISTS (SELECT 1
FROM [dbo].[OrderRefundReasons]
WHERE [Content] = N'Defective / Damaged product')
    INSERT INTO [dbo].[OrderRefundReasons]
    ([Content], [Description], [ResponsibleParty], [IsSystem], [IsDeleted], [CreatedAt])
VALUES
    (N'Defective / Damaged product', N'The product is defective or damaged due to manufacturing issues.', 'Store', 0, 0, @NowRefundReasons);

IF NOT EXISTS (SELECT 1
FROM [dbo].[OrderRefundReasons]
WHERE [Content] = N'Missing item')
    INSERT INTO [dbo].[OrderRefundReasons]
    ([Content], [Description], [ResponsibleParty], [IsSystem], [IsDeleted], [CreatedAt])
VALUES
    (N'Missing item', N'The quantity received is less than the quantity ordered.', 'Store', 0, 0, @NowRefundReasons);

IF NOT EXISTS (SELECT 1
FROM [dbo].[OrderRefundReasons]
WHERE [Content] = N'Product not as described')
    INSERT INTO [dbo].[OrderRefundReasons]
    ([Content], [Description], [ResponsibleParty], [IsSystem], [IsDeleted], [CreatedAt])
VALUES
    (N'Product not as described', N'The product differs from the images or description on the website.', 'Store', 0, 0, @NowRefundReasons);

IF NOT EXISTS (SELECT 1
FROM [dbo].[OrderRefundReasons]
WHERE [Content] = N'No longer needed')
    INSERT INTO [dbo].[OrderRefundReasons]
    ([Content], [Description], [ResponsibleParty], [IsSystem], [IsDeleted], [CreatedAt])
VALUES
    (N'No longer needed', N'The customer changed their mind after placing the order.', 'Customer', 0, 0, @NowRefundReasons);

IF NOT EXISTS (SELECT 1
FROM [dbo].[OrderRefundReasons]
WHERE [Content] = N'Ordered the wrong product')
    INSERT INTO [dbo].[OrderRefundReasons]
    ([Content], [Description], [ResponsibleParty], [IsSystem], [IsDeleted], [CreatedAt])
VALUES
    (N'Ordered the wrong product', N'The customer selected the wrong product when placing the order.', 'Customer', 0, 0, @NowRefundReasons);

IF NOT EXISTS (SELECT 1
FROM [dbo].[OrderRefundReasons]
WHERE [Content] = N'Wrong size / color selected')
    INSERT INTO [dbo].[OrderRefundReasons]
    ([Content], [Description], [ResponsibleParty], [IsSystem], [IsDeleted], [CreatedAt])
VALUES
    (N'Wrong size / color selected', N'The customer selected the wrong product variant.', 'Customer', 0, 0, @NowRefundReasons);
GO
/* ══════════════════════════════════════════════════════════════
   SECTION 8 – PRODUCTS (30 products)
══════════════════════════════════════════════════════════════ */
PRINT N'[8] Products...';
DECLARE @UtcNow8 DATETIME2(0) = (SELECT UtcNow
FROM #SeedTime);
DECLARE @UtcToday8 DATE = (SELECT UtcToday
FROM #SeedTime);
DECLARE @ProductBase DATETIME2(0) = DATEADD(MONTH, -5, @UtcNow8);

SET IDENTITY_INSERT [dbo].[Products] ON;

IF NOT EXISTS (SELECT 1
FROM [dbo].[Products]
WHERE ProductID = 1)
    INSERT INTO [dbo].[Products]
    (ProductID,ProductName,Price,Quantity,ProductStatus,CategoryID,BrandID,PriceRangeID,StockThreshold,CreatedAt)
VALUES
    ( 1, N'Lego City Central Police Station - 668 Pieces', 1290000, 45, 'Active', 1, 1, 4, 10, DATEADD(DAY, 0, @ProductBase)),
    ( 2, N'Lego Technic Bugatti Chiron Supercar - 3599 Pieces', 5990000, 8, 'Active', 1, 1, 6, 3, DATEADD(DAY, 2, @ProductBase)),
    ( 3, N'Lego Friends Resort House - 699 Pieces', 1590000, 22, 'Active', 1, 1, 4, 5, DATEADD(DAY, 4, @ProductBase)),
    ( 4, N'Wooden Transportation Puzzle Blocks - 36 Pieces', 420000, 180, 'Active', 2, 5, 2, 20, DATEADD(DAY, 7, @ProductBase)),
    ( 5, N'Wooden Alphabet Blocks - 52 Pieces', 350000, 200, 'Active', 2, 5, 2, 30, DATEADD(DAY, 9, @ProductBase)),
    ( 6, N'4D Smart Flash Cards - Wild Animals (120 Cards)', 145000, 600, 'Active', 3, 2, 1, 50, DATEADD(DAY, 11, @ProductBase)),
    ( 7, N'Math Flash Cards - Numbers 1-100', 185000, 450, 'Active', 3, 5, 1, 50, DATEADD(DAY, 13, @ProductBase)),
    ( 8, N'ScienceMax Microscope 40x-400x', 375000, 75, 'Active', 4, 5, 2, 8, DATEADD(DAY, 28, @ProductBase)),
    ( 9, N'Erupting Volcano Experiment Kit', 280000, 120, 'Active', 4, 5, 2, 15, DATEADD(DAY, 30, @ProductBase)),
    (10, N'Kids Telescope Set 50x/100x', 490000, 55, 'Active', 4, 2, 2, 8, DATEADD(DAY, 32, @ProductBase)),
    (11, N'Donald Duck Ride-On Toy with Lights and Music', 545000, 38, 'Active', 5, 2, 3, 5, DATEADD(DAY, 37, @ProductBase)),
    (12, N'Disney Princess Tricycle with Push Handle', 890000, 18, 'Active', 5, 8, 3, 3, DATEADD(DAY, 39, @ProductBase)),
    (13, N'Boho Pattern Rubber Ball - Size 5', 85000, 320, 'Active', 6, 2, 1, 30, DATEADD(DAY, 42, @ProductBase)),
    (14, N'FIFA Pro Vulcanized Soccer Ball - Size 4', 165000, 250, 'Active', 6, 4, 1, 25, DATEADD(DAY, 44, @ProductBase)),
    (15, N'Large Eagle Kite 1.4 m with 30 m String', 115000, 140, 'Active', 7, 2, 1, 15, DATEADD(DAY, 47, @ProductBase)),
    (16, N'3D Dragon Kite with Carbon Frame', 185000, 90, 'Active', 7, 2, 1, 10, DATEADD(DAY, 49, @ProductBase)),
    (17, N'Green T-Rex Dinosaur Plush Toy - 80 cm', 840000, 28, 'Active', 8, 5, 3, 4, DATEADD(DAY, 58, @ProductBase)),
    (18, N'Panda Plush Toy - 60 cm (Lying)', 650000, 35, 'Active', 8, 5, 3, 5, DATEADD(DAY, 60, @ProductBase)),
    (19, N'Pastel Long-Eared Bunny Plush Toy - 45 cm', 420000, 60, 'Active', 8, 5, 2, 8, DATEADD(DAY, 62, @ProductBase)),
    (20, N'Barbie Dreamtopia Mermaid Doll with 3 Outfits', 359000, 110, 'Active', 9, 8, 2, 12, DATEADD(DAY, 65, @ProductBase)),
    (21, N'Barbie Fashionista Set - 6 Outfits', 490000, 75, 'Active', 9, 8, 2, 8, DATEADD(DAY, 67, @ProductBase)),
    (22, N'Gao Red Ranger Action Figure - 24 Points', 595000, 22, 'Active', 10, 6, 3, 4, DATEADD(DAY, 69, @ProductBase)),
    (23, N'Kamen Rider Zero-One SHFiguarts Figure', 890000, 15, 'Active', 10, 6, 3, 3, DATEADD(DAY, 71, @ProductBase)),
    (24, N'T-Rex Dinosaur Figure - 1:10 Scale', 395000, 88, 'Active', 11, 3, 2, 10, DATEADD(DAY, 73, @ProductBase)),
    (25, N'Jurassic World Mini Dinosaur Set - 6 Pack', 285000, 150, 'Active', 11, 7, 2, 15, DATEADD(DAY, 75, @ProductBase)),
    (26, N'RC Off-Road Car Traxxas TRX-Mini 4x4', 945000, 55, 'Active', 12, 3, 3, 6, DATEADD(DAY, 88, @ProductBase)),
    (27, N'RC Drift Car - 2.4GHz', 680000, 40, 'Active', 12, 3, 3, 6, DATEADD(DAY, 90, @ProductBase)),
    (28, N'Mini Gyro RC Helicopter - 2.4GHz', 1490000, 12, 'Active', 13, 6, 4, 2, DATEADD(DAY, 97, @ProductBase)),
    (29, N'Play-Doh 24-Color Non-Toxic Modeling Clay', 88000, 980, 'Active', 4, 7, 1, 80, DATEADD(DAY, 102, @ProductBase)),
    (30, N'10-Inch LCD Writing Tablet with Stylus', 175000, 380, 'Active', 4, 2, 1, 40, DATEADD(DAY, 107, @ProductBase));

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
    (15, N'Eagle kite with 1.4m wingspan, carbon frame, and 210T polyester fabric.', 3, 4, 1, 1, 280, 140, 10, 5),
    (16, N'3D dragon kite with 1.8 m wingspan and a carbon frame.', 3, 4, 1, 1, 350, 180, 12, 6),

    -- Plush toys (17-19)
    (17, N'Rex dinosaur plush toy 80 cm with soft velvet fabric and PP cotton stuffing.', 3, 2, 2, 2, 900, 80, 35, 40),
    (18, N'Panda plush toy 60 cm in a lying position. Velvet fabric with PP stuffing. Machine washable.', 3, 2, 3, 2, 680, 60, 30, 25),
    (19, N'Pastel long-eared bunny plush toy 45 cm. Soft velvet fabric. Includes gift box.', 3, 2, 2, 2, 480, 45, 20, 20),

    -- Barbie dolls (20-21)
    (20, N'Mattel Barbie Dreamtopia mermaid with purple-pink gradient hair and 3 outfits.', 1, 3, 2, 3, 280, 32, 10, 30),
    (21, N'Barbie Fashionista set with 6 fashionable outfits. Suitable for ages 3+.', 1, 3, 2, 3, 320, 33, 12, 30),

    -- Superhero figures (22-23)
    (22, N'Bandai Gao Red Ranger figure, 18 cm height with 24 articulation points.', 1, 5, 1, 2, 180, 12, 8, 18),
    (23, N'Kamen Rider Zero-One SHFiguarts figure 15 cm tall with 30 joints. Includes 8 interchangeable hands.', 1, 5, 1, 2, 160, 10, 8, 15),

    -- Dinosaurs (24-25)
    (24, N'T-Rex figure 1:10 scale, 25 cm height, 45 cm length. Aluminum alloy and ABS plastic. Spring-loaded jaw.', 4, 4, 1, 3, 520, 45, 18, 25),
    (25, N'Set of 6 mini dinosaur figures, height 8-12 cm. Soft ABS plastic.', 1, 3, 1, 3, 350, 30, 20, 8),

    -- RC cars (26-27)
    (26, N'RC Traxxas TRX-Mini 1:16 with a brushless 2838KV motor, max speed 45 km/h. LiPo battery included.', 4, 4, 1, 3, 680, 36, 22, 14),
    (27, N'RC drift car with CNC aluminum wheels. Speed up to 30 km/h. USB charging in 90 minutes.', 4, 4, 1, 2, 520, 34, 20, 12),

    -- RC helicopter (28)
    (28, N'4-channel 2.4GHz RC gyro helicopter with 6-axis stabilization. Flight time 12-15 minutes.', 1, 5, 1, 2, 280, 32, 32, 12),

    -- Clay / drawing board (29-30)
    (29, N'Hasbro Play-Doh set with 24 colors, each jar 85 g. Non-toxic and gluten-free.', 1, 3, 3, 3, 2040, 35, 24, 8),
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
        DECLARE @ImgCreated DATETIME2(0) = (SELECT UtcNow
        FROM #SeedTime);

        INSERT INTO [dbo].[ProductImages]
            (ProductID, ImageUrl, IsMain, CreatedAt)
        VALUES
            (@i, 'https://picsum.photos/seed/' + @seed + '-a/400/400', 1, @ImgCreated),
            (@i, 'https://picsum.photos/seed/' + @seed + '-b/400/400', 0, @ImgCreated),
            (@i, 'https://picsum.photos/seed/' + @seed + '-c/400/400', 0, @ImgCreated),
            (@i, 'https://picsum.photos/seed/' + @seed + '-d/400/400', 0, @ImgCreated);

        SET @i = @i + 1;
    END
END
GO
/* ══════════════════════════════════════════════════════════════
   SECTION 13 – PROMOTIONS
══════════════════════════════════════════════════════════════ */
PRINT N'[13] Promotions...';

DECLARE @admID INT = (SELECT TOP 1
    AccountID
FROM Accounts
WHERE Email = 'admintoystore@gmail.com');
DECLARE @UtcNow DATETIME2(0) = (SELECT UtcNow
FROM #SeedTime);
DECLARE @UtcToday DATE = (SELECT UtcToday
FROM #SeedTime);

IF NOT EXISTS (SELECT 1
FROM [dbo].[Promotions]
WHERE PromotionName = N'Summer Toy Festival 2026')
    INSERT INTO [dbo].[Promotions]
    (CreatedBy, PromotionName, PromotionType, Description, StartDate, EndDate, Status, Priority, CreatedAt)
VALUES
    (@admID, N'Summer Toy Festival 2026', 'DISCOUNT',
        N'Massive summer discounts on selected toy collections.',
        DATEADD(DAY, -23, @UtcToday), DATEADD(DAY, 68, @UtcToday), 'Active', 10, @UtcNow),

    (@admID, N'Back To School 2026', 'DISCOUNT',
        N'Special discounts on educational toys and learning cards.',
        DATEADD(DAY, 21, @UtcToday), DATEADD(DAY, 83, @UtcToday), 'Scheduled', 8, @UtcNow),

    (@admID, N'Lego Builders Week', 'DISCOUNT',
        N'Exclusive discounts on all Lego sets and building blocks.',
        DATEADD(DAY, 7, @UtcToday), DATEADD(DAY, 14, @UtcToday), 'Scheduled', 14, @UtcNow),

    (@admID, N'Mega Flash Sale', 'FLASH_SALE',
        N'Limited-time flash sale with discounts up to 50%.',
        DATEADD(DAY, -7, @UtcToday), DATEADD(DAY, 90, @UtcToday), 'Active', 20, @UtcNow),

    (@admID, N'Family Day Flash Sale', 'FLASH_SALE',
        N'Special offers celebrating Family Day.',
        @UtcToday, DATEADD(DAY, 60, @UtcToday), 'Active', 15, @UtcNow),

    (@admID, N'Weekend Flash Blitz', 'FLASH_SALE',
        N'48-hour weekend blitz – deep cuts on bestsellers while stocks last.',
        DATEADD(DAY, -3, @UtcToday), DATEADD(DAY, 60, @UtcToday), 'Active', 18, @UtcNow),

    (@admID, N'Halloween Spooktacular', 'DISCOUNT',
        N'Spooky season deals on figures, dolls, and dress-up toys.',
        DATEADD(DAY, 99, @UtcToday), DATEADD(DAY, 129, @UtcToday), 'Scheduled', 12, @UtcNow),

    (@admID, N'Black Friday Preview', 'DISCOUNT',
        N'Early Black Friday deals across Lego, Barbie, and action figures.',
        DATEADD(DAY, 130, @UtcToday), DATEADD(DAY, 159, @UtcToday), 'Scheduled', 16, @UtcNow),

    (@admID, N'Christmas & New Year 2026', 'DISCOUNT',
        N'Holiday season discounts up to 40% on figures and dolls.',
        DATEADD(DAY, 160, @UtcToday), DATEADD(DAY, 190, @UtcToday), 'Scheduled', 12, @UtcNow),

    (@admID, N'Spring Clearance 2026', 'DISCOUNT',
        N'End-of-season clearance on outdoor and STEM toys.',
        DATEADD(DAY, -90, @UtcToday), DATEADD(DAY, -60, @UtcToday), 'Expired', 5, DATEADD(DAY, -90, @UtcNow)),

    (@admID, N'RC Expo (Archived)', 'DISCOUNT',
        N'Former RC & racing promotion – manually deactivated.',
        DATEADD(DAY, -120, @UtcToday), DATEADD(DAY, -90, @UtcToday), 'Inactive', 4, DATEADD(DAY, -120, @UtcNow)),

    (@admID, N'Plush Toy Carnival', 'DISCOUNT',
        N'Soft toy specials on teddy bears, pandas, and bunny plushies.',
        DATEADD(DAY, -14, @UtcToday), DATEADD(DAY, 45, @UtcToday), 'Active', 9, @UtcNow),

    (@admID, N'Outdoor Play Month', 'DISCOUNT',
        N'Discounts on ride-on toys, balls, and kites for active kids.',
        DATEADD(DAY, -10, @UtcToday), DATEADD(DAY, 50, @UtcToday), 'Active', 7, @UtcNow),

    (@admID, N'Science & STEM Sale', 'DISCOUNT',
        N'Microscopes, telescopes, and experiment kits at reduced prices.',
        DATEADD(DAY, -10, @UtcToday), DATEADD(DAY, 50, @UtcToday), 'Active', 6, @UtcNow);
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 14 – PROMOTION TIME SLOTS
══════════════════════════════════════════════════════════════ */
PRINT N'[14] PromotionTimeSlots...';

IF NOT EXISTS (SELECT 1
FROM [dbo].[PromotionTimeSlots])
BEGIN
    DECLARE @UtcNow14 DATETIME2(0) = (SELECT UtcNow
    FROM #SeedTime);
    DECLARE @UtcToday14 DATE = (SELECT UtcToday
    FROM #SeedTime);

    DECLARE @VnLunchStart DATETIME2(0) = DATEADD(HOUR, 11, CAST(@UtcToday14 AS DATETIME2(0)));
    DECLARE @VnLunchEnd   DATETIME2(0) = DATEADD(HOUR, 14, CAST(@UtcToday14 AS DATETIME2(0)));
    DECLARE @VnEvenStart  DATETIME2(0) = DATEADD(HOUR, 19, CAST(@UtcToday14 AS DATETIME2(0)));
    DECLARE @VnEvenEnd    DATETIME2(0) = DATEADD(HOUR, 22, CAST(@UtcToday14 AS DATETIME2(0)));
    DECLARE @VnWkndStart  DATETIME2(0) = DATEADD(HOUR, 10, CAST(DATEADD(DAY, 1, @UtcToday14) AS DATETIME2(0)));
    DECLARE @VnWkndEnd    DATETIME2(0) = DATEADD(HOUR, 13, CAST(DATEADD(DAY, 1, @UtcToday14) AS DATETIME2(0)));
    DECLARE @VnYestStart  DATETIME2(0) = DATEADD(HOUR, 11, CAST(DATEADD(DAY, -1, @UtcToday14) AS DATETIME2(0)));
    DECLARE @VnYestEnd    DATETIME2(0) = DATEADD(HOUR, 14, CAST(DATEADD(DAY, -1, @UtcToday14) AS DATETIME2(0)));

    DECLARE @LunchStartUtc DATETIME2(0) = CAST((@VnLunchStart AT TIME ZONE 'SE Asia Standard Time') AT TIME ZONE 'UTC' AS DATETIME2(0));
    DECLARE @LunchEndUtc   DATETIME2(0) = CAST((@VnLunchEnd   AT TIME ZONE 'SE Asia Standard Time') AT TIME ZONE 'UTC' AS DATETIME2(0));
    DECLARE @EvenStartUtc  DATETIME2(0) = CAST((@VnEvenStart  AT TIME ZONE 'SE Asia Standard Time') AT TIME ZONE 'UTC' AS DATETIME2(0));
    DECLARE @EvenEndUtc    DATETIME2(0) = CAST((@VnEvenEnd    AT TIME ZONE 'SE Asia Standard Time') AT TIME ZONE 'UTC' AS DATETIME2(0));
    DECLARE @WkndStartUtc  DATETIME2(0) = CAST((@VnWkndStart  AT TIME ZONE 'SE Asia Standard Time') AT TIME ZONE 'UTC' AS DATETIME2(0));
    DECLARE @WkndEndUtc    DATETIME2(0) = CAST((@VnWkndEnd    AT TIME ZONE 'SE Asia Standard Time') AT TIME ZONE 'UTC' AS DATETIME2(0));
    DECLARE @YestStartUtc  DATETIME2(0) = CAST((@VnYestStart  AT TIME ZONE 'SE Asia Standard Time') AT TIME ZONE 'UTC' AS DATETIME2(0));
    DECLARE @YestEndUtc    DATETIME2(0) = CAST((@VnYestEnd    AT TIME ZONE 'SE Asia Standard Time') AT TIME ZONE 'UTC' AS DATETIME2(0));

    DECLARE @LiveStartUtc DATETIME2(0) = @UtcNow14;
    DECLARE @LiveEndUtc   DATETIME2(0) = DATEADD(HOUR, 3, @UtcNow14);

    DECLARE @LunchStatus VARCHAR(20) = CASE
        WHEN @UtcNow14 >= @LunchStartUtc AND @UtcNow14 < @LunchEndUtc THEN 'Active'
        WHEN @UtcNow14 >= @LunchEndUtc THEN 'Expired'
        ELSE 'Scheduled' END;

    DECLARE @pFlash1 INT = (SELECT TOP 1
        PromotionID
    FROM Promotions
    WHERE PromotionName = N'Mega Flash Sale');
    DECLARE @pFlash2 INT = (SELECT TOP 1
        PromotionID
    FROM Promotions
    WHERE PromotionName = N'Family Day Flash Sale');
    DECLARE @pFlash3 INT = (SELECT TOP 1
        PromotionID
    FROM Promotions
    WHERE PromotionName = N'Weekend Flash Blitz');

    INSERT INTO [dbo].[PromotionTimeSlots]
        (PromotionID, StartAt, EndAt, Status, CreatedAt)
    VALUES
        -- Mega Flash Sale: only campaign with a "live now" demo slot + full slot lifecycle
        (@pFlash1, @LiveStartUtc, @LiveEndUtc, 'Active', @UtcNow14),
        (@pFlash1, @LunchStartUtc, @LunchEndUtc, @LunchStatus, @UtcNow14),
        (@pFlash1, @EvenStartUtc, @EvenEndUtc, 'Scheduled', @UtcNow14),
        (@pFlash1, @WkndStartUtc, @WkndEndUtc, 'Scheduled', @UtcNow14),
        (@pFlash1, @YestStartUtc, @YestEndUtc, 'Expired', @UtcNow14),

        -- Family Day Flash Sale: VN lunch + evening only (no overlapping live slot)
        (@pFlash2, @LunchStartUtc, @LunchEndUtc, @LunchStatus, @UtcNow14),
        (@pFlash2, @EvenStartUtc, @EvenEndUtc, 'Scheduled', @UtcNow14),
        (@pFlash2, @WkndStartUtc, @WkndEndUtc, 'Scheduled', @UtcNow14),

        -- Weekend Flash Blitz: weekend-focused slots only
        (@pFlash3, @WkndStartUtc, @WkndEndUtc, 'Scheduled', @UtcNow14),
        (@pFlash3, DATEADD(DAY, 1, @WkndStartUtc), DATEADD(DAY, 1, @WkndEndUtc), 'Scheduled', @UtcNow14),
        (@pFlash3, @YestStartUtc, @YestEndUtc, 'Expired', @UtcNow14);
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
    WHERE PromotionName = N'Outdoor Play Month'
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

        -- Outdoor Play Month (RC toys)
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
    DECLARE @nowPPS DATETIME2(0) = (SELECT UtcNow FROM #SeedTime);

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

    DECLARE @todayUtc DATE = CAST(@nowPPS AS DATE);

    -- Pick slots by role, not OFFSET (StartAt order != business meaning)
    DECLARE @sLive INT = (
        SELECT TOP 1
        TimeSlotID
    FROM PromotionTimeSlots
    WHERE PromotionID = @pFlash1Id AND Status = 'Active'
    ORDER BY StartAt DESC);
    DECLARE @sLunch INT = (
        SELECT TOP 1
        TimeSlotID
    FROM PromotionTimeSlots
    WHERE PromotionID = @pFlash1Id
        AND CAST(StartAt AS DATE) = @todayUtc
        AND TimeSlotID <> ISNULL(@sLive, -1)
    ORDER BY StartAt ASC);
    DECLARE @sEven INT = (
        SELECT TOP 1
        TimeSlotID
    FROM PromotionTimeSlots
    WHERE PromotionID = @pFlash1Id AND Status = 'Scheduled'
        AND CAST(StartAt AS DATE) = @todayUtc
    ORDER BY StartAt ASC);
    DECLARE @sWknd INT = (
        SELECT TOP 1
        TimeSlotID
    FROM PromotionTimeSlots
    WHERE PromotionID = @pFlash1Id AND Status = 'Scheduled'
        AND CAST(StartAt AS DATE) > @todayUtc
    ORDER BY StartAt ASC);

    DECLARE @fLunch INT = (
        SELECT TOP 1
        TimeSlotID
    FROM PromotionTimeSlots
    WHERE PromotionID = @pFlash2Id AND CAST(StartAt AS DATE) = @todayUtc
    ORDER BY StartAt ASC);
    DECLARE @fEven INT = (
        SELECT TOP 1
        TimeSlotID
    FROM PromotionTimeSlots
    WHERE PromotionID = @pFlash2Id AND Status = 'Scheduled'
        AND CAST(StartAt AS DATE) = @todayUtc
    ORDER BY StartAt ASC);
    DECLARE @fWknd INT = (
        SELECT TOP 1
        TimeSlotID
    FROM PromotionTimeSlots
    WHERE PromotionID = @pFlash2Id AND Status = 'Scheduled'
        AND CAST(StartAt AS DATE) > @todayUtc
    ORDER BY StartAt ASC);

    DECLARE @wWknd1 INT = (
        SELECT TOP 1
        TimeSlotID
    FROM PromotionTimeSlots
    WHERE PromotionID = @pFlash3Id AND Status = 'Scheduled'
    ORDER BY StartAt ASC);
    DECLARE @wWknd2 INT = (
        SELECT TimeSlotID
    FROM (
            SELECT TimeSlotID, ROW_NUMBER() OVER (ORDER BY StartAt ASC) AS rn
        FROM PromotionTimeSlots
        WHERE PromotionID = @pFlash3Id AND Status = 'Scheduled'
        ) ranked
    WHERE rn = 2);
    DECLARE @wExpired INT = (
        SELECT TOP 1
        TimeSlotID
    FROM PromotionTimeSlots
    WHERE PromotionID = @pFlash3Id AND Status = 'Expired'
    ORDER BY StartAt DESC);

    INSERT INTO [dbo].[PromotionProductSlots]
        (TimeSlotID, ProductID, SalePrice, DiscountPercent,
        SaleQuantity, SoldQuantity, ReservedQuantity, CreatedAt)
    VALUES
        -- Mega Flash Sale (live slot = products customers see now)
        (@sLive, 24, 197000, 50.13, 10, 3, 0, @nowPPS),
        (@sLive, 13, 39000, 54.12, 50, 12, 0, @nowPPS),
        (@sLive, 29, 44000, 50.00, 80, 25, 0, @nowPPS),
        (@sLunch, 17, 420000, 50.00, 12, 12, 0, @nowPPS),
        (@sLunch, 18, 325000, 50.00, 15, 15, 0, @nowPPS),
        (@sEven, 1, 645000, 50.00, 20, 0, 0, @nowPPS),
        (@sEven, 20, 179000, 50.14, 30, 0, 0, @nowPPS),
        (@sEven, 25, 142000, 50.18, 40, 0, 0, @nowPPS),
        (@sWknd, 2, 2995000, 50.00, 5, 0, 0, @nowPPS),
        (@sWknd, 28, 745000, 50.00, 6, 0, 0, @nowPPS),

        -- Family Day Flash Sale
        (@fLunch, 11, 272000, 50.09, 15, 0, 0, @nowPPS),
        (@fLunch, 19, 210000, 50.00, 25, 0, 0, @nowPPS),
        (@fEven, 6, 72500, 50.00, 60, 0, 0, @nowPPS),
        (@fEven, 7, 92500, 50.00, 45, 0, 0, @nowPPS),
        (@fWknd, 21, 245000, 50.00, 20, 0, 0, @nowPPS),
        (@fWknd, 22, 297000, 50.08, 12, 0, 0, @nowPPS),

        -- Weekend Flash Blitz (expired slot shows sell-through)
        (@wExpired, 23, 445000, 50.00, 8, 8, 0, @nowPPS),
        (@wExpired, 3, 795000, 50.00, 10, 7, 0, @nowPPS),
        (@wWknd1, 4, 210000, 50.00, 30, 0, 0, @nowPPS),
        (@wWknd1, 14, 82000, 50.30, 40, 0, 0, @nowPPS),
        (@wWknd2, 26, 472000, 50.05, 10, 0, 0, @nowPPS),
        (@wWknd2, 27, 340000, 50.00, 12, 0, 0, @nowPPS);
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
    DECLARE @UtcNowV DATETIME2(0) = (SELECT UtcNow
    FROM #SeedTime);
    DECLARE @UtcTodayV DATE = (SELECT UtcToday
    FROM #SeedTime);
    DECLARE @VActiveStart DATETIME2(0) = DATEADD(DAY, -23, CAST(@UtcTodayV AS DATETIME2(0)));
    DECLARE @VActiveEnd DATETIME2(0) = DATEADD(DAY, 98, CAST(@UtcTodayV AS DATETIME2(0)));
    DECLARE @creatorID INT = (SELECT TOP 1
        AccountID
    FROM Accounts
    WHERE Email = 'admintoystore@gmail.com');

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
            @VActiveStart,
            @VActiveEnd,
            'Active',
            0,
            @UtcNowV
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
            @VActiveStart,
            @VActiveEnd,
            'Active',
            0,
            @UtcNowV
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
            @VActiveStart,
            @VActiveEnd,
            'Active',
            0,
            @UtcNowV
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
            @VActiveStart,
            @VActiveEnd,
            'Active',
            0,
            @UtcNowV
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
            @VActiveStart,
            @VActiveEnd,
            'Active',
            0,
            @UtcNowV
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
            @VActiveStart,
            @VActiveEnd,
            'Active',
            0,
            @UtcNowV
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
            @VActiveStart,
            @VActiveEnd,
            'Active',
            0,
            @UtcNowV
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
            @VActiveStart,
            @VActiveEnd,
            'Active',
            0,
            @UtcNowV
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
            @VActiveStart,
            @VActiveEnd,
            'Active',
            0,
            @UtcNowV
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
            @VActiveStart,
            @VActiveEnd,
            'Active',
            0,
            @UtcNowV
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
            DATEADD(DAY, -7, CAST(@UtcTodayV AS DATETIME2(0))),
            DATEADD(DAY, 53, CAST(@UtcTodayV AS DATETIME2(0))),
            'Active',
            0,
            @UtcNowV
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
            @VActiveStart,
            @VActiveEnd,
            'Active',
            0,
            @UtcNowV
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
            @VActiveStart,
            @VActiveEnd,
            'Active',
            0,
            @UtcNowV
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
            @VActiveStart,
            @VActiveEnd,
            'Active',
            0,
            @UtcNowV
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
            @VActiveStart,
            @VActiveEnd,
            'Active',
            0,
            @UtcNowV
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
            0,
            1,
            DATEADD(DAY, 21, CAST(@UtcTodayV AS DATETIME2(0))),
            DATEADD(DAY, 51, CAST(@UtcTodayV AS DATETIME2(0))),
            'Scheduled',
            0,
            @UtcNowV
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
            @VActiveStart,
            @VActiveEnd,
            'Active',
            0,
            @UtcNowV
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
            @VActiveStart,
            @VActiveEnd,
            'Active',
            0,
            @UtcNowV
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
            @VActiveStart,
            @VActiveEnd,
            'Active',
            0,
            @UtcNowV
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
            @VActiveStart,
            @VActiveEnd,
            'Active',
            0,
            @UtcNowV
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
            DATEADD(DAY, 14, CAST(@UtcTodayV AS DATETIME2(0))),
            DATEADD(DAY, 44, CAST(@UtcTodayV AS DATETIME2(0))),
            'Pending',
            0,
            @UtcNowV
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
            DATEADD(DAY, -90, CAST(@UtcTodayV AS DATETIME2(0))),
            DATEADD(DAY, -60, CAST(@UtcTodayV AS DATETIME2(0))),
            'Inactive',
            0,
            DATEADD(DAY, -90, @UtcNowV)
    ),

        (
            @creatorID,
            'EXPIRING48H',
            N'Expiring Soon 48H',
            N'Last chance – 10% off before this voucher expires in 48 hours',
            'PERCENTAGE',
            10,
            80000,
            'ORDER_TOTAL',
            100000,
            500,
            0,
            1,
            @UtcNowV,
            DATEADD(DAY, 2, @UtcNowV),
            'Active',
            0,
            @UtcNowV
    ),

        (
            @creatorID,
            'UPCOMING20',
            N'Upcoming 20% Teaser',
            N'20% off launching next week – save the code for later',
            'PERCENTAGE',
            20,
            200000,
            'ORDER_TOTAL',
            250000,
            300,
            0,
            1,
            DATEADD(DAY, 7, CAST(@UtcTodayV AS DATETIME2(0))),
            DATEADD(DAY, 14, CAST(@UtcTodayV AS DATETIME2(0))),
            'Scheduled',
            0,
            @UtcNowV
    ),

        (
            @creatorID,
            'SPRING12',
            N'Spring 12% Off',
            N'Expired spring voucher kept for history',
            'PERCENTAGE',
            12,
            100000,
            'ORDER_TOTAL',
            100000,
            100,
            100,
            1,
            DATEADD(DAY, -60, CAST(@UtcTodayV AS DATETIME2(0))),
            DATEADD(DAY, -30, CAST(@UtcTodayV AS DATETIME2(0))),
            'Expired',
            0,
            DATEADD(DAY, -60, @UtcNowV)
    ),

        (
            @creatorID,
            'REJECTED50K',
            N'Rejected Staff Proposal 50K',
            N'Admin rejected this voucher proposal',
            'FIXED',
            50000,
            NULL,
            'ORDER_TOTAL',
            300000,
            100,
            0,
            1,
            DATEADD(DAY, 30, CAST(@UtcTodayV AS DATETIME2(0))),
            DATEADD(DAY, 60, CAST(@UtcTodayV AS DATETIME2(0))),
            'Rejected',
            0,
            @UtcNowV
    );
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 17.1 – DYNAMIC DAILY SEEDING (VOUCHERS, PROMOTIONS, FLASH SALES)
   From @UtcToday (2026-08-01) through 2026-08-25. Realistic production-grade titles.
══════════════════════════════════════════════════════════════ */
PRINT N'[17.1] Dynamic Daily Seeding...';

DECLARE @UtcNow171 DATETIME2(0) = (SELECT UtcNow FROM #SeedTime);
DECLARE @startDate DATE = (SELECT UtcToday FROM #SeedTime);
DECLARE @endDate DATE = DATEADD(DAY, 24, @startDate);

DECLARE @currentDate DATE = @startDate;
DECLARE @dayNum INT = 1;

DECLARE @creatorID INT = (SELECT TOP 1 AccountID FROM Accounts WHERE Email = 'admintoystore@gmail.com');

WHILE @currentDate <= @endDate
BEGIN
    DECLARE @dateStr VARCHAR(10) = CONVERT(VARCHAR(10), @currentDate, 120);
    DECLARE @dtStart DATETIME2(0) = CAST(@currentDate AS DATETIME2(0));
    DECLARE @dtEnd DATETIME2(0) = DATEADD(SECOND, -1, DATEADD(DAY, 2, CAST(@currentDate AS DATETIME2(0))));

    -- -------------------------------------------------------------
    -- 1. DYNAMIC VOUCHERS (Realistic Production Voucher Data)
    -- -------------------------------------------------------------
    DECLARE @vCode VARCHAR(30);
    DECLARE @vName NVARCHAR(255);
    DECLARE @vDesc NVARCHAR(255);
    DECLARE @vType VARCHAR(10);
    DECLARE @vVal DECIMAL(12,2);
    DECLARE @vCap DECIMAL(12,0);
    DECLARE @vTarget VARCHAR(20);
    DECLARE @vMinAmt DECIMAL(12,0);

    SELECT 
        @vCode = Code,
        @vName = Name,
        @vDesc = Description,
        @vType = Type,
        @vVal  = Val,
        @vCap  = Cap,
        @vTarget = Target,
        @vMinAmt = MinAmt
    FROM (
        VALUES
        (1,  'AUGSTART50K',   N'August Kickoff Special 50K',       N'50,000 VND discount for orders over 400,000 VND',            'FIXED',      50000, NULL,   'FINAL_PRICE',  400000),
        (2,  'FREESHIPAUG2',  N'August Express Free Shipping',     N'Free shipping up to 30,000 VND on orders from 150,000 VND', 'FIXED',      30000, 30000,  'SHIPPING_FEE', 150000),
        (3,  'SUMMERJOY10',   N'Summer Joy 10% Voucher',          N'10% off entire order up to 100,000 VND',                     'PERCENTAGE', 10,    100000, 'ORDER_TOTAL',  200000),
        (4,  'LEGOCLUB40K',   N'Lego Master Builder Voucher',      N'40,000 VND discount on building sets and Lego models',       'FIXED',      40000, NULL,   'FINAL_PRICE',  350000),
        (5,  'FREESHIPAUG5',  N'Midweek Shipping Saver',           N'Free shipping up to 30,000 VND for all customers',          'FIXED',      30000, 30000,  'SHIPPING_FEE', 150000),
        (6,  'SMARTKID15',    N'Smart Explorer 15% Off',           N'15% off STEM and educational toys',                          'PERCENTAGE', 15,    150000, 'ORDER_TOTAL',  250000),
        (7,  'WEEKENDSPECIAL',N'Weekend Family Play Discount',     N'50,000 VND off for big family toy orders',                   'FIXED',      50000, NULL,   'FINAL_PRICE',  400000),
        (8,  'SUPER88BONUS',  N'Super 8/8 Mega Voucher',           N'10% off total order value',                                  'PERCENTAGE', 10,    100000, 'ORDER_TOTAL',  200000),
        (9,  'FREESHIPAUG9',  N'Sunday Free Shipping Express',     N'Free shipping up to 30,000 VND',                             'FIXED',      30000, 30000,  'SHIPPING_FEE', 150000),
        (10, 'PLAYTIME50K',   N'Playtime Bonus 50K',               N'50,000 VND discount on all toy categories',                  'FIXED',      50000, NULL,   'FINAL_PRICE',  400000),
        (11, 'BTSWARMUP10',   N'Back to School Early Bird 10%',    N'10% off educational toys and desk accessories',             'PERCENTAGE', 10,    100000, 'ORDER_TOTAL',  200000),
        (12, 'FREESHIPBTS',   N'School Ready Free Shipping',       N'Free shipping up to 30,000 VND',                             'FIXED',      30000, 30000,  'SHIPPING_FEE', 150000),
        (13, 'KIDSWORLD30K',  N'Kids World Special 30K',           N'30,000 VND discount on plush & action figures',              'FIXED',      30000, NULL,   'FINAL_PRICE',  250000),
        (14, 'WEEKENDJOY50K', N'Weekend Joy Cash Back',            N'50,000 VND off on orders from 400,000 VND',                  'FIXED',      50000, NULL,   'FINAL_PRICE',  400000),
        (15, 'MIDMONTH15',    N'Mid-Month Special 15% Off',        N'15% off all orders up to 150,000 VND',                       'PERCENTAGE', 15,    150000, 'ORDER_TOTAL',  300000),
        (16, 'FREESHIPMID',   N'Mid-Month Free Shipping',          N'Free shipping up to 30,000 VND',                             'FIXED',      30000, 30000,  'SHIPPING_FEE', 150000),
        (17, 'STEMPOWER40K',  N'STEM & Science Power Bonus',       N'40,000 VND off science and learning kits',                   'FIXED',      40000, NULL,   'FINAL_PRICE',  300000),
        (18, 'RACERCLUB10',   N'RC Speed Racer 10% Off',           N'10% off remote control cars and vehicles',                   'PERCENTAGE', 10,    100000, 'ORDER_TOTAL',  200000),
        (19, 'FREESHIP19',    N'August Delivery Boost',            N'Free shipping up to 30,000 VND',                             'FIXED',      30000, 30000,  'SHIPPING_FEE', 150000),
        (20, 'TOYHOUSE2026',  N'Toy House Exclusive 50K',          N'50,000 VND discount for loyal customers',                    'FIXED',      50000, NULL,   'FINAL_PRICE',  400000),
        (21, 'BTSCOUNTDOWN10',N'Back to School Countdown 10%',    N'10% off educational sets and backpacks',                    'PERCENTAGE', 10,    100000, 'ORDER_TOTAL',  200000),
        (22, 'FREESHIPBTS22', N'School Prep Free Delivery',        N'Free shipping up to 30,000 VND',                             'FIXED',      30000, 30000,  'SHIPPING_FEE', 150000),
        (23, 'SUNDAYFUN50K',  N'Super Sunday Fun Bonus',           N'50,000 VND off on orders from 400,000 VND',                  'FIXED',      50000, NULL,   'FINAL_PRICE',  400000),
        (24, 'LEGOWEEKEND15', N'Lego Builder Festival 15%',        N'15% off building blocks and Lego sets',                      'PERCENTAGE', 15,    150000, 'ORDER_TOTAL',  300000),
        (25, 'FINALBOOST50K', N'August Grand Finale 50K',          N'50,000 VND discount to wrap up August',                      'FIXED',      50000, NULL,   'FINAL_PRICE',  400000)
    ) AS V(DayIdx, Code, Name, Description, Type, Val, Cap, Target, MinAmt)
    WHERE DayIdx = ((@dayNum - 1) % 25) + 1;

    DECLARE @vStatus VARCHAR(20) = CASE
        WHEN @dtEnd < @UtcNow171 THEN 'Expired'
        WHEN @dtStart > @UtcNow171 THEN 'Scheduled'
        ELSE 'Active' END;

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Vouchers] WHERE VoucherCode = @vCode)
    BEGIN
        INSERT INTO [dbo].[Vouchers]
            (CreatedBy, VoucherCode, VoucherName, VoucherDescription,
            DiscountType, DiscountValue, MaxDiscountCap, DiscountTarget, MinOrderAmount,
            TotalQuantity, UsedQuantity, MaxUsagePerUser, StartDate, EndDate, Status, IsDeleted, CreatedAt)
        VALUES
            (@creatorID, @vCode, @vName, @vDesc,
                @vType, @vVal, @vCap, @vTarget, @vMinAmt,
                1000, 0, 1, @dtStart, @dtEnd, @vStatus, 0, @UtcNow171);
    END;

    -- -------------------------------------------------------------
    -- 2. DYNAMIC PROMOTIONS (DISCOUNT) (Realistic Promotion Names)
    -- -------------------------------------------------------------
    DECLARE @promoName NVARCHAR(200);
    DECLARE @promoDesc NVARCHAR(500);

    SELECT 
        @promoName = Name,
        @promoDesc = Description
    FROM (
        VALUES
        (1,  N'August Toy Welcome Carnival',       N'Kickstart August with exclusive deals on top-rated toys.'),
        (2,  N'Creative Lego & Blocks Showcase',   N'Special discounts on building block sets for young creators.'),
        (3,  N'Outdoor Adventure & Ride-On Sale',  N'Best deals on scooters, balls, kites, and outdoor gear.'),
        (4,  N'Plush & Huggable Pals Fair',        N'Adorable teddy bears and plushies at unbeatable prices.'),
        (5,  N'STEM & Genius Kid Discovery',       N'Educational kits, microscopes, and STEM toys on sale.'),
        (6,  N'Speed & Motion RC Racing Days',     N'High-speed RC cars and helicopters with special discounts.'),
        (7,  N'Weekend Family Bonding Sale',       N'Board games and family toys for a fun weekend.'),
        (8,  N'Super 8/8 Toy Shopping Festival',   N'One-day mega discounts across all popular categories.'),
        (9,  N'Action Figures & Superheroes Rally',N'Transformers, Marvel, and Kamen Rider collectibles on deal.'),
        (10, N'Toddler & Baby Playtime Essentials',N'Safe, non-toxic baby toys and wooden play sets.'),
        (11, N'Back to School Learning Prep',      N'Prepare for school with flashcards and learning tablets.'),
        (12, N'Lego Technic & City Builders Fair', N'Deep discounts on advanced Lego engineering sets.'),
        (13, N'Art & Craft Creative Workshop',     N'Play-Doh, drawing tablets, and craft kits at low prices.'),
        (14, N'Mid-August Toy Wonderland',         N'Mid-month deals on trending items and bestsellers.'),
        (15, N'Super Heroes & Anime Figure Expo',  N'Bandai, Ultraman, and superhero action figures on sale.'),
        (16, N'Active Kids Sports & Games Special',N'Balls, ride-on cars, and outdoor play sets.'),
        (17, N'Little Scientist Experiment Week',  N'Science experiment kits and telescopes at discounted rates.'),
        (18, N'RC Car & Drone Hobby Days',         N'Remote-controlled vehicles and stunt cars for kids.'),
        (19, N'Dollhouse & Fashion Doll Parade',    N'Fashion dolls, dollhouses, and plush toys special.'),
        (20, N'Toy House Premium Collection Sale', N'Handpicked top-tier toys with extra discount.'),
        (21, N'Back to School Final Countdown',    N'Essential educational toys before school starts.'),
        (22, N'Lego Architecture & City Festival', N'Building blocks for all ages with discount prices.'),
        (23, N'Super Weekend Flash Carnival',      N'Weekend clearance on popular toy lines.'),
        (24, N'Creative Mind Modeling Clay Sale',  N'Play-Doh and sculpting kits at reduced prices.'),
        (25, N'August Grand Finale Clearance',     N'Wrap up August with the biggest discounts of the month.')
    ) AS P(DayIdx, Name, Description)
    WHERE DayIdx = ((@dayNum - 1) % 25) + 1;

    DECLARE @promoID INT;
    DECLARE @promoStatus VARCHAR(20) = CASE
        WHEN @dtEnd < @UtcNow171 THEN 'Expired'
        WHEN @dtStart > @UtcNow171 THEN 'Scheduled'
        ELSE 'Active' END;

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Promotions] WHERE PromotionName = @promoName)
    BEGIN
        INSERT INTO [dbo].[Promotions]
            (CreatedBy, PromotionName, PromotionType, Description, StartDate, EndDate, Status, Priority, IsDeleted, CreatedAt)
        VALUES
            (@creatorID, @promoName, 'DISCOUNT', @promoDesc, @dtStart, @dtEnd, @promoStatus, 10, 0, @UtcNow171);

        SET @promoID = SCOPE_IDENTITY();

        -- Link to 3 products (systematically rotating product IDs)
        DECLARE @p1 INT = (@dayNum % 30) + 1;
        DECLARE @p2 INT = ((@dayNum + 7) % 30) + 1;
        DECLARE @p3 INT = ((@dayNum + 13) % 30) + 1;

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
    -- 3. DYNAMIC FLASH SALES (Realistic Production Flash Sale Names)
    -- -------------------------------------------------------------
    DECLARE @flashPromoName NVARCHAR(200);
    SELECT @flashPromoName = FlashName
    FROM (
        VALUES
        (1,  N'August Kickoff Flash Rush'),
        (2,  N'Lego Builder Flash Hour'),
        (3,  N'Outdoor Fun Flash Blitz'),
        (4,  N'Plushie Hugs Flash Sale'),
        (5,  N'STEM Genius Flash Hour'),
        (6,  N'RC Speed Flash Hunt'),
        (7,  N'Weekend Family Flash Special'),
        (8,  N'Super 8/8 Mega Flash Blitz'),
        (9,  N'Superhero Action Flash Sale'),
        (10, N'Toddler Playtime Flash Hour'),
        (11, N'School Prep Flash Rush'),
        (12, N'Technic Master Flash Blitz'),
        (13, N'Art & Craft Flash Special'),
        (14, N'Mid-Month Lightning Flash Sale'),
        (15, N'Anime Figure Flash Hunt'),
        (16, N'Active Sports Flash Rush'),
        (17, N'Little Explorer Flash Special'),
        (18, N'RC Stunt Car Flash Blitz'),
        (19, N'Dollhouse Dreams Flash Sale'),
        (20, N'Toy House Golden Flash Hour'),
        (21, N'School Countdown Flash Blitz'),
        (22, N'Lego Kingdom Flash Special'),
        (23, N'Super Sunday Flash Rush'),
        (24, N'Creative Clay Flash Blitz'),
        (25, N'August Grand Finale Flash Sale')
    ) AS F(DayIdx, FlashName)
    WHERE DayIdx = ((@dayNum - 1) % 25) + 1;

    DECLARE @flashPromoID INT;

    DECLARE @flashPromoStatus VARCHAR(20) = CASE
        WHEN @dtEnd < @UtcNow171 THEN 'Expired'
        WHEN @dtStart > @UtcNow171 THEN 'Scheduled'
        ELSE 'Active' END;

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Promotions] WHERE PromotionName = @flashPromoName)
    BEGIN
        INSERT INTO [dbo].[Promotions]
            (CreatedBy, PromotionName, PromotionType, Description, StartDate, EndDate, Status, Priority, IsDeleted, CreatedAt)
        VALUES
            (@creatorID, @flashPromoName, 'FLASH_SALE', N'Limited time daily flash sale with up to 50% discount.', @dtStart, @dtEnd, @flashPromoStatus, 20, 0, @UtcNow171);

        SET @flashPromoID = SCOPE_IDENTITY();

        DECLARE @VnSlot1Start DATETIME2(0) = DATEADD(HOUR, 11, CAST(@currentDate AS DATETIME2(0)));
        DECLARE @VnSlot1End   DATETIME2(0) = DATEADD(HOUR, 14, CAST(@currentDate AS DATETIME2(0)));
        DECLARE @VnSlot2Start DATETIME2(0) = DATEADD(HOUR, 19, CAST(@currentDate AS DATETIME2(0)));
        DECLARE @VnSlot2End   DATETIME2(0) = DATEADD(HOUR, 22, CAST(@currentDate AS DATETIME2(0)));

        DECLARE @slot1Start DATETIME2(0) = CAST((@VnSlot1Start AT TIME ZONE 'SE Asia Standard Time') AT TIME ZONE 'UTC' AS DATETIME2(0));
        DECLARE @slot1End   DATETIME2(0) = CAST((@VnSlot1End   AT TIME ZONE 'SE Asia Standard Time') AT TIME ZONE 'UTC' AS DATETIME2(0));
        DECLARE @slot2Start DATETIME2(0) = CAST((@VnSlot2Start AT TIME ZONE 'SE Asia Standard Time') AT TIME ZONE 'UTC' AS DATETIME2(0));
        DECLARE @slot2End   DATETIME2(0) = CAST((@VnSlot2End   AT TIME ZONE 'SE Asia Standard Time') AT TIME ZONE 'UTC' AS DATETIME2(0));

        DECLARE @slot1Status VARCHAR(20) = CASE
            WHEN @flashPromoStatus = 'Scheduled' THEN 'Scheduled'
            WHEN @flashPromoStatus = 'Expired' THEN 'Expired'
            WHEN @UtcNow171 >= @slot1Start AND @UtcNow171 < @slot1End THEN 'Active'
            WHEN @UtcNow171 >= @slot1End THEN 'Expired'
            ELSE 'Scheduled' END;

        DECLARE @slot2Status VARCHAR(20) = CASE
            WHEN @flashPromoStatus = 'Scheduled' THEN 'Scheduled'
            WHEN @flashPromoStatus = 'Expired' THEN 'Expired'
            WHEN @UtcNow171 >= @slot2Start AND @UtcNow171 < @slot2End THEN 'Active'
            WHEN @UtcNow171 >= @slot2End THEN 'Expired'
            ELSE 'Scheduled' END;

        INSERT INTO [dbo].[PromotionTimeSlots]
            (PromotionID, StartAt, EndAt, Status, IsDeleted, CreatedAt)
        VALUES
            (@flashPromoID, @slot1Start, @slot1End, @slot1Status, 0, @UtcNow171),
            (@flashPromoID, @slot2Start, @slot2End, @slot2Status, 0, @UtcNow171);

        DECLARE @slot1ID INT;
        DECLARE @slot2ID INT;

        SET @slot1ID = (SELECT TimeSlotID FROM PromotionTimeSlots WHERE PromotionID = @flashPromoID AND StartAt = @slot1Start);
        SET @slot2ID = (SELECT TimeSlotID FROM PromotionTimeSlots WHERE PromotionID = @flashPromoID AND StartAt = @slot2Start);

        DECLARE @sold1 INT = CASE WHEN @slot1Status = 'Expired' THEN 8 + (@dayNum % 7) ELSE 0 END;
        DECLARE @sold2 INT = CASE WHEN @slot2Status = 'Expired' THEN 5 + (@dayNum % 5) ELSE 0 END;

        DECLARE @fp1 INT = ((@dayNum + 3) % 30) + 1;
        DECLARE @fp2 INT = ((@dayNum + 11) % 30) + 1;
        DECLARE @fprice1 DECIMAL(12,0) = (SELECT Price FROM Products WHERE ProductID = @fp1);
        DECLARE @fprice2 DECIMAL(12,0) = (SELECT Price FROM Products WHERE ProductID = @fp2);

        IF @slot1ID IS NOT NULL
        BEGIN
            IF @fprice1 IS NOT NULL
                INSERT INTO [dbo].[PromotionProductSlots]
                (TimeSlotID, ProductID, SalePrice, DiscountPercent, SaleQuantity, SoldQuantity, ReservedQuantity, IsDeleted, CreatedAt)
            VALUES
                (@slot1ID, @fp1, @fprice1 * 0.50, 50.00, 20, @sold1, 0, 0, @UtcNow171);
            IF @fprice2 IS NOT NULL
                INSERT INTO [dbo].[PromotionProductSlots]
                (TimeSlotID, ProductID, SalePrice, DiscountPercent, SaleQuantity, SoldQuantity, ReservedQuantity, IsDeleted, CreatedAt)
            VALUES
                (@slot1ID, @fp2, @fprice2 * 0.50, 50.00, 20, @sold1, 0, 0, @UtcNow171);
        END;

        DECLARE @fp3 INT = ((@dayNum + 17) % 30) + 1;
        DECLARE @fp4 INT = ((@dayNum + 23) % 30) + 1;
        DECLARE @fprice3 DECIMAL(12,0) = (SELECT Price FROM Products WHERE ProductID = @fp3);
        DECLARE @fprice4 DECIMAL(12,0) = (SELECT Price FROM Products WHERE ProductID = @fp4);

        IF @slot2ID IS NOT NULL
        BEGIN
            IF @fprice3 IS NOT NULL
                INSERT INTO [dbo].[PromotionProductSlots]
                (TimeSlotID, ProductID, SalePrice, DiscountPercent, SaleQuantity, SoldQuantity, ReservedQuantity, IsDeleted, CreatedAt)
            VALUES
                (@slot2ID, @fp3, @fprice3 * 0.50, 50.00, 20, @sold2, 0, 0, @UtcNow171);
            IF @fprice4 IS NOT NULL
                INSERT INTO [dbo].[PromotionProductSlots]
                (TimeSlotID, ProductID, SalePrice, DiscountPercent, SaleQuantity, SoldQuantity, ReservedQuantity, IsDeleted, CreatedAt)
            VALUES
                (@slot2ID, @fp4, @fprice4 * 0.50, 50.00, 20, @sold2, 0, 0, @UtcNow171);
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
DECLARE @UtcNow27 DATETIME2(0) = (SELECT UtcNow
FROM #SeedTime);

IF NOT EXISTS (SELECT 1
FROM [dbo].[OrderRefundReasons])
BEGIN
    INSERT INTO [dbo].[OrderRefundReasons]
        (Content, Description, IsDeleted, IsSystem, CreatedAt)
    VALUES
        (N'Product defective from manufacturer', N'The delivered product has technical defects', 0, 0, @UtcNow27),
        (N'Wrong product delivered', N'The shop sent the wrong color, size, or model', 0, 0, @UtcNow27),
        (N'Product not as described', N'The actual product does not match the images or description', 0, 0, @UtcNow27),
        (N'Product damaged during shipping', N'The package was dented or broken during delivery', 0, 0, @UtcNow27),
        (N'Missing accessories', N'The package does not include all accessories as described', 0, 0, @UtcNow27),
        (N'Delivery failed / unable to deliver', N'Automatic refund when GHN returns the package to warehouse due to failed delivery (System-only)', 0, 1, @UtcNow27);
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 28 – ORDERS (UTC timeline, English addresses)
   10 historical delivered/completed + 4 worker-test orders
══════════════════════════════════════════════════════════════ */
PRINT N'[28] Orders & OrderDetails...';

IF NOT EXISTS (SELECT 1
FROM [dbo].[Orders])
BEGIN
    DECLARE @UtcNow28 DATETIME2(0) = (SELECT UtcNow
    FROM #SeedTime);
    DECLARE @cust1 INT = (SELECT TOP 1
        AccountID
    FROM Accounts
    WHERE Email = 'lananh.pham@gmail.com');
    DECLARE @cust2 INT = (SELECT TOP 1
        AccountID
    FROM Accounts
    WHERE Email = 'hung.nguyen88@gmail.com');
    DECLARE @cust3 INT = (SELECT TOP 1
        AccountID
    FROM Accounts
    WHERE Email = 'bauchau.vu@gmail.com');
    DECLARE @cust4 INT = (SELECT TOP 1
        AccountID
    FROM Accounts
    WHERE Email = 'mtuando@outlook.com');
    DECLARE @cust5 INT = (SELECT TOP 1
        AccountID
    FROM Accounts
    WHERE Email = 'thuha.hoang@gmail.com');

    SET IDENTITY_INSERT [dbo].[Orders] ON;
    INSERT INTO [dbo].[Orders]
        (OrderID, AccountID, StatusID, OrderCode,
        ShippingName, ShippingPhone, ShippingAddress,
        ShippingWardCode, ShippingWardName, ShippingDistrictId, ShippingDistrictName, ShippingProvinceId, ShippingProvinceName,
        OrderDate, ConfirmedAt, ShippedAt, DeliveredAt, CompletedAt,
        PaymentMethod, PaymentStatus, PaidAt,
        SubTotal, VoucherDiscountAmount, EstimatedShippingFee, TotalAmount, IsDeleted, CreatedAt)
    VALUES
        (1, @cust1, 6, 'ORD202605010001', N'Lan Anh Pham', '0912001001', N'12 Nguyen Hue St', '20101', N'Phường Bến Nghé', 1442, N'Quận 1', 202, N'Hồ Chí Minh',
            DATEADD(DAY, -75, @UtcNow28), DATEADD(DAY, -75, DATEADD(HOUR, 2, @UtcNow28)), DATEADD(DAY, -74, @UtcNow28), DATEADD(DAY, -73, @UtcNow28), NULL,
            'SHIP_COD', 'PAID', DATEADD(DAY, -73, @UtcNow28), 1710000, 0, 30000, 1740000, 0, DATEADD(DAY, -75, @UtcNow28)),
        (2, @cust1, 7, 'ORD202605010002', N'Lan Anh Pham', '0912001001', N'12 Nguyen Hue St', '20101', N'Phường Bến Nghé', 1442, N'Quận 1', 202, N'Hồ Chí Minh',
            DATEADD(DAY, -45, @UtcNow28), DATEADD(DAY, -45, DATEADD(HOUR, 2, @UtcNow28)), DATEADD(DAY, -44, @UtcNow28), DATEADD(DAY, -43, @UtcNow28), DATEADD(DAY, -40, @UtcNow28),
            'SHIP_COD', 'PAID', DATEADD(DAY, -43, @UtcNow28), 735000, 0, 30000, 765000, 0, DATEADD(DAY, -45, @UtcNow28)),
        (3, @cust2, 6, 'ORD202605010003', N'Hung Nguyen', '0912001002', N'45 Le Loi St', '20301', N'Phường 1', 1444, N'Quận 3', 202, N'Hồ Chí Minh',
            DATEADD(DAY, -60, @UtcNow28), DATEADD(DAY, -60, DATEADD(HOUR, 2, @UtcNow28)), DATEADD(DAY, -59, @UtcNow28), DATEADD(DAY, -58, @UtcNow28), NULL,
            'SHIP_COD', 'PAID', DATEADD(DAY, -58, @UtcNow28), 6395000, 0, 30000, 6425000, 0, DATEADD(DAY, -60, @UtcNow28)),
        (4, @cust2, 7, 'ORD202605010004', N'Hung Nguyen', '0912001002', N'45 Le Loi St', '20301', N'Phường 1', 1444, N'Quận 3', 202, N'Hồ Chí Minh',
            DATEADD(DAY, -30, @UtcNow28), DATEADD(DAY, -30, DATEADD(HOUR, 2, @UtcNow28)), DATEADD(DAY, -29, @UtcNow28), DATEADD(DAY, -28, @UtcNow28), DATEADD(DAY, -25, @UtcNow28),
            'SHIP_COD', 'PAID', DATEADD(DAY, -28, @UtcNow28), 630000, 0, 30000, 660000, 0, DATEADD(DAY, -30, @UtcNow28)),
        (5, @cust3, 6, 'ORD202605010005', N'Bau Chau Vu', '0912001003', N'88 Tran Hung Dao St', '20501', N'Phường 1', 1447, N'Quận 5', 202, N'Hồ Chí Minh',
            DATEADD(DAY, -55, @UtcNow28), DATEADD(DAY, -55, DATEADD(HOUR, 2, @UtcNow28)), DATEADD(DAY, -54, @UtcNow28), DATEADD(DAY, -53, @UtcNow28), NULL,
            'SHIP_COD', 'PAID', DATEADD(DAY, -53, @UtcNow28), 1640000, 0, 30000, 1670000, 0, DATEADD(DAY, -55, @UtcNow28)),
        (6, @cust3, 7, 'ORD202605010006', N'Bau Chau Vu', '0912001003', N'88 Tran Hung Dao St', '20501', N'Phường 1', 1447, N'Quận 5', 202, N'Hồ Chí Minh',
            DATEADD(DAY, -35, @UtcNow28), DATEADD(DAY, -35, DATEADD(HOUR, 2, @UtcNow28)), DATEADD(DAY, -34, @UtcNow28), DATEADD(DAY, -33, @UtcNow28), DATEADD(DAY, -30, @UtcNow28),
            'SHIP_COD', 'PAID', DATEADD(DAY, -33, @UtcNow28), 1080000, 0, 30000, 1110000, 0, DATEADD(DAY, -35, @UtcNow28)),
        (7, @cust4, 6, 'ORD202605010007', N'Minh Tuan Do', '0912001004', N'99 Dien Bien Phu St', '21601', N'Phường 1', 1462, N'Quận Bình Thạnh', 202, N'Hồ Chí Minh',
            DATEADD(DAY, -50, @UtcNow28), DATEADD(DAY, -50, DATEADD(HOUR, 2, @UtcNow28)), DATEADD(DAY, -49, @UtcNow28), DATEADD(DAY, -48, @UtcNow28), NULL,
            'SHIP_COD', 'PAID', DATEADD(DAY, -48, @UtcNow28), 870000, 0, 30000, 900000, 0, DATEADD(DAY, -50, @UtcNow28)),
        (8, @cust4, 7, 'ORD202605010008', N'Minh Tuan Do', '0912001004', N'99 Dien Bien Phu St', '21601', N'Phường 1', 1462, N'Quận Bình Thạnh', 202, N'Hồ Chí Minh',
            DATEADD(DAY, -20, @UtcNow28), DATEADD(DAY, -20, DATEADD(HOUR, 2, @UtcNow28)), DATEADD(DAY, -19, @UtcNow28), DATEADD(DAY, -18, @UtcNow28), DATEADD(DAY, -15, @UtcNow28),
            'SHIP_COD', 'PAID', DATEADD(DAY, -18, @UtcNow28), 980000, 0, 30000, 1010000, 0, DATEADD(DAY, -20, @UtcNow28)),
        (9, @cust5, 6, 'ORD202605010009', N'Thu Ha Hoang', '0912001005', N'22 Hoang Van Thu St', '21701', N'Phường 1', 1457, N'Quận Phú Nhuận', 202, N'Hồ Chí Minh',
            DATEADD(DAY, -40, @UtcNow28), DATEADD(DAY, -40, DATEADD(HOUR, 2, @UtcNow28)), DATEADD(DAY, -39, @UtcNow28), DATEADD(DAY, -38, @UtcNow28), NULL,
            'SHIP_COD', 'PAID', DATEADD(DAY, -38, @UtcNow28), 1290000, 0, 30000, 1320000, 0, DATEADD(DAY, -40, @UtcNow28)),
        (10, @cust5, 7, 'ORD202605010010', N'Thu Ha Hoang', '0912001005', N'22 Hoang Van Thu St', '21701', N'Phường 1', 1457, N'Quận Phú Nhuận', 202, N'Hồ Chí Minh',
            DATEADD(DAY, -10, @UtcNow28), DATEADD(DAY, -10, DATEADD(HOUR, 2, @UtcNow28)), DATEADD(DAY, -9, @UtcNow28), DATEADD(DAY, -8, @UtcNow28), DATEADD(DAY, -5, @UtcNow28),
            'SHIP_COD', 'PAID', DATEADD(DAY, -8, @UtcNow28), 420000, 0, 30000, 450000, 0, DATEADD(DAY, -10, @UtcNow28)),
        (11, @cust1, 1, 'ORD202605010011', N'Lan Anh Pham', '0912001001', N'12 Nguyen Hue St', '20101', N'Phường Bến Nghé', 1442, N'Quận 1', 202, N'Hồ Chí Minh',
            DATEADD(MINUTE, -15, @UtcNow28), NULL, NULL, NULL, NULL,
            'SE_PAY', 'PENDING', NULL, 359000, 0, 30000, 389000, 0, DATEADD(MINUTE, -15, @UtcNow28)),
        (12, @cust2, 1, 'ORD202605010012', N'Hung Nguyen', '0912001002', N'45 Le Loi St', '20301', N'Phường 1', 1444, N'Quận 3', 202, N'Hồ Chí Minh',
            DATEADD(MINUTE, -45, @UtcNow28), NULL, NULL, NULL, NULL,
            'SE_PAY', 'PENDING', NULL, 490000, 0, 30000, 520000, 0, DATEADD(MINUTE, -45, @UtcNow28)),
        (13, @cust3, 1, 'ORD202605010013', N'Bau Chau Vu', '0912001003', N'88 Tran Hung Dao St', '20501', N'Phường 1', 1447, N'Quận 5', 202, N'Hồ Chí Minh',
            DATEADD(HOUR, -25, @UtcNow28), NULL, NULL, NULL, NULL,
            'SHIP_COD', 'PENDING', NULL, 280000, 0, 30000, 310000, 0, DATEADD(HOUR, -25, @UtcNow28)),
        (14, @cust4, 6, 'ORD202605010014', N'Minh Tuan Do', '0912001004', N'99 Dien Bien Phu St', '21601', N'Phường 1', 1462, N'Quận Bình Thạnh', 202, N'Hồ Chí Minh',
            DATEADD(DAY, -6, @UtcNow28), DATEADD(DAY, -6, DATEADD(HOUR, 2, @UtcNow28)), DATEADD(DAY, -5, @UtcNow28), DATEADD(DAY, -4, @UtcNow28), NULL,
            'SHIP_COD', 'PAID', DATEADD(DAY, -4, @UtcNow28), 595000, 0, 30000, 625000, 0, DATEADD(DAY, -6, @UtcNow28));
    SET IDENTITY_INSERT [dbo].[Orders] OFF;

    INSERT INTO [dbo].[OrderDetails]
        (OrderID, ProductID, ProductName, ProductImage, Quantity, UnitPrice, DiscountAmount, CreatedAt)
    SELECT o.OrderID, p.ProductID, p.ProductName,
        'https://picsum.photos/seed/prod-' + CAST(p.ProductID AS VARCHAR) + '/300/300',
        1, p.Price, 0, o.CreatedAt
    FROM (VALUES
            (1, 1),
            (1, 3),
            (2, 6),
            (2, 7),
            (3, 2),
            (3, 1),
            (4, 4),
            (4, 9),
            (5, 3),
            (5, 10),
            (6, 8),
            (6, 1),
            (7, 5),
            (7, 9),
            (8, 10),
            (8, 6),
            (9, 1),
            (9, 7),
            (10, 4),
            (10, 5),
            (11, 20),
            (12, 10),
            (13, 9),
            (14, 22)
    ) AS v(OID, PID)
        JOIN Orders o ON o.OrderID = v.OID
        JOIN Products p ON p.ProductID = v.PID;

    -- Seed 3 failed orders for khoalmce181686@fpt.edu.vn to test Block Customer
    DECLARE @cust_test INT = (SELECT TOP 1 AccountID FROM Accounts WHERE Email = 'khoalmce181686@fpt.edu.vn');
    IF @cust_test IS NOT NULL
    BEGIN
        SET IDENTITY_INSERT [dbo].[Orders] ON;
        INSERT INTO [dbo].[Orders]
            (OrderID, AccountID, StatusID, OrderCode,
            ShippingName, ShippingPhone, ShippingAddress,
            ShippingWardCode, ShippingWardName, ShippingDistrictId, ShippingDistrictName, ShippingProvinceId, ShippingProvinceName,
            OrderDate, ConfirmedAt, ShippedAt, FailedDeliveryAt,
            LastGHNFailCode, DeliveryFailCount,
            PaymentMethod, PaymentStatus, PaidAt,
            SubTotal, VoucherDiscountAmount, EstimatedShippingFee, TotalAmount, IsDeleted, CreatedAt)
        VALUES
            (15, @cust_test, 12, 'ORD202607150001', N'Le Minh Khoa', '0912001999', N'123 Main St', '20101', N'Phường Bến Nghé', 1442, N'Quận 1', 202, N'Hồ Chí Minh',
                DATEADD(DAY, -10, @UtcNow28), DATEADD(DAY, -10, DATEADD(HOUR, 2, @UtcNow28)), DATEADD(DAY, -9, @UtcNow28), DATEADD(DAY, -8, @UtcNow28),
                'GHN-DFC1A2', 3,
                'SHIP_COD', 'PENDING', NULL, 1290000, 0, 30000, 1320000, 0, DATEADD(DAY, -10, @UtcNow28)),
            (16, @cust_test, 12, 'ORD202607160001', N'Le Minh Khoa', '0912001999', N'123 Main St', '20101', N'Phường Bến Nghé', 1442, N'Quận 1', 202, N'Hồ Chí Minh',
                DATEADD(DAY, -8, @UtcNow28), DATEADD(DAY, -8, DATEADD(HOUR, 2, @UtcNow28)), DATEADD(DAY, -7, @UtcNow28), DATEADD(DAY, -6, @UtcNow28),
                'GHN-DFC1A2', 3,
                'SHIP_COD', 'PENDING', NULL, 350000, 0, 30000, 380000, 0, DATEADD(DAY, -8, @UtcNow28)),
            (17, @cust_test, 12, 'ORD202607170001', N'Le Minh Khoa', '0912001999', N'123 Main St', '20101', N'Phường Bến Nghé', 1442, N'Quận 1', 202, N'Hồ Chí Minh',
                DATEADD(DAY, -5, @UtcNow28), DATEADD(DAY, -5, DATEADD(HOUR, 2, @UtcNow28)), DATEADD(DAY, -4, @UtcNow28), DATEADD(DAY, -3, @UtcNow28),
                'GHN-DFC1A2', 3,
                'SHIP_COD', 'PENDING', NULL, 420000, 0, 30000, 450000, 0, DATEADD(DAY, -5, @UtcNow28));
        SET IDENTITY_INSERT [dbo].[Orders] OFF;

        -- OrderDetails for these 3 orders
        INSERT INTO [dbo].[OrderDetails]
            (OrderID, ProductID, ProductName, ProductImage, Quantity, UnitPrice, DiscountAmount, CreatedAt)
        VALUES
            (15, 1, N'Lego City Central Police Station - 668 Pieces', 'https://picsum.photos/seed/prod-1/300/300', 1, 1290000, 0, DATEADD(DAY, -10, @UtcNow28)),
            (16, 5, N'Wooden Alphabet Blocks - 52 Pieces', 'https://picsum.photos/seed/prod-5/300/300', 1, 350000, 0, DATEADD(DAY, -8, @UtcNow28)),
            (17, 19, N'Pastel Long-Eared Bunny Plush Toy - 45 cm', 'https://picsum.photos/seed/prod-19/300/300', 1, 420000, 0, DATEADD(DAY, -5, @UtcNow28));

        -- Seed CustomerDeliveryAbuseCase for this customer to make it pending admin review immediately
        IF NOT EXISTS (SELECT 1 FROM [dbo].[CustomerDeliveryAbuseCases] WHERE AccountID = @cust_test)
        BEGIN
            INSERT INTO [dbo].[CustomerDeliveryAbuseCases]
                (AccountID, Status, WarningLevel, SuspiciousOrderCount, CountingFrom, LastGHNFailCode, LastSuspiciousOrderDate, CodRestrictedAt, ReviewRequestedAt, CreatedAt)
            VALUES
                (@cust_test, 'PENDING_ADMIN_REVIEW', 3, 3, DATEADD(MONTH, -5, @UtcNow28), 'GHN-DFC1A2', DATEADD(DAY, -5, @UtcNow28), DATEADD(DAY, -5, @UtcNow28), DATEADD(DAY, -5, @UtcNow28), @UtcNow28);
        END
    END
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
    DECLARE @revAdmin  INT = (SELECT TOP 1 AccountID FROM Accounts WHERE Email = 'admintoystore@gmail.com');

    -- Insert dynamic realistic product reviews for delivered/completed orders in English
    INSERT INTO [dbo].[ReviewProducts]
        (AccountID, ProductID, OrderID, Rating, Comment, ModerationStatus, IsDeleted, IsEdited, CreatedAt)
    SELECT
        o.AccountID,
        od.ProductID,
        od.OrderID,
        -- Rating: 4 or 5 stars (with occasional 3-star for realistic feedback)
        CAST(CASE WHEN (od.ProductID + od.OrderID) % 10 = 0 THEN 3 ELSE 4 + ((od.ProductID + od.OrderID) % 2) END AS TINYINT),
        -- Realistic category-focused English customer reviews
        CASE ((od.ProductID + od.OrderID) % 7)
            WHEN 0 THEN N'Outstanding quality! My child plays with this set every single day. Premium, durable build.'
            WHEN 1 THEN N'Super fast shipping and secure shock-proof packaging. Product matches the description perfectly!'
            WHEN 2 THEN N'Beautiful toy with great design details. Assembly guide is clear and easy to follow. Well worth the price!'
            WHEN 3 THEN N'Bought this as a birthday gift for my child and they absolutely love it! Smooth operation and very safe.'
            WHEN 4 THEN N'Safe non-toxic plastic with no odor. Smooth rounded edges give great peace of mind for parents.'
            WHEN 5 THEN N'Very soft and huggable plush toy with bright vivid colors. Holds its shape well after machine washing.'
            ELSE       N'Great educational toy that helps build problem-solving skills and patience. Will definitely buy again from ToyHouse!'
        END,
        -- Moderation Status: Approved (majority) with a few Pending/Flagged for moderation testing
        CASE 
            WHEN (od.ProductID + od.OrderID) % 15 = 0 THEN 'Pending'
            WHEN (od.ProductID + od.OrderID) % 17 = 0 THEN 'ManualReview'
            ELSE 'Approved'
        END,
        0, 0,
        DATEADD(HOUR, 12, ISNULL(o.DeliveredAt, DATEADD(DAY, 3, o.OrderDate)))
    FROM [dbo].[Orders] o
        JOIN [dbo].[OrderDetails] od ON od.OrderID = o.OrderID
    WHERE o.StatusID IN (6, 7); -- Delivered or Completed

    -- ReviewProductImages: attach 1 high quality review image for ~50% of reviews
    INSERT INTO [dbo].[ReviewProductImages]
        (ReviewProductID, ImageURL, ModerationStatus, IsDeleted, CreatedAt)
    SELECT
        rp.ReviewID,
        'https://picsum.photos/seed/product-review-' + CAST(rp.ReviewID AS VARCHAR) + '/400/400',
        rp.ModerationStatus,
        0,
        DATEADD(MINUTE, 5, rp.CreatedAt)
    FROM [dbo].[ReviewProducts] rp
    WHERE rp.ReviewID % 2 = 1;

    -- StaffReviewProductReplies: staff replies politely to approved reviews in English (~50%)
    INSERT INTO [dbo].[StaffReviewProductReplies]
        (ReviewProductID, StaffID, Content, IsDeleted, CreatedAt)
    SELECT
        rp.ReviewID,
        @revStaff1,
        N'Thank you for your wonderful review! ToyHouse appreciates your trust, and we wish your child happy and creative playtimes!',
        0,
        DATEADD(HOUR, 2, rp.CreatedAt)
    FROM [dbo].[ReviewProducts] rp
    WHERE rp.ReviewID % 2 = 0 AND rp.ModerationStatus = 'Approved';

    -- ReviewProductReactions: admin/staff likes helpful reviews
    INSERT INTO [dbo].[ReviewProductReactions]
        (ReviewProductID, AccountID, ReactionTypeID, IsDeleted, CreatedAt)
    SELECT
        rp.ReviewID,
        @revAdmin,
        1, -- ReactionTypeID 1 = Like/Helpful
        0,
        DATEADD(HOUR, 1, rp.CreatedAt)
    FROM [dbo].[ReviewProducts] rp
    WHERE rp.ModerationStatus = 'Approved';

    -- ReviewModerationLogs: AI auto-approved log records
    INSERT INTO [dbo].[ReviewModerationLogs]
        (TargetType, ReviewID, ImageID, ModeratorType, ModeratedBy,
        Action, AIModelVersion, ModerationResult, Reason, CreatedAt)
    SELECT
        'Text',
        rp.ReviewID,
        NULL,
        'AI',
        NULL,
        CASE WHEN rp.ModerationStatus = 'Approved' THEN 'Approved' ELSE 'ManualReview' END,
        'claude-moderation-v1.0',
        N'{"score":0.01,"categories":{"hate":false,"harassment":false,"spam":false},"passed":true}',
        NULL,
        DATEADD(MINUTE, 2, rp.CreatedAt)
    FROM [dbo].[ReviewProducts] rp;

    -- ReviewModerationLogs for review images
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
    DECLARE @UtcNow282 DATETIME2(0) = (SELECT UtcNow FROM #SeedTime);

    -- Customers
    DECLARE @c_lananh INT = (SELECT TOP 1 AccountID FROM Accounts WHERE Email = 'lananh.pham@gmail.com');
    DECLARE @c_hung   INT = (SELECT TOP 1 AccountID FROM Accounts WHERE Email = 'hung.nguyen88@gmail.com');
    DECLARE @c_bauchau INT = (SELECT TOP 1 AccountID FROM Accounts WHERE Email = 'bauchau.vu@gmail.com');
    DECLARE @c_tuan   INT = (SELECT TOP 1 AccountID FROM Accounts WHERE Email = 'mtuando@outlook.com');
    DECLARE @c_thuha  INT = (SELECT TOP 1 AccountID FROM Accounts WHERE Email = 'thuha.hoang@gmail.com');

    -- Refund Reasons
    DECLARE @r_defective INT = (SELECT TOP 1 RefundReasonID FROM OrderRefundReasons WHERE Content LIKE N'%defective%');
    DECLARE @r_wrong     INT = (SELECT TOP 1 RefundReasonID FROM OrderRefundReasons WHERE Content LIKE N'%Wrong product%');
    DECLARE @r_failed    INT = (SELECT TOP 1 RefundReasonID FROM OrderRefundReasons WHERE Content LIKE N'%Delivery failed%');

    IF @r_defective IS NULL SET @r_defective = 1;
    IF @r_wrong IS NULL SET @r_wrong = 2;
    IF @r_failed IS NULL SET @r_failed = 3;

    -- Orders
    DECLARE @o2 INT = (SELECT TOP 1 OrderID FROM Orders WHERE OrderCode = 'ORD202605010002');
    DECLARE @o4 INT = (SELECT TOP 1 OrderID FROM Orders WHERE OrderCode = 'ORD202605010004');
    DECLARE @o5 INT = (SELECT TOP 1 OrderID FROM Orders WHERE OrderCode = 'ORD202605010005');
    DECLARE @o6 INT = (SELECT TOP 1 OrderID FROM Orders WHERE OrderCode = 'ORD202605010006');
    DECLARE @o8 INT = (SELECT TOP 1 OrderID FROM Orders WHERE OrderCode = 'ORD202605010008');
    DECLARE @o10 INT = (SELECT TOP 1 OrderID FROM Orders WHERE OrderCode = 'ORD202605010010');

    -- 1. REF202607010001 (RefundRequested - StatusID = 1)
    IF @o5 IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[OrderRefunds]
            (OrderID, RefundReasonID, CustomerID, RequestedBy, RefundCode, SubTotal, TotalAmount, ApprovedAmount, StatusID, CreatedAt)
        VALUES
            (@o5, @r_defective, @c_bauchau, @c_bauchau, 'REF202607010001', 850000, 850000, 850000, 1, DATEADD(DAY, -15, @UtcNow282));

        DECLARE @rf1 INT = SCOPE_IDENTITY();
        INSERT INTO [dbo].[RefundDetails] (RefundID, ProductID, Quantity, UnitPrice, RefundAmount, CreatedAt)
        VALUES (@rf1, 8, 1, 850000, 850000, DATEADD(DAY, -15, @UtcNow282));

        INSERT INTO [dbo].[RefundImages] (RefundID, ImageURL, CreatedAt)
        VALUES 
            (@rf1, 'https://picsum.photos/seed/refund-defective-a/400/400', DATEADD(DAY, -15, @UtcNow282)),
            (@rf1, 'https://picsum.photos/seed/refund-defective-b/400/400', DATEADD(DAY, -15, @UtcNow282));
    END

    -- 2. REF202607010002 (RefundApproved - StatusID = 2)
    IF @o2 IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[OrderRefunds]
            (OrderID, RefundReasonID, CustomerID, RequestedBy, RefundCode, SubTotal, TotalAmount, ApprovedAmount, StatusID, CreatedAt)
        VALUES
            (@o2, @r_wrong, @c_lananh, @c_lananh, 'REF202607010002', 680000, 680000, 680000, 2, DATEADD(DAY, -20, @UtcNow282));

        DECLARE @rf2 INT = SCOPE_IDENTITY();
        INSERT INTO [dbo].[RefundDetails] (RefundID, ProductID, Quantity, UnitPrice, RefundAmount, CreatedAt)
        VALUES (@rf2, 27, 1, 680000, 680000, DATEADD(DAY, -20, @UtcNow282));

        INSERT INTO [dbo].[RefundImages] (RefundID, ImageURL, CreatedAt)
        VALUES (@rf2, 'https://picsum.photos/seed/refund-wrong-a/400/400', DATEADD(DAY, -20, @UtcNow282));
    END

    -- 3. REF202607010003 (RefundRejected - StatusID = 3)
    IF @o4 IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[OrderRefunds]
            (OrderID, RefundReasonID, CustomerID, RequestedBy, RefundCode, SubTotal, TotalAmount, ApprovedAmount, StatusID, CreatedAt)
        VALUES
            (@o4, @r_defective, @c_hung, @c_hung, 'REF202607010003', 88000, 88000, 0, 3, DATEADD(DAY, -25, @UtcNow282));

        DECLARE @rf3 INT = SCOPE_IDENTITY();
        INSERT INTO [dbo].[RefundDetails] (RefundID, ProductID, Quantity, UnitPrice, RefundAmount, CreatedAt)
        VALUES (@rf3, 29, 1, 88000, 88000, DATEADD(DAY, -25, @UtcNow282));

        INSERT INTO [dbo].[RefundImages] (RefundID, ImageURL, CreatedAt)
        VALUES (@rf3, 'https://picsum.photos/seed/refund-rejected-a/400/400', DATEADD(DAY, -25, @UtcNow282));
    END

    -- 4. REF202607010004 (RefundShipping - StatusID = 5)
    IF @o6 IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[OrderRefunds]
            (OrderID, RefundReasonID, CustomerID, RequestedBy, RefundCode, SubTotal, TotalAmount, ApprovedAmount, StatusID, CreatedAt)
        VALUES
            (@o6, @r_wrong, @c_bauchau, @c_bauchau, 'REF202607010004', 900000, 900000, 900000, 5, DATEADD(DAY, -10, @UtcNow282));

        DECLARE @rf4 INT = SCOPE_IDENTITY();
        INSERT INTO [dbo].[RefundDetails] (RefundID, ProductID, Quantity, UnitPrice, RefundAmount, CreatedAt)
        VALUES (@rf4, 22, 1, 900000, 900000, DATEADD(DAY, -10, @UtcNow282));

        INSERT INTO [dbo].[RefundImages] (RefundID, ImageURL, CreatedAt)
        VALUES (@rf4, 'https://picsum.photos/seed/refund-shipping-a/400/400', DATEADD(DAY, -10, @UtcNow282));
    END

    -- 5. REF202607010005 (RefundCompleted - StatusID = 8)
    IF @o8 IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[OrderRefunds]
            (OrderID, RefundReasonID, CustomerID, RequestedBy, RefundCode, SubTotal, TotalAmount, ApprovedAmount, StatusID, CreatedAt)
        VALUES
            (@o8, @r_defective, @c_tuan, @c_tuan, 'REF202607010005', 945000, 945000, 945000, 8, DATEADD(DAY, -30, @UtcNow282));

        DECLARE @rf5 INT = SCOPE_IDENTITY();
        INSERT INTO [dbo].[RefundDetails] (RefundID, ProductID, Quantity, UnitPrice, RefundAmount, CreatedAt)
        VALUES (@rf5, 26, 1, 945000, 945000, DATEADD(DAY, -30, @UtcNow282));

        INSERT INTO [dbo].[RefundImages] (RefundID, ImageURL, CreatedAt)
        VALUES (@rf5, 'https://picsum.photos/seed/refund-completed-a/400/400', DATEADD(DAY, -30, @UtcNow282));
    END

    -- 6. REF202607010006 (RefundCancelled - StatusID = 9)
    IF @o10 IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[OrderRefunds]
            (OrderID, RefundReasonID, CustomerID, RequestedBy, RefundCode, SubTotal, TotalAmount, ApprovedAmount, StatusID, CreatedAt)
        VALUES
            (@o10, @r_failed, @c_thuha, @c_thuha, 'REF202607010006', 175000, 175000, 0, 9, DATEADD(DAY, -8, @UtcNow282));

        DECLARE @rf6 INT = SCOPE_IDENTITY();
        INSERT INTO [dbo].[RefundDetails] (RefundID, ProductID, Quantity, UnitPrice, RefundAmount, CreatedAt)
        VALUES (@rf6, 30, 1, 175000, 175000, DATEADD(DAY, -8, @UtcNow282));

        INSERT INTO [dbo].[RefundImages] (RefundID, ImageURL, CreatedAt)
        VALUES (@rf6, 'https://picsum.photos/seed/refund-cancelled-a/400/400', DATEADD(DAY, -8, @UtcNow282));
    END
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 31 – BLOG
   NOTE: BlogPostStats is auto-created by trigger
         trg_BlogPost_InitStats on INSERT BlogPosts – no manual seed needed
══════════════════════════════════════════════════════════════ */
PRINT N'[31] Blog...';
DECLARE @UtcNow31 DATETIME2(0) = (SELECT UtcNow
FROM #SeedTime);
DECLARE @BlogLookupCreated DATETIME2(0) = DATEADD(MONTH, -6, @UtcNow31);

SET IDENTITY_INSERT [dbo].[BlogCategories] ON;

IF NOT EXISTS (SELECT 1
FROM [dbo].[BlogCategories]
WHERE BlogCategoryID = 1)
    INSERT INTO [dbo].[BlogCategories]
    (BlogCategoryID, BlogCategoriesName, CreatedAt)
VALUES
    (1, N'News & Promotions', @BlogLookupCreated),
    (2, N'Parenting Knowledge', @BlogLookupCreated),
    (3, N'Product Reviews', @BlogLookupCreated),
    (4, N'Play & Creativity', @BlogLookupCreated),
    (5, N'Toy Safety', @BlogLookupCreated);

SET IDENTITY_INSERT [dbo].[BlogCategories] OFF;
GO

DECLARE @UtcNow31b DATETIME2(0) = (SELECT UtcNow
FROM #SeedTime);
DECLARE @bst1 INT = (SELECT TOP 1
    AccountID
FROM Accounts
WHERE Email = 'nhung.st@toyhouse.vn');
DECLARE @bst2 INT = (SELECT TOP 1
    AccountID
FROM Accounts
WHERE Email = 'dung.st@toyhouse.vn');
DECLARE @badm INT = (SELECT TOP 1
    AccountID
FROM Accounts
WHERE Email = 'admintoystore@gmail.com');

IF NOT EXISTS (SELECT 1
FROM [dbo].[BlogPosts])
    INSERT INTO [dbo].[BlogPosts]
    (AccountID, ApprovedBy, BlogCategoryID, BlogTitle, BlogContent, BlogThumbnail, Status, IsFeatured, BlogAt, CreatedAt)
VALUES
    (@bst1, @badm, 1, N'Summer Sale – Up To 50% Off Official Toys',
        N'The Summer Toy Festival 2026 is live! Lego, Bandai, and Fisher-Price are discounted up to 50%.',
        'https://picsum.photos/seed/blog-sale-he/600/400', 'Published', 1, DATEADD(DAY, -14, @UtcNow31b), DATEADD(DAY, -14, @UtcNow31b)),
    (@bst1, @badm, 2, N'7 Golden Rules for Choosing Safe Toys for Children Under 3',
        N'Ages 0–3 are a critical development period. Look for BPA-free materials, rounded edges, and non-toxic water-based paint.',
        'https://picsum.photos/seed/blog-safety/600/400', 'Published', 0, DATEADD(DAY, -45, @UtcNow31b), DATEADD(DAY, -45, @UtcNow31b)),
    (@bst2, @badm, 3, N'Hands-On Review: Lego City Police Station After 3 Months',
        N'This 668-piece set holds up well after 3 months. Bricks stay firm and do not break. A 7-year-old can assemble about 80% independently.',
        'https://picsum.photos/seed/blog-review-lego/600/400', 'Published', 1, DATEADD(DAY, -20, @UtcNow31b), DATEADD(DAY, -20, @UtcNow31b)),
    (@bst2, @badm, 4, N'Top 10 Active Toys That Build Physical Skills for Ages 3–6',
        N'Active play supports both health and cognitive growth. Here are the 10 most popular movement toys for preschoolers.',
        'https://picsum.photos/seed/blog-activity/600/400', 'Published', 1, DATEADD(DAY, -10, @UtcNow31b), DATEADD(DAY, -10, @UtcNow31b)),
    (@bst1, @badm, 1, N'48-Hour Flash Sale – Mega Deals Starting Now',
        N'Only 48 hours! Flash Sale with up to 50% off hundreds of products. Limited stock available!',
        'https://picsum.photos/seed/blog-flash55/600/400', 'Published', 0, DATEADD(DAY, -1, @UtcNow31b), DATEADD(DAY, -1, @UtcNow31b)),
    (@bst2, @badm, 2, N'Back To School Buying Guide 2026',
        N'Everything parents need to know before the new school year – flash cards, science kits, and learning toys.',
        'https://picsum.photos/seed/blog-bts/600/400', 'Scheduled', 1, DATEADD(DAY, 18, @UtcNow31b), @UtcNow31b),
    (@bst1, @badm, 1, N'Halloween Toy Preview 2026',
        N'Spooky figures, dress-up sets, and limited-edition collectibles arriving this October.',
        'https://picsum.photos/seed/blog-halloween/600/400', 'Scheduled', 0, DATEADD(DAY, 99, @UtcNow31b), @UtcNow31b),
    (@bst1, NULL, 1, N'Member Week Draft – Work in Progress',
        N'Draft content for the upcoming member appreciation week campaign.',
        'https://picsum.photos/seed/blog-draft/600/400', 'Draft', 0, NULL, @UtcNow31b),
    (@bst2, NULL, 1, N'Flash Sale Recap – Pending Review',
        N'Weekly recap of flash sale highlights and bestsellers – awaiting admin approval.',
        'https://picsum.photos/seed/blog-pending/600/400', 'Pending', 0, DATEADD(DAY, 3, @UtcNow31b), @UtcNow31b);
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[ReviewBlogs])
BEGIN
    DECLARE @UtcNow31c DATETIME2(0) = (SELECT UtcNow FROM #SeedTime);
    DECLARE @staffRep INT = (SELECT TOP 1 AccountID FROM Accounts WHERE Email = 'nhung.st@toyhouse.vn');
    DECLARE @adminRep INT = (SELECT TOP 1 AccountID FROM Accounts WHERE Email = 'admintoystore@gmail.com');

    -- 1. Comments for Post: Summer Sale – Up To 50% Off Official Toys
    INSERT INTO [dbo].[ReviewBlogs]
        (BlogPostID, AccountID, Comment, CreatedAt)
    SELECT bp.BlogPostID, a.AccountID, v.Comment, DATEADD(HOUR, v.HourOffset, bp.BlogAt)
    FROM [dbo].[BlogPosts] bp
    CROSS JOIN (VALUES
        ('lananh.pham@gmail.com', N'Just grabbed the Lego City Police Station during this sale! 15% off was an awesome deal.', 5),
        ('hung.nguyen88@gmail.com', N'Ordered the RC Traxxas off-road truck during the flash sale hour! Saved a lot on shipping.', 12),
        ('bauchau.vu@gmail.com', N'Can I stack the 50K new member voucher with the summer promotion discount at checkout?', 18),
        ('thuha.hoang@gmail.com', N'Free shipping was automatically applied on my order over 500k. Super smooth shopping experience!', 24)
    ) AS v(Email, Comment, HourOffset)
        JOIN Accounts a ON a.Email = v.Email
    WHERE bp.BlogTitle = N'Summer Sale – Up To 50% Off Official Toys';

    -- 2. Comments for Post: 7 Golden Rules for Choosing Safe Toys for Children Under 3
    INSERT INTO [dbo].[ReviewBlogs]
        (BlogPostID, AccountID, Comment, CreatedAt)
    SELECT bp.BlogPostID, a.AccountID, v.Comment, DATEADD(HOUR, v.HourOffset, bp.BlogAt)
    FROM [dbo].[BlogPosts] bp
    CROSS JOIN (VALUES
        ('lananh.pham@gmail.com', N'Such an insightful article! I used to buy toys purely on instinct without checking edge smoothness.', 6),
        ('thuha.hoang@gmail.com', N'Does ToyHouse certify that all wooden toys use 100% non-toxic water-based paints?', 14),
        ('mtuando@outlook.com', N'Forwarded this to my mothers group. Safe teether materials are so crucial for 1-year-olds!', 22),
        ('minh.tran92@gmail.com', N'Great tips on avoiding small detachable parts that pose choking hazards for toddlers.', 30)
    ) AS v(Email, Comment, HourOffset)
        JOIN Accounts a ON a.Email = v.Email
    WHERE bp.BlogTitle = N'7 Golden Rules for Choosing Safe Toys for Children Under 3';

    -- 3. Comments for Post: Hands-On Review: Lego City Police Station After 3 Months
    INSERT INTO [dbo].[ReviewBlogs]
        (BlogPostID, AccountID, Comment, CreatedAt)
    SELECT bp.BlogPostID, a.AccountID, v.Comment, DATEADD(HOUR, v.HourOffset, bp.BlogAt)
    FROM [dbo].[BlogPosts] bp
    CROSS JOIN (VALUES
        ('hung.nguyen88@gmail.com', N'My 7-year-old built about 80% of this set on his own over the weekend! Clutch power is top notch.', 8),
        ('mtuando@outlook.com', N'Great hands-on review! How does the ABS plastic hold up if dropped on tile floors?', 16),
        ('lananh.pham@gmail.com', N'Did your set come with all 668 pieces intact or were any small connectors missing?', 24),
        ('bauchau.vu@gmail.com', N'We have had this set for 3 months as well and it remains my son''s absolute favorite building toy.', 32)
    ) AS v(Email, Comment, HourOffset)
        JOIN Accounts a ON a.Email = v.Email
    WHERE bp.BlogTitle = N'Hands-On Review: Lego City Police Station After 3 Months';

    -- Specific topic-focused Staff Replies for customer inquiries
    -- Reply to Bau Chau's voucher stacking question
    INSERT INTO [dbo].[ReviewBlogReplies]
        (ReviewBlogID, AccountID, Comment, CreatedAt)
    SELECT rb.ReviewBlogID, @staffRep,
        N'Hi Bau Chau! Yes, you can combine store-wide promotion discounts with valid voucher codes at checkout!',
        DATEADD(HOUR, 2, rb.CreatedAt)
    FROM ReviewBlogs rb
    WHERE rb.Comment LIKE N'%stack%';

    -- Reply to Thu Ha's safety & non-toxic paint question
    INSERT INTO [dbo].[ReviewBlogReplies]
        (ReviewBlogID, AccountID, Comment, CreatedAt)
    SELECT rb.ReviewBlogID, @staffRep,
        N'Hi Thu Ha! All wooden toys at ToyHouse undergo strict EN71 & ASTM safety testing using 100% non-toxic water-based paints.',
        DATEADD(HOUR, 2, rb.CreatedAt)
    FROM ReviewBlogs rb
    WHERE rb.Comment LIKE N'%non-toxic%';

    -- Reply to Tuan's Lego durability question
    INSERT INTO [dbo].[ReviewBlogReplies]
        (ReviewBlogID, AccountID, Comment, CreatedAt)
    SELECT rb.ReviewBlogID, @staffRep,
        N'Hi Tuan! Lego ABS plastic is highly impact-resistant. Even after drops on tiles, the bricks maintain perfect clutch power!',
        DATEADD(HOUR, 2, rb.CreatedAt)
    FROM ReviewBlogs rb
    WHERE rb.Comment LIKE N'%tile floors%';

    -- Reactions: admin & customers like helpful comments
    INSERT INTO [dbo].[ReviewBlogReactions]
        (ReviewBlogID, AccountID, ReactionTypeID, CreatedAt)
    SELECT rb.ReviewBlogID, @adminRep, 1, DATEADD(MINUTE, 30, rb.CreatedAt)
    FROM ReviewBlogs rb;

    -- BlogPostReactions: likes and hearts on blog posts
    INSERT INTO [dbo].[BlogPostReactions]
        (BlogPostID, AccountID, ReactionTypeID, CreatedAt)
    SELECT b.BlogPostID, a.AccountID, v.ReactionTypeID, DATEADD(MINUTE, 30, b.BlogAt)
    FROM BlogPosts b
    CROSS JOIN (VALUES
        ('lananh.pham@gmail.com', 1),
        ('hung.nguyen88@gmail.com', 2),
        ('bauchau.vu@gmail.com', 1),
        ('thuha.hoang@gmail.com', 1),
        ('mtuando@outlook.com', 2)
    ) AS v(Email, ReactionTypeID)
        JOIN Accounts a ON a.Email = v.Email
    WHERE b.Status = 'Published'
        AND NOT EXISTS (
            SELECT 1 FROM BlogPostReactions bpr 
            WHERE bpr.BlogPostID = b.BlogPostID AND bpr.AccountID = a.AccountID
        );
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 31.1 – DYNAMIC BLOGS AND COMMENTS
   Generates realistic blog posts and comments (reviews/reactions)
   from today/June 4, 2026 up to September 30, 2026.
══════════════════════════════════════════════════════════════ */
PRINT N'[31.1] Dynamic Blogs and Comments Seeding...';

DECLARE @bStartDate DATE = (SELECT UtcToday
FROM #SeedTime);
DECLARE @bEndDate DATE = DATEADD(MONTH, 6, @bStartDate);

DECLARE @bCurrentDate DATE = @bStartDate;
DECLARE @blogNum INT = 1;

DECLARE @staff1 INT = (SELECT TOP 1
    AccountID
FROM Accounts
WHERE Email = 'nhung.st@toyhouse.vn');
DECLARE @staff2 INT = (SELECT TOP 1
    AccountID
FROM Accounts
WHERE Email = 'dung.st@toyhouse.vn');
DECLARE @adminId INT = (SELECT TOP 1
    AccountID
FROM Accounts
WHERE Email = 'admintoystore@gmail.com');

-- Get the customer accounts into a table variable for easy indexing
DECLARE @Customers TABLE (
    Idx INT IDENTITY(1,1) PRIMARY KEY,
    AccID INT NOT NULL,
    Email VARCHAR(100) NOT NULL
);
INSERT INTO @Customers
    (AccID, Email)
SELECT AccountID, Email
FROM Accounts
WHERE Email IN (
    'lananh.pham@gmail.com', 'hung.nguyen88@gmail.com', 'bauchau.vu@gmail.com', 'mtuando@outlook.com', 'thuha.hoang@gmail.com'
)
ORDER BY Email;

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

        IF NOT EXISTS (SELECT 1
        FROM [dbo].[BlogPosts]
        WHERE BlogTitle = @title)
        BEGIN
            INSERT INTO [dbo].[BlogPosts]
                (AccountID, ApprovedBy, BlogCategoryID, BlogTitle, BlogContent, BlogThumbnail, Status, IsFeatured, BlogAt, CreatedAt)
            VALUES
                (@authorID, @adminId, @catID, @title, @content, @thumb, 'Published', CASE WHEN @blogNum % 4 = 0 THEN 1 ELSE 0 END, @bDate, @bDate);

            DECLARE @newPostID INT = SCOPE_IDENTITY();

            -- 1. Insert 1-2 Comments (ReviewBlogs)
            DECLARE @commentCount INT = (@blogNum % 2) + 1;
            -- 1 or 2 comments
            DECLARE @c INT = 1;

            WHILE @c <= @commentCount
            BEGIN
                DECLARE @custIdx INT = ((@blogNum + @c) % 5) + 1;
                DECLARE @custID INT = (SELECT AccID
                FROM @Customers
                WHERE Idx = @custIdx);
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

                INSERT INTO [dbo].[ReviewBlogs]
                    (BlogPostID, AccountID, Comment, ModerationStatus, IsHidden, IsDeleted, CreatedAt)
                VALUES
                    (@newPostID, @custID, @commentText, 'Approved', 0, 0, @commentDate);

                DECLARE @newCommentID INT = SCOPE_IDENTITY();

                -- 2. Staff Reply (ReviewBlogReplies) for 50% of comments
                IF (@blogNum + @c) % 2 = 0
                BEGIN
                    DECLARE @replyText NVARCHAR(500) = N'Thank you for your comment! If you have any further questions, feel free to contact our support hotline.';
                    INSERT INTO [dbo].[ReviewBlogReplies]
                        (ReviewBlogID, AccountID, Comment, CreatedAt)
                    VALUES
                        (@newCommentID, @authorID, @replyText, DATEADD(MINUTE, 30, @commentDate));
                END;

                -- 3. Review Reactions
                INSERT INTO [dbo].[ReviewBlogReactions]
                    (ReviewBlogID, AccountID, ReactionTypeID, CreatedAt)
                VALUES
                    (@newCommentID, @adminId, 1, DATEADD(MINUTE, 45, @commentDate));

                SET @c = @c + 1;
            END;

            -- 4. Blog Post Reactions (Likes/Love from 2-3 users)
            DECLARE @rCust1 INT = (SELECT AccID
            FROM @Customers
            WHERE Idx = 1);
            DECLARE @rCust2 INT = (SELECT AccID
            FROM @Customers
            WHERE Idx = 2);
            DECLARE @rCust3 INT = (SELECT AccID
            FROM @Customers
            WHERE Idx = 3);

            INSERT INTO [dbo].[BlogPostReactions]
                (BlogPostID, AccountID, ReactionTypeID, CreatedAt)
            VALUES
                (@newPostID, @rCust1, 1, DATEADD(MINUTE, 10, @bDate)),
                (@newPostID, @rCust2, 2, DATEADD(MINUTE, 25, @bDate));

            IF @blogNum % 2 = 0
                INSERT INTO [dbo].[BlogPostReactions]
                (BlogPostID, AccountID, ReactionTypeID, CreatedAt)
            VALUES
                (@newPostID, @rCust3, 1, DATEADD(MINUTE, 40, @bDate));
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
DECLARE @UtcNow33 DATETIME2(0) = (SELECT UtcNow
FROM #SeedTime);

SET IDENTITY_INSERT [dbo].[ShiftTemplates] ON;
IF NOT EXISTS (SELECT 1
FROM [dbo].[ShiftTemplates]
WHERE ShiftTemplateID = 1)
    INSERT INTO [dbo].[ShiftTemplates]
    (ShiftTemplateID, ShiftName, StartTime, EndTime, MaxOrdersPerShift, IsActive, CreatedAt)
VALUES
    (1, N'Morning Shift', '07:00:00', '12:00:00', 20, 1, @UtcNow33),
    (2, N'Afternoon Shift', '12:00:00', '17:00:00', 25, 1, @UtcNow33),
    (3, N'Evening Shift', '17:00:00', '22:00:00', 25, 1, @UtcNow33);
SET IDENTITY_INSERT [dbo].[ShiftTemplates] OFF;
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 33.1 – WORK SCHEDULES (next 14 days)
══════════════════════════════════════════════════════════════ */
PRINT N'[33.1] WorkSchedules...';

IF NOT EXISTS (SELECT 1
FROM [dbo].[WorkSchedules])
BEGIN
    DECLARE @UtcNow331 DATETIME2(0) = (SELECT UtcNow
    FROM #SeedTime);
    DECLARE @UtcToday331 DATE = (SELECT UtcToday
    FROM #SeedTime);
    DECLARE @admWS INT = (SELECT TOP 1
        AccountID
    FROM Accounts
    WHERE Email = 'admintoystore@gmail.com');
    DECLARE @st1WS INT = (SELECT TOP 1
        AccountID
    FROM Accounts
    WHERE Email = 'nhung.st@toyhouse.vn');
    DECLARE @st2WS INT = (SELECT TOP 1
        AccountID
    FROM Accounts
    WHERE Email = 'dung.st@toyhouse.vn');
    DECLARE @mc1WS INT = (SELECT TOP 1
        AccountID
    FROM Accounts
    WHERE Email = 'bao.kho@toyhouse.vn');

    DECLARE @d INT = 0;
    WHILE @d <= 24
    BEGIN
        DECLARE @workDate DATE = DATEADD(DAY, @d, @UtcToday331);
        INSERT INTO [dbo].[WorkSchedules]
            (AccountID, ShiftTemplateID, WorkDate, Status, CreatedBy, CreatedAt)
        VALUES
            (@st1WS, 1 + (@d % 3), @workDate, 'Scheduled', @admWS, @UtcNow331),
            (@st2WS, 1 + ((@d + 1) % 3), @workDate, 'Scheduled', @admWS, @UtcNow331),
            (@mc1WS, 2, @workDate, 'Scheduled', @admWS, @UtcNow331);
        SET @d = @d + 1;
    END
END
GO
/* ══════════════════════════════════════════════════════════════
   SECTION 35 – NOTIFICATION TEMPLATES, CAMPAIGNS, DELIVERIES
══════════════════════════════════════════════════════════════ */
BEGIN TRY
    BEGIN TRAN;

    DECLARE @Tpl TABLE (
    TemplateCode VARCHAR(50) NOT NULL PRIMARY KEY,
    UsageScope VARCHAR(10) NOT NULL,
    TitleTemplate NVARCHAR(255) NOT NULL,
    MessageTemplate NVARCHAR(500) NOT NULL
    );

    /* ─── SYSTEM GROUP: automatic, cannot be disabled/deleted ─── */

    -- Orders — customer & staff
    INSERT INTO @Tpl
VALUES
    ('ORDER_PLACED', 'SYSTEM', N'Order placed successfully',
        N'Your order {{OrderCode}} ({{TotalAmount}} VND) has been received.'),
    ('ORDER_CONFIRMED', 'SYSTEM', N'Order confirmed: {{OrderCode}}',
        N'Your order {{OrderCode}} has been confirmed and is being prepared.'),
    ('ORDER_PACKING', 'SYSTEM', N'Order {{OrderCode}} is being packed',
        N'Your order {{OrderCode}} is being packed by our team.'),
    ('ORDER_SHIPPING', 'SYSTEM', N'Order {{OrderCode}} is on the way',
        N'Order {{OrderCode}} has been handed to courier {{ShipperName}}. Please keep your phone available.'),
    ('ORDER_DELIVERED', 'SYSTEM', N'Delivery successful',
        N'Order {{OrderCode}} has been delivered successfully. We would love to hear your feedback.'),
    ('ORDER_CANCELLED', 'SYSTEM', N'Order {{OrderCode}} cancelled',
        N'Your order {{OrderCode}} was cancelled. Reason: {{CancelReason}}.'),
    ('ORDER_DELIVERY_FAILED', 'SYSTEM', N'Delivery unsuccessful',
        N'The courier could not reach you. Another delivery attempt will be made. Please keep your phone nearby.'),
    ('ORDER_RETURN_REFUND_PENDING', 'SYSTEM', N'Returned to shop',
        N'Order {{OrderCode}} has returned to our shop. We are processing your refund.'),
    ('ORDER_CANCELLED_DELIVERY_FAIL', 'SYSTEM', N'Order cancelled',
        N'Order {{OrderCode}} was cancelled due to failed delivery.'),
    ('ORDER_ASSIGNED', 'SYSTEM', N'Order Assigned: {{OrderCode}}',
        N'Order {{OrderCode}} from {{CustomerName}} has been assigned to you. Total: {{TotalAmount}} VND. Please process it during your current shift.');

    -- Payments & wallet
    INSERT INTO @Tpl
VALUES
    ('PAYMENT_SUCCESS', 'SYSTEM', N'Payment successful',
        N'You paid {{Amount}} VND for order {{OrderCode}}.'),
    ('PAYMENT_FAILED', 'SYSTEM', N'Payment failed',
        N'Payment of {{Amount}} VND for order {{OrderCode}} failed.'),
    ('WALLET_TOPUP', 'SYSTEM', N'Wallet top-up successful',
        N'{{Amount}} VND was added to your wallet. Current balance: {{Balance}} VND.'),
    ('WALLET_REFUND', 'SYSTEM', N'Refund to wallet',
        N'{{Amount}} VND was refunded to your wallet for order {{OrderCode}}.'),
    ('WALLET_WITHDRAWAL_SUCCESS', 'SYSTEM', N'Withdrawal successful',
        N'{{Amount}} VND has been transferred to {{BankName}} - {{AccountNumber}} successfully.'),
    ('WALLET_WITHDRAWAL_FAILED', 'SYSTEM', N'Withdrawal failed',
        N'Withdrawal of {{Amount}} VND failed. Reason: {{FailReason}}. Your wallet balance was not changed.'),
    ('REFUND_APPROVED', 'SYSTEM', N'Refund approved',
        N'Your refund request for order {{OrderCode}} was approved. {{Amount}} VND will be returned to your wallet.'),
    ('REFUND_REJECTED', 'SYSTEM', N'Refund rejected',
        N'Your refund request for order {{OrderCode}} was rejected. Contact support if you need help.'),
    ('REFUND_COMPLETED', 'SYSTEM', N'Refund completed',
        N'{{Amount}} VND from order {{OrderCode}} has been refunded to your wallet.');

    -- Products & Stock
    INSERT INTO @Tpl
VALUES
    ('PRODUCT_BACK_IN_STOCK', 'SYSTEM', N'Product back in stock: {{ProductName}}',
        N'The product {{ProductName}} you are interested in is back in stock at {{Price}} VND. Get it now before it runs out!'),
    ('WISHLIST_PRICE_DROP', 'SYSTEM', N'Price drop on {{ProductName}}',
        N'{{ProductName}} in your wishlist is now on sale for only {{Price}} VND.');

    -- Reviews & Blogs
    INSERT INTO @Tpl
VALUES
    ('REVIEW_STAFF_REPLIED', 'SYSTEM', N'Reply to review on {{ProductName}}',
        N'A customer service representative has replied to your review of {{ProductName}}.'),
    ('BLOG_COMMENT_REPLIED', 'SYSTEM', N'Blog comment reply: {{BlogTitle}}',
        N'Someone replied to your comment on {{BlogTitle}}.');

    -- Staff Notifications
    INSERT INTO @Tpl
VALUES
    ('STAFF_NEW_ORDER', 'SYSTEM', N'New order received: {{OrderCode}}',
        N'The system registered a new order {{OrderCode}} worth {{TotalAmount}} VND. Please process it.'),
    ('STAFF_CANCEL_REQUEST', 'SYSTEM', N'Cancellation request for {{OrderCode}}',
        N'Customer {{CustomerName}} has requested to cancel order {{OrderCode}}. Reason: {{Reason}}.'),
    ('STAFF_REFUND_REQUEST', 'SYSTEM', N'Refund request for {{OrderCode}}',
        N'Customer {{CustomerName}} requested a refund for order {{OrderCode}}.'),
    ('STAFF_SYSTEM_REFUND_READY', 'SYSTEM', N'Inspection complete — refund pending',
        N'Merchandise has inspected the returned items for order {{OrderCode}}. Please confirm the wallet refund.'),
    ('STAFF_REVIEW_MODERATION', 'SYSTEM', N'Approve new review',
        N'There is a new {{Rating}}-star review for product {{ProductName}} that requires moderation.'),
    ('STAFF_LOW_RATING', 'SYSTEM', N'Low rating warning',
        N'Product {{ProductName}} just received a {{Rating}}-star review. Please check and resolve.'),
    ('STAFF_SHIFT_STARTED', 'SYSTEM', N'Shift {{ShiftName}} started',
        N'Your work shift {{ShiftName}} has started. Have a great shift!');

    -- Merchandise Notifications
    INSERT INTO @Tpl
VALUES
    ('MERCH_READY_TO_PACK', 'SYSTEM', N'Order ready for packing',
        N'Order {{OrderCode}} is ready to be packed.'),
    ('MERCH_PICKED_UP', 'SYSTEM', N'Package picked up by courier',
        N'The courier has successfully picked up the package for order {{OrderCode}}.'),
    ('MERCH_RETURNED', 'SYSTEM', N'GHN package returned to shop',
        N'Order {{OrderCode}} has been returned by GHN. Please confirm receipt and inspect the items.'),
    ('MERCH_LOW_STOCK', 'SYSTEM', N'Low stock warning',
        N'Product {{ProductName}} has only {{Quantity}} units left. Restocking is needed.'),
    ('MERCH_OUT_OF_STOCK', 'SYSTEM', N'Out of stock warning',
        N'Product {{ProductName}} is completely out of stock.');

    -- Admin Notifications
    INSERT INTO @Tpl
VALUES
    ('ADMIN_PAYMENT_ERROR', 'SYSTEM', N'Payment gateway error',
        N'Payment gateway {{GatewayName}} reported error: {{ErrorMessage}}.'),
    ('ADMIN_JOB_FAILED', 'SYSTEM', N'Background job failed',
        N'Background Job {{JobName}} failed. Please check the logs.'),
    ('ADMIN_OUTBOX_STUCK', 'SYSTEM', N'Outbox event stuck',
        N'Outbox event {{EventType}} ({{EventId}}) has reached retry limits. Please check logs.'),
    ('ADMIN_SHIPPING_ERROR', 'SYSTEM', N'Shipping sync error',
        N'Error synchronizing shipping status for order {{OrderCode}}: {{ErrorMessage}}.'),
    ('ADMIN_DAMAGE_LOST', 'SYSTEM', N'Items damaged/lost',
        N'Order {{OrderCode}} reported as damaged or lost during delivery.'),
    ('ADMIN_RETURN_FAIL', 'SYSTEM', N'GHN return failed',
        N'Order {{OrderCode}} (GHN: {{ProviderOrderCode}}) reported return_fail. Please process manually.'),
    ('ADMIN_BLOG_PENDING', 'SYSTEM', N'Blog pending approval: {{BlogTitle}}',
        N'Blog post {{BlogTitle}} was submitted and is waiting for your approval.'),
    ('ADMIN_ORDER_QUEUED', 'SYSTEM', N'Order {{OrderCode}} pending assignment',
        N'Order {{OrderCode}} is pending assignment due to: {{Reason}}.'),
    ('ADMIN_SHIFT_ENDED_PENDING', 'SYSTEM', N'Shift ended with pending orders',
        N'Shift {{ShiftName}} (staff #{{AccountId}}) ended with {{CurrentLoad}} orders still pending.'),
    ('ADMIN_SHIFT_FULL', 'SYSTEM', N'Shift capacity reached',
        N'One or both roles in shift {{ShiftName}} reached order capacity on {{WorkDate}}. Please manage manually.');

    -- Birthday Notifications (Automated Background Job)
    INSERT INTO @Tpl
VALUES
    ('BIRTHDAY_CUSTOMER', 'SYSTEM', N'Happy Birthday, {{CustomerName}}!',
        N'Happy birthday to you! ToyStore has sent you a special gift. Please check your Voucher wallet!'),
    ('BIRTHDAY_CHILD', 'SYSTEM', N'Happy Birthday, {{ChildName}}!',
        N'Happy birthday to {{ChildName}}! ToyStore wishes them healthy growth and joy. Parents, pick a favorite toy for them!');

    /* ─── ADMIN GROUP: marketing, togglable ─────────────────────── */
    INSERT INTO @Tpl
VALUES
    ('FLASH_SALE_STARTED', 'ADMIN', N'🔥 Flash Sale Alert: {{PromotionName}} is NOW LIVE!',
        N'The wait is over! {{PromotionName}} has officially started ({{StartDate}} – {{EndDate}}). Exclusive deals are waiting — shop your favorites before stock runs out!'),
    ('VOUCHER_NEW', 'ADMIN', N'🎁 Special Gift For You: Claim {{VoucherCode}} Now!',
        N'We''ve unlocked an exclusive {{DiscountValue}} voucher (Code: {{VoucherCode}}) just for you! Valid until {{ExpiryDate}}. Tap to claim and treat your little ones today!'),
    ('VOUCHER_EXPIRING', 'ADMIN', N'⏰ Last Chance: Your {{VoucherCode}} Voucher Expires Soon!',
        N'Don''t miss out on your {{DiscountValue}} discount! Voucher {{VoucherCode}} will expire on {{ExpiryDate}}. Use it now before it''s gone!'),
    ('BLOG_NEW', 'ADMIN', N'📖 New Blog Article: {{BlogTitle}}',
        N'Check out our latest blog post "{{BlogTitle}}"! Discover helpful guides, tips, and fun toy reviews for your family.');

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
-- Campaigns (UI-realistic: reference + target + action route + lifecycle)
-- ============================================================
IF NOT EXISTS (SELECT 1
FROM [Notification].[Campaigns])
BEGIN
    DECLARE @UtcNowC DATETIME2(0) = (SELECT UtcNow
    FROM #SeedTime);
    DECLARE @admCamp INT = (SELECT TOP 1
        AccountID
    FROM Accounts
    WHERE Email = 'admintoystore@gmail.com');
    DECLARE @stfCamp INT = (SELECT TOP 1
        AccountID
    FROM Accounts
    WHERE Email = 'nhung.st@toyhouse.vn');

    -- Target convention (matches Admin Campaign Wizard):
    --   ADMIN marketing → TargetType ALL (broadcast to all active customers)
    --   SYSTEM          → automated notifications; INDIVIDUAL per account when event-driven

    -- Reference entities (same lookups as Campaign Wizard step 1)
    DECLARE @pSummer INT = (SELECT TOP 1
        PromotionID
    FROM Promotions
    WHERE PromotionName = N'Summer Toy Festival 2026');
    DECLARE @pMega   INT = (SELECT TOP 1
        PromotionID
    FROM Promotions
    WHERE PromotionName = N'Mega Flash Sale');
    DECLARE @pBTS    INT = (SELECT TOP 1
        PromotionID
    FROM Promotions
    WHERE PromotionName = N'Back To School 2026');
    DECLARE @pBF     INT = (SELECT TOP 1
        PromotionID
    FROM Promotions
    WHERE PromotionName = N'Black Friday Preview');
    DECLARE @vWelcome INT = (SELECT TOP 1
        VoucherID
    FROM Vouchers
    WHERE VoucherCode = 'WELCOME15');
    DECLARE @vFree    INT = (SELECT TOP 1
        VoucherID
    FROM Vouchers
    WHERE VoucherCode = 'FREESHIP50');
    DECLARE @vVIP     INT = (SELECT TOP 1
        VoucherID
    FROM Vouchers
    WHERE VoucherCode = 'VIP300K');
    DECLARE @vExpiring INT = (SELECT TOP 1
        VoucherID
    FROM Vouchers
    WHERE VoucherCode = 'EXPIRING48H');
    DECLARE @vUpcoming INT = (SELECT TOP 1
        VoucherID
    FROM Vouchers
    WHERE VoucherCode = 'UPCOMING20');
    DECLARE @blogHalloween INT = (SELECT TOP 1
        BlogPostID
    FROM BlogPosts
    WHERE BlogTitle = N'Halloween Toy Preview 2026');

    DECLARE @pSummerStart DATETIME2(0) = (SELECT StartDate
    FROM Promotions
    WHERE PromotionID = @pSummer);
    DECLARE @pSummerEnd   DATETIME2(0) = (SELECT EndDate
    FROM Promotions
    WHERE PromotionID = @pSummer);
    DECLARE @vFreeStart   DATETIME2(0) = (SELECT StartDate
    FROM Vouchers
    WHERE VoucherID = @vFree);
    DECLARE @vFreeEnd     DATETIME2(0) = (SELECT EndDate
    FROM Vouchers
    WHERE VoucherID = @vFree);
    DECLARE @blogHalloweenAt DATETIME2(0) = (SELECT BlogAt
    FROM BlogPosts
    WHERE BlogPostID = @blogHalloween);

    DECLARE @schedFreeShip DATETIME2(0) = DATEADD(DAY, 2, @UtcNowC);
    DECLARE @schedHalloween DATETIME2(0) = DATEADD(DAY, 7, @UtcNowC);
    DECLARE @sentSummerAt DATETIME2(0) = DATEADD(DAY, -14, @UtcNowC);
    DECLARE @sentWelcomeAt DATETIME2(0) = DATEADD(DAY, -20, @UtcNowC);
    DECLARE @sentFlashAt DATETIME2(0) = DATEADD(HOUR, -1, @UtcNowC);

    -- 1) Sent – Summer DISCOUNT promo blast (staff submitted → admin approved → scheduled → sent)
    INSERT INTO [Notification].[Campaigns]
        (CampaignName, TemplateCode, SourceType, TargetType, Status,
        ReferenceType, ReferenceID, ActionType, ActionTarget,
        SubmittedByAccountID, SubmittedAt, ReviewedByAccountID, ReviewedAt,
        ValidFrom, ValidTo, ScheduledAt,
        CreatedByAccountID, IsDeleted, CreatedAt)
    VALUES
        (N'Summer Toy Festival 2026 – Customer Blast', 'FLASH_SALE_STARTED', 'ADMIN', 'ALL', 'Sent',
            'SALE', @pSummer, 'ROUTE', N'/?flashSale=' + CAST(@pSummer AS NVARCHAR(10)),
            @stfCamp, DATEADD(DAY, -16, @UtcNowC), @admCamp, DATEADD(DAY, -15, @UtcNowC),
            @pSummerStart, @pSummerEnd, @sentSummerAt,
            @stfCamp, 0, DATEADD(DAY, -17, @UtcNowC));

    -- 2) Sent – WELCOME15 broadcast to all customers (default Wizard target = ALL)
    INSERT INTO [Notification].[Campaigns]
        (CampaignName, TemplateCode, SourceType, TargetType, Status,
        ReferenceType, ReferenceID, ActionType, ActionTarget,
        SubmittedByAccountID, SubmittedAt, ReviewedByAccountID, ReviewedAt,
        ValidFrom, ValidTo, ScheduledAt,
        CreatedByAccountID, IsDeleted, CreatedAt)
    VALUES
        (N'Welcome Gift – WELCOME15 for All Customers', 'VOUCHER_NEW', 'ADMIN', 'ALL', 'Sent',
            'VOUCHER', @vWelcome, 'ROUTE', N'/profile/vouchers?code=WELCOME15',
            @stfCamp, DATEADD(DAY, -22, @UtcNowC), @admCamp, DATEADD(DAY, -21, @UtcNowC),
            (SELECT StartDate
            FROM Vouchers
            WHERE VoucherID = @vWelcome),
            (SELECT EndDate
            FROM Vouchers
            WHERE VoucherID = @vWelcome),
            @sentWelcomeAt,
            @stfCamp, 0, DATEADD(DAY, -23, @UtcNowC));

    -- 3) Sent – SYSTEM individual review reminders (per-account targets, no marketing reference)
    INSERT INTO [Notification].[Campaigns]
        (CampaignName, TemplateCode, SourceType, TargetType, Status, ScheduledAt, CreatedAt)
    VALUES
        (N'Post-Delivery Review Reminder', 'ORDER_DELIVERED', 'SYSTEM', 'INDIVIDUAL', 'Sent',
            DATEADD(DAY, -30, @UtcNowC), DATEADD(DAY, -30, @UtcNowC));

    -- 4) Sent – Mega Flash with reference + custom overrides (Wizard step 2–3)
    INSERT INTO [Notification].[Campaigns]
        (CampaignName, TemplateCode, SourceType, TargetType, Status,
        ReferenceType, ReferenceID, ActionType, ActionTarget,
        TitleOverride, MessageOverride,
        SubmittedByAccountID, SubmittedAt, ReviewedByAccountID, ReviewedAt,
        ScheduledAt, CreatedByAccountID, IsDeleted, CreatedAt)
    VALUES
        (N'Mega Flash Sale – Live Now', 'FLASH_SALE_STARTED', 'ADMIN', 'ALL', 'Sent',
            'SALE', @pMega, 'ROUTE', N'/?flashSale=' + CAST(@pMega AS NVARCHAR(10)),
            N'Mega Flash Sale is Live!', N'Click here to check out amazing toy deals right now!',
            @stfCamp, DATEADD(HOUR, -3, @UtcNowC), @admCamp, DATEADD(HOUR, -2, @UtcNowC),
            @sentFlashAt, @admCamp, 0, DATEADD(HOUR, -4, @UtcNowC));

    -- 5) Approved – BTS promo waiting for staff to pick schedule window
    INSERT INTO [Notification].[Campaigns]
        (CampaignName, TemplateCode, SourceType, TargetType, Status,
        ReferenceType, ReferenceID, ActionType, ActionTarget,
        SubmittedByAccountID, SubmittedAt, ReviewedByAccountID, ReviewedAt, ApprovedExpireAt,
        CreatedByAccountID, IsDeleted, CreatedAt)
    VALUES
        (N'Back To School 2026 – Early Notice', 'FLASH_SALE_STARTED', 'ADMIN', 'ALL', 'Approved',
            'SALE', @pBTS, 'ROUTE', N'/?flashSale=' + CAST(@pBTS AS NVARCHAR(10)),
            @stfCamp, DATEADD(DAY, -5, @UtcNowC), @admCamp, DATEADD(DAY, -2, @UtcNowC), DATEADD(DAY, 5, @UtcNowC),
            @stfCamp, 0, DATEADD(DAY, -6, @UtcNowC));

    -- 6) PendingApproval – Black Friday promo (ALL customers)
    INSERT INTO [Notification].[Campaigns]
        (CampaignName, TemplateCode, SourceType, TargetType, Status,
        ReferenceType, ReferenceID, ActionType, ActionTarget,
        SubmittedByAccountID, SubmittedAt, CreatedByAccountID, IsDeleted, CreatedAt)
    VALUES
        (N'Black Friday Preview 2026 – Storewide Push', 'FLASH_SALE_STARTED', 'ADMIN', 'ALL', 'PendingApproval',
            'SALE', @pBF, 'ROUTE', N'/?flashSale=' + CAST(@pBF AS NVARCHAR(10)),
            @stfCamp, DATEADD(DAY, -1, @UtcNowC), @stfCamp, 0, DATEADD(DAY, -2, @UtcNowC));

    -- 7) Rejected – VIP300K over budget (admin ReviewNote required)
    INSERT INTO [Notification].[Campaigns]
        (CampaignName, TemplateCode, SourceType, TargetType, Status,
        ReferenceType, ReferenceID, ActionType, ActionTarget,
        SubmittedByAccountID, SubmittedAt, ReviewedByAccountID, ReviewedAt, ReviewNote,
        CreatedByAccountID, IsDeleted, CreatedAt)
    VALUES
        (N'VIP300K High-Value Voucher Blast', 'VOUCHER_NEW', 'ADMIN', 'ALL', 'Rejected',
            'VOUCHER', @vVIP, 'ROUTE', N'/profile/vouchers?code=VIP300K',
            @stfCamp, DATEADD(DAY, -4, @UtcNowC), @admCamp, DATEADD(DAY, -3, @UtcNowC),
            N'Rejected: voucher budget exceeds Q3 marketing cap. Please reduce discount or narrow audience.',
            @stfCamp, 0, DATEADD(DAY, -5, @UtcNowC));

    -- 8) Scheduled – FREESHIP50 (ValidFrom/To aligned with voucher entity)
    INSERT INTO [Notification].[Campaigns]
        (CampaignName, TemplateCode, SourceType, TargetType, Status,
        ReferenceType, ReferenceID, ActionType, ActionTarget,
        ValidFrom, ValidTo, ScheduledAt,
        SubmittedByAccountID, SubmittedAt, ReviewedByAccountID, ReviewedAt,
        CreatedByAccountID, IsDeleted, CreatedAt)
    VALUES
        (N'FREESHIP50 Free Shipping Reminder', 'VOUCHER_NEW', 'ADMIN', 'ALL', 'Scheduled',
            'VOUCHER', @vFree, 'ROUTE', N'/profile/vouchers?code=FREESHIP50',
            @vFreeStart, @vFreeEnd, @schedFreeShip,
            @stfCamp, DATEADD(DAY, -5, @UtcNowC), @admCamp, DATEADD(DAY, -4, @UtcNowC),
            @stfCamp, 0, DATEADD(DAY, -6, @UtcNowC));

    -- 9) Scheduled – Halloween blog (custom title/message like Wizard without template)
    INSERT INTO [Notification].[Campaigns]
        (CampaignName, TemplateCode, SourceType, TargetType, Status,
        ReferenceType, ReferenceID, ActionType, ActionTarget, ImageUrl,
        TitleOverride, MessageOverride,
        ValidFrom, ValidTo, ScheduledAt,
        SubmittedByAccountID, SubmittedAt, ReviewedByAccountID, ReviewedAt,
        CreatedByAccountID, IsDeleted, CreatedAt)
    VALUES
        (N'Halloween Toy Preview – Save the Date', NULL, 'ADMIN', 'ALL', 'Scheduled',
            'BLOG', @blogHalloween, 'ROUTE', N'/blog/' + CAST(@blogHalloween AS NVARCHAR(10)),
            (SELECT BlogThumbnail
            FROM BlogPosts
            WHERE BlogPostID = @blogHalloween),
            N'📖 Fresh Blog Post: Halloween Toy Preview 2026!',
            N'Get an exclusive early look at spooky figures, costume sets, and fun activities before the October drop. Tap to read now!',
            @UtcNowC, @blogHalloweenAt, @schedHalloween,
            @stfCamp, DATEADD(DAY, -3, @UtcNowC), @admCamp, DATEADD(DAY, -2, @UtcNowC),
            @stfCamp, 0, DATEADD(DAY, -4, @UtcNowC));

    -- 10) Cancelled – staff cancelled after scheduling UPCOMING20 teaser
    INSERT INTO [Notification].[Campaigns]
        (CampaignName, TemplateCode, SourceType, TargetType, Status,
        ReferenceType, ReferenceID, ActionType, ActionTarget,
        ValidFrom, ValidTo, ScheduledAt,
        SubmittedByAccountID, SubmittedAt, ReviewedByAccountID, ReviewedAt,
        CreatedByAccountID, IsDeleted, CreatedAt)
    VALUES
        (N'UPCOMING20 Teaser – Cancelled by Staff', 'VOUCHER_NEW', 'ADMIN', 'ALL', 'Cancelled',
            'VOUCHER', @vUpcoming, 'ROUTE', N'/profile/vouchers?code=UPCOMING20',
            (SELECT StartDate
            FROM Vouchers
            WHERE VoucherID = @vUpcoming),
            (SELECT EndDate
            FROM Vouchers
            WHERE VoucherID = @vUpcoming),
            DATEADD(DAY, 10, @UtcNowC),
            @stfCamp, DATEADD(DAY, -8, @UtcNowC), @admCamp, DATEADD(DAY, -7, @UtcNowC),
            @stfCamp, 0, DATEADD(DAY, -9, @UtcNowC));

    -- 11) Failed – dispatch exhausted retries on EXPIRING48H urgency push
    INSERT INTO [Notification].[Campaigns]
        (CampaignName, TemplateCode, SourceType, TargetType, Status,
        ReferenceType, ReferenceID, ActionType, ActionTarget,
        ValidFrom, ValidTo, ScheduledAt,
        SubmittedByAccountID, SubmittedAt, ReviewedByAccountID, ReviewedAt,
        CreatedByAccountID, IsDeleted, CreatedAt)
    VALUES
        (N'EXPIRING48H Urgency Push – Send Failed', 'VOUCHER_EXPIRING', 'ADMIN', 'ALL', 'Failed',
            'VOUCHER', @vExpiring, 'ROUTE', N'/profile/vouchers?code=EXPIRING48H',
            DATEADD(HOUR, -4, @UtcNowC), DATEADD(DAY, 2, @UtcNowC),
            DATEADD(HOUR, -3, @UtcNowC),
            @stfCamp, DATEADD(DAY, -2, @UtcNowC), @admCamp, DATEADD(DAY, -1, @UtcNowC),
            @stfCamp, 0, DATEADD(DAY, -3, @UtcNowC));

    -- 12) Draft – staff saved wizard (ALL customers + voucher reference)
    INSERT INTO [Notification].[Campaigns]
        (CampaignName, TemplateCode, SourceType, TargetType, Status,
        ReferenceType, ReferenceID, ActionType, ActionTarget,
        CreatedByAccountID, IsDeleted, CreatedAt)
    VALUES
        (N'Draft – EXPIRING48H Last-Chance Reminder', 'VOUCHER_EXPIRING', 'ADMIN', 'ALL', 'Draft',
            'VOUCHER', @vExpiring, 'ROUTE', N'/profile/vouchers?code=EXPIRING48H',
            @stfCamp, 0, @UtcNowC);

    -- CampaignTargets: only INDIVIDUAL campaigns (ALL must have empty targets per schema)
    INSERT INTO [Notification].[CampaignTargets]
        (CampaignID, TargetType, TargetValue)
    SELECT c.CampaignID, 'ACCOUNT_ID', CAST(a.AccountID AS VARCHAR(20))
    FROM [Notification].[Campaigns] c
    CROSS APPLY (
        SELECT DISTINCT o.AccountID
        FROM Orders o
        WHERE o.DeliveredAt IS NOT NULL AND o.StatusID IN (6, 7)
    ) a
    WHERE c.CampaignName = N'Post-Delivery Review Reminder';

    -- CampaignSchedules
    INSERT INTO [Notification].[CampaignSchedules]
        (CampaignID, ScheduledBy, ScheduledAt, ExecutionStatus, AttemptCount, LastError, ExecutedAt, CreatedAt)
    SELECT c.CampaignID, @stfCamp, c.ScheduledAt, 'Waiting', 0, NULL, NULL, @UtcNowC
    FROM [Notification].[Campaigns] c
    WHERE c.Status = 'Scheduled' AND c.ScheduledAt IS NOT NULL;

    INSERT INTO [Notification].[CampaignSchedules]
        (CampaignID, ScheduledBy, ScheduledAt, ExecutionStatus, AttemptCount, MaxAttemptCount, LastError, ExecutedAt, CreatedAt)
    SELECT c.CampaignID, @stfCamp, c.ScheduledAt, 'Cancelled', 0, 3, NULL, NULL, @UtcNowC
    FROM [Notification].[Campaigns] c
    WHERE c.CampaignName = N'UPCOMING20 Teaser – Cancelled by Staff';

    INSERT INTO [Notification].[CampaignSchedules]
        (CampaignID, ScheduledBy, ScheduledAt, ExecutionStatus, AttemptCount, MaxAttemptCount, LastError, ExecutedAt, CreatedAt)
    SELECT c.CampaignID, @stfCamp, c.ScheduledAt, 'Failed', 3, 3,
        N'Notification fan-out failed: downstream email gateway timeout after 3 attempts.',
        c.ScheduledAt, @UtcNowC
    FROM [Notification].[Campaigns] c
    WHERE c.CampaignName = N'EXPIRING48H Urgency Push – Send Failed';

    -- Reference snapshots (taken when staff schedules – entity state frozen)
    INSERT INTO [Notification].[CampaignReferenceSnapshots]
        (CampaignID, ReferenceType, ReferenceID, EntityStatus, EntityStartDate, EntityEndDate, SnapshotAt)
    SELECT c.CampaignID, c.ReferenceType, c.ReferenceID, v.Status, v.StartDate, v.EndDate, @UtcNowC
    FROM [Notification].[Campaigns] c
        JOIN [dbo].[Vouchers] v ON c.ReferenceType = 'VOUCHER' AND v.VoucherID = c.ReferenceID
    WHERE c.Status IN ('Scheduled', 'Sent', 'Failed', 'Cancelled') AND c.ReferenceType = 'VOUCHER';

    INSERT INTO [Notification].[CampaignReferenceSnapshots]
        (CampaignID, ReferenceType, ReferenceID, EntityStatus, EntityStartDate, EntityEndDate, SnapshotAt)
    SELECT c.CampaignID, c.ReferenceType, c.ReferenceID, p.Status, p.StartDate, p.EndDate, @UtcNowC
    FROM [Notification].[Campaigns] c
        JOIN [dbo].[Promotions] p ON c.ReferenceType = 'SALE' AND p.PromotionID = c.ReferenceID
    WHERE c.ReferenceType = 'SALE'
        AND c.Status IN ('Scheduled', 'Sent', 'PendingApproval', 'Approved');

    INSERT INTO [Notification].[CampaignReferenceSnapshots]
        (CampaignID, ReferenceType, ReferenceID, EntityStatus, EntityStartDate, EntityEndDate, SnapshotAt)
    SELECT c.CampaignID, c.ReferenceType, c.ReferenceID, bp.Status, bp.CreatedAt, bp.BlogAt, @UtcNowC
    FROM [Notification].[Campaigns] c
        JOIN [dbo].[BlogPosts] bp ON c.ReferenceType = 'BLOG' AND bp.BlogPostID = c.ReferenceID
    WHERE c.Status = 'Scheduled' AND c.ReferenceType = 'BLOG';
END
GO

-- ============================================================
-- Campaign Stats
-- ============================================================
IF NOT EXISTS (SELECT 1
FROM [Notification].[CampaignStats])
    INSERT INTO [Notification].[CampaignStats]
    (CampaignID, TotalSent, TotalRead, TotalClicked, ComputedAt)
SELECT
    c.CampaignID,
    CASE WHEN c.Status = 'Sent' THEN 5 ELSE 0 END,
    CASE WHEN c.Status = 'Sent' THEN 3 ELSE 0 END,
    CASE WHEN c.CampaignName = N'Mega Flash Sale – Live Now' THEN 2
         WHEN c.Status = 'Sent' THEN 1 ELSE 0 END,
    (SELECT UtcNow
    FROM #SeedTime)
FROM [Notification].[Campaigns] c;
GO

-- ============================================================
-- Deliveries (transactional notifications: CampaignID NULL)
-- ============================================================
DECLARE @cmpRv INT = (SELECT TOP 1
    CampaignID
FROM [Notification].[Campaigns]
WHERE CampaignName = N'Post-Delivery Review Reminder');

-- Order placed – system notification, not linked to a marketing campaign
INSERT INTO [Notification].[Deliveries]
    (AccountID, CampaignID, TemplateCode, RecipientType, NotificationType,
    Title, Message, Payload, Status, IdempotencyKey, CreatedAt)
SELECT
    o.AccountID,
    NULL,
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

-- Mega Flash Sale live campaign deliveries
DECLARE @cmpFlash INT = (SELECT TOP 1
    CampaignID
FROM [Notification].[Campaigns]
WHERE CampaignName = N'Mega Flash Sale – Live Now');
DECLARE @pMegaD INT = (SELECT TOP 1
    PromotionID
FROM Promotions
WHERE PromotionName = N'Mega Flash Sale');

IF @cmpFlash IS NOT NULL AND NOT EXISTS (SELECT 1
    FROM [Notification].[Deliveries]
    WHERE CampaignID = @cmpFlash)
    INSERT INTO [Notification].[Deliveries]
    (AccountID, CampaignID, TemplateCode, RecipientType, Channel, NotificationType,
    ActionType, ActionTarget, Title, Message, Payload, Status, IsDeleted, CreatedAt)
SELECT
    AccountID, @cmpFlash, 'FLASH_SALE_STARTED', 'CUSTOMER', 'WEB_BELL', 'PROMOTION',
    'ROUTE', N'/?flashSale=' + CAST(@pMegaD AS NVARCHAR(10)),
    N'Mega Flash Sale is Live!', N'Click here to check out amazing toy deals right now!',
    N'{"promotionId":' + CAST(@pMegaD AS VARCHAR) + N'}',
    'Unread', 0, (SELECT UtcNow
    FROM #SeedTime)
FROM Accounts
WHERE RoleID = 1 AND IsActive = 1 AND IsDeleted = 0;
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
    DECLARE @UtcNow37 DATETIME2(0) = (SELECT UtcNow
    FROM #SeedTime);
    DECLARE @admAL INT = (SELECT TOP 1
        AccountID
    FROM Accounts
    WHERE Email = 'admintoystore@gmail.com');
    DECLARE @stfAL INT = (SELECT TOP 1
        AccountID
    FROM Accounts
    WHERE Email = 'nhung.st@toyhouse.vn');

    DECLARE @cmpSummer INT = (SELECT TOP 1
        CampaignID
    FROM [Notification].[Campaigns]
    WHERE CampaignName = N'Summer Toy Festival 2026 – Customer Blast');
    DECLARE @cmpV INT = (SELECT TOP 1
        CampaignID
    FROM [Notification].[Campaigns]
    WHERE CampaignName = N'Welcome Gift – WELCOME15 for All Customers');
    DECLARE @cmpR INT = (SELECT TOP 1
        CampaignID
    FROM [Notification].[Campaigns]
    WHERE CampaignName = N'Post-Delivery Review Reminder');
    DECLARE @cmpBTS INT = (SELECT TOP 1
        CampaignID
    FROM [Notification].[Campaigns]
    WHERE CampaignName = N'Back To School 2026 – Early Notice');
    DECLARE @cmpBF INT = (SELECT TOP 1
        CampaignID
    FROM [Notification].[Campaigns]
    WHERE CampaignName = N'Black Friday Preview 2026 – Storewide Push');
    DECLARE @cmpSched INT = (SELECT TOP 1
        CampaignID
    FROM [Notification].[Campaigns]
    WHERE CampaignName = N'FREESHIP50 Free Shipping Reminder');
    DECLARE @cmpRej INT = (SELECT TOP 1
        CampaignID
    FROM [Notification].[Campaigns]
    WHERE CampaignName = N'VIP300K High-Value Voucher Blast');
    DECLARE @cmpCancel INT = (SELECT TOP 1
        CampaignID
    FROM [Notification].[Campaigns]
    WHERE CampaignName = N'UPCOMING20 Teaser – Cancelled by Staff');

    INSERT INTO [Notification].[CampaignApprovalLogs]
        (CampaignID, Action, ActorID, Note, CreatedAt)
    VALUES
        (@cmpSummer, 'Submitted', @stfAL, N'Notify all customers about the active Summer Toy Festival promotion', DATEADD(DAY, -16, @UtcNow37)),
        (@cmpSummer, 'Approved', @admAL, N'Approved – linked to Summer Toy Festival 2026 promotion', DATEADD(DAY, -15, @UtcNow37)),
        (@cmpSummer, 'Scheduled', @stfAL, N'Scheduled broadcast for campaign launch window', DATEADD(DAY, -14, @UtcNow37)),
        (@cmpV, 'Submitted', @stfAL, N'WELCOME15 voucher broadcast to all customers', DATEADD(DAY, -22, @UtcNow37)),
        (@cmpV, 'Approved', @admAL, N'Approved – storewide welcome voucher campaign', DATEADD(DAY, -21, @UtcNow37)),
        (@cmpR, 'Approved', @admAL, N'System campaign – auto-approved for delivered-order recipients', DATEADD(DAY, -30, @UtcNow37)),
        (@cmpBTS, 'Submitted', @stfAL, N'Back To School 2026 early notice linked to scheduled promotion', DATEADD(DAY, -5, @UtcNow37)),
        (@cmpBTS, 'Approved', @admAL, N'Approved – staff must schedule within 5-day approval window', DATEADD(DAY, -2, @UtcNow37)),
        (@cmpBF, 'Submitted', @stfAL, N'Black Friday Preview storewide push to all customers', DATEADD(DAY, -1, @UtcNow37)),
        (@cmpRej, 'Submitted', @stfAL, N'VIP300K voucher blast to all customers', DATEADD(DAY, -4, @UtcNow37)),
        (@cmpRej, 'Rejected', @admAL, N'Rejected: voucher budget exceeds Q3 marketing cap', DATEADD(DAY, -3, @UtcNow37)),
        (@cmpSched, 'Submitted', @stfAL, N'FREESHIP50 reminder linked to active voucher', DATEADD(DAY, -5, @UtcNow37)),
        (@cmpSched, 'Approved', @admAL, N'Approved – schedule within voucher validity window', DATEADD(DAY, -4, @UtcNow37)),
        (@cmpSched, 'Scheduled', @stfAL, N'Scheduled FREESHIP50 push for all customers', DATEADD(DAY, -1, @UtcNow37)),
        (@cmpCancel, 'Submitted', @stfAL, N'UPCOMING20 teaser campaign draft', DATEADD(DAY, -8, @UtcNow37)),
        (@cmpCancel, 'Approved', @admAL, N'Approved for scheduling', DATEADD(DAY, -7, @UtcNow37)),
        (@cmpCancel, 'Scheduled', @stfAL, N'Scheduled then cancelled – duplicate with FREESHIP50 week', DATEADD(DAY, -6, @UtcNow37)),
        (@cmpCancel, 'Cancelled', @stfAL, N'Cancelled – overlaps with FREESHIP50 reminder week', DATEADD(DAY, -5, @UtcNow37));

    INSERT INTO [Notification].[CampaignScheduleLogs]
        (CampaignID, ActorID, Action, PreviousScheduledAt, NewScheduledAt, CreatedAt)
    VALUES
        (@cmpSched, @stfAL, 'Scheduled', NULL, DATEADD(DAY, 2, @UtcNow37), DATEADD(DAY, -1, @UtcNow37)),
        (@cmpCancel, @stfAL, 'Scheduled', NULL, DATEADD(DAY, 10, @UtcNow37), DATEADD(DAY, -6, @UtcNow37));
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
IF NOT EXISTS (SELECT 1
FROM [dbo].[Wallets])
BEGIN
    -- Insert wallets for customers (RoleID = 1)
    INSERT INTO [dbo].[Wallets]
        (AccountID, Currency, Balance, Status, CreatedAt)
    SELECT AccountID, 'VND', 5000000.00, 'Active', CreatedAt
    FROM Accounts
    WHERE RoleID = 1;

    -- Insert wallet pins for all wallets
    INSERT INTO [dbo].[WalletPins]
        (WalletID, PinHash, IsActive, FailedAttempts, TotalFailedAttempts, LastChangedAt, CreatedAt)
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
    SELECT w.WalletID, a.AccountID, v.ActionType, v.IsSuccess,
        DATEADD(DAY, v.DayOffset, (SELECT UtcNow
        FROM #SeedTime))
    FROM Accounts a
        JOIN Wallets w ON w.AccountID = a.AccountID
        JOIN (VALUES
            ('lananh.pham@gmail.com', 'PAYMENT', 1, -60),
            ('lananh.pham@gmail.com', 'PAYMENT', 1, -20),
            ('hung.nguyen88@gmail.com', 'PAYMENT', 0, -15),
            ('hung.nguyen88@gmail.com', 'PAYMENT', 1, -15),
            ('thuha.hoang@gmail.com', 'PAYMENT', 1, -10),
            ('thuha.hoang@gmail.com', 'VIEW_BALANCE', 1, -5),
            ('mtuando@outlook.com', 'VIEW_BALANCE', 1, -3)
    ) AS v(Email, ActionType, IsSuccess, DayOffset) ON a.Email = v.Email
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
    ('VoucherExpiryReminderJob', '0 9 * * *', 1, 'Completed'),
    ('PromotionStatusJob', '0 0 * * *', 1, 'Completed'),
    ('BirthdayNotificationJob', '0 7 * * *', 1, 'Pending'),
    ('LowStockScanJob', '0 9 * * *', 1, 'Pending'),
    ('AutoCompleteOrderJob', '0 0 * * *', 1, 'Completed'),
    ('PaymentOverdueJob', '*/15 * * * *', 1, 'Completed'),
    ('CustomerDeliveryAbuseScanJob', '0 1 * * *', 1, 'Completed'),
    ('CampaignSchedulerJob', '*/5 * * * *', 1, 'Completed'),
    ('ProductReviewModerationPollJob', '*/5 * * * *', 1, 'Completed'),
    ('BlogCommentModerationPollJob', '*/5 * * * *', 1, 'Completed'),
    ('BlogCommentManualReviewTimeoutJob', '0 * * * *', 1, 'Completed'),
    ('BlogCommentPermissionUnlockJob', '0 0 * * *', 1, 'Completed'),
    ('BackInStockJob', '0 9 * * *', 1, 'Completed');

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

/* ══════════════════════════════════════════════════════════════
   SECTION 45 – AI BLOG PROMPT TEMPLATES [NEW]
   Seeds prompt templates for the AI Blog Generation module.
══════════════════════════════════════════════════════════════ */
PRINT N'[45-NEW] AIPromptTemplates...';

IF NOT EXISTS (SELECT 1 FROM [dbo].[AIPromptTemplates])
BEGIN
    DECLARE @UtcNowTpl DATETIME2(0) = (SELECT UtcNow FROM #SeedTime);

    INSERT INTO [dbo].[AIPromptTemplates]
        (TemplateName, PromptStructure, DefaultTone, DefaultCategoryID, IsActive, CreatedAt)
    VALUES
        (N'SEO Product Review Builder', N'Write a comprehensive, SEO-optimized product review for {{ProductName}} in {{CategoryName}}. Highlight key features, safety materials, age appropriateness, and pros & cons.', 'Friendly & Informative', 3, 1, DATEADD(MONTH, -3, @UtcNowTpl)),
        (N'Parenting Guide Writer', N'Write an engaging parenting advice guide focusing on {{Topic}}. Provide practical tips for parents of children aged {{AgeRange}} with an empathetic tone.', 'Empathetic & Expert', 2, 1, DATEADD(MONTH, -3, @UtcNowTpl)),
        (N'Toy Safety Inspection Article', N'Write an educational article explaining safety standards (EN71, ASTM, BPA-Free) for {{ToyType}}. Focus on choking hazard prevention and non-toxic materials.', 'Informative & Formal', 5, 1, DATEADD(MONTH, -3, @UtcNowTpl));
END
GO

PRINT N'================================================================';
PRINT N'  DataSeed v5.4 complete – UTC timeline, English content.';
PRINT N'================================================================';
GO