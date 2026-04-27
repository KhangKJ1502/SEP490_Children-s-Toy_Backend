# Template Management Test Guide

## Base URL

- http://localhost:5000
- https://localhost:5001

## Endpoints

- GET /api/templates
- POST /api/templates
- PUT /api/templates/{templateId}
- GET /api/templates/search

## Request Samples

### POST /api/templates

```json
{
  "templateCode": "ORDER_CREATED",
  "titleTemplate": "Order created successfully",
  "messageTemplate": "Your order has been created and is waiting for confirmation.",
  "isActive": true
}
```

### PUT /api/templates/{templateId}

```json
{
  "templateCode": "ORDER_CREATED",
  "titleTemplate": "Order created",
  "messageTemplate": "Your order was created successfully.",
  "isActive": true
}
```

## Happy Path Cases

1. GET template list with default paging returns 200 and paginated data.
2. POST template with valid payload returns 201 and created entity.
3. PUT template with valid payload returns 200 and updated entity.
4. GET template search with matched keyword returns 200 and filtered data.

## Validation Error Cases

1. POST template with empty templateCode returns 400.
2. POST template with invalid templateCode format returns 400.
3. POST template with titleTemplate longer than 255 characters returns 400.
4. POST template with messageTemplate longer than 500 characters returns 400.
5. PUT template with empty titleTemplate returns 400.
6. GET list with pageNumber < 1 or pageSize > 100 returns 400.
7. GET search without searchTerm returns 400.

## Not Found Cases

1. PUT template with non-existing templateId returns 404.

## Business/Conflict Cases

1. POST template with duplicated templateCode returns 409.
2. PUT template with duplicated templateCode returns 409.

## Database Verification

1. Verify new records in table Notification.Templates.
2. Verify IsDeleted is 0 for newly created records.
3. Verify UpdatedAt is changed after edit actions.
4. Verify IsActive is saved correctly for create and update actions.
