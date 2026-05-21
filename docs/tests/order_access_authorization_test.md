# Order access authorization — manual test matrix

JWT must include `accountId` and `roleId` claims. Never pass another user's ID in the body for authorization.

## Role IDs (database)

| RoleID | RoleName    | Order list scope                          |
|--------|-------------|-------------------------------------------|
| 1      | Customer    | Own orders via `/api/orders` only         |
| 2      | Admin       | All orders + order queue                  |
| 3      | Staff       | Only orders with active OA `RoleID = 3`   |
| 4      | Merchandise | Only orders with active OA `RoleID = 4`   |

## Staff (RoleID 3)

| # | Action | Setup | Expected |
|---|--------|-------|----------|
| S1 | `GET /api/admin/orders` | Staff A assigned order 1, Staff B assigned order 2 | List contains only order 1 |
| S2 | `GET /api/admin/orders/{id}` | Staff A, order 2 assigned to B | 403 FORBIDDEN |
| S3 | `PATCH .../confirm` | Staff A, order assigned to B, Pending | 403 FORBIDDEN |
| S4 | `PATCH .../confirm` | Staff A, order assigned to A, Pending | 200 |
| S5 | `GET .../tracking` | Staff A, order assigned to B | 403 FORBIDDEN |
| S6 | Queued order (no OA) | Staff A | Not in list; detail 403 |

## Merchandise (RoleID 4)

| # | Action | Setup | Expected |
|---|--------|-------|----------|
| M1 | `GET /api/admin/orders` | Merch A assigned order 1 only | List contains only order 1 |
| M2 | `PATCH .../process` | Merch A, order assigned to B | 403 FORBIDDEN |
| M3 | `PATCH .../ship` | Merch A, order assigned to A, Processing | 200 |

## Admin (RoleID 2)

| # | Action | Expected |
|---|--------|----------|
| A1 | `GET /api/admin/orders` | All orders (subject to status/payment filters) |
| A2 | `GET /api/order-queue` | 200 |
| A3 | `GET /api/admin/dashboard/revenue` | 200 |
| A4 | `PATCH .../assign` | Updates `OrderAssignments` (+ legacy `AssignedToStaffID` for staff role) |

## Reassignment

| # | Action | Expected |
|---|--------|----------|
| R1 | Admin reassigns Staff on order from A to B | Staff A loses access; B gains access |
| R2 | Mark absent on schedule | Deactivated OA → former assignee 403 on detail |

## Admin FE note

Default admin order list for Staff/Merchandise is **server-filtered to assigned orders**. The `assignedToMe` query flag is no longer required for security (optional for UI only).
