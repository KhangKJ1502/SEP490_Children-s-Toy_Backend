# GHN Delivery Fail & Return-to-Warehouse — Test Guide (TA)

## 1. Feature Overview

This feature handles the **GHN return-to-sender flow**: when the customer **does not receive** the package, GHN retries delivery and eventually **returns the goods to the shop/warehouse**. This is **not** the same as a customer-initiated refund after order completion.

**Typical GHN sequence:**

```
delivery_fail (retry delivery)
    → waiting_to_return
    → return / return_transporting / returning
    → returned (success — goods back at warehouse)
    OR return_fail (return attempt failed — admin manual action)
```

**Related services:**

| Component | Path |
|-----------|------|
| Webhook entry | `ToyStore.API/Controllers/ShippingWebhooksController.cs` |
| Webhook handler | `ToyStore.Infrastructure/Services/ShippingWebhookService.cs` |
| Return flow logic | `ToyStore.Infrastructure/Services/ShippingReturnFlowService.cs` |
| Status resolver | `ToyStore.Application/Services/ShippingStatusMapper.cs` |

---

## 2. GHN Status → System Mapping

| GHN status | Internal `Orders.StatusID` | Customer UI label | Admin sees |
|------------|---------------------------|-------------------|------------|
| `delivery_fail` | **5** Delivering (unchanged) | Delivering | Delivering |
| `waiting_to_return` | **10** Returning | Returning to warehouse | Returning |
| `return` | **8** Cancelled (COD) or **10** Returning (prepaid) | Cancelled / Returning to warehouse | Cancelled / Returning |
| `return_transporting`, `return_sorting`, `returning` | **10** Returning (unchanged) | Returning to warehouse | Returning |
| `returned` | **11** ReturnCompleted → then payment branch | Returned to warehouse / Refund processing | ReturnCompleted |
| `return_fail` | **10** Returning (unchanged) | Returning to warehouse | Returning + admin alert |
| `damage`, `lost` | **8** Cancelled | Cancelled | Cancelled |
| `cancel` (GHN) | **8** Cancelled | Cancelled | Cancelled |
| `exception` | Current status (unchanged) | — | — |

### Payment branch after `returned`

| PaymentMethod | Action |
|---------------|--------|
| `SHIP_COD` | Cancel order (8), `PaymentStatus=CANCELLED`, restore stock, notify customer |
| `SE_PAY` / `WALLET` / `BANK_TRANSFER` | Create `OrderRefunds` (Requested), keep status 11, notify customer + staff for approval |

---

## 3. Prerequisites

### 3.1 Database seed (run once)

```bash
docs/database/changes/20260526_ReturnFlow_StatusAndRefundReason.sql
```

Adds: `StatusOrders` 10/11, refund reason *"Giao hàng thất bại / không giao được"*, notification templates.

### 3.2 Running processes

| Process | Purpose |
|---------|---------|
| `ToyStore.API` | Receives webhooks |
| `ToyStore.Worker` | Processes outbox → sends notifications |

### 3.3 Test order requirements

- Order in **Delivering** (`StatusID = 5`)
- Row in `ShippingProviderTransactions` with valid `ProviderOrderCode`
- For prepaid return tests: `PaymentMethod` = `SE_PAY` / `WALLET` / `BANK_TRANSFER` and `PaymentStatus = PAID`

**Find a test order:**

```sql
SELECT o.OrderID, o.OrderCode, o.StatusID, o.PaymentMethod, o.PaymentStatus,
       s.ProviderOrderCode, s.Status AS GhnStatus
FROM Orders o
JOIN ShippingProviderTransactions s ON s.OrderID = o.OrderID
WHERE o.StatusID = 5
ORDER BY o.OrderID DESC;
```

---

## 4. Webhook Request Format

| Item | Value |
|------|-------|
| Method | `POST` |
| URL | `/api/webhooks/shipping/GHN` |
| Header | `X-Webhook-Token: <Webhooks:Shipping:Tokens:GHN>` (see `appsettings.json`) |
| Body | `{"OrderCode":"<ProviderOrderCode>","status":"<ghn_status>"}` |

