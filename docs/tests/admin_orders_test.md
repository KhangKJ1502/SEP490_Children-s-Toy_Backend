# Admin Orders — Test Plan

## Scope

Module quản lý đơn hàng phía Admin:
- `GET /api/admin/orders`
- `GET /api/admin/orders/{id}`
- `PATCH /api/admin/orders/{id}/confirm`
- `PATCH /api/admin/orders/{id}/process`
- `PATCH /api/admin/orders/{id}/ship`
- `PATCH /api/admin/orders/{id}/cancel`
- `PATCH /api/admin/orders/{id}/assign`
- `POST /api/webhooks/shipping/{provider}`

---

## UC1 — GET /api/admin/orders

| # | Test | Role | Input | Expected |
|---|------|------|-------|----------|
| 1.1 | Danh sách mặc định | Staff | pageNumber=1, pageSize=10 | 200 + chỉ đơn **đã gán OA RoleID=3** và status Pending/Confirmed |
| 1.2 | Danh sách mặc định | Merchandise | pageNumber=1 | 200 + chỉ đơn **đã gán OA RoleID=4** và status Confirmed/Processing/Shipped |
| 1.3 | Danh sách mặc định | Admin | pageNumber=1 | 200 + tất cả trạng thái |
| 1.4 | Đơn không gán cho mình | Staff | GET order id của Staff khác | 403 (xem `order_access_authorization_test.md`) |
| 1.5 | Filter keyword | Staff | keyword=ORD-001 | 200 + đúng đơn |
| 1.6 | Filter fromDate/toDate | Admin | fromDate=2026-01-01&toDate=2026-12-31 | 200 + đơn trong khoảng |
| 1.7 | Filter statusId | Admin | statusId=3 | 200 + chỉ đơn Processing |
| 1.8 | Không có token | — | — | 401 |
| 1.9 | Role Customer | Customer | — | 403 |
| 1.10 | pageSize > 100 | Admin | pageSize=200 | 200 + clamp về 100 |

---

## UC2 — GET /api/admin/orders/{id}

| # | Test | Role | Input | Expected |
|---|------|------|-------|----------|
| 2.1 | Lấy đơn tồn tại | Admin | id=1 | 200 + full detail (items, history, shipping) |
| 2.1b | Đơn gán cho Staff khác | Staff | id=đơn của người khác | 403 FORBIDDEN |
| 2.2 | Đơn không tồn tại | Admin | id=99999 | 404 |
| 2.3 | Đơn đã soft-delete | Admin | id=xoá | 404 |

---

## UC3 — PATCH /api/admin/orders/{id}/confirm

| # | Test | Role | Input | Expected |
|---|------|------|-------|----------|
| 3.1 | Confirm Pending | Staff | id=pending-order, note="ok" | 200 + ConfirmedAt set |
| 3.2 | Confirm Pending | Admin | — | 200 |
| 3.3 | Confirm Confirmed | Staff | id=confirmed-order | 422 — already confirmed |
| 3.4 | Confirm Processing | Staff | — | 422 |
| 3.5 | Role Merchandise cố confirm | Merchandise | — | 403 |
| 3.6 | Đơn không tồn tại | Staff | id=99999 | 404 |
| 3.7 | Kiểm tra DB: AssignedToStaffId set, OrderStatusHistory ghi thêm | Staff | — | verify DB |

---

## UC4 — PATCH /api/admin/orders/{id}/process

| # | Test | Role | Input | Expected |
|---|------|------|-------|----------|
| 4.1 | Process Confirmed | Merchandise | id=confirmed-order | 200 |
| 4.2 | Process Pending | Merchandise | — | 422 |
| 4.3 | Role Staff cố process | Staff | — | 403 |
| 4.4 | Kiểm tra DB: AssignedToStaffId = merchandiseId | Merchandise | — | verify DB |

---

## UC5 — PATCH /api/admin/orders/{id}/ship

