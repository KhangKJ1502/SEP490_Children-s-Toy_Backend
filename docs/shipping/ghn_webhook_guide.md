# Tài Liệu Hướng Dẫn Tích Hợp & Kiểm Thử GHN Webhook (SEP490_ToyStore)

Tài liệu này cung cấp toàn bộ sơ đồ trạng thái, danh sách ánh xạ mã lỗi, các lệnh **cURL** giả lập theo quy trình từ gốc và các câu lệnh truy vấn SQL kiểm thử hệ thống cho lập trình viên vận hành luồng Webhook của Giao Hàng Nhanh (GHN).

---

## 1. THÔNG TIN ENDPOINT & BẢO MẬT
*   **Địa chỉ API (Local):** `https://localhost:7083/api/webhooks/ghn` (Hoặc cổng HTTP `http://localhost:5216/api/webhooks/ghn`)
*   **Phương thức:** `POST`
*   **Bảo mật:** Xác thực Header `X-Webhook-Token` chống giả mạo.
*   **Header yêu cầu:**
    *   `X-Webhook-Token: fDoaiBJjrN3jc2Re4SIUZgZdNz5HzsPup6BRutbmXlh`
    *   `Content-Type: application/json`

---

## 2. BẢNG ÁNH XẠ TRẠNG THÁI VẬN CHUYỂN GHN (FULL FLOW)

Hệ thống tự động lắng nghe và chuyển đổi các trạng thái vận chuyển vật lý từ GHN thành trạng thái nghiệp vụ chuẩn của **StatusOrders** để hiển thị đồng bộ lên UI khách hàng và quản lý:

| Trạng thái thô GHN | Ý nghĩa nghiệp vụ | ID Hệ thống | Trạng thái hiển thị (UI) | Phân quyền CSKH / Kho |
| :--- | :--- | :---: | :--- | :--- |
| **`ready_to_pick`** | Đơn hàng vận chuyển vừa tạo | **`4`** | `Shipped` (Đã gửi vận chuyển) | Merchandise (Kho) |
| **`picking`** | Shipper đang đến lấy hàng | **`4`** | `Shipped` | Merchandise (Kho) |
| **`money_collect_picking`**| Shipper đang giao dịch với shop | **`4`** | `Shipped` | Merchandise (Kho) |
| **`picked`** | Shipper đã lấy hàng thành công | **`4`** | `Shipped` | Merchandise (Kho) |
| **`storing`** | Hàng đã nhập kho phân loại GHN | `Giữ nguyên` | `Shipped` *(Chỉ ghi log hành trình)* | Không đổi |
| **`transporting`** | Hàng đang luân chuyển giữa các kho | `Giữ nguyên` | `Shipped` *(Chỉ ghi log hành trình)* | Không đổi |
| **`sorting`** | Hàng đang phân loại tại kho GHN | `Giữ nguyên` | `Shipped` *(Chỉ ghi log hành trình)* | Không đổi |
| **`delivering`** | Shipper đang đi giao cho khách | **`5`** | `Delivering` (Đang giao hàng) | Merchandise (Kho) |
| **`money_collect_delivering`**| Shipper đang tương tác thu tiền khách | **`5`** | `Delivering` | Merchandise (Kho) |
| **`delivered`** | Giao hàng thành công | **`6`** | `Delivered` (Giao thành công) | Kết thúc (Giải phóng ca) |
| **`delivery_fail`** | Giao hàng thất bại | **`12`** | `DeliveryFailed` (Giao thất bại) | **Staff (CSKH liên hệ)** |
| **`waiting_to_return`** | Đơn treo chờ trả (Sau 3 lần lỗi) | **`13`** | `WaitingReturn` (Chờ trả hàng) | Merchandise (Kho) |
| **`return`** | Chờ chuyển hoàn về shop | **`10`** | `Returning` (Đang hoàn hàng) | Merchandise (Kho) |
| **`return_transporting`** | Hàng hoàn đang luân chuyển kho | `Giữ nguyên` | `Returning` *(Chỉ ghi log hành trình)* | Không đổi |
| **`return_sorting`** | Hàng hoàn đang phân loại kho | `Giữ nguyên` | `Returning` *(Chỉ ghi log hành trình)* | Không đổi |
| **`returning`** | Shipper đang mang trả hàng lại shop | **`10`** | `Returning` (Đang hoàn hàng) | Merchandise (Kho) |
| **`returned`** | Shop đã nhận lại hàng hoàn | **`11`** | `ReturnCompleted` (Đã hoàn hàng) | Kết thúc (Tự hoàn kho, voucher) |
| **`return_fail`** | Shop từ chối nhận lại do sự cố | **`14`** | `ReturnFailed` (Hoàn thất bại) | **Admin / Merchandise xử lý** |
| **`cancel`** | Hủy đơn hàng vận chuyển | **`8`** | `Cancelled` (Đã hủy) | Kết thúc (Tự động hoàn kho) |
| **`exception`** | Xử lý sự cố ngoại lệ phát sinh | **`12`** | `DeliveryFailed` (Giao thất bại) | Staff / Admin |
| **`damage`** | Hàng hóa hư hại hoàn toàn | **`16`** | `Damaged` (Hàng bị hỏng) | Tự động hủy đơn & hoàn tiền |
| **`lost`** | Hàng hóa bị thất lạc | **`15`** | `Lost` (Hàng bị mất) | Tự động hủy đơn & hoàn tiền |

