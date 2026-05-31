/* =================================================================
   DataSeed FULL v5.3 – SEP490_ToyStore
   Cập nhật theo Schema v3.2
================================================================= */

USE [SEP490_ToyStore];
GO
SET NOCOUNT ON;
GO

PRINT N'================================================================';
PRINT N'  DataSeed v5.3 (Schema v3.2 Compatible) – Bắt đầu...';
PRINT N'================================================================';

/* ══════════════════════════════════════════════════════════════
   SECTION 1 – ROLES
══════════════════════════════════════════════════════════════ */
PRINT N'[1] Roles...';
SET IDENTITY_INSERT [dbo].[Roles] ON;
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE RoleID = 1)
    INSERT INTO [dbo].[Roles] (RoleID, RoleName, Description, CreatedAt) VALUES
    (1, 'Customer',    N'Khách hàng',         '2024-01-05 08:00:00'),
    (2, 'Admin',       N'Quản trị viên',      '2024-01-05 08:00:00'),
    (3, 'Staff',       N'Nhân viên bán hàng', '2024-01-05 08:00:00'),
    (4, 'Merchandise', N'Nhân viên kho',      '2024-01-05 08:00:00');
SET IDENTITY_INSERT [dbo].[Roles] OFF;
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 2 – SEXES
══════════════════════════════════════════════════════════════ */
PRINT N'[2] Sexes...';
SET IDENTITY_INSERT [dbo].[Sexes] ON;
IF NOT EXISTS (SELECT 1 FROM [dbo].[Sexes] WHERE SexID = 1)
    INSERT INTO [dbo].[Sexes] (SexID, SexName, CreatedAt) VALUES
    (1, N'Nam',  '2024-01-05 08:00:00'),
    (2, N'Nữ',   '2024-01-05 08:00:00'),
    (3, N'Khác', '2024-01-05 08:00:00');
SET IDENTITY_INSERT [dbo].[Sexes] OFF;
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 3 – ACCOUNTS
══════════════════════════════════════════════════════════════ */
PRINT N'[3] Accounts – Staff & Admin...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[Accounts] WHERE Email = 'admin@toyhouse.vn')
    INSERT INTO [dbo].[Accounts]
        (RoleID, SexID, EmployeeCode, AccountName, PhoneNumber, Email, DOB, PasswordHash, IsActive, CreatedAt)
    VALUES
    (2,1,'AD001',N'Nguyễn Minh Khôi',    '0901000001','admin@toyhouse.vn',    '1990-03-15','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2024-01-05 08:00:00'),
    (3,2,'ST001',N'Trần Thị Hồng Nhung', '0901000002','nhung.st@toyhouse.vn', '1995-07-22','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2024-01-05 08:00:00'),
    (3,1,'ST002',N'Phạm Văn Dũng',       '0901000003','dung.st@toyhouse.vn',  '1993-11-08','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2024-01-10 08:00:00'),
    (3,2,'ST003',N'Lê Thị Thu Thảo',     '0901000004','thao.st@toyhouse.vn',  '1997-04-30','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2024-01-10 08:00:00'),
    (4,1,'MC001',N'Lê Quốc Bảo',         '0901000005','bao.kho@toyhouse.vn',  '1992-09-18','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2024-01-05 08:00:00'),
    (4,2,'MC002',N'Nguyễn Thị Lan',      '0901000006','lan.kho@toyhouse.vn',  '1994-06-25','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2024-01-10 08:00:00');
GO

PRINT N'[3] Accounts – Customers (5 người)...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[Accounts] WHERE Email = 'lananh.pham@gmail.com')
    INSERT INTO [dbo].[Accounts]
        (RoleID, SexID, AccountName, PhoneNumber, Email, DOB, PasswordHash, IsActive, CreatedAt)
    VALUES
    (1,2,N'Phạm Thị Lan Anh',  '0912001001','lananh.pham@gmail.com',   '1988-03-12','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2024-03-12 10:15:00'),
    (1,1,N'Nguyễn Văn Hùng',   '0912001002','hung.nguyen88@gmail.com',  '1988-06-20','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2024-04-02 14:30:00'),
    (1,2,N'Vũ Thị Bảo Châu',   '0912001003','bauchau.vu@gmail.com',     '1992-01-15','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2024-04-20 09:00:00'),
    (1,1,N'Đỗ Minh Tuấn',      '0912001004','mtuando@outlook.com',      '1985-08-10','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2024-05-08 16:45:00'),
    (1,2,N'Hoàng Thị Thu Hà',  '0912001005','thuha.hoang@gmail.com',    '1990-12-05','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2024-06-01 11:20:00');
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 4 – USER PREFERENCES
══════════════════════════════════════════════════════════════ */
PRINT N'[4] Notification.UserPreferences...';
INSERT INTO [Notification].[UserPreferences]
    (AccountID, EmailOptIn, WebPushOptIn, OrderUpdates, Promotions, StockAlerts, BlogAlerts)
SELECT a.AccountID, 1, 0, 1, 1, 1, 1
FROM [dbo].[Accounts] a
WHERE NOT EXISTS (
    SELECT 1 FROM [Notification].[UserPreferences] up WHERE up.AccountID = a.AccountID);
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 5 – LOOKUP TABLES
══════════════════════════════════════════════════════════════ */
PRINT N'[5] Lookup tables...';

SET IDENTITY_INSERT [dbo].[SuperCategories] ON;
IF NOT EXISTS (SELECT 1 FROM [dbo].[SuperCategories] WHERE SuperCategoryID = 1)
    INSERT INTO [dbo].[SuperCategories] (SuperCategoryID, SuperCategoryName, CreatedAt) VALUES
    (1,N'Đồ chơi lắp ráp',    '2024-01-05 08:00:00'),
    (2,N'Đồ chơi giáo dục',   '2024-01-05 08:00:00'),
    (3,N'Vận động ngoài trời', '2024-01-05 08:00:00'),
    (4,N'Búp bê & Thú bông',   '2024-01-05 08:00:00'),
    (5,N'Mô hình & Nhân vật',  '2024-01-05 08:00:00'),
    (6,N'Đồ chơi điều khiển',  '2024-01-05 08:00:00');
SET IDENTITY_INSERT [dbo].[SuperCategories] OFF;

SET IDENTITY_INSERT [dbo].[Categories] ON;
IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE CategoryID = 1)
    INSERT INTO [dbo].[Categories] (CategoryID, SuperCategoryID, CategoryName, CreatedAt) VALUES
    ( 1,1,N'Lego',                      '2024-01-05 08:00:00'),
    ( 2,1,N'Xếp hình khối',            '2024-01-05 08:00:00'),
    ( 3,2,N'Thẻ học thông minh',       '2024-01-05 08:00:00'),
    ( 4,2,N'Đồ chơi khoa học',        '2024-01-05 08:00:00'),
    ( 5,3,N'Xe chòi chân',             '2024-01-05 08:00:00'),
    ( 6,3,N'Bóng',                      '2024-01-05 08:00:00'),
    ( 7,3,N'Diều',                      '2024-01-05 08:00:00'),
    ( 8,4,N'Gấu bông & Thú nhồi bông', '2024-01-05 08:00:00'),
    ( 9,4,N'Búp bê thay đồ',           '2024-01-05 08:00:00'),
    (10,5,N'Siêu nhân',                 '2024-01-05 08:00:00'),
    (11,5,N'Khủng long',                '2024-01-05 08:00:00'),
    (12,6,N'Xe điều khiển',            '2024-01-05 08:00:00'),
    (13,6,N'Máy bay RC',                '2024-01-05 08:00:00');
SET IDENTITY_INSERT [dbo].[Categories] OFF;

SET IDENTITY_INSERT [dbo].[Materials] ON;
IF NOT EXISTS (SELECT 1 FROM [dbo].[Materials] WHERE MaterialID = 1)
    INSERT INTO [dbo].[Materials] (MaterialID, MaterialName, CreatedAt) VALUES
    (1,N'Nhựa ABS cao cấp',   '2024-01-05 08:00:00'),
    (2,N'Gỗ tự nhiên MDF',   '2024-01-05 08:00:00'),
    (3,N'Vải cotton & nhung', '2024-01-05 08:00:00'),
    (4,N'Hợp kim nhôm',      '2024-01-05 08:00:00'),
    (5,N'Cao su thiên nhiên', '2024-01-05 08:00:00');
SET IDENTITY_INSERT [dbo].[Materials] OFF;

SET IDENTITY_INSERT [dbo].[Ages] ON;
IF NOT EXISTS (SELECT 1 FROM [dbo].[Ages] WHERE AgeID = 1)
    INSERT INTO [dbo].[Ages] (AgeID, AgeRange, CreatedAt) VALUES
    (1,N'0-1', '2024-01-05 08:00:00'),
    (2,N'1-3', '2024-01-05 08:00:00'),
    (3,N'3-6', '2024-01-05 08:00:00'),
    (4,N'6-12','2024-01-05 08:00:00'),
    (5,N'12+', '2024-01-05 08:00:00');
SET IDENTITY_INSERT [dbo].[Ages] OFF;

SET IDENTITY_INSERT [dbo].[Origins] ON;
IF NOT EXISTS (SELECT 1 FROM [dbo].[Origins] WHERE OriginID = 1)
    INSERT INTO [dbo].[Origins] (OriginID, OriginName, CreatedAt) VALUES
    (1,N'Việt Nam',   '2024-01-05 08:00:00'),
    (2,N'Trung Quốc', '2024-01-05 08:00:00'),
    (3,N'Mỹ',        '2024-01-05 08:00:00'),
    (4,N'Nhật Bản',  '2024-01-05 08:00:00'),
    (5,N'Đan Mạch',  '2024-01-05 08:00:00');
SET IDENTITY_INSERT [dbo].[Origins] OFF;

SET IDENTITY_INSERT [dbo].[Brands] ON;
IF NOT EXISTS (SELECT 1 FROM [dbo].[Brands] WHERE BrandID = 1)
    INSERT INTO [dbo].[Brands] (BrandID, BrandName, CreatedAt) VALUES
    (1,N'Lego',         '2024-01-05 08:00:00'),
    (2,N'Fisher-Price', '2024-01-05 08:00:00'),
    (3,N'Hot Wheels',   '2024-01-05 08:00:00'),
    (4,N'Polo',         '2024-01-05 08:00:00'),
    (5,N'MyKingdom',    '2024-01-05 08:00:00'),
    (6,N'Bandai',       '2024-01-05 08:00:00'),
    (7,N'Hasbro',       '2024-01-05 08:00:00'),
    (8,N'Mattel',       '2024-01-05 08:00:00');
SET IDENTITY_INSERT [dbo].[Brands] OFF;

SET IDENTITY_INSERT [dbo].[PriceRanges] ON;
IF NOT EXISTS (SELECT 1 FROM [dbo].[PriceRanges] WHERE PriceRangeID = 1)
    INSERT INTO [dbo].[PriceRanges] (PriceRangeID, PriceRangeMin, PriceRangeMax, CreatedAt) VALUES
    (1,        0,    199000,'2024-01-05 08:00:00'),  -- 0 - 199K
    (2,   200000,    499000,'2024-01-05 08:00:00'),  -- 200K - 499K
    (3,   500000,    999000,'2024-01-05 08:00:00'),  -- 500K - 999K
    (4,  1000000,   1999000,'2024-01-05 08:00:00'),  -- 1M - 1.999M
    (5,  2000000,   4999000,'2024-01-05 08:00:00'),  -- 2M - 4.999M
    (6,  5000000,   9999000,'2024-01-05 08:00:00'),  -- 5M - 9.999M
    (7, 10000000, 999999000,'2024-01-05 08:00:00');  -- 10M+
SET IDENTITY_INSERT [dbo].[PriceRanges] OFF;

SET IDENTITY_INSERT [dbo].[StatusOrders] ON;
IF NOT EXISTS (SELECT 1 FROM [dbo].[StatusOrders] WHERE StatusID = 1)
    INSERT INTO [dbo].[StatusOrders] (StatusID, StatusName, Description) VALUES
    (1,'Pending',    N'Chờ xác nhận'),
    (2,'Confirmed',  N'Đã xác nhận'),
    (3,'Processing', N'Đang xử lý / đóng gói'),
    (4,'Shipped',    N'Đã bàn giao vận chuyển'),
    (5,'Delivering', N'Đang giao'),
    (6,'Delivered',  N'Đã giao thành công'),
    (7,'Completed',  N'Hoàn thành'),
    (8,'Cancelled',  N'Đã hủy'),
    (9,'Refunded',   N'Đã hoàn tiền'),
    (10,'Returning',        N'Đang hoàn hàng về shop'), 
    (11,'ReturnCompleted',  N'Hàng đã về shop, chờ xử lý'),
    (12, 'DeliveryFailed', N'Giao thất bại, chờ xử lý'),
    (13, 'WaitingReturn',  N'Chờ hoàn hàng về shop'),
    (14, 'ReturnFailed',   N'Hoàn hàng thất bại'),
    (15, 'Lost',           N'Hàng bị mất trong vận chuyển'),
    (16, 'Damaged',        N'Hàng bị hư hỏng');
SET IDENTITY_INSERT [dbo].[StatusOrders] OFF;

SET IDENTITY_INSERT [dbo].[ReactionTypes] ON;
IF NOT EXISTS (SELECT 1 FROM [dbo].[ReactionTypes] WHERE ReactionTypeID = 1)
    INSERT INTO [dbo].[ReactionTypes] (ReactionTypeID, Code, DisplayName, CreatedAt) VALUES
    (1,'like',  N'Thích',     '2024-01-05 08:00:00'),
    (2,'love',  N'Yêu thích', '2024-01-05 08:00:00'),
    (3,'haha',  N'Haha',      '2024-01-05 08:00:00'),
    (4,'wow',   N'Wow',       '2024-01-05 08:00:00'),
    (5,'sad',   N'Buồn',      '2024-01-05 08:00:00');
SET IDENTITY_INSERT [dbo].[ReactionTypes] OFF;
GO

