/* ══════════════════════════════════════════════════════════════
   NOTIFICATION TEMPLATES — Full idempotent seed
   Chạy script này để đảm bảo toàn bộ TemplateCode cần thiết
   đã tồn tại trong [Notification].[Templates].

   An toàn để chạy lại nhiều lần — dùng NOT EXISTS guard.
   Chạy script này TRƯỚC khi khởi động ứng dụng sau refactor.
══════════════════════════════════════════════════════════════ */
BEGIN TRY
    BEGIN TRAN;

    DECLARE @Tpl TABLE (
        TemplateCode    VARCHAR(50)   NOT NULL PRIMARY KEY,
        UsageScope      VARCHAR(10)   NOT NULL,
        TitleTemplate   NVARCHAR(255) NOT NULL,
        MessageTemplate NVARCHAR(500) NOT NULL
    );

    /* ─── NHÓM SYSTEM: tự động, không tắt/xóa ─────────────────── */

    -- Đơn hàng — khách hàng
    INSERT INTO @Tpl VALUES
    ('ORDER_PLACED',          'SYSTEM', N'Đặt hàng thành công',
     N'Đơn hàng {{OrderCode}} trị giá {{TotalAmount}} đ của bạn đã được ghi nhận thành công.'),
    ('ORDER_CONFIRMED',       'SYSTEM', N'Xác nhận đơn hàng {{OrderCode}}',
     N'Đơn hàng {{OrderCode}} của bạn đã được shop xác nhận và chuẩn bị hàng.'),
    ('ORDER_PACKING',         'SYSTEM', N'Đơn hàng {{OrderCode}} đang đóng gói',
     N'Đơn hàng {{OrderCode}} của bạn đang được nhân viên đóng gói cẩn thận.'),
    ('ORDER_SHIPPING',        'SYSTEM', N'Đơn hàng {{OrderCode}} đang giao',
     N'Đơn hàng {{OrderCode}} đã được giao cho bưu tá {{ShipperName}}. Vui lòng chú ý điện thoại.'),
    ('ORDER_DELIVERED',       'SYSTEM', N'Giao hàng thành công',
     N'Đơn hàng {{OrderCode}} đã giao thành công. Đừng quên đánh giá sản phẩm để nhận ưu đãi nhé!'),
    ('ORDER_CANCELLED',       'SYSTEM', N'Đơn hàng {{OrderCode}} đã hủy',
     N'Đơn hàng {{OrderCode}} của bạn đã bị hủy với lý do: {{CancelReason}}.'),
    ('ORDER_DELIVERY_FAILED', 'SYSTEM', N'Giao hàng thất bại',
     N'Giao hàng đơn {{OrderCode}} không thành công do: {{FailReason}}. Vui lòng liên hệ CSKH.'),
    ('ORDER_ASSIGNED',        'SYSTEM', N'Đơn hàng {{OrderCode}} đã được phân công',
     N'Đơn hàng {{OrderCode}} đã được phân công cho bạn xử lý.');

    -- Thanh toán & Ví
    INSERT INTO @Tpl VALUES
    ('PAYMENT_SUCCESS',  'SYSTEM', N'Thanh toán thành công',
     N'Bạn đã thanh toán thành công {{Amount}} đ cho đơn hàng {{OrderCode}}.'),
    ('PAYMENT_FAILED',   'SYSTEM', N'Thanh toán thất bại',
     N'Giao dịch thanh toán {{Amount}} đ cho đơn hàng {{OrderCode}} không thành công.'),
    ('WALLET_TOPUP',     'SYSTEM', N'Nạp tiền thành công',
     N'Ví của bạn vừa được nạp thêm {{Amount}} đ. Số dư hiện tại: {{Balance}} đ.'),
    ('WALLET_REFUND',    'SYSTEM', N'Hoàn tiền vào ví',
     N'Bạn vừa được hoàn {{Amount}} đ vào ví từ đơn hàng {{OrderCode}}.'),
    ('REFUND_APPROVED',  'SYSTEM', N'Yêu cầu hoàn tiền được chấp thuận',
     N'Yêu cầu hoàn tiền cho đơn hàng {{OrderCode}} đã được chấp thuận. {{Amount}} đ sẽ được hoàn vào ví của bạn.'),
    ('REFUND_REJECTED',  'SYSTEM', N'Yêu cầu hoàn tiền bị từ chối',
     N'Yêu cầu hoàn tiền cho đơn hàng {{OrderCode}} đã bị từ chối. Vui lòng liên hệ CSKH nếu cần hỗ trợ.'),
    ('REFUND_COMPLETED', 'SYSTEM', N'Hoàn tiền hoàn tất',
     N'{{Amount}} đ từ đơn hàng {{OrderCode}} đã được hoàn thành công vào ví của bạn.');

    -- Sản phẩm & Tồn kho
    INSERT INTO @Tpl VALUES
    ('PRODUCT_BACK_IN_STOCK', 'SYSTEM', N'Sản phẩm {{ProductName}} đã có hàng',
     N'Sản phẩm {{ProductName}} bạn quan tâm đã có hàng trở lại với giá {{Price}} đ. Mua ngay kẻo hết!'),
    ('WISHLIST_PRICE_DROP',   'SYSTEM', N'Giảm giá sản phẩm {{ProductName}}',
     N'Sản phẩm {{ProductName}} trong wishlist của bạn đang giảm giá chỉ còn {{Price}} đ.');

    -- Review & Blog
    INSERT INTO @Tpl VALUES
    ('REVIEW_STAFF_REPLIED', 'SYSTEM', N'Phản hồi đánh giá sản phẩm {{ProductName}}',
     N'Nhân viên CSKH vừa trả lời đánh giá của bạn cho sản phẩm {{ProductName}}.'),
    ('BLOG_COMMENT_REPLIED', 'SYSTEM', N'Phản hồi bình luận bài viết {{BlogTitle}}',
     N'Bình luận của bạn trên bài viết {{BlogTitle}} vừa có lượt trả lời mới.');

    -- Thông báo cho Staff
    INSERT INTO @Tpl VALUES
    ('STAFF_NEW_ORDER',         'SYSTEM', N'Có đơn hàng mới: {{OrderCode}}',
     N'Hệ thống vừa ghi nhận đơn hàng mới {{OrderCode}} trị giá {{TotalAmount}} đ. Vui lòng xử lý.'),
    ('STAFF_CANCEL_REQUEST',    'SYSTEM', N'Yêu cầu hủy đơn {{OrderCode}}',
     N'Khách hàng {{CustomerName}} vừa gửi yêu cầu hủy đơn hàng {{OrderCode}}. Lý do: {{Reason}}.'),
    ('STAFF_REFUND_REQUEST',    'SYSTEM', N'Yêu cầu hoàn tiền {{OrderCode}}',
     N'Khách hàng {{CustomerName}} yêu cầu hoàn tiền cho đơn hàng {{OrderCode}}.'),
    ('STAFF_REVIEW_MODERATION', 'SYSTEM', N'Duyệt đánh giá mới',
     N'Có đánh giá {{Rating}} sao mới cho sản phẩm {{ProductName}} cần bạn kiểm duyệt.'),
    ('STAFF_LOW_RATING',        'SYSTEM', N'Cảnh báo đánh giá thấp',
     N'Sản phẩm {{ProductName}} vừa nhận một đánh giá {{Rating}} sao. Vui lòng kiểm tra và xử lý.'),
    ('STAFF_SHIFT_STARTED',     'SYSTEM', N'Ca làm việc {{ShiftName}} đã bắt đầu',
     N'Ca làm việc {{ShiftName}} của bạn đã bắt đầu. Chúc bạn làm việc hiệu quả!');

    -- Thông báo cho Merchandise
    INSERT INTO @Tpl VALUES
    ('MERCH_READY_TO_PACK', 'SYSTEM', N'Có đơn hàng chờ đóng gói',
     N'Đơn hàng {{OrderCode}} đã sẵn sàng để đóng gói.'),
    ('MERCH_PICKED_UP',     'SYSTEM', N'Bưu tá đã lấy hàng',
     N'Bưu tá đã lấy thành công kiện hàng của đơn {{OrderCode}}.'),
    ('MERCH_RETURNED',      'SYSTEM', N'Hàng hoàn về kho',
     N'Đơn hàng {{OrderCode}} đã bị hoàn trả về kho.'),
    ('MERCH_LOW_STOCK',     'SYSTEM', N'Cảnh báo sắp hết hàng',
     N'Sản phẩm {{ProductName}} trong kho chỉ còn {{Quantity}} chiếc. Cần nhập thêm.'),
    ('MERCH_OUT_OF_STOCK',  'SYSTEM', N'Cảnh báo hết hàng',
     N'Sản phẩm {{ProductName}} đã hoàn toàn hết hàng trong kho.');

    -- Thông báo cho Admin
    INSERT INTO @Tpl VALUES
    ('ADMIN_PAYMENT_ERROR',    'SYSTEM', N'Lỗi cổng thanh toán',
     N'Cổng thanh toán {{GatewayName}} báo lỗi: {{ErrorMessage}}.'),
    ('ADMIN_JOB_FAILED',       'SYSTEM', N'Lỗi Background Job',
     N'Background Job {{JobName}} chạy thất bại. Vui lòng kiểm tra log.'),
    ('ADMIN_OUTBOX_STUCK',     'SYSTEM', N'Lỗi Outbox Event bị kẹt',
     N'Outbox event {{EventType}} ({{EventId}}) đã đạt giới hạn retry. Vui lòng kiểm tra log.'),
    ('ADMIN_SHIPPING_ERROR',   'SYSTEM', N'Lỗi đồng bộ vận chuyển',
     N'Lỗi đồng bộ trạng thái vận chuyển cho đơn {{OrderCode}}: {{ErrorMessage}}.'),
    ('ADMIN_DAMAGE_LOST',      'SYSTEM', N'Hàng hóa thất lạc/hư hỏng',
     N'Ghi nhận đơn hàng {{OrderCode}} bị hư hỏng hoặc thất lạc trong quá trình vận chuyển.'),
    ('ADMIN_BLOG_PENDING',     'SYSTEM', N'Duyệt bài viết {{BlogTitle}}',
     N'Bài viết {{BlogTitle}} vừa được gửi và đang chờ bạn phê duyệt.'),
    ('ADMIN_ORDER_QUEUED',     'SYSTEM', N'Đơn hàng {{OrderCode}} đang chờ phân công',
     N'Đơn hàng {{OrderCode}} chưa được phân công do {{Reason}}. Vui lòng xử lý thủ công.'),
    ('ADMIN_SHIFT_ENDED_PENDING', 'SYSTEM', N'Ca {{ShiftName}} kết thúc còn đơn chờ',
     N'Ca làm việc {{ShiftName}} (nhân viên #{{AccountId}}) đã kết thúc với {{CurrentLoad}} đơn hàng đang xử lý dở.');

    /* ─── NHÓM ADMIN: marketing, bật/tắt được ──────────────────── */
    INSERT INTO @Tpl VALUES
    ('FLASH_SALE_STARTED', 'ADMIN', N'⚡ {{PromotionName}} Bắt Đầu!',
     N'Chương trình siêu sale {{PromotionName}} đã chính thức mở bán từ {{StartDate}} đến {{EndDate}}. Chớp deal ngay!'),
    ('VOUCHER_NEW',        'ADMIN', N'🎁 Tặng bạn Voucher {{VoucherCode}}',
     N'Bạn vừa nhận được mã {{VoucherCode}} giảm {{DiscountValue}} ({{DiscountType}}). Áp dụng ngay trước khi hết hạn vào {{ExpiryDate}}!'),
    ('VOUCHER_EXPIRING',   'ADMIN', N'⏰ Voucher {{VoucherCode}} sắp hết hạn!',
     N'Đừng bỏ lỡ mã {{VoucherCode}} (Giảm {{DiscountValue}}). Sẽ hết hạn vào ngày {{ExpiryDate}}. Xài ngay!'),
    ('BIRTHDAY_CUSTOMER',  'ADMIN', N'🎂 Chúc mừng sinh nhật {{CustomerName}}!',
     N'Chúc mừng sinh nhật bạn! ToyStore xin gửi tặng bạn một món quà đặc biệt. Vui lòng kiểm tra mục Voucher nhé!'),
    ('BIRTHDAY_CHILD',     'ADMIN', N'🎂 Chúc mừng sinh nhật bé {{ChildName}}!',
     N'Chúc mừng sinh nhật bé {{ChildName}}! ToyStore chúc bé mau ăn chóng lớn và luôn vui vẻ. Ba mẹ hãy chọn cho bé món đồ chơi yêu thích nhé!');

    /* ─── INSERT idempotent vào bảng thật ───────────────────────── */
    INSERT INTO [Notification].[Templates]
        ([TemplateCode], [UsageScope], [TitleTemplate], [MessageTemplate], [IsActive], [IsDeleted], [CreatedAt])
    SELECT t.TemplateCode, t.UsageScope, t.TitleTemplate, t.MessageTemplate, 1, 0, GETDATE()
    FROM   @Tpl t
    WHERE  NOT EXISTS (
        SELECT 1 FROM [Notification].[Templates] db
        WHERE  db.TemplateCode = t.TemplateCode
    );

    COMMIT TRAN;
    PRINT N'✅ notification_templates_full.sql: INSERT OK — '
        + CAST(@@ROWCOUNT AS VARCHAR) + N' template mới được thêm.';
END TRY
BEGIN CATCH
    ROLLBACK TRAN;
    PRINT N'❌ Lỗi: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO
