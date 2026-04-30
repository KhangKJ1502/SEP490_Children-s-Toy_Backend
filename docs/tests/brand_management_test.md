# Brand Management Test Guide

## Base URL

- http://localhost:5000
- https://localhost:5001

## Endpoints

- GET /api/brands
- POST /api/brands
- PUT /api/brands/{brandId}

## Request Samples

### POST /api/brands

```json
{
  "brandName": "LEGO"
}
```

### PUT /api/brands/{brandId}

```json
{
  "brandName": "LEGO Education",
  "status": "Active"
}
```

## Happy Path Cases

1. GET brand list with default paging returns 200 and paginated data.
2. POST brand with valid payload returns 201 and created entity.
3. PUT brand with valid payload returns 200 and updated entity.
4. GET brand search with matched keyword returns 200 and filtered data.
5. GET/PUT response includes `status` (Active/Inactive) and `updatedAt`.

## Validation Error Cases

1. POST brand with empty brandName returns 400.
2. POST brand with brandName longer than 100 characters returns 400.
3. PUT brand with empty brandName returns 400.
4. PUT brand with invalid status (not Active/Inactive) returns 400.
5. GET list with pageNumber < 1 or pageSize > 100 returns 400.
6. GET search without searchTerm returns 400.

## Not Found Cases

1. PUT brand with non-existing brandId returns 404.

## Business/Conflict Cases

1. POST brand with duplicated name returns 409.
2. PUT brand with duplicated name returns 409.

## Database Verification

1. Verify new records in table Brands.
2. Verify IsDeleted is 0 for newly created records.
3. Verify Status maps to `IsDeleted` (`Active` => 0, `Inactive` => 1).
4. Verify UpdatedAt is changed when BrandName or Status is changed.
