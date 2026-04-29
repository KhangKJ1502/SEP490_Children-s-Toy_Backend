# Campaign API — Test Guide

## Base URL

```
https://localhost:{PORT}/api/campaigns
```

---

## Endpoints

### 1. GET /api/campaigns — View Campaign List

**Query Parameters:**

| Parameter    | Type    | Default | Description                                  |
|--------------|---------|---------|----------------------------------------------|
| pageNumber   | int     | 1       | Page number (>= 1)                           |
| pageSize     | int     | 10      | Items per page (1–100)                       |
| searchTerm   | string? | null    | Filter by campaign name, template code, event key |
| status       | string? | null    | Filter by status (e.g. `draft`, `sent`)      |
| sourceType   | string? | null    | Filter by source type                        |
| sortBy       | string? | null    | `campaignname`, `status`, `scheduledat`, `createdat` |
| sortDesc     | bool    | false   | Sort descending if true                      |

**Happy path (200):**

```http
GET /api/campaigns?pageNumber=1&pageSize=10
```

Expected response:
```json
{
  "items": [...],
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 2,
  "totalCount": 15,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

**Filter by status:**

```http
GET /api/campaigns?status=sent&pageSize=5
```

**Sort by createdAt descending:**

```http
GET /api/campaigns?sortBy=createdat&sortDesc=true
```

**Validation Error (400) — invalid page:**

```http
GET /api/campaigns?pageNumber=0
```

Expected:
```json
{
  "isSuccess": false,
  "errorCode": "VALIDATION_ERROR",
  "errorMessage": "Page number must be greater than 0."
}
```

---

### 2. GET /api/campaigns/{campaignId} — View Campaign Detail

**Happy path (200):**

```http
GET /api/campaigns/1
```

Expected response:
```json
{
  "campaignId": 1,
  "campaignName": "Summer Sale Push",
  "templateCode": "PROMO_01",
  "titleOverride": null,
  "messageOverride": null,
  "sourceType": "manual",
  "targetType": "all",
  "status": "sent",
  "scheduledAt": "2025-06-01T08:00:00",
  "eventKey": null,
  "imageUrl": "https://...",
  "actionType": "url",
  "actionTarget": "https://store.com/sale",
  "createdByAccountId": 5,
  "createdAt": "2025-05-20T10:00:00",
  "updatedAt": null,
  "stat": {
    "statId": 1,
    "totalSent": 1200,
    "totalRead": 800,
    "totalClicked": 300,
    "computedAt": "2025-06-02T00:00:00"
  },
  "targets": [
    {
      "campaignTargetId": 1,
      "targetType": "segment",
      "targetValue": "vip_customers"
    }
  ]
}
```

**Not Found (404):**

```http
GET /api/campaigns/99999
```

Expected:
```json
{
  "isSuccess": false,
  "errorCode": "NOT_FOUND",
  "errorMessage": "Campaign with ID '99999' was not found."
}
```

**Validation Error (400) — invalid ID:**

```http
GET /api/campaigns/0
```

Expected:
```json
{
  "isSuccess": false,
  "errorCode": "VALIDATION_ERROR",
  "errorMessage": "Campaign ID must be greater than 0."
}
```

---

### 3. GET /api/campaigns/search — Search Campaigns

**Query Parameters:**

| Parameter  | Type   | Required | Description        |
|------------|--------|----------|--------------------|
| searchTerm | string | ✅ Yes    | Keyword to search  |
| pageNumber | int    | No       | Default 1          |
| pageSize   | int    | No       | Default 10         |
| sortBy     | string | No       | Sort column        |
| sortDesc   | bool   | No       | Default false      |

**Happy path (200):**

```http
GET /api/campaigns/search?searchTerm=summer&pageNumber=1&pageSize=5
```

**Validation Error (400) — empty search term:**

```http
GET /api/campaigns/search?searchTerm=
```

Expected:
```json
{
  "isSuccess": false,
  "errorCode": "VALIDATION_ERROR",
  "errorMessage": "Search term is required."
}
```

---

## Database Check

```sql
-- Xem danh sach Campaign
SELECT c.CampaignId, c.CampaignName, c.Status, c.SourceType, c.TargetType, c.ScheduledAt, c.IsDeleted
FROM Campaigns c
WHERE c.IsDeleted = 0
ORDER BY c.CampaignId;

-- Xem chi tiet Campaign + Stat + Targets
SELECT c.*, cs.TotalSent, cs.TotalRead, cs.TotalClicked, ct.TargetType, ct.TargetValue
FROM Campaigns c
LEFT JOIN CampaignStats cs ON cs.CampaignId = c.CampaignId
LEFT JOIN CampaignTargets ct ON ct.CampaignId = c.CampaignId
WHERE c.CampaignId = 1 AND c.IsDeleted = 0;
```
# Campaign Add — API Test Guide

> Base URL: `https://localhost:{port}/api/campaigns`

---

## 1. Happy Path — Tao Campaign thanh cong (201 Created)

### 1.1 Campaign co ban (TargetType = ALL, khong co Template)

```http
POST /api/campaigns
Content-Type: application/json
```

```json
{
  "campaignName": "Flash Sale Tet 2026",
  "sourceType": "ADMIN",
  "targetType": "ALL",
  "titleOverride": "Flash Sale Tet - Giam den 50%",
  "messageOverride": "Nhanh tay mua sam truoc Tet voi hang ngan uu dai!",
  "imageUrl": "https://example.com/images/flash-sale-tet.jpg",
  "actionType": "NAVIGATE",
  "actionTarget": "https://toystore.vn/flash-sale",
  "createdByAccountId": 1,
  "targets": []
}
```