SET IDENTITY_INSERT [dbo].[StatusRefunds] ON;
IF NOT EXISTS (SELECT 1 FROM [dbo].[StatusRefunds] WHERE StatusID = 1)
    INSERT INTO [dbo].[StatusRefunds] (StatusID, StatusName, Description) VALUES
    (1, 'RefundRequested',        N'Khách hàng yêu cầu hoàn tiền/trả hàng'),
    (2, 'RefundApproved',         N'Yêu cầu được chấp nhận, chờ tạo vận đơn thu hồi'),
    (3, 'RefundRejected',         N'Yêu cầu bị từ chối'),
    (4, 'RefundPickupCreated',    N'Đã tạo đơn thu hồi GHN, chờ shipper lấy hàng'),
    (5, 'RefundShipping',         N'Hàng hoàn đang trên đường về kho'),
    (6, 'RefundReceived',         N'Kho đã nhận được hàng hoàn'),
    (7, 'RefundInspectionPending', N'Hàng đang được kiểm tra chất lượng tại kho'),
    (8, 'RefundCompleted',        N'Đã hoàn tiền cho khách & nhập kho thành công'),
    (9, 'RefundCancelled',        N'Khách hàng đã hủy yêu cầu hoàn tiền');
SET IDENTITY_INSERT [dbo].[StatusRefunds] OFF;
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 8 – PRODUCTS (30 sản phẩm)
══════════════════════════════════════════════════════════════ */
PRINT N'[8] Products...';
SET IDENTITY_INSERT [dbo].[Products] ON;
IF NOT EXISTS (SELECT 1 FROM [dbo].[Products] WHERE ProductID = 1)
    INSERT INTO [dbo].[Products]
        (ProductID,ProductName,Price,Quantity,ProductStatus,CategoryID,BrandID,PriceRangeID,StockThreshold,CreatedAt)
    VALUES
    ( 1,N'Lego City Trạm Cảnh Sát Trung Tâm 668 Mảnh',        1290000, 45,'Active', 1,1,4,10,'2024-02-01 08:00:00'),
    ( 2,N'Lego Technic Siêu Xe Bugatti Chiron 3599 Mảnh',      5990000,  8,'Active', 1,1,6, 3,'2024-02-03 08:00:00'),
    ( 3,N'Lego Friends Nhà Nghỉ Dưỡng Malia 699 Mảnh',        1590000, 22,'Active', 1,1,4, 5,'2024-02-05 08:00:00'),
    ( 4,N'Bộ Xếp Hình Gỗ Phương Tiện Giao Thông 36 Khối',      420000,180,'Active', 2,5,2,20,'2024-02-08 08:00:00'),
    ( 5,N'Xếp Hình Gỗ Bảng Chữ Cái 52 Khối Màu Sắc',          350000,200,'Active', 2,5,2,30,'2024-02-10 08:00:00'),
    ( 6,N'Thẻ Học Thông Minh 4D Động Vật Hoang Dã 120 Thẻ',    145000,600,'Active', 3,2,1,50,'2024-02-12 08:00:00'),
    ( 7,N'Bộ Thẻ Học Toán Tư Duy Số Đếm 1-100',               185000,450,'Active', 3,5,1,50,'2024-02-14 08:00:00'),
    ( 8,N'Kính Hiển Vi Đồ Chơi ScienceMax 40-400x',            375000, 75,'Active', 4,3,2, 8,'2024-03-01 08:00:00'),
    ( 9,N'Bộ Thí Nghiệm Núi Lửa Phun Trào Mini',               280000,120,'Active', 4,5,2,15,'2024-03-03 08:00:00'),
    (10,N'Bộ Kính Thiên Văn Trẻ Em 50x/100x',                  490000, 55,'Active', 4,2,2, 8,'2024-03-05 08:00:00'),
    (11,N'Xe Chòi Chân Hình Vịt Donald Có Đèn Nhạc',           545000, 38,'Active', 5,2,3, 5,'2024-03-10 08:00:00'),
    (12,N'Xe Đạp 3 Bánh Disney Princess Có Tay Vịn',           890000, 18,'Active', 5,8,3, 3,'2024-03-12 08:00:00'),
    (13,N'Bóng Cao Su Hoa Văn Boho Size 5',                     85000,320,'Active', 6,2,1,30,'2024-03-15 08:00:00'),
    (14,N'Bóng Đá FIFA Pro Cao Su Lưu Hóa Size 4',             165000,250,'Active', 6,3,1,25,'2024-03-17 08:00:00'),
    (15,N'Diều Hình Đại Bàng Cánh Lớn 1.4m Kèm Dây 30m',      115000,140,'Active', 7,2,1,15,'2024-03-20 08:00:00'),
    (16,N'Diều Hình Rồng 3D Khung Carbon Siêu Bền',            185000, 90,'Active', 7,2,1,10,'2024-03-22 08:00:00'),
    (17,N'Gấu Bông Khủng Long Rex Xanh Lá 80cm Siêu Mềm',      840000, 28,'Active', 8,5,3, 4,'2024-04-01 08:00:00'),
    (18,N'Gấu Bông Gấu Trúc Panda 60cm Dáng Nằm',             650000, 35,'Active', 8,5,3, 5,'2024-04-03 08:00:00'),
    (19,N'Gấu Bông Thỏ Tai Dài Pastel 45cm',                   420000, 60,'Active', 8,5,2, 8,'2024-04-05 08:00:00'),
    (20,N'Búp Bê Barbie Dreamtopia Tiên Cá Kèm 3 Bộ Váy',     359000,110,'Active', 9,8,2,12,'2024-04-08 08:00:00'),
    (21,N'Búp Bê Barbie Fashionista Set 6 Trang Phục',         490000, 75,'Active', 9,8,2, 8,'2024-04-10 08:00:00'),
    (22,N'Mô Hình Siêu Nhân Gao Red Ranger Khớp Xoay 24 Điểm',595000, 22,'Active',10,6,3, 4,'2024-04-12 08:00:00'),
    (23,N'Mô Hình Kamen Rider Zero-One Bandai SHFiguarts',      890000, 15,'Active',10,6,3, 3,'2024-04-14 08:00:00'),
    (24,N'Mô Hình Khủng Long T-Rex Cơ Bắp Tỉ Lệ 1:10',       395000, 88,'Active',11,3,2,10,'2024-04-16 08:00:00'),
    (25,N'Bộ 6 Khủng Long Jurassic World Mini',               285000,150,'Active',11,7,2,15,'2024-04-18 08:00:00'),
    (26,N'Siêu Xe Địa Hình RC Traxxas TRX-Mini 4x4 Turbo',    945000, 55,'Active',12,3,3, 6,'2024-05-01 08:00:00'),
    (27,N'Xe RC Drift Bánh Nhôm 2.4GHz Tốc Độ 30km/h',        680000, 40,'Active',12,3,3, 6,'2024-05-03 08:00:00'),
    (28,N'Trực Thăng Mini Gyro RC 2.4GHz Chống Va Chạm',     1490000, 12,'Active',13,6,4, 2,'2024-05-10 08:00:00'),
    (29,N'Đất Nặn PlayDoh 24 Màu An Toàn Không Độc Hại',        88000,980,'Active', 2,7,1,80,'2024-05-15 08:00:00'),
    (30,N'Bảng Vẽ Ma Thuật Tự Xóa LCD 10 Inch Kèm Bút',      175000,380,'Active', 3,2,1,40,'2024-05-20 08:00:00');
