DataSeed.txt
1
100%
/* =================================================================
   DataSeed FULL v5.2 – SEP490_ToyStore
================================================================= */

USE [SEP490_ToyStore];
GO
SET NOCOUNT ON;
GO

PRINT N'================================================================';
PRINT N'  DataSeed Fixed v5.2 – Bắt đầu...';
PRINT N'================================================================';

/* ══════════════════════════════════════════════════════════════
   SECTION 1 – ROLES
══════════════════════════════════════════════════════════════ */
PRINT N'[1/40] Roles...';
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
PRINT N'[2/40] Sexes...';
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
PRINT N'[3/40] Accounts – Staff & Admin...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[Accounts] WHERE Email = 'admin@toyhouse.vn')
    INSERT INTO [dbo].[Accounts]
        (RoleID, SexID, EmployeeCode, AccountName, PhoneNumber, Email, DOB, PasswordHash, IsActive, CreatedAt)
    VALUES
    (2,1,'AD001',N'Nguyễn Minh Khôi',    '0901000001','admin@toyhouse.vn',    '1990-03-15','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2024-01-05 08:00:00'),
    (3,2,'ST001',N'Trần Thị Hồng Nhung', '0901000002','nhung.st@toyhouse.vn', '1995-07-22','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2024-01-05 08:00:00'),
    (3,1,'ST002',N'Phạm Văn Dũng',       '0901000003','dung.st@toyhouse.vn',  '1993-11-08','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2024-01-10 08:00:00'),
    (3,2,'ST003',N'Lê Thị Thu Thảo',    '0901000004','thao.st@toyhouse.vn',  '1997-04-30','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2024-01-10 08:00:00'),
    (4,1,'MC001',N'Lê Quốc Bảo',         '0901000005','bao.kho@toyhouse.vn',  '1992-09-18','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2024-01-05 08:00:00'),
    (4,2,'MC002',N'Nguyễn Thị Lan',     '0901000006','lan.kho@toyhouse.vn',  '1994-06-25','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2024-01-10 08:00:00');
GO

PRINT N'[3/40] Accounts – Customers (40 người)...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[Accounts] WHERE Email = 'lananh.pham@gmail.com')
    INSERT INTO [dbo].[Accounts]
        (RoleID, SexID, AccountName, PhoneNumber, Email, DOB, PasswordHash, IsActive, CreatedAt)
    VALUES
    (1,2,N'Phạm Thị Lan Anh',      '0912001001','lananh.pham@gmail.com',      '1988-03-12','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2024-03-12 10:15:00'),
    (1,1,N'Nguyễn Văn Hùng',       '0912001002','hung.nguyen88@gmail.com',     '1988-06-20','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2024-04-02 14:30:00'),
    (1,2,N'Vũ Thị Bảo Châu',       '0912001003','bauchau.vu@gmail.com',        '1992-01-15','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2024-04-20 09:00:00'),
    (1,1,N'Đỗ Minh Tuấn',          '0912001004','mtuando@outlook.com',         '1985-08-10','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2024-05-08 16:45:00'),
    (1,2,N'Hoàng Thị Thu Hà',      '0912001005','thuha.hoang@gmail.com',       '1990-12-05','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2024-06-01 11:20:00'),
    (1,1,N'Lê Văn Phúc',           '0912001006','vanphuc.le@gmail.com',        '1987-05-22','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2024-07-15 08:30:00'),
    (1,2,N'Trần Ngọc Diệp',        '0912001007','ndiep.tran@gmail.com',        '1993-09-17','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2024-08-03 13:00:00'),
    (1,2,N'Bùi Thị Mỹ Linh',      '0912001008','mylinh.bui2024@gmail.com',    '1995-02-28','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2024-08-22 15:10:00'),
    (1,1,N'Phan Quốc Khánh',       '0912001009','quockhanh.phan@gmail.com',    '1986-11-30','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2024-09-10 09:45:00'),
    (1,2,N'Ngô Thị Thanh Tuyền',   '0912001010','thanh.tuyen.ngo@gmail.com',   '1991-07-14','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2024-09-28 17:00:00'),
    (1,1,N'Dương Văn Long',        '0912001011','dvlong.toys@gmail.com',       '1989-04-03','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2024-10-14 10:30:00'),
    (1,2,N'Trịnh Thị Kim Oanh',    '0912001012','kimoanh.trinh@gmail.com',     '1994-10-19','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2024-11-01 08:00:00'),
    (1,1,N'Huỳnh Đức Thịnh',      '0912001013','ducthinh.huynh@gmail.com',    '1990-08-07','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2024-11-20 14:15:00'),
    (1,2,N'Mai Thị Hồng Vân',     '0912001014','hongvan.mai@gmail.com',       '1996-01-25','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2024-12-05 09:30:00'),
    (1,1,N'Đinh Minh Quân',        '0912001015','minhquan.dinh@icloud.com',    '1987-06-11','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2024-12-18 11:00:00'),
    (1,2,N'Lý Thị Xuân Mai',       '0912001016','xuanmai.ly@gmail.com',        '1993-03-08','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2025-01-07 10:00:00'),
    (1,1,N'Châu Văn Tài',          '0912001017','vantai.chau@gmail.com',       '1988-12-20','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2025-01-25 13:45:00'),
    (1,2,N'Nguyễn Thị Yến Nhi',   '0912001018','yennhi.ng@gmail.com',         '1997-09-02','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2025-02-14 08:30:00'),
    (1,1,N'Võ Thanh Sang',         '0912001019','thanhsang.vo@gmail.com',      '1991-05-16','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2025-03-03 16:00:00'),
    (1,2,N'Phùng Thị Bích Trâm',  '0912001020','bichtram.phung@gmail.com',    '1995-11-29','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2025-03-20 09:15:00'),
    (1,1,N'Cao Minh Nhật',         '0912001021','minhnhat.cao@gmail.com',      '1986-07-04','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2025-04-01 10:00:00'),
    (1,2,N'Đặng Thị Phương Thảo', '0912001022','phuongthao.dang@gmail.com',   '1994-02-18','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2025-04-05 11:00:00'),
    (1,1,N'Lưu Quang Hải',        '0912001023','quanghai.luu@gmail.com',      '1989-10-22','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2025-04-10 09:00:00'),
    (1,2,N'Tô Thị Minh Châu',     '0912001024','minhchau.to@gmail.com',       '1992-06-30','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2025-04-12 14:00:00'),
    (1,1,N'Trương Đức Anh',       '0912001025','ducanh.truong@gmail.com',     '1990-01-13','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2025-04-15 10:30:00'),
    (1,2,N'Hồ Thị Ngọc Huyền',   '0912001026','ngochuyen.ho@gmail.com',      '1996-08-25','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2025-04-18 08:00:00'),
    (1,1,N'Bùi Văn Kiên',         '0912001027','kien.bui@gmail.com',          '1985-04-07','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2025-04-20 13:00:00'),
    (1,2,N'Lâm Thị Kim Liên',     '0912001028','kimlien.lam@gmail.com',       '1993-12-14','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2025-04-22 09:30:00'),
    (1,1,N'Phan Đình Tuấn Anh',   '0912001029','tuananh.phan@gmail.com',      '1988-09-01','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2025-04-25 11:00:00'),
    (1,2,N'Vương Thị Thanh Nga',  '0912001030','thanhnga.vuong@gmail.com',    '1997-03-19','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2025-04-28 14:30:00'),
    (1,1,N'Nguyễn Hải Đăng',     '0912001031','haidang.ng@gmail.com',        '1991-11-06','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2025-05-01 08:00:00'),
    (1,2,N'Trần Thị Cẩm Tú',     '0912001032','camtu.tran@gmail.com',        '1994-05-23','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2025-05-02 10:00:00'),
    (1,1,N'Lê Trọng Nghĩa',       '0912001033','trongnghia.le@gmail.com',     '1987-08-17','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2025-05-03 09:00:00'),
    (1,2,N'Ngô Thị Hương Giang',  '0912001034','huonggiang.ngo@gmail.com',    '1995-02-08','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2025-05-04 11:30:00'),
    (1,1,N'Đoàn Văn Phong',       '0912001035','vanphong.doan@gmail.com',     '1989-07-27','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2025-05-05 14:00:00'),
    (1,2,N'Lại Thị Bảo Ngọc',    '0912001036','baongoc.lai@gmail.com',       '1992-04-11','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2025-05-06 08:30:00'),
    (1,1,N'Khổng Minh Trí',       '0912001037','minhtri.khong@gmail.com',     '1990-10-03','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2025-05-07 10:00:00'),
    (1,2,N'Hà Thị Thu Hằng',      '0912001038','thuhang.ha@gmail.com',        '1996-06-16','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2025-05-08 13:00:00'),
    (1,1,N'Đinh Công Sơn',        '0912001039','congson.dinh@gmail.com',      '1988-01-28','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2025-05-09 09:00:00'),
    (1,2,N'Phạm Thị Diễm Quỳnh', '0912001040','diemquynh.pham@gmail.com',    '1993-09-09','$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',1,'2025-05-10 11:00:00');
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 4 – USER PREFERENCES
══════════════════════════════════════════════════════════════ */
PRINT N'[4/40] Notification.UserPreferences (patch missing rows)...';
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
PRINT N'[5/40] SuperCategories, Categories, Materials, Ages, Origins, Brands, PriceRanges, StatusOrders, ReactionTypes...';

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
    (1,       0,   199000,'2024-01-05 08:00:00'),
    (2,  200000,   499000,'2024-01-05 08:00:00'),
    (3,  500000,   999000,'2024-01-05 08:00:00'),
    (4, 1000000,  4999000,'2024-01-05 08:00:00'),
    (5, 5000000, 99999000,'2024-01-05 08:00:00');
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
    (9,'Refunded',   N'Đã hoàn tiền');
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

