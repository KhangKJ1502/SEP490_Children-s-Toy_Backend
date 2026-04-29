# Test chức năng Promotion

Tài liệu hướng dẫn kiểm tra API quản lý Promotion.

**Base URL:** `http://localhost:5000/api/promotions` (Thay đổi cổng nếu cấu hình của bạn khác)

---

## 1. Lấy danh sách Promotion (View list / Search)

**Endpoint:** `GET /api/promotions`

**Query Parameters:**
- `pageNumber` (int, default 1)
- `pageSize` (int, default 10)
- `sortBy` (string, optional): name, startdate, enddate, priority
- `sortDesc` (bool, default false)
- `searchTerm` (string, optional)
- `status` (string, optional)

**Happy Path (200 OK):**
```bash
curl -X GET "http://localhost:5000/api/promotions?pageNumber=1&pageSize=10"
```

---

## 2. Tạo Promotion (Add)

**Endpoint:** `POST /api/promotions`

**Header:** `Content-Type: application/json`

**Happy Path (201 Created):**
```json
{
  "promotionName": "Mừng Giáng Sinh 2026",
  "promotionType": "Discount",
  "description": "Giảm giá mạnh toàn bộ sản phẩm dịp Giáng Sinh",
  "startDate": "2026-12-01T00:00:00Z",
  "endDate": "2026-12-25T23:59:59Z",
  "status": "Active",
  "priority": 1
}
```

**Validation Error (400 Bad Request):**
*Gửi `startDate` lớn hơn `endDate`, hoặc `promotionName` rỗng:*
```json
{
  "promotionName": "",
  "promotionType": "Discount",
  "startDate": "2026-12-26T00:00:00Z",
  "endDate": "2026-12-25T23:59:59Z",
  "status": "Active",
  "priority": 1
}
```
*Kết quả mong đợi:* Nhận được thông báo lỗi cho Name và EndDate.

**Business Rule (409 Conflict):**
*Tạo Promotion với `promotionName` đã tồn tại.*

---

## 3. Cập nhật Promotion (Edit)

**Endpoint:** `PUT /api/promotions/{id}`

**Header:** `Content-Type: application/json`

**Happy Path (200 OK):**
```json
{
  "status": "Inactive",
  "priority": 2
}
```

**Not Found (404 Not Found):**
*Cập nhật một ID không tồn tại trên hệ thống.*

---

## 4. Lấy chi tiết Promotion (Get By Id)

**Endpoint:** `GET /api/promotions/{id}`

**Happy Path (200 OK):**
```bash
curl -X GET "http://localhost:5000/api/promotions/1"
```

**Not Found (404 Not Found):**
*ID không tồn tại trên hệ thống.*

---

## Các bước Test Regression (Dành cho AI Test)

1. Test tạo thành công Promotion mới.
2. Test tạo với dữ liệu không hợp lệ (Validation fails).
3. Test tạo Promotion bị trùng tên.
4. Test lấy danh sách và tìm kiếm theo tên vừa tạo.
5. Test lấy chi tiết Promotion bằng ID.
6. Test cập nhật Promotion thành công.
7. Test cập nhật Promotion với ID không tồn tại.