SET IDENTITY_INSERT [dbo].[Products] OFF;
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 9 – PRODUCT DETAILS
══════════════════════════════════════════════════════════════ */
PRINT N'[9] ProductDetails...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[ProductDetails] WHERE ProductID = 1)
    INSERT INTO [dbo].[ProductDetails]
        (ProductID, Description, MaterialID, AgeID, SexID, OriginID,
         WeightGram, LengthCm, WidthCm, HeightCm)
    VALUES
    -- Lego (1-3)
    ( 1,N'Bộ Lego City 668 mảnh tái hiện trạm cảnh sát 3 tầng. Nhựa ABS EN71. Phù hợp 6+ tuổi.',          1,4,1,5,  900, 48, 28, 9),
    ( 2,N'Lego Technic 3599 mảnh siêu xe Bugatti Chiron tỉ lệ 1:8. Động cơ W16 mô phỏng. 18+ tuổi.',     1,5,1,5, 3400, 58, 38,14),
    ( 3,N'Lego Friends nhà nghỉ dưỡng với hồ bơi, spa, và 5 nhân vật. 8+ tuổi.',                          1,4,2,5, 1100, 53, 37,10),
    -- Xếp hình gỗ (4-5)
    ( 4,N'36 khối gỗ MDF sơn nước an toàn. Bo góc 5mm. Phù hợp 1–5 tuổi.',                               2,3,3,1,  650, 30, 20,10),
    ( 5,N'52 khối gỗ bảng chữ cái A-Z, a-z. Màu sắc rực rỡ. 2-5 tuổi.',                                  2,2,3,1,  750, 32, 22,10),
    -- Thẻ học (6-7)
    ( 6,N'120 thẻ 4D tích hợp AR – quét app xem 60 loài động vật sống động.',                             3,3,3,2,  300, 22, 15, 5),
    ( 7,N'100 thẻ học toán từ 1-100. Hai mặt: số đếm và bài tập. 3-8 tuổi.',                              3,3,3,1,  250, 21, 14, 4),
    -- Khoa học (8-10)
    ( 8,N'Kính hiển vi ScienceMax 3 mức zoom (40x/100x/400x), đèn LED, 12 tiêu bản.',                     1,4,3,3,  850, 30, 15,20),
    ( 9,N'Bộ thí nghiệm núi lửa phun trào gồm 12 thí nghiệm an toàn. 6+ tuổi.',                          1,4,3,3,  420, 26, 20, 8),
    (10,N'Kính thiên văn 2 mức phóng đại (50x/100x). Kèm chân đế và bản đồ sao. 8+ tuổi.',               1,4,3,3,  680, 55, 12,12),
    -- Xe chòi chân / xe đạp (11-12)
    (11,N'Xe chòi chân hình vịt Donald: đèn LED + nhạc. Tải tối đa 30kg.',                                5,3,3,2, 2800, 55, 35,42),
    (12,N'Xe đạp 3 bánh Disney Princess khung thép. Có tay vịn + ô che nắng. 2-5 tuổi.',                  4,3,2,2, 4500, 76, 43,60),
    -- Bóng (13-14)
    (13,N'Bóng cao su thiên nhiên size 5 họa tiết Boho. Lưu hóa 2 lớp.',                                  5,3,3,1,  430, 22, 22,22),
    (14,N'Bóng đá FIFA Pro cao su lưu hóa size 4. Chống thấm nước.',                                      5,4,1,3,  390, 20, 20,20),
    -- Diều (15-16)
    (15,N'Diều đại bàng sải cánh 1.4m, khung carbon, vải polyester 210T.',                                2,4,1,1,  280,140, 10, 5),
    (16,N'Diều rồng 3D sải cánh 1.8m. Khung carbon siêu bền. Gió 3-8 Beaufort.',                         2,4,1,1,  350,180, 12, 6),
    -- Gấu bông / thú nhồi bông (17-19)
    (17,N'Gấu bông khủng long Rex 80cm vải nhung siêu mềm, bông PP chống nấm.',                           3,2,2,2,  900, 80, 35,40),
    (18,N'Gấu trúc Panda 60cm nằm, vải nhung cao cấp, bông PP, giặt máy được.',                           3,2,3,2,  680, 60, 30,25),
    (19,N'Thỏ tai dài 45cm màu pastel. Vải nhung Hàn Quốc. Kèm hộp quà.',                                3,2,2,2,  480, 45, 20,20),
    -- Búp bê Barbie (20-21)
    (20,N'Barbie Dreamtopia Tiên Cá chính hãng Mattel. Tóc gradient tím-hồng, 3 bộ trang phục.',         1,3,2,3,  280, 32, 10,30),
    (21,N'Barbie Fashionista set 6 trang phục đa phong cách. 3+ tuổi.',                                   1,3,2,3,  320, 33, 12,30),
    -- Siêu nhân / mô hình (22-23)
    (22,N'Siêu Nhân Gao Red Ranger Bandai Nhật, cao 18cm, 24 khớp xoay. Limited edition.',                5,5,1,4,  180, 12,  8,18),
    (23,N'Kamen Rider Zero-One SHFiguarts cao 15cm, 30 khớp. Kèm 8 bàn tay thay thế.',                   5,5,1,4,  160, 10,  8,15),
    -- Khủng long (24-25)
    (24,N'T-Rex tỉ lệ 1:10, cao 25cm, dài 45cm. Hợp kim nhôm-nhựa ABS. Miệng lò xo.',                   4,4,1,3,  520, 45, 18,25),
    (25,N'Bộ 6 khủng long Jurassic World mini cao 8-12cm. Nhựa ABS mềm.',                                 1,3,1,3,  350, 30, 20, 8),
    -- Xe RC (26-27)
    (26,N'RC Traxxas TRX-Mini 1:16, brushless 2838KV, max 45km/h. Pin LiPo.',                             4,4,1,3,  680, 36, 22,14),
    (27,N'Xe RC drift bánh nhôm CNC. Tốc độ 30km/h. Sạc USB 90 phút.',                                   4,4,1,2,  520, 34, 20,12),
    -- Máy bay RC (28)
    (28,N'Trực thăng RC Gyro 4 kênh 2.4GHz, con quay 6 trục. Bay 12-15 phút.',                            1,5,1,2,  280, 32, 32,12),
    -- Đất nặn / bảng vẽ (29-30)
    (29,N'PlayDoh 24 màu chính hãng Hasbro, mỗi hộp 85g. Không độc, không gluten.',                      3,3,3,3, 2040, 35, 24, 8),
    (30,N'Bảng LCD 10 inch viết-vẽ-xóa tức thì. 1 pin CR2025 dùng 50.000 lần.',                          1,2,3,2,  210, 28, 18, 1);
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 10 – PRODUCT IMAGES
══════════════════════════════════════════════════════════════ */
PRINT N'[10] ProductImages...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[ProductImages] WHERE ProductID = 1 AND IsMain = 1)
BEGIN
    DECLARE @i INT = 1;
    WHILE @i <= 30
    BEGIN
        DECLARE @seed VARCHAR(30) = 'toy' + CAST(@i AS VARCHAR);
        INSERT INTO [dbo].[ProductImages] (ProductID, ImageUrl, IsMain, CreatedAt) VALUES
        (@i, 'https://picsum.photos/seed/' + @seed + '-a/400/400', 1, GETDATE()),
        (@i, 'https://picsum.photos/seed/' + @seed + '-b/400/400', 0, GETDATE()),
        (@i, 'https://picsum.photos/seed/' + @seed + '-c/400/400', 0, GETDATE()),
        (@i, 'https://picsum.photos/seed/' + @seed + '-d/400/400', 0, GETDATE());
        SET @i = @i + 1;
    END
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 11 – BLOCK REASONS
══════════════════════════════════════════════════════════════ */
PRINT N'[11] BlockReasons...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[BlockReasons])
    INSERT INTO [dbo].[BlockReasons] (Content, Description, CreatedAt) VALUES
    (N'Spam đánh giá',        N'Đăng đánh giá hàng loạt không liên quan',           '2024-01-05 08:00:00'),
    (N'Dấu hiệu gian lận',   N'Đặt hàng rồi khai không nhận để hoàn tiền',         '2024-01-05 08:00:00'),
    (N'Ngôn ngữ xúc phạm',   N'Nhắn tin xúc phạm nhân viên',                       '2024-01-05 08:00:00'),
    (N'Tài khoản trùng lặp', N'Phát hiện nhiều tài khoản cùng một người dùng',     '2024-01-05 08:00:00'),
    (N'Lạm dụng voucher',    N'Sử dụng nhiều tài khoản để hưởng voucher chào mừng','2024-01-05 08:00:00');
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 12 – USER BLOCK HISTORY
══════════════════════════════════════════════════════════════ */
PRINT N'[12] UserBlockHistory...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[UserBlockHistory])
BEGIN
    DECLARE @admBlk INT = (SELECT TOP 1 AccountID FROM Accounts WHERE Email='admin@toyhouse.vn');
    DECLARE @br2    INT = (SELECT TOP 1 BlockReasonID FROM BlockReasons WHERE Content=N'Dấu hiệu gian lận');

    INSERT INTO [dbo].[UserBlockHistory]
        (AccountID, BlockedBy, BlockReasonID, Note, BlockedAt, BlockedUntil, UnblockedAt)
    VALUES
    (
        (SELECT TOP 1 AccountID FROM Accounts WHERE Email='bauchau.vu@gmail.com'),
        @admBlk, @br2,
        N'Tài khoản báo cáo không nhận hàng 1 lần trong tháng 3/2026 (đã gỡ chặn)',
        '2026-03-06 09:00:00', '2026-03-13 09:00:00', '2026-03-13 09:00:00'
    );
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 13 – PROMOTIONS
══════════════════════════════════════════════════════════════ */
PRINT N'[13] Promotions...';
DECLARE @admID INT = (SELECT TOP 1 AccountID FROM Accounts WHERE Email='admin@toyhouse.vn');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Promotions] WHERE PromotionName = N'Sale Hè Rực Rỡ 2026')
    INSERT INTO [dbo].[Promotions]
        (CreatedBy,PromotionName,PromotionType,Description,StartDate,EndDate,Status,Priority,CreatedAt)
    VALUES
    (@admID,N'Sale Hè Rực Rỡ 2026','DISCOUNT',
     N'Giảm giá hàng loạt đồ chơi chào hè – áp dụng từ 01/04 đến 30/06/2026',
     '2026-04-01','2026-06-30','Active',10,'2026-03-20 09:00:00'),
    (@admID,N'Back To School 2026','DISCOUNT',
     N'Mùa tựu trường – giảm đặc biệt đồ chơi giáo dục và thẻ học',
     '2026-07-15','2026-09-15','Scheduled',8,'2026-07-01 09:00:00'),
    (@admID,N'Siêu Flash Sale 5.5 – 48 Tiếng','FLASH_SALE',
     N'Giảm sốc lên đến 50% – số lượng có hạn!',
     '2026-05-04','2026-05-06','Scheduled',20,'2026-04-15 10:00:00'),
    (@admID,N'Flash Sale Ngày Gia Đình 28/6','FLASH_SALE',
     N'Ưu đãi đặc biệt nhân Ngày Gia Đình Việt Nam',
     '2026-06-27','2026-06-29','Scheduled',15,'2026-06-10 09:00:00'),
    (@admID,N'Giáng Sinh & Tết Dương Lịch 2027','DISCOUNT',
     N'Tặng quà yêu thương – giảm đến 40% toàn bộ mô hình và búp bê',
     '2026-12-20','2027-01-05','Scheduled',12,'2026-12-01 09:00:00');
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 14 – PROMOTION TIME SLOTS
══════════════════════════════════════════════════════════════ */
PRINT N'[14] PromotionTimeSlots...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[PromotionTimeSlots])
BEGIN
    DECLARE @pFlash1 INT = (SELECT TOP 1 PromotionID FROM Promotions WHERE PromotionName=N'Siêu Flash Sale 5.5 – 48 Tiếng');
    DECLARE @pFlash2 INT = (SELECT TOP 1 PromotionID FROM Promotions WHERE PromotionName=N'Flash Sale Ngày Gia Đình 28/6');
    INSERT INTO [dbo].[PromotionTimeSlots] (PromotionID,StartAt,EndAt,Status,CreatedAt) VALUES
    (@pFlash1,'2026-05-04 02:00:00','2026-05-04 05:00:00','Scheduled','2026-04-15 03:00:00'),
    (@pFlash1,'2026-05-04 13:00:00','2026-05-04 15:00:00','Scheduled','2026-04-15 03:00:00'),
    (@pFlash1,'2026-05-05 02:00:00','2026-05-05 05:00:00','Scheduled','2026-04-15 03:00:00'),
    (@pFlash1,'2026-05-05 13:00:00','2026-05-05 15:00:00','Scheduled','2026-04-15 03:00:00'),
    (@pFlash2,'2026-06-27 13:00:00','2026-06-27 15:00:00','Scheduled','2026-06-10 03:00:00'),
    (@pFlash2,'2026-06-28 13:00:00','2026-06-28 15:00:00','Scheduled','2026-06-10 03:00:00');
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 15 – PRODUCT PROMOTIONS (DISCOUNT)
══════════════════════════════════════════════════════════════ */
PRINT N'[15] ProductPromotions...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[ProductPromotions])
BEGIN
    DECLARE @pSale INT = (SELECT TOP 1 PromotionID FROM Promotions WHERE PromotionName=N'Sale Hè Rực Rỡ 2026');
    DECLARE @pBTS  INT = (SELECT TOP 1 PromotionID FROM Promotions WHERE PromotionName=N'Back To School 2026');
    INSERT INTO [dbo].[ProductPromotions]
        (ProductID,PromotionID,SalePrice,DiscountPercent,SaleQuantity,SoldQuantity,ReservedQuantity,CreatedAt)
    VALUES
    ( 1,@pSale,1099000,14.88,30, 8,0,'2026-03-20 02:00:00'),
    ( 2,@pSale,4990000,16.69, 5, 1,0,'2026-03-20 02:00:00'),
    ( 3,@pSale,1290000,18.87,15, 3,0,'2026-03-20 02:00:00'),
    (11,@pSale, 449000,17.61,20, 5,0,'2026-03-20 02:00:00'),
    (17,@pSale, 699000,16.79,15, 4,0,'2026-03-20 02:00:00'),
    (20,@pSale, 299000,16.71,30, 9,0,'2026-03-20 02:00:00'),
    ( 6,@pBTS,   69000,18.82,NULL,0,0,'2026-07-01 02:00:00'),
    ( 7,@pBTS,  149000,18.92,NULL,0,0,'2026-07-01 02:00:00'),
    (29,@pBTS,   70000,20.45,NULL,0,0,'2026-07-01 02:00:00'),
    ( 8,@pBTS,  299000,20.27,NULL,0,0,'2026-07-01 02:00:00');
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 16 – PROMOTION PRODUCT SLOTS (FLASH_SALE)
══════════════════════════════════════════════════════════════ */
PRINT N'[16] PromotionProductSlots...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[PromotionProductSlots])
BEGIN
    DECLARE @s1 INT=(SELECT TOP 1 TimeSlotID FROM PromotionTimeSlots WHERE StartAt='2026-05-04 02:00:00');
    DECLARE @s2 INT=(SELECT TOP 1 TimeSlotID FROM PromotionTimeSlots WHERE StartAt='2026-05-04 13:00:00');
    DECLARE @s3 INT=(SELECT TOP 1 TimeSlotID FROM PromotionTimeSlots WHERE StartAt='2026-05-05 02:00:00');
    DECLARE @s4 INT=(SELECT TOP 1 TimeSlotID FROM PromotionTimeSlots WHERE StartAt='2026-05-05 13:00:00');
    INSERT INTO [dbo].[PromotionProductSlots]
        (TimeSlotID,ProductID,SalePrice,DiscountPercent,SaleQuantity,SoldQuantity,ReservedQuantity,CreatedAt)
    VALUES
    (@s1,24,197000,50.13,10,0,0,'2026-04-15 03:00:00'),
    (@s1,13, 39000,54.12,50,0,0,'2026-04-15 03:00:00'),
    (@s2,17,420000,50.00,12,0,0,'2026-04-15 03:00:00'),
    (@s3, 1,645000,50.00,20,0,0,'2026-04-15 03:00:00'),
    (@s3,20,179000,50.14,30,0,0,'2026-04-15 03:00:00'),
    (@s4, 2,2995000,50.00,5,0,0,'2026-04-15 03:00:00'),
    (@s4,29, 40000,54.55,100,0,0,'2026-04-15 03:00:00');
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 17 – VOUCHERS
══════════════════════════════════════════════════════════════ */
PRINT N'[17] Vouchers...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[Vouchers] WHERE VoucherCode='SHIP0626')
    INSERT INTO [dbo].[Vouchers]
        (VoucherCode,VoucherName,VoucherDescription,DiscountType,DiscountValue,
         MaxDiscountCap,DiscountTarget,MinOrderAmount,TotalQuantity,UsedQuantity,
         MaxUsagePerUser,StartDate,EndDate,Status,IsDeleted,CreatedAt)
    VALUES
    ('SHIP0626', N'Freeship Tháng 6',        N'Miễn phí vận chuyển tối đa 50.000đ – đơn từ 200.000đ',
     'FIXED',  50000,50000,'SHIPPING_FEE',200000,1000,0,1,'2026-06-01','2026-06-30','Active',0,'2026-05-25 08:00:00'),
    ('WELCOME10',N'Giảm 10% Chào Khách Mới',N'Giảm 10% lần mua đầu tiên',
     'PERCENTAGE',10,200000,'ORDER_TOTAL',0,500,0,1,'2026-01-01','2026-12-31','Active',0,'2026-01-01 08:00:00'),
    ('VIP200K', N'Ưu Đãi Khách VIP 200K',   N'Áp dụng đơn từ 1.000.000đ',
     'FIXED',200000,NULL,'ORDER_TOTAL',1000000,100,0,1,'2026-04-01','2026-06-30','Active',0,'2026-03-20 09:00:00'),
    ('KIDSBDAY',N'Quà Sinh Nhật 50K',        N'Giảm 50.000đ cho đơn từ 300.000đ',
     'FIXED', 50000,NULL,'ORDER_TOTAL',300000,1000,0,1,'2026-01-01','2026-12-31','Active',0,'2026-01-01 08:00:00');
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 18 – ORDERS (10 đơn hàng)
══════════════════════════════════════════════════════════════ */
PRINT N'[18] Orders (10 đơn hàng)...';

IF OBJECT_ID('tempdb..#OrderBatch') IS NOT NULL DROP TABLE #OrderBatch;
CREATE TABLE #OrderBatch (
    Email VARCHAR(100), OrderCode VARCHAR(30), StatusID TINYINT,
    OrderDate DATETIME2(0), ConfirmedAt DATETIME2(0), ShippedAt DATETIME2(0),
    DeliveredAt DATETIME2(0), CompletedAt DATETIME2(0), CancelledAt DATETIME2(0),
    PaymentMethod VARCHAR(20), PaymentStatus VARCHAR(20), PaidAt DATETIME2(0),
    SubTotal DECIMAL(12,0), ShipFee DECIMAL(10,0), VoucherDiscount DECIMAL(12,0),
    TotalAmount DECIMAL(12,0), CancelReason NVARCHAR(500), PaymentCode VARCHAR(50),
    ShipCode VARCHAR(50), ShipName NVARCHAR(100), ShipPhone VARCHAR(15),
    ShipAddress NVARCHAR(500), ShipWard VARCHAR(20), ShipDistrict INT, ShipProvince INT
);

INSERT INTO #OrderBatch VALUES
('lananh.pham@gmail.com','ORD-2026-00001',7,'2026-02-14 10:30:00','2026-02-14 14:00:00','2026-02-15 09:00:00','2026-02-16 11:00:00','2026-02-17 08:00:00',NULL,
 'WALLET','PAID','2026-02-14 10:30:00',1290000,0,0,1290000,NULL,'PAY-00001',NULL,N'Phạm Thị Lan Anh','0912001001',N'72 Lê Lợi, P. Bến Nghé','W10101',101,1),
('lananh.pham@gmail.com','ORD-2026-00041',7,'2026-04-05 09:00:00','2026-04-05 11:00:00','2026-04-06 09:00:00','2026-04-07 14:00:00','2026-04-08 08:00:00',NULL,
 'WALLET','PAID','2026-04-05 09:00:00',420000,20000,0,440000,NULL,'PAY-00041',NULL,N'Phạm Thị Lan Anh','0912001001',N'72 Lê Lợi, P. Bến Nghé','W10101',101,1),
('hung.nguyen88@gmail.com','ORD-2026-00002',4,'2026-04-10 08:15:00','2026-04-10 11:00:00','2026-04-11 09:30:00',NULL,NULL,NULL,
 'SE_PAY','PAID','2026-04-10 08:15:00',945000,30000,0,975000,NULL,'PAY-00002','GHN-00002',N'Nguyễn Văn Hùng','0912001002',N'15 Trần Phú, P. Mộ Lao','W20501',205,2),