/* ══════════════════════════════════════════════════════════════
   SECTION 6 – PROVINCES / DISTRICTS / WARDS
══════════════════════════════════════════════════════════════ */
PRINT N'[6/40] Provinces, Districts, Wards...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[Provinces] WHERE ProvinceId = 1)
    INSERT INTO [dbo].[Provinces] (ProvinceId, ProvinceName, ProvinceCode, IsActive) VALUES
    (1, N'TP. Hồ Chí Minh', 'HCM', 1),
    (2, N'Hà Nội',          'HNI', 1),
    (3, N'Đà Nẵng',        'DNG', 1),
    (4, N'Cần Thơ',        'CTH', 1),
    (5, N'Bình Dương',     'BDG', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Districts] WHERE DistrictId = 101)
    INSERT INTO [dbo].[Districts] (DistrictId, ProvinceId, DistrictName, IsActive) VALUES
    (101, 1, N'Quận 1',         1),(102, 1, N'Quận 3',         1),(103, 1, N'Quận 5',         1),
    (104, 1, N'Quận 7',         1),(105, 1, N'Thủ Đức',       1),(201, 2, N'Hoàn Kiếm',     1),
    (202, 2, N'Đống Đa',       1),(203, 2, N'Thanh Xuân',    1),(204, 2, N'Cầu Giấy',      1),
    (205, 2, N'Hà Đông',       1),(301, 3, N'Hải Châu',      1),(302, 3, N'Sơn Trà',       1),
    (401, 4, N'Ninh Kiều',     1),(501, 5, N'Thuận An',      1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Wards] WHERE WardCode = 'W10101')
    INSERT INTO [dbo].[Wards] (WardCode, DistrictId, WardName, IsActive) VALUES
    ('W10101',101,N'Phường Bến Nghé',         1),('W10102',101,N'Phường Đa Kao',           1),
    ('W10201',102,N'Phường Võ Thị Sáu',      1),('W10301',103,N'Phường Nguyễn Cư Trinh',  1),
    ('W10401',104,N'Phường Tân Phú',         1),('W10501',105,N'Phường Linh Chiểu',      1),
    ('W20101',201,N'Phường Hoàn Kiếm',       1),('W20201',202,N'Phường Văn Miếu',        1),
    ('W20301',203,N'Phường Nhân Chính',      1),('W20401',204,N'Phường Dịch Vọng',       1),
    ('W20501',205,N'Phường Mộ Lao',         1),('W30101',301,N'Phường Hải Châu 1',     1),
    ('W40101',401,N'Phường An Hòa',         1),('W50101',501,N'Phường An Phú',         1);
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 7 – ADDRESSES
══════════════════════════════════════════════════════════════ */
PRINT N'[7/40] Addresses...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[Addresses])
BEGIN
    DECLARE @addrData TABLE (Email VARCHAR(100), AddressLine NVARCHAR(500),
        WardCode VARCHAR(20), DistrictId INT, ProvinceId INT, IsDefault BIT);
    INSERT INTO @addrData VALUES
    ('lananh.pham@gmail.com',     N'72 Lê Lợi, P. Bến Nghé',             'W10101',101,1,1),
    ('lananh.pham@gmail.com',     N'12 Nguyễn Huệ, P. Đa Kao',           'W10102',101,1,0),
    ('hung.nguyen88@gmail.com',   N'15 Trần Phú, P. Mộ Lao',             'W20501',205,2,1),
    ('hung.nguyen88@gmail.com',   N'88 Lê Lợi, P. Văn Miếu',             'W20201',202,2,0),
    ('bauchau.vu@gmail.com',      N'88 Nguyễn Trãi, P. Nhân Chính',      'W20301',203,2,1),
    ('mtuando@outlook.com',       N'34 Đinh Tiên Hoàng, P. Đa Kao',      'W10102',101,1,1),
    ('thuha.hoang@gmail.com',     N'120 Lê Văn Lương, P. Nhân Chính',    'W20301',203,2,1),
    ('vanphuc.le@gmail.com',      N'9 Đinh Lễ, P. Hoàn Kiếm',            'W20101',201,2,1),
    ('ndiep.tran@gmail.com',      N'55 Hoàng Diệu 2, P. Linh Chiểu',    'W10501',105,1,1),
    ('mylinh.bui2024@gmail.com',  N'210 Nguyễn Văn Cừ, P. Nguyễn Cư Trinh','W10301',103,1,1),
    ('quockhanh.phan@gmail.com',  N'17 Võ Văn Tần, P. Võ Thị Sáu',      'W10201',102,1,1),
    ('thanh.tuyen.ngo@gmail.com', N'33 Bùi Thị Xuân, P. Bến Nghé',       'W10101',101,1,1),
    ('dvlong.toys@gmail.com',     N'45 Lý Thường Kiệt, P. Hoàn Kiếm',   'W20101',201,2,1),
    ('kimoanh.trinh@gmail.com',   N'6 Trần Hưng Đạo, P. Bến Nghé',       'W10101',101,1,1),
    ('ducthinh.huynh@gmail.com',  N'99 Hải Châu, P. Hải Châu 1',         'W30101',301,3,1),
    ('hongvan.mai@gmail.com',     N'7 Lê Duẩn, P. Hải Châu 1',           'W30101',301,3,1),
    ('minhquan.dinh@icloud.com',  N'12 Ngô Quyền, P. An Hòa',            'W40101',401,4,1),
    ('xuanmai.ly@gmail.com',      N'88 Nguyễn An Ninh, P. An Phú',       'W50101',501,5,1),
    ('vantai.chau@gmail.com',     N'22 Pasteur, P. Đa Kao',              'W10102',101,1,1),
    ('yennhi.ng@gmail.com',       N'5 Bạch Đằng, P. Dịch Vọng',         'W20401',204,2,1),
    ('thanhsang.vo@gmail.com',    N'18 Nguyễn Thị Minh Khai, P. Bến Nghé','W10101',101,1,1),
    ('bichtram.phung@gmail.com',  N'30 Đinh Công Tráng, P. Võ Thị Sáu', 'W10201',102,1,1),
    ('minhnhat.cao@gmail.com',    N'44 Lê Thánh Tôn, P. Bến Nghé',       'W10101',101,1,1),
    ('phuongthao.dang@gmail.com', N'66 Hoàng Diệu 2, P. Linh Chiểu',    'W10501',105,1,1),
    ('quanghai.luu@gmail.com',    N'10 Trần Phú, P. Mộ Lao',             'W20501',205,2,1),
    ('minhchau.to@gmail.com',     N'23 Nguyễn Huệ, P. Bến Nghé',         'W10101',101,1,1),
    ('ducanh.truong@gmail.com',   N'77 Lê Văn Lương, P. Nhân Chính',     'W20301',203,2,1),
    ('ngochuyen.ho@gmail.com',    N'9 Cách Mạng Tháng 8, P. Bến Nghé',   'W10101',101,1,1),
    ('kien.bui@gmail.com',        N'55 Hoàng Diệu, P. Hoàn Kiếm',        'W20101',201,2,1),
    ('kimlien.lam@gmail.com',     N'12 Lý Tự Trọng, P. Nguyễn Cư Trinh','W10301',103,1,1),
    ('tuananh.phan@gmail.com',    N'88 Trần Bình Trọng, P. Võ Thị Sáu', 'W10201',102,1,1),
    ('thanhnga.vuong@gmail.com',  N'4 Công Xã Paris, P. Bến Nghé',        'W10101',101,1,1),
    ('haidang.ng@gmail.com',      N'15 Phan Bội Châu, P. Dịch Vọng',     'W20401',204,2,1),
    ('camtu.tran@gmail.com',      N'61 Nguyễn Trãi, P. Nhân Chính',      'W20301',203,2,1),
    ('trongnghia.le@gmail.com',   N'38 Bà Triệu, P. Hoàn Kiếm',          'W20101',201,2,1),
    ('huonggiang.ngo@gmail.com',  N'20 Trần Hưng Đạo, P. Linh Chiểu',   'W10501',105,1,1),
    ('vanphong.doan@gmail.com',   N'7 Nguyễn Chí Thanh, P. Hoàn Kiếm',  'W20101',201,2,1),
    ('baongoc.lai@gmail.com',     N'50 Lê Lợi, P. Bến Nghé',             'W10101',101,1,1),
    ('minhtri.khong@gmail.com',   N'3 Hai Bà Trưng, P. Mộ Lao',          'W20501',205,2,1),
    ('thuhang.ha@gmail.com',      N'19 Vạn Xuân, P. Võ Thị Sáu',        'W10201',102,1,1),
    ('congson.dinh@gmail.com',    N'71 Hải Châu, P. Hải Châu 1',         'W30101',301,3,1),
    ('diemquynh.pham@gmail.com',  N'88 Pasteur, P. Đa Kao',              'W10102',101,1,1);

    INSERT INTO [dbo].[Addresses]
        (AccountID, RecipientName, PhoneNumber, AddressLine,
         WardCode, DistrictId, ProvinceId, IsDefault, CreatedAt)
    SELECT a.AccountID, a.AccountName, a.PhoneNumber,
           d.AddressLine, d.WardCode, d.DistrictId, d.ProvinceId, d.IsDefault, GETDATE()
    FROM Accounts a
    JOIN @addrData d ON a.Email = d.Email;
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 8 – PRODUCTS (30 sản phẩm)
══════════════════════════════════════════════════════════════ */
PRINT N'[8/40] Products...';
SET IDENTITY_INSERT [dbo].[Products] ON;
IF NOT EXISTS (SELECT 1 FROM [dbo].[Products] WHERE ProductID = 1)
    INSERT INTO [dbo].[Products]
        (ProductID,ProductName,Price,Quantity,ProductStatus,CategoryID,BrandID,PriceRangeID,StockThreshold,CreatedAt)
    VALUES
    ( 1,N'Lego City Trạm Cảnh Sát Trung Tâm 668 Mảnh',        1290000, 45,'Active', 1,1,4,10,'2024-02-01 08:00:00'),
    ( 2,N'Lego Technic Siêu Xe Bugatti Chiron 3599 Mảnh',      5990000,  8,'Active', 1,1,5, 3,'2024-02-03 08:00:00'),
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
PRINT N'[9/40] ProductDetails...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[ProductDetails] WHERE ProductID = 1)
    INSERT INTO [dbo].[ProductDetails] (ProductID, Description, MaterialID, AgeID, SexID, OriginID)
    VALUES
    ( 1,N'Bộ Lego City 668 mảnh tái hiện trạm cảnh sát 3 tầng. Nhựa ABS EN71. Phù hợp 6+ tuổi.',1,4,1,5),
    ( 2,N'Lego Technic 3599 mảnh siêu xe Bugatti Chiron tỉ lệ 1:8. Động cơ W16 mô phỏng. 18+ tuổi.',1,5,1,5),
    ( 3,N'Lego Friends nhà nghỉ dưỡng với hồ bơi, spa, và 5 nhân vật. 8+ tuổi.',1,4,2,5),
    ( 4,N'36 khối gỗ MDF sơn nước an toàn. Bo góc 5mm. Phù hợp 1–5 tuổi.',2,3,3,1),
    ( 5,N'52 khối gỗ bảng chữ cái A-Z, a-z. Màu sắc rực rỡ. 2-5 tuổi.',2,2,3,1),
    ( 6,N'120 thẻ 4D tích hợp AR – quét app xem 60 loài động vật sống động.',3,3,3,2),
    ( 7,N'100 thẻ học toán từ 1-100. Hai mặt: số đếm và bài tập. 3-8 tuổi.',3,3,3,1),
    ( 8,N'Kính hiển vi ScienceMax 3 mức zoom (40x/100x/400x), đèn LED, 12 tiêu bản.',1,4,3,3),
    ( 9,N'Bộ thí nghiệm núi lửa phun trào gồm 12 thí nghiệm an toàn. 6+ tuổi.',1,4,3,3),
    (10,N'Kính thiên văn 2 mức phóng đại (50x/100x). Kèm chân đế và bản đồ sao. 8+ tuổi.',1,4,3,3),
    (11,N'Xe chòi chân hình vịt Donald: đèn LED + nhạc. Tải tối đa 30kg.',5,3,3,2),
    (12,N'Xe đạp 3 bánh Disney Princess khung thép. Có tay vịn + ô che nắng. 2-5 tuổi.',4,3,2,2),
    (13,N'Bóng cao su thiên nhiên size 5 họa tiết Boho. Lưu hóa 2 lớp.',5,3,3,1),
    (14,N'Bóng đá FIFA Pro cao su lưu hóa size 4. Chống thấm nước.',5,4,1,3),
    (15,N'Diều đại bàng sải cánh 1.4m, khung carbon, vải polyester 210T.',2,4,1,1),
    (16,N'Diều rồng 3D sải cánh 1.8m. Khung carbon siêu bền. Gió 3-8 Beaufort.',2,4,1,1),
    (17,N'Gấu bông khủng long Rex 80cm vải nhung siêu mềm, bông PP chống nấm.',3,2,2,2),
    (18,N'Gấu trúc Panda 60cm nằm, vải nhung cao cấp, bông PP, giặt máy được.',3,2,3,2),
    (19,N'Thỏ tai dài 45cm màu pastel. Vải nhung Hàn Quốc. Kèm hộp quà.',3,2,2,2),
    (20,N'Barbie Dreamtopia Tiên Cá chính hãng Mattel. Tóc gradient tím-hồng, 3 bộ trang phục.',1,3,2,3),
    (21,N'Barbie Fashionista set 6 trang phục đa phong cách. 3+ tuổi.',1,3,2,3),
    (22,N'Siêu Nhân Gao Red Ranger Bandai Nhật, cao 18cm, 24 khớp xoay. Limited edition.',5,5,1,4),
    (23,N'Kamen Rider Zero-One SHFiguarts cao 15cm, 30 khớp. Kèm 8 bàn tay thay thế.',5,5,1,4),
    (24,N'T-Rex tỉ lệ 1:10, cao 25cm, dài 45cm. Hợp kim nhôm-nhựa ABS. Miệng lò xo.',4,4,1,3),
    (25,N'Bộ 6 khủng long Jurassic World mini cao 8-12cm. Nhựa ABS mềm.',1,3,1,3),
    (26,N'RC Traxxas TRX-Mini 1:16, brushless 2838KV, max 45km/h. Pin LiPo.',4,4,1,3),
    (27,N'Xe RC drift bánh nhôm CNC. Tốc độ 30km/h. Sạc USB 90 phút.',4,4,1,2),
    (28,N'Trực thăng RC Gyro 4 kênh 2.4GHz, con quay 6 trục. Bay 12-15 phút.',1,5,1,2),
    (29,N'PlayDoh 24 màu chính hãng Hasbro, mỗi hộp 85g. Không độc, không gluten.',3,3,3,3),
    (30,N'Bảng LCD 10 inch viết-vẽ-xóa tức thì. 1 pin CR2025 dùng 50.000 lần.',1,2,3,2);
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 10 – PRODUCT IMAGES
══════════════════════════════════════════════════════════════ */
PRINT N'[10/40] ProductImages...';
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
PRINT N'[11/40] BlockReasons...';
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
PRINT N'[12/40] UserBlockHistory...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[UserBlockHistory])
BEGIN
    DECLARE @admBlk INT = (SELECT TOP 1 AccountID FROM Accounts WHERE Email='admin@toyhouse.vn');
    DECLARE @br2    INT = (SELECT TOP 1 BlockReasonID FROM BlockReasons WHERE Content=N'Dấu hiệu gian lận');
    DECLARE @br5    INT = (SELECT TOP 1 BlockReasonID FROM BlockReasons WHERE Content=N'Lạm dụng voucher');

    INSERT INTO [dbo].[UserBlockHistory]
        (AccountID, BlockedBy, BlockReasonID, Note, BlockedAt, BlockedUntil, UnblockedAt)
    VALUES
    (
        (SELECT TOP 1 AccountID FROM Accounts WHERE Email='trongnghia.le@gmail.com'),
        @admBlk, @br2,
        N'Tài khoản báo cáo không nhận hàng 2 lần liên tiếp trong tháng 3/2026',
        '2026-04-04 09:00:00', '2026-04-11 09:00:00', '2026-04-11 09:00:00'
    ),
    (
        (SELECT TOP 1 AccountID FROM Accounts WHERE Email='ngochuyen.ho@gmail.com'),
        @admBlk, @br5,
        N'Tạo 3 tài khoản phụ để dùng voucher WELCOME10',
        '2026-03-20 10:00:00', '2026-06-20 10:00:00', NULL
    );
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 13 – PROMOTIONS
══════════════════════════════════════════════════════════════ */
PRINT N'[13/40] Promotions...';
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
PRINT N'[14/40] PromotionTimeSlots...';
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
PRINT N'[15/40] ProductPromotions (DISCOUNT)...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[ProductPromotions])
BEGIN
    DECLARE @pSale INT = (SELECT TOP 1 PromotionID FROM Promotions WHERE PromotionName=N'Sale Hè Rực Rỡ 2026');
    DECLARE @pBTS  INT = (SELECT TOP 1 PromotionID FROM Promotions WHERE PromotionName=N'Back To School 2026');
    INSERT INTO [dbo].[ProductPromotions]
        (ProductID,PromotionID,SalePrice,DiscountPercent,SaleQuantity,SoldQuantity,ReservedQuantity,IsActive,CreatedAt)
    VALUES
    ( 1,@pSale, 1099000,14.88, 30, 8,0,1,'2026-03-20 02:00:00'),
    ( 2,@pSale, 4990000,16.69,  5, 1,0,1,'2026-03-20 02:00:00'),
    ( 3,@pSale, 1290000,18.87, 15, 3,0,1,'2026-03-20 02:00:00'),
    (11,@pSale,  449000,17.61, 20, 5,0,1,'2026-03-20 02:00:00'),
    (12,@pSale,  750000,15.73, 10, 2,0,1,'2026-03-20 02:00:00'),
    (17,@pSale,  699000,16.79, 15, 4,0,1,'2026-03-20 02:00:00'),
    (20,@pSale,  299000,16.71, 30, 9,0,1,'2026-03-20 02:00:00'),
    (22,@pSale,  490000,17.65, 10, 2,0,1,'2026-03-20 02:00:00'),
    (26,@pSale,  790000,16.40, 20, 6,0,1,'2026-03-20 02:00:00'),
    (28,@pSale, 1250000,16.11,  8, 1,0,1,'2026-03-20 02:00:00'),
    ( 6,@pBTS,   69000,18.82,NULL,0,0,1,'2026-07-01 02:00:00'),
    ( 7,@pBTS,  149000,18.92,NULL,0,0,1,'2026-07-01 02:00:00'),
    (29,@pBTS,   70000,20.45,NULL,0,0,1,'2026-07-01 02:00:00'),
    (30,@pBTS,  139000,20.57,NULL,0,0,1,'2026-07-01 02:00:00'),
    ( 8,@pBTS,  299000,20.27,NULL,0,0,1,'2026-07-01 02:00:00');
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 16 – PROMOTION PRODUCT SLOTS (FLASH_SALE)
══════════════════════════════════════════════════════════════ */
PRINT N'[16/40] PromotionProductSlots (FLASH_SALE)...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[PromotionProductSlots])
BEGIN
    DECLARE @s1 INT=(SELECT TOP 1 TimeSlotID FROM PromotionTimeSlots WHERE StartAt='2026-05-04 02:00:00');
    DECLARE @s2 INT=(SELECT TOP 1 TimeSlotID FROM PromotionTimeSlots WHERE StartAt='2026-05-04 13:00:00');
    DECLARE @s3 INT=(SELECT TOP 1 TimeSlotID FROM PromotionTimeSlots WHERE StartAt='2026-05-05 02:00:00');
    DECLARE @s4 INT=(SELECT TOP 1 TimeSlotID FROM PromotionTimeSlots WHERE StartAt='2026-05-05 13:00:00');
    INSERT INTO [dbo].[PromotionProductSlots]
        (TimeSlotID,ProductID,SalePrice,DiscountPercent,SaleQuantity,SoldQuantity,ReservedQuantity,IsActive,CreatedAt)
    VALUES
    (@s1,24,197000,50.13,10,0,0,1,'2026-04-15 03:00:00'),
    (@s1,26,700000,25.93,15,0,0,1,'2026-04-15 03:00:00'),
    (@s1,13, 39000,54.12,50,0,0,1,'2026-04-15 03:00:00'),
    (@s2,22,297000,50.08,20,0,0,1,'2026-04-15 03:00:00'),
    (@s2,28,990000,33.56, 8,0,0,1,'2026-04-15 03:00:00'),
    (@s2,17,420000,50.00,12,0,0,1,'2026-04-15 03:00:00'),
    (@s3, 1,645000,50.00,20,0,0,1,'2026-04-15 03:00:00'),
    (@s3,20,179000,50.14,30,0,0,1,'2026-04-15 03:00:00'),
    (@s3,11,272000,50.09,15,0,0,1,'2026-04-15 03:00:00'),
    (@s4, 2,2995000,50.00,5,0,0,1,'2026-04-15 03:00:00'),
    (@s4,23,445000,50.06, 8,0,0,1,'2026-04-15 03:00:00'),
    (@s4,29, 40000,54.55,100,0,0,1,'2026-04-15 03:00:00');
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 17 – VOUCHERS
══════════════════════════════════════════════════════════════ */
PRINT N'[17/40] Vouchers...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[Vouchers] WHERE VoucherCode='SHIP0626')
    INSERT INTO [dbo].[Vouchers]
        (VoucherCode,VoucherName,VoucherDescription,DiscountType,DiscountValue,
         MaxDiscountCap,DiscountTarget,MinOrderAmount,TotalQuantity,UsedQuantity,
         MaxUsagePerUser,StartDate,EndDate,Status,IsDeleted,CreatedAt)
    VALUES
    ('SHIP0626',  N'Freeship Tháng 6',        N'Miễn phí vận chuyển tối đa 50.000đ – đơn từ 200.000đ',
     'FIXED',   50000,  50000,'SHIPPING_FEE', 200000,1000, 12,1,'2026-06-01','2026-06-30','Active',0,'2026-05-25 08:00:00'),
    ('SHIP1226',  N'Freeship Noel',            N'Freeship toàn quốc mùa Giáng Sinh',
     'FIXED',   30000,  30000,'SHIPPING_FEE', 300000, 500,  8,1,'2026-12-20','2026-12-31','Scheduled',0,'2026-12-01 08:00:00'),
    ('WELCOME10', N'Giảm 10% Chào Khách Mới', N'Giảm 10% lần mua đầu tiên',
     'PERCENTAGE',10,   200000,'ORDER_TOTAL',      0, 500,  1,1,'2026-01-01','2026-12-31','Active',0,'2026-01-01 08:00:00'),
    ('VIP200K',   N'Ưu Đãi Khách VIP 200K',   N'Áp dụng đơn từ 1.000.000đ',
     'FIXED',  200000,    NULL,'ORDER_TOTAL',1000000, 100,  1,1,'2026-04-01','2026-06-30','Active',0,'2026-03-20 09:00:00'),
    ('SUMMER15',  N'Summer Sale 15%',          N'Giảm 15% đồ chơi ngoài trời',
     'PERCENTAGE',15,   300000,'ORDER_TOTAL', 500000, 300,  1,1,'2026-06-01','2026-08-31','Scheduled',0,'2026-05-20 09:00:00'),
    ('KIDSBDAY',  N'Quà Sinh Nhật 50K',        N'Giảm 50.000đ cho đơn từ 300.000đ',
     'FIXED',   50000,    NULL,'ORDER_TOTAL', 300000,1000,  1,1,'2026-01-01','2026-12-31','Active',0,'2026-01-01 08:00:00'),
    ('FLASHVIP',  N'Flash Sale VIP – Thêm 5%',N'Thêm 5% cho khách VIP trong Flash Sale',
     'PERCENTAGE', 5,   100000,'ORDER_TOTAL', 500000, 200,  1,1,'2026-05-04','2026-05-06','Scheduled',0,'2026-04-20 08:00:00');
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 18 – ORDERS (50 đơn hàng)
══════════════════════════════════════════════════════════════ */
PRINT N'[18/40] Orders (50 đơn hàng)...';

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
('lananh.pham@gmail.com','ORD-2026-00001',7,'2026-02-14 10:30:00','2026-02-14 14:00:00','2026-02-15 09:00:00','2026-02-16 11:00:00','2026-02-17 08:00:00',NULL,'WALLET','PAID','2026-02-14 10:30:00',1290000,0,0,1290000,NULL,'PAY-00001',NULL,N'Phạm Thị Lan Anh','0912001001',N'72 Lê Lợi, P. Bến Nghé','W10101',101,1),
('hung.nguyen88@gmail.com','ORD-2026-00002',4,'2026-04-10 08:15:00','2026-04-10 11:00:00','2026-04-11 09:30:00',NULL,NULL,NULL,'SE_PAY','PAID','2026-04-10 08:15:00',945000,30000,0,975000,NULL,'PAY-00002','GHN-00002',N'Nguyễn Văn Hùng','0912001002',N'15 Trần Phú, P. Mộ Lao','W20501',205,2),
('bauchau.vu@gmail.com','ORD-2026-00003',8,'2026-03-05 16:45:00',NULL,NULL,NULL,NULL,'2026-03-06 17:00:00','BANK_TRANSFER','PENDING',NULL,840000,25000,0,865000,N'Khách chưa chuyển khoản sau 24h',NULL,NULL,N'Vũ Thị Bảo Châu','0912001003',N'88 Nguyễn Trãi, P. Nhân Chính','W20301',203,2),
('mtuando@outlook.com','ORD-2026-00004',2,'2026-04-18 09:00:00','2026-04-18 10:30:00',NULL,NULL,NULL,NULL,'SHIP_COD','COD_PENDING',NULL,1698000,35000,0,1733000,NULL,NULL,NULL,N'Đỗ Minh Tuấn','0912001004',N'34 Đinh Tiên Hoàng, P. Đa Kao','W10102',101,1),
('thuha.hoang@gmail.com','ORD-2026-00005',6,'2026-04-01 13:20:00','2026-04-01 15:00:00','2026-04-02 09:00:00','2026-04-03 14:30:00',NULL,NULL,'WALLET','PAID','2026-04-01 13:20:00',1199000,0,0,1199000,NULL,'PAY-00005',NULL,N'Hoàng Thị Thu Hà','0912001005',N'120 Lê Văn Lương, P. Nhân Chính','W20301',203,2),
('vanphuc.le@gmail.com','ORD-2026-00006',1,'2026-04-22 11:00:00',NULL,NULL,NULL,NULL,NULL,'SE_PAY','PAID','2026-04-22 11:00:00',1490000,0,0,1490000,NULL,'PAY-00006',NULL,N'Lê Văn Phúc','0912001006',N'9 Đinh Lễ, P. Hoàn Kiếm','W20101',201,2),
('ndiep.tran@gmail.com','ORD-2026-00007',7,'2026-03-20 09:30:00','2026-03-20 11:00:00','2026-03-21 08:00:00','2026-03-22 10:00:00','2026-03-23 08:00:00',NULL,'WALLET','PAID','2026-03-20 09:30:00',535000,30000,0,565000,NULL,'PAY-00007',NULL,N'Trần Ngọc Diệp','0912001007',N'55 Hoàng Diệu 2, P. Linh Chiểu','W10501',105,1),
('mylinh.bui2024@gmail.com','ORD-2026-00008',3,'2026-04-20 14:00:00','2026-04-20 16:00:00',NULL,NULL,NULL,NULL,'SE_PAY','PAID','2026-04-20 14:00:00',595000,20000,0,615000,NULL,'PAY-00008',NULL,N'Bùi Thị Mỹ Linh','0912001008',N'210 Nguyễn Văn Cừ, P. Nguyễn Cư Trinh','W10301',103,1),
('quockhanh.phan@gmail.com','ORD-2026-00009',2,'2026-04-21 16:30:00','2026-04-22 09:00:00',NULL,NULL,NULL,NULL,'SHIP_COD','COD_PENDING',NULL,770000,30000,0,800000,NULL,NULL,NULL,N'Phan Quốc Khánh','0912001009',N'17 Võ Văn Tần, P. Võ Thị Sáu','W10201',102,1),
('thanh.tuyen.ngo@gmail.com','ORD-2026-00010',7,'2026-03-15 10:00:00','2026-03-15 12:00:00','2026-03-16 09:00:00','2026-03-17 11:00:00','2026-03-18 08:00:00',NULL,'WALLET','PAID','2026-03-15 10:00:00',2235000,0,200000,2035000,NULL,'PAY-00010',NULL,N'Ngô Thị Thanh Tuyền','0912001010',N'33 Bùi Thị Xuân, P. Bến Nghé','W10101',101,1),
('dvlong.toys@gmail.com','ORD-2026-00011',7,'2026-02-20 09:00:00','2026-02-20 11:00:00','2026-02-21 09:00:00','2026-02-22 14:00:00','2026-02-23 08:00:00',NULL,'WALLET','PAID','2026-02-20 09:00:00',840000,0,0,840000,NULL,'PAY-00011',NULL,N'Dương Văn Long','0912001011',N'45 Lý Thường Kiệt, P. Hoàn Kiếm','W20101',201,2),
('kimoanh.trinh@gmail.com','ORD-2026-00012',7,'2026-03-01 10:00:00','2026-03-01 12:00:00','2026-03-02 09:00:00','2026-03-03 15:00:00','2026-03-04 08:00:00',NULL,'SE_PAY','PAID','2026-03-01 10:00:00',420000,25000,0,445000,NULL,'PAY-00012',NULL,N'Trịnh Thị Kim Oanh','0912001012',N'6 Trần Hưng Đạo, P. Bến Nghé','W10101',101,1),
('ducthinh.huynh@gmail.com','ORD-2026-00013',6,'2026-04-05 08:00:00','2026-04-05 10:00:00','2026-04-06 09:00:00','2026-04-07 16:00:00',NULL,NULL,'BANK_TRANSFER','PAID','2026-04-05 08:00:00',280000,20000,0,300000,NULL,'PAY-00013',NULL,N'Huỳnh Đức Thịnh','0912001013',N'99 Hải Châu, P. Hải Châu 1','W30101',301,3),
('hongvan.mai@gmail.com','ORD-2026-00014',4,'2026-04-12 14:00:00','2026-04-12 16:00:00','2026-04-13 09:30:00',NULL,NULL,NULL,'SE_PAY','PAID','2026-04-12 14:00:00',650000,25000,0,675000,NULL,'PAY-00014','GHN-00014',N'Mai Thị Hồng Vân','0912001014',N'7 Lê Duẩn, P. Hải Châu 1','W30101',301,3),
('minhquan.dinh@icloud.com','ORD-2026-00015',8,'2026-04-08 09:00:00',NULL,NULL,NULL,NULL,'2026-04-08 11:00:00','WALLET','REFUNDED',NULL,395000,20000,0,415000,N'Sản phẩm hết hàng sau khi đặt',NULL,NULL,N'Đinh Minh Quân','0912001015',N'12 Ngô Quyền, P. An Hòa','W40101',401,4),
('xuanmai.ly@gmail.com','ORD-2026-00016',7,'2026-01-10 09:00:00','2026-01-10 11:00:00','2026-01-11 09:00:00','2026-01-12 14:00:00','2026-01-13 08:00:00',NULL,'WALLET','PAID','2026-01-10 09:00:00',359000,0,0,359000,NULL,'PAY-00016',NULL,N'Lý Thị Xuân Mai','0912001016',N'88 Nguyễn An Ninh, P. An Phú','W50101',501,5),
('vantai.chau@gmail.com','ORD-2026-00017',7,'2026-01-18 10:00:00','2026-01-18 12:00:00','2026-01-19 09:00:00','2026-01-20 15:00:00','2026-01-21 08:00:00',NULL,'SE_PAY','PAID','2026-01-18 10:00:00',490000,25000,0,515000,NULL,'PAY-00017',NULL,N'Châu Văn Tài','0912001017',N'22 Pasteur, P. Đa Kao','W10102',101,1),
('yennhi.ng@gmail.com','ORD-2026-00018',6,'2026-02-05 08:00:00','2026-02-05 10:00:00','2026-02-06 09:00:00','2026-02-07 16:00:00',NULL,NULL,'WALLET','PAID','2026-02-05 08:00:00',185000,20000,0,205000,NULL,'PAY-00018',NULL,N'Nguyễn Thị Yến Nhi','0912001018',N'5 Bạch Đằng, P. Dịch Vọng','W20401',204,2),
('thanhsang.vo@gmail.com','ORD-2026-00019',4,'2026-04-25 11:00:00','2026-04-25 13:00:00','2026-04-26 09:00:00',NULL,NULL,NULL,'SE_PAY','PAID','2026-04-25 11:00:00',890000,30000,0,920000,NULL,'PAY-00019','GHN-00019',N'Võ Thanh Sang','0912001019',N'18 Nguyễn Thị Minh Khai, P. Bến Nghé','W10101',101,1),
('bichtram.phung@gmail.com','ORD-2026-00020',3,'2026-04-28 09:00:00','2026-04-28 11:00:00',NULL,NULL,NULL,NULL,'SHIP_COD','COD_PENDING',NULL,595000,25000,0,620000,NULL,NULL,NULL,N'Phùng Thị Bích Trâm','0912001020',N'30 Đinh Công Tráng, P. Võ Thị Sáu','W10201',102,1),
('minhnhat.cao@gmail.com','ORD-2026-00021',7,'2026-01-25 09:00:00','2026-01-25 11:00:00','2026-01-26 09:00:00','2026-01-27 14:00:00','2026-01-28 08:00:00',NULL,'WALLET','PAID','2026-01-25 09:00:00',280000,20000,0,300000,NULL,'PAY-00021',NULL,N'Cao Minh Nhật','0912001021',N'44 Lê Thánh Tôn, P. Bến Nghé','W10101',101,1),
('phuongthao.dang@gmail.com','ORD-2026-00022',7,'2026-02-10 10:00:00','2026-02-10 12:00:00','2026-02-11 09:00:00','2026-02-12 15:00:00','2026-02-13 08:00:00',NULL,'SE_PAY','PAID','2026-02-10 10:00:00',650000,25000,0,675000,NULL,'PAY-00022',NULL,N'Đặng Thị Phương Thảo','0912001022',N'66 Hoàng Diệu 2, P. Linh Chiểu','W10501',105,1),
('quanghai.luu@gmail.com','ORD-2026-00023',6,'2026-04-15 08:00:00','2026-04-15 10:00:00','2026-04-16 09:00:00','2026-04-17 16:00:00',NULL,NULL,'WALLET','PAID','2026-04-15 08:00:00',420000,20000,0,440000,NULL,'PAY-00023',NULL,N'Lưu Quang Hải','0912001023',N'10 Trần Phú, P. Mộ Lao','W20501',205,2),
('minhchau.to@gmail.com','ORD-2026-00024',2,'2026-04-26 14:00:00','2026-04-27 09:00:00',NULL,NULL,NULL,NULL,'SHIP_COD','COD_PENDING',NULL,1290000,35000,0,1325000,NULL,NULL,NULL,N'Tô Thị Minh Châu','0912001024',N'23 Nguyễn Huệ, P. Bến Nghé','W10101',101,1),
('ducanh.truong@gmail.com','ORD-2026-00025',7,'2026-03-10 09:00:00','2026-03-10 11:00:00','2026-03-11 09:00:00','2026-03-12 14:00:00','2026-03-13 08:00:00',NULL,'BANK_TRANSFER','PAID','2026-03-10 09:00:00',890000,0,0,890000,NULL,'PAY-00025',NULL,N'Trương Đức Anh','0912001025',N'77 Lê Văn Lương, P. Nhân Chính','W20301',203,2),
('ngochuyen.ho@gmail.com','ORD-2026-00026',8,'2026-03-18 10:00:00',NULL,NULL,NULL,NULL,'2026-03-18 14:00:00','SE_PAY','FAILED',NULL,345000,20000,0,365000,N'Thanh toán thất bại, tự động hủy',NULL,NULL,N'Hồ Thị Ngọc Huyền','0912001026',N'9 Cách Mạng Tháng 8, P. Bến Nghé','W10101',101,1),
('kien.bui@gmail.com','ORD-2026-00027',7,'2026-02-28 09:00:00','2026-02-28 11:00:00','2026-03-01 09:00:00','2026-03-02 15:00:00','2026-03-03 08:00:00',NULL,'WALLET','PAID','2026-02-28 09:00:00',1490000,0,0,1490000,NULL,'PAY-00027',NULL,N'Bùi Văn Kiên','0912001027',N'55 Hoàng Diệu, P. Hoàn Kiếm','W20101',201,2),
('kimlien.lam@gmail.com','ORD-2026-00028',6,'2026-04-20 08:00:00','2026-04-20 10:00:00','2026-04-21 09:00:00','2026-04-22 16:00:00',NULL,NULL,'SE_PAY','PAID','2026-04-20 08:00:00',375000,20000,0,395000,NULL,'PAY-00028',NULL,N'Lâm Thị Kim Liên','0912001028',N'12 Lý Tự Trọng, P. Nguyễn Cư Trinh','W10301',103,1),
('tuananh.phan@gmail.com','ORD-2026-00029',3,'2026-04-29 10:00:00','2026-04-29 12:00:00',NULL,NULL,NULL,NULL,'WALLET','PAID','2026-04-29 10:00:00',490000,20000,0,510000,NULL,'PAY-00029',NULL,N'Phan Đình Tuấn Anh','0912001029',N'88 Trần Bình Trọng, P. Võ Thị Sáu','W10201',102,1),
('thanhnga.vuong@gmail.com','ORD-2026-00030',2,'2026-04-30 14:00:00','2026-04-30 16:00:00',NULL,NULL,NULL,NULL,'SHIP_COD','COD_PENDING',NULL,840000,30000,0,870000,NULL,NULL,NULL,N'Vương Thị Thanh Nga','0912001030',N'4 Công Xã Paris, P. Bến Nghé','W10101',101,1),
('haidang.ng@gmail.com','ORD-2026-00031',7,'2026-01-20 09:00:00','2026-01-20 11:00:00','2026-01-21 09:00:00','2026-01-22 14:00:00','2026-01-23 08:00:00',NULL,'WALLET','PAID','2026-01-20 09:00:00',595000,20000,0,615000,NULL,'PAY-00031',NULL,N'Nguyễn Hải Đăng','0912001031',N'15 Phan Bội Châu, P. Dịch Vọng','W20401',204,2),
('camtu.tran@gmail.com','ORD-2026-00032',7,'2026-02-02 10:00:00','2026-02-02 12:00:00','2026-02-03 09:00:00','2026-02-04 15:00:00','2026-02-05 08:00:00',NULL,'SE_PAY','PAID','2026-02-02 10:00:00',650000,25000,0,675000,NULL,'PAY-00032',NULL,N'Trần Thị Cẩm Tú','0912001032',N'61 Nguyễn Trãi, P. Nhân Chính','W20301',203,2),
('trongnghia.le@gmail.com','ORD-2026-00033',8,'2026-04-03 08:00:00',NULL,NULL,NULL,NULL,'2026-04-03 12:00:00','SHIP_COD','PENDING',NULL,285000,20000,0,305000,N'Khách đổi ý không mua',NULL,NULL,N'Lê Trọng Nghĩa','0912001033',N'38 Bà Triệu, P. Hoàn Kiếm','W20101',201,2),
('huonggiang.ngo@gmail.com','ORD-2026-00034',7,'2026-03-05 09:00:00','2026-03-05 11:00:00','2026-03-06 09:00:00','2026-03-07 14:00:00','2026-03-08 08:00:00',NULL,'WALLET','PAID','2026-03-05 09:00:00',420000,0,0,420000,NULL,'PAY-00034',NULL,N'Ngô Thị Hương Giang','0912001034',N'20 Trần Hưng Đạo, P. Linh Chiểu','W10501',105,1),
('vanphong.doan@gmail.com','ORD-2026-00035',6,'2026-04-22 10:00:00','2026-04-22 12:00:00','2026-04-23 09:00:00','2026-04-24 15:00:00',NULL,NULL,'SE_PAY','PAID','2026-04-22 10:00:00',890000,30000,0,920000,NULL,'PAY-00035',NULL,N'Đoàn Văn Phong','0912001035',N'7 Nguyễn Chí Thanh, P. Hoàn Kiếm','W20101',201,2),
('baongoc.lai@gmail.com','ORD-2026-00036',1,'2026-05-10 08:00:00',NULL,NULL,NULL,NULL,NULL,'SE_PAY','PAID','2026-05-10 08:00:00',1290000,0,0,1290000,NULL,'PAY-00036',NULL,N'Lại Thị Bảo Ngọc','0912001036',N'50 Lê Lợi, P. Bến Nghé','W10101',101,1),
('minhtri.khong@gmail.com','ORD-2026-00037',4,'2026-05-08 09:00:00','2026-05-08 11:00:00','2026-05-09 09:00:00',NULL,NULL,NULL,'WALLET','PAID','2026-05-08 09:00:00',840000,0,0,840000,NULL,'PAY-00037','GHN-00037',N'Khổng Minh Trí','0912001037',N'3 Hai Bà Trưng, P. Mộ Lao','W20501',205,2),
('thuhang.ha@gmail.com','ORD-2026-00038',2,'2026-05-09 14:00:00','2026-05-09 16:00:00',NULL,NULL,NULL,NULL,'SHIP_COD','COD_PENDING',NULL,375000,20000,0,395000,NULL,NULL,NULL,N'Hà Thị Thu Hằng','0912001038',N'19 Vạn Xuân, P. Võ Thị Sáu','W10201',102,1),
('congson.dinh@gmail.com','ORD-2026-00039',7,'2026-01-30 09:00:00','2026-01-30 11:00:00','2026-01-31 09:00:00','2026-02-01 14:00:00','2026-02-02 08:00:00',NULL,'BANK_TRANSFER','PAID','2026-01-30 09:00:00',945000,30000,0,975000,NULL,'PAY-00039',NULL,N'Đinh Công Sơn','0912001039',N'71 Hải Châu, P. Hải Châu 1','W30101',301,3),
('diemquynh.pham@gmail.com','ORD-2026-00040',3,'2026-05-10 10:00:00','2026-05-10 12:00:00',NULL,NULL,NULL,NULL,'WALLET','PAID','2026-05-10 10:00:00',490000,20000,0,510000,NULL,'PAY-00040',NULL,N'Phạm Thị Diễm Quỳnh','0912001040',N'88 Pasteur, P. Đa Kao','W10102',101,1),
('lananh.pham@gmail.com','ORD-2026-00041',7,'2026-04-05 09:00:00','2026-04-05 11:00:00','2026-04-06 09:00:00','2026-04-07 14:00:00','2026-04-08 08:00:00',NULL,'WALLET','PAID','2026-04-05 09:00:00',420000,20000,0,440000,NULL,'PAY-00041',NULL,N'Phạm Thị Lan Anh','0912001001',N'72 Lê Lợi, P. Bến Nghé','W10101',101,1),
('hung.nguyen88@gmail.com','ORD-2026-00042',7,'2026-03-22 10:00:00','2026-03-22 12:00:00','2026-03-23 09:00:00','2026-03-24 15:00:00','2026-03-25 08:00:00',NULL,'SE_PAY','PAID','2026-03-22 10:00:00',280000,20000,0,300000,NULL,'PAY-00042',NULL,N'Nguyễn Văn Hùng','0912001002',N'15 Trần Phú, P. Mộ Lao','W20501',205,2),
('ndiep.tran@gmail.com','ORD-2026-00043',6,'2026-04-28 08:00:00','2026-04-28 10:00:00','2026-04-29 09:00:00','2026-04-30 16:00:00',NULL,NULL,'WALLET','PAID','2026-04-28 08:00:00',650000,0,0,650000,NULL,'PAY-00043',NULL,N'Trần Ngọc Diệp','0912001007',N'55 Hoàng Diệu 2, P. Linh Chiểu','W10501',105,1),
('thuha.hoang@gmail.com','ORD-2026-00044',7,'2026-02-25 09:00:00','2026-02-25 11:00:00','2026-02-26 09:00:00','2026-02-27 14:00:00','2026-02-28 08:00:00',NULL,'SE_PAY','PAID','2026-02-25 09:00:00',359000,20000,0,379000,NULL,'PAY-00044',NULL,N'Hoàng Thị Thu Hà','0912001005',N'120 Lê Văn Lương, P. Nhân Chính','W20301',203,2),
('thanh.tuyen.ngo@gmail.com','ORD-2026-00045',4,'2026-05-08 14:00:00','2026-05-08 16:00:00','2026-05-09 09:30:00',NULL,NULL,NULL,'WALLET','PAID','2026-05-08 14:00:00',5990000,0,0,5990000,NULL,'PAY-00045','GHN-00045',N'Ngô Thị Thanh Tuyền','0912001010',N'33 Bùi Thị Xuân, P. Bến Nghé','W10101',101,1),
('dvlong.toys@gmail.com','ORD-2026-00046',2,'2026-05-09 09:00:00','2026-05-09 11:00:00',NULL,NULL,NULL,NULL,'SHIP_COD','COD_PENDING',NULL,890000,30000,0,920000,NULL,NULL,NULL,N'Dương Văn Long','0912001011',N'45 Lý Thường Kiệt, P. Hoàn Kiếm','W20101',201,2),
('kimoanh.trinh@gmail.com','ORD-2026-00047',7,'2026-03-28 10:00:00','2026-03-28 12:00:00','2026-03-29 09:00:00','2026-03-30 15:00:00','2026-03-31 08:00:00',NULL,'WALLET','PAID','2026-03-28 10:00:00',840000,0,0,840000,NULL,'PAY-00047',NULL,N'Trịnh Thị Kim Oanh','0912001012',N'6 Trần Hưng Đạo, P. Bến Nghé','W10101',101,1),
('quockhanh.phan@gmail.com','ORD-2026-00048',8,'2026-04-15 08:00:00',NULL,NULL,NULL,NULL,'2026-04-15 10:00:00','SE_PAY','FAILED',NULL,1590000,35000,0,1625000,N'Lỗi thanh toán – timeout',NULL,NULL,N'Phan Quốc Khánh','0912001009',N'17 Võ Văn Tần, P. Võ Thị Sáu','W10201',102,1),
('mtuando@outlook.com','ORD-2026-00049',7,'2026-02-18 09:00:00','2026-02-18 11:00:00','2026-02-19 09:00:00','2026-02-20 14:00:00','2026-02-21 08:00:00',NULL,'BANK_TRANSFER','PAID','2026-02-18 09:00:00',595000,25000,0,620000,NULL,'PAY-00049',NULL,N'Đỗ Minh Tuấn','0912001004',N'34 Đinh Tiên Hoàng, P. Đa Kao','W10102',101,1),
('vanphuc.le@gmail.com','ORD-2026-00050',7,'2026-03-12 10:00:00','2026-03-12 12:00:00','2026-03-13 09:00:00','2026-03-14 15:00:00','2026-03-15 08:00:00',NULL,'SE_PAY','PAID','2026-03-12 10:00:00',945000,0,0,945000,NULL,'PAY-00050',NULL,N'Lê Văn Phúc','0912001006',N'9 Đinh Lễ, P. Hoàn Kiếm','W20101',201,2);

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
PRINT N'[19/40] OrderDetails...';
IF OBJECT_ID('tempdb..#OD') IS NOT NULL DROP TABLE #OD;
CREATE TABLE #OD (OrderCode VARCHAR(30), ProductID INT, Qty SMALLINT, UnitPrice DECIMAL(12,0));
INSERT INTO #OD VALUES
('ORD-2026-00001', 1,1,1290000),
('ORD-2026-00002',26,1, 945000),
('ORD-2026-00003',17,1, 840000),
('ORD-2026-00004', 1,1,1290000),('ORD-2026-00004',29,2,88000),('ORD-2026-00004',30,1,175000),('ORD-2026-00004', 6,1,145000),
('ORD-2026-00005',20,1, 359000),('ORD-2026-00005',17,1,840000),
('ORD-2026-00006',28,1,1490000),
('ORD-2026-00007',13,1,  85000),('ORD-2026-00007',15,1,115000),('ORD-2026-00007',29,3,88000),('ORD-2026-00007', 6,1,145000),
('ORD-2026-00008',22,1, 595000),
('ORD-2026-00009',24,1, 395000),('ORD-2026-00009', 8,1,375000),
('ORD-2026-00010', 1,1,1290000),('ORD-2026-00010',11,1,545000),('ORD-2026-00010',30,2,175000),
('ORD-2026-00011',17,1, 840000),
('ORD-2026-00012', 4,1, 420000),
('ORD-2026-00013', 9,1, 280000),
('ORD-2026-00014',18,1, 650000),
('ORD-2026-00015',24,1, 395000),
('ORD-2026-00016',20,1, 359000),
('ORD-2026-00017',21,1, 490000),
('ORD-2026-00018', 7,1, 185000),
('ORD-2026-00019',12,1, 890000),
('ORD-2026-00020',22,1, 595000),
('ORD-2026-00021', 9,1, 280000),
('ORD-2026-00022',18,1, 650000),
('ORD-2026-00023',19,1, 420000),
('ORD-2026-00024', 1,1,1290000),
('ORD-2026-00025',12,1, 890000),
('ORD-2026-00026',25,1, 285000),('ORD-2026-00026', 6,1,85000),
('ORD-2026-00027',28,1,1490000),
('ORD-2026-00028', 8,1, 375000),
('ORD-2026-00029',21,1, 490000),
('ORD-2026-00030',17,1, 840000),
('ORD-2026-00031',22,1, 595000),
('ORD-2026-00032',18,1, 650000),
('ORD-2026-00033',25,1, 285000),
('ORD-2026-00034',19,1, 420000),
('ORD-2026-00035',12,1, 890000),
('ORD-2026-00036', 1,1,1290000),
('ORD-2026-00037',17,1, 840000),
('ORD-2026-00038', 8,1, 375000),
('ORD-2026-00039',26,1, 945000),
('ORD-2026-00040',21,1, 490000),
('ORD-2026-00041', 4,1, 420000),
('ORD-2026-00042', 9,1, 280000),
('ORD-2026-00043',18,1, 650000),
('ORD-2026-00044',20,1, 359000),
('ORD-2026-00045', 2,1,5990000),
('ORD-2026-00046',12,1, 890000),
('ORD-2026-00047',17,1, 840000),
('ORD-2026-00048', 3,1,1590000),
('ORD-2026-00049',22,1, 595000),
('ORD-2026-00050',26,1, 945000);

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
PRINT N'[20/40] OrderStatusHistory...';
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
   SECTION 21 – ORDER VOUCHERS & VOUCHER USAGE LOGS
