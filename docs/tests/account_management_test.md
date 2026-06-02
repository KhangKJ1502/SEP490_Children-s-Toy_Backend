# Account Management Test Guide

## Base URL

- http://localhost:5000
- https://localhost:5001

## Endpoints

- GET /api/accounts
- GET /api/accounts/{accountId}
- POST /api/accounts
- PUT /api/accounts/{accountId}/status

## Request Samples

### GET /api/accounts

Query string example:

```
/api/accounts?pageNumber=1&pageSize=10&sortBy=accountName&sortDesc=false&searchTerm=staff
```

### POST /api/accounts (Staff)

```json
{
  "roleId": 3,
  "accountName": "Staff Demo",
  "phoneNumber": "0912345678",
  "email": "staff.demo@toystore.local",
  "password": "StaffDemo123"
}
```

### POST /api/accounts (Merchandiser)

```json
{
  "roleId": 4,
  "accountName": "Merchandiser Demo",
  "phoneNumber": "0987654321",
  "email": "merch.demo@toystore.local",
  "password": "MerchDemo123"
}
```

### PUT /api/accounts/{accountId}/status

```json
{
  "isActive": false
}
```

## Happy Path Cases

1. GET account list with default paging returns 200 and paginated data.
2. GET account list with searchTerm returns 200 and data filtered by AccountName/PhoneNumber/Email.
3. GET account detail by valid accountId returns 200 and account detail.
4. POST account with roleId 3 returns 201 and created account with IsActive = true.
5. POST account with roleId 4 returns 201 and created account with IsActive = true.
6. POST account auto-generates EmployeeCode in format 4 digits + 2 uppercase letters.
7. PUT status with valid accountId and payload returns 200 and updates only IsActive.

## Validation Error Cases

1. POST account with roleId <= 0 returns 400.
2. POST account with roleId not in {3,4} returns 400.
3. POST account with accountName shorter than 2 characters returns 400.
4. POST account with accountName containing special characters returns 400.
5. POST account with accountName containing spaces between words returns 201.
6. POST account with accountName containing only whitespace returns 400.
7. POST account with accountName length greater than 99 returns 400.
8. POST account with invalid Vietnamese phone number format returns 400.
9. POST account with invalid email format returns 400.
10. POST account with weak password returns 400.
11. PUT status with missing isActive returns 400.
12. GET list with pageNumber < 1 or pageSize > 100 returns 400.

## Authorization Cases

1. POST account without token returns 401.
2. POST account with non-admin role token returns 403.
3. POST account with admin token returns 201.

## Not Found Cases

1. GET account detail with non-existing accountId returns 404.
2. PUT status with non-existing accountId returns 404.
3. POST account with roleId 3/4 not existing in Roles table returns 404.

## Business/Conflict Cases

1. POST account with duplicated email returns 409.
2. PUT status with unchanged value returns 200 and keeps data unchanged.

## Database Verification

1. Verify created account exists in Accounts table with expected RoleID.
2. Verify created account has IsActive = 1 and IsDeleted = 0.
3. Verify EmployeeCode is auto-generated and matches regex `^[0-9]{4}[A-Z]{2}$`.
4. Verify PasswordHash is stored and plaintext password is not stored.
5. Verify UpdatedAt changes after status update.
6. Verify GET /api/accounts response includes ImageUrl and UpdatedAt fields.
7. Verify no API delete endpoint is exposed in Swagger.
