# Tài Liệu Hướng Dẫn Tích Hợp & Kiểm Thử GHN Webhook (SEP490_ToyStore)

Tài liệu này cung cấp sơ đồ trạng thái, mã lỗi GHN, **thứ tự test theo vòng đời đơn thật** (từ checkout → GHN → hoàn tiền ví), và các lệnh **cURL** giả lập webhook.

**Mã vận đơn mẫu trong doc:** `LXDQRT` (thay bằng `ShippingOrderCode` / `ProviderOrderCode` của đơn bạn đang test).

**Tài liệu liên quan:**

| File                                               | Nội dung                           |
| -------------------------------------------------- | ---------------------------------- |
| `docs/testing/ghn_return_flow_webhook_tests.md`    | TC chi tiết return / prepaid / COD |
| `PLAN_TEST/Backend/prepaid_exception_playbook.md`  | P1–P7 hoàn tiền ví & ngoại lệ      |
| `PLAN_TEST/Backend/known_quirks_and_test_notes.md` | ORD-G\*, AS-IS / GAP               |

**Biến PowerShell (dùng cho mọi cURL bên dưới):**

```powershell
$GhnCode = "LXDQRT"   # ProviderOrderCode trên ShippingProviderTransactions
$BaseUrl = "https://localhost:7083/api/webhooks/ghn"   # hoặc http://localhost:5216/...
$Token   = "fDoaiBJjrN3jc2Re4SIUZgZdNz5HzsPup6BRutbmXlh"   # appsettings Webhooks
```

> **Lưu ý triển khai (2026-05):** Webhook tuần tự (`OrderWebhookTransitionValidator`). **Mỗi đơn PAID chỉ được hoàn ví 1 lần** (key `REFUND_{orderCode}`). Auto ví khi cancel **chỉ trước Shipped**; Shipped+ → refund quản lý (Approve → Complete). System refund: không Reject.

---

## 0. THỨ TỰ TEST THEO VÒNG ĐỜI ĐƠN HÀNG THẬT

Test **một đơn end-to-end** trước; sau đó mới tách nhánh B/C/D/E/F bằng SQL reset hoặc đơn mới.

### Giai đoạn 1 — Trước GHN (UI + API, không webhook)

| #   | Ai làm      | Việc cần làm                           | Kỳ vọng                                                                                           |
| --- | ----------- | -------------------------------------- | ------------------------------------------------------------------------------------------------- |
| 1   | Khách       | Thêm giỏ → Checkout → chọn địa chỉ     | Đơn tạo, `Status=Pending`                                                                         |
| 2a  | Khách       | **SE_PAY:** quét QR, chờ webhook SePay | `PaymentStatus=PAID`, auto Confirm (nếu cấu hình)                                                 |
| 2b  | Khách       | **WALLET:** thanh toán ví              | `PAID`                                                                                            |
| 2c  | Khách       | **SHIP_COD:** đặt đơn                  | `PaymentStatus=COD_PENDING` hoặc `PENDING`                                                        |
| 3   | Staff/Merch | Confirm → Processing (nếu chưa auto)   | `Confirmed` → `Processing`                                                                        |
| 4   | Merch/Admin | **Ship** (tạo đơn GHN)                 | `Shipped`, `Orders.ShippingOrderCode` = mã GHN (vd. `LXDQRT`), row `ShippingProviderTransactions` |

**SQL — lấy đơn sẵn sàng bắt webhook:**

```sql
SELECT o.OrderID, o.OrderCode, o.StatusID, so.StatusName, o.PaymentMethod, o.PaymentStatus,
       o.ShippingOrderCode, t.ProviderOrderCode, t.Status AS GhnLastStatus
FROM Orders o
JOIN StatusOrders so ON so.StatusID = o.StatusID
LEFT JOIN ShippingProviderTransactions t ON t.OrderID = o.OrderID AND t.Provider = 'GHN'
WHERE t.ProviderOrderCode = 'LXDQRT';   -- hoặc o.ShippingOrderCode = 'LXDQRT'
```

### Giai đoạn 2 — Webhook GHN: nhánh A (giao thành công)

Gọi **đúng thứ tự thời gian** (mục 4 — Kịch bản A: A1 → A6). Sau mỗi bước chạy SQL mục 5B.

| Bước webhook                  | Trạng thái đơn (internal) | Tab khách (bucket) |
| ----------------------------- | ------------------------- | ------------------ |
| A1 `ready_to_pick`            | Shipped (4)               | shipping           |
| A2–A3 `picking` / `picked`    | Shipped (log)             | shipping           |
| A4 `delivering`               | Delivering (5)            | delivering         |
| A5 `money_collect_delivering` | Delivering (log)          | delivering         |
| A6 `delivered`                | Delivered (6)             | completed          |