══════════════════════════════════════════════════════════════ */
PRINT N'[21/40] OrderVouchers, VoucherUsageLogs...';
INSERT INTO [dbo].[OrderVouchers] (OrderID,VoucherID,DiscountAmountApplied)
SELECT o.OrderID, v.VoucherID, 200000
FROM Orders o
CROSS JOIN Vouchers v
WHERE o.OrderCode='ORD-2026-00010' AND v.VoucherCode='VIP200K'
  AND NOT EXISTS (SELECT 1 FROM OrderVouchers ov WHERE ov.OrderID=o.OrderID);

INSERT INTO [dbo].[VoucherUsageLogs] (VoucherID,AccountID,OrderID,UsedAt)
SELECT v.VoucherID, o.AccountID, o.OrderID, o.OrderDate
FROM Orders o
CROSS JOIN Vouchers v
WHERE o.OrderCode='ORD-2026-00010' AND v.VoucherCode='VIP200K'
  AND NOT EXISTS (SELECT 1 FROM VoucherUsageLogs vl WHERE vl.OrderID=o.OrderID);
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 22 – PAYMENT HISTORY
══════════════════════════════════════════════════════════════ */
PRINT N'[22/40] PaymentHistory...';
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
PRINT N'[23/40] PaymentGatewayTransactions...';
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
PRINT N'[24/40] Wallets, WalletTransactions...';
INSERT INTO [dbo].[Wallets] (AccountID,Currency,Balance,Status,CreatedAt)
SELECT AccountID,'VND',20000000,'Active',GETDATE()
FROM Accounts
WHERE RoleID IN (1,2,3,4)
  AND NOT EXISTS (SELECT 1 FROM Wallets w WHERE w.AccountID=Accounts.AccountID);

