/* =================================================================
   DataSeed FINAL – SEP490_ToyStore v3.2
   Áp dụng trên schema v3.1 (đã tạo sẵn database + schema)
   Chạy 1 lần duy nhất, idempotent (IF NOT EXISTS guard mọi nơi)
================================================================= */

USE [SEP490_ToyStore];
GO
SET NOCOUNT ON;
GO

DECLARE @cname NVARCHAR(200);
SELECT @cname = kc.name
FROM   sys.key_constraints  kc
JOIN   sys.index_columns     ic ON ic.object_id = kc.parent_object_id
                                AND ic.index_id  = kc.unique_index_id
JOIN   sys.columns            c ON  c.object_id  = ic.object_id
                                AND  c.column_id  = ic.column_id
WHERE  kc.parent_object_id = OBJECT_ID('dbo.Orders')
  AND  kc.type = 'UQ'
  AND   c.name = 'PaymentCode';

IF @cname IS NOT NULL
    EXEC('ALTER TABLE [dbo].[Orders] DROP CONSTRAINT [' + @cname + ']');

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE  object_id = OBJECT_ID('dbo.Orders')
      AND  name      = 'UQ_Orders_PaymentCode')
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_Orders_PaymentCode]
    ON [dbo].[Orders] ([PaymentCode])
    WHERE [PaymentCode] IS NOT NULL;
GO


/* ══════════════════════════════════════════
   1. ROLES
══════════════════════════════════════════ */
SET IDENTITY_INSERT [dbo].[Roles] ON;
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE RoleID = 1)
    INSERT INTO [dbo].[Roles] (RoleID, RoleName, Description, CreatedAt) VALUES
    (1, 'Customer',    N'Khách hàng',         '2024-01-05 08:00:00'),
    (2, 'Admin',       N'Quản trị viên',      '2024-01-05 08:00:00'),
    (3, 'Staff',       N'Nhân viên bán hàng', '2024-01-05 08:00:00'),
    (4, 'Mechandise',  N'Nhân viên kho',      '2024-01-05 08:00:00');
SET IDENTITY_INSERT [dbo].[Roles] OFF;
GO


/* ══════════════════════════════════════════
   2. ACCOUNTS – nhân viên (3 người)
══════════════════════════════════════════ */
IF NOT EXISTS (SELECT 1 FROM [dbo].[Accounts] WHERE Email = 'admin@toyhouse.vn')
    INSERT INTO [dbo].[Accounts]
        (RoleID, EmployeeCode, AccountName, PhoneNumber, Email, PasswordHash, IsActive, CreatedAt)
    VALUES
    (2,'AD001',N'Nguyễn Minh Khôi',    '0901000001','admin@toyhouse.vn',   '$2a$11$hashedpassword000000000000000000000000000000000000000000',1,'2024-01-05 08:00:00'),
    (3,'ST001',N'Trần Thị Hồng Nhung', '0901000002','nhung.st@toyhouse.vn','$2a$11$hashedpassword000000000000000000000000000000000000000000',1,'2024-01-05 08:00:00'),
    (4,'MC001',N'Lê Quốc Bảo',         '0901000003','bao.kho@toyhouse.vn', '$2a$11$hashedpassword000000000000000000000000000000000000000000',1,'2024-01-05 08:00:00');
GO


/* ══════════════════════════════════════════
   3. ACCOUNTS – khách hàng (20 người)
   Trigger TR_Accounts_InitPreferences tự chèn UserPreferences
══════════════════════════════════════════ */
IF NOT EXISTS (SELECT 1 FROM [dbo].[Accounts] WHERE Email = 'lananh.pham@gmail.com')
    INSERT INTO [dbo].[Accounts]
        (RoleID, AccountName, PhoneNumber, Email, PasswordHash, IsActive, CreatedAt)
    VALUES
    (1,N'Phạm Thị Lan Anh',    '0912001001','lananh.pham@gmail.com',    '$2a$11$hashedpassword000000000000000000000000000000000000000000',1,'2024-03-12 10:15:00'),
    (1,N'Nguyễn Văn Hùng',     '0912001002','hung.nguyen88@gmail.com',  '$2a$11$hashedpassword000000000000000000000000000000000000000000',1,'2024-04-02 14:30:00'),
    (1,N'Vũ Thị Bảo Châu',     '0912001003','bauchau.vu@gmail.com',     '$2a$11$hashedpassword000000000000000000000000000000000000000000',1,'2024-04-20 09:00:00'),
    (1,N'Đỗ Minh Tuấn',        '0912001004','mtuando@outlook.com',      '$2a$11$hashedpassword000000000000000000000000000000000000000000',1,'2024-05-08 16:45:00'),
    (1,N'Hoàng Thị Thu Hà',    '0912001005','thuha.hoang@gmail.com',    '$2a$11$hashedpassword000000000000000000000000000000000000000000',1,'2024-06-01 11:20:00'),
    (1,N'Lê Văn Phúc',         '0912001006','vanphuc.le@gmail.com',     '$2a$11$hashedpassword000000000000000000000000000000000000000000',1,'2024-07-15 08:30:00'),
    (1,N'Trần Ngọc Diệp',      '0912001007','ndiep.tran@gmail.com',     '$2a$11$hashedpassword000000000000000000000000000000000000000000',1,'2024-08-03 13:00:00'),
    (1,N'Bùi Thị Mỹ Linh',    '0912001008','mylinh.bui2024@gmail.com', '$2a$11$hashedpassword000000000000000000000000000000000000000000',1,'2024-08-22 15:10:00'),
    (1,N'Phan Quốc Khánh',     '0912001009','quockhanh.phan@gmail.com', '$2a$11$hashedpassword000000000000000000000000000000000000000000',1,'2024-09-10 09:45:00'),
    (1,N'Ngô Thị Thanh Tuyền', '0912001010','thanh.tuyen.ngo@gmail.com','$2a$11$hashedpassword000000000000000000000000000000000000000000',1,'2024-09-28 17:00:00'),
    (1,N'Dương Văn Long',      '0912001011','dvlong.toys@gmail.com',    '$2a$11$hashedpassword000000000000000000000000000000000000000000',1,'2024-10-14 10:30:00'),
    (1,N'Trịnh Thị Kim Oanh',  '0912001012','kimoanh.trinh@gmail.com',  '$2a$11$hashedpassword000000000000000000000000000000000000000000',1,'2024-11-01 08:00:00'),
    (1,N'Huỳnh Đức Thịnh',    '0912001013','ducthinh.huynh@gmail.com', '$2a$11$hashedpassword000000000000000000000000000000000000000000',1,'2024-11-20 14:15:00'),
    (1,N'Mai Thị Hồng Vân',   '0912001014','hongvan.mai@gmail.com',    '$2a$11$hashedpassword000000000000000000000000000000000000000000',1,'2024-12-05 09:30:00'),
    (1,N'Đinh Minh Quân',      '0912001015','minhquan.dinh@icloud.com', '$2a$11$hashedpassword000000000000000000000000000000000000000000',1,'2024-12-18 11:00:00'),
    (1,N'Lý Thị Xuân Mai',     '0912001016','xuanmai.ly@gmail.com',     '$2a$11$hashedpassword000000000000000000000000000000000000000000',1,'2025-01-07 10:00:00'),
    (1,N'Châu Văn Tài',        '0912001017','vantai.chau@gmail.com',    '$2a$11$hashedpassword000000000000000000000000000000000000000000',1,'2025-01-25 13:45:00'),
    (1,N'Nguyễn Thị Yến Nhi',  '0912001018','yennhi.ng@gmail.com',      '$2a$11$hashedpassword000000000000000000000000000000000000000000',1,'2025-02-14 08:30:00'),
    (1,N'Võ Thanh Sang',       '0912001019','thanhsang.vo@gmail.com',   '$2a$11$hashedpassword000000000000000000000000000000000000000000',1,'2025-03-03 16:00:00'),
    (1,N'Phùng Thị Bích Trâm', '0912001020','bichtram.phung@gmail.com', '$2a$11$hashedpassword000000000000000000000000000000000000000000',1,'2025-03-20 09:15:00');
GO


/* ══════════════════════════════════════════
   4. LOOKUP TABLES
══════════════════════════════════════════ */

-- SuperCategories
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

-- Categories
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

-- Materials
SET IDENTITY_INSERT [dbo].[Materials] ON;
IF NOT EXISTS (SELECT 1 FROM [dbo].[Materials] WHERE MaterialID = 1)
    INSERT INTO [dbo].[Materials] (MaterialID, MaterialName, CreatedAt) VALUES
    (1,N'Nhựa ABS cao cấp',   '2024-01-05 08:00:00'),
    (2,N'Gỗ tự nhiên MDF',   '2024-01-05 08:00:00'),
    (3,N'Vải cotton & nhung', '2024-01-05 08:00:00'),
    (4,N'Hợp kim nhôm',      '2024-01-05 08:00:00'),
    (5,N'Cao su thiên nhiên', '2024-01-05 08:00:00');
SET IDENTITY_INSERT [dbo].[Materials] OFF;

-- Ages
SET IDENTITY_INSERT [dbo].[Ages] ON;
IF NOT EXISTS (SELECT 1 FROM [dbo].[Ages] WHERE AgeID = 1)
    INSERT INTO [dbo].[Ages] (AgeID, AgeRange, CreatedAt) VALUES
    (1,N'0-1', '2024-01-05 08:00:00'),
    (2,N'1-3', '2024-01-05 08:00:00'),
    (3,N'3-6', '2024-01-05 08:00:00'),
    (4,N'6-12','2024-01-05 08:00:00'),
    (5,N'12+', '2024-01-05 08:00:00');
SET IDENTITY_INSERT [dbo].[Ages] OFF;

-- Sexes
SET IDENTITY_INSERT [dbo].[Sexes] ON;
IF NOT EXISTS (SELECT 1 FROM [dbo].[Sexes] WHERE SexID = 1)
    INSERT INTO [dbo].[Sexes] (SexID, SexName, CreatedAt) VALUES
    (1,N'Nam',  '2024-01-05 08:00:00'),
    (2,N'Nữ',   '2024-01-05 08:00:00'),
    (3,N'Khác', '2024-01-05 08:00:00');
SET IDENTITY_INSERT [dbo].[Sexes] OFF;

-- Origins
SET IDENTITY_INSERT [dbo].[Origins] ON;
IF NOT EXISTS (SELECT 1 FROM [dbo].[Origins] WHERE OriginID = 1)
    INSERT INTO [dbo].[Origins] (OriginID, OriginName, CreatedAt) VALUES
    (1,N'Việt Nam',   '2024-01-05 08:00:00'),
    (2,N'Trung Quốc', '2024-01-05 08:00:00'),
    (3,N'Mỹ',        '2024-01-05 08:00:00'),
    (4,N'Nhật Bản',  '2024-01-05 08:00:00'),
    (5,N'Đan Mạch',  '2024-01-05 08:00:00');
SET IDENTITY_INSERT [dbo].[Origins] OFF;

-- Brands
SET IDENTITY_INSERT [dbo].[Brands] ON;
IF NOT EXISTS (SELECT 1 FROM [dbo].[Brands] WHERE BrandID = 1)
    INSERT INTO [dbo].[Brands] (BrandID, BrandName, CreatedAt) VALUES
    (1,N'Lego',         '2024-01-05 08:00:00'),
    (2,N'Fisher-Price', '2024-01-05 08:00:00'),
    (3,N'Hot Wheels',   '2024-01-05 08:00:00'),
    (4,N'Polo',         '2024-01-05 08:00:00'),
    (5,N'MyKingdom',    '2024-01-05 08:00:00'),
    (6,N'Bandai',       '2024-01-05 08:00:00');
SET IDENTITY_INSERT [dbo].[Brands] OFF;

-- PriceRanges
SET IDENTITY_INSERT [dbo].[PriceRanges] ON;
IF NOT EXISTS (SELECT 1 FROM [dbo].[PriceRanges] WHERE PriceRangeID = 1)
    INSERT INTO [dbo].[PriceRanges] (PriceRangeID, PriceRangeMin, PriceRangeMax, CreatedAt) VALUES
    (1,       0,   199000,'2024-01-05 08:00:00'),
    (2,  200000,   499000,'2024-01-05 08:00:00'),
    (3,  500000,   999000,'2024-01-05 08:00:00'),
    (4, 1000000,  4999000,'2024-01-05 08:00:00'),
    (5, 5000000, 99999000,'2024-01-05 08:00:00');
SET IDENTITY_INSERT [dbo].[PriceRanges] OFF;

-- StatusOrders
SET IDENTITY_INSERT [dbo].[StatusOrders] ON;
IF NOT EXISTS (SELECT 1 FROM [dbo].[StatusOrders] WHERE StatusID = 1)
    INSERT INTO [dbo].[StatusOrders] (StatusID, StatusName) VALUES
    (1,'Pending'),
    (2,'Confirmed'),
    (3,'Processing'),
    (4,'Shipped'),
    (5,'Delivering'),
    (6,'Delivered'),
    (7,'Completed'),
    (8,'Cancelled');
SET IDENTITY_INSERT [dbo].[StatusOrders] OFF;

-- ReactionTypes
SET IDENTITY_INSERT [dbo].[ReactionTypes] ON;
IF NOT EXISTS (SELECT 1 FROM [dbo].[ReactionTypes] WHERE ReactionTypeID = 1)
    INSERT INTO [dbo].[ReactionTypes] (ReactionTypeID, Code, DisplayName, CreatedAt) VALUES
    (1,'like', N'Thích',     '2024-01-05 08:00:00'),
    (2,'love', N'Yêu thích', '2024-01-05 08:00:00'),
    (3,'haha', N'Haha',      '2024-01-05 08:00:00');
SET IDENTITY_INSERT [dbo].[ReactionTypes] OFF;
GO


/* ══════════════════════════════════════════
   5. PROMOTIONS
══════════════════════════════════════════ */
IF NOT EXISTS (SELECT 1 FROM [dbo].[Promotions] WHERE PromotionName = N'Sale Hè Rực Rỡ 2026')
    INSERT INTO [dbo].[Promotions]
        (CreatedBy, PromotionName, PromotionType, Description, StartDate, EndDate, Status, Priority, CreatedAt)
    VALUES
    (
        (SELECT TOP 1 AccountID FROM Accounts WHERE Email='admin@toyhouse.vn'),
        N'Sale Hè Rực Rỡ 2026','DISCOUNT',
        N'Giảm giá hàng loạt đồ chơi chào hè – áp dụng từ 01/04 đến 30/06/2026',
        '2026-04-01','2026-06-30','Active',10,'2026-03-20 09:00:00'
    ),
    (
        (SELECT TOP 1 AccountID FROM Accounts WHERE Email='admin@toyhouse.vn'),
        N'Siêu Flash Sale 5.5 – Chỉ 48 Tiếng','FLASH_SALE',
        N'Giảm sốc lên đến 50% – số lượng có hạn, hết hàng không đặt lại!',
        '2026-05-04','2026-05-06','Scheduled',20,'2026-04-15 10:00:00'
    );