('hung.nguyen88@gmail.com','ORD-2026-00042',7,'2026-03-22 10:00:00','2026-03-22 12:00:00','2026-03-23 09:00:00','2026-03-24 15:00:00','2026-03-25 08:00:00',NULL,
 'SE_PAY','PAID','2026-03-22 10:00:00',280000,20000,0,300000,NULL,'PAY-00042',NULL,N'Nguyễn Văn Hùng','0912001002',N'15 Trần Phú, P. Mộ Lao','W20501',205,2),
('bauchau.vu@gmail.com','ORD-2026-00003',8,'2026-03-05 16:45:00',NULL,NULL,NULL,NULL,'2026-03-06 17:00:00',
 'SE_PAY','PENDING',NULL,840000,25000,0,865000,N'Khách chưa chuyển khoản sau 24h',NULL,NULL,N'Vũ Thị Bảo Châu','0912001003',N'88 Nguyễn Trãi, P. Nhân Chính','W20301',203,2),
('mtuando@outlook.com','ORD-2026-00004',2,'2026-04-18 09:00:00','2026-04-18 10:30:00',NULL,NULL,NULL,NULL,
 'SHIP_COD','COD_PENDING',NULL,1698000,35000,0,1733000,NULL,NULL,NULL,N'Đỗ Minh Tuấn','0912001004',N'34 Đinh Tiên Hoàng, P. Đa Kao','W10102',101,1),
('mtuando@outlook.com','ORD-2026-00049',7,'2026-02-18 09:00:00','2026-02-18 11:00:00','2026-02-19 09:00:00','2026-02-20 14:00:00','2026-02-21 08:00:00',NULL,
 'SE_PAY','PAID','2026-02-18 09:00:00',595000,25000,0,620000,NULL,'PAY-00049',NULL,N'Đỗ Minh Tuấn','0912001004',N'34 Đinh Tiên Hoàng, P. Đa Kao','W10102',101,1),
('thuha.hoang@gmail.com','ORD-2026-00005',6,'2026-04-01 13:20:00','2026-04-01 15:00:00','2026-04-02 09:00:00','2026-04-03 14:30:00',NULL,NULL,
 'WALLET','PAID','2026-04-01 13:20:00',1199000,0,0,1199000,NULL,'PAY-00005',NULL,N'Hoàng Thị Thu Hà','0912001005',N'120 Lê Văn Lương, P. Nhân Chính','W20301',203,2),
('thuha.hoang@gmail.com','ORD-2026-00044',7,'2026-02-25 09:00:00','2026-02-25 11:00:00','2026-02-26 09:00:00','2026-02-27 14:00:00','2026-02-28 08:00:00',NULL,
 'SE_PAY','PAID','2026-02-25 09:00:00',359000,20000,0,379000,NULL,'PAY-00044',NULL,N'Hoàng Thị Thu Hà','0912001005',N'120 Lê Văn Lương, P. Nhân Chính','W20301',203,2);

INSERT INTO [dbo].[Orders]
    (AccountID,StatusID,OrderCode,
     ShippingName,ShippingPhone,ShippingAddress,
     ShippingWardCode,ShippingWardName,ShippingDistrictId,ShippingDistrictName,
     ShippingProvinceId,ShippingProvinceName,
     OrderDate,ConfirmedAt,ShippedAt,DeliveredAt,CompletedAt,CancelledAt,
     PaymentMethod,PaymentStatus,PaidAt,PaymentCode,ShippingOrderCode,
     SubTotal,EstimatedShippingFee,VoucherDiscountAmount,TotalAmount,
     CancelReason,CreatedAt)
SELECT
    a.AccountID, b.StatusID, b.OrderCode,
    b.ShipName, b.ShipPhone, b.ShipAddress,
    b.ShipWard, ISNULL(w.WardName, N'Phường N/A'),
    b.ShipDistrict, ISNULL(d.DistrictName, N'Quận N/A'),
    b.ShipProvince, ISNULL(p.ProvinceName, N'Tỉnh N/A'),
    b.OrderDate, b.ConfirmedAt, b.ShippedAt,
    b.DeliveredAt, b.CompletedAt, b.CancelledAt,
    b.PaymentMethod, b.PaymentStatus, b.PaidAt,
    ISNULL(b.PaymentCode, 'PAY-COD-' + b.OrderCode),
    ISNULL(b.ShipCode, 'GHN-PEN-' + b.OrderCode),
    b.SubTotal, b.ShipFee, b.VoucherDiscount, b.TotalAmount,
    b.CancelReason, b.OrderDate
FROM #OrderBatch b
JOIN Accounts a ON a.Email = b.Email
LEFT JOIN Wards w ON w.WardCode = b.ShipWard
LEFT JOIN Districts d ON d.DistrictId = b.ShipDistrict
LEFT JOIN Provinces p ON p.ProvinceId = b.ShipProvince
WHERE NOT EXISTS (SELECT 1 FROM Orders o WHERE o.OrderCode = b.OrderCode);

DROP TABLE #OrderBatch;
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 19 – ORDER DETAILS
══════════════════════════════════════════════════════════════ */
PRINT N'[19] OrderDetails...';
IF OBJECT_ID('tempdb..#OD') IS NOT NULL DROP TABLE #OD;
CREATE TABLE #OD (OrderCode VARCHAR(30), ProductID INT, Qty SMALLINT, UnitPrice DECIMAL(12,0));
INSERT INTO #OD VALUES
('ORD-2026-00001', 1,1,1290000),
('ORD-2026-00041', 4,1, 420000),
('ORD-2026-00002',26,1, 945000),
('ORD-2026-00042', 9,1, 280000),
('ORD-2026-00003',17,1, 840000),
('ORD-2026-00004', 1,1,1290000),('ORD-2026-00004',29,2,88000),('ORD-2026-00004',30,1,175000),('ORD-2026-00004', 6,1,145000),
('ORD-2026-00049',22,1, 595000),
('ORD-2026-00005',20,1, 359000),('ORD-2026-00005',17,1,840000),
('ORD-2026-00044',20,1, 359000);

INSERT INTO [dbo].[OrderDetails]
    (OrderID,ProductID,ProductName,Quantity,UnitPrice,DiscountAmount,CreatedAt)
SELECT o.OrderID, p.ProductID, p.ProductName, od.Qty, od.UnitPrice, 0, o.OrderDate
FROM #OD od
JOIN Orders  o ON o.OrderCode  = od.OrderCode
JOIN Products p ON p.ProductID = od.ProductID
WHERE NOT EXISTS (
    SELECT 1 FROM OrderDetails x
    WHERE x.OrderID=o.OrderID AND x.ProductID=p.ProductID);

DROP TABLE #OD;
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 20 – ORDER STATUS HISTORY
══════════════════════════════════════════════════════════════ */
PRINT N'[20] OrderStatusHistory...';
INSERT INTO [dbo].[OrderStatusHistory] (OrderID,StatusID,Note,CreatedAt)
SELECT o.OrderID, 1, N'Đơn hàng mới', o.OrderDate FROM Orders o
WHERE NOT EXISTS (SELECT 1 FROM OrderStatusHistory h WHERE h.OrderID=o.OrderID AND h.StatusID=1);

INSERT INTO [dbo].[OrderStatusHistory] (OrderID,StatusID,Note,CreatedAt)
SELECT o.OrderID, 2, N'Xác nhận đơn', o.ConfirmedAt FROM Orders o WHERE o.ConfirmedAt IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM OrderStatusHistory h WHERE h.OrderID=o.OrderID AND h.StatusID=2);

INSERT INTO [dbo].[OrderStatusHistory] (OrderID,StatusID,Note,CreatedAt)
SELECT o.OrderID, 3, N'Đang đóng gói', DATEADD(HOUR,2,o.ConfirmedAt)
FROM Orders o WHERE o.ConfirmedAt IS NOT NULL AND o.StatusID IN (3,4,5,6,7)
  AND NOT EXISTS (SELECT 1 FROM OrderStatusHistory h WHERE h.OrderID=o.OrderID AND h.StatusID=3);

INSERT INTO [dbo].[OrderStatusHistory] (OrderID,StatusID,Note,CreatedAt)
SELECT o.OrderID, 4, N'Bàn giao vận chuyển', o.ShippedAt FROM Orders o WHERE o.ShippedAt IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM OrderStatusHistory h WHERE h.OrderID=o.OrderID AND h.StatusID=4);

INSERT INTO [dbo].[OrderStatusHistory] (OrderID,StatusID,Note,CreatedAt)
SELECT o.OrderID, 6, N'Giao hàng thành công', o.DeliveredAt FROM Orders o WHERE o.DeliveredAt IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM OrderStatusHistory h WHERE h.OrderID=o.OrderID AND h.StatusID=6);

INSERT INTO [dbo].[OrderStatusHistory] (OrderID,StatusID,Note,CreatedAt)
SELECT o.OrderID, 7, N'Hoàn thành', o.CompletedAt FROM Orders o WHERE o.CompletedAt IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM OrderStatusHistory h WHERE h.OrderID=o.OrderID AND h.StatusID=7);

INSERT INTO [dbo].[OrderStatusHistory] (OrderID,StatusID,Note,CreatedAt)
SELECT o.OrderID, 8, N'Đã hủy: '+ISNULL(o.CancelReason,N'Không rõ lý do'), o.CancelledAt
FROM Orders o WHERE o.CancelledAt IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM OrderStatusHistory h WHERE h.OrderID=o.OrderID AND h.StatusID=8);
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 22 – PAYMENT HISTORY
══════════════════════════════════════════════════════════════ */
PRINT N'[22] PaymentHistory...';
INSERT INTO [dbo].[PaymentHistory]
    (AccountID,OrderID,PaymentStatus,PaymentMethod,TransactionCode,Amount,CreatedAt)
SELECT o.AccountID, o.OrderID, o.PaymentStatus, o.PaymentMethod,
       ISNULL(o.PaymentCode,'TXN-'+o.OrderCode), o.TotalAmount, o.OrderDate
FROM Orders o
WHERE NOT EXISTS (SELECT 1 FROM PaymentHistory ph WHERE ph.OrderID=o.OrderID);
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 23 – PAYMENT GATEWAY TRANSACTIONS
══════════════════════════════════════════════════════════════ */
PRINT N'[23] PaymentGatewayTransactions...';
INSERT INTO [dbo].[PaymentGatewayTransactions]
    (OrderID,Provider,RequestID,Amount,ResponseCode,ResponseMessage,Status,CreatedAt)
SELECT o.OrderID,'SE_PAY','REQ-'+o.OrderCode,o.TotalAmount,
       CASE o.PaymentStatus WHEN 'PAID' THEN '00' WHEN 'FAILED' THEN '99' ELSE '01' END,
       CASE o.PaymentStatus WHEN 'PAID' THEN 'Transaction Success' WHEN 'FAILED' THEN 'Transaction Failed' ELSE 'Pending' END,
       CASE o.PaymentStatus WHEN 'PAID' THEN 'Paid' WHEN 'FAILED' THEN 'Failed' ELSE 'Pending' END,
       o.OrderDate
FROM Orders o
WHERE o.PaymentMethod='SE_PAY'
  AND NOT EXISTS (SELECT 1 FROM PaymentGatewayTransactions pg WHERE pg.OrderID=o.OrderID);
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 24 – WALLETS & TRANSACTIONS
══════════════════════════════════════════════════════════════ */
PRINT N'[24] Wallets, WalletTransactions...';
INSERT INTO [dbo].[Wallets] (AccountID,Currency,Balance,Status,CreatedAt)
SELECT AccountID,'VND',20000000,'Active',GETDATE()
FROM Accounts
WHERE NOT EXISTS (SELECT 1 FROM Wallets w WHERE w.AccountID=Accounts.AccountID);

INSERT INTO [dbo].[WalletTransactions]
    (WalletID,AccountID,TxnType,Direction,Amount,BalanceBefore,BalanceAfter,Method,Status,Reason,CreatedAt)
SELECT w.WalletID,w.AccountID,'TopUp','CR',20000000,0,20000000,'BankTransfer','Completed',N'Nạp tiền ban đầu',GETDATE()
FROM Wallets w
WHERE NOT EXISTS (
    SELECT 1 FROM WalletTransactions wt WHERE wt.WalletID=w.WalletID AND wt.TxnType='TopUp');

INSERT INTO [dbo].[WalletTransactions]
    (WalletID,AccountID,RelatedOrderID,TxnType,Direction,Amount,
     BalanceBefore,BalanceAfter,Method,Status,Reason,CreatedAt)
SELECT
    w.WalletID, o.AccountID, o.OrderID, 'Payment','DR', o.TotalAmount,
    20000000 - ISNULL((SELECT SUM(o2.TotalAmount) FROM Orders o2
        WHERE o2.AccountID=o.AccountID AND o2.PaymentMethod='WALLET'
          AND o2.PaymentStatus='PAID' AND o2.OrderID < o.OrderID),0),
    20000000 - ISNULL((SELECT SUM(o2.TotalAmount) FROM Orders o2
        WHERE o2.AccountID=o.AccountID AND o2.PaymentMethod='WALLET'
          AND o2.PaymentStatus='PAID' AND o2.OrderID < o.OrderID),0) - o.TotalAmount,
    'Internal','Completed',N'Thanh toán đơn '+o.OrderCode, o.OrderDate
FROM Orders o
JOIN Wallets w ON w.AccountID=o.AccountID
WHERE o.PaymentMethod='WALLET' AND o.PaymentStatus='PAID'
  AND NOT EXISTS (SELECT 1 FROM WalletTransactions wt WHERE wt.RelatedOrderID=o.OrderID AND wt.TxnType='Payment');
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 25 – WALLET PINS
══════════════════════════════════════════════════════════════ */
PRINT N'[25] WalletPins...';
INSERT INTO [dbo].[WalletPins] (WalletID,PinHash,IsActive,FailedAttempts,CreatedAt)
SELECT w.WalletID,'$2a$11$pin_hash_placeholder_for_seed_data_only_xxxxxxxxxx',1,0,GETDATE()
FROM Wallets w
JOIN Accounts a ON a.AccountID=w.AccountID
WHERE a.RoleID=1
  AND NOT EXISTS (SELECT 1 FROM WalletPins wp WHERE wp.WalletID=w.WalletID);
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 26 – SHIPPING
══════════════════════════════════════════════════════════════ */
PRINT N'[26] Shipping...';
INSERT INTO [dbo].[ShippingProviderTransactions]
    (OrderID,Provider,ProviderOrderCode,TrackingNumber,Status,ShippingFee,CodAmount,CreatedAt)
