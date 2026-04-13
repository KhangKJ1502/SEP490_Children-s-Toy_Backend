# Database Changelog — ToyStore (SEP490)

> **Quy tắc:** Entry mới nhất nằm TRÊN CÙNG.
> **Cập nhật file này** mỗi khi thay đổi schema (thêm bảng, cột, index).

---

## [2026-04-13] `v1.0 — InitialCreate`

### Thay đổi

| Loại | Bảng | Chi tiết |
|---|---|---|
| ➕ Schema | `Notification`, `Interaction`, `Recommendation`, `System` | Tạo 4 schema riêng biệt |
| ➕ Thêm bảng | `System.DomainEventOutbox` | Outbox pattern cho Event-Driven |
| ➕ Thêm bảng | `System.BackgroundJobs` | Quản lý background jobs / cron |
| ➕ Thêm bảng | `Roles` | Vai trò người dùng (Customer, Staff, Admin, ...) |
| ➕ Thêm bảng | `Accounts` | Tài khoản người dùng — Email unique, soft delete |
| ➕ Thêm bảng | `BlockReasons` | Danh sách lý do khóa tài khoản |
| ➕ Thêm bảng | `UserBlockHistory` | Lịch sử khóa/mở khóa tài khoản |
| ➕ Thêm bảng | `Addresses` | Địa chỉ giao hàng của user |
| ➕ Thêm bảng | `SuperCategories` | Danh mục lớn (Level 1) |
| ➕ Thêm bảng | `Categories` | Danh mục sản phẩm (Level 2, FK→SuperCategories) |
| ➕ Thêm bảng | `Materials` | Chất liệu đồ chơi |
| ➕ Thêm bảng | `Ages` | Độ tuổi phù hợp |
| ➕ Thêm bảng | `Sexes` | Giới tính phù hợp |
| ➕ Thêm bảng | `Origins` | Xuất xứ sản phẩm |
| ➕ Thêm bảng | `Brands` | Thương hiệu |
| ➕ Thêm bảng | `PriceRanges` | Khoảng giá để lọc |
| ➕ Thêm bảng | `Promotions` | Chương trình khuyến mãi theo % |
| ➕ Thêm bảng | `Products` | Sản phẩm đồ chơi — với FK→Categories/Brands/PriceRanges/Promotions |
| ➕ Thêm bảng | `ProductDetails` | 1-1 với Products — mô tả, vật liệu, tuổi, giới tính, xuất xứ |
| ➕ Thêm bảng | `ProductImages` | Ảnh sản phẩm (1 ảnh chính unique per product) |
| ➕ Thêm bảng | `StatusOrders` | Bảng trạng thái đơn hàng (lookup table) |
| ➕ Thêm bảng | `Orders` | Đơn hàng — với computed TotalAmount (trigger) |
| ➕ Thêm bảng | `OrderDetails` | Chi tiết đơn — LineTotal là COMPUTED PERSISTED |
| ➕ Thêm bảng | `OrderStatusHistory` | Audit log thay đổi trạng thái đơn |
| ➕ Thêm bảng | `Cart` | Giỏ hàng — 1 giỏ per user |
| ➕ Thêm bảng | `CartItems` | Sản phẩm trong giỏ — soft remove via RemovedAt |
| ➕ Thêm bảng | `Wishlists` | Sản phẩm yêu thích |
| ➕ Thêm bảng | `VoucherTypes` | Loại voucher |
| ➕ Thêm bảng | `Vouchers` | Mã giảm giá — có MaxUsagePerUser, số lượng |
| ➕ Thêm bảng | `OrderVouchers` | Composite PK — voucher áp dụng cho đơn |
| ➕ Thêm bảng | `VoucherUsageLogs` | Lịch sử sử dụng voucher |
| ➕ Thêm bảng | `BlogCategories` | Danh mục bài viết blog |
| ➕ Thêm bảng | `BlogPosts` | Bài viết blog — có workflow duyệt |
| ➕ Thêm bảng | `BlogPostCategories` | N-N: bài viết ↔ danh mục blog |
| ➕ Thêm bảng | `Banners` | Banner quảng cáo theo vị trí |
| ➕ Thêm bảng | `ReviewProducts` | Đánh giá sản phẩm (rating 1-5, phải có đơn hàng) |
| ➕ Thêm bảng | `ReviewProductImages` | Ảnh kèm đánh giá |
| ➕ Thêm bảng | `ReviewProductReplies` | Phản hồi đánh giá |
| ➕ Thêm bảng | `ReviewProductReactions` | Like/Dislike đánh giá |
| ➕ Thêm bảng | `Wallets` | Ví điện tử — 1 ví per user |
| ➕ Thêm bảng | `WalletTransactions` | Giao dịch ví — double-entry (CR/DR) |
| ➕ Thêm bảng | `PaymentHistory` | Lịch sử thanh toán |
| ➕ Thêm bảng | `OrderRefunds` | Yêu cầu hoàn tiền |
| ➕ Thêm bảng | `Notification.Templates` | Template thông báo |
| ➕ Thêm bảng | `Notification.Deliveries` | Thông báo đã gửi tới user |
| ➕ Thêm bảng | `ChatConversations` | Hội thoại chatbot (hỗ trợ guest) |
| ➕ Thêm bảng | `ChatMessages` | Tin nhắn trong hội thoại |
| ➕ Thêm bảng | `Interaction.Events` | Log hành vi user (click, view, ...) |
| ➕ Thêm bảng | `Recommendation.ItemSimilarities` | Độ tương đồng sản phẩm cho AI |
| ➕ Triggers | Nhiều bảng | 9 triggers (xem erd.md mục Triggers) |
| ➕ Indexes | Nhiều bảng | 25+ indexes tối ưu performance |
| ➕ Constraints | Nhiều bảng | CHECK constraints cho business rules |

### Lý do

> Khởi tạo toàn bộ schema ban đầu cho hệ thống ToyStore E-Commerce (SEP490).
> Bao gồm: quản lý tài khoản & phân quyền, catalog sản phẩm đồ chơi, quản lý đơn hàng,
> giỏ hàng, voucher, blog, đánh giá, thanh toán + ví điện tử, chatbot, AI recommendation.

### Bảng bị ảnh hưởng

Tất cả bảng (schema khởi tạo lần đầu).

### Cách áp dụng

```sql
-- Chạy file SQL trực tiếp trên SQL Server Management Studio (SSMS)
-- Hoặc dùng sqlcmd:
sqlcmd -S localhost -U sa -P "YourPassword" -i docs/database/schema.sql
```

---

<!-- TEMPLATE — copy và điền khi thêm thay đổi mới

## [YYYY-MM-DD] `Mô tả ngắn thay đổi`

### Thay đổi

| Loại | Bảng | Chi tiết |
|---|---|---|
| ➕ Thêm bảng | `` |  |
| ➕ Thêm cột | `.` | kiểu dữ liệu — nullable/not null |
| 🔧 Sửa cột | `.` | Thay đổi gì |
| ➕ Thêm index | `` | Mục đích |
| ➕ Thêm FK | `→` | ON DELETE behavior |
| 🗑️ Xoá cột | `.` | Lý do xoá |

### Lý do

> Mô tả feature yêu cầu thay đổi này.

### Bảng bị ảnh hưởng

``, ``

### Cách áp dụng

```sql
-- SQL script thêm vào schema.sql và chạy lại, HOẶC chạy ALTER TABLE riêng:
ALTER TABLE [Bảng] ADD [CộtMới] kiểu_dữ_liệu NULL;
```

-->