---

## 3. DANH SÁCH MÃ LỖI GHN MỚI NHẤT (GHN-FAIL-CODES)

Khi trạng thái là `delivery_fail`, `storing` lỗi hay `return_fail`, hệ thống dịch tự động các mã lỗi sang mô tả tiếng Việt:

### A. Nhóm lỗi Lấy hàng thất bại
*   `GHN-PFA1A0`: Người gửi hẹn lại ngày lấy hàng
*   `GHN-PFA2A2`: Thông tin lấy hàng sai (địa chỉ / SĐT)
*   `GHN-PFA2A1`: Thuê bao người gửi không liên lạc được / Máy bận
*   `GHN-PFA2A3`: Người gửi không nghe máy
*   `GHN-PFA1A1`: Người gửi muốn gửi hàng tại bưu cục
*   `GHN-PCB0B2`: Hàng vi phạm quy định khối lượng, kích thước
*   `GHN-PFA4A1`: Hàng vi phạm quy cách đóng gói
*   `GHN-PCB0B1`: Người gửi không muốn gửi hàng nữa
*   `GHN-PFA4A2`: Hàng hóa GHN không vận chuyển
*   `GHN-PFA3A2`: Nhân viên lấy hàng gặp sự cố **(Lỗi nhà vận chuyển)**

### B. Nhóm lỗi Giao hàng thất bại
*   `GHN-DFC1A0`: Người nhận hẹn lại ngày giao *(Tự động lên lịch giao lại)*
*   `GHN-DFC1A2`: Không liên lạc được người nhận / Số điện thoại chặn shipper **(Cảnh báo đưa vào Blacklist)**
*   `GHN-DFC1A4`: Người nhận không nghe máy *(Cho phép giao lại tối đa 2 lần)*
*   `GHN-DCD0A1`: Sai thông tin người nhận (địa chỉ / SĐT) *(Cần Staff sửa thông tin)*
*   `GHN-DFC1A1`: Người nhận đổi địa chỉ giao hàng
*   `GHN-DFC1A7`: Người nhận từ chối nhận do không cho xem / thử hàng
*   `GHN-DCD0A6`: Người nhận từ chối nhận do sai sản phẩm **(Lỗi Shop - Hoàn trả cọc tự động)**
*   `GHN-DCD0A7`: Người nhận từ chối nhận do sai số tiền COD **(Lỗi Shop - Hoàn trả cọc tự động)**
*   `GHN-DCD0A5`: Người nhận từ chối nhận do hàng hóa hư hỏng **(Lỗi vận chuyển - Kích hoạt bồi thường)**
*   `GHN-DCD1A5`: Người nhận từ chối nhận do không có tiền **(Cảnh báo đưa vào Blacklist)**
*   `GHN-DCD0A8`: Người nhận đổi ý không mua nữa
*   `GHN-DCD1A1`: Người nhận báo không đặt hàng **(Cảnh báo đơn ảo / đơn bom - Blacklist ngay)**
*   `GHN-DFC1A6`: Nhân viên giao hàng gặp sự cố **(Lỗi nhà vận chuyển)**
*   `GHN-DCD1A3`: Hàng suy suyển, bể vỡ trong quá trình vận chuyển **(Lỗi vận chuyển - Bồi thường)**