| # | Test | Role | Input | Expected |
|---|------|------|-------|----------|
| 5.1 | Ship Processing, SHIP_CODE | Merchandise | provider=GHN | 200 + TrackingNumber |
| 5.2 | Ship Processing, BANK_TRANSFER | Merchandise | provider=GHN | 200 + CodAmount=0 |
| 5.3 | Ship Confirmed | Merchandise | — | 422 |
| 5.4 | Provider không hỗ trợ | Merchandise | provider=GHTK | 400 validation |
| 5.5 | GHN API lỗi (mock) | Merchandise | — | 502 |
| 5.6 | Role Staff cố ship | Staff | — | 403 |
| 5.7 | Kiểm tra DB: ShippingProviderTransaction, Order.ShippedAt, Order.ShippingOrderCode | — | — | verify DB |

---

## UC6 — PATCH /api/admin/orders/{id}/cancel

| # | Test | Role | Input | Expected |
|---|------|------|-------|----------|
| 6.1 | Cancel Pending | Staff | reason="Customer request" | 200 + CancelledAt |
| 6.2 | Cancel Confirmed | Admin | — | 200 |
| 6.3 | Cancel Processing | Staff | — | 422 |
| 6.4 | Cancel Shipped | Staff | — | 422 |
| 6.5 | reason rỗng | Staff | reason="" | 400 validation |
| 6.6 | Role Merchandise cố cancel | Merchandise | — | 403 |

---

## UC7 — PATCH /api/admin/orders/{id}/assign

| # | Test | Role | Input | Expected |
|---|------|------|-------|----------|
| 7.1 | Assign Pending sang Staff | Admin | targetAccountId=staff-id | 200 |
| 7.2 | Assign Processing sang Merchandise | Admin | targetAccountId=merch-id | 200 |
| 7.3 | Assign Processing sang Staff | Admin | targetAccountId=staff-id | 422 |
| 7.4 | TargetAccountId không tồn tại | Admin | targetAccountId=99999 | 404 |
| 7.5 | TargetAccountId không active | Admin | targetAccountId=inactive | 404 |
| 7.6 | Role Staff gọi | Staff | — | 403 |
| 7.7 | Assign Delivered | Admin | — | 422 |

---

## UC8 — POST /api/webhooks/shipping/{provider}

| # | Test | Provider | Header | Payload | Expected |
|---|------|----------|--------|---------|----------|
| 8.1 | Token hợp lệ, status delivering | GHN | X-Webhook-Token=... | {"order_code":"ORD","status":"delivering"} | 200 + Order→Delivering |
| 8.2 | Token hợp lệ, status delivered | GHN | valid | {"order_code":"ORD","status":"delivered"} | 200 + Order→Delivered + DeliveredAt |
| 8.3 | Token sai | GHN | X-Webhook-Token=wrong | — | 200 (không xử lý, log warning) |
| 8.4 | Thiếu header token | GHN | — | — | 200 |
| 8.5 | ProviderOrderCode không tồn tại | GHN | valid | {"order_code":"NOTEXIST","status":"delivering"} | 200 (log warning) |
| 8.6 | Status không map | GHN | valid | {"order_code":"ORD","status":"picking"} | 200 + chỉ ghi ShippingStatusHistory |
| 8.7 | JSON lỗi | GHN | valid | invalid-json | 200 (log warning) |

---

## Transaction rollback tests

| # | Scenario | Expected |
|---|----------|----------|
| T1 | DB lỗi giữa confirm (SaveChanges throw) | Rollback toàn bộ, trạng thái không thay đổi |
| T2 | GHN API lỗi trước BeginTransaction (ship) | Không có ghi DB nào |
| T3 | DB lỗi sau GHN success (ship) | Rollback DB, log warning; GHN đơn đã tạo (cần manual cleanup) |

---

## Auto-confirm (UC3b)

| # | Scenario | Expected |
|---|----------|----------|
| A1 | PaymentStatus=PAID, Status=Pending | Confirm tự động, OrderStatusHistory.ChangedBy=null |
| A2 | PaymentStatus=PAID, Status=Confirmed | Skip, không thay đổi |
| A3 | PaymentStatus=PENDING, Status=Pending | Skip |
| A4 | DB lỗi khi auto-confirm | Log error, không throw, payment không bị rollback |
