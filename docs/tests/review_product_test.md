# Test chức năng Manage Review Product

## Base URL
- **Public/Customer**: `{{baseUrl}}/api/reviews`
- **Admin/Staff**: `{{baseUrl}}/api/admin/reviews`

---

## I. Phía Customer (Guest / Customer)

### 1. [Customer] Create Review
**Endpoint:** `POST {{baseUrl}}/api/reviews`
**Auth:** Bearer Token (Role: Customer)
**Headers:** `Content-Type: multipart/form-data`

**Form Data:**
- `OrderId`: 1 (int)
- `ProductId`: 1 (int)
- `Rating`: 5 (byte, 1-5)
- `Comment`: "Sản phẩm tuyệt vời!" (string)
- `Images`: [File 1], [File 2] (IFormFile, max 3 files)

**Cases cần test:**
- ✅ **Thành công (201):** Order Completed <= 20 ngày, chưa review. Response trả về review data kèm ảnh (nếu có). ModerationStatus sẽ là "Approved" do auto-approve flow.
- ❌ **Lỗi 400 (Validation):** Rating ngoài khoảng 1-5, comment quá dài, quá 3 ảnh, ảnh sai format/dung lượng.
- ❌ **Lỗi 400 (Business - Not Completed):** Order chưa ở trạng thái Completed.
- ❌ **Lỗi 400 (Business - Expired):** Order Completed quá 20 ngày.
- ❌ **Lỗi 409 (Conflict):** Đã review sản phẩm này trong order này.

### 2. [Customer] Update Review
**Endpoint:** `PUT {{baseUrl}}/api/reviews/{reviewId}`
**Auth:** Bearer Token (Role: Customer)
**Headers:** `Content-Type: multipart/form-data`

**Form Data:** (Tất cả optional)
- `Rating`: 4 (byte)
- `Comment`: "Cập nhật đánh giá" (string)
- `Images`: [New File] (IFormFile)

**Cases cần test:**
- ✅ **Thành công (200):** Sửa thành công. `IsEdited` chuyển thành true. Ảnh cũ bị xoá mềm.
- ❌ **Lỗi 400 (Business - Already Edited):** Review đã có `IsEdited = true`.
- ❌ **Lỗi 400 (Business - Expired):** Quá 3 ngày kể từ lúc Approved.
- ❌ **Lỗi 404 (Not Found):** Không tồn tại hoặc review của người khác.

### 3. [Customer] Delete Review
**Endpoint:** `DELETE {{baseUrl}}/api/reviews/{reviewId}`
**Auth:** Bearer Token (Role: Customer)

**Cases cần test:**
- ✅ **Thành công (204):** Xoá mềm review và các ảnh kèm theo.
- ❌ **Lỗi 404:** Không tìm thấy hoặc review của user khác.

### 4. [Guest] Get Reviews by Product
**Endpoint:** `GET {{baseUrl}}/api/reviews?productId=1&pageNumber=1&pageSize=10`
**Auth:** AllowAnonymous

**Cases cần test:**
- ✅ **Thành công (200):** Chỉ trả về review có `ModerationStatus = "Approved"` và `!IsDeleted`. Trả về tên người dùng (ẩn email), danh sách ảnh và reply.
- ✅ **Filter/Sort:** Test filter `rating=5`, `hasImage=true`, sort by `rating` desc.

---

## II. Phía Admin/Staff

### 1. [Staff] Get Admin Reviews List
**Endpoint:** `GET {{baseUrl}}/api/admin/reviews?pageNumber=1&pageSize=10`
**Auth:** Bearer Token (Role: Staff/Merchandise/Admin)

**Cases cần test:**
- ✅ **Thành công (200):** Trả về toàn bộ review kể cả Pending/Rejected, IsDeleted. Bao gồm ImagesCount, RepliesCount.
- ✅ **Filter:** Filter theo `moderationStatus=Pending`, `productId`, `accountId`, `searchTerm`.

### 2. [Staff] Get Review Detail
**Endpoint:** `GET {{baseUrl}}/api/admin/reviews/{reviewId}`
**Auth:** Bearer Token (Role: Staff/Merchandise/Admin)

**Cases cần test:**
- ✅ **Thành công (200):** Trả về đầy đủ thông tin, ảnh, reply và lịch sử moderation log (ReviewModerationLogs).

### 3. [Staff] Update Moderation Status
**Endpoint:** `PUT {{baseUrl}}/api/admin/reviews/{reviewId}/status`
**Auth:** Bearer Token (Role: Staff/Merchandise/Admin)

**Body (JSON):**
```json
{
  "moderationStatus": "Rejected",
  "reason": "Chứa nội dung không phù hợp"
}
```

**Cases cần test:**
- ✅ **Thành công (200):** Cập nhật status cho review VÀ tự động cập nhật status cho các ảnh (cascade). Ghi 1 log cho review và từng log cho các ảnh.
- ❌ **Lỗi 400 (Validation):** Status sai giá trị cho phép (phải là Approved/Rejected/ManualReview).

### 4. [Staff] Create Reply
**Endpoint:** `POST {{baseUrl}}/api/admin/reviews/{reviewId}/reply`
**Auth:** Bearer Token (Role: Staff/Merchandise/Admin)

**Body (JSON):**
```json
{
  "content": "Cảm ơn bạn đã phản hồi!"
}
```

**Cases cần test:**
- ✅ **Thành công (200):** Đăng reply thành công. Trả về thông tin reply kèm StaffName.

### 5. [Staff] Update Reply
**Endpoint:** `PUT {{baseUrl}}/api/admin/reviews/{reviewId}/reply/{replyId}`
**Auth:** Bearer Token (Role: Staff/Merchandise/Admin)

**Body (JSON):**
```json
{
  "content": "Nội dung reply đã được sửa"
}
```

**Cases cần test:**
- ✅ **Thành công (200):** Sửa thành công (nếu là chủ sở hữu hoặc admin).
- ❌ **Lỗi 401 (Unauthorized):** Staff khác cố sửa reply.

### 6. [Staff] Delete Reply
**Endpoint:** `DELETE {{baseUrl}}/api/admin/reviews/{reviewId}/reply/{replyId}`
**Auth:** Bearer Token (Role: Staff/Merchandise/Admin)

**Cases cần test:**
- ✅ **Thành công (204):** Xoá mềm reply.
- ❌ **Lỗi 401 (Unauthorized):** Staff khác cố xoá.
