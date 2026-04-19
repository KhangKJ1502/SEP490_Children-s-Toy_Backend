# HƯỚNG DẪN TEST CHỨC NĂNG — Template

> **Cách dùng:** Khi muốn AI test một chức năng, hãy tag file này kèm tên chức năng:
> `@feature_test.md Test chức năng [TÊN CHỨC NĂNG]`
>
> AI sẽ tự động thực hiện toàn bộ quy trình test bên dưới.

---

## QUY TRÌNH TEST BẮT BUỘC

Khi được yêu cầu test, AI PHẢI thực hiện TỪNG BƯỚC sau:

### Bước 1: Khởi động Server

```
cd d:\SEP490_BackEnd
dotnet run --project ToyStore.API
```

- Chờ server sẵn sàng (log hiện "Now listening on...")
- Ghi nhận Base URL (mặc định: `https://localhost:7xxx` hoặc `http://localhost:5xxx`)

### Bước 2: Kiểm tra Swagger

- Mở trình duyệt tới `{BaseURL}/swagger`
- Xác nhận endpoint của chức năng cần test đã xuất hiện trên Swagger

### Bước 3: Test API — Các case BẮT BUỘC

Với MỖI endpoint của chức năng, test ĐẦY ĐỦ các case sau:

#### ✅ Case thành công (Happy Path)

| Test        | Mô tả                    | Expected               |
| ----------- | ------------------------ | ---------------------- |
| GET all     | Lấy danh sách            | 200 OK + data array    |
| GET by ID   | Lấy theo ID hợp lệ       | 200 OK + data object   |
| POST create | Tạo mới với data hợp lệ  | 201 Created / 200 OK   |
| PUT update  | Cập nhật với data hợp lệ | 200 OK                 |
| DELETE      | Xoá (soft delete)        | 200 OK / 204 NoContent |

#### ❌ Case lỗi Validation (400 Bad Request)

| Test                      | Mô tả                             | Expected                |
| ------------------------- | --------------------------------- | ----------------------- |
| POST thiếu field bắt buộc | Gửi body thiếu Name, Price, ...   | 400 + validation errors |
| POST giá trị sai          | Giá âm, SalePrice > Price, ...    | 400 + validation errors |
| POST format sai           | Email sai, SĐT sai, SKU sai, ...  | 400 + validation errors |
| POST cross-field sai      | MinAge > MaxAge, Start > End, ... | 400 + validation errors |

#### 🔍 Case lỗi Not Found (404)

| Test                    | Mô tả                  | Expected      |
| ----------------------- | ---------------------- | ------------- |
| GET ID không tồn tại    | Dùng random GUID       | 404 Not Found |
| PUT ID không tồn tại    | Update với ID không có | 404 Not Found |
| DELETE ID không tồn tại | Xoá ID không có        | 404 Not Found |

#### ⚠️ Case lỗi Business Logic

| Test               | Mô tả                         | Expected           |
| ------------------ | ----------------------------- | ------------------ |
| POST duplicate     | Tạo trùng SKU / Email / ...   | 409 Conflict       |
| DELETE đã xoá      | Xoá lại record đã soft delete | 400 Business Error |
| Thao tác cần quyền | Không có token (nếu có auth)  | 401 Unauthorized   |

#### 🔐 Case lỗi Authorization

| Test           | Mô tả                                                                                       | Expected         |
| -------------- | ------------------------------------------------------------------------------------------- | ---------------- |
| Không có token | Gọi endpoint cần auth mà không gửi Bearer token                                             | 401 Unauthorized |
| Token hết hạn  | Gửi token cũ đã expire                                                                      | 401 Unauthorized |
| Sai role       | Dùng token role thấp gọi endpoint cần role cao hơn (ví dụ: Customer gọi endpoint của Admin) | 403 Forbidden    |

#### 🔄 Case Transaction Rollback (chỉ áp dụng cho chức năng có Transaction)

| Test                                  | Mô tả                                                       | Expected                                    |
| ------------------------------------- | ----------------------------------------------------------- | ------------------------------------------- |
| Một item trong danh sách không hợp lệ | Tạo Order có 3 item, item thứ 2 hết hàng                    | 400 + không có record nào được tạo trong DB |
| Lỗi giữa chừng                        | Dữ liệu đúng nhưng giả lập lỗi (sai FK, duplicate...)       | 500 + toàn bộ transaction rollback          |
| Verify DB sau rollback                | Sau khi fail, kiểm tra DB xem có record nào bị tạo dở không | DB sạch, không có record nào                |