SELECT
    o.OrderID,'GHN',ISNULL(o.ShippingOrderCode,'GHN-'+o.OrderCode),
    'VN'+RIGHT(REPLACE(o.OrderCode,'-',''),8),
    CASE o.StatusID WHEN 7 THEN 'delivered' WHEN 6 THEN 'delivered'
                    WHEN 4 THEN 'transporting' ELSE 'ready_to_pick' END,
    o.EstimatedShippingFee,
    CASE o.PaymentMethod WHEN 'SHIP_COD' THEN o.TotalAmount ELSE 0 END,
    ISNULL(o.ShippedAt, o.OrderDate)
FROM Orders o
WHERE o.StatusID >= 2 AND o.StatusID <> 8
  AND NOT EXISTS (SELECT 1 FROM ShippingProviderTransactions sp WHERE sp.OrderID=o.OrderID);

INSERT INTO [dbo].[ShippingStatusHistories]
    (ShippingTxId,OrderId,PreviousStatus,NewStatus,Source,ProcessedAt)
SELECT sp.ShippingTransactionID, sp.OrderID,'created',sp.Status,'Seed',GETDATE()
FROM ShippingProviderTransactions sp
WHERE NOT EXISTS (
    SELECT 1 FROM ShippingStatusHistories sh WHERE sh.ShippingTxId=sp.ShippingTransactionID);
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 27 – ORDER REFUNDS
══════════════════════════════════════════════════════════════ */
PRINT N'[27] OrderRefundReasons...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderRefundReasons])
BEGIN
    INSERT INTO [dbo].[OrderRefundReasons] (Content, Description, IsDeleted, IsSystem, CreatedAt) VALUES
    (N'Sản phẩm bị lỗi từ nhà sản xuất', N'Hàng giao đến bị lỗi kỹ thuật', 0, 0, '2024-01-05 08:00:00'),
    (N'Giao nhầm sản phẩm', N'Shop gửi sai màu, size hoặc model', 0, 0, '2024-01-05 08:00:00'),
    (N'Sản phẩm không đúng mô tả', N'Hình ảnh/mô tả không khớp sản phẩm thực tế', 0, 0, '2024-01-05 08:00:00'),
    (N'Hàng bị hư hỏng trong vận chuyển', N'Kiện hàng bị móp méo, vỡ trong quá trình giao', 0, 0, '2024-01-05 08:00:00'),
    (N'Thiếu phụ kiện đi kèm', N'Hộp không có đủ phụ kiện như mô tả', 0, 0, '2024-01-05 08:00:00'),
    (N'Giao hàng thất bại / không giao được', N'Refund tự động khi GHN trả hàng về kho do giao thất bại (System-only)', 0, 1, GETDATE());
END
GO


IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderRefunds])
BEGIN
    DECLARE @refCust3 INT = (SELECT TOP 1 AccountID FROM Accounts WHERE Email='bauchau.vu@gmail.com');
    DECLARE @refR1    INT = (SELECT TOP 1 RefundReasonID FROM OrderRefundReasons WHERE Content=N'Sản phẩm bị lỗi từ nhà sản xuất');
    DECLARE @ord3     INT = (SELECT TOP 1 OrderID FROM Orders WHERE OrderCode='ORD-2026-00003');

    IF @ord3 IS NOT NULL
        INSERT INTO [dbo].[OrderRefunds]
            (OrderID,RefundReasonID,CustomerID,RequestedBy,RefundCode,SubTotal,TotalAmount,ApprovedAmount,StatusID,CreatedAt)
        VALUES (@ord3, @refR1, @refCust3, @refCust3, 'REF-2026-00001', 840000, 840000, 840000, 1, '2026-03-07 09:00:00');

    INSERT INTO [dbo].[RefundImages] (RefundID,ImageURL,CreatedAt)
    SELECT RefundID,'https://picsum.photos/seed/refund-'+CAST(RefundID AS VARCHAR)+'-a/400/400',GETDATE() FROM OrderRefunds;
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 28 – CART & WISHLIST
══════════════════════════════════════════════════════════════ */
PRINT N'[28] Cart, CartItems, Wishlists...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[Cart])
BEGIN
    INSERT INTO [dbo].[Cart] (AccountID,CreatedAt)
    SELECT AccountID, GETDATE() FROM Accounts WHERE RoleID=1;

    INSERT INTO [dbo].[CartItems]
		(CartID,ProductID,Quantity,PriceAtThatTime,CurrentPrice,AddedAt)
	SELECT c.CartID, v.ProductID, v.Qty, v.Pr, v.Pr, GETDATE()
	FROM Cart c
	JOIN Accounts a ON a.AccountID=c.AccountID
	JOIN (VALUES
		('lananh.pham@gmail.com',   1, 1, 1290000),
		('lananh.pham@gmail.com',  29, 2,   88000),
		('hung.nguyen88@gmail.com',26, 1,  945000),
		('hung.nguyen88@gmail.com',28, 1, 1490000),
		('bauchau.vu@gmail.com',    1, 1, 1290000),
		('bauchau.vu@gmail.com',   17, 1,  840000),
		('mtuando@outlook.com',     8, 1,  375000),
		('mtuando@outlook.com',    24, 1,  395000),
		('thuha.hoang@gmail.com',  11, 1,  545000),
		('thuha.hoang@gmail.com',  15, 2,  115000)
	) AS v(Email, ProductID, Qty, Pr) ON a.Email = v.Email  -- bỏ số 1 thừa ở đây
	WHERE NOT EXISTS (
		SELECT 1 FROM CartItems ci WHERE ci.CartID=c.CartID AND ci.ProductID=v.ProductID);
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Wishlists])
    INSERT INTO [dbo].[Wishlists] (AccountID,ProductID,CreatedAt)
    SELECT a.AccountID, v.ProductID, GETDATE()
    FROM Accounts a
    JOIN (VALUES
        ('lananh.pham@gmail.com',   2),
        ('lananh.pham@gmail.com',  23),
        ('hung.nguyen88@gmail.com', 1),
        ('hung.nguyen88@gmail.com',27),
        ('bauchau.vu@gmail.com',   18),
        ('mtuando@outlook.com',    22),
        ('thuha.hoang@gmail.com',   3),
        ('thuha.hoang@gmail.com',  12)
    ) AS v(Email,ProductID) ON a.Email=v.Email;
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 29 – PRODUCT FOLLOWERS
══════════════════════════════════════════════════════════════ */
PRINT N'[29] ProductFollowers...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[ProductFollowers])
    INSERT INTO [dbo].[ProductFollowers] (ProductID, AccountID, CreatedAt)
    SELECT v.ProductID, a.AccountID, GETDATE() -- Đã đảo lại đúng thứ tự
    FROM Accounts a
    JOIN (VALUES
        ('lananh.pham@gmail.com',  28),
        ('hung.nguyen88@gmail.com', 2),
        ('bauchau.vu@gmail.com',   12),
        ('mtuando@outlook.com',     3),
        ('thuha.hoang@gmail.com',   1)
    ) AS v(Email,ProductID) ON a.Email=v.Email
    WHERE NOT EXISTS (
        SELECT 1 FROM ProductFollowers pf
        WHERE pf.AccountID=a.AccountID AND pf.ProductID=v.ProductID);
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 30 – CUSTOMER CHILDREN
══════════════════════════════════════════════════════════════ */
PRINT N'[30] CustomerChildren...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[CustomerChildren])
    INSERT INTO [dbo].[CustomerChildren] (AccountID,SexID,FullName,NickName,DOB,CreatedAt)
    SELECT a.AccountID, v.SexID, v.FullName, v.NickName, v.DOB, GETDATE()
    FROM Accounts a
    JOIN (VALUES
        ('lananh.pham@gmail.com',  1,N'Phạm Tiến Phát',   N'Củ Cải', '2020-05-15'),
        ('lananh.pham@gmail.com',  2,N'Phạm Thảo Trân',   N'Bào Ngư','2022-11-20'),
        ('bauchau.vu@gmail.com',   1,N'Vũ Hoàng Nam',     N'Gấu',    '2019-08-10'),
        ('mtuando@outlook.com',    1,N'Đỗ Khắc Duy',      N'Bi',     '2017-09-08'),
        ('mtuando@outlook.com',    2,N'Đỗ Thùy Linh',     N'Bông',   '2020-12-18'),
        ('thuha.hoang@gmail.com',  1,N'Hoàng Minh Tuấn',  N'Tun',    '2018-07-12')
    ) AS v(Email,SexID,FullName,NickName,DOB) ON a.Email=v.Email;
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 31 – BLOG
   NOTE: BlogPostStats được tự động tạo bởi trigger
         trg_BlogPost_InitStats khi INSERT BlogPosts → không cần seed thủ công
══════════════════════════════════════════════════════════════ */
PRINT N'[31] Blog...';
SET IDENTITY_INSERT [dbo].[BlogCategories] ON;
IF NOT EXISTS (SELECT 1 FROM [dbo].[BlogCategories] WHERE BlogCategoryID=1)
    INSERT INTO [dbo].[BlogCategories] (BlogCategoryID,BlogCategoriesName,CreatedAt) VALUES
    (1,N'Tin Tức & Khuyến Mãi',   '2024-01-10 08:00:00'),
    (2,N'Kiến Thức Nuôi Dạy Trẻ', '2024-01-10 08:00:00'),
    (3,N'Review Sản Phẩm',         '2024-01-10 08:00:00'),
    (4,N'Vui Chơi & Sáng Tạo',     '2024-01-10 08:00:00'),
    (5,N'An Toàn Đồ Chơi',         '2024-01-10 08:00:00');
SET IDENTITY_INSERT [dbo].[BlogCategories] OFF;
GO

DECLARE @bst1 INT=(SELECT TOP 1 AccountID FROM Accounts WHERE Email='nhung.st@toyhouse.vn');
DECLARE @bst2 INT=(SELECT TOP 1 AccountID FROM Accounts WHERE Email='dung.st@toyhouse.vn');
DECLARE @badm INT=(SELECT TOP 1 AccountID FROM Accounts WHERE Email='admin@toyhouse.vn');