INSERT INTO [dbo].[WalletTransactions]
    (WalletID,AccountID,TxnType,Direction,Amount,BalanceBefore,BalanceAfter,
     Method,Status,Reason,CreatedAt)
SELECT w.WalletID,w.AccountID,'TopUp','CR',20000000,0,20000000,
       'BankTransfer','Completed',N'Nạp tiền ban đầu',GETDATE()
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
  AND NOT EXISTS (SELECT 1 FROM WalletTransactions wt WHERE wt.RelatedOrderID=o.OrderID AND wt.TxnType='Payment')
  AND 20000000 - ISNULL((SELECT SUM(o2.TotalAmount) FROM Orders o2
        WHERE o2.AccountID=o.AccountID AND o2.PaymentMethod='WALLET'
          AND o2.PaymentStatus='PAID' AND o2.OrderID < o.OrderID),0) - o.TotalAmount >= 0;
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 25 – WALLET PINS
══════════════════════════════════════════════════════════════ */
PRINT N'[25/40] WalletPins...';
INSERT INTO [dbo].[WalletPins] (WalletID,PinHash,IsActive,FailedAttempts,CreatedAt)
SELECT w.WalletID,
       '$2a$11$pin_hash_placeholder_for_seed_data_only_xxxxxxxxxx',
       1, 0, GETDATE()
FROM Wallets w
JOIN Accounts a ON a.AccountID=w.AccountID
WHERE a.RoleID=1
  AND NOT EXISTS (SELECT 1 FROM WalletPins wp WHERE wp.WalletID=w.WalletID);
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 26 – SHIPPING
══════════════════════════════════════════════════════════════ */
PRINT N'[26/40] ShippingProviderTransactions, ShippingStatusHistories...';
INSERT INTO [dbo].[ShippingProviderTransactions]
    (OrderID,Provider,ProviderOrderCode,TrackingNumber,Status,ShippingFee,CodAmount,CreatedAt)
SELECT
    o.OrderID,'GHN',
    ISNULL(o.ShippingOrderCode,'GHN-'+o.OrderCode),
    'VN'+RIGHT(REPLACE(o.OrderCode,'-',''),8),
    CASE o.StatusID
        WHEN 7 THEN 'delivered' WHEN 6 THEN 'delivered'
        WHEN 5 THEN 'delivering' WHEN 4 THEN 'transporting'
        ELSE 'ready_to_pick' END,
    o.EstimatedShippingFee,
    CASE o.PaymentMethod WHEN 'SHIP_COD' THEN o.TotalAmount ELSE 0 END,
    ISNULL(o.ShippedAt, o.OrderDate)
FROM Orders o
WHERE o.StatusID >= 2 AND o.StatusID <> 8
  AND NOT EXISTS (SELECT 1 FROM ShippingProviderTransactions sp WHERE sp.OrderID=o.OrderID);

INSERT INTO [dbo].[ShippingStatusHistories]
    (ShippingTxId,OrderId,PreviousStatus,NewStatus,Source,ProcessedAt)
