# Test Plan: Customer Bank Account Management

## 1. Overview
- **Base URL:** `http://localhost:5216/api/bank-accounts`
- **Authentication:** Bearer token required (Customer role).

## 2. API Endpoints

### 2.1. GET `/api/bank-accounts`
Retrieves list of active saved bank accounts for current user. Sorted: `IsDefault DESC, LastUsedAt DESC`.

- **Headers:**
  - `Authorization: Bearer <CustomerToken>`
- **Response 200 OK:**
  ```json
  [
    {
      "savedBankAccountId": 1,
      "accountId": 10,
      "bankBin": "970415",
      "bankName": "VietinBank",
      "bankShortName": "VietinBank",
      "bankCode": "ICB",
      "accountNumber": "10111222333",
      "accountName": "NGUYEN VAN A",
      "isDefault": true,
      "lastUsedAt": null,
      "createdAt": "2026-06-11T20:00:00Z"
    }
  ]
  ```

---

### 2.2. POST `/api/bank-accounts`
Creates or restores a saved bank account.

- **Headers:**
  - `Authorization: Bearer <CustomerToken>`
- **Request Body:**
  ```json
  {
    "bankBin": "970415",
    "bankName": "VietinBank",
    "bankShortName": "VietinBank",
    "bankCode": "ICB",
    "accountNumber": "10111222333",
    "accountName": "NGUYEN VAN A",
    "isDefault": true
  }
  ```
- **Response 200 OK (restored/created record):**
  ```json
  {
    "savedBankAccountId": 1,
    "accountId": 10,
    "bankBin": "970415",
    "bankName": "VietinBank",
    "bankShortName": "VietinBank",
    "bankCode": "ICB",
    "accountNumber": "10111222333",
    "accountName": "NGUYEN VAN A",
    "isDefault": true,
    "lastUsedAt": null,
    "createdAt": "2026-06-11T20:00:00Z"
  }
  ```
- **Response 409 Conflict (duplicate record with IsDeleted=0):**
  ```json
  {
    "code": "CONFLICT",
    "message": "This bank account is already saved."
  }
  ```

---

### 2.3. PUT `/api/bank-accounts/{id}/delete`
Soft deletes a saved bank account.

- **Headers:**
  - `Authorization: Bearer <CustomerToken>`
- **Response 204 No Content:** (Success)
- **Response 400 Bad Request (pending withdrawal requests exists):**
  ```json
  {
    "code": "BUSINESS_RULE_VIOLATION",
    "message": "Cannot delete bank account as it has pending or processing withdrawal requests."
  }
  ```

---

### 2.4. GET `/api/bank-accounts/banks`
Fetches all supported banks from BankLookup proxy with cache.

- **Headers:** None required.
- **Response 200 OK:**
  ```json
  [
    {
      "code": "VCB",
      "bin": "970436",
      "short_name": "Vietcombank",
      "lookup_supported": 1
    }
  ]
  ```

---

### 2.5. GET `/api/bank-accounts/lookup`
Verifies owner name of bank account.

- **Request Query Parameters:**
  - `bankCode`: VCB
  - `accountNumber`: 0011004123456
- **Response 200 OK:**
  ```json
  "NGUYEN VAN A"
  ```
- **Response 422 Unprocessable Entity:**
  ```json
  {
    "code": "UNPROCESSABLE_ENTITY",
    "message": "The account number does not exist, or the bank does not support account lookup."
  }
  ```
