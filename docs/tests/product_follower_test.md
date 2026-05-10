# Hướng dẫn kiểm tra chức năng Theo dõi sản phẩm (Product Tracking)

## 1. Thông tin chung
- **Base URL:** `http://localhost:5000/api/product-followers`
- **Yêu cầu xác thực:** BẮT BUỘC (JWT Token của Customer)

## 2. Các API Endpoint

### 2.1 Đăng ký theo dõi sản phẩm
- **URL:** `POST /follow/{productId}`
- **Tham số:** `productId` (int, bắt buộc)
- **Mô tả:** Đăng ký nhận thông báo khi sản phẩm có hàng (ComingSoon -> Active hoặc Out of stock -> In stock).

**Request Sample:**
`POST http://localhost:5000/api/product-followers/follow/10`

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Successfully followed the product.",
  "data": null
}
```

### 2.2 Hủy theo dõi sản phẩm
- **URL:** `DELETE /unfollow/{productId}`
- **Tham số:** `productId` (int, bắt buộc)

**Request Sample:**
`DELETE http://localhost:5000/api/product-followers/unfollow/10`

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Successfully unfollowed the product.",
  "data": null
}
```

### 2.3 Kiểm tra trạng thái theo dõi
- **URL:** `GET /is-following/{productId}`

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Checked follow status successfully.",
  "data": true
}
```

### 2.4 Lấy danh sách sản phẩm đang theo dõi
- **URL:** `GET /my-follows`

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Followed product IDs loaded successfully.",
  "data": [10, 15, 22]
}
```

## 3. Các trường hợp lỗi cần test
1. **401 Unauthorized:** Gọi API mà không có Token hoặc Token hết hạn.
2. **404 Not Found:** Theo dõi một `productId` không tồn tại hoặc đã bị xóa.
3. **400 Bad Request:** Truyền `productId` không phải là số nguyên.

## 4. Kiểm tra dữ liệu trong Database
Chạy câu lệnh SQL sau để kiểm tra bản ghi đã được tạo/xóa chưa:
```sql
SELECT * FROM ProductFollowers WHERE AccountId = [Id_Cua_Ban]
```

## 5. Kiểm tra logic thông báo (Regression)
1. Đăng ký theo dõi một sản phẩm có trạng thái `ComingSoon`.
2. Dùng Admin cập nhật sản phẩm đó sang trạng thái `Active` và số lượng `> 0`.
3. Đợi `BackInStockJob` chạy (mặc định 30p) hoặc trigger tay (nếu có tool).
4. Kiểm tra chuông thông báo ở Frontend Customer có hiện tin nhắn báo sản phẩm đã có hàng không.