SELECT sp.ShippingTransactionID, sp.OrderID, 'created', sp.Status, 'Seed', GETDATE()
FROM ShippingProviderTransactions sp
WHERE NOT EXISTS (
    SELECT 1 FROM ShippingStatusHistories sh WHERE sh.ShippingTxId=sp.ShippingTransactionID);
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 27 – ORDER REFUNDS
══════════════════════════════════════════════════════════════ */
PRINT N'[27/40] OrderRefundReasons, OrderRefunds, RefundImages...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderRefundReasons])
    INSERT INTO [dbo].[OrderRefundReasons] (Content,Description,CreatedAt) VALUES
    (N'Sản phẩm bị lỗi từ nhà sản xuất', N'Hàng giao đến bị lỗi kỹ thuật',               '2024-01-05 08:00:00'),
    (N'Giao nhầm sản phẩm',               N'Shop gửi sai màu, size hoặc model',            '2024-01-05 08:00:00'),
    (N'Sản phẩm không đúng mô tả',        N'Hình ảnh/mô tả không khớp sản phẩm thực tế',  '2024-01-05 08:00:00'),
    (N'Hàng bị hư hỏng trong vận chuyển', N'Kiện hàng bị móp méo, vỡ trong quá trình giao','2024-01-05 08:00:00'),
    (N'Thiếu phụ kiện đi kèm',            N'Hộp không có đủ phụ kiện như mô tả',          '2024-01-05 08:00:00');
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderRefunds])
BEGIN
    DECLARE @refCust3  INT = (SELECT TOP 1 AccountID FROM Accounts WHERE Email='bauchau.vu@gmail.com');
    DECLARE @refCust15 INT = (SELECT TOP 1 AccountID FROM Accounts WHERE Email='minhquan.dinh@icloud.com');
    DECLARE @refCust26 INT = (SELECT TOP 1 AccountID FROM Accounts WHERE Email='ngochuyen.ho@gmail.com');
    DECLARE @refR1     INT = (SELECT TOP 1 RefundReasonID FROM OrderRefundReasons WHERE Content=N'Sản phẩm bị lỗi từ nhà sản xuất');
    DECLARE @refR4     INT = (SELECT TOP 1 RefundReasonID FROM OrderRefundReasons WHERE Content=N'Hàng bị hư hỏng trong vận chuyển');
    DECLARE @refR3     INT = (SELECT TOP 1 RefundReasonID FROM OrderRefundReasons WHERE Content=N'Sản phẩm không đúng mô tả');
    DECLARE @ord3      INT = (SELECT TOP 1 OrderID FROM Orders WHERE OrderCode='ORD-2026-00003');
    DECLARE @ord15     INT = (SELECT TOP 1 OrderID FROM Orders WHERE OrderCode='ORD-2026-00015');
    DECLARE @ord26     INT = (SELECT TOP 1 OrderID FROM Orders WHERE OrderCode='ORD-2026-00026');

    IF @ord3 IS NOT NULL
        INSERT INTO [dbo].[OrderRefunds]
            (OrderID,RefundReasonID,CustomerID,RequestedBy,ApprovedAmount,RefundStatus,CreatedAt)
        VALUES (@ord3, @refR1, @refCust3,  @refCust3,  840000,'Requested','2026-03-07 09:00:00');

    IF @ord15 IS NOT NULL
        INSERT INTO [dbo].[OrderRefunds]
            (OrderID,RefundReasonID,CustomerID,RequestedBy,ApprovedAmount,RefundStatus,CreatedAt)
        VALUES (@ord15, @refR4, @refCust15, @refCust15, 395000,'Approved', '2026-04-09 10:00:00');

    IF @ord26 IS NOT NULL
        INSERT INTO [dbo].[OrderRefunds]
            (OrderID,RefundReasonID,CustomerID,RequestedBy,ApprovedAmount,RefundStatus,CreatedAt)
        VALUES (@ord26, @refR3, @refCust26, @refCust26, 345000,'Rejected', '2026-03-19 09:00:00');

    INSERT INTO [dbo].[RefundImages] (RefundID,ImageURL,CreatedAt)
    SELECT RefundID,'https://picsum.photos/seed/refund-'+CAST(RefundID AS VARCHAR)+'-a/400/400',GETDATE() FROM OrderRefunds;
    INSERT INTO [dbo].[RefundImages] (RefundID,ImageURL,CreatedAt)
    SELECT RefundID,'https://picsum.photos/seed/refund-'+CAST(RefundID AS VARCHAR)+'-b/400/400',GETDATE() FROM OrderRefunds;
    INSERT INTO [dbo].[RefundImages] (RefundID,ImageURL,CreatedAt)
    SELECT RefundID,'https://picsum.photos/seed/refund-'+CAST(RefundID AS VARCHAR)+'-c/400/400',GETDATE() FROM OrderRefunds WHERE RefundStatus='Approved';
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 28 – CART & WISHLIST
══════════════════════════════════════════════════════════════ */
PRINT N'[28/40] Cart, CartItems, Wishlists...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[Cart])
BEGIN
    INSERT INTO [dbo].[Cart] (AccountID,CreatedAt)
    SELECT AccountID, GETDATE() FROM Accounts WHERE RoleID=1;

    INSERT INTO [dbo].[CartItems]
        (CartID,ProductID,Quantity,PriceAtThatTime,CurrentPrice,IsSelected,AddedAt)
    SELECT c.CartID, v.ProductID, v.Qty, v.Pr, v.Pr, 1, GETDATE()
    FROM Cart c
    JOIN Accounts a ON a.AccountID=c.AccountID
    JOIN (VALUES
        ('lananh.pham@gmail.com',      1,1,1290000),('lananh.pham@gmail.com',     29,2,  88000),
        ('hung.nguyen88@gmail.com',   26,1, 945000),('hung.nguyen88@gmail.com',   28,1,1490000),
        ('bauchau.vu@gmail.com',       1,1,1290000),('bauchau.vu@gmail.com',      17,1, 840000),
        ('mtuando@outlook.com',        8,1, 375000),('mtuando@outlook.com',       24,1, 395000),
        ('thuha.hoang@gmail.com',     11,1, 545000),('thuha.hoang@gmail.com',     15,2, 115000),
        ('vanphuc.le@gmail.com',      22,1, 595000),('vanphuc.le@gmail.com',       2,1,5990000),
        ('ndiep.tran@gmail.com',      20,1, 359000),('ndiep.tran@gmail.com',      19,1, 420000),
        ('mylinh.bui2024@gmail.com',  25,3, 285000),('quockhanh.phan@gmail.com',  27,1, 680000),
        ('thanh.tuyen.ngo@gmail.com',  3,1,1590000),('thanh.tuyen.ngo@gmail.com', 21,1, 490000),
        ('dvlong.toys@gmail.com',     23,1, 890000),('kimoanh.trinh@gmail.com',   30,2, 175000),
        ('ducthinh.huynh@gmail.com',  16,1, 185000),('hongvan.mai@gmail.com',     18,1, 650000),
        ('minhquan.dinh@icloud.com',   9,1, 280000),('xuanmai.ly@gmail.com',      10,1, 490000),
        ('vantai.chau@gmail.com',     14,2, 165000),('yennhi.ng@gmail.com',        5,1, 350000),
        ('thanhsang.vo@gmail.com',    12,1, 890000),('bichtram.phung@gmail.com',  13,3,  85000),
        ('minhnhat.cao@gmail.com',     7,2, 185000),('phuongthao.dang@gmail.com', 26,1, 945000),
        ('quanghai.luu@gmail.com',    24,1, 395000),('minhchau.to@gmail.com',      6,2, 145000),
        ('ducanh.truong@gmail.com',   23,1, 890000),('ngochuyen.ho@gmail.com',    30,1, 175000),
        ('kien.bui@gmail.com',         4,1, 420000),('kimlien.lam@gmail.com',     15,1, 115000),
        ('tuananh.phan@gmail.com',    27,1, 680000),('thanhnga.vuong@gmail.com',  20,1, 359000),
        ('haidang.ng@gmail.com',       9,2, 280000),('camtu.tran@gmail.com',      18,1, 650000)
    ) AS v(Email,ProductID,Qty,Pr) ON a.Email=v.Email
    WHERE NOT EXISTS (
        SELECT 1 FROM CartItems ci WHERE ci.CartID=c.CartID AND ci.ProductID=v.ProductID);
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Wishlists])
    INSERT INTO [dbo].[Wishlists] (AccountID,ProductID,CreatedAt)
    SELECT a.AccountID, v.ProductID, GETDATE()
    FROM Accounts a
    JOIN (VALUES
        ('lananh.pham@gmail.com',      2),('lananh.pham@gmail.com',     23),
        ('hung.nguyen88@gmail.com',    1),('hung.nguyen88@gmail.com',   27),
        ('bauchau.vu@gmail.com',      18),('mtuando@outlook.com',       22),
        ('thuha.hoang@gmail.com',      3),('vanphuc.le@gmail.com',      28),
        ('ndiep.tran@gmail.com',      25),('mylinh.bui2024@gmail.com',   9),
        ('quockhanh.phan@gmail.com',  26),('thanh.tuyen.ngo@gmail.com', 11),
        ('dvlong.toys@gmail.com',     12),('kimoanh.trinh@gmail.com',   17),
        ('ducthinh.huynh@gmail.com',   1),('hongvan.mai@gmail.com',     20),
        ('minhquan.dinh@icloud.com',  28),('xuanmai.ly@gmail.com',       6),
        ('vantai.chau@gmail.com',     15),('yennhi.ng@gmail.com',       19),
        ('thanhsang.vo@gmail.com',    24),('bichtram.phung@gmail.com',  13),
        ('minhnhat.cao@gmail.com',     8),('phuongthao.dang@gmail.com', 21),
        ('quanghai.luu@gmail.com',     4),('minhchau.to@gmail.com',      7),
        ('ducanh.truong@gmail.com',   16),('ngochuyen.ho@gmail.com',    30),
        ('kien.bui@gmail.com',         5),('kimlien.lam@gmail.com',     14),
        ('tuananh.phan@gmail.com',    29),('thanhnga.vuong@gmail.com',  22),
        ('haidang.ng@gmail.com',      10),('camtu.tran@gmail.com',      19),
        ('trongnghia.le@gmail.com',    2),('huonggiang.ngo@gmail.com',  24),
        ('vanphong.doan@gmail.com',   28),('baongoc.lai@gmail.com',      1),
        ('minhtri.khong@gmail.com',   26),('thuhang.ha@gmail.com',      17)
    ) AS v(Email,ProductID) ON a.Email=v.Email;
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 29 – PRODUCT FOLLOWERS
══════════════════════════════════════════════════════════════ */
PRINT N'[29/40] ProductFollowers...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[ProductFollowers])
    INSERT INTO [dbo].[ProductFollowers] (ProductID,AccountID,CreatedAt)
    SELECT a.AccountID, v.ProductID, GETDATE()
    FROM Accounts a
    JOIN (VALUES
        ('lananh.pham@gmail.com',     28),('hung.nguyen88@gmail.com',    2),
        ('bauchau.vu@gmail.com',      12),('mtuando@outlook.com',         3),
        ('thuha.hoang@gmail.com',      1),('vanphuc.le@gmail.com',       26),
        ('ndiep.tran@gmail.com',       5),('mylinh.bui2024@gmail.com',   28),
        ('quockhanh.phan@gmail.com',   1),('thanh.tuyen.ngo@gmail.com',  27),
        ('dvlong.toys@gmail.com',     22),('kimoanh.trinh@gmail.com',    17),
        ('ducthinh.huynh@gmail.com',  12),('hongvan.mai@gmail.com',       1),
        ('minhquan.dinh@icloud.com',  26),('xuanmai.ly@gmail.com',       20)
    ) AS v(Email,ProductID) ON a.Email=v.Email
    WHERE NOT EXISTS (
        SELECT 1 FROM ProductFollowers pf
        WHERE pf.AccountID=a.AccountID AND pf.ProductID=v.ProductID);
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 30 – CUSTOMER CHILDREN
══════════════════════════════════════════════════════════════ */
PRINT N'[30/40] CustomerChildren...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[CustomerChildren])
    INSERT INTO [dbo].[CustomerChildren] (AccountID,SexID,FullName,NickName,DOB,CreatedAt)
    SELECT a.AccountID, v.SexID, v.FullName, v.NickName, v.DOB, GETDATE()
    FROM Accounts a
    JOIN (VALUES
        ('lananh.pham@gmail.com',    1,N'Phạm Tiến Phát',   N'Củ Cải',  '2020-05-15'),
        ('lananh.pham@gmail.com',    2,N'Phạm Thảo Trân',   N'Bào Ngư', '2022-11-20'),
        ('bauchau.vu@gmail.com',     1,N'Vũ Hoàng Nam',     N'Gấu',     '2019-08-10'),
        ('thanh.tuyen.ngo@gmail.com',2,N'Ngô Mai Phương',   N'Nhím',    '2023-01-05'),
        ('thuha.hoang@gmail.com',    1,N'Hoàng Minh Tuấn',  N'Tun',     '2018-07-12'),
        ('ndiep.tran@gmail.com',     2,N'Trần Ngọc Anh',    N'Kẹo',     '2021-03-25'),
        ('mtuando@outlook.com',      1,N'Đỗ Khắc Duy',      N'Bi',      '2017-09-08'),
        ('mtuando@outlook.com',      2,N'Đỗ Thùy Linh',     N'Bông',    '2020-12-18'),
        ('dvlong.toys@gmail.com',    1,N'Dương Gia Bảo',    N'Bảo',     '2019-04-22'),
        ('kimoanh.trinh@gmail.com',  2,N'Trịnh Khánh Linh', N'Linh',    '2022-08-30'),
        ('hung.nguyen88@gmail.com',  1,N'Nguyễn Gia Khang', N'Khang',   '2016-05-10'),
        ('mylinh.bui2024@gmail.com', 2,N'Bùi Phương Anh',   N'Anh',     '2021-09-14'),
        ('quockhanh.phan@gmail.com', 1,N'Phan Minh Hiếu',   N'Hiếu',    '2018-03-20'),
        ('quanghai.luu@gmail.com',   2,N'Lưu Bảo Châu',     N'Châu',    '2023-06-11'),
        ('ducanh.truong@gmail.com',  1,N'Trương Minh Anh',  N'Minh',    '2020-01-08')
    ) AS v(Email,SexID,FullName,NickName,DOB) ON a.Email=v.Email;
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 31 – BLOG
   FIX: Added missing END to close the IF NOT EXISTS ReviewProducts
        block that caused Msg 102 syntax error in v5.1.
        Used ROW_NUMBER() CTE to map blog post position → BlogPostID.
══════════════════════════════════════════════════════════════ */
PRINT N'[31/40] BlogCategories, BlogPosts, ReviewBlogs, Replies, Reactions...';

/* ── BlogCategories ── */
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

/* ── BlogPosts ── */
DECLARE @bst1 INT=(SELECT TOP 1 AccountID FROM Accounts WHERE Email='nhung.st@toyhouse.vn');
DECLARE @bst2 INT=(SELECT TOP 1 AccountID FROM Accounts WHERE Email='dung.st@toyhouse.vn');
DECLARE @badm INT=(SELECT TOP 1 AccountID FROM Accounts WHERE Email='admin@toyhouse.vn');

IF NOT EXISTS (SELECT 1 FROM [dbo].[BlogPosts])
    INSERT INTO [dbo].[BlogPosts]
        (AccountID,ApprovedBy,BlogCategoryID,BlogTitle,BlogContent,
         BlogThumbnail,Status,IsFeatured,BlogAt,CreatedAt)
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
     'https://picsum.photos/seed/blog-flash55/600/400','Published',0,'2026-04-20 08:00:00','2026-04-20 08:00:00'),
    (@bst2,@badm,5,N'Hướng Dẫn Kiểm Tra Tem Chứng Nhận Đồ Chơi Nhập Khẩu Chính Hãng',
     N'Cách đọc tem CE/ASTM/TCVN, kiểm tra mã QR truy xuất nguồn gốc, và các dấu hiệu nhận biết hàng giả.',
     'https://picsum.photos/seed/blog-authentic/600/400','Published',0,'2026-02-20 14:00:00','2026-02-20 14:00:00'),
    (@bst1,NULL, 3,N'Review Siêu Xe RC Traxxas TRX-Mini – Có Xứng Đáng Với Mức Giá?',
     N'Traxxas TRX-Mini giá gần 1 triệu liệu có đáng mua? Đánh giá chi tiết về pin, tốc độ và độ bền.',
     'https://picsum.photos/seed/blog-rc-review/600/400','Pending',0,NULL,'2026-04-28 10:00:00'),
    (@bst2,@badm,2,N'Đồ Chơi Nhập Vai – Bí Kíp Phát Triển Sáng Tạo Và EQ Cho Bé',
     N'Chuyên gia tâm lý trẻ em khuyến nghị đồ chơi nhập vai phát triển EQ và tư duy sáng tạo.',
     'https://picsum.photos/seed/blog-roleplay/600/400','Published',0,'2026-03-15 10:00:00','2026-03-15 10:00:00'),
    (@bst1,@badm,4,N'5 Trò Chơi Ngoài Trời Giúp Bé Phát Triển Vận Động Toàn Diện',
     N'Các trò chơi ngoài trời không chỉ rèn luyện thể lực mà còn tăng cường kỹ năng xã hội.',
     'https://picsum.photos/seed/blog-outdoor/600/400','Published',1,'2026-05-01 09:00:00','2026-05-01 09:00:00'),
    (@bst2,@badm,1,N'Voucher Freeship Tháng 6 – Áp Dụng Cho Mọi Đơn Từ 200K',
     N'Tháng 6 này ToyHouse tặng freeship toàn quốc. Dùng mã SHIP0626 khi thanh toán.',
     'https://picsum.photos/seed/blog-freeship/600/400','Published',0,'2026-05-25 08:00:00','2026-05-25 08:00:00');