GO


/* ══════════════════════════════════════════
   6. PRODUCTS (15 sản phẩm)
══════════════════════════════════════════ */
SET IDENTITY_INSERT [dbo].[Products] ON;
IF NOT EXISTS (SELECT 1 FROM [dbo].[Products] WHERE ProductID = 1)
    INSERT INTO [dbo].[Products]
        (ProductID, ProductName, Price, Quantity, ProductStatus, CategoryID, BrandID, PriceRangeID, CreatedAt)
    VALUES
    ( 1,N'Lego City Trạm Cảnh Sát Trung Tâm 668 Mảnh',       1290000, 45,'Active', 1,1,4,'2024-02-01 08:00:00'),
    ( 2,N'Bộ Xếp Hình Gỗ Phương Tiện Giao Thông 36 Khối',     420000,180,'Active', 2,5,2,'2024-02-05 08:00:00'),
    ( 3,N'Thẻ Học Thông Minh 4D Động Vật Hoang Dã – 120 Thẻ', 145000,600,'Active', 3,2,1,'2024-02-10 08:00:00'),
    ( 4,N'Kính Hiển Vi Đồ Chơi ScienceMax 40–400x',            375000, 75,'Active', 4,3,2,'2024-03-01 08:00:00'),
    ( 5,N'Xe Chòi Chân Hình Vịt Donald Có Đèn Nhạc',           545000, 38,'Active', 5,2,3,'2024-03-10 08:00:00'),
    ( 6,N'Bóng Cao Su Hoa Văn Boho Size 5 – Chống Bơm Vỡ',      85000,320,'Active', 6,2,1,'2024-03-15 08:00:00'),
    ( 7,N'Diều Hình Đại Bàng Cánh Lớn 1.4m – Kèm Dây 30m',    115000,140,'Active', 7,2,1,'2024-03-20 08:00:00'),
    ( 8,N'Gấu Bông Khủng Long Rex Xanh Lá 80cm Siêu Mềm',      840000, 28,'Active', 8,5,3,'2024-04-01 08:00:00'),
    ( 9,N'Búp Bê Barbie Dreamtopia Tiên Cá – Kèm 3 Bộ Váy',    359000,110,'Active', 9,2,2,'2024-04-05 08:00:00'),
    (10,N'Mô Hình Siêu Nhân Gao Red Ranger Khớp Xoay 24 Điểm', 595000, 22,'Active',10,6,3,'2024-04-10 08:00:00'),
    (11,N'Mô Hình Khủng Long T-Rex Cơ Bắp – Tỉ Lệ 1:10',       395000, 88,'Active',11,3,2,'2024-04-15 08:00:00'),
    (12,N'Siêu Xe Địa Hình RC Traxxas TRX-Mini 4x4 Turbo',      945000, 55,'Active',12,3,3,'2024-05-01 08:00:00'),
    (13,N'Trực Thăng Mini Gyro RC 2.4GHz Chống Va Chạm',       1490000, 12,'Active',13,6,4,'2024-05-10 08:00:00'),
    (14,N'Đất Nặn PlayDoh 24 Màu An Toàn Không Độc Hại',         88000,980,'Active', 2,2,1,'2024-05-15 08:00:00'),
    (15,N'Bảng Vẽ Ma Thuật Tự Xóa LCD 10 Inch – Kèm Bút',      175000,380,'Active', 3,2,1,'2024-05-20 08:00:00');
SET IDENTITY_INSERT [dbo].[Products] OFF;
GO


/* ══════════════════════════════════════════
   7. PRODUCT DETAILS
══════════════════════════════════════════ */
IF NOT EXISTS (SELECT 1 FROM [dbo].[ProductDetails] WHERE ProductID = 1)
    INSERT INTO [dbo].[ProductDetails] (ProductID, Description, MaterialID, AgeID, SexID, OriginID)
    VALUES
    ( 1,N'Bộ Lego City 668 mảnh tái hiện trạm cảnh sát 3 tầng với 1 trực thăng, 4 xe tuần tra và 6 minifigure. Nhựa ABS EN71. Phù hợp trẻ từ 6 tuổi.',1,4,1,5),
    ( 2,N'36 khối gỗ MDF sơn nước an toàn, khắc nổi 18 phương tiện giao thông. Bo góc 5mm. Phù hợp 1–5 tuổi.',2,3,3,1),
    ( 3,N'120 thẻ 4D tích hợp AR – quét app xem động vật sống động. 60 loài + 60 thẻ song ngữ Việt–Anh. Bìa cứng 350g, in UV.',3,3,3,2),
    ( 4,N'Kính hiển vi ScienceMax 3 mức zoom (40x/100x/400x), đèn LED, 12 tiêu bản, 5 lam kính, cẩm nang 20 thí nghiệm.',1,4,3,3),
    ( 5,N'Xe chòi chân hình vịt Donald: đèn LED + nhạc, khung sắt sơn tĩnh điện, bánh cao su đặc. Tải tối đa 30kg, điều chỉnh 3 mức chiều cao yên.',5,3,3,2),
    ( 6,N'Bóng cao su thiên nhiên size 5 (Ø21cm) họa tiết Boho. Lưu hóa 2 lớp, khử mùi, an toàn tiếp xúc da.',5,3,3,1),
    ( 7,N'Diều đại bàng sải cánh 1.4m, khung carbon, vải polyester 210T. Bay ổn định gió 3–7 Beaufort. Kèm dây dù 30m + túi đựng.',2,4,1,1),
    ( 8,N'Gấu bông khủng long Rex 80cm vải nhung siêu mềm, bông PP chống nấm. Mắt nhựa khâu chặt, giặt máy được. Kèm hộp quà.',3,2,2,2),
    ( 9,N'Barbie Dreamtopia Tiên Cá chính hãng Mattel. Tóc gradient tím–hồng, 3 bộ trang phục chủ đề đại dương, 12 phụ kiện.',1,3,2,3),
    (10,N'Siêu Nhân Gao Red Ranger Bandai Nhật, cao 18cm, 24 khớp xoay. Kèm 2 kiếm tháo lắp + đế acrylic. Limited edition.',5,5,1,4),
    (11,N'T-Rex tỉ lệ 1:10, cao 25cm dài 45cm, hợp kim nhôm–nhựa ABS. Da sần, miệng lò xo mở đóng. Nặng 680g.',4,4,1,3),
    (12,N'RC Traxxas TRX-Mini 1:16, brushless 2838KV, max 45km/h. Sạc 60 phút, chạy 30 phút. Tần số 2.4GHz.',4,4,1,3),
    (13,N'Trực thăng RC Gyro 4 kênh 2.4GHz, con quay 6 trục. Bay 12–15 phút, sạc USB 40 phút. Từ 12 tuổi.',1,5,1,2),
    (14,N'PlayDoh 24 màu chính hãng Hasbro, mỗi hộp 85g. Không độc, không gluten. Kèm 6 khuôn thú + dụng cụ cắt.',3,3,3,3),
    (15,N'Bảng LCD 10 inch viết–vẽ–xóa tức thì. 1 pin CR2025 dùng 50.000 lần. Khóa xóa an toàn. Kèm bút stylus + dây đeo.',1,2,3,2);
GO


/* ══════════════════════════════════════════
   8. PRODUCT IMAGES (3 ảnh/sản phẩm)
══════════════════════════════════════════ */
IF NOT EXISTS (SELECT 1 FROM [dbo].[ProductImages] WHERE ProductID = 1 AND IsMain = 1)
    INSERT INTO [dbo].[ProductImages] (ProductID, ImageUrl, IsMain, CreatedAt)
    VALUES
    -- Product 1
    ( 1,'https://picsum.photos/seed/lego-city-1/400/400',      1,'2024-02-01 08:00:00'),
    ( 1,'https://picsum.photos/seed/lego-city-2/400/400',      0,'2024-02-01 08:00:00'),
    ( 1,'https://picsum.photos/seed/lego-city-3/400/400',      0,'2024-02-01 08:00:00'),
    -- Product 2
    ( 2,'https://picsum.photos/seed/wooden-block-1/400/400',   1,'2024-02-05 08:00:00'),
    ( 2,'https://picsum.photos/seed/wooden-block-2/400/400',   0,'2024-02-05 08:00:00'),
    ( 2,'https://picsum.photos/seed/wooden-block-3/400/400',   0,'2024-02-05 08:00:00'),
    -- Product 3
    ( 3,'https://picsum.photos/seed/flashcard-1/400/400',      1,'2024-02-10 08:00:00'),
    ( 3,'https://picsum.photos/seed/flashcard-2/400/400',      0,'2024-02-10 08:00:00'),
    ( 3,'https://picsum.photos/seed/flashcard-3/400/400',      0,'2024-02-10 08:00:00'),
    -- Product 4
    ( 4,'https://picsum.photos/seed/microscope-1/400/400',     1,'2024-03-01 08:00:00'),
    ( 4,'https://picsum.photos/seed/microscope-2/400/400',     0,'2024-03-01 08:00:00'),
    ( 4,'https://picsum.photos/seed/microscope-3/400/400',     0,'2024-03-01 08:00:00'),
    -- Product 5
    ( 5,'https://picsum.photos/seed/duck-car-1/400/400',       1,'2024-03-10 08:00:00'),
    ( 5,'https://picsum.photos/seed/duck-car-2/400/400',       0,'2024-03-10 08:00:00'),
    ( 5,'https://picsum.photos/seed/duck-car-3/400/400',       0,'2024-03-10 08:00:00'),
    -- Product 6
    ( 6,'https://picsum.photos/seed/ball-boho-1/400/400',      1,'2024-03-15 08:00:00'),
    ( 6,'https://picsum.photos/seed/ball-boho-2/400/400',      0,'2024-03-15 08:00:00'),
    ( 6,'https://picsum.photos/seed/ball-boho-3/400/400',      0,'2024-03-15 08:00:00'),
    -- Product 7
    ( 7,'https://picsum.photos/seed/eagle-kite-1/400/400',     1,'2024-03-20 08:00:00'),
    ( 7,'https://picsum.photos/seed/eagle-kite-2/400/400',     0,'2024-03-20 08:00:00'),
    ( 7,'https://picsum.photos/seed/eagle-kite-3/400/400',     0,'2024-03-20 08:00:00'),
    -- Product 8
    ( 8,'https://picsum.photos/seed/trex-plush-1/400/400',     1,'2024-04-01 08:00:00'),
    ( 8,'https://picsum.photos/seed/trex-plush-2/400/400',     0,'2024-04-01 08:00:00'),
    ( 8,'https://picsum.photos/seed/trex-plush-3/400/400',     0,'2024-04-01 08:00:00'),
    -- Product 9
    ( 9,'https://picsum.photos/seed/barbie-mermaid-1/400/400', 1,'2024-04-05 08:00:00'),
    ( 9,'https://picsum.photos/seed/barbie-mermaid-2/400/400', 0,'2024-04-05 08:00:00'),
    ( 9,'https://picsum.photos/seed/barbie-mermaid-3/400/400', 0,'2024-04-05 08:00:00'),
    -- Product 10
    (10,'https://picsum.photos/seed/sentai-red-1/400/400',     1,'2024-04-10 08:00:00'),
    (10,'https://picsum.photos/seed/sentai-red-2/400/400',     0,'2024-04-10 08:00:00'),
    (10,'https://picsum.photos/seed/sentai-red-3/400/400',     0,'2024-04-10 08:00:00'),
    -- Product 11
    (11,'https://picsum.photos/seed/trex-metal-1/400/400',     1,'2024-04-15 08:00:00'),
    (11,'https://picsum.photos/seed/trex-metal-2/400/400',     0,'2024-04-15 08:00:00'),
    (11,'https://picsum.photos/seed/trex-metal-3/400/400',     0,'2024-04-15 08:00:00'),
    -- Product 12
    (12,'https://picsum.photos/seed/rc-traxxas-1/400/400',     1,'2024-05-01 08:00:00'),
    (12,'https://picsum.photos/seed/rc-traxxas-2/400/400',     0,'2024-05-01 08:00:00'),
    (12,'https://picsum.photos/seed/rc-traxxas-3/400/400',     0,'2024-05-01 08:00:00'),
    -- Product 13
    (13,'https://picsum.photos/seed/helicopter-rc-1/400/400',  1,'2024-05-10 08:00:00'),
    (13,'https://picsum.photos/seed/helicopter-rc-2/400/400',  0,'2024-05-10 08:00:00'),
    (13,'https://picsum.photos/seed/helicopter-rc-3/400/400',  0,'2024-05-10 08:00:00'),
    -- Product 14
    (14,'https://picsum.photos/seed/playdoh-24-1/400/400',     1,'2024-05-15 08:00:00'),
    (14,'https://picsum.photos/seed/playdoh-24-2/400/400',     0,'2024-05-15 08:00:00'),
    (14,'https://picsum.photos/seed/playdoh-24-3/400/400',     0,'2024-05-15 08:00:00'),
    -- Product 15
    (15,'https://picsum.photos/seed/lcd-board-1/400/400',      1,'2024-05-20 08:00:00'),
    (15,'https://picsum.photos/seed/lcd-board-2/400/400',      0,'2024-05-20 08:00:00'),
    (15,'https://picsum.photos/seed/lcd-board-3/400/400',      0,'2024-05-20 08:00:00');
GO


/* ══════════════════════════════════════════
   9. PRODUCT PROMOTIONS & TIME SLOTS
   - ProductPromotions  → chỉ dùng cho DISCOUNT
   - PromotionTimeSlots → chỉ dùng cho FLASH_SALE
   - PromotionProductSlots → chỉ dùng cho FLASH_SALE
     (gắn sản phẩm và giá/số lượng riêng theo từng slot)
══════════════════════════════════════════ */
IF NOT EXISTS (SELECT 1 FROM [dbo].[ProductPromotions]
               WHERE ProductID=1
                 AND PromotionID=(SELECT TOP 1 PromotionID FROM Promotions
                                  WHERE PromotionName=N'Sale Hè Rực Rỡ 2026'))