| #   | Ai làm           | Việc                          | Kỳ vọng                                                               |
| --- | ---------------- | ----------------------------- | --------------------------------------------------------------------- |
| 5   | Khách            | Nút “Đã nhận hàng”            | `Completed` (7)                                                       |
| 6   | Khách (tuỳ chọn) | Yêu cầu hoàn trả trong 3 ngày | `RefundRequested` — luồng pickup admin (không phải system refund GHN) |

### Giai đoạn 2 — Webhook GHN: nhánh B (giao fail → hoàn về kho → hoàn tiền prepaid)

**Chuẩn bị:** đơn prepaid `PAID`, đang `Delivering` (5), `ProviderOrderCode=LXDQRT`.

| Bước  | Webhook                              | Trạng thái đơn                                             | Ghi chú                                                                |
| ----- | ------------------------------------ | ---------------------------------------------------------- | ---------------------------------------------------------------------- |
| B1–B3 | `delivery_fail` (có thể gọi 1–3 lần) | **DeliveryFailed (12)**                                    | `DeliveryFailCount` tăng; **không** tự nhảy WaitingReturn khi đủ 3 lần |
| B3b   | `waiting_to_return`                  | **WaitingReturn (13)**                                     | GHN chủ động báo chờ lấy hoàn — **bước bắt buộc** trước `return`       |
| B4    | `return`                             | COD → **Cancelled (8)**; Prepaid → **Returning (10)**      | COD: hủy + restore stock ngay tại `return`                             |
| B5    | `returning`                          | Returning (10)                                             | Chỉ log / giữ Returning                                                |
| B6    | `returned`                           | Prepaid → **Cancelled (8)** + `PaymentStatus` vẫn **PAID** | System `OrderRefunds` **RefundRequested**                              |
| B7    | Admin                                | Refund: **Approve → Complete**                             | `PaymentStatus=REFUNDED`, credit ví `REFUND_{refundCode}`              |
| —     | Khách                                | Mở ví / tạo PIN (nếu chưa có)                              | Số dư ≥ số tiền hoàn                                                   |

**Không test nhánh B trên cùng đơn đã chạy xong nhánh A** (đã Delivered/Completed).

### Giai đoạn 2 — Nhánh C / D / E / F (đơn riêng, reset SQL)

| Nhánh | Webhook chính                 | Prepaid kỳ vọng                                                     |
| ----- | ----------------------------- | ------------------------------------------------------------------- |
| **C** | `return_fail` (sau Returning) | `ReturnFailed (14)`, cần admin xử lý thủ công                       |
| **D** | `lost`                        | `Lost (15)` hoặc Cancelled + system refund; **không** restore stock |
| **E** | `damage`                      | `Damaged (16)` tương tự lost                                        |
| **F** | `cancel` (GHN hủy VC)         | `Cancelled (8)`; COD restore stock; prepaid có thể system refund    |

### Giai đoạn 3 — Hủy đơn từ app (song song với GHN)

| #   | Ai          | Điều kiện                          | Kỳ vọng                                                        |
| --- | ----------- | ---------------------------------- | -------------------------------------------------------------- |
| H1  | Khách       | Pending/Confirmed, chưa delivering | Cancelled; prepaid PAID → ví `REFUND_{orderCode}`              |
| H2  | Khách/Admin | Đã có `ShippingOrderCode`          | Gọi `IGhnClient.CancelOrderAsync` (best-effort) rồi hủy DB     |
| H3  | System      | SE_PAY chưa PAID ~30 phút          | `SePayExpiryJob` → EXPIRED + Cancelled (không dính worker 24h) |

### Checklist một vòng “đủ luồng” (gợi ý 4–5 đơn)

1. **Đơn 1 — Happy path:** Giai đoạn 1 → A1–A6 → khách Complete.
2. **Đơn 2 — Prepaid return:** Giai đoạn 1 (SE_PAY PAID) → A1–A4 → B1→B3b→B4→B5→B6 → Admin refund Complete → kiểm ví.
3. **Đơn 3 — COD return:** `SHIP_COD` → A1–A4 → B1…→B4 (`return`) → Cancelled, stock restore.
4. **Đơn 4 — Lost hoặc Damage:** Giai đoạn 1 prepaid → A1–A4 → một webhook `lost` **hoặc** `damage` → system refund → Admin Complete.
5. **Đơn 5 — Hủy sớm:** Prepaid PAID, Shipped → khách hủy → REFUNDED ví + GHN cancel log.

Chạy **ToyStore.API** + **ToyStore.Worker** khi test notification/outbox.

---

## 1. THÔNG TIN ENDPOINT & BẢO MẬT

- **Địa chỉ API (Local):** `https://localhost:7083/api/webhooks/ghn` (Hoặc cổng HTTP `http://localhost:5216/api/webhooks/ghn`)
- **Phương thức:** `POST`
- **Bảo mật:** Xác thực Header `X-Webhook-Token` chống giả mạo.
- **Header yêu cầu:**
  - `X-Webhook-Token: fDoaiBJjrN3jc2Re4SIUZgZdNz5HzsPup6BRutbmXlh`
  - `Content-Type: application/json`