### Bước 4: Test Regression — Các chức năng LIÊN QUAN (BẮT BUỘC)

> ⚠️ **PHẢI test các chức năng liên quan ĐÃ ĐƯỢC CODE để đảm bảo KHÔNG ẢNH HƯỞNG feature khác.**
>
> ⚠️ **Nếu feature liên quan CHƯA ĐƯỢC CODE → BỎ QUA, không cần test.**
> Đó là nhiệm vụ của người code feature đó — khi họ code xong, họ BẮT BUỘC phải test lại các feature đã tồn tại.

AI phải:

1. Kiểm tra xem feature liên quan **đã có Controller/Service trong codebase chưa**
2. Nếu **đã có** → test lại Happy Path của feature đó
3. Nếu **chưa có** → ghi nhận "Chưa code — sẽ test khi feature này được implement"

**Cách xác định feature liên quan:**

- Feature có **Foreign Key** liên kết (ví dụ: OrderItem → Product, Order → User)
- Feature **dùng chung Service/Repository** (ví dụ: đổi Product → ảnh hưởng Cart, Order)
- Feature có **logic phụ thuộc** (ví dụ: xoá Category → ảnh hưởng Product thuộc Category đó)

**Ví dụ:**

| Đang test | Test thêm (NẾU ĐÃ CODE)           |
| --------- | --------------------------------- |
| Product   | Cart, OrderItem, Review, Category |
| Order     | Product (tồn kho), Payment, User  |
| Category  | Product (liên kết CategoryId)     |
| User      | Order, Review                     |
| Coupon    | Order (áp mã giảm giá)            |
| Payment   | Order (đổi trạng thái)            |

**Quy trình test regression:**

1. Xác định danh sách feature liên quan
2. Kiểm tra feature đó **đã tồn tại** trong code chưa (có Controller + Service không)
3. Nếu đã có → test lại các case Happy Path (GET all, GET by ID)
4. Nếu chưa có → bỏ qua, ghi chú trong báo cáo
5. Ghi nhận kết quả vào bảng báo cáo

### Bước 5: Ghi nhận kết quả

Sau khi test xong, AI PHẢI báo cáo theo format sau trong chat:

```
## KẾT QUẢ TEST: [TÊN CHỨC NĂNG]

### Tổng kết:
- ✅ Passed: X/Y cases
- ❌ Failed: X/Y cases

### Chi tiết — Feature chính:

| # | Case | Endpoint | Status | Kết quả | Ghi chú |
|---|------|----------|--------|---------|---------|
| 1 | GET all | GET /api/xxx | 200 | ✅ PASS | |
| 2 | POST thiếu Name | POST /api/xxx | 400 | ✅ PASS | Trả đúng lỗi |
| 3 | ... | ... | ... | ❌ FAIL | Mô tả lỗi |

### Chi tiết — Regression (Feature liên quan):

| # | Feature | Endpoint | Status | Kết quả | Ghi chú |
|---|---------|----------|--------|---------|---------|
| 1 | Cart | GET /api/carts | 200 | ✅ PASS | Data vẫn đúng |
| 2 | Order | GET /api/orders | 200 | ✅ PASS | Không bị ảnh hưởng |

### Lỗi cần fix (nếu có):
1. Mô tả lỗi + đề xuất fix
```

---

### Bước 5b: Lưu báo cáo ra file

Sau khi hoàn thành báo cáo ở Bước 5, AI **BẮT BUỘC** tạo file báo cáo theo quy tắc:

- **Thư mục:** `docs/test-reports/`
- **Tên file:** `[feature]_report_[yyyy-MM-dd].md`
  - Ví dụ: `product_report_2025-01-15.md`, `order_report_2025-01-15.md`
- **Tạo thư mục nếu chưa có:** `mkdir -p docs/test-reports`

**Nội dung file báo cáo BẮT BUỘC có đủ các mục sau:**

