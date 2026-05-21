/*
  ADMIN_SHIFT_FULL — Notify admins when shift capacity reaches max concurrent orders.
*/
IF NOT EXISTS (
    SELECT 1 FROM [Notification].[Templates] WHERE [TemplateCode] = N'ADMIN_SHIFT_FULL'
)
BEGIN
    INSERT INTO [Notification].[Templates]
        ([TemplateCode],[UsageScope],[TitleTemplate],[MessageTemplate],[IsActive],[IsDeleted],[CreatedAt])
    VALUES (
        N'ADMIN_SHIFT_FULL',
        N'SYSTEM',
        N'Ca {{ShiftName}} đã đầy đơn ngày {{WorkDate}}',
        N'Một hoặc hai vai trò trong ca {{ShiftName}} đã đạt giới hạn số đơn xử lý đồng thời. Ngày {{WorkDate}}. Vui lòng xử lý thủ công (tăng MaxLoad hoặc phân đơn lại).',
        1,
        0,
        SYSUTCDATETIME()
    );
END
GO