BEGIN
    DECLARE @pSale   INT = (SELECT TOP 1 PromotionID FROM Promotions WHERE PromotionName=N'Sale Hè Rực Rỡ 2026');
    DECLARE @pFlash  INT = (SELECT TOP 1 PromotionID FROM Promotions WHERE PromotionName=N'Siêu Flash Sale 5.5 – Chỉ 48 Tiếng');

    /* --- ProductPromotions: chỉ cho DISCOUNT --- */
    INSERT INTO [dbo].[ProductPromotions]
        (ProductID, PromotionID, SalePrice, DiscountPercent, SaleQuantity, SoldQuantity, IsActive, CreatedAt)
    VALUES
    ( 1, @pSale,  1099000, 14.88,  30, 8, 1,'2026-03-20 02:00:00'),  -- UTC (= 09:00 ICT)
    (12, @pSale,   850000, 10.05, NULL, 3, 1,'2026-03-20 02:00:00');

    /* --- PromotionTimeSlots: 2 khung giờ Flash Sale 5.5 (lưu UTC) --- */
    -- Slot 1: 04/05/2026 09:00–12:00 ICT  = 02:00–05:00 UTC
    -- Slot 2: 04/05/2026 20:00–22:00 ICT  = 13:00–15:00 UTC
    INSERT INTO [dbo].[PromotionTimeSlots]
        (PromotionID, StartAt, EndAt, Status, CreatedAt)
    VALUES
    (@pFlash, '2026-05-04T02:00:00', '2026-05-04T05:00:00', 'Scheduled', '2026-04-15T03:00:00'),
    (@pFlash, '2026-05-04T13:00:00', '2026-05-04T15:00:00', 'Scheduled', '2026-04-15T03:00:00');

    /* --- PromotionProductSlots: sản phẩm + giá/sốlượng riêng cho từng slot --- */
    DECLARE @slot1 INT = (SELECT TOP 1 TimeSlotID FROM PromotionTimeSlots
                          WHERE PromotionID=@pFlash AND StartAt='2026-05-04T02:00:00');
    DECLARE @slot2 INT = (SELECT TOP 1 TimeSlotID FROM PromotionTimeSlots
                          WHERE PromotionID=@pFlash AND StartAt='2026-05-04T13:00:00');

    -- Slot 1 (sáng): SP11 (T-Rex) và SP12 (RC Traxxas)
    INSERT INTO [dbo].[PromotionProductSlots]
        (TimeSlotID, ProductID, SalePrice, DiscountPercent, SaleQuantity, SoldQuantity, IsActive, CreatedAt)
    VALUES
    (@slot1, 11, 197000, 50.13, 10, 0, 1, '2026-04-15T03:00:00'),
    (@slot1, 12, 700000, 25.93, 15, 0, 1, '2026-04-15T03:00:00');

    -- Slot 2 (đêm): SP10 (Siêu Nhân) và SP13 (Trực thăng RC)
    INSERT INTO [dbo].[PromotionProductSlots]
        (TimeSlotID, ProductID, SalePrice, DiscountPercent, SaleQuantity, SoldQuantity, IsActive, CreatedAt)
    VALUES
    (@slot2, 10, 297000, 50.08, 20, 0, 1, '2026-04-15T03:00:00'),
    (@slot2, 13, 990000, 33.56, 8,  0, 1, '2026-04-15T03:00:00');
END
GO


/* ══════════════════════════════════════════
   10. VOUCHERS
══════════════════════════════════════════ */
IF NOT EXISTS (SELECT 1 FROM [dbo].[Vouchers] WHERE VoucherCode='SHIP0626')
    INSERT INTO [dbo].[Vouchers]
        (VoucherCode, VoucherName, VoucherDescription, DiscountType, DiscountValue,
         DiscountTarget, MinOrderAmount, TotalQuantity, UsedQuantity, MaxUsagePerUser,
         StartDate, EndDate, Status, IsDeleted, CreatedAt)
    VALUES
    ('SHIP0626',  N'Freeship Tháng 6',                N'Miễn phí vận chuyển tối đa 50.000đ – đơn từ 200.000đ',
     'FIXED',    50000, 'SHIPPING_FEE', 200000, 1000, 0, 1, '2026-06-01','2026-06-30','Active',0,'2026-05-25 08:00:00'),
    ('WELCOME10', N'Giảm 10% Chào Mừng Khách Mới',   N'Dành riêng lần mua đầu tiên – giảm 10% toàn đơn',
     'PERCENTAGE',10,   'ORDER_TOTAL',       0,  500, 0, 1, '2026-01-01','2026-12-31','Active',0,'2026-01-01 08:00:00'),
    ('VIP200K',   N'Ưu Đãi Khách VIP – Giảm 200.000đ',N'Áp dụng đơn từ 1.000.000đ',
     'FIXED',   200000, 'ORDER_TOTAL', 1000000,  100, 0, 1, '2026-04-01','2026-06-30','Active',0,'2026-03-20 09:00:00');
GO


/* ══════════════════════════════════════════
   11. PRODUCT FOLLOWERS (5 khách theo dõi SP sắp hết hàng)
══════════════════════════════════════════ */
IF NOT EXISTS (SELECT 1 FROM [dbo].[ProductFollowers])
    INSERT INTO [dbo].[ProductFollowers] (ProductID, AccountID, CreatedAt)
    SELECT TOP 5
        p.ProductID, a.AccountID, GETDATE()
    FROM Products p
    CROSS JOIN Accounts a
    WHERE a.RoleID = 1 AND p.Quantity < 50
    ORDER BY NEWID();
GO


/* ══════════════════════════════════════════
   12. ĐỊA CHỈ GIAO HÀNG (helper vars cho Orders)
   Lấy WardCode / DistrictId / ProvinceId đầu tiên tồn tại
══════════════════════════════════════════ */
GO
-- Các đơn hàng dùng DECLARE cục bộ trong từng batch nên không cần global vars

/* ══════════════════════════════════════════
   13. ORDERS MẪU (10 đơn, mỗi đơn 1 GO riêng để SCOPE_IDENTITY đúng)
   StatusID mapping:
     1=Pending 2=Confirmed 3=Processing 4=Shipped
     5=Delivering 6=Delivered 7=Completed 8=Cancelled
══════════════════════════════════════════ */

-- ────────────────────────────────────────
-- ĐƠN 1: Phạm Thị Lan Anh – Completed – WALLET
-- ────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM [dbo].[Orders] WHERE OrderCode='ORD-2026-00001')
BEGIN
    DECLARE @w1 VARCHAR(20) = ISNULL((SELECT TOP 1 WardCode   FROM Wards     WHERE IsActive=1 ORDER BY NEWID()),'W01');
    DECLARE @d1 INT         = ISNULL((SELECT TOP 1 DistrictId FROM Districts  WHERE IsActive=1 ORDER BY NEWID()),1);
    DECLARE @p1 INT         = ISNULL((SELECT TOP 1 ProvinceId FROM Provinces  WHERE IsActive=1 ORDER BY NEWID()),1);

    INSERT INTO [dbo].[Orders]
        (AccountID, StatusID, OrderCode,
         ShippingName, ShippingPhone, ShippingAddress,
         ShippingWardCode, ShippingWardName, ShippingDistrictId, ShippingDistrictName,
         ShippingProvinceId, ShippingProvinceName,
         OrderDate, ConfirmedAt, ShippedAt, DeliveredAt, CompletedAt,
         PaymentMethod, PaymentStatus, PaidAt,
         SubTotal, EstimatedShippingFee, TotalAmount, CreatedAt)
    VALUES(
        (SELECT TOP 1 AccountID FROM Accounts WHERE Email='lananh.pham@gmail.com'),
        7,'ORD-2026-00001',
        N'Phạm Thị Lan Anh','0912001001',N'72 Lê Lợi, P. Bến Nghé',
        @w1,N'Phường Bến Nghé',@d1,N'Quận 1',@p1,N'TP. Hồ Chí Minh',
        '2026-02-14 10:30:00','2026-02-14 14:00:00','2026-02-15 09:00:00','2026-02-16 11:00:00','2026-02-17 08:00:00',
        'WALLET','PAID','2026-02-14 10:30:00',
        1290000, 0, 1290000, '2026-02-14 10:30:00');

    DECLARE @o1 INT = SCOPE_IDENTITY();

    INSERT INTO [dbo].[OrderDetails] (OrderID, ProductID, ProductName, Quantity, UnitPrice, CreatedAt)
    VALUES(@o1,1,N'Lego City Trạm Cảnh Sát Trung Tâm 668 Mảnh',1,1290000,'2026-02-14 10:30:00');

    INSERT INTO [dbo].[OrderStatusHistory] (OrderID, StatusID, Note, CreatedAt) VALUES
    (@o1,1,N'Đơn hàng mới',                  '2026-02-14 10:30:00'),
    (@o1,2,N'Shop xác nhận và đóng gói',      '2026-02-14 14:00:00'),
    (@o1,4,N'Bàn giao GHN',                   '2026-02-15 09:00:00'),
    (@o1,6,N'Giao hàng thành công',            '2026-02-16 11:00:00'),
    (@o1,7,N'Khách xác nhận đã nhận hàng',    '2026-02-17 08:00:00');
END
GO

-- ────────────────────────────────────────
-- ĐƠN 2: Nguyễn Văn Hùng – Shipped – SE_PAY
-- ────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM [dbo].[Orders] WHERE OrderCode='ORD-2026-00002')
BEGIN
    DECLARE @w2 VARCHAR(20) = ISNULL((SELECT TOP 1 WardCode   FROM Wards    WHERE IsActive=1 ORDER BY NEWID()),'W01');
    DECLARE @d2 INT         = ISNULL((SELECT TOP 1 DistrictId FROM Districts WHERE IsActive=1 ORDER BY NEWID()),1);
    DECLARE @p2 INT         = ISNULL((SELECT TOP 1 ProvinceId FROM Provinces WHERE IsActive=1 ORDER BY NEWID()),1);

    INSERT INTO [dbo].[Orders]
        (AccountID, StatusID, OrderCode,
         ShippingName, ShippingPhone, ShippingAddress,
         ShippingWardCode, ShippingWardName, ShippingDistrictId, ShippingDistrictName,
         ShippingProvinceId, ShippingProvinceName,
         OrderDate, ConfirmedAt, ShippedAt,
         PaymentMethod, PaymentStatus, PaidAt,
         SubTotal, EstimatedShippingFee, TotalAmount, CreatedAt)
    VALUES(
        (SELECT TOP 1 AccountID FROM Accounts WHERE Email='hung.nguyen88@gmail.com'),
        4,'ORD-2026-00002',
        N'Nguyễn Văn Hùng','0912001002',N'15 Trần Phú, P. Mộ Lao',
        @w2,N'Phường Mộ Lao',@d2,N'Hà Đông',@p2,N'Hà Nội',
        '2026-04-10 08:15:00','2026-04-10 11:00:00','2026-04-11 09:30:00',
        'SE_PAY','PAID','2026-04-10 08:15:00',
        945000, 30000, 975000, '2026-04-10 08:15:00');

    DECLARE @o2 INT = SCOPE_IDENTITY();

    INSERT INTO [dbo].[OrderDetails] (OrderID, ProductID, ProductName, Quantity, UnitPrice, CreatedAt)
    VALUES(@o2,12,N'Siêu Xe Địa Hình RC Traxxas TRX-Mini 4x4 Turbo',1,945000,'2026-04-10 08:15:00');

    INSERT INTO [dbo].[OrderStatusHistory] (OrderID, StatusID, Note, CreatedAt) VALUES
    (@o2,1,N'Đơn hàng mới',                  '2026-04-10 08:15:00'),
    (@o2,2,N'Xác nhận – chờ kho lấy hàng',  '2026-04-10 11:00:00'),
    (@o2,4,N'GHN đã lấy hàng, đang vận chuyển','2026-04-11 09:30:00');
END
GO

-- ────────────────────────────────────────
-- ĐƠN 3: Vũ Thị Bảo Châu – Cancelled – BANK_TRANSFER
-- ────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM [dbo].[Orders] WHERE OrderCode='ORD-2026-00003')
BEGIN
    DECLARE @w3 VARCHAR(20) = ISNULL((SELECT TOP 1 WardCode   FROM Wards    WHERE IsActive=1 ORDER BY NEWID()),'W01');
    DECLARE @d3 INT         = ISNULL((SELECT TOP 1 DistrictId FROM Districts WHERE IsActive=1 ORDER BY NEWID()),1);
    DECLARE @p3 INT         = ISNULL((SELECT TOP 1 ProvinceId FROM Provinces WHERE IsActive=1 ORDER BY NEWID()),1);

    INSERT INTO [dbo].[Orders]
        (AccountID, StatusID, OrderCode,
         ShippingName, ShippingPhone, ShippingAddress,
         ShippingWardCode, ShippingWardName, ShippingDistrictId, ShippingDistrictName,
         ShippingProvinceId, ShippingProvinceName,
         OrderDate, CancelledAt,
         PaymentMethod, PaymentStatus, CancelReason,
         SubTotal, EstimatedShippingFee, TotalAmount, CreatedAt)
    VALUES(
        (SELECT TOP 1 AccountID FROM Accounts WHERE Email='bauchau.vu@gmail.com'),
        8,'ORD-2026-00003',
        N'Vũ Thị Bảo Châu','0912001003',N'88 Nguyễn Trãi, P. Thanh Xuân Trung',
        @w3,N'Phường Thanh Xuân Trung',@d3,N'Thanh Xuân',@p3,N'Hà Nội',
        '2026-03-05 16:45:00','2026-03-06 17:00:00',
        'BANK_TRANSFER','PENDING',N'Khách chưa chuyển khoản sau 24h',
        840000, 25000, 865000, '2026-03-05 16:45:00');

    DECLARE @o3 INT = SCOPE_IDENTITY();

    INSERT INTO [dbo].[OrderDetails] (OrderID, ProductID, ProductName, Quantity, UnitPrice, CreatedAt)
    VALUES(@o3,8,N'Gấu Bông Khủng Long Rex Xanh Lá 80cm Siêu Mềm',1,840000,'2026-03-05 16:45:00');

    INSERT INTO [dbo].[OrderStatusHistory] (OrderID, StatusID, Note, CreatedAt) VALUES
    (@o3,1,N'Đơn hàng mới',                         '2026-03-05 16:45:00'),
    (@o3,8,N'Khách hủy – chưa chuyển khoản sau 24h','2026-03-06 17:00:00');
END
GO

