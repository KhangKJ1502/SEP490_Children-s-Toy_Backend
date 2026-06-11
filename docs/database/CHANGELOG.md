# Database Changelog â€” ToyStore (SEP490)

> **Quy táº¯c:** Entry má»›i nháº¥t náº±m TRÃŠN CÃ™NG.
> **Cáº­p nháº­t file nÃ y** má»—i khi thay Ä‘á»•i schema (thÃªm báº£ng, cá»™t, index).

## [2026-06-11] `v3.7 - Add SavedBankAccounts and WithdrawalRequests`

### Thay đổi

| Loại | Bảng | Chi tiết |
|---|---|---|
| ➕ Thêm bảng | `dbo.SavedBankAccounts` | Bảng quản lý danh sách tài khoản ngân hàng đã lưu của user (schema v3.2) |
| ➕ Thêm bảng | `dbo.WithdrawalRequests` | Bảng quản lý các yêu cầu rút tiền qua PayOS (schema v3.2) |
| ➕ Thêm index | `UQ_SavedBankAccounts_OneDefault` | Unique filtered index đảm bảo chỉ có tối đa 1 tài khoản mặc định/user |

### Lý do

> Tích hợp tính năng rút tiền và quản lý tài khoản ngân hàng liên kết, hỗ trợ gọi BankLookup API tra cứu tên chủ tài khoản và đồng bộ luồng rút tiền PayOS.

### Script

`docs/database/changes/20260611_SavedBankAccounts.sql`

---

## [2026-06-04] `v3.6 - Remove Voucher ImageURL`

### Thay đổi

| Loại | Bảng | Chi tiết |
|---|---|---|
| 🗑️ Xoá cột | `dbo.Vouchers` | Xoá cột `ImageURL` không còn sử dụng |

### Lý do

> Đơn giản hóa cấu trúc Voucher và loại bỏ thuộc tính hình ảnh không cần thiết của mã giảm giá.

### Script

`docs/database/changes/20260604_2028_RemoveVoucherImageUrl.sql`

---

## [2026-05-26] `v3.3 - Add Refund/Return Tables`

### Thay đổi

| Loại | Bảng | Chi tiết |
|---|---|---|
| ➕ Thêm bảng | `dbo.StatusRefunds` | Bảng danh mục trạng thái hoàn tiền |
| ➕ Thêm bảng | `dbo.RefundRequests` | Bảng yêu cầu hoàn tiền/trả hàng |
| ➕ Thêm bảng | `dbo.RefundDetails` | Chi tiết sản phẩm trả lại (hỗ trợ Partial Refund) |
| ➕ Thêm bảng | `dbo.RefundStatusHistory` | Lịch sử thay đổi trạng thái hoàn tiền |

### Lý do

> Phân tách hoàn toàn domain mua hàng (Purchase Order) và domain trả hàng (Refund/Return Request), hỗ trợ hoàn hảo cho luồng partial refund và tích hợp với GHN reverse shipment.

### Script

`docs/database/changes/20260526_1510_AddRefundTables.sql`

---

## [2026-05-19] Shift full alert + scheduling rules

### Thay doi

| Loại | Chi tiết |
|------|----------|
| Cột mới | `dbo.StaffShiftCapacity.ShiftFullNotifiedAt` — gửi cảnh báo admin một lần khi đạt MaxLoad |
| Template | `ADMIN_SHIFT_FULL` trong `[Notification].[Templates]` |

### Script

- `docs/database/changes/20260519_ShiftFull_NotifiedAt.sql`
- `docs/database/changes/20260519_AdminShiftFull_NotificationTemplate.sql`

---

## [2026-05-14] `v3.4 - Shift scheduling + auto assignment (Luong B)`

### Thay doi

