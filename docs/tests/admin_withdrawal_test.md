# Test Guide - Admin Withdrawal History Management

Guide to verify the Admin Withdrawal History Management feature on both Backend and Frontend.

## 1. Backend API Testing

- **Base URL:** `http://localhost:5216/api/admin/withdrawals` (or your local backend port)
- **Authorization Required:** User must be logged in as `Admin` (Role ID: 2). Add the JWT token in `Authorization: Bearer <token>` header.

### Endpoints

#### 1.1. Get Paginated and Filtered Withdrawal Requests
- **HTTP Method:** `GET`
- **Path:** `/api/admin/withdrawals`
- **Query Parameters:**
  - `page` (int, optional, default: 1): Current page.
  - `pageSize` (int, optional, default: 10): Items per page.
  - `keyword` (string, optional): Search by Customer Name or Request Code (`ReferenceId`).
  - `status` (string, optional): Filter by status (`PENDING`, `PROCESSING`, `SUCCESS`, `FAILED`, `CANCELLED`).
  - `dateFrom` (DateTime, optional): Start date range (e.g. `2026-06-01T00:00:00Z`).
  - `dateTo` (DateTime, optional): End date range (e.g. `2026-06-14T23:59:59Z`).

- **Example Request:**
  ```http
  GET /api/admin/withdrawals?page=1&pageSize=10&status=SUCCESS&keyword=Nguyen HTTP/1.1
  Host: localhost:5216
  Authorization: Bearer <JWT_TOKEN>
  ```

- **Example Response (200 OK):**
  ```json
  {
    "items": [
      {
        "withdrawalId": 12,
        "referenceId": "WDR-1234567890",
        "amount": 50000.0,
        "toBankBin": "970415",
        "toBankName": "VietinBank",
        "toAccountNumber": "10987654321",
        "toAccountName": "NGUYEN VAN A",
        "status": "SUCCESS",
        "createdAt": "2026-06-11T15:35:00Z",
        "accountId": 3,
        "customerName": "Nguyen Van A",
        "customerEmail": "nguyena@example.com",
        "customerPhone": "0987654321"
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 1,
    "totalCount": 1,
    "hasPreviousPage": false,
    "hasNextPage": false
  }
  ```

#### 1.2. Get Withdrawal Details
- **HTTP Method:** `GET`
- **Path:** `/api/admin/withdrawals/{id}`
- **Example Request:**
  ```http
  GET /api/admin/withdrawals/12 HTTP/1.1
  Host: localhost:5216
  Authorization: Bearer <JWT_TOKEN>
  ```

- **Example Response (200 OK):**
  ```json
  {
    "withdrawalId": 12,
    "walletId": 3,
    "accountId": 3,
    "walletTransactionId": 45,
    "referenceId": "WDR-1234567890",
    "amount": 50000.0,
    "toBankBin": "970415",
    "toBankName": "VietinBank",
    "toAccountNumber": "10987654321",
    "toAccountName": "NGUYEN VAN A",
    "payosPayoutId": "pout-99128",
    "payosTransactionId": "txn-99128",
    "status": "SUCCESS",
    "failReason": null,
    "retryCount": 0,
    "processingAt": "2026-06-11T15:36:00Z",
    "completedAt": "2026-06-11T15:37:00Z",
    "cancelledAt": null,
    "createdAt": "2026-06-11T15:35:00Z",
    "customerName": "Nguyen Van A",
    "customerEmail": "nguyena@example.com",
    "customerPhone": "0987654321",
    "statusHistory": [
      {
        "historyId": 22,
        "fromStatus": null,
        "toStatus": "PENDING",
        "source": "USER",
        "note": "Withdrawal requested",
        "createdAt": "2026-06-11T15:35:00Z"
      },
      {
        "historyId": 23,
        "fromStatus": "PENDING",
        "toStatus": "PROCESSING",
        "source": "SYSTEM",
        "note": "Sent payout request to PayOS",
        "createdAt": "2026-06-11T15:36:00Z"
      },
      {
        "historyId": 24,
        "fromStatus": "PROCESSING",
        "toStatus": "SUCCESS",
        "source": "WEBHOOK",
        "note": "Payout completed successfully via PayOS",
        "createdAt": "2026-06-11T15:37:00Z"
      }
    ]
  }
  ```

### Common Error Cases

- **401 Unauthorized:** Request is made without a token or with an invalid token.
- **403 Forbidden:** User has a token but does not have the `Admin` role (e.g. `Staff` or `Merchandise`).
- **404 Not Found:** Requesting `/api/admin/withdrawals/{id}` for an ID that does not exist.
  ```json
  {
    "code": "NOT_FOUND",
    "message": "WithdrawalRequest with ID 99999 was not found."
  }
  ```

---

## 2. Frontend Verification

### 2.1. Authorization Checks
1. Log in with a **Staff** or **Merchandise** account.
2. Attempt to navigate directly to `http://localhost:3000/admin/withdrawals`.
3. Verify that you are blocked or redirected, and the `Withdrawal` link is NOT visible in the left sidebar under "Wallet Management".
4. Log in with an **Admin** account.
5. Verify that the "Withdrawal" sub-menu is visible and you can access the page.

### 2.2. Functional Checks on `/admin/withdrawals` Page
1. **Withdrawal Requests List:** Verify that the requests are listed inside a table showing Request Code, Customer Name, Amount (formatted in VND), Bank Details, Status (colored Badges), and Created At.
2. **Search:** Type a customer name (e.g., "Nguyen") or a request code in the search bar and press **Enter**. Verify that the list updates.
3. **Status Filter:** Select a status from the status dropdown (e.g., "SUCCESS" or "PENDING"). Verify that the list is filtered.
4. **Date Filter:** Choose a start date and an end date using the calendar pickers. Verify that only requests within the range are displayed.
5. **Clear Filters:** Click the "Clear Filters" button and verify that all filters (keyword, status, date pickers) reset to their defaults.
6. **Pagination:** Change "Rows per page" or select page numbers and verify that it updates the list accordingly.
7. **Detail Modal:** Click on any withdrawal request row or its eye icon button:
   - A modal must open.
   - Verify that all detailed user info, bank info, and payout IDs/transaction IDs are present.
   - Check the transition timeline displaying the audit logs chronologically.
