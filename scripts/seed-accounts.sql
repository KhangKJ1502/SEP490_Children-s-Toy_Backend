-- =============================================================
-- SEED DATA: Roles + Admin/Staff/Merchandise accounts
-- PasswordHash = SHA256 UTF-8 (khớp với C# SHA256.HashData(Encoding.UTF8))
-- QUAN TRỌNG: KHÔNG dùng N prefix (N'...') trong HASHBYTES vì SQL Server
--             sẽ dùng UTF-16, trong khi C# dùng UTF-8 → hash khác nhau!
-- Mật khẩu: Admin@123 | Staff@123 | Mechandise@123
-- =============================================================

USE [SEP490_ToyStore];
GO

-- =============================================
-- 1. Seed Roles (nếu chưa có)
-- =============================================
IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [RoleName] = N'Customer')
    INSERT INTO [Roles] ([RoleName], [Description]) VALUES (N'Customer', N'Khách hàng mua sắm');
IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [RoleName] = N'Admin')
    INSERT INTO [Roles] ([RoleName], [Description]) VALUES (N'Admin', N'Quản trị viên hệ thống');
IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [RoleName] = N'Staff')
    INSERT INTO [Roles] ([RoleName], [Description]) VALUES (N'Staff', N'Nhân viên hỗ trợ');
IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [RoleName] = N'Merchandise')
    INSERT INTO [Roles] ([RoleName], [Description]) VALUES (N'Merchandise', N'Nhân viên quản lý hàng hóa');
GO

-- Lấy RoleID động vì IDENTITY không đảm bảo thứ tự khi insert từng cái
DECLARE @CustomerRoleId TINYINT = (SELECT [RoleID] FROM [Roles] WHERE [RoleName] = 'Customer');
DECLARE @AdminRoleId    TINYINT = (SELECT [RoleID] FROM [Roles] WHERE [RoleName] = 'Admin');
DECLARE @StaffRoleId    TINYINT = (SELECT [RoleID] FROM [Roles] WHERE [RoleName] = 'Staff');
DECLARE @MercRoleId     TINYINT = (SELECT [RoleID] FROM [Roles] WHERE [RoleName] = 'Merchandise');

-- =============================================
-- 2. Seed Admin account
--    Password: Admin@123
--    Hash = SHA256 of UTF-8 bytes (KHÔNG dùng N prefix)
-- =============================================
IF NOT EXISTS (SELECT 1 FROM [Accounts] WHERE [Email] = 'admin@toystore.com')
BEGIN
    INSERT INTO [Accounts] (
        [RoleID], [EmployeeCode], [AccountName],
        [Email], [PasswordHash],
        [IsActive], [IsDeleted], [Provider], [CreatedAt]
    )
    VALUES (
        @AdminRoleId,
        '0001AD',
        N'Admin ToyStore',
        'admin@toystore.com',
        UPPER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'Admin@123'), 2)),
        1, 0, 'Local', GETDATE()
    );
    PRINT 'Admin account created: admin@toystore.com / Admin@123';
END
ELSE
BEGIN
    -- Cập nhật hash nếu đã tồn tại (phòng trường hợp đã seed bằng script cũ bị lỗi N prefix)
    UPDATE [Accounts]
    SET [PasswordHash] = UPPER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'Admin@123'), 2)),
        [UpdatedAt] = GETDATE()
    WHERE [Email] = 'admin@toystore.com';
    PRINT 'Admin account already exists - password hash updated.';
END
GO

-- =============================================
-- 3. Seed Staff account
--    Password: Staff@123
-- =============================================
DECLARE @StaffRoleId TINYINT = (SELECT [RoleID] FROM [Roles] WHERE [RoleName] = 'Staff');

IF NOT EXISTS (SELECT 1 FROM [Accounts] WHERE [Email] = 'staff@toystore.com')
BEGIN
    INSERT INTO [Accounts] (
        [RoleID], [EmployeeCode], [AccountName],
        [Email], [PasswordHash],
        [IsActive], [IsDeleted], [Provider], [CreatedAt]
    )
    VALUES (
        @StaffRoleId,
        '0002ST',
        N'Staff ToyStore',
        'staff@toystore.com',
        UPPER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'Staff@123'), 2)),
        1, 0, 'Local', GETDATE()
    );
    PRINT 'Staff account created: staff@toystore.com / Staff@123';
END
ELSE
BEGIN
    UPDATE [Accounts]
    SET [PasswordHash] = UPPER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'Staff@123'), 2)),
        [UpdatedAt] = GETDATE()
    WHERE [Email] = 'staff@toystore.com';
    PRINT 'Staff account already exists - password hash updated.';
END
GO

-- =============================================
-- 4. Seed Merchandise account
--    Password: Mechandise@123
-- =============================================
DECLARE @MercRoleId TINYINT = (SELECT [RoleID] FROM [Roles] WHERE [RoleName] = 'Merchandise');

IF NOT EXISTS (SELECT 1 FROM [Accounts] WHERE [Email] = 'merchandise@toystore.com')
BEGIN
    INSERT INTO [Accounts] (
        [RoleID], [EmployeeCode], [AccountName],
        [Email], [PasswordHash],
        [IsActive], [IsDeleted], [Provider], [CreatedAt]
    )
    VALUES (
        @MercRoleId,
        '0003MC',
        N'Merchandise ToyStore',
        'merchandise@toystore.com',
        UPPER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'Mechandise@123'), 2)),
        1, 0, 'Local', GETDATE()
    );
    PRINT 'Merchandise account created: merchandise@toystore.com / Mechandise@123';
END
ELSE
BEGIN
    UPDATE [Accounts]
    SET [PasswordHash] = UPPER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'Mechandise@123'), 2)),
        [UpdatedAt] = GETDATE()
    WHERE [Email] = 'merchandise@toystore.com';
    PRINT 'Merchandise account already exists - password hash updated.';
END
GO

-- =============================================
-- 5. Verify seeded accounts
-- =============================================
SELECT
    a.[AccountID],
    a.[AccountName],
    a.[Email],
    r.[RoleName],
    a.[EmployeeCode],
    a.[IsActive],
    a.[CreatedAt]
FROM [Accounts] a
INNER JOIN [Roles] r ON r.[RoleID] = a.[RoleID]
WHERE a.[Email] IN (
    'admin@toystore.com',
    'staff@toystore.com',
    'merchandise@toystore.com'
);
GO