IF NOT EXISTS (SELECT 1 FROM [dbo].[BlogPosts])
    INSERT INTO [dbo].[BlogPosts]
        (AccountID,ApprovedBy,BlogCategoryID,BlogTitle,BlogContent,BlogThumbnail,Status,IsFeatured,BlogAt,CreatedAt)
    VALUES
    (@bst1,@badm,1,N'Sale Hè Rực Rỡ – Giảm Đến 50% Đồ Chơi Chính Hãng',
     N'Chuỗi sự kiện Sale Hè 2026 bắt đầu từ 01/04! Lego, Bandai, Fisher-Price đồng loạt giảm đến 50%.',
     'https://picsum.photos/seed/blog-sale-he/600/400','Published',1,'2026-03-28 09:00:00','2026-03-28 09:00:00'),
    (@bst1,@badm,2,N'7 Tiêu Chí Vàng Chọn Đồ Chơi An Toàn Cho Trẻ Dưới 3 Tuổi',
     N'Giai đoạn 0–3 tuổi là thời điểm vàng phát triển. Cần chú ý: không BPA, không góc sắc, sơn nước không độc.',
     'https://picsum.photos/seed/blog-safety/600/400','Published',0,'2026-02-10 10:00:00','2026-02-10 10:00:00'),
    (@bst2,@badm,3,N'Review Thực Tế: Lego City Trạm Cảnh Sát Sau 3 Tháng Sử Dụng',
     N'Bộ 668 mảnh chất lượng rất tốt sau 3 tháng. Mảnh ghép chắc chắn, không gãy. Bé 7 tuổi tự lắp được 80%.',
     'https://picsum.photos/seed/blog-review-lego/600/400','Published',1,'2026-03-05 14:00:00','2026-03-05 14:00:00'),
    (@bst2,@badm,4,N'Top 10 Đồ Chơi Vận Động Phát Triển Thể Chất Cho Bé 3-6 Tuổi',
     N'Vận động không chỉ giúp bé khỏe mạnh mà còn phát triển trí tuệ. Top 10 đồ chơi được yêu thích nhất.',
     'https://picsum.photos/seed/blog-activity/600/400','Published',1,'2026-04-01 10:00:00','2026-04-01 10:00:00'),
    (@bst1,@badm,1,N'Flash Sale 5.5 – Siêu Ưu Đãi Chỉ 48 Tiếng Bắt Đầu Từ 4/5',
     N'Duy nhất 48 tiếng Flash Sale 5.5! Giảm đến 50% hàng trăm sản phẩm. Số lượng có hạn!',
     'https://picsum.photos/seed/blog-flash55/600/400','Published',0,'2026-04-20 08:00:00','2026-04-20 08:00:00');
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[ReviewBlogs])
BEGIN
    ;WITH BlogRanked AS (
        SELECT BlogPostID, ROW_NUMBER() OVER (ORDER BY BlogPostID) AS Pos
        FROM [dbo].[BlogPosts]
    )
    INSERT INTO [dbo].[ReviewBlogs] (BlogPostID, AccountID, Comment, CreatedAt)
    SELECT br.BlogPostID, a.AccountID, v.Comment, v.CreatedAt
    FROM (VALUES
        (1,'lananh.pham@gmail.com',   N'Mình đã mua Lego City trong đợt này, giảm 15% rất hời! Bé nhà mình mê lắm.','2026-03-29 10:00:00'),
        (1,'hung.nguyen88@gmail.com', N'Sale to thật! Vừa đặt RC Traxxas, tiết kiệm được nhiều.','2026-03-30 08:00:00'),
        (1,'bauchau.vu@gmail.com',    N'Có áp dụng voucher VIP200K kèm sale hè không ad?','2026-03-30 09:30:00'),
        (1,'mtuando@outlook.com',     N'Giao hàng siêu nhanh, đặt sáng chiều đã có. Đóng gói cẩn thận 5 sao!','2026-03-31 14:00:00'),
        (1,'thuha.hoang@gmail.com',   N'Đã mua Barbie Dreamtopia cho bé gái, con thích lắm cảm ơn shop!','2026-04-01 09:00:00'),
        (2,'lananh.pham@gmail.com',   N'Bài viết rất bổ ích! Trước giờ mình toàn mua theo cảm tính.','2026-02-11 10:00:00'),
        (2,'thuha.hoang@gmail.com',   N'Cảm ơn shop! Mình đã chia sẻ bài này cho hội mẹ bỉm rồi.','2026-02-12 11:00:00'),
        (3,'hung.nguyen88@gmail.com', N'Review rất chi tiết! Mình cũng đang nghĩ mua bộ này cho con 8 tuổi.','2026-03-06 11:00:00'),
        (3,'mtuando@outlook.com',     N'Mình mua rồi cũng thấy xứng đáng. Bé 7 tuổi tự lắp được 70%.','2026-03-08 10:00:00'),
        (4,'lananh.pham@gmail.com',   N'Xe chòi chân vịt Donald nhà mình mua cho con 2 tuổi, bé leo lên đạp ngay!','2026-04-02 09:00:00'),
        (4,'bauchau.vu@gmail.com',    N'Bài viết rất hữu ích! Đang tìm đồ chơi sinh nhật cho bé trai 4 tuổi.','2026-04-04 11:00:00'),
        (5,'hung.nguyen88@gmail.com', N'Đã đặt lịch nhắc cho ngày 4/5, không thể bỏ lỡ Flash Sale này!','2026-04-21 10:00:00'),
        (5,'mtuando@outlook.com',     N'50% thật không? Mô hình Bandai mà giảm 50% là quá hời rồi!','2026-04-22 08:00:00')
    ) AS v(BpPos, Email, Comment, CreatedAt)
    JOIN BlogRanked br ON br.Pos = v.BpPos
    JOIN Accounts a ON a.Email = v.Email;

    DECLARE @staffRep INT=(SELECT TOP 1 AccountID FROM Accounts WHERE Email='nhung.st@toyhouse.vn');
    INSERT INTO [dbo].[ReviewBlogReplies] (ReviewBlogID,AccountID,Comment,CreatedAt)
    SELECT rb.ReviewBlogID, @staffRep,
           N'Cảm ơn bạn đã đồng hành cùng ToyHouse! Chúc bé luôn vui vẻ và phát triển toàn diện!',
           DATEADD(HOUR,1,rb.CreatedAt)
    FROM ReviewBlogs rb;

    DECLARE @badm2 INT=(SELECT TOP 1 AccountID FROM Accounts WHERE Email='admin@toyhouse.vn');
    INSERT INTO [dbo].[ReviewBlogReactions] (ReviewBlogID,AccountID,ReactionTypeID,CreatedAt)
    SELECT rb.ReviewBlogID, @badm2, 1, DATEADD(MINUTE,30,rb.CreatedAt)
    FROM ReviewBlogs rb
    WHERE NOT EXISTS (
        SELECT 1 FROM ReviewBlogReactions r WHERE r.ReviewBlogID=rb.ReviewBlogID AND r.AccountID=@badm2);

    INSERT INTO [dbo].[BlogPostReactions] (BlogPostID,AccountID,ReactionTypeID,CreatedAt)
    SELECT b.BlogPostID, a.AccountID, v.ReactionTypeID, GETDATE()
    FROM BlogPosts b
    CROSS JOIN (VALUES
        ('lananh.pham@gmail.com',   1),('hung.nguyen88@gmail.com',  2),
        ('bauchau.vu@gmail.com',    1),('thuha.hoang@gmail.com',    1),
        ('mtuando@outlook.com',     2)
    ) AS v(Email,ReactionTypeID)
    JOIN Accounts a ON a.Email=v.Email
    WHERE b.Status='Published'
      AND NOT EXISTS (
          SELECT 1 FROM BlogPostReactions bpr
          WHERE bpr.BlogPostID=b.BlogPostID AND bpr.AccountID=a.AccountID);
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 32 – REVIEWS
══════════════════════════════════════════════════════════════ */
PRINT N'[32] ReviewProducts...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[ReviewProducts])
BEGIN
    INSERT INTO [dbo].[ReviewProducts]
        (AccountID,ProductID,OrderID,Rating,Comment,ModerationStatus,CreatedAt)
    SELECT a.AccountID, v.ProductID, o.OrderID, v.Rating, v.Comment, 'Approved', v.CreatedAt
    FROM (VALUES
        ('lananh.pham@gmail.com',  1,'ORD-2026-00001',5,N'Mua cho con trai 7 tuổi, bé mê lắm! Hộp đẹp, mảnh ghép chắc. Giao hàng nhanh 1 ngày!','2026-02-18 09:00:00'),
        ('lananh.pham@gmail.com',  4,'ORD-2026-00041',5,N'Lần 2 mua vì lần trước quá ưng! Shop đóng gói cẩn thận.','2026-04-09 09:00:00'),
        ('hung.nguyen88@gmail.com',9,'ORD-2026-00042',4,N'Bộ núi lửa phun trào rất vui! Con trai 6 tuổi thích mê. Hướng dẫn toàn tiếng Anh hơi khó.','2026-03-26 10:00:00'),
        ('mtuando@outlook.com',   22,'ORD-2026-00049',5,N'Siêu Nhân Gao chắc chắn, 24 khớp xoay rất linh hoạt. Con 8 tuổi tạo pose đủ kiểu.','2026-02-22 10:00:00'),
        ('thuha.hoang@gmail.com', 20,'ORD-2026-00044',5,N'Barbie Dreamtopia tiên cá đẹp lắm! Con gái 5 tuổi đòi mua thêm bộ khác luôn.','2026-02-28 11:00:00')
    ) AS v(Email, ProductID, OrderCode, Rating, Comment, CreatedAt)
    JOIN Accounts a ON a.Email = v.Email
    JOIN Orders   o ON o.OrderCode = v.OrderCode
    WHERE EXISTS (
        SELECT 1 FROM OrderDetails od WHERE od.OrderID = o.OrderID AND od.ProductID = v.ProductID
    )
    AND NOT EXISTS (
        SELECT 1 FROM ReviewProducts x
        WHERE x.AccountID = a.AccountID AND x.OrderID = o.OrderID AND x.ProductID = v.ProductID
    );

    DECLARE @stfRpl INT=(SELECT TOP 1 AccountID FROM Accounts WHERE RoleID=3);
    INSERT INTO [dbo].[StaffReviewProductReplies] (ReviewProductID,StaffID,Content,CreatedAt)
    SELECT r.ReviewID, @stfRpl,
           N'Cảm ơn quý khách rất nhiều vì review chi tiết! Chúng mình rất vui khi sản phẩm mang lại niềm vui cho bé. Hẹn gặp lại! 🎁',
           DATEADD(HOUR,3,r.CreatedAt)
    FROM ReviewProducts r;

    INSERT INTO [dbo].[ReviewProductImages] (ReviewProductID,ImageURL,ModerationStatus,CreatedAt)
    SELECT ReviewID,'https://picsum.photos/seed/rv'+CAST(ReviewID AS VARCHAR)+'-1/300/300','Approved',GETDATE() FROM ReviewProducts;
    INSERT INTO [dbo].[ReviewProductImages] (ReviewProductID,ImageURL,ModerationStatus,CreatedAt)
    SELECT ReviewID,'https://picsum.photos/seed/rv'+CAST(ReviewID AS VARCHAR)+'-2/300/300','Approved',GETDATE() FROM ReviewProducts;

    INSERT INTO [dbo].[ReviewProductReactions] (ReviewProductID,AccountID,ReactionTypeID,CreatedAt)
    SELECT rp.ReviewID, a.AccountID, 1, GETDATE()
    FROM ReviewProducts rp
    CROSS JOIN (SELECT TOP 3 AccountID FROM Accounts WHERE RoleID=1 ORDER BY AccountID) a
    WHERE rp.AccountID <> a.AccountID
      AND NOT EXISTS (SELECT 1 FROM ReviewProductReactions x WHERE x.ReviewProductID=rp.ReviewID AND x.AccountID=a.AccountID);
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 33 – SHIFT TEMPLATES & WORK SCHEDULES
══════════════════════════════════════════════════════════════ */
PRINT N'[33] ShiftTemplates...';
SET IDENTITY_INSERT [dbo].[ShiftTemplates] ON;
IF NOT EXISTS (SELECT 1 FROM [dbo].[ShiftTemplates] WHERE ShiftTemplateID=1)
    INSERT INTO [dbo].[ShiftTemplates]
        (ShiftTemplateID,ShiftName,StartTime,EndTime,MaxOrdersPerShift,IsActive,CreatedAt)
    VALUES
    (1,N'Morning Shift', '07:00:00','12:00:00',20,1,'2026-01-01 08:00:00'),
    (2,N'Afternoon Shift','12:00:00','17:00:00',25,1,'2026-01-01 08:00:00'),
    (3,N'Evening Shift',  '17:00:00','22:00:00',25,1,'2026-01-01 08:00:00');
SET IDENTITY_INSERT [dbo].[ShiftTemplates] OFF;
GO

PRINT N'[33] WorkSchedules...';
DECLARE @admWS INT=(SELECT TOP 1 AccountID FROM Accounts WHERE Email='admin@toyhouse.vn');
DECLARE @stf1  INT=(SELECT TOP 1 AccountID FROM Accounts WHERE Email='nhung.st@toyhouse.vn');
DECLARE @stf2  INT=(SELECT TOP 1 AccountID FROM Accounts WHERE Email='dung.st@toyhouse.vn');
DECLARE @stf3  INT=(SELECT TOP 1 AccountID FROM Accounts WHERE Email='thao.st@toyhouse.vn');
DECLARE @mc1   INT=(SELECT TOP 1 AccountID FROM Accounts WHERE Email='bao.kho@toyhouse.vn');
DECLARE @mc2   INT=(SELECT TOP 1 AccountID FROM Accounts WHERE Email='lan.kho@toyhouse.vn');

DECLARE @dayWS DATE='2026-04-01';
DECLARE @endWS DATE='2026-05-14';

WHILE @dayWS <= @endWS
BEGIN
    DECLARE @stWS VARCHAR(20)=
        CASE WHEN @dayWS < '2026-05-13' THEN 'Completed'
             WHEN @dayWS = '2026-05-13' THEN 'OnDuty'
             ELSE 'Scheduled' END;

    IF NOT EXISTS (SELECT 1 FROM WorkSchedules WHERE AccountID=@stf1 AND WorkDate=@dayWS AND ShiftTemplateID=1)
        INSERT INTO WorkSchedules(AccountID,ShiftTemplateID,WorkDate,Status,CreatedBy,CreatedAt) VALUES(@stf1,1,@dayWS,@stWS,@admWS,DATEADD(DAY,-7,@dayWS));
    IF NOT EXISTS (SELECT 1 FROM WorkSchedules WHERE AccountID=@stf1 AND WorkDate=@dayWS AND ShiftTemplateID=2)
        INSERT INTO WorkSchedules(AccountID,ShiftTemplateID,WorkDate,Status,CreatedBy,CreatedAt) VALUES(@stf1,2,@dayWS,@stWS,@admWS,DATEADD(DAY,-7,@dayWS));
    IF NOT EXISTS (SELECT 1 FROM WorkSchedules WHERE AccountID=@stf2 AND WorkDate=@dayWS AND ShiftTemplateID=2)
        INSERT INTO WorkSchedules(AccountID,ShiftTemplateID,WorkDate,Status,CreatedBy,CreatedAt) VALUES(@stf2,2,@dayWS,@stWS,@admWS,DATEADD(DAY,-7,@dayWS));
    IF NOT EXISTS (SELECT 1 FROM WorkSchedules WHERE AccountID=@stf2 AND WorkDate=@dayWS AND ShiftTemplateID=3)
        INSERT INTO WorkSchedules(AccountID,ShiftTemplateID,WorkDate,Status,CreatedBy,CreatedAt) VALUES(@stf2,3,@dayWS,@stWS,@admWS,DATEADD(DAY,-7,@dayWS));
    IF NOT EXISTS (SELECT 1 FROM WorkSchedules WHERE AccountID=@stf3 AND WorkDate=@dayWS AND ShiftTemplateID=1)
        INSERT INTO WorkSchedules(AccountID,ShiftTemplateID,WorkDate,Status,CreatedBy,CreatedAt) VALUES(@stf3,1,@dayWS,@stWS,@admWS,DATEADD(DAY,-7,@dayWS));
    IF NOT EXISTS (SELECT 1 FROM WorkSchedules WHERE AccountID=@stf3 AND WorkDate=@dayWS AND ShiftTemplateID=3)
        INSERT INTO WorkSchedules(AccountID,ShiftTemplateID,WorkDate,Status,CreatedBy,CreatedAt) VALUES(@stf3,3,@dayWS,@stWS,@admWS,DATEADD(DAY,-7,@dayWS));
    IF NOT EXISTS (SELECT 1 FROM WorkSchedules WHERE AccountID=@mc1 AND WorkDate=@dayWS AND ShiftTemplateID=2)
        INSERT INTO WorkSchedules(AccountID,ShiftTemplateID,WorkDate,Status,CreatedBy,CreatedAt) VALUES(@mc1,2,@dayWS,@stWS,@admWS,DATEADD(DAY,-7,@dayWS));
    IF NOT EXISTS (SELECT 1 FROM WorkSchedules WHERE AccountID=@mc1 AND WorkDate=@dayWS AND ShiftTemplateID=3)
        INSERT INTO WorkSchedules(AccountID,ShiftTemplateID,WorkDate,Status,CreatedBy,CreatedAt) VALUES(@mc1,3,@dayWS,@stWS,@admWS,DATEADD(DAY,-7,@dayWS));
    IF NOT EXISTS (SELECT 1 FROM WorkSchedules WHERE AccountID=@mc2 AND WorkDate=@dayWS AND ShiftTemplateID=1)
        INSERT INTO WorkSchedules(AccountID,ShiftTemplateID,WorkDate,Status,CreatedBy,CreatedAt) VALUES(@mc2,1,@dayWS,@stWS,@admWS,DATEADD(DAY,-7,@dayWS));
    IF NOT EXISTS (SELECT 1 FROM WorkSchedules WHERE AccountID=@mc2 AND WorkDate=@dayWS AND ShiftTemplateID=2)
        INSERT INTO WorkSchedules(AccountID,ShiftTemplateID,WorkDate,Status,CreatedBy,CreatedAt) VALUES(@mc2,2,@dayWS,@stWS,@admWS,DATEADD(DAY,-7,@dayWS));

    SET @dayWS=DATEADD(DAY,1,@dayWS);
