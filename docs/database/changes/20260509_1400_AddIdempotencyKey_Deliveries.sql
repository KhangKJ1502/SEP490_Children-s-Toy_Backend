-- ============================================================
-- Người thực hiện: System
-- Ngày: 2026-05-09
-- Mô tả thay đổi: Thêm cột IdempotencyKey vào [Notification].[Deliveries]
--                 + Unique filtered index để đảm bảo idempotency khi Job retry
-- ============================================================

USE [SEP409_ToyStore];
GO

ALTER TABLE [Notification].[Deliveries]
ADD [IdempotencyKey] VARCHAR(200) NULL;
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ_Deliveries_IdempotencyKey]
ON [Notification].[Deliveries]([IdempotencyKey])
WHERE [IdempotencyKey] IS NOT NULL;
GO