-- ────────────────────────────────────────
-- ĐƠN 4: Đỗ Minh Tuấn – Confirmed – COD (đa sản phẩm)
-- ────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM [dbo].[Orders] WHERE OrderCode='ORD-2026-00004')
BEGIN
    DECLARE @w4 VARCHAR(20) = ISNULL((SELECT TOP 1 WardCode   FROM Wards    WHERE IsActive=1 ORDER BY NEWID()),'W01');
    DECLARE @d4 INT         = ISNULL((SELECT TOP 1 DistrictId FROM Districts WHERE IsActive=1 ORDER BY NEWID()),1);
    DECLARE @p4 INT         = ISNULL((SELECT TOP 1 ProvinceId FROM Provinces WHERE IsActive=1 ORDER BY NEWID()),1);

    INSERT INTO [dbo].[Orders]
        (AccountID, StatusID, OrderCode,
         ShippingName, ShippingPhone, ShippingAddress,
         ShippingWardCode, ShippingWardName, ShippingDistrictId, ShippingDistrictName,
         ShippingProvinceId, ShippingProvinceName,
         OrderDate, ConfirmedAt,
         PaymentMethod, PaymentStatus,
         SubTotal, EstimatedShippingFee, TotalAmount, CreatedAt)
    VALUES(
        (SELECT TOP 1 AccountID FROM Accounts WHERE Email='mtuando@outlook.com'),
        2,'ORD-2026-00004',
        N'Đỗ Minh Tuấn','0912001004',N'34 Đinh Tiên Hoàng, P. Đa Kao',
        @w4,N'Phường Đa Kao',@d4,N'Quận 1',@p4,N'TP. Hồ Chí Minh',
        '2026-04-18 09:00:00','2026-04-18 10:30:00',
        'SHIP_COD','PENDING',
        1734000, 35000, 1769000, '2026-04-18 09:00:00');

    DECLARE @o4 INT = SCOPE_IDENTITY();

    INSERT INTO [dbo].[OrderDetails] (OrderID, ProductID, ProductName, Quantity, UnitPrice, CreatedAt) VALUES
    (@o4, 1,N'Lego City Trạm Cảnh Sát Trung Tâm 668 Mảnh',       1,1290000,'2026-04-18 09:00:00'),
    (@o4,14,N'Đất Nặn PlayDoh 24 Màu An Toàn Không Độc Hại',      2,  88000,'2026-04-18 09:00:00'),
    (@o4,15,N'Bảng Vẽ Ma Thuật Tự Xóa LCD 10 Inch – Kèm Bút',     1, 175000,'2026-04-18 09:00:00'),
    (@o4, 3,N'Thẻ Học Thông Minh 4D Động Vật Hoang Dã – 120 Thẻ', 1, 145000,'2026-04-18 09:00:00');

    INSERT INTO [dbo].[OrderStatusHistory] (OrderID, StatusID, Note, CreatedAt) VALUES
    (@o4,1,N'Đơn hàng mới – khách chọn COD',  '2026-04-18 09:00:00'),
    (@o4,2,N'Staff xác nhận – đang đóng gói', '2026-04-18 10:30:00');
END
GO

-- ────────────────────────────────────────
-- ĐƠN 5: Hoàng Thị Thu Hà – Delivered – WALLET
-- ────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM [dbo].[Orders] WHERE OrderCode='ORD-2026-00005')
BEGIN
    DECLARE @w5 VARCHAR(20) = ISNULL((SELECT TOP 1 WardCode   FROM Wards    WHERE IsActive=1 ORDER BY NEWID()),'W01');
    DECLARE @d5 INT         = ISNULL((SELECT TOP 1 DistrictId FROM Districts WHERE IsActive=1 ORDER BY NEWID()),1);
    DECLARE @p5 INT         = ISNULL((SELECT TOP 1 ProvinceId FROM Provinces WHERE IsActive=1 ORDER BY NEWID()),1);

    INSERT INTO [dbo].[Orders]
        (AccountID, StatusID, OrderCode,
         ShippingName, ShippingPhone, ShippingAddress,
         ShippingWardCode, ShippingWardName, ShippingDistrictId, ShippingDistrictName,
         ShippingProvinceId, ShippingProvinceName,
         OrderDate, ConfirmedAt, ShippedAt, DeliveredAt,
         PaymentMethod, PaymentStatus, PaidAt,
         SubTotal, EstimatedShippingFee, TotalAmount, CreatedAt)
    VALUES(
        (SELECT TOP 1 AccountID FROM Accounts WHERE Email='thuha.hoang@gmail.com'),
        6,'ORD-2026-00005',
        N'Hoàng Thị Thu Hà','0912001005',N'120 Lê Văn Lương, P. Nhân Chính',
        @w5,N'Phường Nhân Chính',@d5,N'Thanh Xuân',@p5,N'Hà Nội',
        '2026-04-01 13:20:00','2026-04-01 15:00:00','2026-04-02 09:00:00','2026-04-03 14:30:00',
        'WALLET','PAID','2026-04-01 13:20:00',
        1199000, 0, 1199000, '2026-04-01 13:20:00');

    DECLARE @o5 INT = SCOPE_IDENTITY();

    INSERT INTO [dbo].[OrderDetails] (OrderID, ProductID, ProductName, Quantity, UnitPrice, CreatedAt) VALUES
    (@o5,9,N'Búp Bê Barbie Dreamtopia Tiên Cá – Kèm 3 Bộ Váy',1,359000,'2026-04-01 13:20:00'),
    (@o5,8,N'Gấu Bông Khủng Long Rex Xanh Lá 80cm Siêu Mềm',   1,840000,'2026-04-01 13:20:00');

    INSERT INTO [dbo].[OrderStatusHistory] (OrderID, StatusID, Note, CreatedAt) VALUES
    (@o5,1,N'Đơn mới',                            '2026-04-01 13:20:00'),
    (@o5,2,N'Đã xác nhận',                        '2026-04-01 15:00:00'),
    (@o5,4,N'Đang giao – GHN MHĐ 20260401HN',     '2026-04-02 09:00:00'),
    (@o5,6,N'Giao thành công – khách ký nhận',    '2026-04-03 14:30:00');
END
GO

-- ────────────────────────────────────────
-- ĐƠN 6: Lê Văn Phúc – Pending – SE_PAY
-- ────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM [dbo].[Orders] WHERE OrderCode='ORD-2026-00006')
BEGIN
    DECLARE @w6 VARCHAR(20) = ISNULL((SELECT TOP 1 WardCode   FROM Wards    WHERE IsActive=1 ORDER BY NEWID()),'W01');
    DECLARE @d6 INT         = ISNULL((SELECT TOP 1 DistrictId FROM Districts WHERE IsActive=1 ORDER BY NEWID()),1);
    DECLARE @p6 INT         = ISNULL((SELECT TOP 1 ProvinceId FROM Provinces WHERE IsActive=1 ORDER BY NEWID()),1);

    INSERT INTO [dbo].[Orders]
        (AccountID, StatusID, OrderCode,
         ShippingName, ShippingPhone, ShippingAddress,
         ShippingWardCode, ShippingWardName, ShippingDistrictId, ShippingDistrictName,
         ShippingProvinceId, ShippingProvinceName,
         OrderDate,
         PaymentMethod, PaymentStatus, PaidAt,
         SubTotal, EstimatedShippingFee, TotalAmount, CreatedAt)
    VALUES(
        (SELECT TOP 1 AccountID FROM Accounts WHERE Email='vanphuc.le@gmail.com'),
        1,'ORD-2026-00006',
        N'Lê Văn Phúc','0912001006',N'9 Đinh Lễ, P. Hoàn Kiếm',
        @w6,N'Phường Hoàn Kiếm',@d6,N'Hoàn Kiếm',@p6,N'Hà Nội',
        '2026-04-22 11:00:00',
        'SE_PAY','PAID','2026-04-22 11:00:00',
        1490000, 0, 1490000, '2026-04-22 11:00:00');

    DECLARE @o6 INT = SCOPE_IDENTITY();

    INSERT INTO [dbo].[OrderDetails] (OrderID, ProductID, ProductName, Quantity, UnitPrice, CreatedAt)
    VALUES(@o6,13,N'Trực Thăng Mini Gyro RC 2.4GHz Chống Va Chạm',1,1490000,'2026-04-22 11:00:00');

    INSERT INTO [dbo].[OrderStatusHistory] (OrderID, StatusID, Note, CreatedAt) VALUES
    (@o6,1,N'Đơn hàng mới – thanh toán SePay','2026-04-22 11:00:00');
END
GO

-- ────────────────────────────────────────
-- ĐƠN 7: Trần Ngọc Diệp – Completed – WALLET
-- ────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM [dbo].[Orders] WHERE OrderCode='ORD-2026-00007')
BEGIN
    DECLARE @w7 VARCHAR(20) = ISNULL((SELECT TOP 1 WardCode   FROM Wards    WHERE IsActive=1 ORDER BY NEWID()),'W01');
    DECLARE @d7 INT         = ISNULL((SELECT TOP 1 DistrictId FROM Districts WHERE IsActive=1 ORDER BY NEWID()),1);
    DECLARE @p7 INT         = ISNULL((SELECT TOP 1 ProvinceId FROM Provinces WHERE IsActive=1 ORDER BY NEWID()),1);

    INSERT INTO [dbo].[Orders]
        (AccountID, StatusID, OrderCode,
         ShippingName, ShippingPhone, ShippingAddress,
         ShippingWardCode, ShippingWardName, ShippingDistrictId, ShippingDistrictName,
         ShippingProvinceId, ShippingProvinceName,
         OrderDate, ConfirmedAt, ShippedAt, DeliveredAt, CompletedAt,
         PaymentMethod, PaymentStatus, PaidAt,
         SubTotal, EstimatedShippingFee, TotalAmount, CreatedAt)
    VALUES(
        (SELECT TOP 1 AccountID FROM Accounts WHERE Email='ndiep.tran@gmail.com'),
        7,'ORD-2026-00007',
        N'Trần Ngọc Diệp','0912001007',N'55 Hoàng Diệu 2, P. Linh Chiểu',
        @w7,N'Phường Linh Chiểu',@d7,N'Thủ Đức',@p7,N'TP. Hồ Chí Minh',
        '2026-03-20 09:30:00','2026-03-20 11:00:00','2026-03-21 08:00:00','2026-03-22 10:00:00','2026-03-23 08:00:00',
        'WALLET','PAID','2026-03-20 09:30:00',
        535000, 30000, 565000, '2026-03-20 09:30:00');

    DECLARE @o7 INT = SCOPE_IDENTITY();

    INSERT INTO [dbo].[OrderDetails] (OrderID, ProductID, ProductName, Quantity, UnitPrice, CreatedAt) VALUES
    (@o7, 6,N'Bóng Cao Su Hoa Văn Boho Size 5 – Chống Bơm Vỡ',       1, 85000,'2026-03-20 09:30:00'),
    (@o7, 7,N'Diều Hình Đại Bàng Cánh Lớn 1.4m – Kèm Dây 30m',       1,115000,'2026-03-20 09:30:00'),
    (@o7,14,N'Đất Nặn PlayDoh 24 Màu An Toàn Không Độc Hại',           3, 88000,'2026-03-20 09:30:00'),
    (@o7, 3,N'Thẻ Học Thông Minh 4D Động Vật Hoang Dã – 120 Thẻ',     1,145000,'2026-03-20 09:30:00');

    INSERT INTO [dbo].[OrderStatusHistory] (OrderID, StatusID, Note, CreatedAt) VALUES
    (@o7,1,N'Đơn mới',             '2026-03-20 09:30:00'),
    (@o7,2,N'Xác nhận',            '2026-03-20 11:00:00'),
    (@o7,4,N'Đang giao',           '2026-03-21 08:00:00'),
    (@o7,6,N'Giao thành công',     '2026-03-22 10:00:00'),
    (@o7,7,N'Khách xác nhận hoàn thành','2026-03-23 08:00:00');
END
GO

-- ────────────────────────────────────────
-- ĐƠN 8: Bùi Thị Mỹ Linh – Processing – SE_PAY
-- ────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM [dbo].[Orders] WHERE OrderCode='ORD-2026-00008')
BEGIN
    DECLARE @w8 VARCHAR(20) = ISNULL((SELECT TOP 1 WardCode   FROM Wards    WHERE IsActive=1 ORDER BY NEWID()),'W01');
    DECLARE @d8 INT         = ISNULL((SELECT TOP 1 DistrictId FROM Districts WHERE IsActive=1 ORDER BY NEWID()),1);
    DECLARE @p8 INT         = ISNULL((SELECT TOP 1 ProvinceId FROM Provinces WHERE IsActive=1 ORDER BY NEWID()),1);

    INSERT INTO [dbo].[Orders]
        (AccountID, StatusID, OrderCode,
         ShippingName, ShippingPhone, ShippingAddress,
         ShippingWardCode, ShippingWardName, ShippingDistrictId, ShippingDistrictName,
         ShippingProvinceId, ShippingProvinceName,
         OrderDate, ConfirmedAt,
         PaymentMethod, PaymentStatus, PaidAt,
         SubTotal, EstimatedShippingFee, TotalAmount, CreatedAt)
    VALUES(
        (SELECT TOP 1 AccountID FROM Accounts WHERE Email='mylinh.bui2024@gmail.com'),
        3,'ORD-2026-00008',
        N'Bùi Thị Mỹ Linh','0912001008',N'210 Nguyễn Văn Cừ, P. Nguyễn Cư Trinh',
        @w8,N'Phường Nguyễn Cư Trinh',@d8,N'Quận 5',@p8,N'TP. Hồ Chí Minh',
        '2026-04-20 14:00:00','2026-04-20 16:00:00',
        'SE_PAY','PAID','2026-04-20 14:00:00',
        595000, 20000, 615000, '2026-04-20 14:00:00');

    DECLARE @o8 INT = SCOPE_IDENTITY();

    INSERT INTO [dbo].[OrderDetails] (OrderID, ProductID, ProductName, Quantity, UnitPrice, CreatedAt)
    VALUES(@o8,10,N'Mô Hình Siêu Nhân Gao Red Ranger Khớp Xoay 24 Điểm',1,595000,'2026-04-20 14:00:00');

    INSERT INTO [dbo].[OrderStatusHistory] (OrderID, StatusID, Note, CreatedAt) VALUES
    (@o8,1,N'Đơn hàng mới',                      '2026-04-20 14:00:00'),
    (@o8,2,N'Đã xác nhận',                       '2026-04-20 16:00:00'),
    (@o8,3,N'GHN đang giao – dự kiến 22/04',     '2026-04-21 09:00:00');
END
GO

