-- ============================================================
-- SQL Change Script
-- Người thực hiện: Antigravity
-- Ngày: 2026-06-04
-- Mô tả thay đổi: Loại bỏ thuộc tính ImageURL ở bảng Vouchers
-- ============================================================

USE [SEP490_ToyStore];
GO

-- Xoá cột ImageURL trong bảng Vouchers
ALTER TABLE [Vouchers] DROP COLUMN [ImageURL];
GO