---

## 2. BẢNG ÁNH XẠ TRẠNG THÁI VẬN CHUYỂN GHN (FULL FLOW)

Hệ thống tự động lắng nghe và chuyển đổi các trạng thái vận chuyển vật lý từ GHN thành trạng thái nghiệp vụ chuẩn của **StatusOrders** để hiển thị đồng bộ lên UI khách hàng và quản lý:

| Trạng thái thô GHN             | Ý nghĩa nghiệp vụ                     |                               ID Hệ thống                               | Trạng thái hiển thị (UI)                                        | Phân quyền CSKH / Kho                                         |
| :----------------------------- | :------------------------------------ | :---------------------------------------------------------------------: | :-------------------------------------------------------------- | :------------------------------------------------------------ |
| **`ready_to_pick`**            | Đơn hàng vận chuyển vừa tạo           |                                 **`4`**                                 | `Shipped` (Đã gửi vận chuyển)                                   | Merchandise (Kho)                                             |
| **`picking`**                  | Shipper đang đến lấy hàng             |                                 **`4`**                                 | `Shipped`                                                       | Merchandise (Kho)                                             |
| **`money_collect_picking`**    | Shipper đang giao dịch với shop       |                                 **`4`**                                 | `Shipped`                                                       | Merchandise (Kho)                                             |
| **`picked`**                   | Shipper đã lấy hàng thành công        |                                 **`4`**                                 | `Shipped`                                                       | Merchandise (Kho)                                             |
| **`storing`**                  | Hàng đã nhập kho phân loại GHN        |                              `Giữ nguyên`                               | `Shipped` _(Chỉ ghi log hành trình)_                            | Không đổi                                                     |
| **`transporting`**             | Hàng đang luân chuyển giữa các kho    |                              `Giữ nguyên`                               | `Shipped` _(Chỉ ghi log hành trình)_                            | Không đổi                                                     |
| **`sorting`**                  | Hàng đang phân loại tại kho GHN       |                              `Giữ nguyên`                               | `Shipped` _(Chỉ ghi log hành trình)_                            | Không đổi                                                     |
| **`delivering`**               | Shipper đang đi giao cho khách        |                                 **`5`**                                 | `Delivering` (Đang giao hàng)                                   | Merchandise (Kho)                                             |
| **`money_collect_delivering`** | Shipper đang tương tác thu tiền khách |                                 **`5`**                                 | `Delivering`                                                    | Merchandise (Kho)                                             |
| **`delivered`**                | Giao hàng thành công                  |                                 **`6`**                                 | `Delivered` (Giao thành công)                                   | Kết thúc (Giải phóng ca)                                      |
| **`delivery_fail`**            | Giao hàng thất bại                    |                                **`12`**                                 | `DeliveryFailed` (Giao thất bại)                                | **Staff (CSKH liên hệ)**                                      |
| **`waiting_to_return`**        | Đơn treo chờ trả (Sau 3 lần lỗi)      |                                **`13`**                                 | `WaitingReturn` (Chờ trả hàng)                                  | Merchandise (Kho)                                             |
| **`return`**                   | Chờ chuyển hoàn về shop               |                                **`10`**                                 | `Returning` (Đang hoàn hàng)                                    | Merchandise (Kho)                                             |
| **`return_transporting`**      | Hàng hoàn đang luân chuyển kho        |                              `Giữ nguyên`                               | `Returning` _(Chỉ ghi log hành trình)_                          | Không đổi                                                     |
| **`return_sorting`**           | Hàng hoàn đang phân loại kho          |                              `Giữ nguyên`                               | `Returning` _(Chỉ ghi log hành trình)_                          | Không đổi                                                     |
| **`returning`**                | Shipper đang mang trả hàng lại shop   |                                **`10`**                                 | `Returning` (Đang hoàn hàng)                                    | Merchandise (Kho)                                             |
| **`returned`**                 | Shop đã nhận lại hàng hoàn            | **COD:** `Cancelled` (8) / **Prepaid:** `Cancelled` (8) + system refund | Customer: _Refund processing_ / Admin: Cancelled + refund queue | Prepaid: **không** restore stock; Admin Approve→Complete → ví |
| **`return_fail`**              | Shop từ chối nhận lại do sự cố        |                                **`14`**                                 | `ReturnFailed` (Hoàn thất bại)                                  | **Admin / Merchandise xử lý**                                 |
| **`cancel`**                   | Hủy đơn hàng vận chuyển               |                                 **`8`**                                 | `Cancelled` (Đã hủy)                                            | Kết thúc (Tự động hoàn kho)                                   |
| **`exception`**                | Xử lý sự cố ngoại lệ phát sinh        |                                **`12`**                                 | `DeliveryFailed` (Giao thất bại)                                | Staff / Admin                                                 |
| **`damage`**                   | Hàng hóa hư hại hoàn toàn             |                                **`16`**                                 | `Damaged` / _Refund processing_                                 | Prepaid: system refund → ví; **không** restore stock          |
| **`lost`**                     | Hàng hóa bị thất lạc                  |                                **`15`**                                 | `Lost` / _Refund processing_                                    | Giống `damage`                                                |

