-- Tác giả: Antigravity AI
-- Ngày tạo: 2026-05-26 10:40:00
-- Mô tả: Cho phép lưu VoucherTarget = 'FINAL_PRICE' vào bảng OrderVouchers bằng cách cập nhật check constraint.

USE [SEP490_ChildrensToyStore]; -- Hoặc tên DB hiện tại của bạn
GO

PRINT 'Cập nhật check constraint bảng OrderVouchers...';

-- 1. Drop check constraint cũ
IF EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_OrderVouchers_VoucherTarget' AND parent_object_id = OBJECT_ID('OrderVouchers'))
BEGIN
    ALTER TABLE [OrderVouchers] DROP CONSTRAINT [CK_OrderVouchers_VoucherTarget];
    PRINT 'Đã drop check constraint CK_OrderVouchers_VoucherTarget cũ.';
END
GO

-- 2. Add check constraint mới cho phép 'FINAL_PRICE'
ALTER TABLE [OrderVouchers]
    ADD CONSTRAINT [CK_OrderVouchers_VoucherTarget] CHECK ([VoucherTarget] IN ('ORDER_TOTAL', 'SHIPPING_FEE', 'FINAL_PRICE'));
PRINT 'Đã tạo check constraint CK_OrderVouchers_VoucherTarget mới hỗ trợ FINAL_PRICE.';
GO