| Loai           | Bang/Thu tuc                                                        | Chi tiet                                                 |
| -------------- | ------------------------------------------------------------------- | -------------------------------------------------------- |
| + Them bang    | `dbo.ShiftTemplates`                                                | Mau ca lam viec, gio bat dau/ket thuc, MaxOrdersPerShift |
| + Them bang    | `dbo.WorkSchedules`                                                 | Lich lam viec theo ngay, trang thai ca                   |
| + Them bang    | `dbo.StaffShiftCapacity`                                            | Tai nang luc real-time cho ca                            |
| + Them bang    | `dbo.OrderAssignments`                                              | Lich su phan cong don (active/inactive)                  |
| + Them bang    | `dbo.OrderQueue`                                                    | Hang cho khi khong co nguoi kha dung                     |
| + Them trigger | `TR_WorkSchedules_CreateCapacity`                                   | Tu dong tao capacity khi insert WorkSchedules            |
| + Them SP      | `sp_AutoAssignOrder`, `sp_ReleaseOrderCapacity`, `sp_ReassignOrder` | Xu ly phan don, giai phong tai, reassign                 |

### Ly do

Bo sung Luong B: quan ly ca, auto-assign theo least-loaded, queue retry va ban giao ca.

### Script

`docs/database/changes/20260514_1300_ShiftScheduling_AutoAssign.sql`

---

## [2026-05-13] `v3.4 - Add Inactive Status to Promotion and PromotionTimeSlot`

### Thay đổi

| Loại | Bảng | Chi tiết |
|---|---|---|
| 🔧 Sửa constraint | `CK_Promotions_Status` | Thêm 'Inactive' vào danh sách trạng thái hợp lệ |
| 🔧 Sửa constraint | `CK_PromotionTimeSlots_Status` | Thêm 'Inactive' vào danh sách trạng thái hợp lệ |

### Lý do

Hỗ trợ chức năng dừng khẩn cấp (Emergency Stop) cho các PromotionTimeSlot đang Active, và tạm hoãn các Promotion đang Scheduled. 
Khi chuyển sang Inactive, TimeSlot sẽ không còn khả dụng cho user.

### Script

`docs/database/changes/20260513_2022_AddInactiveStatusToPromotions.sql`

---

## [2026-05-11] `v3.3 - Wallet PIN tables: WalletPins + WalletPinAttempts`

### Thay doi

| Loai         | Bang                          | Chi tiet                                                                                                     |
| ------------ | ----------------------------- | ------------------------------------------------------------------------------------------------------------ |
| + Them bang  | `dbo.WalletPins`              | Luu hash PIN, trang thai active, so lan nhap sai, lock time va lich su doi PIN                               |
| + Them index | `UQ_WalletPins_WalletID`      | Unique filtered index (`WHERE IsActive = 1`) dam bao moi vi chi co 1 PIN dang active                         |
| + Them bang  | `dbo.WalletPinAttempts`       | Log tung lan nhap PIN theo `ActionType` (`PAYMENT`, `VIEW_BALANCE`, `TOP_UP`) va ket qua thanh cong/that bai |
| + Them index | `IX_WalletPinAttempts_Wallet` | Toi uu truy van lich su nhap PIN theo vi va thoi gian moi nhat                                               |

### Ly do

Can bo sung lop bao mat PIN cho vi dien tu, theo doi so lan nhap sai de lock tam thoi, dong thoi luu audit cho cac thao tac can xac thuc PIN.

### Script

`docs/database/changes/20260511_1700_AddWalletPinTables.sql`

---

## [2026-05-09] `v3.2 â€” Notification: BirthdayNotifiedYear trÃªn CustomerChildren`

### Thay Ä‘á»•i

| Loáº¡i          | Báº£ng                                      | Chi tiáº¿t                                                                       |
| --------------- | ------------------------------------------- | -------------------------------------------------------------------------------- |
| âž• ThÃªm cá»™t | `dbo.CustomerChildren.BirthdayNotifiedYear` | `SMALLINT NULL` â€” nÄƒm gáº§n nháº¥t Ä‘Ã£ gá»­i thÃ´ng bÃ¡o sinh nháº­t cho bÃ© |

### LÃ½ do

`BirthdayNotificationJob` cáº§n biáº¿t Ä‘Ã£ gá»­i thÃ´ng bÃ¡o sinh nháº­t cho bÃ© trong nÄƒm hiá»‡n táº¡i chÆ°a. Cá»™t nÃ y lÆ°u `YEAR(GETDATE())` sau khi gá»­i thÃ nh cÃ´ng. Job chá»‰ gá»­i khi `BirthdayNotifiedYear IS NULL OR BirthdayNotifiedYear < YEAR(GETDATE())`.