**Nhãn UI khách (API `statusName` / `displayLabel`):** map qua `CustomerOrderDisplayStatusMapper` — vd. _Returning to warehouse_, _Returned to warehouse_, _Refund processing_, _Delivering_. Tab lọc dùng `statusBucket`: `pending` | `shipping` | `delivering` | `completed` | `cancelled` | `refunded`.

**Webhook tuần tự (fulfillment):** `OrderWebhookTransitionValidator` — `delivered` chỉ từ `Delivering` hoặc `Shipped`; `delivering` từ `Shipped`/`Processing`. Gửi sai thứ tự → status đơn **không đổi** (vẫn ghi `ShippingStatusHistories`).

---

## 3. DANH SÁCH MÃ LỖI GHN MỚI NHẤT (GHN-FAIL-CODES)

Khi trạng thái là `delivery_fail`, `storing` lỗi hay `return_fail`, hệ thống dịch tự động các mã lỗi sang mô tả tiếng Việt:

### A. Nhóm lỗi Lấy hàng thất bại

- `GHN-PFA1A0`: Người gửi hẹn lại ngày lấy hàng
- `GHN-PFA2A2`: Thông tin lấy hàng sai (địa chỉ / SĐT)
- `GHN-PFA2A1`: Thuê bao người gửi không liên lạc được / Máy bận
- `GHN-PFA2A3`: Người gửi không nghe máy
- `GHN-PFA1A1`: Người gửi muốn gửi hàng tại bưu cục
- `GHN-PCB0B2`: Hàng vi phạm quy định khối lượng, kích thước
- `GHN-PFA4A1`: Hàng vi phạm quy cách đóng gói
- `GHN-PCB0B1`: Người gửi không muốn gửi hàng nữa
- `GHN-PFA4A2`: Hàng hóa GHN không vận chuyển
- `GHN-PFA3A2`: Nhân viên lấy hàng gặp sự cố **(Lỗi nhà vận chuyển)**

### B. Nhóm lỗi Giao hàng thất bại

- `GHN-DFC1A0`: Người nhận hẹn lại ngày giao _(Tự động lên lịch giao lại)_
- `GHN-DFC1A2`: Không liên lạc được người nhận / Số điện thoại chặn shipper **(Cảnh báo đưa vào Blacklist)**
- `GHN-DFC1A4`: Người nhận không nghe máy _(Cho phép giao lại tối đa 2 lần)_
- `GHN-DCD0A1`: Sai thông tin người nhận (địa chỉ / SĐT) _(Cần Staff sửa thông tin)_
- `GHN-DFC1A1`: Người nhận đổi địa chỉ giao hàng
- `GHN-DFC1A7`: Người nhận từ chối nhận do không cho xem / thử hàng
- `GHN-DCD0A6`: Người nhận từ chối nhận do sai sản phẩm **(Lỗi Shop - Hoàn trả cọc tự động)**
- `GHN-DCD0A7`: Người nhận từ chối nhận do sai số tiền COD **(Lỗi Shop - Hoàn trả cọc tự động)**
- `GHN-DCD0A5`: Người nhận từ chối nhận do hàng hóa hư hỏng **(Lỗi vận chuyển - Kích hoạt bồi thường)**
- `GHN-DCD1A5`: Người nhận từ chối nhận do không có tiền **(Cảnh báo đưa vào Blacklist)**
- `GHN-DCD0A8`: Người nhận đổi ý không mua nữa
- `GHN-DCD1A1`: Người nhận báo không đặt hàng **(Cảnh báo đơn ảo / đơn bom - Blacklist ngay)**
- `GHN-DFC1A6`: Nhân viên giao hàng gặp sự cố **(Lỗi nhà vận chuyển)**
- `GHN-DCD1A3`: Hàng suy suyển, bể vỡ trong quá trình vận chuyển **(Lỗi vận chuyển - Bồi thường)**

### C. Nhóm lỗi Trả hàng thất bại

- `GHN-RFE0A0`: Người gửi hẹn lại ngày trả hàng
- `GHN-RFE0A1`: Người gửi đổi địa chỉ trả hàng
- `GHN-RFE0A6`: Người gửi không nghe máy
- `GHN-RFE0A3`: Người gửi từ chối nhận lại do sai sản phẩm
- `GHN-RFE0A4`: Người gửi từ chối nhận lại do hàng hư hỏng **(Bồi thường hoàn trả)**
- `GHN-RFE0A5`: Nhân viên trả hàng gặp sự cố

---