### C. Nhóm lỗi Trả hàng thất bại
*   `GHN-RFE0A0`: Người gửi hẹn lại ngày trả hàng
*   `GHN-RFE0A1`: Người gửi đổi địa chỉ trả hàng
*   `GHN-RFE0A6`: Người gửi không nghe máy
*   `GHN-RFE0A3`: Người gửi từ chối nhận lại do sai sản phẩm
*   `GHN-RFE0A4`: Người gửi từ chối nhận lại do hàng hư hỏng **(Bồi thường hoàn trả)**
*   `GHN-RFE0A5`: Nhân viên trả hàng gặp sự cố

---

## 4. KỊCH BẢN CẢNH BÁO GIẢ LẬP WEBHOOK (cURL POWERSHELL)

Hãy chuẩn bị một đơn hàng kiểm thử trong DB bằng cách chạy script SQL ở **Mục 5**, thay thế mã vận đơn `"YOUR_ORDER_CODE"` trong các lệnh dưới đây bằng mã đơn hàng của bạn (Ví dụ: `LXCQKA`).

### KỊCH BẢN A: LUỒNG GIAO HÀNG THÀNH CÔNG (HAPPY PATH)

#### Bước A1: Tiếp nhận đơn và đóng gói (`ready_to_pick`)
```powershell
curl -X POST "https://localhost:7083/api/webhooks/ghn" `
-H "X-Webhook-Token: fDoaiBJjrN3jc2Re4SIUZgZdNz5HzsPup6BRutbmXlh" `
-H "Content-Type: application/json" `
-d '{
    "OrderCode": "LX6EYM", "Status": "ready_to_pick", "Type": "create", "Time": "2026-05-27T10:00:00.000Z",
    "CODAmount": 3000000, "PaymentType": 1, "TotalFee": 71400, "Weight": 200, "Length": 10, "Width": 10, "Height": 10, "ReasonCode": "", "Reason": ""
}'
```

#### Bước A2: Shipper bắt đầu đi giao (`delivering`)
```powershell
curl -X POST "https://localhost:7083/api/webhooks/ghn" `
-H "X-Webhook-Token: fDoaiBJjrN3jc2Re4SIUZgZdNz5HzsPup6BRutbmXlh" `
-H "Content-Type: application/json" `
-d '{
    "OrderCode": "LX6EYM", "Status": "delivering", "Type": "switch_status", "Time": "2026-05-27T12:00:00.000Z",
    "CODAmount": 3000000, "PaymentType": 1, "TotalFee": 71400, "Weight": 200, "Length": 10, "Width": 10, "Height": 10, "ReasonCode": "", "Reason": ""
}'
```

#### Bước A3: Giao hàng thành công (`delivered`)
```powershell
curl -X POST "https://localhost:7083/api/webhooks/ghn" `
-H "X-Webhook-Token: fDoaiBJjrN3jc2Re4SIUZgZdNz5HzsPup6BRutbmXlh" `
-H "Content-Type: application/json" `
-d '{
    "OrderCode": "LX6EYM", "Status": "delivered", "Type": "switch_status", "Time": "2026-05-27T14:30:00.000Z",
    "CODAmount": 3000000, "PaymentType": 1, "TotalFee": 71400, "Weight": 200, "Length": 10, "Width": 10, "Height": 10, "ReasonCode": "", "Reason": ""
}'
```

---

### KỊCH BẢN B: LUỒNG KHÁCH BƠM HÀNG - GIAO THẤT BẠI 3 LẦN & CHUYỂN HOÀN

#### Bước B1: Giao hàng thất bại lần 1 - Khách không nghe máy (`delivery_fail`)
*(Tăng `DeliveryFailCount` = 1, cập nhật lý do bằng tiếng Việt)*
```powershell
curl -X POST "https://localhost:7083/api/webhooks/ghn" `
-H "X-Webhook-Token: fDoaiBJjrN3jc2Re4SIUZgZdNz5HzsPup6BRutbmXlh" `
-H "Content-Type: application/json" `
-d '{
    "OrderCode": "LXNLCE", "Status": "delivery_fail", "Type": "update", "Time": "2026-05-27T15:00:00.000Z",
    "CODAmount": 3000000, "PaymentType": 1, "TotalFee": 71400, "Weight": 200, "Length": 10, "Width": 10, "Height": 10,
    "ReasonCode": "GHN-DFC1A4", "Reason": "Người nhận không nghe máy"
}'
```