**Expected:** `201 Created` — Response body chua CampaignDto voi `status = "Draft"`.

---

### 1.2 Campaign voi Template va ScheduledAt

```json
{
  "campaignName": "Welcome New User 2026",
  "templateCode": "PROMO_ALERT",
  "sourceType": "SYSTEM",
  "targetType": "ALL",
  "scheduledAt": "2026-05-15T10:00:00Z",
  "createdByAccountId": 1,
  "targets": []
}
```

**Expected:** `201 Created` — `templateCode = "WELCOME_NEW_USER"`, `scheduledAt` co gia tri.

> **Luu y:** `templateCode` phai ton tai trong bang Templates. Neu chua co, tao Template truoc.

---

### 1.3 Campaign voi TargetType = SEGMENT va danh sach Targets

```json
{
  "campaignName": "VIP Customer Promotion",
  "sourceType": "ADMIN",
  "targetType": "SEGMENT",
  "titleOverride": "Uu dai danh rieng cho ban!",
  "messageOverride": "Cam on ban da la khach hang VIP. Nhan uu dai 30% ngay!",
  "createdByAccountId": 1,
  "targets": [
    { "targetType": "ACCOUNT_ID", "targetValue": "1" },
    { "targetType": "ACCOUNT_ID", "targetValue": "2" },
    { "targetType": "ROLE", "targetValue": "Customer" }
  ]
}
```

**Expected:** `201 Created` — `targets` co 3 items, moi item co `campaignTargetId > 0`.

---

## 2. Validation Error (400 Bad Request)

### 2.1 CampaignName rong

```json
{
  "campaignName": "",
  "sourceType": "ADMIN",
  "targetType": "ALL",
  "createdByAccountId": 1,
  "targets": []
}
```

**Expected:** `400` — Error: `"Campaign name is required."`

---

### 2.2 CampaignName qua ngan

```json
{
  "campaignName": "AB",
  "sourceType": "ADMIN",
  "targetType": "ALL",
  "createdByAccountId": 1,
  "targets": []
}
```

**Expected:** `400` — Error: `"Campaign name must be at least 3 characters."`

---

### 2.3 SourceType khong hop le

```json
{
  "campaignName": "Test Invalid Source",
  "sourceType": "EMAIL",
  "targetType": "ALL",
  "createdByAccountId": 1,
  "targets": []
}
```

**Expected:** `400` — Error: `"Source type must be one of: ADMIN, SYSTEM."`

---

### 2.4 TargetType = SEGMENT nhung khong co Targets

```json
{
  "campaignName": "Segment Without Targets",
  "sourceType": "ADMIN",
  "targetType": "SEGMENT",
  "createdByAccountId": 1,
  "targets": []
}
```

**Expected:** `400` — Error: `"At least one target is required when target type is SEGMENT."`

---

### 2.5 TargetType = ALL nhung co Targets

```json
{
  "campaignName": "All With Targets",
  "sourceType": "ADMIN",
  "targetType": "ALL",
  "createdByAccountId": 1,
  "targets": [
    { "targetType": "ACCOUNT_ID", "targetValue": "1" }
  ]
}
```

**Expected:** `400` — Error: `"Targets must be empty when target type is ALL."`

---

### 2.6 ScheduledAt trong qua khu

```json
{
  "campaignName": "Past Schedule",
  "sourceType": "ADMIN",
  "targetType": "ALL",
  "scheduledAt": "2020-01-01T00:00:00Z",
  "createdByAccountId": 1,
  "targets": []
}
```

**Expected:** `400` — Error: `"Scheduled time must not be in the past."`

---

### 2.7 ImageUrl khong hop le

```json
{
  "campaignName": "Invalid Image URL",
  "sourceType": "ADMIN",
  "targetType": "ALL",
  "imageUrl": "not-a-valid-url",
  "createdByAccountId": 1,
  "targets": []
}
```

**Expected:** `400` — Error: `"Image URL is not a valid URL."`

---

### 2.8 CreatedByAccountId = 0

```json
{
  "campaignName": "No Creator",
  "sourceType": "ADMIN",
  "targetType": "ALL",
  "createdByAccountId": 0,
  "targets": []
}
```

**Expected:** `400` — Error: `"Created by account ID must be greater than 0."`

---

## 3. Conflict (409)

### 3.1 Trung ten Campaign

**Buoc 1:** Tao campaign "Flash Sale Tet 2026" (xem case 1.1)
**Buoc 2:** Tao lai voi cung ten:

```json
{
  "campaignName": "Flash Sale Tet 2026",
  "sourceType": "ADMIN",
  "targetType": "ALL",
  "createdByAccountId": 1,
  "targets": []
}
```

**Expected:** `409 Conflict` — `"Campaign name already exists."`

---

## 4. Not Found (404)

### 4.1 TemplateCode khong ton tai

```json
{
  "campaignName": "Non Existent Template",
  "templateCode": "NONEXISTENT_CODE",
  "sourceType": "ADMIN",
  "targetType": "ALL",
  "createdByAccountId": 1,
  "targets": []
}
```

**Expected:** `404 Not Found` — `"Template with ID 'NONEXISTENT_CODE' was not found."`

---

## 5. Kiem tra Database

Sau khi tao thanh cong, kiem tra:

```sql
-- Kiem tra Campaign vua tao
SELECT TOP 5 * FROM [Notification].[Campaigns] ORDER BY CampaignID DESC;

-- Kiem tra CampaignTargets (neu co)
SELECT ct.* FROM [Notification].[CampaignTargets] ct
JOIN [Notification].[Campaigns] c ON ct.CampaignID = c.CampaignID
WHERE c.CampaignName = N'VIP Customer Promotion';
```