GO

/* ── ReviewBlogs using ROW_NUMBER() to map position → BlogPostID ──
   FIX [S31]: Added the closing END that was missing in v5.1,
   which caused Msg 102 and aborted the entire batch.
── */
IF NOT EXISTS (SELECT 1 FROM [dbo].[ReviewBlogs])
BEGIN
    ;WITH BlogRanked AS (
        SELECT BlogPostID,
               ROW_NUMBER() OVER (ORDER BY BlogPostID) AS Pos
        FROM [dbo].[BlogPosts]
    )
    INSERT INTO [dbo].[ReviewBlogs] (BlogPostID, AccountID, Comment, CreatedAt)
    SELECT br.BlogPostID, a.AccountID, v.Comment, v.CreatedAt
    FROM (VALUES
        (1,'lananh.pham@gmail.com',      N'Mình đã mua Lego City trong đợt này, giảm 15% rất hời! Bé nhà mình mê lắm.','2026-03-29 10:00:00'),
        (1,'hung.nguyen88@gmail.com',    N'Sale to thật! Vừa mua 2 bộ RC Traxxas, tiết kiệm cả 300k.','2026-03-30 08:00:00'),
        (1,'bauchau.vu@gmail.com',       N'Có áp dụng voucher VIP200K kèm sale hè không ad?','2026-03-30 09:30:00'),
        (1,'mtuando@outlook.com',        N'Giao hàng siêu nhanh, đặt sáng chiều đã có. Đóng gói cẩn thận 5 sao!','2026-03-31 14:00:00'),
        (1,'thuha.hoang@gmail.com',      N'Đã mua Barbie Dreamtopia cho bé gái, con thích lắm cảm ơn shop!','2026-04-01 09:00:00'),
        (2,'ndiep.tran@gmail.com',       N'Bài viết rất bổ ích! Trước giờ mình toàn mua theo cảm tính.','2026-02-11 10:00:00'),
        (2,'mylinh.bui2024@gmail.com',   N'Cảm ơn shop! Mình đã chia sẻ bài này cho hội mẹ bỉm rồi.','2026-02-12 11:00:00'),
        (2,'quockhanh.phan@gmail.com',   N'Cho hỏi đồ chơi bán trên shop có đầy đủ chứng nhận TCVN không ạ?','2026-02-13 08:00:00'),
        (2,'thanh.tuyen.ngo@gmail.com',  N'Bài viết quá hay! Vừa mua PlayDoh 24 màu, bé rất thích!','2026-02-14 10:00:00'),
        (3,'dvlong.toys@gmail.com',      N'Review rất chi tiết! Mình cũng đang nghĩ mua bộ này cho con 8 tuổi.','2026-03-06 11:00:00'),
        (3,'kimoanh.trinh@gmail.com',    N'Bé nhà mình 6 tuổi có chơi được không bạn ơi?','2026-03-07 09:00:00'),
        (3,'ducthinh.huynh@gmail.com',   N'Mình mua rồi cũng thấy xứng đáng. Bé 7 tuổi tự lắp được 70%.','2026-03-08 10:00:00'),
        (3,'hongvan.mai@gmail.com',      N'Mảnh lắp ghép có hay bị mất không bạn?','2026-03-09 11:00:00'),
        (4,'minhquan.dinh@icloud.com',   N'Xe chòi chân vịt Donald nhà mình mua cho con 2 tuổi, bé leo lên đạp ngay!','2026-04-02 09:00:00'),
        (4,'xuanmai.ly@gmail.com',       N'Mình thích nhất là diều đại bàng, bay ổn định dù gió ít.','2026-04-03 10:00:00'),
        (4,'vantai.chau@gmail.com',      N'Bài viết rất hữu ích! Đang tìm đồ chơi sinh nhật cho bé trai 4 tuổi.','2026-04-04 11:00:00'),
        (4,'yennhi.ng@gmail.com',        N'Xe đạp 3 bánh Disney Princess quá cute! Con gái mình đòi mua.','2026-04-05 08:00:00'),
        (4,'thanhsang.vo@gmail.com',     N'Bóng cao su Boho nhà mình mua 3 tháng vẫn tốt!','2026-04-06 09:00:00'),
        (5,'bichtram.phung@gmail.com',   N'Đã đặt lịch nhắc cho ngày 4/5, không thể bỏ lỡ Flash Sale này!','2026-04-21 10:00:00'),
        (5,'minhnhat.cao@gmail.com',     N'50% thật không? Mô hình Bandai mà giảm 50% là quá hời rồi!','2026-04-22 08:00:00'),
        (6,'phuongthao.dang@gmail.com',  N'Mình đã bị mua phải hàng nhái rồi, bài này đọc xong mới hiểu sao.','2026-02-21 09:00:00'),
        (6,'quanghai.luu@gmail.com',     N'Cảm ơn! QR code truy xuất nguồn gốc là tiện lợi nhất.','2026-02-22 11:00:00'),
        (8,'minhchau.to@gmail.com',      N'Đúng rồi, búp bê nhập vai cho bé gái 4 tuổi là tốt nhất.','2026-03-16 09:00:00'),
        (8,'ducanh.truong@gmail.com',    N'Con trai 5 tuổi nhà mình mê siêu nhân nhập vai lắm!','2026-03-17 10:00:00'),
        (9,'ngochuyen.ho@gmail.com',     N'Bóng và diều là 2 món ngoài trời tốt nhất cho bé.','2026-05-02 09:00:00'),
        (9,'kien.bui@gmail.com',         N'Mình hay cho bé đá bóng cuối tuần, rất tốt cho sức khỏe!','2026-05-03 10:00:00'),
        (9,'kimlien.lam@gmail.com',      N'Bài viết hay lắm! Đã chia sẻ cho nhóm phụ huynh lớp rồi.','2026-05-04 08:00:00'),
        (10,'tuananh.phan@gmail.com',    N'Mình đang chờ mã SHIP0626! Muốn mua thêm mấy bộ Lego.','2026-05-26 09:00:00'),
        (10,'thanhnga.vuong@gmail.com',  N'Freeship toàn quốc thật không? Mình ở Cần Thơ được không?','2026-05-26 10:00:00'),
        (10,'haidang.ng@gmail.com',      N'Đã dùng mã rồi, freeship thật! Đơn 350k không mất phí ship.','2026-05-28 11:00:00')
    ) AS v(BpPos, Email, Comment, CreatedAt)
    JOIN BlogRanked br ON br.Pos = v.BpPos
    JOIN Accounts a ON a.Email = v.Email;

    /* Staff replies */
    DECLARE @staffRep2 INT=(SELECT TOP 1 AccountID FROM Accounts WHERE Email='nhung.st@toyhouse.vn');
    INSERT INTO [dbo].[ReviewBlogReplies] (ReviewBlogID,AccountID,Comment,CreatedAt)
    SELECT TOP 15 rb.ReviewBlogID, @staffRep2,
           N'Cảm ơn bạn đã đồng hành cùng ToyHouse! Chúc bé luôn vui vẻ và phát triển toàn diện!',
           DATEADD(HOUR,1,rb.CreatedAt)
    FROM ReviewBlogs rb ORDER BY rb.CreatedAt;

    /* Customer replies */
    DECLARE @cust1 INT=(SELECT TOP 1 AccountID FROM Accounts WHERE Email='lananh.pham@gmail.com');
    INSERT INTO [dbo].[ReviewBlogReplies] (ReviewBlogID,AccountID,Comment,CreatedAt)
    SELECT TOP 5 rb.ReviewBlogID, @cust1,
           N'Mình cũng đồng ý với bạn, sản phẩm tốt lắm!',
           DATEADD(HOUR,2,rb.CreatedAt)
    FROM ReviewBlogs rb WHERE rb.AccountID <> @cust1
    ORDER BY rb.CreatedAt;

    /* ReviewBlog Reactions */
    DECLARE @badm2 INT=(SELECT TOP 1 AccountID FROM Accounts WHERE Email='admin@toyhouse.vn');
    INSERT INTO [dbo].[ReviewBlogReactions] (ReviewBlogID,AccountID,ReactionTypeID,CreatedAt)
    SELECT rb.ReviewBlogID, @badm2, 1, DATEADD(MINUTE,30,rb.CreatedAt)
    FROM ReviewBlogs rb
    WHERE NOT EXISTS (
        SELECT 1 FROM ReviewBlogReactions r WHERE r.ReviewBlogID=rb.ReviewBlogID AND r.AccountID=@badm2);

    DECLARE @hung INT=(SELECT TOP 1 AccountID FROM Accounts WHERE Email='hung.nguyen88@gmail.com');
    INSERT INTO [dbo].[ReviewBlogReactions] (ReviewBlogID,AccountID,ReactionTypeID,CreatedAt)
    SELECT rb.ReviewBlogID, @hung, 2, DATEADD(MINUTE,45,rb.CreatedAt)
    FROM ReviewBlogs rb
    WHERE rb.AccountID <> @hung
      AND NOT EXISTS (
        SELECT 1 FROM ReviewBlogReactions r WHERE r.ReviewBlogID=rb.ReviewBlogID AND r.AccountID=@hung);

    /* BlogPost Reactions */
    INSERT INTO [dbo].[BlogPostReactions] (BlogPostID,AccountID,ReactionTypeID,CreatedAt)
    SELECT b.BlogPostID, a.AccountID, v.ReactionTypeID, GETDATE()
    FROM BlogPosts b
    CROSS JOIN (VALUES
        ('lananh.pham@gmail.com',    1),('hung.nguyen88@gmail.com',  2),
        ('bauchau.vu@gmail.com',     1),('thuha.hoang@gmail.com',    1),
        ('ndiep.tran@gmail.com',     2),('mylinh.bui2024@gmail.com', 1),
        ('quockhanh.phan@gmail.com', 1),('thanh.tuyen.ngo@gmail.com',2),
        ('dvlong.toys@gmail.com',    4),('kimoanh.trinh@gmail.com',  1)
    ) AS v(Email,ReactionTypeID)
    JOIN Accounts a ON a.Email=v.Email
    WHERE b.Status='Published'
      AND NOT EXISTS (
          SELECT 1 FROM BlogPostReactions bpr
          WHERE bpr.BlogPostID=b.BlogPostID AND bpr.AccountID=a.AccountID);
END  -- ← FIX: this END was missing in v5.1, causing Msg 102
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 32 – REVIEWS
══════════════════════════════════════════════════════════════ */
PRINT N'[32/40] ReviewProducts, Images, Replies, Reactions...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[ReviewProducts])
BEGIN
    INSERT INTO [dbo].[ReviewProducts]
        (AccountID,ProductID,OrderID,Rating,Comment,ModerationStatus,CreatedAt)
    SELECT a.AccountID, v.ProductID, o.OrderID, v.Rating, v.Comment, 'Approved', v.CreatedAt
    FROM (VALUES
        ('lananh.pham@gmail.com',     1,'ORD-2026-00001',5,N'Mua cho con trai 7 tuổi, bé mê lắm! Hộp đẹp, đầy đủ phụ kiện, mảnh ghép chắc. Giao hàng nhanh 1 ngày!','2026-02-18 09:00:00'),
        ('thanh.tuyen.ngo@gmail.com', 1,'ORD-2026-00010',5,N'Set hàng Lego City + xe chòi + 2 bảng LCD giảm 200k voucher VIP. Hài lòng 100%!','2026-03-19 08:00:00'),
        ('ndiep.tran@gmail.com',     13,'ORD-2026-00007',5,N'Bóng cao su chất lượng tốt. Con chơi ngoài trời mấy tuần vẫn không xì, không phai màu.','2026-03-24 10:00:00'),
        ('dvlong.toys@gmail.com',    17,'ORD-2026-00011',4,N'Gấu bông rất mềm, con bé 3 tuổi ôm cả ngày. Trừ 1 sao vì giao hơi chậm 1 ngày.','2026-02-24 10:00:00'),
        ('kimoanh.trinh@gmail.com',   4,'ORD-2026-00012',5,N'Bộ xếp hình gỗ rất đẹp và chắc chắn. Sơn không bong, không bay mùi. Con 2 tuổi chơi được ngay.','2026-03-05 11:00:00'),
        ('lananh.pham@gmail.com',     4,'ORD-2026-00041',5,N'Lần 2 mua vì lần trước quá ưng! Mua thêm 1 bộ tặng cháu. Shop đóng gói cẩn thận.','2026-04-09 09:00:00'),
        ('hung.nguyen88@gmail.com',   9,'ORD-2026-00042',4,N'Bộ núi lửa phun trào rất vui! Con trai 6 tuổi thích mê. Hướng dẫn toàn tiếng Anh hơi khó.','2026-03-26 10:00:00'),
        ('ndiep.tran@gmail.com',     18,'ORD-2026-00043',5,N'Gấu trúc panda 60cm siêu xinh! Vải mịn, bông đầy. Mua làm quà sinh nhật được khen lắm.','2026-05-01 09:00:00'),
        ('thuha.hoang@gmail.com',    20,'ORD-2026-00044',5,N'Barbie Dreamtopia tiên cá đẹp lắm! Con gái 5 tuổi đòi mua thêm bộ khác luôn. Chất lượng tốt hơn mong đợi.','2026-02-28 11:00:00'),
        ('vanphuc.le@gmail.com',     26,'ORD-2026-00050',5,N'RC Traxxas TRX-Mini quá xịn! Tốc độ mạnh, pin trâu, điều khiển nhạy. Con trai 10 tuổi mê không rời.','2026-03-16 09:00:00'),
        ('kien.bui@gmail.com',       28,'ORD-2026-00027',5,N'Trực thăng RC bay ổn định, con quay chống lắc cực tốt. Bé 12 tuổi điều khiển được ngay sau 30 phút.','2026-03-04 10:00:00'),
        ('congson.dinh@gmail.com',   26,'ORD-2026-00039',4,N'Xe RC địa hình rất tốt, chạy được cả sân đất. Pin khoảng 20 phút, hơi ngắn nhưng chấp nhận được.','2026-02-03 09:00:00'),
        ('phuongthao.dang@gmail.com',18,'ORD-2026-00022',5,N'Gấu trúc panda quá xịn! Mua tặng sinh nhật bạn, ai cũng khen đẹp và sang.','2026-02-14 09:00:00'),
        ('camtu.tran@gmail.com',     18,'ORD-2026-00032',5,N'Sản phẩm đúng như mô tả, giao nhanh, đóng gói kỹ. Sẽ quay lại mua tiếp.','2026-02-06 10:00:00'),
        ('xuanmai.ly@gmail.com',     20,'ORD-2026-00016',4,N'Barbie đẹp, con gái 5 tuổi rất thích. Tóc hơi cứng lúc đầu nhưng chải xong mềm ngay.','2026-01-14 10:00:00'),
        ('vantai.chau@gmail.com',    21,'ORD-2026-00017',5,N'Barbie Fashionista set 6 váy rất đáng tiền! Nhiều style khác nhau, bé thích thay đổi mỗi ngày.','2026-01-22 09:00:00'),
        ('haidang.ng@gmail.com',     22,'ORD-2026-00031',5,N'Siêu Nhân Gao Red Ranger chắc chắn, 24 khớp xoay rất linh hoạt. Con 8 tuổi tạo pose đủ kiểu.','2026-01-24 11:00:00'),
        ('minhnhat.cao@gmail.com',    9,'ORD-2026-00021',4,N'Bộ núi lửa phun trào vui lắm. Một số hoá chất trong kit hơi khó tìm lại khi cần bổ sung.','2026-01-29 10:00:00'),
        ('quanghai.luu@gmail.com',   19,'ORD-2026-00023',5,N'Thỏ tai dài pastel siêu mềm và cute! Mua tặng bạn gái nhân dịp Valentine, được khen hết lời.','2026-04-18 09:00:00'),
        ('huonggiang.ngo@gmail.com', 19,'ORD-2026-00034',5,N'Sản phẩm đúng mô tả. Vải nhung mịn, màu pastel đẹp. Giao hàng trong ngày rất tiện.','2026-03-09 10:00:00')
    ) AS v(Email, ProductID, OrderCode, Rating, Comment, CreatedAt)
    JOIN Accounts a ON a.Email = v.Email
    JOIN Orders   o ON o.OrderCode = v.OrderCode
    WHERE EXISTS (
        SELECT 1 FROM OrderDetails od
        WHERE od.OrderID = o.OrderID AND od.ProductID = v.ProductID
    )
    AND NOT EXISTS (
        SELECT 1 FROM ReviewProducts x
        WHERE x.AccountID = a.AccountID AND x.OrderID = o.OrderID AND x.ProductID = v.ProductID
    );

    /* Staff replies */
    DECLARE @stfRpl2 INT=(SELECT TOP 1 AccountID FROM Accounts WHERE RoleID=3);
    INSERT INTO [dbo].[StaffReviewProductReplies] (ReviewProductID,StaffID,Content,CreatedAt)
    SELECT r.ReviewID, @stfRpl2,
           N'Cảm ơn quý khách rất nhiều vì review chi tiết! Chúng mình rất vui khi sản phẩm mang lại niềm vui cho bé. Hẹn gặp lại! 🎁',
           DATEADD(HOUR,3,r.CreatedAt)
    FROM ReviewProducts r;

    /* Review Images */
    INSERT INTO [dbo].[ReviewProductImages] (ReviewProductID,ImageURL,ModerationStatus,CreatedAt)
    SELECT ReviewID,'https://picsum.photos/seed/rv'+CAST(ReviewID AS VARCHAR)+'-1/300/300','Approved',GETDATE() FROM ReviewProducts;
    INSERT INTO [dbo].[ReviewProductImages] (ReviewProductID,ImageURL,ModerationStatus,CreatedAt)
    SELECT ReviewID,'https://picsum.photos/seed/rv'+CAST(ReviewID AS VARCHAR)+'-2/300/300','Approved',GETDATE() FROM ReviewProducts;
    INSERT INTO [dbo].[ReviewProductImages] (ReviewProductID,ImageURL,ModerationStatus,CreatedAt)
    SELECT ReviewID,'https://picsum.photos/seed/rv'+CAST(ReviewID AS VARCHAR)+'-3/300/300','Approved',GETDATE() FROM ReviewProducts WHERE ReviewID % 3 = 0;

    /* Review Reactions */
    INSERT INTO [dbo].[ReviewProductReactions] (ReviewProductID,AccountID,ReactionTypeID,CreatedAt)
    SELECT rp.ReviewID, a.AccountID, 1, GETDATE()
    FROM ReviewProducts rp
    CROSS JOIN (SELECT TOP 5 AccountID FROM Accounts WHERE RoleID=1 ORDER BY AccountID) a
    WHERE rp.AccountID <> a.AccountID
      AND NOT EXISTS (SELECT 1 FROM ReviewProductReactions x WHERE x.ReviewProductID=rp.ReviewID AND x.AccountID=a.AccountID);

    INSERT INTO [dbo].[ReviewProductReactions] (ReviewProductID,AccountID,ReactionTypeID,CreatedAt)
    SELECT rp.ReviewID, a.AccountID, 2, GETDATE()
    FROM ReviewProducts rp
    CROSS JOIN (SELECT TOP 3 AccountID FROM Accounts WHERE RoleID IN (2,3) ORDER BY AccountID) a
    WHERE rp.AccountID <> a.AccountID
      AND NOT EXISTS (SELECT 1 FROM ReviewProductReactions x WHERE x.ReviewProductID=rp.ReviewID AND x.AccountID=a.AccountID);
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 33 – SHIFT TEMPLATES
   FIX: Separated into its own GO batch so it always executes
        before the WHILE loop that references ShiftTemplateIDs.
        In v5.1 the Msg 102 error from S31 aborted the combined
        batch, leaving ShiftTemplates empty and causing every
        WorkSchedules INSERT to fail with FK_WorkSchedules_ShiftTemplates.