END
GO

/* [FIX-01] StaffShiftCapacity:
   Rows were already INSERTed by the trigger when WorkSchedules was created.
   Only UPDATE CurrentLoad to reflect the correct shift status.
   CurrentLoad = MaxLoad when Completed, MaxLoad/2 when OnDuty, 0 when Scheduled. */
PRINT N'[33-FIX] StaffShiftCapacity – UPDATE CurrentLoad (rows already created by trigger)...';
UPDATE ssc
SET
    ssc.CurrentLoad = CASE ws.Status
                          WHEN 'Completed' THEN st.MaxOrdersPerShift
                          WHEN 'OnDuty'    THEN st.MaxOrdersPerShift / 2
                          ELSE 0
                      END,
    ssc.UpdatedAt   = GETDATE()
FROM [dbo].[StaffShiftCapacity] ssc
JOIN [dbo].[WorkSchedules]  ws ON ws.ScheduleID      = ssc.ScheduleID
JOIN [dbo].[ShiftTemplates] st ON st.ShiftTemplateID = ws.ShiftTemplateID;
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 34 – ORDER ASSIGNMENTS & QUEUE
══════════════════════════════════════════════════════════════ */
PRINT N'[34] OrderAssignments, OrderQueue...';
DECLARE @schedStf INT=(SELECT TOP 1 ScheduleID FROM WorkSchedules
    WHERE AccountID=(SELECT TOP 1 AccountID FROM Accounts WHERE Email='nhung.st@toyhouse.vn')
      AND Status='Completed' ORDER BY WorkDate ASC);
DECLARE @schedMrc INT=(SELECT TOP 1 ScheduleID FROM WorkSchedules
    WHERE AccountID=(SELECT TOP 1 AccountID FROM Accounts WHERE Email='bao.kho@toyhouse.vn')
      AND Status='Completed' ORDER BY WorkDate ASC);
DECLARE @admAsn INT=(SELECT TOP 1 AccountID FROM Accounts WHERE Email='admin@toyhouse.vn');
DECLARE @stfAsn INT=(SELECT TOP 1 AccountID FROM Accounts WHERE Email='nhung.st@toyhouse.vn');
DECLARE @mrcAsn INT=(SELECT TOP 1 AccountID FROM Accounts WHERE Email='bao.kho@toyhouse.vn');

INSERT INTO [dbo].[OrderAssignments]
    (OrderID,ScheduleID,AccountID,RoleID,IsActive,AssignedBy,Notes,AssignedAt)
SELECT o.OrderID,@schedStf,@stfAsn,3,CASE WHEN o.StatusID=8 THEN 0 ELSE 1 END,
       @admAsn,N'Phân ca tự động – Staff',DATEADD(HOUR,2,o.OrderDate)
FROM Orders o
WHERE @schedStf IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM OrderAssignments oa WHERE oa.OrderID=o.OrderID AND oa.RoleID=3);

INSERT INTO [dbo].[OrderAssignments]
    (OrderID,ScheduleID,AccountID,RoleID,IsActive,AssignedBy,Notes,AssignedAt)
SELECT o.OrderID,@schedMrc,@mrcAsn,4,CASE WHEN o.StatusID=8 THEN 0 ELSE 1 END,
       @admAsn,N'Phân ca tự động – Merchandise',DATEADD(HOUR,3,o.OrderDate)
FROM Orders o
WHERE @schedMrc IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM OrderAssignments oa WHERE oa.OrderID=o.OrderID AND oa.RoleID=4);

IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderQueue])
    INSERT INTO [dbo].[OrderQueue] (OrderID,QueuedAt,Reason,IsResolved)
    SELECT TOP 1 o.OrderID,GETDATE(),'NO_STAFF_ON_DUTY',0
    FROM Orders o WHERE o.StatusID=1 ORDER BY o.OrderDate ASC;
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 35 – NOTIFICATION TEMPLATES, CAMPAIGNS, DELIVERIES
══════════════════════════════════════════════════════════════ */
PRINT N'[35] Notification Templates, Campaigns, Deliveries...';
BEGIN TRY
    BEGIN TRAN;
    DECLARE @Tpl TABLE (
        TemplateCode    VARCHAR(50)   NOT NULL PRIMARY KEY,
        UsageScope      VARCHAR(10)   NOT NULL,
        TitleTemplate   NVARCHAR(255) NOT NULL,
        MessageTemplate NVARCHAR(500) NOT NULL
    );
    INSERT INTO @Tpl VALUES
    -- Orders (Customer)
    ('ORDER_PLACED',             'SYSTEM', N'Order Placed Successfully',                    N'Order {{OrderCode}} ({{TotalAmount}} VND) has been recorded.'),
    ('ORDER_CONFIRMED',          'SYSTEM', N'Order {{OrderCode}} Confirmed',                N'The shop has confirmed and is preparing your items.'),
    ('ORDER_PACKING',            'SYSTEM', N'Order {{OrderCode}} Is Being Packed',          N'Your order {{OrderCode}} is currently being carefully packed by our staff.'),
    ('ORDER_SHIPPING',           'SYSTEM', N'Your Order Is On Its Way',                    N'Order {{OrderCode}} has been handed off to shipper {{ShipperName}}. Please keep your phone nearby.'),
    ('ORDER_DELIVERED',          'SYSTEM', N'Order Delivered Successfully',                 N'Order {{OrderCode}} has been delivered. Don''t forget to leave a review!'),
    ('ORDER_CANCELLED',          'SYSTEM', N'Order Cancelled',                             N'Order {{OrderCode}} has been cancelled. Reason: {{CancelReason}}.'),
    ('ORDER_DELIVERY_FAILED',    'SYSTEM', N'Delivery Failed',                             N'Delivery for order {{OrderCode}} was unsuccessful. Please contact customer support.'),
    ('ORDER_ASSIGNED',           'SYSTEM', N'Order Assigned: {{OrderCode}}',               N'Order {{OrderCode}} from {{CustomerName}} has been assigned to you. Total: {{TotalAmount}}. Please process it during your current shift.'),
    
    -- Payment & Wallet
    ('PAYMENT_SUCCESS',          'SYSTEM', N'Payment Successful',                          N'You have paid {{Amount}} VND for order {{OrderCode}}.'),
    ('PAYMENT_FAILED',           'SYSTEM', N'Payment Failed',                              N'Payment for order {{OrderCode}} was unsuccessful.'),
    ('WALLET_TOPUP',             'SYSTEM', N'Wallet Top-Up Successful',                    N'Your wallet has been topped up with {{Amount}}. Balance: {{Balance}}.'),
    ('WALLET_REFUND',            'SYSTEM', N'Refund to Wallet',                            N'{{Amount}} VND from order {{OrderCode}} has been refunded to your wallet.'),
    ('REFUND_APPROVED',          'SYSTEM', N'Refund Request Approved',                     N'Your refund request for order {{OrderCode}} has been approved. {{Amount}} VND will be credited to your wallet.'),
    ('REFUND_REJECTED',          'SYSTEM', N'Refund Request Rejected',                     N'Your refund request for order {{OrderCode}} has been rejected. Please contact customer support if you need assistance.'),
    ('REFUND_COMPLETED',         'SYSTEM', N'Refund Completed',                            N'{{Amount}} VND from order {{OrderCode}} has been successfully refunded to your wallet.'),
    
    -- Products & Inventory
    ('PRODUCT_BACK_IN_STOCK',    'SYSTEM', N'{{ProductName}} Is Back in Stock',            N'Good news! {{ProductName}} is now back in stock at {{Price}}. Shop before it runs out!'),
    ('WISHLIST_PRICE_DROP',      'SYSTEM', N'Price Drop on {{ProductName}}',               N'{{ProductName}} in your wishlist is now on sale for only {{Price}}.'),
    
    -- Reviews & Blog
    ('REVIEW_STAFF_REPLIED',     'SYSTEM', N'Reply to Your Review on {{ProductName}}',     N'A staff member just replied to your review.'),
    ('BLOG_COMMENT_REPLIED',     'SYSTEM', N'Reply to Your Comment on {{BlogTitle}}',      N'Your comment on the post {{BlogTitle}} has received a new reply.'),
    
    -- Staff Notifications
    ('STAFF_NEW_ORDER',          'SYSTEM', N'New Order: {{OrderCode}}',                    N'Order {{OrderCode}} ({{TotalAmount}} VND) needs to be processed.'),
    ('STAFF_CANCEL_REQUEST',     'SYSTEM', N'Cancellation Request for Order {{OrderCode}}',N'Customer {{CustomerName}} has submitted a cancellation request for order {{OrderCode}}. Reason: {{Reason}}.'),
    ('STAFF_REFUND_REQUEST',     'SYSTEM', N'Refund Request for Order {{OrderCode}}',      N'Customer {{CustomerName}} has requested a refund for order {{OrderCode}}.'),
    ('STAFF_REVIEW_MODERATION',  'SYSTEM', N'New Review Pending Moderation',               N'A new {{Rating}}-star review for {{ProductName}} is awaiting your moderation.'),
    ('STAFF_LOW_RATING',         'SYSTEM', N'Low Rating Alert',                            N'{{ProductName}} has just received a {{Rating}}-star review. Please check and take action.'),
    ('STAFF_SHIFT_STARTED',      'SYSTEM', N'Shift {{ShiftName}} Has Started',             N'Your shift {{ShiftName}} has started. Have a productive shift!'),
    
    -- Merchandise Notifications
    ('MERCH_READY_TO_PACK',      'SYSTEM', N'Order Ready for Packing',                     N'Order {{OrderCode}} is ready to be packed.'),
    ('MERCH_PICKED_UP',          'SYSTEM', N'Parcel Picked Up by Shipper',                 N'The shipper has successfully picked up the parcel for order {{OrderCode}}.'),
    ('MERCH_RETURNED',           'SYSTEM', N'Return Received at Warehouse',                N'Order {{OrderCode}} has been returned to the warehouse.'),
    ('MERCH_LOW_STOCK',          'SYSTEM', N'Low Stock Warning',                           N'Only {{Quantity}} units of {{ProductName}} remaining.'),
    ('MERCH_OUT_OF_STOCK',       'SYSTEM', N'Out of Stock Alert',                          N'{{ProductName}} is completely out of stock in the warehouse.'),
    
    -- Admin Notifications
    ('ADMIN_PAYMENT_ERROR',      'SYSTEM', N'Payment Gateway Error',                       N'Payment gateway {{GatewayName}} reported an error: {{ErrorMessage}}.'),
    ('ADMIN_JOB_FAILED',         'SYSTEM', N'Background Job Failed',                       N'Background Job {{JobName}} has failed. Please check the logs.'),
    ('ADMIN_OUTBOX_STUCK',       'SYSTEM', N'Outbox Event Stuck',                          N'Outbox event {{EventType}} ({{EventId}}) has reached the retry limit. Please check the logs.'),
    ('ADMIN_SHIPPING_ERROR',     'SYSTEM', N'Shipping Sync Error',                         N'Error syncing shipping status for order {{OrderCode}}: {{ErrorMessage}}.'),
    ('ADMIN_DAMAGE_LOST',        'SYSTEM', N'Damaged or Lost Shipment',                    N'Order {{OrderCode}} has been reported as damaged or lost during delivery.'),
    ('ADMIN_BLOG_PENDING',       'SYSTEM', N'Blog Post Pending Approval: {{BlogTitle}}',   N'The post {{BlogTitle}} has been submitted and is awaiting your approval.'),
    ('ADMIN_ORDER_QUEUED',       'SYSTEM', N'Order {{OrderCode}} Awaiting Assignment',     N'Order {{OrderCode}} has not been assigned due to: {{Reason}}. Please handle manually.'),
    ('ADMIN_SHIFT_ENDED_PENDING','SYSTEM', N'Shift {{ShiftName}} Ended with Pending Orders',N'Shift {{ShiftName}} (Staff #{{AccountId}}) has ended with {{CurrentLoad}} orders still in progress.'),
    
    -- Admin / Marketing
    ('FLASH_SALE_STARTED',       'ADMIN',  N'⚡ {{PromotionName}} Has Started!',           N'Flash sale runs from {{StartDate}} to {{EndDate}}. Shop now!'),
    ('VOUCHER_NEW',              'ADMIN',  N'🎁 You''ve Received Voucher {{VoucherCode}}', N'Code {{VoucherCode}} gives {{DiscountValue}} off ({{DiscountType}}). Expires: {{ExpiryDate}}!'),
    ('VOUCHER_EXPIRING',         'ADMIN',  N'⏰ Voucher {{VoucherCode}} Is Expiring Soon!',N'Don''t miss out on code {{VoucherCode}} ({{DiscountValue}} off). It expires on {{ExpiryDate}}. Use it now!'),
    ('BIRTHDAY_CUSTOMER',        'SYSTEM', N'🎂 Happy Birthday, {{CustomerName}}!',        N'ToyHouse has a special gift just for you!'),
    ('BIRTHDAY_CHILD',           'SYSTEM', N'🎂 Happy Birthday, {{ChildName}}!',           N'ToyStore wishes {{ChildName}} health and joy. Pick out their favorite toy today!'),
    
    ('BLOG_CAMPAIGN_NEW_POST',     'ADMIN', N'📝 New Post: {{BlogTitle}}',                 N'A new blog post "{{BlogTitle}}" has just been published. Read it now and share your thoughts!'),
    ('BLOG_CAMPAIGN_FEATURED',     'ADMIN', N'⭐ Featured This Week: {{BlogTitle}}',        N'"{{BlogTitle}}" has been selected as our featured post this week. Don''t miss it!'),
    ('BLOG_CAMPAIGN_WEEKLY_DIGEST','ADMIN', N'📰 ToyHouse Weekly Digest',                  N'This week''s highlights are now available. Tap to explore!'),
    ('BLOG_CAMPAIGN_TOPIC_ALERT',  'ADMIN', N'🔔 New Posts in "{{CategoryName}}"',         N'There are new posts in the "{{CategoryName}}" category you follow. Check them out!');

    -- Insert only templates that do not already exist
    INSERT INTO [Notification].[Templates]
        ([TemplateCode],[UsageScope],[TitleTemplate],[MessageTemplate],[IsActive],[IsDeleted],[CreatedAt])
    SELECT t.TemplateCode, t.UsageScope, t.TitleTemplate, t.MessageTemplate, 1, 0, GETDATE()
    FROM @Tpl t
    WHERE NOT EXISTS (
        SELECT 1 FROM [Notification].[Templates] db
        WHERE db.TemplateCode = t.TemplateCode
    );

    -- Re-activate any templates that were soft-deactivated and Update Message if they exist
    UPDATE db
    SET 
        db.IsActive = 1, 
        db.IsDeleted = 0,
        db.TitleTemplate = t.TitleTemplate,
        db.MessageTemplate = t.MessageTemplate,
        db.UpdatedAt = GETDATE()
    FROM [Notification].[Templates] db
    JOIN @Tpl t ON t.TemplateCode = db.TemplateCode;

    COMMIT TRAN;
    PRINT N'✅ Notification templates: OK';