## 4. KỊCH BẢN GIẢ LẬP WEBHOOK GHN CHI TIẾT (`OrderCode` = `LXDQRT`)

Chuẩn bị đơn thật đã **Ship** (có `ProviderOrderCode`) **hoặc** chạy SQL mục 5 — thay `"LXDQRT"` trong JSON bằng `$GhnCode`.

---

### KỊCH BẢN A: LUỒNG GIAO HÀNG THÀNH CÔNG RỰC RỠ (HAPPY PATH)

#### Bước A1: Tiếp nhận đơn và đóng gói (`ready_to_pick`)

_Trạng thái hệ thống chuyển: Shipped (4)_

```powershell
curl -X POST "https://localhost:7083/api/webhooks/ghn" `
-H "X-Webhook-Token: fDoaiBJjrN3jc2Re4SIUZgZdNz5HzsPup6BRutbmXlh" `
-H "Content-Type: application/json" `
-d '{
    "OrderCode": "LXDQRT", "Status": "ready_to_pick", "Type": "create", "Time": "2026-05-28T10:00:00.000Z",
    "CODAmount": 3000000, "PaymentType": 1, "TotalFee": 71400, "Weight": 200, "Length": 10, "Width": 10, "Height": 10, "ReasonCode": "", "Reason": ""
}'
```

#### Bước A2: Shipper đang đến lấy hàng (`picking`)

_Ghi nhận nhật ký lịch sử. Trạng thái giữ nguyên: Shipped (4)_

```powershell
curl -X POST "https://localhost:7083/api/webhooks/ghn" `
-H "X-Webhook-Token: fDoaiBJjrN3jc2Re4SIUZgZdNz5HzsPup6BRutbmXlh" `
-H "Content-Type: application/json" `
-d '{
    "OrderCode": "LXDQRT", "Status": "picking", "Type": "switch_status", "Time": "2026-05-28T10:30:00.000Z",
    "CODAmount": 3000000, "PaymentType": 1, "TotalFee": 71400, "Weight": 200, "Length": 10, "Width": 10, "Height": 10, "ReasonCode": "", "Reason": ""
}'
```

#### Bước A3: Shipper đã lấy hàng thành công khỏi Shop (`picked`)

_Ghi nhận nhật ký lịch sử. Trạng thái giữ nguyên: Shipped (4)_

```powershell
curl -X POST "https://localhost:7083/api/webhooks/ghn" `
-H "X-Webhook-Token: fDoaiBJjrN3jc2Re4SIUZgZdNz5HzsPup6BRutbmXlh" `
-H "Content-Type: application/json" `
-d '{
    "OrderCode": "LXDQRT", "Status": "picked", "Type": "switch_status", "Time": "2026-05-28T11:00:00.000Z",
    "CODAmount": 3000000, "PaymentType": 1, "TotalFee": 71400, "Weight": 200, "Length": 10, "Width": 10, "Height": 10, "ReasonCode": "", "Reason": ""
}'
```

#### Bước A4: Hàng hóa đang đi giao cho người nhận (`delivering`)

_Trạng thái hệ thống chuyển: Delivering (5)_

```powershell
curl -X POST "https://localhost:7083/api/webhooks/ghn" `
-H "X-Webhook-Token: fDoaiBJjrN3jc2Re4SIUZgZdNz5HzsPup6BRutbmXlh" `
-H "Content-Type: application/json" `
-d '{
    "OrderCode": "LXDQRT", "Status": "delivering", "Type": "switch_status", "Time": "2026-05-28T12:00:00.000Z",
    "CODAmount": 3000000, "PaymentType": 1, "TotalFee": 71400, "Weight": 200, "Length": 10, "Width": 10, "Height": 10, "ReasonCode": "", "Reason": ""
}'
```

#### Bước A5: Shipper đang tương tác thu tiền khách (`money_collect_delivering`)

_Ghi nhận nhật ký. Trạng thái giữ nguyên: Delivering (5)_

```powershell
curl -X POST "https://localhost:7083/api/webhooks/ghn" `
-H "X-Webhook-Token: fDoaiBJjrN3jc2Re4SIUZgZdNz5HzsPup6BRutbmXlh" `
-H "Content-Type: application/json" `
-d '{
    "OrderCode": "LXDQRT", "Status": "money_collect_delivering", "Type": "switch_status", "Time": "2026-05-28T14:00:00.000Z",
    "CODAmount": 3000000, "PaymentType": 1, "TotalFee": 71400, "Weight": 200, "Length": 10, "Width": 10, "Height": 10, "ReasonCode": "", "Reason": ""
}'
```

#### Bước A6: Giao hàng thành công (`delivered`)

_Trạng thái hệ thống chuyển: Delivered (6). Kết thúc hành trình thành công!_

```powershell
curl -X POST "https://localhost:7083/api/webhooks/ghn" `
-H "X-Webhook-Token: fDoaiBJjrN3jc2Re4SIUZgZdNz5HzsPup6BRutbmXlh" `
-H "Content-Type: application/json" `
-d '{
    "OrderCode": "LXDQRT", "Status": "delivered", "Type": "switch_status", "Time": "2026-05-28T14:30:00.000Z",
    "CODAmount": 3000000, "PaymentType": 1, "TotalFee": 71400, "Weight": 200, "Length": 10, "Width": 10, "Height": 10, "ReasonCode": "", "Reason": ""
}'
```