══════════════════════════════════════════════════════════════ */
PRINT N'[33/40] ShiftTemplates...';
SET IDENTITY_INSERT [dbo].[ShiftTemplates] ON;
IF NOT EXISTS (SELECT 1 FROM [dbo].[ShiftTemplates] WHERE ShiftTemplateID=1)
    INSERT INTO [dbo].[ShiftTemplates]
        (ShiftTemplateID,ShiftName,StartTime,EndTime,MaxOrdersPerShift,IsActive,CreatedAt)
    VALUES
    (1,N'Ca Sáng', '07:00:00','12:00:00',25,1,'2026-01-01 08:00:00'),
    (2,N'Ca Chiều','12:00:00','17:00:00',25,1,'2026-01-01 08:00:00'),
    (3,N'Ca Tối',  '17:00:00','22:00:00',20,1,'2026-01-01 08:00:00');
SET IDENTITY_INSERT [dbo].[ShiftTemplates] OFF;
GO

/* ── WorkSchedules & StaffShiftCapacity ── */
PRINT N'[33/40] WorkSchedules, StaffShiftCapacity...';
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

IF NOT EXISTS (SELECT 1 FROM [dbo].[StaffShiftCapacity])
    INSERT INTO [dbo].[StaffShiftCapacity] (ScheduleID,MaxLoad,CurrentLoad,UpdatedAt)
    SELECT ws.ScheduleID,
           st.MaxOrdersPerShift,
           CASE ws.Status WHEN 'Completed' THEN st.MaxOrdersPerShift
                          WHEN 'OnDuty'    THEN st.MaxOrdersPerShift / 2
                          ELSE 0 END,
           GETDATE()
    FROM WorkSchedules ws
    JOIN ShiftTemplates st ON st.ShiftTemplateID = ws.ShiftTemplateID;
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 34 – ORDER ASSIGNMENTS & QUEUE
   FIX: ScheduleID NULL resolved because S33 now inserts correctly.
══════════════════════════════════════════════════════════════ */
PRINT N'[34/40] OrderAssignments, OrderQueue...';
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
SELECT o.OrderID, @schedStf, @stfAsn, 3,
       CASE WHEN o.StatusID=8 THEN 0 ELSE 1 END,
       @admAsn, N'Phân ca tự động – Staff', DATEADD(HOUR,2,o.OrderDate)
FROM Orders o
WHERE @schedStf IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM OrderAssignments oa WHERE oa.OrderID=o.OrderID AND oa.RoleID=3);

INSERT INTO [dbo].[OrderAssignments]
    (OrderID,ScheduleID,AccountID,RoleID,IsActive,AssignedBy,Notes,AssignedAt)
SELECT o.OrderID, @schedMrc, @mrcAsn, 4,
       CASE WHEN o.StatusID=8 THEN 0 ELSE 1 END,
       @admAsn, N'Phân ca tự động – Merchandise', DATEADD(HOUR,3,o.OrderDate)
FROM Orders o
WHERE @schedMrc IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM OrderAssignments oa WHERE oa.OrderID=o.OrderID AND oa.RoleID=4);

IF NOT EXISTS (SELECT 1 FROM [dbo].[OrderQueue])
    INSERT INTO [dbo].[OrderQueue] (OrderID,QueuedAt,Reason,IsResolved)
    SELECT TOP 3 o.OrderID, GETDATE(),'NO_STAFF_ON_DUTY',0
    FROM Orders o WHERE o.StatusID=1 ORDER BY o.OrderDate ASC;
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 35 – NOTIFICATION TEMPLATES, CAMPAIGNS, DELIVERIES
   FIX: NotificationType 'REVIEW' is not in the CHECK constraint
        (allowed values: ORDER, PROMOTION, SYSTEM, BLOG, STOCK).
        Changed REVIEW_REMINDER deliveries to use 'ORDER' since
        review reminders are tied to completed orders.
══════════════════════════════════════════════════════════════ */
PRINT N'[35/40] Notification Templates, Campaigns, Stats, Deliveries...';
IF NOT EXISTS (SELECT 1 FROM [Notification].[Templates])
BEGIN
    INSERT INTO [Notification].[Templates]
        (TemplateCode,UsageScope,TitleTemplate,MessageTemplate,IsActive,IsDeleted,CreatedAt)
    VALUES
    ('ORDER_PLACED',         'SYSTEM',N'Đặt hàng thành công [OrderCode]',    N'Đơn hàng [OrderCode] trị giá [TotalAmount] của bạn đã được ghi nhận.',1,0,GETDATE()),
    ('ORDER_CONFIRMED',      'SYSTEM',N'Xác nhận đơn hàng [OrderCode]',      N'Đơn hàng [OrderCode] đã được shop xác nhận và đang chuẩn bị hàng.',1,0,GETDATE()),
    ('ORDER_SHIPPING',       'SYSTEM',N'Đơn hàng [OrderCode] đang giao',     N'Đơn hàng [OrderCode] đã bàn giao cho đơn vị vận chuyển [ShipperName].',1,0,GETDATE()),
    ('ORDER_DELIVERED',      'SYSTEM',N'Giao hàng thành công',               N'Đơn hàng [OrderCode] giao thành công. Đừng quên đánh giá để nhận ưu đãi!',1,0,GETDATE()),
    ('ORDER_CANCELLED',      'SYSTEM',N'Đơn hàng [OrderCode] đã hủy',       N'Đơn hàng [OrderCode] đã hủy với lý do: [CancelReason].',1,0,GETDATE()),
    ('PAYMENT_SUCCESS',      'SYSTEM',N'Thanh toán thành công',              N'Bạn đã thanh toán [Amount] đ cho đơn hàng [OrderCode].',1,0,GETDATE()),
    ('PAYMENT_FAILED',       'SYSTEM',N'Thanh toán thất bại',                N'Giao dịch [Amount] đ cho đơn [OrderCode] không thành công. Vui lòng thử lại.',1,0,GETDATE()),
    ('WALLET_TOPUP',         'SYSTEM',N'Nạp tiền thành công',                N'Ví của bạn đã được nạp [Amount] đ. Số dư: [Balance] đ.',1,0,GETDATE()),
    ('WALLET_REFUND',        'SYSTEM',N'Hoàn tiền thành công',               N'Bạn đã được hoàn [Amount] đ từ đơn [OrderCode].',1,0,GETDATE()),
    ('PRODUCT_BACK_IN_STOCK','SYSTEM',N'[ProductName] đã có hàng trở lại!',  N'Sản phẩm [ProductName] bạn quan tâm đã có hàng với giá [Price] đ.',1,0,GETDATE()),
    ('WISHLIST_PRICE_DROP',  'SYSTEM',N'Giá giảm: [ProductName]',            N'[ProductName] trong wishlist của bạn đang giảm còn [Price] đ.',1,0,GETDATE()),
    ('REVIEW_STAFF_REPLIED', 'SYSTEM',N'Phản hồi đánh giá của bạn',         N'Nhân viên vừa trả lời đánh giá của bạn cho sản phẩm [ProductName].',1,0,GETDATE()),
    ('STAFF_NEW_ORDER',      'SYSTEM',N'Đơn hàng mới: [OrderCode]',          N'Hệ thống ghi nhận đơn mới [OrderCode] trị giá [TotalAmount]. Vui lòng xử lý.',1,0,GETDATE()),
    ('STAFF_REFUND_REQUEST', 'SYSTEM',N'Yêu cầu hoàn tiền [OrderCode]',     N'Khách hàng [CustomerName] yêu cầu hoàn tiền cho đơn [OrderCode].',1,0,GETDATE()),
    ('MERCH_LOW_STOCK',      'SYSTEM',N'Cảnh báo sắp hết hàng',             N'Sản phẩm [ProductName] chỉ còn [Quantity] chiếc. Cần nhập thêm.',1,0,GETDATE()),
    ('MERCH_OUT_OF_STOCK',   'SYSTEM',N'Cảnh báo hết hàng',                 N'Sản phẩm [ProductName] đã hết hàng trong kho.',1,0,GETDATE()),
    ('ADMIN_JOB_FAILED',     'SYSTEM',N'Lỗi Background Job',                 N'Job [JobName] chạy thất bại lúc [Time]. Vui lòng kiểm tra log.',1,0,GETDATE()),
    ('ADMIN_SHIPPING_ERROR', 'SYSTEM',N'Lỗi đồng bộ vận chuyển',            N'Lỗi đồng bộ trạng thái vận chuyển đơn [OrderCode]: [ErrorMessage].',1,0,GETDATE()),
    ('BIRTHDAY_CHILD',       'SYSTEM',N'Chúc mừng sinh nhật bé [ChildName]!',N'Chúc bé [ChildName] mau ăn chóng lớn! ToyHouse có quà đặc biệt dành cho bé!',1,0,GETDATE()),
    ('FLASH_SALE_STARTED',   'ADMIN', N'[PromotionName] Bắt Đầu!',          N'Chương trình [PromotionName] đã mở bán từ [StartDate] đến [EndDate]. Chớp deal ngay!',1,0,GETDATE()),
    ('VOUCHER_NEW',          'ADMIN', N'Tặng bạn Voucher [VoucherCode]',    N'Bạn nhận được [VoucherCode] giảm [DiscountValue]. Áp dụng trước [ExpiryDate]!',1,0,GETDATE()),
    ('VOUCHER_EXPIRING',     'ADMIN', N'Voucher [VoucherCode] sắp hết!',    N'Đừng bỏ lỡ [VoucherCode]. Hết hạn vào [ExpiryDate]. Xài ngay!',1,0,GETDATE()),
    ('BIRTHDAY_CUSTOMER',    'ADMIN', N'Sinh nhật [CustomerName]!',         N'Chúc mừng sinh nhật! ToyHouse gửi tặng bạn voucher đặc biệt. Kiểm tra mục Voucher nhé!',1,0,GETDATE()),
    ('PROMO_REMINDER',       'ADMIN', N'Nhắc nhở: [PromotionName] sắp kết thúc!',N'Chương trình [PromotionName] sẽ kết thúc vào [EndDate]. Mua ngay!',1,0,GETDATE()),
    ('REVIEW_REMINDER',      'ADMIN', N'Đánh giá sản phẩm nhận xu thưởng!', N'Đơn hàng [OrderCode] đã giao thành công. Đánh giá ngay để nhận 50 xu!',1,0,GETDATE());
END
GO

DECLARE @admCamp INT=(SELECT TOP 1 AccountID FROM Accounts WHERE Email='admin@toyhouse.vn');
IF NOT EXISTS (SELECT 1 FROM [Notification].[Campaigns])
    INSERT INTO [Notification].[Campaigns]
        (CampaignName,TemplateCode,SourceType,TargetType,Status,ScheduledAt,CreatedByAccountID,CreatedAt)
    VALUES
    (N'Thông Báo Sale Hè Rực Rỡ 2026', 'FLASH_SALE_STARTED','ADMIN','ALL','Sent',NULL,@admCamp,'2026-03-28 08:00:00'),
    (N'Gửi Voucher Welcome Khách Mới',  'VOUCHER_NEW',       'ADMIN','ALL','Sent',NULL,@admCamp,'2026-01-01 08:00:00'),
    (N'Nhắc Nhở Flash Sale 5.5',        'FLASH_SALE_STARTED','ADMIN','ALL','Sent',NULL,@admCamp,'2026-04-30 08:00:00'),
    (N'Voucher Freeship Tháng 6',       'VOUCHER_NEW',       'ADMIN','ALL','Scheduled','2026-05-31 20:00:00',@admCamp,'2026-05-20 08:00:00'),
    (N'Chúc Mừng Sinh Nhật Batch 1',   'BIRTHDAY_CUSTOMER', 'SYSTEM','INDIVIDUAL','Sent',NULL,NULL,'2026-01-01 00:00:00'),
    (N'Nhắc Đánh Giá Sau Giao Hàng',   'REVIEW_REMINDER',   'SYSTEM','INDIVIDUAL','Sent',NULL,NULL,'2026-04-01 00:00:00'),
    (N'Voucher Expiring – WELCOME10',   'VOUCHER_EXPIRING',  'ADMIN','ALL','Sent',NULL,@admCamp,'2026-12-20 08:00:00');
GO

