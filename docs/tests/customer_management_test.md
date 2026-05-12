# Customer Management Test Guide

## Base URL

- http://localhost:5000
- https://localhost:5001

## Endpoints

- GET /api/customers
- GET /api/customers/{accountId}
- PUT /api/customers/{accountId}

## Request Samples

### GET /api/customers

Query string example:

```
/api/customers?pageNumber=1&pageSize=10&sortBy=createdAt&sortDesc=true&searchTerm=minh
```

### PUT /api/customers/{accountId}

Admin may only change customer account status (active/inactive). Personal profile fields are not accepted.

```json
{
  "isActive": false
}
```

## Happy Path Cases

1. GET customer list returns 200 and paginated data where all items have role Customer.
2. GET customer list with searchTerm returns 200 and filters by AccountName/PhoneNumber/Email.
3. GET customer detail by valid customer accountId returns 200.
4. PUT customer with valid `isActive` returns 200 and updates only `IsActive` (and `UpdatedAt`).
5. PUT customer with same `isActive` as current returns 200 and returns current detail unchanged.
6. PUT customer does not change AccountName, PhoneNumber, Email, DOB, SexId, or ImageURL.

## Validation Error Cases

1. GET list with pageNumber < 1 returns 400.
2. GET list with pageSize < 1 or > 100 returns 400.
3. PUT with missing or null `isActive` returns 400.
4. Extra properties in body are ignored by binder; only `isActive` is applied.

## Not Found Cases

1. GET detail with non-existing accountId returns 404.
2. GET detail with accountId that is not customer role returns 404.
3. PUT update with non-existing accountId returns 404.
4. PUT update with accountId that is not customer role returns 404.

## Conflict/Business Cases

1. No conflict rules specific to status toggle beyond validation and not-found.

## Database Verification

1. Verify customer row in Accounts table has RoleID = 1 after PUT.
2. Verify only `IsActive` and `UpdatedAt` change after PUT; profile columns unchanged.
3. Verify no staff/merchandiser/admin account appears in GET /api/customers.
