# Category Management Test Guide

## Base URL

- http://localhost:5000
- https://localhost:5001

## Endpoints

- GET /api/categories/super-categories
- POST /api/categories/super-categories
- PUT /api/categories/super-categories/{superCategoryId}
- GET /api/categories/super-categories/search
- GET /api/categories
- POST /api/categories
- PUT /api/categories/{categoryId}
- GET /api/categories/search

## Request Samples

### POST /api/categories/super-categories

```json
{
  "superCategoryName": "Educational Toys"
}
```

### POST /api/categories

```json
{
  "superCategoryId": 1,
  "categoryName": "Building Blocks"
}
```

### PUT /api/categories/super-categories/{superCategoryId}

```json
{
  "superCategoryName": "STEM Toys"
}
```

### PUT /api/categories/{categoryId}

```json
{
  "superCategoryId": 1,
  "categoryName": "Science Kits"
}
```

## Happy Path Cases

1. GET super category list with default paging returns 200 and paginated data.
2. POST super category with valid name returns 201 and created entity.
3. GET category list with default paging returns 200 and paginated data.
4. POST category with valid payload returns 201 and created entity.
5. PUT super category with valid payload returns 200 and updated entity.
6. PUT category with valid payload returns 200 and updated entity.
7. GET super category search with matched keyword returns 200 and filtered data.
8. GET category search with matched keyword returns 200 and filtered data.

## Validation Error Cases

1. POST super category with empty superCategoryName returns 400.
2. POST super category with superCategoryName longer than 25 characters returns 400.
3. POST category with superCategoryId <= 0 returns 400.
4. POST category with empty categoryName returns 400.
5. GET list with pageNumber < 1 or pageSize > 100 returns 400.
6. PUT super category with empty superCategoryName returns 400.
7. PUT category with superCategoryId <= 0 returns 400.
8. GET search without searchTerm returns 400.

## Not Found Cases

1. POST category with non-existing superCategoryId returns 404.
2. PUT super category with non-existing superCategoryId returns 404.
3. PUT category with non-existing categoryId returns 404.
4. PUT category with non-existing superCategoryId returns 404.

## Business/Conflict Cases

1. POST super category with duplicated name returns 409.
2. POST category with duplicated name returns 409.
3. PUT super category with duplicated name returns 409.
4. PUT category with duplicated name returns 409.

## Database Verification

1. Verify new super categories in table SuperCategories.
2. Verify new categories in table Categories.
3. Verify IsDeleted is 0 for newly created records.
4. Verify UpdatedAt is changed after edit actions.