-- ────────────────────────────────────────
-- ĐƠN 9: Phan Quốc Khánh – Confirmed – COD
-- ────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM [dbo].[Orders] WHERE OrderCode='ORD-2026-00009')
BEGIN
    DECLARE @w9 VARCHAR(20) = ISNULL((SELECT TOP 1 WardCode   FROM Wards    WHERE IsActive=1 ORDER BY NEWID()),'W01');
    DECLARE @d9 INT         = ISNULL((SELECT TOP 1 DistrictId FROM Districts WHERE IsActive=1 ORDER BY NEWID()),1);
    DECLARE @p9 INT         = ISNULL((SELECT TOP 1 ProvinceId FROM Provinces WHERE IsActive=1 ORDER BY NEWID()),1);

    INSERT INTO [dbo].[Orders]
        (AccountID, StatusID, OrderCode,
         ShippingName, ShippingPhone, ShippingAddress,
         ShippingWardCode, ShippingWardName, ShippingDistrictId, ShippingDistrictName,
         ShippingProvinceId, ShippingProvinceName,
         OrderDate, ConfirmedAt,
         PaymentMethod, PaymentStatus,
         SubTotal, EstimatedShippingFee, TotalAmount, CreatedAt)
    VALUES(
        (SELECT TOP 1 AccountID FROM Accounts WHERE Email='quockhanh.phan@gmail.com'),
        2,'ORD-2026-00009',
        N'Phan Quốc Khánh','0912001009',N'17 Võ Văn Tần, P. Võ Thị Sáu',
        @w9,N'Phường Võ Thị Sáu',@d9,N'Quận 3',@p9,N'TP. Hồ Chí Minh',
        '2026-04-21 16:30:00','2026-04-22 09:00:00',
        'SHIP_COD','PENDING',
        770000, 30000, 800000, '2026-04-21 16:30:00');

    DECLARE @o9 INT = SCOPE_IDENTITY();

    INSERT INTO [dbo].[OrderDetails] (OrderID, ProductID, ProductName, Quantity, UnitPrice, CreatedAt) VALUES
    (@o9,11,N'Mô Hình Khủng Long T-Rex Cơ Bắp – Tỉ Lệ 1:10',1,395000,'2026-04-21 16:30:00'),
    (@o9, 4,N'Kính Hiển Vi Đồ Chơi ScienceMax 40–400x',        1,375000,'2026-04-21 16:30:00');

    INSERT INTO [dbo].[OrderStatusHistory] (OrderID, StatusID, Note, CreatedAt) VALUES
    (@o9,1,N'Đơn hàng COD mới',                   '2026-04-21 16:30:00'),
    (@o9,2,N'Staff xác nhận – đang lấy hàng kho', '2026-04-22 09:00:00');
END
GO

-- ────────────────────────────────────────
-- ĐƠN 10: Ngô Thị Thanh Tuyền – Completed – WALLET + Voucher VIP200K
-- ────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM [dbo].[Orders] WHERE OrderCode='ORD-2026-00010')
BEGIN
    DECLARE @w10 VARCHAR(20) = ISNULL((SELECT TOP 1 WardCode   FROM Wards    WHERE IsActive=1 ORDER BY NEWID()),'W01');
    DECLARE @d10 INT         = ISNULL((SELECT TOP 1 DistrictId FROM Districts WHERE IsActive=1 ORDER BY NEWID()),1);
    DECLARE @p10 INT         = ISNULL((SELECT TOP 1 ProvinceId FROM Provinces WHERE IsActive=1 ORDER BY NEWID()),1);

    INSERT INTO [dbo].[Orders]
        (AccountID, StatusID, OrderCode,
         ShippingName, ShippingPhone, ShippingAddress,
         ShippingWardCode, ShippingWardName, ShippingDistrictId, ShippingDistrictName,
         ShippingProvinceId, ShippingProvinceName,
         OrderDate, ConfirmedAt, ShippedAt, DeliveredAt, CompletedAt,
         PaymentMethod, PaymentStatus, PaidAt,
         SubTotal, EstimatedShippingFee, VoucherDiscountAmount, TotalAmount, CreatedAt)
    VALUES(
        (SELECT TOP 1 AccountID FROM Accounts WHERE Email='thanh.tuyen.ngo@gmail.com'),
        7,'ORD-2026-00010',
        N'Ngô Thị Thanh Tuyền','0912001010',N'33 Bùi Thị Xuân, P. Phạm Ngũ Lão',
        @w10,N'Phường Phạm Ngũ Lão',@d10,N'Quận 1',@p10,N'TP. Hồ Chí Minh',
        '2026-03-15 10:00:00','2026-03-15 12:00:00','2026-03-16 09:00:00','2026-03-17 11:00:00','2026-03-18 08:00:00',
        'WALLET','PAID','2026-03-15 10:00:00',
        2235000, 0, 200000, 2035000, '2026-03-15 10:00:00');

    DECLARE @o10 INT = SCOPE_IDENTITY();

    INSERT INTO [dbo].[OrderDetails] (OrderID, ProductID, ProductName, Quantity, UnitPrice, CreatedAt) VALUES
    (@o10, 1,N'Lego City Trạm Cảnh Sát Trung Tâm 668 Mảnh',    1,1290000,'2026-03-15 10:00:00'),
    (@o10, 5,N'Xe Chòi Chân Hình Vịt Donald Có Đèn Nhạc',       1, 545000,'2026-03-15 10:00:00'),
    (@o10,15,N'Bảng Vẽ Ma Thuật Tự Xóa LCD 10 Inch – Kèm Bút',  2, 175000,'2026-03-15 10:00:00');

    INSERT INTO [dbo].[OrderStatusHistory] (OrderID, StatusID, Note, CreatedAt) VALUES
    (@o10,1,N'Đơn mới – áp dụng voucher VIP200K', '2026-03-15 10:00:00'),
    (@o10,2,N'Xác nhận',                           '2026-03-15 12:00:00'),
    (@o10,4,N'GHN lấy hàng',                       '2026-03-16 09:00:00'),
    (@o10,6,N'Giao thành công',                    '2026-03-17 11:00:00'),
    (@o10,7,N'Hoàn thành – khách review 5 sao',    '2026-03-18 08:00:00');

    INSERT INTO [dbo].[OrderVouchers] (OrderID, VoucherID, DiscountAmountApplied)
    SELECT @o10, VoucherID, 200000 FROM Vouchers WHERE VoucherCode='VIP200K';

    INSERT INTO [dbo].[VoucherUsageLogs] (VoucherID, AccountID, OrderID, UsedAt)
    SELECT v.VoucherID, o.AccountID, o.OrderID, o.OrderDate
    FROM   Vouchers v, Orders o
    WHERE  v.VoucherCode='VIP200K' AND o.OrderCode='ORD-2026-00010';
END
GO


/* ══════════════════════════════════════════
   14. 30 ĐƠN HÀNG NGẪU NHIÊN (ĐƠN 11–40)
══════════════════════════════════════════ */
DECLARE @wAuto  VARCHAR(20) = ISNULL((SELECT TOP 1 WardCode   FROM Wards    WHERE IsActive=1),'W01');
DECLARE @dAuto  INT         = ISNULL((SELECT TOP 1 DistrictId FROM Districts WHERE IsActive=1),1);
DECLARE @pAuto  INT         = ISNULL((SELECT TOP 1 ProvinceId FROM Provinces WHERE IsActive=1),1);

DECLARE @loop   INT = 11;

WHILE @loop <= 40
BEGIN
    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM Orders WHERE OrderCode='ORD-2026-000'+CAST(@loop AS VARCHAR))
        BEGIN
            DECLARE @accID   INT = (SELECT TOP 1 AccountID FROM Accounts WHERE RoleID=1 ORDER BY NEWID());
            DECLARE @staffID INT = (SELECT TOP 1 AccountID FROM Accounts WHERE RoleID IN (2,3) ORDER BY NEWID());
            DECLARE @prodID  INT;
            DECLARE @prodPr  DECIMAL(12,0);
            DECLARE @prodNm  NVARCHAR(255);
            SELECT TOP 1 @prodID=ProductID, @prodPr=Price, @prodNm=ProductName
            FROM Products WHERE IsDeleted=0 AND ProductStatus='Active' ORDER BY NEWID();

            DECLARE @oCode   VARCHAR(30)  = 'ORD-2026-000'+CAST(@loop AS VARCHAR);
            DECLARE @pmeth   VARCHAR(20)  = CHOOSE((ABS(CHECKSUM(NEWID()))%3)+1,'SE_PAY','WALLET','BANK_TRANSFER');
            DECLARE @scen    INT          = (ABS(CHECKSUM(NEWID()))%6)+1;

            DECLARE @tOrder    DATETIME2(0) = DATEADD(DAY,-(ABS(CHECKSUM(NEWID()))%60),GETDATE());
            DECLARE @tConfirm  DATETIME2(0) = NULL;
            DECLARE @tShip     DATETIME2(0) = NULL;
            DECLARE @tDeliver  DATETIME2(0) = NULL;
            DECLARE @tComplete DATETIME2(0) = NULL;
            DECLARE @tCancel   DATETIME2(0) = NULL;
            DECLARE @fStatus   TINYINT;
            DECLARE @pstat     VARCHAR(20)  = 'PENDING';
            DECLARE @paidAt    DATETIME2(0) = NULL;
            DECLARE @cancelRsn NVARCHAR(500)= NULL;

            IF @scen=1 BEGIN SET @fStatus=1; END
            IF @scen=2 BEGIN SET @fStatus=2; SET @tConfirm=DATEADD(HOUR,2,@tOrder); END
            IF @scen=3 BEGIN SET @fStatus=4; SET @tConfirm=DATEADD(HOUR,2,@tOrder); SET @tShip=DATEADD(DAY,1,@tConfirm); END
            IF @scen=4 BEGIN SET @fStatus=6; SET @tConfirm=DATEADD(HOUR,2,@tOrder); SET @tShip=DATEADD(DAY,1,@tConfirm); SET @tDeliver=DATEADD(DAY,2,@tShip); END
            IF @scen=5 BEGIN SET @fStatus=7; SET @tConfirm=DATEADD(HOUR,2,@tOrder); SET @tShip=DATEADD(DAY,1,@tConfirm); SET @tDeliver=DATEADD(DAY,2,@tShip); SET @tComplete=DATEADD(DAY,1,@tDeliver); END
            IF @scen=6 BEGIN SET @fStatus=8; SET @tCancel=DATEADD(HOUR,5,@tOrder); SET @cancelRsn=N'Khách đổi ý'; END

            IF (@pmeth IN ('SE_PAY','WALLET') AND @scen<>6) OR @scen IN (4,5)
                BEGIN SET @pstat='PAID'; SET @paidAt=@tOrder; END
            IF @scen=6 SET @pstat='FAILED';

            DECLARE @qty      INT          = (ABS(CHECKSUM(NEWID()))%3)+1;
            DECLARE @sub      DECIMAL(12,0)= @qty * @prodPr;
            DECLARE @total    DECIMAL(12,0)= @sub + 30000;

            INSERT INTO [dbo].[Orders]
                (AccountID, StatusID, AssignedToStaffID, OrderCode,
                 ShippingName, ShippingPhone, ShippingAddress,
                 ShippingWardCode, ShippingWardName,
                 ShippingDistrictId, ShippingDistrictName,
                 ShippingProvinceId, ShippingProvinceName,
                 OrderDate, ConfirmedAt, ShippedAt, DeliveredAt, CompletedAt, CancelledAt,
                 PaymentMethod, PaymentStatus, PaidAt,
                 SubTotal, EstimatedShippingFee, ActualShippingFee, TotalAmount,
                 CancelReason, CreatedAt)
            VALUES(
                @accID, @fStatus,
                IIF(@scen>1 AND @scen<>6, @staffID, NULL),
                @oCode,
                N'Khách Hàng Tự Động', '0909999888',
                N'Địa chỉ giả lập '+CAST(@loop AS VARCHAR),
                @wAuto, N'Phường Test',
                @dAuto, N'Quận Test',
                @pAuto, N'Thành Phố Test',
                @tOrder, @tConfirm, @tShip, @tDeliver, @tComplete, @tCancel,
                @pmeth, @pstat, @paidAt,
                @sub, 30000, IIF(@scen>=4, 30000, NULL), @total,
                @cancelRsn, @tOrder);

            DECLARE @oAuto INT = SCOPE_IDENTITY();

            INSERT INTO [dbo].[OrderDetails]
                (OrderID, ProductID, ProductName, Quantity, UnitPrice, DiscountAmount, CreatedAt)
            VALUES(@oAuto, @prodID, @prodNm, @qty, @prodPr, 0, @tOrder);

            INSERT INTO [dbo].[OrderStatusHistory] (OrderID, StatusID, ChangedBy, Note, CreatedAt)
            VALUES(@oAuto,1,@accID,N'Hệ thống ghi nhận',@tOrder);
            IF @scen>=2 AND @scen<>6
                INSERT INTO [dbo].[OrderStatusHistory] (OrderID, StatusID, ChangedBy, Note, CreatedAt)
                VALUES(@oAuto,2,@staffID,N'Xác nhận đơn',@tConfirm);
            IF @scen>=3 AND @scen<>6
                INSERT INTO [dbo].[OrderStatusHistory] (OrderID, StatusID, ChangedBy, Note, CreatedAt)
                VALUES(@oAuto,4,@staffID,N'Giao vận chuyển',@tShip);
            IF @scen>=4 AND @scen<>6
                INSERT INTO [dbo].[OrderStatusHistory] (OrderID, StatusID, ChangedBy, Note, CreatedAt)
                VALUES(@oAuto,6,NULL,N'Giao hàng OK',@tDeliver);
            IF @scen=5
                INSERT INTO [dbo].[OrderStatusHistory] (OrderID, StatusID, ChangedBy, Note, CreatedAt)
                VALUES(@oAuto,7,@accID,N'Hoàn tất',@tComplete);
            IF @scen=6
                INSERT INTO [dbo].[OrderStatusHistory] (OrderID, StatusID, ChangedBy, Note, CreatedAt)
                VALUES(@oAuto,8,@accID,N'Đã hủy',@tCancel);

            INSERT INTO [dbo].[PaymentHistory] (AccountID, OrderID, PaymentStatus, PaymentMethod, Amount, CreatedAt)
            VALUES(@accID, @oAuto, @pstat, @pmeth, @total, @tOrder);

            IF @scen IN (3,4,5)
                INSERT INTO [dbo].[ShippingProviderTransactions]
                    (OrderID, Provider, ProviderOrderCode, TrackingNumber, ServiceType, Status,
                     ShippingFee, CodAmount, CreatedAt)
                VALUES(@oAuto,'GHN','GHN-'+@oCode,
                       'VN'+CAST(ABS(CHECKSUM(NEWID()))%1000000 AS VARCHAR),
                       N'Nhanh',
                       IIF(@scen=3,'transporting','delivered'),
                       30000, 0, @tShip);
        END
    END TRY
    BEGIN CATCH
        PRINT N'⚠ Bỏ qua vòng lặp '+CAST(@loop AS VARCHAR)+': '+ERROR_MESSAGE();
    END CATCH

    SET @loop = @loop + 1;