---

## [2026-05-09] `v3.1 â€” Notification: IdempotencyKey trÃªn Deliveries`

### Thay Ä‘á»•i

| Loáº¡i          | Báº£ng                                   | Chi tiáº¿t                                                     |
| --------------- | ---------------------------------------- | -------------------------------------------------------------- |
| âž• ThÃªm cá»™t | `Notification.Deliveries.IdempotencyKey` | `VARCHAR(200) NULL` â€” key chá»‘ng gá»­i trÃ¹ng khi Job retry |
| âž• ThÃªm index | `UQ_Deliveries_IdempotencyKey`           | Unique filtered index (`WHERE IdempotencyKey IS NOT NULL`)     |

### LÃ½ do

Há»‡ thá»‘ng Outbox + background job cÃ³ thá»ƒ retry khi lá»—i transient. Cá»™t `IdempotencyKey` + unique index Ä‘áº£m báº£o má»—i sá»± kiá»‡n chá»‰ INSERT 1 delivery row duy nháº¥t dÃ¹ job cháº¡y láº¡i nhiá»u láº§n. Format key: `{EventType}:{AggregateId}:{AccountId}:{Channel}`.

### Script

`docs/database/changes/20260509_1400_AddIdempotencyKey_Deliveries.sql`

---

## [2026-05-08] `v2.1 â€” Promotion Schema v2: StartAt/EndAt + PromotionProductSlots`

### Thay Ä‘á»•i

| Loáº¡i                | Báº£ng                             | Chi tiáº¿t                                       |
| --------------------- | ---------------------------------- | ------------------------------------------------ |
| ðŸ—‘ï¸ XoÃ¡ cá»™t     | `PromotionTimeSlots.SlotDate`      | Gá»™p vÃ o StartAt/EndAt                         |
| ðŸ—‘ï¸ XoÃ¡ cá»™t     | `PromotionTimeSlots.StartTime`     | Gá»™p vÃ o StartAt                               |
| ðŸ—‘ï¸ XoÃ¡ cá»™t     | `PromotionTimeSlots.EndTime`       | Gá»™p vÃ o EndAt                                 |
| âž• ThÃªm cá»™t       | `PromotionTimeSlots.StartAt`       | `DATETIME2(0) NOT NULL` â€” UTC                  |
| âž• ThÃªm cá»™t       | `PromotionTimeSlots.EndAt`         | `DATETIME2(0) NOT NULL` â€” UTC                  |
| ðŸ”§ Sá»­a constraint | `CK_PromotionTimeSlots_Range`      | Thay `StartTime < EndTime` â†’ `StartAt < EndAt` |
| ðŸ”§ Sá»­a index      | `IX_PromotionTimeSlots_Active`     | DÃ¹ng `(Status, StartAt, EndAt)`                 |
| âž• ThÃªm index       | `IX_PromotionTimeSlots_Promotion`  | `(PromotionID, Status)`                          |
| ðŸ”§ Sá»­a unique     | `UQ_PromotionTimeSlots_UniqueSlot` | DÃ¹ng `(PromotionID, StartAt, EndAt)`            |
| ðŸ”§ Sá»­a default    | `PromotionTimeSlots.CreatedAt`     | Äá»•i sang `GETUTCDATE()`                        |
| âž• ThÃªm báº£ng      | `PromotionProductSlots`            | LiÃªn káº¿t sáº£n pháº©m vá»›i slot Flash Sale   |

### Báº£ng PromotionProductSlots â€” cá»™t má»›i

| Cá»™t              | Kiá»ƒu              | MÃ´ táº£                               |
| ------------------ | ------------------- | -------------------------------------- |
| `SlotProductID`    | `INT IDENTITY PK`   |                                        |
| `TimeSlotID`       | `INT FK`            | â†’ PromotionTimeSlots                 |
| `ProductID`        | `INT FK`            | â†’ Products                           |
| `SalePrice`        | `DECIMAL(12,2)`     | GiÃ¡ flash-sale cá»§a slot nÃ y        |
| `DiscountPercent`  | `DECIMAL(5,2) NULL` | % giáº£m giÃ¡                          |
| `SaleQuantity`     | `INT NOT NULL`      | Sá»‘ lÆ°á»£ng tá»‘i Ä‘a (báº¯t buá»™c) |
| `SoldQuantity`     | `INT DEFAULT 0`     | ÄÃ£ bÃ¡n                               |
| `ReservedQuantity` | `INT DEFAULT 0`     | Äang giá»¯                             |
| `IsActive`         | `BIT DEFAULT 1`     |                                        |