```markdown
# BÁO CÁO TEST: [TÊN CHỨC NĂNG]

- **Ngày test:** yyyy-MM-dd HH:mm
- **Người test:** AI (Claude Code)
- **Branch/Commit:** [ghi nhận git branch + commit hash hiện tại]
- **Base URL:** https://localhost:xxxx

---

## Tổng kết

| Hạng mục      | Passed | Failed | Skipped | Tổng  |
| ------------- | ------ | ------ | ------- | ----- |
| Feature chính | X      | X      | X       | X     |
| Regression    | X      | X      | X       | X     |
| **Tổng cộng** | **X**  | **X**  | **X**   | **X** |

> Kết luận: ✅ PASS toàn bộ / ❌ CÓ LỖI — cần fix trước khi merge

---

## Chi tiết — Feature chính

| #   | Case            | Endpoint      | Expected | Actual | Kết quả | Ghi chú                   |
| --- | --------------- | ------------- | -------- | ------ | ------- | ------------------------- |
| 1   | GET all         | GET /api/xxx  | 200      | 200    | ✅ PASS |                           |
| 2   | POST thiếu Name | POST /api/xxx | 400      | 400    | ✅ PASS | Trả đúng validation error |
| 3   | ...             | ...           | ...      | ...    | ❌ FAIL | Mô tả lỗi cụ thể          |

---

## Chi tiết — Regression

| #   | Feature | Endpoint        | Expected | Actual | Kết quả | Ghi chú        |
| --- | ------- | --------------- | -------- | ------ | ------- | -------------- |
| 1   | Cart    | GET /api/carts  | 200      | 200    | ✅ PASS |                |
| 2   | Order   | GET /api/orders | 200      | 200    | ✅ PASS |                |
| 3   | Payment | —               | —        | —      | ⏭ SKIP | Chưa được code |

---

## Lỗi cần fix

### Lỗi 1: [Tên lỗi ngắn gọn]

- **Endpoint:** POST /api/xxx
- **Case:** Mô tả case bị fail
- **Expected:** Kết quả mong đợi
- **Actual:** Kết quả thực tế
- **Đề xuất fix:** Mô tả cách fix
- **Trạng thái:** ⏳ Chưa fix / ✅ Đã fix

---

## Response mẫu (lưu để tham khảo)

### POST /api/xxx — 201 Created

\`\`\`json
{ "// AI paste response body thực tế ở đây" }
\`\`\`

### POST /api/xxx — 400 Validation Error

\`\`\`json
{ "// AI paste response body thực tế ở đây" }
\`\`\`
```

**Sau khi tạo file xong**, AI thông báo trong chat:

```
✅ Đã lưu báo cáo: docs/test-reports/[tên-file].md
```

---

### Bước 6: Fix lỗi (nếu có)

- Nếu có case FAIL → AI tự đề xuất fix
- Hỏi user có muốn fix không
- Fix xong → test lại case đó + test lại regression

---

## MẪU REQUEST JSON (AI tự điền theo chức năng)

### POST — Tạo mới (data hợp lệ)

```json
{
    "// AI điền mẫu JSON hợp lệ cho chức năng đang test"
}
```

### POST — Tạo mới (data thiếu field)

```json
{
    "// AI điền mẫu JSON thiếu field bắt buộc"
}
```

### POST — Tạo mới (data sai logic)

```json
{
    "// AI điền mẫu JSON vi phạm business logic"
}
```

### PUT — Cập nhật (data hợp lệ)

```json
{
    "// AI điền mẫu JSON update hợp lệ"
}
```

---

## LƯU Ý QUAN TRỌNG

- **PHẢI test thực tế** — gọi API thật, không giả lập
- **PHẢI ghi nhận response body** — cho mỗi case
- **PHẢI test ĐẦY ĐỦ validation** — không bỏ sót field nào
- **PHẢI test cross-field logic** — SalePrice < Price, MinAge < MaxAge, ...
- **PHẢI kiểm tra database** — sau khi create/update/delete, verify data trong DB
- **Nếu có Transaction** — test case rollback (tạo order với product hết hàng, ...)
- **PHẢI test regression** — test lại các feature liên quan để đảm bảo không ảnh hưởng
- **Nếu regression FAIL** — đây là lỗi nghiêm trọng, PHẢI fix trước khi báo cáo PASS
- **PHẢI lưu file báo cáo** — sau mỗi lần test, bắt buộc tạo file trong `docs/test-reports/`