---

### KỊCH BẢN B: BOM HÀNG - GIAO THẤT BẠI 3 LẦN & CHUYỂN HOÀN THÀNH CÔNG

_Lưu ý: Dùng đơn prepaid **PAID**, `Delivering` (5), `ProviderOrderCode=LXDQRT`. Reset bằng SQL mục 5 nếu cần._

#### Bước B1: Giao thất bại lần 1 - Khách không nghe máy (`delivery_fail`)

_Hệ thống: **DeliveryFailed (12)**. `DeliveryFailCount` = 1. GHN vẫn có thể giao lại — chưa hoàn kho._

```powershell
curl -X POST "https://localhost:7083/api/webhooks/ghn" `
-H "X-Webhook-Token: fDoaiBJjrN3jc2Re4SIUZgZdNz5HzsPup6BRutbmXlh" `
-H "Content-Type: application/json" `
-d '{
    "OrderCode": "LXDQRT", "Status": "delivery_fail", "Type": "update", "Time": "2026-05-28T15:00:00.000Z",
    "CODAmount": 3000000, "PaymentType": 1, "TotalFee": 71400, "Weight": 200, "Length": 10, "Width": 10, "Height": 10,
    "ReasonCode": "GHN-DFC1A4", "Reason": "Người nhận không nghe máy"
}'
```

#### Bước B2: Giao thất bại lần 2 - Khách chặn số điện thoại (`delivery_fail`)

_Trạng thái giữ nguyên: DeliveryFailed (12). `DeliveryFailCount` = 2._

```powershell
curl -X POST "https://localhost:7083/api/webhooks/ghn" `
-H "X-Webhook-Token: fDoaiBJjrN3jc2Re4SIUZgZdNz5HzsPup6BRutbmXlh" `
-H "Content-Type: application/json" `
-d '{
    "OrderCode": "LXDQRT", "Status": "delivery_fail", "Type": "update", "Time": "2026-05-28T16:00:00.000Z",
    "CODAmount": 3000000, "PaymentType": 1, "TotalFee": 71400, "Weight": 200, "Length": 10, "Width": 10, "Height": 10,
    "ReasonCode": "GHN-DFC1A2", "Reason": "Không liên lạc được người nhận / Chặn số"
}'
```

#### Bước B3: Giao thất bại lần 3 - Khách báo không đặt hàng (`delivery_fail`)

_`DeliveryFailCount` = 3. Trạng thái đơn vẫn **DeliveryFailed (12)** — hệ thống **không** tự chuyển WaitingReturn khi đủ 3 lần._
_GHN sẽ gửi webhook **`waiting_to_return`** (bước B3b bên dưới) khi bắt đầu quy trình hoàn._

#### Bước B3b: Chờ shipper lấy hàng hoàn (`waiting_to_return`)

_Trạng thái hệ thống: **WaitingReturn (13)**._

```powershell
curl -X POST "https://localhost:7083/api/webhooks/ghn" `
-H "X-Webhook-Token: fDoaiBJjrN3jc2Re4SIUZgZdNz5HzsPup6BRutbmXlh" `
-H "Content-Type: application/json" `
-d '{
    "OrderCode": "LXDQRT", "Status": "waiting_to_return", "Type": "switch_status", "Time": "2026-05-28T17:15:00.000Z",
    "CODAmount": 3000000, "PaymentType": 1, "TotalFee": 71400, "Weight": 200, "Length": 10, "Width": 10, "Height": 10, "ReasonCode": "", "Reason": ""
}'
```

```powershell
curl -X POST "https://localhost:7083/api/webhooks/ghn" `
-H "X-Webhook-Token: fDoaiBJjrN3jc2Re4SIUZgZdNz5HzsPup6BRutbmXlh" `
-H "Content-Type: application/json" `
-d '{
    "OrderCode": "LXDQRT", "Status": "delivery_fail", "Type": "update", "Time": "2026-05-28T17:00:00.000Z",
    "CODAmount": 3000000, "PaymentType": 1, "TotalFee": 71400, "Weight": 200, "Length": 10, "Width": 10, "Height": 10,
    "ReasonCode": "GHN-DCD1A1", "Reason": "Người nhận báo không đặt hàng"
}'
```

#### Bước B4: Bưu cục lên lịch chuyển hoàn về Shop (`return`)