#### Bước B2: Giao hàng thất bại lần 2 - Khách chặn số điện thoại (`delivery_fail`)
*(Tăng `DeliveryFailCount` = 2, kích hoạt cảnh báo phân tích hành vi Blacklist)*
```powershell
curl -X POST "https://localhost:7083/api/webhooks/ghn" `
-H "X-Webhook-Token: fDoaiBJjrN3jc2Re4SIUZgZdNz5HzsPup6BRutbmXlh" `
-H "Content-Type: application/json" `
-d '{
    "OrderCode": "LXNLCE", "Status": "delivery_fail", "Type": "update", "Time": "2026-05-27T16:00:00.000Z",
    "CODAmount": 3000000, "PaymentType": 1, "TotalFee": 71400, "Weight": 200, "Length": 10, "Width": 10, "Height": 10,
    "ReasonCode": "GHN-DFC1A2", "Reason": "Không liên lạc được người nhận / Chặn số"
}'
```

#### Bước B3: Giao thất bại lần 3 - Khách báo không hề mua hàng (`delivery_fail`)
*(Đạt ngưỡng 3 lần $\rightarrow$ Hệ thống tự động kích hoạt chuyển hoàn đơn `WaitingReturn`, tự động hoàn lại số tồn kho sản phẩm, hủy tiền COD và phục hồi Voucher giảm giá)*
```powershell
curl -X POST "https://localhost:7083/api/webhooks/ghn" `
-H "X-Webhook-Token: fDoaiBJjrN3jc2Re4SIUZgZdNz5HzsPup6BRutbmXlh" `
-H "Content-Type: application/json" `
-d '{
    "OrderCode": "LXNLCE", "Status": "delivery_fail", "Type": "update", "Time": "2026-05-27T17:00:00.000Z",
    "CODAmount": 3000000, "PaymentType": 1, "TotalFee": 71400, "Weight": 200, "Length": 10, "Width": 10, "Height": 10,
    "ReasonCode": "GHN-DCD1A1", "Reason": "Người nhận báo không đặt hàng"
}'
```

#### Bước B4: Hàng đang được Shipper cầm chuyển hoàn về kho (`returning`)
```powershell
curl -X POST "https://localhost:7083/api/webhooks/ghn" `
-H "X-Webhook-Token: fDoaiBJjrN3jc2Re4SIUZgZdNz5HzsPup6BRutbmXlh" `
-H "Content-Type: application/json" `
-d '{
    "OrderCode": "LXNLCE", "Status": "return", "Type": "switch_status", "Time": "2026-05-27T18:00:00.000Z",
    "CODAmount": 3000000, "PaymentType": 1, "TotalFee": 71400, "Weight": 200, "Length": 10, "Width": 10, "Height": 10, "ReasonCode": "", "Reason": ""
}'
```

#### Bước B5: Bưu cục mang trả hàng hoàn về kho thành công (`returned`)
*(Đơn hàng chính thức khép lại trạng thái `ReturnCompleted` (StatusID = 11), giải phóng toàn bộ ca trực nhân viên)*
```powershell
curl -X POST "https://localhost:7083/api/webhooks/ghn" `
-H "X-Webhook-Token: fDoaiBJjrN3jc2Re4SIUZgZdNz5HzsPup6BRutbmXlh" `
-H "Content-Type: application/json" `
-d '{
    "OrderCode": "LXNLCE", "Status": "returned", "Type": "switch_status", "Time": "2026-05-27T19:00:00.000Z",
    "CODAmount": 3000000, "PaymentType": 1, "TotalFee": 71400, "Weight": 200, "Length": 10, "Width": 10, "Height": 10, "ReasonCode": "", "Reason": ""
}'
```