**PowerShell example:**

```powershell
$token = "<token-from-appsettings>"
$code  = "<ProviderOrderCode>"

Invoke-RestMethod -Method POST `
  -Uri "https://localhost:<port>/api/webhooks/shipping/GHN" `
  -Headers @{ "X-Webhook-Token" = $token; "Content-Type" = "application/json" } `
  -Body "{ `"OrderCode`": `"$code`", `"status`": `"delivery_fail`" }"
```

> API always returns **200 OK** (by design — prevents GHN retries on HTTP errors).

---

## 5. Test Cases

### TC-01 — `delivery_fail` (GHN retry, order unchanged)

**Payload:**

```json
{"OrderCode":"{ProviderOrderCode}","status":"delivery_fail"}
```

**Expected:**

- `Orders.StatusID` = **5** (Delivering)
- `Orders.CancelReason` = `DELIVERY_FAILED_GHN`
- `OrderStatusHistory` note: `GHN delivery_fail lần 1`
- `ShippingStatusHistories` row with `Source = GHN_WEBHOOK`
- Customer notification: delivery failed, GHN will retry

---

### TC-02 — Idempotency (duplicate webhook)

Send the **exact same payload** twice.

**Expected:**

- Second request skipped (no duplicate `ShippingStatusHistories` / side effects)
- No duplicate notification

---

### TC-03 — Full return lifecycle (prepaid order)

Use order with `SE_PAY` / `WALLET` / `BANK_TRANSFER` and `PaymentStatus = PAID`.

| Step | Payload status | Expected `StatusID` |
|------|----------------|---------------------|
| 1 | `waiting_to_return` | **10** Returning |
| 2 | `return_transporting` | **10** (history only) |
| 3 | `returned` | **11** ReturnCompleted → **8** Cancelled |

**After step 3 (`returned`):**

- `OrderRefunds` row: `RefundStatus = RefundReceived`, `RefundSource = System`
- `RestorableQuantity` defaults to `Quantity` per line
- `CustomerShippingPaid` = max(0, TotalAmount - sum(RefundDetail.RefundAmount))
- `ApprovedAmount = TotalAmount` (includes shipping)
- Merchandise notification: link to `/admin/refunds/{refundId}`
- No Staff approval notification (refund is auto-created at warehouse receipt)

**Admin follow-up — GHN Return Inspection Flow (3 steps):**

| Step | Actor | Action | Endpoint |
|------|-------|--------|----------|
| 1 | Merchandise | Submit inspection + restock qty | `PATCH /api/admin/refunds/{id}/status` `{"status":"RefundInspectionPending","restockItems":[{"productId":X,"restorableQuantity":2}],"inspectionNote":"...","damageResponsibility":null}` |
| 2 | Staff | Confirm wallet refund, choose shipping refund | `{"status":"RefundCompleted","includeShippingInRefund":true}` |

**Legacy in-flight refunds** (`RefundRequested` / `RefundApproved`): Merchandise may transition directly to `RefundInspectionPending` without Staff Approve.

**Expected after Staff Complete (`includeShippingInRefund=true`):**
- `FinalRefundAmount = order.TotalAmount`
- Wallet credited with `FinalRefundAmount`
- Stock: only `RestorableQuantity` units returned to inventory (not `Quantity`)
- `VoucherUsageLogs` unchanged (voucher NOT restored)

**Expected after Staff Complete (`includeShippingInRefund=false`):**
- `FinalRefundAmount = TotalAmount - CustomerShippingPaid`

**Expected after case Carrier fault (`damageResponsibility="Carrier"`):**
- All `RestorableQuantity` = 0
- No stock restoration
- `FinalRefundAmount` = TotalAmount (hoặc TotalAmount - CustomerShippingPaid tuỳ chọn ship)

---

### TC-04 — `returned` on COD order

Use order with `PaymentMethod = SHIP_COD`.

**Payload:**

```json
{"OrderCode":"{ProviderOrderCode}","status":"returned"}
```

**Expected:**