_**Prepaid:** Returning (10). **COD:** tại bước `return` đơn chuyển **Cancelled (8)**, restore stock + voucher (xem SQL sau B4)._

```powershell
curl -X POST "https://localhost:7083/api/webhooks/ghn" `
-H "X-Webhook-Token: fDoaiBJjrN3jc2Re4SIUZgZdNz5HzsPup6BRutbmXlh" `
-H "Content-Type: application/json" `
-d '{
    "OrderCode": "LXDQRT", "Status": "return", "Type": "switch_status", "Time": "2026-05-28T17:30:00.000Z",
    "CODAmount": 3000000, "PaymentType": 1, "TotalFee": 71400, "Weight": 200, "Length": 10, "Width": 10, "Height": 10, "ReasonCode": "", "Reason": ""
}'
```

#### Bước B5: Shipper đang mang trả hàng lại Shop (`returning`)

_Ghi nhận nhật ký lịch sử. Trạng thái giữ nguyên: Returning (10)._

```powershell
curl -X POST "https://localhost:7083/api/webhooks/ghn" `
-H "X-Webhook-Token: fDoaiBJjrN3jc2Re4SIUZgZdNz5HzsPup6BRutbmXlh" `
-H "Content-Type: application/json" `
-d '{
    "OrderCode": "LXDQRT", "Status": "returning", "Type": "switch_status", "Time": "2026-05-28T18:00:00.000Z",
    "CODAmount": 3000000, "PaymentType": 1, "TotalFee": 71400, "Weight": 200, "Length": 10, "Width": 10, "Height": 10, "ReasonCode": "", "Reason": ""
}'
```

#### Bước B6: Shop đã nhận lại hàng hoàn thành công (`returned`)

_**Prepaid (SE_PAY / WALLET / BANK):** `Orders.StatusID` → **Cancelled (8)**, `PaymentStatus` giữ **PAID**, tạo **system refund** `RefundRequested`. Khách thấy *Refund processing*. Admin: **Approve → Complete** (không Reject) → `REFUNDED` + credit ví._
_**COD:** đã Cancelled tại B4; `returned` chủ yếu cập nhật `ReturnedAt` / log._

```powershell
curl -X POST "https://localhost:7083/api/webhooks/ghn" `
-H "X-Webhook-Token: fDoaiBJjrN3jc2Re4SIUZgZdNz5HzsPup6BRutbmXlh" `
-H "Content-Type: application/json" `
-d '{
    "OrderCode": "LXDQRT", "Status": "returned", "Type": "switch_status", "Time": "2026-05-28T19:00:00.000Z",
    "CODAmount": 3000000, "PaymentType": 1, "TotalFee": 71400, "Weight": 200, "Length": 10, "Width": 10, "Height": 10, "ReasonCode": "", "Reason": ""
}'
```

---

### KỊCH BẢN C: GIAO THẤT BẠI 3 LẦN & SHOP TỪ CHỐI NHẬN HÀNG HOÀN (HÀNG BỊ HỎNG/SAI LỆCH)

#### Bước C1: Shipper đang hoàn trả hàng nhưng Shop từ chối nhận lại (`return_fail`)

_Hệ thống chuyển trạng thái đơn sang: ReturnFailed (14)._
_Admin/CSKH lập tức vào cuộc xử lý khiếu nại bồi thường với GHN._

```powershell
curl -X POST "https://localhost:7083/api/webhooks/ghn" `
-H "X-Webhook-Token: fDoaiBJjrN3jc2Re4SIUZgZdNz5HzsPup6BRutbmXlh" `
-H "Content-Type: application/json" `
-d '{
    "OrderCode": "LXDQRT", "Status": "return_fail", "Type": "update", "Time": "2026-05-28T20:00:00.000Z",
    "CODAmount": 3000000, "PaymentType": 1, "TotalFee": 71400, "Weight": 200, "Length": 10, "Width": 10, "Height": 10,
    "ReasonCode": "GHN-RFE0A4", "Reason": "Người gửi từ chối nhận lại do hàng hư hỏng nặng"
}'
```

---

### KỊCH BẢN D: SỰ CỐ MẤT HÀNG TRONG VẬN CHUYỂN (`lost`)

_Hệ thống chuyển trạng thái đơn sang: Lost (15)._
_(Nếu là đơn Prepaid: hệ thống tự động Hủy đơn Cancelled (8), tạo yêu cầu hoàn tiền tự động RefundRequested nhưng bảo vệ tồn kho nghiêm ngặt: KHÔNG khôi phục tồn kho ảo do hàng đã mất)._

