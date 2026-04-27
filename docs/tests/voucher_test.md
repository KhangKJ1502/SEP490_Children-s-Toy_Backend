# VOUCHER FEATURE TEST GUIDE

- Feature: Voucher CRU (list voucher, search voucher, create voucher, update voucher)
- Base URL (local): `http://localhost:5000` or `https://localhost:7000`

## 1. Endpoints in scope

- GET `/api/vouchers`
- POST `/api/vouchers`
- PUT `/api/vouchers/{voucherId}`

## 2. Query parameters for list/search

- `pageNumber`: default 1, min 1
- `pageSize`: default 10, min 1, max 100
- `sortBy`: `voucherCode`, `voucherName`, `discountValue`, `startDate`, `endDate`, `status`, `createdAt`
- `sortDesc`: `true|false`
- `searchTerm`: search in code/name/description
- `status`: `Scheduled|Active|Inactive|Expired`

## 3. Request samples

### 3.1 POST - valid create request

```json
{
  "voucherCode": "SUMMER2026",
  "voucherName": "Summer Campaign 2026",
  "voucherDescription": "Discount for summer campaign",
  "discountType": "PERCENTAGE",
  "discountValue": 15,
  "maxDiscountCap": 50000,
  "discountTarget": "ORDER_TOTAL",
  "minOrderAmount": 200000,
  "totalQuantity": 1000,
  "maxUsagePerUser": 1,
  "startDate": "2026-05-01T00:00:00",
  "endDate": "2026-06-01T23:59:59",
  "status": "Scheduled"
}
```

### 3.2 PUT - valid update request

```json
{
  "voucherName": "Summer Campaign 2026 - Updated",
  "discountValue": 20,
  "status": "Active"
}
```

## 4. Happy path test cases

1. GET `/api/vouchers?pageNumber=1&pageSize=10`

- Expected: 200 OK, response type `PaginatedResponse<VoucherListDto>`

2. GET `/api/vouchers?pageNumber=1&pageSize=10&searchTerm=SUMMER`

- Expected: 200 OK, returned items match keyword by code/name/description

3. GET `/api/vouchers?pageNumber=1&pageSize=10&status=Active&sortBy=discountValue&sortDesc=true`

- Expected: 200 OK, filtered by status and correctly sorted

4. POST `/api/vouchers` with valid body

- Expected: 201 Created, returned voucher data includes generated `voucherId`

5. PUT `/api/vouchers/{voucherId}` with valid body

- Expected: 200 OK, updated fields are reflected

## 5. Validation error test cases (400)

1. POST missing required fields (`voucherCode`, `voucherName`, `discountType`, ...)

- Expected: 400 Bad Request with validation errors

2. POST with invalid date range (`startDate >= endDate`)

- Expected: 400 Bad Request

3. POST with `discountType = PERCENTAGE` and `discountValue > 100`

- Expected: 400 Bad Request

4. POST with invalid `discountTarget`

- Expected: 400 Bad Request

5. PUT with empty body `{}`

- Expected: 400 Bad Request (at least one field required)

## 6. Not found and conflict cases

1. PUT `/api/vouchers/999999` with valid payload

- Expected: 404 Not Found

2. POST create with duplicate `voucherCode`

- Expected: 409 Conflict

3. PUT update code to an existing `voucherCode`

- Expected: 409 Conflict

## 7. Database verification

After successful create/update, verify in table `[Vouchers]`:

- `VoucherCode` stored uppercase
- `IsDeleted = 0`
- `CreatedAt` set when create
- `UpdatedAt` set when update
- `UsedQuantity` stays unchanged by update endpoint

## 8. Regression checklist

1. Open Swagger and ensure existing endpoints still appear:

- `/api/health`
- `/api/products`
- `/api/orders`
- `/api/recommendations`

2. Smoke test these endpoints and confirm 200 responses remain unchanged.
