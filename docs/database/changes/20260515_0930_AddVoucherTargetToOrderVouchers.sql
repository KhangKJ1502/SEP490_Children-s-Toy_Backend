-- ============================================================
-- Add VoucherTarget to OrderVouchers for order vs shipping
-- Người thực hiện:
-- Ngày: 2026-05-15
-- Mô tả: Store voucher target (ORDER_TOTAL/SHIPPING_FEE) per order
-- ============================================================

USE [SEP490_ToyStore];
GO

ALTER TABLE [OrderVouchers]
    ADD [VoucherTarget] VARCHAR(20) NULL;
GO

UPDATE ov
SET ov.[VoucherTarget] = v.[DiscountTarget]
FROM [OrderVouchers] ov
    JOIN [Vouchers] v ON v.[VoucherID] = ov.[VoucherID]
WHERE ov.[VoucherTarget] IS NULL;
GO

ALTER TABLE [OrderVouchers]
    ALTER COLUMN [VoucherTarget] VARCHAR(20) NOT NULL;
GO

ALTER TABLE [OrderVouchers]
    ADD CONSTRAINT [CK_OrderVouchers_VoucherTarget]
        CHECK ([VoucherTarget] IN ('ORDER_TOTAL', 'SHIPPING_FEE'));
GO