### LÃ½ do

> Flash Sale cáº§n gÃ¡n sáº£n pháº©m + giÃ¡ + sá»‘ lÆ°á»£ng riÃªng cho tá»«ng time slot.
> `ProductPromotions` chá»‰ giá»¯ nguyÃªn Ä‘á»ƒ dÃ¹ng cho loáº¡i DISCOUNT (khÃ´ng phÃ¢n slot).

### CÃ¡ch Ã¡p dá»¥ng

```bash
# Cháº¡y file SQL change script trÃªn SSMS hoáº·c sqlcmd:
docs/database/changes/20260508_1200_PromotionTimeSlots_StartAt_EndAt.sql
```

---

## [2026-04-13] `v1.0 â€” InitialCreate`

### Thay Ä‘á»•i

| Loáº¡i           | Báº£ng                                                    | Chi tiáº¿t                                                                       |
| ---------------- | --------------------------------------------------------- | -------------------------------------------------------------------------------- |
| âž• Schema       | `Notification`, `Interaction`, `Recommendation`, `System` | Táº¡o 4 schema riÃªng biá»‡t                                                     |
| âž• ThÃªm báº£ng | `System.DomainEventOutbox`                                | Outbox pattern cho Event-Driven                                                  |
| âž• ThÃªm báº£ng | `System.BackgroundJobs`                                   | Quáº£n lÃ½ background jobs / cron                                                |
| âž• ThÃªm báº£ng | `Roles`                                                   | Vai trÃ² ngÆ°á»i dÃ¹ng (Customer, Staff, Admin, ...)                             |
| âž• ThÃªm báº£ng | `Accounts`                                                | TÃ i khoáº£n ngÆ°á»i dÃ¹ng â€” Email unique, soft delete                         |
| âž• ThÃªm báº£ng | `BlockReasons`                                            | Danh sÃ¡ch lÃ½ do khÃ³a tÃ i khoáº£n                                             |
| âž• ThÃªm báº£ng | `UserBlockHistory`                                        | Lá»‹ch sá»­ khÃ³a/má»Ÿ khÃ³a tÃ i khoáº£n                                        |
| âž• ThÃªm báº£ng | `Addresses`                                               | Äá»‹a chá»‰ giao hÃ ng cá»§a user                                                |
| âž• ThÃªm báº£ng | `SuperCategories`                                         | Danh má»¥c lá»›n (Level 1)                                                       |
| âž• ThÃªm báº£ng | `Categories`                                              | Danh má»¥c sáº£n pháº©m (Level 2, FKâ†’SuperCategories)                          |
| âž• ThÃªm báº£ng | `Materials`                                               | Cháº¥t liá»‡u Ä‘á»“ chÆ¡i                                                        |
| âž• ThÃªm báº£ng | `Ages`                                                    | Äá»™ tuá»•i phÃ¹ há»£p                                                           |
| âž• ThÃªm báº£ng | `Sexes`                                                   | Giá»›i tÃ­nh phÃ¹ há»£p                                                          |
| âž• ThÃªm báº£ng | `Origins`                                                 | Xuáº¥t xá»© sáº£n pháº©m                                                         |
| âž• ThÃªm báº£ng | `Brands`                                                  | ThÆ°Æ¡ng hiá»‡u                                                                  |
| âž• ThÃªm báº£ng | `PriceRanges`                                             | Khoáº£ng giÃ¡ Ä‘á»ƒ lá»c                                                         |
| âž• ThÃªm báº£ng | `Promotions`                                              | ChÆ°Æ¡ng trÃ¬nh khuyáº¿n mÃ£i theo %                                             |
| âž• ThÃªm báº£ng | `Products`                                                | Sáº£n pháº©m Ä‘á»“ chÆ¡i â€” vá»›i FKâ†’Categories/Brands/PriceRanges/Promotions |
| âž• ThÃªm báº£ng | `ProductDetails`                                          | 1-1 vá»›i Products â€” mÃ´ táº£, váº­t liá»‡u, tuá»•i, giá»›i tÃ­nh, xuáº¥t xá»© |
| âž• ThÃªm báº£ng | `ProductImages`                                           | áº¢nh sáº£n pháº©m (1 áº£nh chÃ­nh unique per product)                           |
| âž• ThÃªm báº£ng | `StatusOrders`                                            | Báº£ng tráº¡ng thÃ¡i Ä‘Æ¡n hÃ ng (lookup table)                                  |
| âž• ThÃªm báº£ng | `Orders`                                                  | ÄÆ¡n hÃ ng â€” vá»›i computed TotalAmount (trigger)                              |
| âž• ThÃªm báº£ng | `OrderDetails`                                            | Chi tiáº¿t Ä‘Æ¡n â€” LineTotal lÃ  COMPUTED PERSISTED                            |
| âž• ThÃªm báº£ng | `OrderStatusHistory`                                      | Audit log thay Ä‘á»•i tráº¡ng thÃ¡i Ä‘Æ¡n                                        |
| âž• ThÃªm báº£ng | `Cart`                                                    | Giá» hÃ ng â€” 1 giá» per user                                                   |
| âž• ThÃªm báº£ng | `CartItems`                                               | Sáº£n pháº©m trong giá» â€” soft remove via RemovedAt                            |
| âž• ThÃªm báº£ng | `Wishlists`                                               | Sáº£n pháº©m yÃªu thÃ­ch                                                         |
| âž• ThÃªm báº£ng | `VoucherTypes`                                            | Loáº¡i voucher                                                                   |
| âž• ThÃªm báº£ng | `Vouchers`                                                | MÃ£ giáº£m giÃ¡ â€” cÃ³ MaxUsagePerUser, sá»‘ lÆ°á»£ng                           |
| âž• ThÃªm báº£ng | `OrderVouchers`                                           | Composite PK â€” voucher Ã¡p dá»¥ng cho Ä‘Æ¡n                                    |
| âž• ThÃªm báº£ng | `VoucherUsageLogs`                                        | Lá»‹ch sá»­ sá»­ dá»¥ng voucher                                                  |
| âž• ThÃªm báº£ng | `BlogCategories`                                          | Danh má»¥c bÃ i viáº¿t blog                                                      |
| âž• ThÃªm báº£ng | `BlogPosts`                                               | BÃ i viáº¿t blog â€” cÃ³ workflow duyá»‡t                                        |
| âž• ThÃªm báº£ng | `BlogPostCategories`                                      | N-N: bÃ i viáº¿t â†” danh má»¥c blog                                             |
| âž• ThÃªm báº£ng | `Banners`                                                 | Banner quáº£ng cÃ¡o theo vá»‹ trÃ­                                               |
| âž• ThÃªm báº£ng | `ReviewProducts`                                          | ÄÃ¡nh giÃ¡ sáº£n pháº©m (rating 1-5, pháº£i cÃ³ Ä‘Æ¡n hÃ ng)                     |
| âž• ThÃªm báº£ng | `ReviewProductImages`                                     | áº¢nh kÃ¨m Ä‘Ã¡nh giÃ¡                                                           |
| âž• ThÃªm báº£ng | `ReviewProductReplies`                                    | Pháº£n há»“i Ä‘Ã¡nh giÃ¡                                                         |
| âž• ThÃªm báº£ng | `ReviewProductReactions`                                  | Like/Dislike Ä‘Ã¡nh giÃ¡                                                         |
| âž• ThÃªm báº£ng | `Wallets`                                                 | VÃ­ Ä‘iá»‡n tá»­ â€” 1 vÃ­ per user                                              |
| âž• ThÃªm báº£ng | `WalletTransactions`                                      | Giao dá»‹ch vÃ­ â€” double-entry (CR/DR)                                         |
| âž• ThÃªm báº£ng | `PaymentHistory`                                          | Lá»‹ch sá»­ thanh toÃ¡n                                                          |
| âž• ThÃªm báº£ng | `OrderRefunds`                                            | YÃªu cáº§u hoÃ n tiá»n                                                           |
| âž• ThÃªm báº£ng | `Notification.Templates`                                  | Template thÃ´ng bÃ¡o                                                             |
| âž• ThÃªm báº£ng | `Notification.Deliveries`                                 | ThÃ´ng bÃ¡o Ä‘Ã£ gá»­i tá»›i user                                                |
| âž• ThÃªm báº£ng | `ChatConversations`                                       | Há»™i thoáº¡i chatbot (há»— trá»£ guest)                                         |
| âž• ThÃªm báº£ng | `ChatMessages`                                            | Tin nháº¯n trong há»™i thoáº¡i                                                   |
| âž• ThÃªm báº£ng | `Interaction.Events`                                      | Log hÃ nh vi user (click, view, ...)                                             |
| âž• ThÃªm báº£ng | `Recommendation.ItemSimilarities`                         | Äá»™ tÆ°Æ¡ng Ä‘á»“ng sáº£n pháº©m cho AI                                         |
| âž• Triggers     | Nhiá»u báº£ng                                             | 9 triggers (xem erd.md má»¥c Triggers)                                           |
| âž• Indexes      | Nhiá»u báº£ng                                             | 25+ indexes tá»‘i Æ°u performance                                                |
| âž• Constraints  | Nhiá»u báº£ng                                             | CHECK constraints cho business rules                                             |

