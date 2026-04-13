-- ============================================================
-- TEMPLATE: SQL Change Script
-- Copy file này, đổi tên theo format: YYYYMMDD_HHMM_MoTa.sql
-- Ví dụ: 20260415_0930_AddWeightToProducts.sql
-- ============================================================

-- Người thực hiện:
-- Ngày:
-- Mô tả thay đổi:

-- ============================================================
-- NỘI DUNG THAY ĐỔI (xoá các ví dụ không dùng, giữ lại cái cần)
-- ============================================================

USE [SEP409_ToyStore];
GO

-- VÍ DỤ 1: Thêm cột
-- ALTER TABLE [Products] ADD [Weight] DECIMAL(10,2) NULL;
-- GO

-- VÍ DỤ 2: Sửa cột (tăng độ dài)
-- ALTER TABLE [Products] ALTER COLUMN [ProductName] NVARCHAR(500) NOT NULL;
-- GO

-- VÍ DỤ 3: Thêm bảng mới
-- CREATE TABLE [NewTable] (
--     [Id] INT IDENTITY(1,1) PRIMARY KEY,
--     [Name] NVARCHAR(100) NOT NULL,
--     [CreatedAt] DATETIME2(0) NOT NULL DEFAULT GETDATE()
-- );
-- GO

-- VÍ DỤ 4: Thêm FK
-- ALTER TABLE [Products] ADD CONSTRAINT [FK_Products_NewTable]
--     FOREIGN KEY ([NewTableId]) REFERENCES [NewTable]([Id]);
-- GO

-- VÍ DỤ 5: Thêm index
-- CREATE NONCLUSTERED INDEX [IX_Products_Weight] ON [Products]([Weight]);
-- GO

-- VÍ DỤ 6: Xoá cột
-- ALTER TABLE [Products] DROP COLUMN [OldColumn];
-- GO

-- VÍ DỤ 7: Thêm cột NOT NULL (phải có DEFAULT hoặc update data trước)
-- ALTER TABLE [Products] ADD [NewRequiredField] NVARCHAR(50) NOT NULL DEFAULT 'default_value';
-- GO
-- Sau khi data ổn, có thể xoá DEFAULT nếu cần:
-- ALTER TABLE [Products] DROP CONSTRAINT [DF_Products_NewRequiredField];
-- GO