- `Orders.StatusID` = **8** (Cancelled)
- `Orders.PaymentStatus` = `CANCELLED`
- Stock restored
- `PaymentHistory` row with `PaymentStatus = CANCELLED`
- Customer notification: order cancelled due to delivery failure

---

### TC-05 — `damage` / `lost`

**Payload:**

```json
{"OrderCode":"{ProviderOrderCode}","status":"damage"}
```

**Expected:**

- `Orders.StatusID` = **8**
- `Orders.CancelReason` = `DAMAGED_IN_TRANSIT` (or `LOST_IN_TRANSIT` for `lost`)
- Payment branch same as `returned` (COD → cancel; prepaid → system refund)

---

### TC-06 — Customer display masking

After TC-03 step 1 or 2, call:

`GET /api/orders/{id}` (customer JWT)

**Expected:** `StatusName` = **"Delivering"** (not Returning/ReturnCompleted).

---

### TC-07 — Admin list & detail (return tracking)

Run after TC-03 (prepaid) or TC-04 (COD) webhook sequence on a test order in **Delivering**.

#### TC-07a — Admin list filter **Delivering**

`GET /api/admin/orders?statusId=5` (Admin JWT)

**Expected:** Order still appears when internal `StatusID` is **10**, **11**, or **8** (COD after `return`), or when GHN status is in the return flow (`waiting_to_return`, `return`, `returned`, etc.).

List item fields:

| Field | Expected |
|-------|----------|
| `statusId` | Actual internal ID (5, 8, 10, or 11) |
| `fulfillmentLabel` | Human-readable label (e.g. *Returning to warehouse*, *Cancelled — …*) |
| `ghnShippingStatus` | Latest GHN status from `ShippingProviderTransactions` |

#### TC-07b — Admin list filters **Returning** / **Cancelled**

| Request | Expected orders |
|---------|-----------------|
| `?statusId=10` | `StatusID = 10` only |
| `?statusId=11` | `StatusID = 11` only |
| `?statusId=8` | Cancelled (including COD cancelled on GHN `return`) |

#### TC-07c — Admin order detail

`GET /api/admin/orders/{id}` (Admin JWT)

**Expected:**

- `statusId`, `statusName`, `fulfillmentLabel`, `ghnShippingStatus` populated
- `shippingHistory[]` contains webhook steps (`waiting_to_return`, `return`, `returned`, …) sorted newest first
- `statusHistory` shows internal transitions (no customer-side masking)

Admin UI (optional): **Order Management → Delivering** filter, open detail → **Shipping History** tab.

#### TC-07d — Assigned Staff / Merchandise

1. Assign order to a Staff or Merchandise account.
2. Log in as that user → enable **My Orders**.
3. Search by `OrderCode` if the order is **Cancelled**.

**Expected:** Same `fulfillmentLabel` / GHN chip on list row; detail accessible while assignment is active.

---

### TC-08 — Pre-cancelled order

1. Cancel order manually (customer/admin)
2. Send any GHN webhook for that order

**Expected:**

- `ShippingStatusHistories` updated
- `Orders.StatusID` **unchanged** (still Cancelled)

---

## 6. Verification Queries

```sql
-- Order state
SELECT OrderID, StatusID, PaymentStatus, CancelReason, CancelledAt
FROM Orders WHERE OrderID = @OrderID;

-- Audit trail
SELECT StatusID, Note, CreatedAt FROM OrderStatusHistory
WHERE OrderID = @OrderID ORDER BY CreatedAt DESC;

SELECT NewStatus, PreviousStatus, Source, ProcessedAt
FROM ShippingStatusHistories WHERE OrderID = @OrderID ORDER BY ProcessedAt DESC;

-- Refund (prepaid)
SELECT RefundID, RefundStatus, ApprovedAmount, ReasonDetails
FROM OrderRefunds WHERE OrderID = @OrderID;

-- Outbox events
SELECT TOP 5 EventType, Payload, CreatedAt
FROM [System].[DomainEventOutbox]
WHERE AggregateId = CAST(@OrderID AS VARCHAR)
ORDER BY CreatedAt DESC;
```

---