IF NOT EXISTS (SELECT 1 FROM [Notification].[CampaignStats])
    INSERT INTO [Notification].[CampaignStats] (CampaignID,TotalSent,TotalRead,TotalClicked,ComputedAt)
    SELECT CampaignID,
           CASE CampaignName WHEN N'Thông Báo Sale Hè Rực Rỡ 2026' THEN 46 WHEN N'Gửi Voucher Welcome Khách Mới' THEN 46
                             WHEN N'Nhắc Nhở Flash Sale 5.5' THEN 46 WHEN N'Nhắc Đánh Giá Sau Giao Hàng' THEN 30
                             WHEN N'Voucher Expiring – WELCOME10' THEN 40 ELSE 0 END,
           CASE CampaignName WHEN N'Thông Báo Sale Hè Rực Rỡ 2026' THEN 32 WHEN N'Gửi Voucher Welcome Khách Mới' THEN 38
                             WHEN N'Nhắc Nhở Flash Sale 5.5' THEN 25 WHEN N'Nhắc Đánh Giá Sau Giao Hàng' THEN 22
                             WHEN N'Voucher Expiring – WELCOME10' THEN 28 ELSE 0 END,
           CASE CampaignName WHEN N'Thông Báo Sale Hè Rực Rỡ 2026' THEN 18 WHEN N'Gửi Voucher Welcome Khách Mới' THEN 20
                             WHEN N'Nhắc Nhở Flash Sale 5.5' THEN 12 WHEN N'Nhắc Đánh Giá Sau Giao Hàng' THEN 15
                             WHEN N'Voucher Expiring – WELCOME10' THEN 14 ELSE 0 END,
           GETDATE()
    FROM [Notification].[Campaigns];
GO

/* ── Deliveries ── */
DECLARE @cmpSale INT = (SELECT TOP 1 CampaignID FROM [Notification].[Campaigns] WHERE CampaignName=N'Thông Báo Sale Hè Rực Rỡ 2026');
DECLARE @cmpRv   INT = (SELECT TOP 1 CampaignID FROM [Notification].[Campaigns] WHERE CampaignName=N'Nhắc Đánh Giá Sau Giao Hàng');

/* ORDER_PLACED notifications */
INSERT INTO [Notification].[Deliveries]
    (AccountID,CampaignID,TemplateCode,RecipientType,NotificationType,
     Title,Message,Payload,Status,IdempotencyKey,CreatedAt)
SELECT o.AccountID, @cmpSale, 'ORDER_PLACED', 'CUSTOMER','ORDER',
    N'Đặt hàng thành công',
    N'Đơn hàng '+o.OrderCode+N' đã được xác nhận. Chúng mình đang chuẩn bị hàng!',
    '{"orderId":'+CAST(o.OrderID AS VARCHAR)+',"orderCode":"'+o.OrderCode+'"}',
    'Unread', 'DLV-'+o.OrderCode, o.OrderDate
FROM Orders o
WHERE NOT EXISTS (SELECT 1 FROM [Notification].[Deliveries] d WHERE d.IdempotencyKey='DLV-'+o.OrderCode);

/* ORDER_DELIVERED notifications */
INSERT INTO [Notification].[Deliveries]
    (AccountID,CampaignID,TemplateCode,RecipientType,NotificationType,
     Title,Message,Payload,Status,IdempotencyKey,CreatedAt)
SELECT o.AccountID, @cmpSale, 'ORDER_DELIVERED', 'CUSTOMER','ORDER',
    N'Giao hàng thành công!',
    N'Đơn hàng '+o.OrderCode+N' đã giao thành công. Đừng quên đánh giá để nhận ưu đãi!',
    '{"orderId":'+CAST(o.OrderID AS VARCHAR)+',"orderCode":"'+o.OrderCode+'"}',
    'Unread', 'DLV-DEL-'+o.OrderCode, o.DeliveredAt
FROM Orders o
WHERE o.DeliveredAt IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM [Notification].[Deliveries] d WHERE d.IdempotencyKey='DLV-DEL-'+o.OrderCode);

/* REVIEW_REMINDER notifications
   FIX [S35]: Changed NotificationType from 'REVIEW' (invalid per CHECK constraint)
   to 'ORDER' – review reminders are order-lifecycle notifications.
*/
INSERT INTO [Notification].[Deliveries]
    (AccountID,CampaignID,TemplateCode,RecipientType,NotificationType,
     Title,Message,Payload,Status,IdempotencyKey,CreatedAt)
SELECT o.AccountID, @cmpRv, 'REVIEW_REMINDER', 'CUSTOMER','ORDER',
    N'Đánh giá sản phẩm nhận xu thưởng!',
    N'Đơn hàng '+o.OrderCode+N' đã giao thành công. Đánh giá ngay để nhận 50 xu!',
    '{"orderId":'+CAST(o.OrderID AS VARCHAR)+',"orderCode":"'+o.OrderCode+'"}',
    'Unread', 'DLV-RV-'+o.OrderCode, DATEADD(DAY,1,o.DeliveredAt)
FROM Orders o
WHERE o.StatusID IN (6,7) AND o.DeliveredAt IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM [Notification].[Deliveries] d WHERE d.IdempotencyKey='DLV-RV-'+o.OrderCode);

/* Mark delivered-order notifications as Read */
UPDATE d SET d.Status='Read', d.ReadAt=DATEADD(HOUR,2,d.CreatedAt)
FROM [Notification].[Deliveries] d
JOIN Orders o ON o.OrderID=CAST(JSON_VALUE(d.Payload,'$.orderId') AS INT)
WHERE o.StatusID IN (6,7) AND d.Status='Unread';
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 37 – SEARCH KEYWORDS (table not in schema, skipped)
══════════════════════════════════════════════════════════════ */
PRINT N'[37/40] SearchKeywordLogs (skipped – table not in schema)...';
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 38 – SYSTEM TABLES
══════════════════════════════════════════════════════════════ */
PRINT N'[38/40] BackgroundJobs, DomainEventOutbox, AuditLogs...';
IF NOT EXISTS (SELECT 1 FROM [System].[BackgroundJobs])
    INSERT INTO [System].[BackgroundJobs]
        (JobName,CronExpression,IsEnabled,LastRunStatus) VALUES
    ('SyncGHNStatus',      '*/5 * * * *', 1,'Completed'),
    ('RecalcTrending',     '0 * * * *',   1,'Completed'),
    ('ExpireVouchers',     '0 0 * * *',   1,'Completed'),
    ('ExpirePromotions',   '0 0 * * *',   1,'Completed'),
    ('SendPromoNotif',     '0 8 * * *',   1,'Completed'),
    ('UnblockUsers',       '*/15 * * * *',1,'Completed'),
    ('LowStockAlert',      '0 9 * * *',   1,'Pending'),
    ('BirthdayNotif',      '0 7 * * *',   1,'Pending'),
    ('RecalcUserScores',   '0 3 * * *',   1,'Completed'),
    ('CleanExpiredSessions','0 1 * * *',  1,'Completed'),
    ('ArchiveOldOrders',   '0 2 * * 0',   1,'Pending'),
    ('GenerateWeeklyReport','0 8 * * 1',  1,'Completed');

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

/* AuditLogs – only if table exists */
IF OBJECT_ID('System.AuditLogs','U') IS NOT NULL
BEGIN
    DECLARE @admAudit INT=(SELECT TOP 1 AccountID FROM Accounts WHERE Email='admin@toyhouse.vn');
    DECLARE @stfAudit INT=(SELECT TOP 1 AccountID FROM Accounts WHERE Email='nhung.st@toyhouse.vn');
    EXEC sp_executesql N'
    INSERT INTO [System].[AuditLogs]
        (AccountID, Action, EntityType, EntityId, OldValue, NewValue, IPAddress, UserAgent, CreatedAt)
    VALUES
    (@admAudit,''Create'',''Promotion'',
     CAST((SELECT TOP 1 PromotionID FROM Promotions WHERE PromotionName=N''Sale Hè Rực Rỡ 2026'') AS VARCHAR),
     NULL,N''{"name":"Sale Hè Rực Rỡ 2026","type":"DISCOUNT","priority":10}'',
     ''127.0.0.1'',''Mozilla/5.0 Admin Dashboard'',''2026-03-20 09:00:00''),
    (@admAudit,''Create'',''Voucher'',
     CAST((SELECT TOP 1 VoucherID FROM Vouchers WHERE VoucherCode=''VIP200K'') AS VARCHAR),
     NULL,N''{"code":"VIP200K","type":"FIXED","value":200000}'',
     ''127.0.0.1'',''Mozilla/5.0 Admin Dashboard'',''2026-03-20 09:05:00'');',
    N'@admAudit INT, @stfAudit INT', @admAudit=@admAudit, @stfAudit=@stfAudit;
END
GO

/* ══════════════════════════════════════════════════════════════
   SECTION 40 – VERIFICATION
══════════════════════════════════════════════════════════════ */
PRINT N'';
PRINT N'================================================================';
PRINT N'  DataSeed Fixed v5.2 – HOÀN TẤT!';
PRINT N'================================================================';
PRINT N'';

SELECT 'Roles'                               AS [Table], COUNT(*) AS [Rows] FROM Roles                      UNION ALL
SELECT 'Sexes',                                           COUNT(*)           FROM Sexes                      UNION ALL
SELECT 'Accounts',                                        COUNT(*)           FROM Accounts                   UNION ALL
SELECT 'Notification.UserPreferences',                    COUNT(*)           FROM [Notification].[UserPreferences] UNION ALL
SELECT 'Provinces',                                       COUNT(*)           FROM Provinces                  UNION ALL
SELECT 'Districts',                                       COUNT(*)           FROM Districts                  UNION ALL
SELECT 'Wards',                                           COUNT(*)           FROM Wards                      UNION ALL
SELECT 'Addresses',                                       COUNT(*)           FROM Addresses                  UNION ALL
SELECT 'UserBlockHistory',                                COUNT(*)           FROM UserBlockHistory           UNION ALL
SELECT 'SuperCategories',                                 COUNT(*)           FROM SuperCategories            UNION ALL
SELECT 'Categories',                                      COUNT(*)           FROM Categories                 UNION ALL
SELECT 'Brands',                                          COUNT(*)           FROM Brands                     UNION ALL
SELECT 'Materials',                                       COUNT(*)           FROM Materials                  UNION ALL
SELECT 'Ages',                                            COUNT(*)           FROM Ages                       UNION ALL
SELECT 'Origins',                                         COUNT(*)           FROM Origins                    UNION ALL
SELECT 'PriceRanges',                                     COUNT(*)           FROM PriceRanges                UNION ALL
SELECT 'ReactionTypes',                                   COUNT(*)           FROM ReactionTypes              UNION ALL
SELECT 'StatusOrders',                                    COUNT(*)           FROM StatusOrders               UNION ALL
SELECT 'BlockReasons',                                    COUNT(*)           FROM BlockReasons               UNION ALL
SELECT 'Products',                                        COUNT(*)           FROM Products                   UNION ALL
SELECT 'ProductDetails',                                  COUNT(*)           FROM ProductDetails             UNION ALL
SELECT 'ProductImages',                                   COUNT(*)           FROM ProductImages              UNION ALL
SELECT 'Promotions',                                      COUNT(*)           FROM Promotions                 UNION ALL
SELECT 'PromotionTimeSlots',                              COUNT(*)           FROM PromotionTimeSlots         UNION ALL
SELECT 'ProductPromotions',                               COUNT(*)           FROM ProductPromotions          UNION ALL
SELECT 'PromotionProductSlots',                           COUNT(*)           FROM PromotionProductSlots      UNION ALL
SELECT 'Vouchers',                                        COUNT(*)           FROM Vouchers                   UNION ALL
SELECT 'Orders',                                          COUNT(*)           FROM Orders                     UNION ALL
SELECT 'OrderDetails',                                    COUNT(*)           FROM OrderDetails               UNION ALL
SELECT 'OrderStatusHistory',                              COUNT(*)           FROM OrderStatusHistory         UNION ALL
SELECT 'OrderVouchers',                                   COUNT(*)           FROM OrderVouchers              UNION ALL
SELECT 'VoucherUsageLogs',                                COUNT(*)           FROM VoucherUsageLogs           UNION ALL
SELECT 'PaymentHistory',                                  COUNT(*)           FROM PaymentHistory             UNION ALL
SELECT 'PaymentGatewayTransactions',                      COUNT(*)           FROM PaymentGatewayTransactions UNION ALL
SELECT 'Wallets',                                         COUNT(*)           FROM Wallets                    UNION ALL
SELECT 'WalletTransactions',                              COUNT(*)           FROM WalletTransactions         UNION ALL
SELECT 'WalletPins',                                      COUNT(*)           FROM WalletPins                 UNION ALL
SELECT 'ShippingProviderTransactions',                    COUNT(*)           FROM ShippingProviderTransactions UNION ALL
SELECT 'ShippingStatusHistories',                         COUNT(*)           FROM ShippingStatusHistories    UNION ALL
SELECT 'OrderRefundReasons',                              COUNT(*)           FROM OrderRefundReasons         UNION ALL
SELECT 'OrderRefunds',                                    COUNT(*)           FROM OrderRefunds               UNION ALL
SELECT 'RefundImages',                                    COUNT(*)           FROM RefundImages               UNION ALL
SELECT 'Cart',                                            COUNT(*)           FROM Cart                       UNION ALL
SELECT 'CartItems',                                       COUNT(*)           FROM CartItems                  UNION ALL
SELECT 'Wishlists',                                       COUNT(*)           FROM Wishlists                  UNION ALL
SELECT 'ProductFollowers',                                COUNT(*)           FROM ProductFollowers           UNION ALL
SELECT 'CustomerChildren',                                COUNT(*)           FROM CustomerChildren           UNION ALL
SELECT 'BlogCategories',                                  COUNT(*)           FROM BlogCategories             UNION ALL
SELECT 'BlogPosts',                                       COUNT(*)           FROM BlogPosts                  UNION ALL
SELECT 'ReviewBlogs',                                     COUNT(*)           FROM ReviewBlogs                UNION ALL
SELECT 'ReviewBlogReplies',                               COUNT(*)           FROM ReviewBlogReplies          UNION ALL
SELECT 'ReviewBlogReactions',                             COUNT(*)           FROM ReviewBlogReactions        UNION ALL
SELECT 'BlogPostReactions',                               COUNT(*)           FROM BlogPostReactions          UNION ALL
SELECT 'ReviewProducts',                                  COUNT(*)           FROM ReviewProducts             UNION ALL
SELECT 'ReviewProductImages',                             COUNT(*)           FROM ReviewProductImages        UNION ALL
SELECT 'ReviewProductReactions',                          COUNT(*)           FROM ReviewProductReactions     UNION ALL
SELECT 'StaffReviewProductReplies',                       COUNT(*)           FROM StaffReviewProductReplies  UNION ALL
SELECT 'ReviewModerationLogs',                            COUNT(*)           FROM ReviewModerationLogs       UNION ALL
SELECT 'ShiftTemplates',                                  COUNT(*)           FROM ShiftTemplates             UNION ALL
SELECT 'WorkSchedules',                                   COUNT(*)           FROM WorkSchedules              UNION ALL
SELECT 'StaffShiftCapacity',                              COUNT(*)           FROM StaffShiftCapacity         UNION ALL
SELECT 'OrderAssignments',                                COUNT(*)           FROM OrderAssignments           UNION ALL
SELECT 'OrderQueue',                                      COUNT(*)           FROM OrderQueue                 UNION ALL
SELECT 'Notification.Templates',                          COUNT(*)           FROM [Notification].[Templates] UNION ALL
SELECT 'Notification.Campaigns',                          COUNT(*)           FROM [Notification].[Campaigns] UNION ALL
SELECT 'Notification.CampaignStats',                      COUNT(*)           FROM [Notification].[CampaignStats] UNION ALL
SELECT 'Notification.Deliveries',                         COUNT(*)           FROM [Notification].[Deliveries] UNION ALL
SELECT 'Recommendation.UserProductScores',                COUNT(*)           FROM [Recommendation].[UserProductScores] UNION ALL
SELECT 'Recommendation.TrendingProducts',                 COUNT(*)           FROM [Recommendation].[TrendingProducts] UNION ALL
SELECT 'Recommendation.ItemSimilarities',                 COUNT(*)           FROM [Recommendation].[ItemSimilarities] UNION ALL
SELECT 'Recommendation.Widgets',                          COUNT(*)           FROM [Recommendation].[Widgets] UNION ALL
SELECT 'Interaction.Events',                              COUNT(*)           FROM [Interaction].[Events]     UNION ALL
SELECT 'System.BackgroundJobs',                           COUNT(*)           FROM [System].[BackgroundJobs]  UNION ALL
SELECT 'System.DomainEventOutbox',                        COUNT(*)           FROM [System].[DomainEventOutbox]
ORDER BY 1;
GO
