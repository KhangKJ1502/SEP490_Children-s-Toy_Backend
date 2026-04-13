# Hướng dẫn đồng bộ DB khi làm nhóm (DB First)

> **Nguyên tắc:** DB là source of truth. Mỗi thay đổi schema PHẢI có file SQL đi kèm.

---

## Khi BẠN thay đổi DB (thêm bảng/cột/index)

### Làm đúng thứ tự này — KHÔNG BỎ BƯỚC NÀO:

```
1. Sửa DB trên SSMS local của bạn
        ↓
2. Viết file SQL change script (BẮT BUỘC)
        ↓
3. Chạy re-scaffold để cập nhật C# models
        ↓
4. Cập nhật docs/database/CHANGELOG.md + erd.md
        ↓
5. git commit TẤT CẢ + git push
        ↓
6. Thông báo nhóm chat
```

---

### Bước 2: Tạo file SQL change script

```
📁 docs/database/changes/
   ├── _TEMPLATE.sql              ← Copy file này
   ├── 20260413_1200_InitialCreate.sql
   ├── 20260415_0930_AddWeightToProducts.sql   ← ví dụ
   └── 20260416_1400_AddReviewTable.sql        ← ví dụ
```

**Quy tắc đặt tên:** `YYYYMMDD_HHMM_MoTaNganGon.sql`
- `20260415` = ngày 15/04/2026
- `0930` = giờ 09:30
- `AddWeightToProducts` = mô tả rõ thay đổi gì

**Nội dung file:** Chỉ chứa lệnh ALTER/CREATE cần thiết, KHÔNG phải toàn bộ schema.

Ví dụ `20260415_0930_AddWeightToProducts.sql`:
```sql
USE [SEP409_ToyStore];
GO

-- Thêm cột Weight vào bảng Products (yêu cầu tính phí ship)
ALTER TABLE [Products] ADD [Weight] DECIMAL(10,2) NULL;
GO

ALTER TABLE [Products] ADD [Dimensions] NVARCHAR(50) NULL;
GO

CREATE NONCLUSTERED INDEX [IX_Products_Weight]
ON [Products]([Weight])
WHERE [Weight] IS NOT NULL;
GO
```

---

### Bước 3: Re-scaffold

```powershell
dotnet ef dbcontext scaffold "Server=DESKTOP-T27O90D\SQLEXPRESS;Database=SEP409_ToyStore;User ID=sa;Password=khangmc1502@;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer --project ToyStore.Infrastructure --startup-project ToyStore.API --output-dir Models --context-dir Data --context SEP490ToyStoreContext --no-onconfiguring --force
```

---

### Bước 5: git commit (đúng format)

```bash
git add docs/database/changes/20260415_0930_AddWeightToProducts.sql
git add ToyStore.Infrastructure/Models/Product.cs
git add docs/database/CHANGELOG.md
git add docs/database/erd.md
git commit -m "chore(db): AddWeightToProducts — thêm cột Weight, Dimensions cho tính phí ship"
git push
```

---

### Bước 6: Thông báo nhóm chat

> **Template tin nhắn:**
> *"Mình vừa push thay đổi DB: thêm cột `Weight`, `Dimensions` vào bảng `Products`.
> Các bạn nhớ pull về và chạy file `docs/database/changes/20260415_0930_AddWeightToProducts.sql` trên SSMS nhé!"*

---

## Khi BẠN nhận thông báo có DB thay đổi (git pull)

### Làm đúng thứ tự:

```bash
# 1. Pull code mới nhất
git pull

# 2. Kiểm tra file SQL mới trong changes/
# (xem file nào có ngày > lần pull trước của bạn)

# 3. Chạy TẤT CẢ file SQL mới theo THỨ TỰ NGÀY trên SSMS local
# Ví dụ: chạy 20260415_0930_... trước, rồi 20260416_1400_... sau

# 4. KHÔNG cần chạy lại scaffold (người push đã làm rồi, C# models đã update trong git)

# 5. Build để đảm bảo không lỗi
dotnet build ToyStore.sln
```

---

## Dấu hiệu bạn CHƯA đồng bộ DB

| Lỗi runtime | Nguyên nhân | Cách fix |
|---|---|---|
| `Invalid column name 'Weight'` | Chưa chạy SQL change script | Chạy file SQL mới nhất trong `changes/` |
| `Cannot find table 'Reviews'` | Bảng mới chưa được tạo | Chạy SQL script thêm bảng |
| Build pass nhưng crash khi chạy | DB chưa đồng bộ | Kiểm tra changes/ có file SQL mới không |
| Property trong C# không khớp DB | Chạy sai thứ tự SQL scripts | Chạy lại tất cả theo thứ tự ngày |

---

## Quy tắc VÀNG

> 1. **Không bao giờ** sửa DB mà không viết SQL change script
> 2. **Không bao giờ** commit code C# (models) mà không có file SQL đi kèm
> 3. **Không bao giờ** sửa DB của người khác — mỗi người sửa DB local của mình
> 4. **Thứ tự SQL scripts = thứ tự thời gian** — không chạy đảo lộn
> 5. **Báo nhóm ngay** khi push thay đổi DB — không để người khác tự đoán

---

## Khi có conflict DB (2 người sửa cùng lúc)

Ví dụ: Bạn A thêm cột `Weight` vào `Products`, Bạn B cũng thêm cột `Material` vào `Products` cùng lúc.

**Cách xử lý:**
1. Người push sau phải kiểm tra xem file SQL của người push trước có conflict không
2. Nếu cùng bảng: chạy SQL của người push trước TRƯỚC từ SSMS
3. Sau đó chạy SQL của mình
4. Re-scaffold lại (vì cả 2 thay đổi đều cần reflect trong C#)
5. Commit với message rõ ràng

**Phòng tránh:**
- Assign rõ người phụ trách bảng nào trong sprint
- Thông báo nhóm chat khi bắt đầu sửa DB để tránh đụng nhau