## 7. GHN Return Flow vs Customer Refund

| | GHN return flow | Customer refund request |
|---|-----------------|-------------------------|
| Trigger | GHN webhook (failed delivery) | Customer API after **Completed** |
| Goods location | Never delivered / returning to warehouse | Customer already received |
| Refund reason | Giao hàng thất bại / không giao được | Product defect, wrong item, etc. |
| Entry point | `ShippingWebhookService` | `RefundsController` |

---

### TC-09 — System Return: Include shipping fee

**Setup:** Prepaid order (TotalAmount = 500,000đ, CustomerShippingPaid = 30,000đ). Complete full 5-step flow với `includeShippingInRefund=true`.

**Expected:** `FinalRefundAmount = 500,000đ`. Wallet credited 500,000đ.

---

### TC-10 — System Return: Exclude shipping fee

Same setup, `includeShippingInRefund=false`.

**Expected:** `FinalRefundAmount = 470,000đ` (= TotalAmount - CustomerShippingPaid).

---

### TC-11 — System Return: Freeship voucher

Order with freeship voucher → `CustomerShippingPaid = 0`.

**Expected:** Toggle hoàn ship ẩn/disabled. `FinalRefundAmount = TotalAmount` bất kể chọn gì.

---

### TC-12 — Voucher không restore sau Complete

Sau khi Complete system return, kiểm tra:

```sql
SELECT * FROM VoucherUsageLogs WHERE OrderID = @OrderID;
```

**Expected:** Bản ghi vẫn còn, `IsUsed = 1`. Voucher không được kích hoạt lại.

---

### TC-13 — Carrier fault: RestorableQuantity = 0, stock unchanged

Merchandise đánh giá `damageResponsibility = Carrier` tại `RefundInspectionPending`.

**Expected:**
- `RefundDetails.RestorableQuantity = 0` cho tất cả dòng
- `Products.Quantity` không thay đổi sau Complete
- Staff vẫn được Complete (tiền vẫn hoàn cho khách)

---

### TC-14 — Regression: COD flow unchanged

Gửi `returned` webhook với COD order.

**Expected:** Không tạo refund, cancel ngay lập tức, stock restore đầy đủ (`Quantity`). Không có bước Merch/Staff mới.

---

### TC-15 — Regression: Customer-initiated return unchanged

Tạo customer refund request sau khi order **Completed**. Process toàn bộ flow customer return.

**Expected:**
- `getNextStatus` trả về `RefundPickupCreated` (không phải `RefundReceived`)
- Modal không hiện `RestockQuantitySection` hay `SystemReturnCompleteSection`
- `IncludeShippingInRefund` không được set
- Stock restored theo `Quantity` (không phải `RestorableQuantity`)

---

## 8. Test Checklist Summary

- [ ] TC-01: `delivery_fail` keeps Delivering
- [ ] TC-02: Duplicate webhook idempotency
- [ ] TC-03: Prepaid return lifecycle — full 5-step inspection flow
- [ ] TC-04: COD `returned` → Cancelled (regression)
- [ ] TC-05: `damage` / `lost`
- [ ] TC-06: Customer status masking
- [ ] TC-07a: Delivering filter includes return-flow orders
- [ ] TC-07b: Returning (10) / Returned pending (11) / Cancelled (8) filters
- [ ] TC-07c: Admin detail has `shippingHistory` + GHN fields
- [ ] TC-07d: Assigned Staff/Merch — My Orders + GHN chip
- [ ] TC-08: Pre-cancelled order ignored
- [ ] TC-09: IncludeShipping=true → FinalRefundAmount = TotalAmount
- [ ] TC-10: IncludeShipping=false → FinalRefundAmount = TotalAmount - CustomerShippingPaid
- [ ] TC-11: Freeship voucher → toggle ẩn, full TotalAmount
- [ ] TC-12: VoucherUsageLogs intact after Complete (voucher NOT restored)
- [ ] TC-13: Carrier fault → RestorableQuantity=0, no stock change
- [ ] TC-14: COD regression unchanged
- [ ] TC-15: Customer return regression unchanged