---

### KỊCH BẢN C: HÀNG BỊ SỰ CỐ MẤT MÁT TRONG VẬN CHUYỂN (`lost`)
Hệ thống tự động hủy đơn, hoàn tiền mặt tự động nếu đơn đã trả trước và sinh cảnh báo yêu cầu đòi đền bù bảo hiểm từ phía GHN.

```powershell
curl -X POST "https://localhost:7083/api/webhooks/ghn" `
-H "X-Webhook-Token: fDoaiBJjrN3jc2Re4SIUZgZdNz5HzsPup6BRutbmXlh" `
-H "Content-Type: application/json" `
-d '{
    "OrderCode": "LX6EYM", "Status": "lost", "Type": "switch_status", "Time": "2026-05-27T20:00:00.000Z",
    "CODAmount": 3000000, "PaymentType": 1, "TotalFee": 71400, "Weight": 200, "Length": 10, "Width": 10, "Height": 10, "ReasonCode": "", "Reason": ""
}'
```

---

## 5. CÂU LỆNH SQL HỖ TRỢ KIỂM THỬ TRÊN CƠ SỞ DỮ LIỆU

### A. Khởi tạo dữ liệu sạch để bắt đầu kiểm thử mới:
```sql
-- 1. Tạo đơn hàng đồ chơi thử nghiệm
INSERT INTO Orders (OrderCode, AccountID, StatusID, TotalAmount, PaymentMethod, PaymentStatus, DeliveryFailCount, CreatedAt, UpdatedAt, IsDeleted)
VALUES ('ORD_TEST_GHN_002', 1, 3, 3000000, 'SHIP_COD', 'PENDING', 0, GETDATE(), GETDATE(), 0);

DECLARE @OrderID INT = SCOPE_IDENTITY();

-- 2. Thêm chi tiết đơn hàng
INSERT INTO OrderDetails (OrderID, ProductID, ProductName, Quantity, UnitPrice)
VALUES (@OrderID, 1, N'Robot LEGO Chiến Binh Cao Cấp', 1, 3000000);

-- 3. Tạo mã giao vận liên kết với GHN ( LX6EYM )
INSERT INTO ShippingProviderTransactions (OrderID, Provider, ProviderOrderCode, Status, CreatedAt, UpdatedAt)
VALUES (@OrderID, 'GHN', 'LX6EYM', 'ready_to_pick', GETDATE(), GETDATE());
```

### B. Kiểm tra kết quả cập nhật nghiệp vụ sau khi gọi cURL:
```sql
-- Xem thông tin trạng thái đơn hàng và các cột thống kê lỗi vận chuyển
SELECT 
    o.OrderCode, 
    so.StatusName AS DisplayStatus,
    o.PaymentStatus,
    o.DeliveryFailCount,
    o.LastGHNFailCode,
    o.FailedDeliveryAt,
    o.ReturnedAt,
    o.CancelReason,
    tx.Status AS GhnStatus,
    tx.ShippingFee AS RecordedFee,
    tx.CodAmount AS RecordedCOD
FROM Orders o
INNER JOIN StatusOrders so ON o.StatusID = so.StatusID
INNER JOIN ShippingProviderTransactions tx ON o.OrderID = tx.OrderID
WHERE tx.ProviderOrderCode = 'LX6EYM';

-- Truy vấn xem nhật ký lịch sử cuộc gọi webhook GHN (Tránh trùng lặp Idempotency)
SELECT * FROM ShippingStatusHistories 
WHERE OrderID = (SELECT OrderID FROM Orders WHERE OrderCode = 'ORD_TEST_GHN_002')
ORDER BY ProcessedAt DESC;
```