END
GO


/* ══════════════════════════════════════════
   15. SHIPPING HISTORY (cho đơn mẫu đã giao)
══════════════════════════════════════════ */
IF NOT EXISTS (SELECT 1 FROM [dbo].[ShippingStatusHistories])
BEGIN
    INSERT INTO [dbo].[ShippingProviderTransactions]
        (OrderID, Provider, ProviderOrderCode, TrackingNumber, Status, ShippingFee, CodAmount, CreatedAt)
    SELECT
        o.OrderID, 'GHN',
        'GHN26-'+RIGHT(o.OrderCode,5),
        'VN'+RIGHT(o.OrderCode,8)+'HCM',
        CASE o.StatusID
            WHEN 3 THEN 'delivering'
            WHEN 4 THEN 'delivered'
            WHEN 5 THEN 'delivered'
            WHEN 6 THEN 'delivered'
            WHEN 7 THEN 'delivered'
            ELSE 'ready_to_pick'
        END,
        o.EstimatedShippingFee,
        CASE o.PaymentMethod WHEN 'SHIP_COD' THEN o.TotalAmount ELSE 0 END,
        o.OrderDate
    FROM Orders o
    WHERE o.StatusID >= 2
      AND NOT EXISTS (
          SELECT 1 FROM ShippingProviderTransactions sp WHERE sp.OrderID = o.OrderID);

    INSERT INTO [dbo].[ShippingStatusHistories]
        (ShippingTxId, OrderId, PreviousStatus, NewStatus, Source, ProcessedAt)
    SELECT ShippingTransactionID, OrderID, 'created', Status, 'Seed', GETDATE()
    FROM   ShippingProviderTransactions;
END
GO


/* ══════════════════════════════════════════
   16. PAYMENT HISTORY (PAID orders chưa có bản ghi)
══════════════════════════════════════════ */
INSERT INTO [dbo].[PaymentHistory]
    (AccountID, OrderID, PaymentStatus, PaymentMethod, TransactionCode, Amount, CreatedAt)
SELECT o.AccountID, o.OrderID, o.PaymentStatus, o.PaymentMethod,
       'TXN-'+o.OrderCode, o.TotalAmount, o.OrderDate
FROM   Orders o
WHERE  o.PaymentStatus = 'PAID'
  AND  NOT EXISTS (SELECT 1 FROM PaymentHistory ph WHERE ph.OrderID = o.OrderID);
GO


/* ══════════════════════════════════════════
   17. PAYMENT GATEWAY TRANSACTIONS (SE_PAY PAID)
══════════════════════════════════════════ */
INSERT INTO [dbo].[PaymentGatewayTransactions]
    (OrderID, Provider, RequestID, Amount, ResponseCode, ResponseMessage, Status, CreatedAt)
SELECT o.OrderID,'SE_PAY','REQ-'+o.OrderCode,o.TotalAmount,'00','Transaction Success','Paid',o.OrderDate
FROM   Orders o
WHERE  o.PaymentMethod='SE_PAY' AND o.PaymentStatus='PAID'
  AND  NOT EXISTS (SELECT 1 FROM PaymentGatewayTransactions pg WHERE pg.OrderID=o.OrderID);
GO


/* ══════════════════════════════════════════
   18. WALLETS & TRANSACTIONS
══════════════════════════════════════════ */
INSERT INTO [dbo].[Wallets] (AccountID, Currency, Balance, Status, CreatedAt)
SELECT AccountID, 'VND', 3000000, 'Active', GETDATE()
FROM   Accounts
WHERE  RoleID = 1
  AND  NOT EXISTS (SELECT 1 FROM Wallets w WHERE w.AccountID = Accounts.AccountID);

INSERT INTO [dbo].[WalletTransactions]
    (WalletID, AccountID, TxnType, Direction, Amount,
     BalanceBefore, BalanceAfter, Method, Status, Reason, CreatedAt)
SELECT w.WalletID, w.AccountID, 'TopUp','CR',3000000,
       0, 3000000, 'BankTransfer','Completed',N'Nạp tiền ban đầu', GETDATE()
FROM   Wallets w
WHERE  NOT EXISTS (SELECT 1 FROM WalletTransactions wt WHERE wt.WalletID=w.WalletID AND wt.TxnType='TopUp');

INSERT INTO [dbo].[WalletTransactions]
    (WalletID, AccountID, RelatedOrderID, TxnType, Direction, Amount,
     BalanceBefore, BalanceAfter, Method, Status, Reason, CreatedAt)
SELECT w.WalletID, o.AccountID, o.OrderID, 'Payment','DR',
       o.TotalAmount,
       3000000,
       3000000 - o.TotalAmount,
       'Internal','Completed', N'Thanh toán đơn '+o.OrderCode, GETDATE()
FROM   Orders o
JOIN   Wallets w ON o.AccountID = w.AccountID
WHERE  o.PaymentMethod='WALLET' AND o.PaymentStatus='PAID'
  AND  NOT EXISTS (SELECT 1 FROM WalletTransactions wt WHERE wt.RelatedOrderID=o.OrderID AND wt.TxnType='Payment');
GO


/* ══════════════════════════════════════════
   19. ORDER REFUND (đơn 3 bị hủy)
══════════════════════════════════════════ */
IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderRefundReasons])
    INSERT INTO [dbo].[OrderRefundReasons] (Content, Description) VALUES
    (N'Sản phẩm bị lỗi từ nhà sản xuất', N'Hàng giao đến bị lỗi kỹ thuật'),
    (N'Giao nhầm sản phẩm',               N'Shop gửi sai màu, size hoặc model'),
    (N'Sản phẩm không đúng mô tả',        N'Hình ảnh/mô tả không khớp sản phẩm thực tế');

IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderRefunds])
BEGIN
    INSERT INTO [dbo].[OrderRefunds]
        (OrderID, RefundReasonID, CustomerID, RequestedBy, ApprovedAmount, RefundStatus, CreatedAt)
    SELECT TOP 1 o.OrderID, 1, o.AccountID, o.AccountID, o.TotalAmount, 'Requested', '2026-03-07 09:00:00'
    FROM   Orders o WHERE o.OrderCode='ORD-2026-00003';

    INSERT INTO [dbo].[RefundImages] (RefundID, ImageURL, CreatedAt)
    SELECT TOP 1 RefundID,'https://picsum.photos/seed/refund1/400/400',GETDATE()
    FROM   OrderRefunds;
END
GO


/* ══════════════════════════════════════════
   20. ADDRESSES
══════════════════════════════════════════ */
IF NOT EXISTS (SELECT 1 FROM [dbo].[Addresses])
    INSERT INTO [dbo].[Addresses]
        (AccountID, RecipientName, PhoneNumber, AddressLine,
         WardCode, DistrictId, ProvinceId, IsDefault, CreatedAt)
    SELECT
        a.AccountID, a.AccountName, a.PhoneNumber, adr.AddressLine,
        ISNULL((SELECT TOP 1 WardCode   FROM Wards     WHERE IsActive=1 ORDER BY NEWID()),'W01'),
        ISNULL((SELECT TOP 1 DistrictId FROM Districts WHERE IsActive=1 ORDER BY NEWID()),1),
        ISNULL((SELECT TOP 1 ProvinceId FROM Provinces WHERE IsActive=1 ORDER BY NEWID()),1),
        1, GETDATE()
    FROM Accounts a
    JOIN (VALUES
        ('lananh.pham@gmail.com',     N'72 Lê Lợi, P. Bến Nghé, Q.1, TP.HCM'),
        ('hung.nguyen88@gmail.com',   N'15 Trần Phú, P. Mộ Lao, Hà Đông, Hà Nội'),
        ('bauchau.vu@gmail.com',      N'88 Nguyễn Trãi, P. Thanh Xuân Trung, Hà Nội'),
        ('mtuando@outlook.com',       N'34 Đinh Tiên Hoàng, P. Đa Kao, Q.1, TP.HCM'),
        ('thuha.hoang@gmail.com',     N'120 Lê Văn Lương, P. Nhân Chính, Thanh Xuân, HN'),
        ('vanphuc.le@gmail.com',      N'9 Đinh Lễ, P. Hoàn Kiếm, Hà Nội'),
        ('ndiep.tran@gmail.com',      N'55 Hoàng Diệu 2, P. Linh Chiểu, TP. Thủ Đức'),
        ('mylinh.bui2024@gmail.com',  N'210 Nguyễn Văn Cừ, P. Nguyễn Cư Trinh, Q.5, TP.HCM'),
        ('quockhanh.phan@gmail.com',  N'17 Võ Văn Tần, P. Võ Thị Sáu, Q.3, TP.HCM'),
        ('thanh.tuyen.ngo@gmail.com', N'33 Bùi Thị Xuân, P. Phạm Ngũ Lão, Q.1, TP.HCM')
    ) AS adr(Email, AddressLine) ON a.Email = adr.Email;
GO


/* ══════════════════════════════════════════
   21. CUSTOMER CHILDREN
══════════════════════════════════════════ */
IF NOT EXISTS (SELECT 1 FROM [dbo].[CustomerChildren])
    INSERT INTO [dbo].[CustomerChildren] (AccountID, SexID, FullName, NickName, DOB, CreatedAt)
    VALUES
    ((SELECT TOP 1 AccountID FROM Accounts WHERE Email='lananh.pham@gmail.com'),    1,N'Phạm Tiến Phát', N'Củ Cải','2020-05-15',GETDATE()),
    ((SELECT TOP 1 AccountID FROM Accounts WHERE Email='lananh.pham@gmail.com'),    2,N'Phạm Thảo Trân', N'Bào Ngư','2022-11-20',GETDATE()),
    ((SELECT TOP 1 AccountID FROM Accounts WHERE Email='bauchau.vu@gmail.com'),     1,N'Vũ Hoàng Nam',   N'Gấu',   '2019-08-10',GETDATE()),
    ((SELECT TOP 1 AccountID FROM Accounts WHERE Email='thanh.tuyen.ngo@gmail.com'),2,N'Ngô Mai Phương',  N'Nhím',  '2023-01-05',GETDATE());
GO


/* ══════════════════════════════════════════
   22. CART & WISHLIST
══════════════════════════════════════════ */
IF NOT EXISTS (SELECT 1 FROM [dbo].[Cart])
BEGIN
    INSERT INTO [dbo].[Cart] (AccountID, CreatedAt) VALUES
    ((SELECT TOP 1 AccountID FROM Accounts WHERE Email='lananh.pham@gmail.com'),    '2026-04-20 10:00:00'),
    ((SELECT TOP 1 AccountID FROM Accounts WHERE Email='hung.nguyen88@gmail.com'),  '2026-04-21 08:00:00'),
    ((SELECT TOP 1 AccountID FROM Accounts WHERE Email='bauchau.vu@gmail.com'),     '2026-04-22 09:00:00'),
    ((SELECT TOP 1 AccountID FROM Accounts WHERE Email='mtuando@outlook.com'),      '2026-04-22 11:00:00'),
    ((SELECT TOP 1 AccountID FROM Accounts WHERE Email='thuha.hoang@gmail.com'),    '2026-04-23 14:00:00');

    INSERT INTO [dbo].[CartItems] (CartID, ProductID, Quantity, PriceAtThatTime, CurrentPrice, AddedAt)
    SELECT c.CartID,
           v.ProductID, v.Qty, v.Pr, v.Pr, v.AddedAt
    FROM Cart c
    JOIN Accounts a ON c.AccountID = a.AccountID
    JOIN (VALUES
        ('lananh.pham@gmail.com',     8, 1, 840000,'2026-04-20 10:05:00'),
        ('lananh.pham@gmail.com',     9, 1, 359000,'2026-04-20 10:06:00'),
        ('hung.nguyen88@gmail.com',  12, 1, 945000,'2026-04-21 08:10:00'),
        ('hung.nguyen88@gmail.com',  13, 1,1490000,'2026-04-21 08:12:00'),
        ('bauchau.vu@gmail.com',      1, 1,1290000,'2026-04-22 09:05:00'),
        ('bauchau.vu@gmail.com',     14, 3,  88000,'2026-04-22 09:07:00'),
        ('mtuando@outlook.com',       4, 1, 375000,'2026-04-22 11:05:00'),
        ('mtuando@outlook.com',      11, 1, 395000,'2026-04-22 11:06:00'),
        ('thuha.hoang@gmail.com',     5, 1, 545000,'2026-04-23 14:05:00'),
        ('thuha.hoang@gmail.com',     7, 2, 115000,'2026-04-23 14:08:00')
    ) AS v(Email, ProductID, Qty, Pr, AddedAt) ON a.Email = v.Email;
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Wishlists])
    INSERT INTO [dbo].[Wishlists] (AccountID, ProductID, CreatedAt)
    SELECT a.AccountID, v.ProductID, v.CreatedAt
    FROM Accounts a
    JOIN (VALUES
        ('vanphuc.le@gmail.com',     13,'2026-04-10 09:00:00'),
        ('ndiep.tran@gmail.com',      1,'2026-04-12 10:00:00'),
        ('mylinh.bui2024@gmail.com',  8,'2026-04-15 11:00:00'),
        ('quockhanh.phan@gmail.com', 12,'2026-04-18 14:00:00'),
        ('thanh.tuyen.ngo@gmail.com',10,'2026-04-20 09:00:00')
    ) AS v(Email, ProductID, CreatedAt) ON a.Email = v.Email;
GO


/* ══════════════════════════════════════════
   23. BLOG
══════════════════════════════════════════ */
SET IDENTITY_INSERT [dbo].[BlogCategories] ON;
IF NOT EXISTS (SELECT 1 FROM [dbo].[BlogCategories] WHERE BlogCategoryID=1)
    INSERT INTO [dbo].[BlogCategories] (BlogCategoryID, BlogCategoriesName, CreatedAt) VALUES
    (1,N'Tin Tức & Khuyến Mãi',   '2024-01-10 08:00:00'),
    (2,N'Kiến Thức Nuôi Dạy Trẻ', '2024-01-10 08:00:00'),
    (3,N'Review Sản Phẩm',         '2024-01-10 08:00:00');
SET IDENTITY_INSERT [dbo].[BlogCategories] OFF;