### LÃ½ do

> Khá»Ÿi táº¡o toÃ n bá»™ schema ban Ä‘áº§u cho há»‡ thá»‘ng ToyStore E-Commerce (SEP490).
> Bao gá»“m: quáº£n lÃ½ tÃ i khoáº£n & phÃ¢n quyá»n, catalog sáº£n pháº©m Ä‘á»“ chÆ¡i, quáº£n lÃ½ Ä‘Æ¡n hÃ ng,
> giá» hÃ ng, voucher, blog, Ä‘Ã¡nh giÃ¡, thanh toÃ¡n + vÃ­ Ä‘iá»‡n tá»­, chatbot, AI recommendation.

### Báº£ng bá»‹ áº£nh hÆ°á»Ÿng

Táº¥t cáº£ báº£ng (schema khá»Ÿi táº¡o láº§n Ä‘áº§u).

### CÃ¡ch Ã¡p dá»¥ng

```sql
-- Cháº¡y file SQL trá»±c tiáº¿p trÃªn SQL Server Management Studio (SSMS)
-- Hoáº·c dÃ¹ng sqlcmd:
sqlcmd -S localhost -U sa -P "YourPassword" -i docs/database/schema.sql
```

---

<!-- TEMPLATE â€” copy vÃ  Ä‘iá»n khi thÃªm thay Ä‘á»•i má»›i