```powershell
curl -X POST "https://localhost:7083/api/webhooks/ghn" `
-H "X-Webhook-Token: fDoaiBJjrN3jc2Re4SIUZgZdNz5HzsPup6BRutbmXlh" `
-H "Content-Type: application/json" `
-d '{
    "OrderCode": "LXDQRT", "Status": "lost", "Type": "switch_status", "Time": "2026-05-28T20:30:00.000Z",
    "CODAmount": 3000000, "PaymentType": 1, "TotalFee": 71400, "Weight": 200, "Length": 10, "Width": 10, "Height": 10, "ReasonCode": "", "Reason": ""
}'
```

---

### KỊCH BẢN E: SỰ CỐ HỎNG HÀNG TRONG VẬN CHUYỂN (`damage`)

_Hệ thống chuyển trạng thái đơn sang: Damaged (16)._
_(Nếu là đơn Prepaid: hệ thống tự động Hủy đơn Cancelled (8), tạo yêu cầu hoàn tiền tự động RefundRequested nhưng bảo vệ tồn kho nghiêm ngặt: KHÔNG khôi phục tồn kho ảo do hàng đã hỏng)._

```powershell
curl -X POST "https://localhost:7083/api/webhooks/ghn" `
-H "X-Webhook-Token: fDoaiBJjrN3jc2Re4SIUZgZdNz5HzsPup6BRutbmXlh" `
-H "Content-Type: application/json" `
-d '{
    "OrderCode": "LXDQRT", "Status": "damage", "Type": "switch_status", "Time": "2026-05-28T21:00:00.000Z",
    "CODAmount": 3000000, "PaymentType": 1, "TotalFee": 71400, "Weight": 200, "Length": 10, "Width": 10, "Height": 10, "ReasonCode": "", "Reason": ""
}'
```

---

### KỊCH BẢN F: GHN HỦY ĐƠN VẬN CHUYỂN (`cancel`)

_Trạng thái hệ thống chuyển: Cancelled (8)._
_(Kích hoạt giải phóng tồn kho thực tế, hoàn lại Voucher cho khách hàng)._

```powershell
curl -X POST "https://localhost:7083/api/webhooks/ghn" `
-H "X-Webhook-Token: fDoaiBJjrN3jc2Re4SIUZgZdNz5HzsPup6BRutbmXlh" `
-H "Content-Type: application/json" `
-d '{
    "OrderCode": "LXDQRT", "Status": "cancel", "Type": "switch_status", "Time": "2026-05-28T21:30:00.000Z",
    "CODAmount": 3000000, "PaymentType": 1, "TotalFee": 71400, "Weight": 200, "Length": 10, "Width": 10, "Height": 10, "ReasonCode": "", "Reason": ""
}'
```

---

## 5. CÂU LỆNH SQL HỖ TRỢ KIỂM THỬ TRÊN CƠ SỞ DỮ LIỆU

### A. Khởi tạo dữ liệu sạch với mã vận đơn `LXDQRT` (hoặc gắn đơn ship thật):

```sql
-- 1. Tạo đơn hàng đồ chơi thử nghiệm
INSERT INTO Orders (OrderCode, AccountID, StatusID, TotalAmount, PaymentMethod, PaymentStatus, DeliveryFailCount, CreatedAt, UpdatedAt, IsDeleted)
VALUES ('ORD_TEST_GHN_002', 1, 3, 3000000, 'SHIP_COD', 'PENDING', 0, GETDATE(), GETDATE(), 0);

DECLARE @OrderID INT = SCOPE_IDENTITY();

-- 2. Thêm chi tiết đơn hàng
INSERT INTO OrderDetails (OrderID, ProductID, ProductName, Quantity, UnitPrice)
VALUES (@OrderID, 1, N'Robot LEGO Chiến Binh Cao Cấp', 1, 3000000);

-- 3. Tạo mã giao vận liên kết với GHN ( LXDQRT )
INSERT INTO ShippingProviderTransactions (OrderID, Provider, ProviderOrderCode, Status, CreatedAt, UpdatedAt)
VALUES (@OrderID, 'GHN', 'LXDQRT', 'ready_to_pick', GETDATE(), GETDATE());

UPDATE Orders SET ShippingOrderCode = 'LXDQRT', StatusID = 5, PaymentMethod = 'SE_PAY', PaymentStatus = 'PAID'
WHERE OrderID = @OrderID;
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
WHERE tx.ProviderOrderCode = 'LXDQRT';

-- System refund sau returned (prepaid)
SELECT r.RefundID, r.RefundCode, rs.StatusName, r.ApprovedAmount, o.PaymentStatus
FROM OrderRefunds r
JOIN RefundStatuses rs ON rs.StatusID = r.StatusID
JOIN Orders o ON o.OrderID = r.OrderID
WHERE o.OrderID = (SELECT OrderID FROM ShippingProviderTransactions WHERE ProviderOrderCode = 'LXDQRT');

-- Truy vấn xem nhật ký lịch sử cuộc gọi webhook GHN (Tránh trùng lặp Idempotency)
SELECT * FROM ShippingStatusHistories
WHERE OrderID = (SELECT OrderID FROM Orders WHERE OrderCode = 'ORD_TEST_GHN_002')
ORDER BY ProcessedAt DESC;
```