IF NOT EXISTS (SELECT 1 FROM [dbo].[BlogPosts])
    INSERT INTO [dbo].[BlogPosts]
        (AccountID, ApprovedBy, BlogCategoryID, BlogTitle, BlogContent,
         BlogThumbnail, Status, IsFeatured, BlogAt, CreatedAt)
    VALUES
    (
        (SELECT TOP 1 AccountID FROM Accounts WHERE RoleID=3),
        (SELECT TOP 1 AccountID FROM Accounts WHERE RoleID=2),
        1, N'Sale Hè Rực Rỡ – Giảm Đến 50% Hàng Nghìn Sản Phẩm Đồ Chơi',
        N'Chuỗi sự kiện Sale Hè 2026 bắt đầu từ 01/04! Lego, Bandai, Fisher-Price đồng loạt giảm đến 50%. Tặng kèm bảo hiểm 12 tháng cho đơn từ 1 triệu đồng.',
        'https://picsum.photos/seed/blog-sale-he/600/400','Published',1,'2026-03-28 09:00:00','2026-03-28 09:00:00'
    ),
    (
        (SELECT TOP 1 AccountID FROM Accounts WHERE RoleID=3),
        (SELECT TOP 1 AccountID FROM Accounts WHERE RoleID=2),
        2, N'7 Tiêu Chí Vàng Khi Chọn Đồ Chơi An Toàn Cho Trẻ Dưới 3 Tuổi',
        N'Giai đoạn 0–3 tuổi là thời điểm vàng. Cần chú ý: không BPA/phthalate, không góc sắc, kích thước >3cm, sơn nước không độc, có chứng nhận TCVN/CE, nhà sản xuất uy tín, dễ vệ sinh.',
        'https://picsum.photos/seed/blog-safety/600/400','Published',0,'2026-02-10 10:00:00','2026-02-10 10:00:00'
    ),
    (
        (SELECT TOP 1 AccountID FROM Accounts WHERE RoleID=3),
        (SELECT TOP 1 AccountID FROM Accounts WHERE RoleID=2),
        3, N'Review Thực Tế: Lego City Trạm Cảnh Sát Sau 3 Tháng Sử Dụng',
        N'Bộ 668 mảnh chất lượng rất tốt sau 3 tháng. Mảnh ghép chắc chắn, không gãy. Bé 7 tuổi tự lắp được 80%. Hướng dẫn rõ ràng. Điểm trừ duy nhất là giá hơi cao.',
        'https://picsum.photos/seed/blog-review-lego/600/400','Published',1,'2026-03-05 14:00:00','2026-03-05 14:00:00'
    );
GO


/* ══════════════════════════════════════════
   24. BLOCK REASONS & USER BLOCK HISTORY
══════════════════════════════════════════ */
IF NOT EXISTS (SELECT 1 FROM [dbo].[BlockReasons])
    INSERT INTO [dbo].[BlockReasons] (Content, Description, CreatedAt) VALUES
    (N'Spam đánh giá',       N'Đăng đánh giá hàng loạt không liên quan','2024-01-05 08:00:00'),
    (N'Dấu hiệu gian lận',  N'Đặt hàng rồi khai không nhận để hoàn tiền','2024-01-05 08:00:00'),
    (N'Ngôn ngữ xúc phạm',  N'Nhắn tin xúc phạm nhân viên','2024-01-05 08:00:00');

IF NOT EXISTS (SELECT 1 FROM [dbo].[UserBlockHistory])
    INSERT INTO [dbo].[UserBlockHistory]
        (AccountID, BlockedBy, BlockReasonID, Note, BlockedUntil)
    VALUES(
        (SELECT TOP 1 AccountID FROM Accounts WHERE RoleID=1 ORDER BY NEWID()),
        (SELECT TOP 1 AccountID FROM Accounts WHERE RoleID=2),
        1, N'Vi phạm lần đầu – cảnh cáo 7 ngày',
        DATEADD(DAY,7,GETDATE()));
GO


/* ══════════════════════════════════════════
   25. REVIEWS (chỉ cho đơn Completed, product phải có trong OrderDetails)
══════════════════════════════════════════ */
IF NOT EXISTS (SELECT 1 FROM [dbo].[ReviewProducts])
BEGIN
    INSERT INTO [dbo].[ReviewProducts]
        (AccountID, ProductID, OrderID, Rating, Comment, ModerationStatus, CreatedAt)
    VALUES
    (
        (SELECT TOP 1 AccountID FROM Accounts WHERE Email='lananh.pham@gmail.com'),
        1,
        (SELECT TOP 1 OrderID FROM Orders WHERE OrderCode='ORD-2026-00001'),
        5,
        N'Mua cho con trai 7 tuổi, bé mê lắm! Hộp đẹp, đầy đủ phụ kiện, mảnh ghép chắc. Giao hàng nhanh 1 ngày. ToyHouse sẽ là nơi mình quay lại mỗi dịp tặng quà!',
        'Approved', '2026-02-18 09:00:00'
    ),
    (
        (SELECT TOP 1 AccountID FROM Accounts WHERE Email='ndiep.tran@gmail.com'),
        6,
        (SELECT TOP 1 OrderID FROM Orders WHERE OrderCode='ORD-2026-00007'),
        5,
        N'Mua bóng cho con, chất lượng rất tốt. Cao su dày, không có mùi. Con chơi ngoài trời mấy tuần vẫn không xì.',
        'Approved', '2026-03-24 10:00:00'
    ),
    (
        (SELECT TOP 1 AccountID FROM Accounts WHERE Email='thanh.tuyen.ngo@gmail.com'),
        1,
        (SELECT TOP 1 OrderID FROM Orders WHERE OrderCode='ORD-2026-00010'),
        5,
        N'Set hàng Lego City + xe chòi + 2 bảng LCD, được giảm 200k voucher VIP. Rất hài lòng! Xe chòi bé 2.5 tuổi leo lên đạp đi ngay, nhạc đèn vui.',
        'Approved', '2026-03-19 08:00:00'
    );

    DECLARE @staff_r INT = (SELECT TOP 1 AccountID FROM Accounts WHERE RoleID=3);

    INSERT INTO [dbo].[StaffReviewProductReplies]
        (ReviewProductID, StaffID, Content, CreatedAt)
    SELECT r.ReviewID, @staff_r,
           N'Cảm ơn quý khách rất nhiều vì review chi tiết! ToyHouse rất vui khi bé nhà mình yêu thích sản phẩm. Hẹn gặp lại!',
           DATEADD(HOUR,2,r.CreatedAt)
    FROM ReviewProducts r;

    INSERT INTO [dbo].[ReviewProductImages] (ReviewProductID, ImageURL, ModerationStatus, CreatedAt)
    SELECT ReviewID,
           'https://picsum.photos/seed/review-img-'+CAST(ReviewID AS VARCHAR)+'/300/300',
           'Approved', GETDATE()
    FROM ReviewProducts;

    INSERT INTO [dbo].[ReviewProductReactions] (ReviewProductID, AccountID, ReactionTypeID, CreatedAt)
    SELECT rp.ReviewID,
           (SELECT TOP 1 AccountID FROM Accounts WHERE RoleID=3 AND AccountID<>rp.AccountID),
           1, GETDATE()
    FROM ReviewProducts rp;
END
GO


/* ══════════════════════════════════════════
   26. BLOG REVIEWS & REACTIONS
══════════════════════════════════════════ */
IF NOT EXISTS (SELECT 1 FROM [dbo].[ReviewBlogs])
BEGIN
    INSERT INTO [dbo].[ReviewBlogs] (BlogPostID, AccountID, Comment, CreatedAt)
    VALUES
    (
        (SELECT TOP 1 BlogPostID FROM BlogPosts WHERE BlogTitle LIKE N'%Sale Hè%'),
        (SELECT TOP 1 AccountID FROM Accounts WHERE Email='lananh.pham@gmail.com'),
        N'Mình đã mua Lego City trong đợt này, giảm 15% rất hời! Bé nhà mình mê lắm.',
        '2026-03-29 10:00:00'
    ),
    (
        (SELECT TOP 1 BlogPostID FROM BlogPosts WHERE BlogTitle LIKE N'%Review Thực Tế%'),
        (SELECT TOP 1 AccountID FROM Accounts WHERE Email='hung.nguyen88@gmail.com'),
        N'Bài review rất chi tiết! Mình cũng đang nghĩ mua bộ này cho con.',
        '2026-03-06 11:00:00'
    );

    DECLARE @adm_blog INT = (SELECT TOP 1 AccountID FROM Accounts WHERE RoleID=2);

    INSERT INTO [dbo].[ReviewBlogReplies] (ReviewBlogID, AccountID, Comment, CreatedAt)
    SELECT rb.ReviewBlogID, @adm_blog,
           N'Cảm ơn bạn đã đồng hành! Chúc bé luôn vui vẻ và phát triển toàn diện!',
           DATEADD(HOUR,1,rb.CreatedAt)
    FROM ReviewBlogs rb;

    INSERT INTO [dbo].[ReviewBlogReactions] (ReviewBlogID, AccountID, ReactionTypeID, CreatedAt)
    SELECT rb.ReviewBlogID,
           (SELECT TOP 1 AccountID FROM Accounts WHERE Email='ndiep.tran@gmail.com'),
           1, DATEADD(MINUTE,30,rb.CreatedAt)
    FROM ReviewBlogs rb;
END
GO


/* ══════════════════════════════════════════
   27. NOTIFICATION TEMPLATES (23 templates)
══════════════════════════════════════════ */
IF NOT EXISTS (SELECT 1 FROM [Notification].[Templates])
    INSERT INTO [Notification].[Templates]
        (TemplateCode, UsageScope, TitleTemplate, MessageTemplate, IsActive, CreatedAt)
    VALUES
    ('NOTIF_ORDER_PLACED',     'SYSTEM',N'Đặt hàng thành công',               N'Đơn hàng {OrderCode} của bạn đã được đặt. Tổng tiền: {TotalAmount} VND.',1,'2024-01-05 08:00:00'),
    ('NOTIF_ORDER_CONFIRMED',  'SYSTEM',N'Đơn hàng đã được xác nhận',          N'Đơn hàng {OrderCode} đã xác nhận và đang chuẩn bị giao.',1,'2024-01-05 08:00:00'),
    ('NOTIF_ORDER_SHIPPED',    'SYSTEM',N'Đơn hàng đang trên đường',            N'Đơn hàng {OrderCode} đã bàn giao vận chuyển. Mã vận đơn: {ShippingOrderCode}.',1,'2024-01-05 08:00:00'),
    ('NOTIF_ORDER_DELIVERED',  'SYSTEM',N'Đơn hàng đã được giao',              N'Đơn hàng {OrderCode} giao thành công. Xác nhận trong 3 ngày.',1,'2024-01-05 08:00:00'),
    ('NOTIF_ORDER_COMPLETED',  'SYSTEM',N'Đơn hàng hoàn thành',                N'Đơn hàng {OrderCode} đã hoàn thành. Cảm ơn bạn đã mua sắm tại ToyHouse!',1,'2024-01-05 08:00:00'),
    ('NOTIF_ORDER_CANCELLED',  'SYSTEM',N'Đơn hàng đã bị hủy',               N'Đơn hàng {OrderCode} bị hủy. Lý do: {CancelReason}.',1,'2024-01-05 08:00:00'),
    ('NOTIF_REFUND_REQUESTED', 'SYSTEM',N'Yêu cầu hoàn tiền đã gửi',          N'Yêu cầu hoàn tiền đơn {OrderCode} đang chờ xét duyệt.',1,'2024-01-05 08:00:00'),
    ('NOTIF_REFUND_APPROVED',  'SYSTEM',N'Yêu cầu hoàn tiền được chấp thuận', N'Hoàn tiền {ApprovedAmount} VND cho đơn {OrderCode} đã được duyệt.',1,'2024-01-05 08:00:00'),
    ('NOTIF_REFUND_REJECTED',  'SYSTEM',N'Yêu cầu hoàn tiền bị từ chối',      N'Yêu cầu hoàn tiền đơn {OrderCode} bị từ chối. Lý do: {Reason}.',1,'2024-01-05 08:00:00'),
    ('NOTIF_REFUND_COMPLETED', 'SYSTEM',N'Hoàn tiền thành công',               N'{ApprovedAmount} VND từ đơn {OrderCode} đã hoàn vào ví. Số dư: {BalanceAfter} VND.',1,'2024-01-05 08:00:00'),
    ('NOTIF_WALLET_TOPUP',     'SYSTEM',N'Nạp ví thành công',                  N'Ví nạp thêm {Amount} VND. Số dư: {BalanceAfter} VND.',1,'2024-01-05 08:00:00'),
    ('NOTIF_WALLET_PAYMENT',   'SYSTEM',N'Thanh toán đơn hàng bằng ví',        N'Ví trừ {Amount} VND thanh toán đơn {OrderCode}. Còn lại: {BalanceAfter} VND.',1,'2024-01-05 08:00:00'),
    ('NOTIF_PAYMENT_FAILED',   'SYSTEM',N'Thanh toán thất bại',                N'Thanh toán đơn {OrderCode} không thành công. Vui lòng thử lại.',1,'2024-01-05 08:00:00'),
    ('NOTIF_ACCOUNT_REGISTERED','SYSTEM',N'Chào mừng đến với ToyHouse!',       N'Tài khoản {AccountName} đăng ký thành công. Chúc mua sắm vui vẻ!',1,'2024-01-05 08:00:00'),
    ('NOTIF_ACCOUNT_BLOCKED',  'SYSTEM',N'Tài khoản bị tạm khóa',              N'Tài khoản tạm khóa đến {BlockedUntil}. Lý do: {BlockReason}.',1,'2024-01-05 08:00:00'),
    ('NOTIF_ACCOUNT_UNBLOCKED','SYSTEM',N'Tài khoản đã được mở khóa',          N'Tài khoản mở khóa lúc {UnblockedAt}. Bạn có thể đăng nhập bình thường.',1,'2024-01-05 08:00:00'),
    ('NOTIF_LOW_STOCK',        'SYSTEM',N'Cảnh báo tồn kho thấp',              N'Sản phẩm "{ProductName}" (ID:{ProductID}) sắp hết. Số lượng: {Quantity}.',1,'2024-01-05 08:00:00'),
    ('NOTIF_OUT_OF_STOCK',     'SYSTEM',N'Sản phẩm đã hết hàng',               N'Sản phẩm "{ProductName}" (ID:{ProductID}) đã hết hàng. Vui lòng nhập hàng.',1,'2024-01-05 08:00:00'),
    ('NOTIF_BLOG_APPROVED',    'SYSTEM',N'Bài viết đã được duyệt',              N'Bài viết "{BlogTitle}" đã được phê duyệt và đăng công khai.',1,'2024-01-05 08:00:00'),
    ('NOTIF_BLOG_REJECTED',    'SYSTEM',N'Bài viết bị từ chối',                N'Bài viết "{BlogTitle}" bị từ chối. Lý do: {Reason}.',1,'2024-01-05 08:00:00'),
    ('NOTIF_PROMO_START',      'ADMIN', N'Khuyến mãi đã bắt đầu',              N'Chương trình "{PromotionName}" đã chính thức bắt đầu! Nhanh tay săn ưu đãi.',1,'2024-01-05 08:00:00'),
    ('NOTIF_PROMO_ENDING_SOON','ADMIN', N'Khuyến mãi sắp kết thúc!',           N'Chương trình "{PromotionName}" kết thúc lúc {EndDate}. Đừng bỏ lỡ!',1,'2024-01-05 08:00:00'),
    ('NOTIF_ORDER_ASSIGNED',   'SYSTEM',N'Đơn hàng mới được phân công',         N'Đơn hàng {OrderCode} phân công cho bạn. Vui lòng xác nhận.',1,'2024-01-05 08:00:00');