END TRY
BEGIN CATCH
    ROLLBACK TRAN;
    PRINT N'❌ Error: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO


-- ============================================================
-- Campaigns
-- ============================================================
DECLARE @admCamp INT = (SELECT TOP 1 AccountID FROM Accounts WHERE Email = 'admin@toyhouse.vn');

IF NOT EXISTS (SELECT 1 FROM [Notification].[Campaigns])
    INSERT INTO [Notification].[Campaigns]
        (CampaignName, TemplateCode, SourceType, TargetType, Status,
         CreatedByAccountID, IsDeleted, CreatedAt)
    VALUES
    (N'Summer Sale 2026 Announcement',     'FLASH_SALE_STARTED', 'ADMIN',  'ALL',        'Sent', @admCamp, 0, '2026-03-28 08:00:00'),
    (N'Welcome Voucher for New Customers', 'VOUCHER_NEW',        'ADMIN',  'ALL',        'Sent', @admCamp, 0, '2026-01-01 08:00:00'),
    (N'Post-Delivery Review Reminder',     'ORDER_DELIVERED',    'SYSTEM', 'INDIVIDUAL', 'Sent', NULL,     0, '2026-04-01 00:00:00');
GO

-- ============================================================
-- Campaign Stats
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM [Notification].[CampaignStats])
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
DECLARE @cmpSale INT = (SELECT TOP 1 CampaignID FROM [Notification].[Campaigns] WHERE CampaignName = N'Summer Sale 2026 Announcement');
DECLARE @cmpRv   INT = (SELECT TOP 1 CampaignID FROM [Notification].[Campaigns] WHERE CampaignName = N'Post-Delivery Review Reminder');

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
    SELECT 1 FROM [Notification].[Deliveries] d
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
    N'Review Your Products and Earn Reward Points!',
    N'Order ' + o.OrderCode + N' has been successfully delivered. Leave a review now to earn 50 coins!',
    '{"orderId":' + CAST(o.OrderID AS VARCHAR) + ',"orderCode":"' + o.OrderCode + '"}',
    'Unread',
    'DLV-RV-' + o.OrderCode,
    DATEADD(DAY, 1, o.DeliveredAt)
FROM Orders o
WHERE o.StatusID IN (6, 7)
  AND o.DeliveredAt IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM [Notification].[Deliveries] d
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
/* ══════════════════════════════════════════════════════════════
   SECTION 36 – REVIEW MODERATION LOGS  [NEW]
   Ghi log kết quả kiểm duyệt tự động (AI) và thủ công (Staff)
   cho cả text review và ảnh review đã được Approve.
══════════════════════════════════════════════════════════════ */
PRINT N'[36-NEW] ReviewModerationLogs...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[ReviewModerationLogs])
BEGIN
    /* AI auto-approve cho tất cả text review */
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

    /* AI auto-approve cho tất cả ảnh review */
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

    /* Staff override: review của hung (rating 4) được nhân viên xem lại và confirm */
    DECLARE @stfMod INT = (SELECT TOP 1 AccountID FROM Accounts WHERE Email='nhung.st@toyhouse.vn');
    DECLARE @rvHung INT = (SELECT TOP 1 rp.ReviewID
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
         N'Review cần xem lại do đề cập thương hiệu đối thủ – đã xác nhận nội dung hợp lệ',
         DATEADD(HOUR, 1, (SELECT CreatedAt FROM ReviewProducts WHERE ReviewID = @rvHung)));
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 37 – CAMPAIGN APPROVAL LOGS  [NEW]
   Lịch sử duyệt chiến dịch thông báo: Submitted → Approved
══════════════════════════════════════════════════════════════ */
PRINT N'[37-NEW] CampaignApprovalLogs...';
IF NOT EXISTS (SELECT 1 FROM [Notification].[CampaignApprovalLogs])
BEGIN
    DECLARE @admAL  INT = (SELECT TOP 1 AccountID FROM Accounts WHERE Email='admin@toyhouse.vn');
    DECLARE @stfAL  INT = (SELECT TOP 1 AccountID FROM Accounts WHERE Email='nhung.st@toyhouse.vn');
    DECLARE @cmpS INT = (SELECT TOP 1 CampaignID FROM [Notification].[Campaigns] 
						 WHERE CampaignName=N'Summer Sale 2026 Announcement');
	DECLARE @cmpV INT = (SELECT TOP 1 CampaignID FROM [Notification].[Campaigns] 
						 WHERE CampaignName=N'Welcome Voucher for New Customers');
	DECLARE @cmpR INT = (SELECT TOP 1 CampaignID FROM [Notification].[Campaigns] 
						 WHERE CampaignName=N'Post-Delivery Review Reminder');

    /* Campaign Sale Hè: Staff submit → Admin approve */
    INSERT INTO [Notification].[CampaignApprovalLogs] (CampaignID,Action,ActorID,Note,CreatedAt) VALUES
    (@cmpS,'Submitted',@stfAL, N'Đề xuất chiến dịch Sale Hè 2026 để thông báo tới toàn bộ khách hàng','2026-03-27 14:00:00'),
    (@cmpS,'Approved', @admAL, N'Nội dung phù hợp, lịch hợp lý – phê duyệt','2026-03-27 16:30:00'),

    /* Campaign Voucher: Staff submit → Admin approve */
    (@cmpV,'Submitted',@stfAL, N'Chiến dịch gửi voucher chào mừng cho khách mới đăng ký','2025-12-30 09:00:00'),
    (@cmpV,'Approved', @admAL, N'Phê duyệt – ngân sách voucher đã được xác nhận','2025-12-30 11:00:00'),

    /* Campaign Nhắc đánh giá: hệ thống tự tạo, không cần duyệt */
    (@cmpR,'Approved', @admAL, N'System campaign – tự động phê duyệt','2026-04-01 00:00:00');
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 39 – DELIVERY ACTIONS  [NEW]
   Hành động đọc/click thông báo của người dùng
══════════════════════════════════════════════════════════════ */
PRINT N'[39-NEW] DeliveryActions...';
IF NOT EXISTS (SELECT 1 FROM [Notification].[DeliveryActions])
BEGIN
    /* Ghi log Read cho các delivery đã được đánh dấu Read */
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

    /* Click vào một số notification thông báo đơn hàng Completed */
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
          SELECT 1 FROM [Notification].[DeliveryActions] da
          WHERE da.DeliveryID = d.DeliveryID AND da.ActionType = 'Click');
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 43 – RECOMMENDATION WIDGETS
══════════════════════════════════════════════════════════════ */
PRINT N'[43] Recommendation.Widgets...';
IF NOT EXISTS (SELECT 1 FROM [Recommendation].[Widgets])
    INSERT INTO [Recommendation].[Widgets]
        ([WidgetCode], [WidgetName], [Algorithm], [MaxItems], [FallbackAlgo], [IsActive])
    VALUES
        ('homepage_trending',  N'Xu hướng hôm nay',         'trending',       10, 'popular', 1),
        ('pdp_similar',        N'Sản phẩm tương tự',        'content_based',  8,  'popular', 1),
        ('pdp_also_bought',    N'Khách hàng cũng mua',      'collaborative',  8,  'trending', 1),
        ('after_purchase',     N'Mua tiếp theo',            'collaborative',  8,  'trending', 1),
        ('homepage_personal',  N'Gợi ý dành riêng cho bạn', 'weighted_score', 12, 'trending', 1);
GO


/* ══════════════════════════════════════════════════════════════
   SECTION 44 – WALLET PIN ATTEMPTS  [NEW]
   Log các lần nhập PIN ví của khách
══════════════════════════════════════════════════════════════ */
PRINT N'[44-NEW] WalletPinAttempts...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[WalletPinAttempts])
BEGIN
    INSERT INTO [dbo].[WalletPinAttempts]
        (WalletID, AccountID, ActionType, IsSuccess, CreatedAt)
    SELECT w.WalletID, a.AccountID, v.ActionType, v.IsSuccess, v.AtAt
    FROM Accounts a
    JOIN Wallets w ON w.AccountID = a.AccountID
    JOIN (VALUES
        /* lananh: 2 lần thanh toán thành công */
        ('lananh.pham@gmail.com', 'PAYMENT', 1, '2026-02-14 10:29:00'),
        ('lananh.pham@gmail.com', 'PAYMENT', 1, '2026-04-05 08:59:00'),
        /* hung: nhập sai 1 lần, đúng lần 2 */
        ('hung.nguyen88@gmail.com','PAYMENT', 0, '2026-04-01 13:18:00'),
        ('hung.nguyen88@gmail.com','PAYMENT', 1, '2026-04-01 13:19:00'),
        /* thuha: thanh toán thành công */
        ('thuha.hoang@gmail.com', 'PAYMENT', 1, '2026-04-01 13:19:30'),
        ('thuha.hoang@gmail.com', 'VIEW_BALANCE', 1, '2026-04-15 09:00:00'),
        /* mtuando: kiểm tra số dư */
        ('mtuando@outlook.com',   'VIEW_BALANCE', 1, '2026-04-18 08:50:00')
    ) AS v(Email, ActionType, IsSuccess, AtAt) ON a.Email = v.Email
    WHERE a.RoleID = 1;
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 38 – SYSTEM TABLES
══════════════════════════════════════════════════════════════ */
PRINT N'[38] BackgroundJobs, DomainEventOutbox...';
IF NOT EXISTS (SELECT 1 FROM [System].[BackgroundJobs])
    INSERT INTO [System].[BackgroundJobs] (JobName,CronExpression,IsEnabled,LastRunStatus) VALUES
    ('SyncGHNStatus',      '*/5 * * * *',1,'Completed'),
    ('RecalcTrending',     '0 * * * *',  1,'Completed'),
    ('ExpireVouchers',     '0 0 * * *',  1,'Completed'),
    ('ExpirePromotions',   '0 0 * * *',  1,'Completed'),
    ('SendPromoNotif',     '0 8 * * *',  1,'Completed'),
    ('UnblockUsers',       '*/15 * * * *',1,'Completed'),
    ('LowStockAlert',      '0 9 * * *',  1,'Pending'),
    ('BirthdayNotif',      '0 7 * * *',  1,'Pending'),
    ('CleanExpiredSessions','0 1 * * *',  1,'Completed');

INSERT INTO [System].[DomainEventOutbox]
    (EventID,EventType,AggregateType,AggregateId,Payload,OccurredOn)
SELECT NEWID(),'OrderPlacedEvent','Order',CAST(OrderID AS VARCHAR),
       '{"orderId":'+CAST(OrderID AS VARCHAR)+',"orderCode":"'+OrderCode+'","amount":'+CAST(TotalAmount AS VARCHAR)+'}',
       OrderDate
FROM Orders o
WHERE NOT EXISTS (
    SELECT 1 FROM [System].[DomainEventOutbox] eb
    WHERE eb.AggregateId=CAST(o.OrderID AS VARCHAR) AND eb.EventType='OrderPlacedEvent');

INSERT INTO [System].[DomainEventOutbox]
    (EventID,EventType,AggregateType,AggregateId,Payload,OccurredOn)
SELECT NEWID(),'OrderCompletedEvent','Order',CAST(OrderID AS VARCHAR),
       '{"orderId":'+CAST(OrderID AS VARCHAR)+',"orderCode":"'+OrderCode+'"}',
       CompletedAt
FROM Orders o WHERE o.StatusID=7 AND o.CompletedAt IS NOT NULL
  AND NOT EXISTS (
    SELECT 1 FROM [System].[DomainEventOutbox] eb
    WHERE eb.AggregateId=CAST(o.OrderID AS VARCHAR) AND eb.EventType='OrderCompletedEvent');

INSERT INTO [System].[DomainEventOutbox]
    (EventID,EventType,AggregateType,AggregateId,Payload,OccurredOn)
SELECT NEWID(),'ProductReviewedEvent','Review',CAST(ReviewID AS VARCHAR),
       '{"reviewId":'+CAST(ReviewID AS VARCHAR)+',"productId":'+CAST(ProductID AS VARCHAR)+',"rating":'+CAST(Rating AS VARCHAR)+'}',
       CreatedAt
FROM ReviewProducts
WHERE NOT EXISTS (
    SELECT 1 FROM [System].[DomainEventOutbox] eb
    WHERE eb.AggregateId=CAST(ReviewProducts.ReviewID AS VARCHAR)
      AND eb.EventType='ProductReviewedEvent');
GO