## [YYYY-MM-DD] `MÃ´ táº£ ngáº¯n thay Ä‘á»•i`

### Thay Ä‘á»•i

| Loáº¡i | Báº£ng | Chi tiáº¿t |
|---|---|---|
| âž• ThÃªm báº£ng | `` |  |
| âž• ThÃªm cá»™t | `.` | kiá»ƒu dá»¯ liá»‡u â€” nullable/not null |
| ðŸ”§ Sá»­a cá»™t | `.` | Thay Ä‘á»•i gÃ¬ |
| âž• ThÃªm index | `` | Má»¥c Ä‘Ã­ch |
| âž• ThÃªm FK | `â†’` | ON DELETE behavior |
| ðŸ—‘ï¸ XoÃ¡ cá»™t | `.` | LÃ½ do xoÃ¡ |

### LÃ½ do

> MÃ´ táº£ feature yÃªu cáº§u thay Ä‘á»•i nÃ y.

### Báº£ng bá»‹ áº£nh hÆ°á»Ÿng

``, ``

### CÃ¡ch Ã¡p dá»¥ng

```sql
-- SQL script thÃªm vÃ o schema.sql vÃ  cháº¡y láº¡i, HOáº¶C cháº¡y ALTER TABLE riÃªng:
ALTER TABLE [Báº£ng] ADD [Cá»™tMá»›i] kiá»ƒu_dá»¯_liá»‡u NULL;
```

-->