GO


/* ══════════════════════════════════════════
   28. USER PREFERENCES (backfill nếu trigger chưa chạy kịp)
══════════════════════════════════════════ */
INSERT INTO [Notification].[UserPreferences] (AccountID)
SELECT a.AccountID FROM Accounts a
WHERE NOT EXISTS (
    SELECT 1 FROM [Notification].[UserPreferences] up WHERE up.AccountID = a.AccountID);

UPDATE [Notification].[UserPreferences]
SET    [Promotions] = 0
WHERE  AccountID IN (
    SELECT AccountID FROM Accounts
    WHERE  Email IN ('minhquan.dinh@icloud.com','vantai.chau@gmail.com'));
GO


/* ══════════════════════════════════════════
   29. NOTIFICATION CAMPAIGNS & DELIVERIES
══════════════════════════════════════════ */
IF NOT EXISTS (SELECT 1 FROM [Notification].[Campaigns])
BEGIN
    DECLARE @adm_notif INT = (SELECT TOP 1 AccountID FROM Accounts WHERE RoleID=2);

    INSERT INTO [Notification].[Campaigns]
        (CampaignName, TemplateCode, SourceType, TargetType, Status, ScheduledAt, CreatedByAccountID, CreatedAt)
    VALUES
    (N'Thông Báo Sale Hè Rực Rỡ 2026','NOTIF_PROMO_START','ADMIN','ALL',   'Sent',     NULL,               @adm_notif,'2026-03-28 08:00:00'),
    (N'Nhắc Nhở Giỏ Hàng Bỏ Quên',    'NOTIF_ORDER_PLACED','ADMIN','ROLE','Scheduled','2026-05-10 08:00:00',@adm_notif,'2026-04-01 08:00:00');

    DECLARE @cam1 INT = (SELECT TOP 1 CampaignID FROM [Notification].[Campaigns] WHERE CampaignName=N'Thông Báo Sale Hè Rực Rỡ 2026');
    DECLARE @cam2 INT = (SELECT TOP 1 CampaignID FROM [Notification].[Campaigns] WHERE CampaignName=N'Nhắc Nhở Giỏ Hàng Bỏ Quên');

    INSERT INTO [Notification].[CampaignTargets] (CampaignID, TargetType, TargetValue) VALUES
    (@cam1,'ROLE_ID','1'),
    (@cam2,'ROLE_ID','1');

    INSERT INTO [Notification].[CampaignStats] (CampaignID, TotalSent, TotalRead, TotalClicked) VALUES
    (@cam1,100,50,12),
    (@cam2,  0, 0, 0);
END

INSERT INTO [Notification].[Deliveries]
    (AccountID, TemplateCode, RecipientType, NotificationType, Title, Message, Payload, Status, CreatedAt)
SELECT
    o.AccountID, 'NOTIF_ORDER_PLACED', 'CUSTOMER', 'ORDER',
    N'Đặt hàng thành công',
    N'Đơn hàng '+o.OrderCode+N' đã được xác nhận. Chúng mình đang đóng gói cho bạn!',
    '{"orderId":'+CAST(o.OrderID AS VARCHAR)+',"orderCode":"'+o.OrderCode+'"}',
    'Unread', o.OrderDate
FROM Orders o
WHERE NOT EXISTS (
    SELECT 1 FROM [Notification].[Deliveries] d
    WHERE  d.AccountID=o.AccountID
      AND  d.Message LIKE '%'+o.OrderCode+'%');

INSERT INTO [Notification].[DeliveryActions] (DeliveryID, AccountID, ActionType, OccurredAt)
SELECT TOP 5 d.DeliveryID, d.AccountID, 'Read', DATEADD(MINUTE,30,d.CreatedAt)
FROM [Notification].[Deliveries] d
WHERE NOT EXISTS (
    SELECT 1 FROM [Notification].[DeliveryActions] da WHERE da.DeliveryID=d.DeliveryID);
GO


/* ══════════════════════════════════════════
   30. CHAT CONVERSATIONS & MESSAGES
══════════════════════════════════════════ */
IF NOT EXISTS (SELECT 1 FROM [dbo].[ChatConversations])
BEGIN
    INSERT INTO [dbo].[ChatConversations] (AccountID, SessionID, Status, CreatedAt) VALUES
    ((SELECT TOP 1 AccountID FROM Accounts WHERE Email='mtuando@outlook.com'),     'SES-20260418-001','BotActive',  '2026-04-18 08:55:00'),
    ((SELECT TOP 1 AccountID FROM Accounts WHERE Email='quockhanh.phan@gmail.com'),'SES-20260421-002','StaffJoined','2026-04-21 15:50:00');

    DECLARE @cv1 INT = (SELECT TOP 1 ConversationID FROM ChatConversations WHERE SessionID='SES-20260418-001');
    DECLARE @cv2 INT = (SELECT TOP 1 ConversationID FROM ChatConversations WHERE SessionID='SES-20260421-002');

    INSERT INTO [dbo].[ChatMessages] (ConversationID, SenderType, Content, CreatedAt) VALUES
    (@cv1,'USER', N'Bộ Lego City 668 mảnh này phù hợp bé mấy tuổi ạ?','2026-04-18 08:56:00'),
    (@cv1,'BOT',  N'Dạ bộ này phù hợp bé từ 6 tuổi trở lên. Bé 6–8 tuổi cần ba mẹ hỗ trợ; từ 9 tuổi tự lắp hoàn toàn được ạ.','2026-04-18 08:56:30'),
    (@cv1,'USER', N'Con mình 7 tuổi, mua được không shop?','2026-04-18 08:57:00'),
    (@cv1,'BOT',  N'Hoàn toàn phù hợp! Nhiều khách phản hồi bé 7 tuổi tự lắp được 80% với chút hỗ trợ từ ba mẹ.','2026-04-18 08:57:15'),
    (@cv2,'USER', N'Xe RC Traxxas này sạc bao lâu và chạy được bao nhiêu phút ạ?','2026-04-21 15:51:00'),
    (@cv2,'STAFF',N'Dạ, xe sạc đầy khoảng 60 phút, chạy liên tục 25–30 phút. Mua thêm pin dự phòng chơi cả ngày!','2026-04-21 15:53:00'),
    (@cv2,'USER', N'OK mình đặt luôn. Có freeship không ạ?','2026-04-21 15:54:00'),
    (@cv2,'STAFF',N'Đơn từ 200k trở lên freeship. Đơn xe RC 945k của anh/chị freeship 100% ạ!','2026-04-21 15:54:30');
END
GO


/* ══════════════════════════════════════════
   31. RECOMMENDATION & INTERACTION
══════════════════════════════════════════ */
IF NOT EXISTS (SELECT 1 FROM [Recommendation].[UserProductScores])
    INSERT INTO [Recommendation].[UserProductScores]
        (AccountID, ProductID, Score, ViewCount, LastInteractedAt)
    SELECT a.AccountID, p.ProductID,
           ROUND(0.1 + RAND(CHECKSUM(NEWID()))*0.9, 2),
           CEILING(RAND(CHECKSUM(NEWID()))*15),
           DATEADD(HOUR,-CEILING(RAND(CHECKSUM(NEWID()))*72),GETDATE())
    FROM Accounts a CROSS JOIN Products p
    WHERE a.RoleID=1 AND p.ProductID IN (1,8,9,12,13);

IF NOT EXISTS (SELECT 1 FROM [Recommendation].[TrendingProducts])
    INSERT INTO [Recommendation].[TrendingProducts]
        (ProductID, Scope, Score, Rank, WindowHours) VALUES
    ( 1,'global',95.5,1,24),
    (12,'global',82.3,2,24),
    ( 8,'global',74.1,3,24),
    ( 9,'global',68.7,4,24),
    (13,'global',55.2,5,24);

IF NOT EXISTS (SELECT 1 FROM [Recommendation].[ItemSimilarities])
    INSERT INTO [Recommendation].[ItemSimilarities]
        (SourceProductID, SimilarProductID, SimilarityScore) VALUES
    ( 1, 2,0.78),( 2, 1,0.78),
    ( 8, 9,0.82),( 9, 8,0.82),
    (12,13,0.90),(13,12,0.90),
    (10,11,0.75),(11,10,0.75);

IF NOT EXISTS (SELECT 1 FROM [Recommendation].[Widgets])
    INSERT INTO [Recommendation].[Widgets]
        (WidgetCode, WidgetName, Algorithm, MaxItems, IsActive) VALUES
    ('homepage_trending',N'Sản phẩm đang được yêu thích','trending',       8,1),
    ('pdp_similar',      N'Sản phẩm tương tự',           'item_similarity',6,1),
    ('cart_upsell',      N'Có thể bạn cũng thích',        'collaborative',  4,1);

IF NOT EXISTS (SELECT 1 FROM [Interaction].[Events])
    INSERT INTO [Interaction].[Events]
        (AccountID, SessionID, EventType, EntityID, EntityType, Source, DeviceType)
    SELECT a.AccountID, 'SES-'+CAST(a.AccountID AS VARCHAR),
           CASE (ABS(CHECKSUM(NEWID()))%3)
               WHEN 0 THEN 'ViewProduct'
               WHEN 1 THEN 'AddToCart'
               ELSE 'AddToWishlist' END,
           CAST(p.ProductID AS VARCHAR),'Product',
           CASE (ABS(CHECKSUM(NEWID()))%3)
               WHEN 0 THEN 'homepage'
               WHEN 1 THEN 'search'
               ELSE 'category' END,
           CASE (ABS(CHECKSUM(NEWID()))%2) WHEN 0 THEN 'mobile' ELSE 'desktop' END
    FROM Accounts a CROSS JOIN Products p
    WHERE a.RoleID=1 AND p.ProductID IN (1,8,12);
GO


/* ══════════════════════════════════════════
   32. SYSTEM BACKGROUND JOBS
══════════════════════════════════════════ */
IF NOT EXISTS (SELECT 1 FROM [System].[BackgroundJobs])
    INSERT INTO [System].[BackgroundJobs]
        (JobName, CronExpression, IsEnabled, LastRunStatus) VALUES
    ('SyncGHNStatus',  '*/5 * * * *','1','Pending'),
    ('RecalcTrending', '0 * * * *',  '1','Pending'),
    ('ExpireVouchers', '0 0 * * *',  '1','Pending'),
    ('SendPromoNotif', '0 8 * * *',  '1','Pending');
GO


/* ══════════════════════════════════════════
   33. DOMAIN EVENT OUTBOX
══════════════════════════════════════════ */
INSERT INTO [System].[DomainEventOutbox]
    (EventID, EventType, AggregateType, AggregateId, Payload, OccurredOn)
SELECT
    NEWID(), 'OrderPlacedEvent', 'Order',
    CAST(OrderID AS VARCHAR),
    '{"orderId":'+CAST(OrderID AS VARCHAR)+',"orderCode":"'+OrderCode+'","amount":'+CAST(TotalAmount AS VARCHAR)+'}',
    OrderDate
FROM Orders o
WHERE NOT EXISTS (
    SELECT 1 FROM [System].[DomainEventOutbox]
    WHERE  AggregateId = CAST(o.OrderID AS VARCHAR)
      AND  EventType   = 'OrderPlacedEvent');
GO


/* ═══════════════════════════════════════════════════════════════
   HOÀN TẤT
═══════════════════════════════════════════════════════════════ */
PRINT N'';
PRINT N'══════════════════════════════════════════════════════════';
PRINT N'✅  DataSeed v3.2 hoàn tất!';
PRINT N'';
PRINT N'  Dữ liệu đã seed:';
PRINT N'  • 4 Roles, 3 nhân viên, 20 khách hàng';
PRINT N'  • 6 SuperCategories, 13 Categories, 5 Materials/Ages/Origins';
PRINT N'  • 6 Brands, 5 PriceRanges, 8 StatusOrders, 3 ReactionTypes';
PRINT N'  • 2 Promotions + time slots, 3 Vouchers';
PRINT N'  • 15 Products + ProductDetails + 45 ProductImages';
PRINT N'  • 10 đơn hàng mẫu (đầy đủ edge case: COD, WALLET, SE_PAY,';
PRINT N'    voucher, cancel, multi-item, delivered, completed)';
PRINT N'  • 30 đơn hàng ngẫu nhiên (ĐƠN 11–40)';
PRINT N'  • Shipping transactions + status histories';
PRINT N'  • Payment history + Gateway transactions (SE_PAY)';
PRINT N'  • Wallets + WalletTransactions (TopUp + Payment)';
PRINT N'  • 1 OrderRefund + RefundImages';
PRINT N'  • 10 Addresses, 4 CustomerChildren';
PRINT N'  • Cart + CartItems, Wishlists';
PRINT N'  • 3 BlogCategories, 3 BlogPosts';
PRINT N'  • BlockReasons, 1 UserBlockHistory';
PRINT N'  • 3 ProductReviews + StaffReplies + Images + Reactions';
PRINT N'  • 2 BlogReviews + BlogReplies + BlogReactions';
PRINT N'  • 23 Notification Templates, UserPreferences (backfill)';
PRINT N'  • 2 Campaigns + CampaignTargets + CampaignStats';
PRINT N'  • Notification Deliveries + DeliveryActions';
PRINT N'  • Chat Conversations + Messages';
PRINT N'  • Recommendation (Scores, Trending, Similarities, Widgets)';
PRINT N'  • Interaction Events';
PRINT N'  • 4 BackgroundJobs, DomainEventOutbox';
PRINT N'══════════════════════════════════════════════════════════';
GO