# Test Guide: Product Review Like Feature

This document describes how to test and verify the Product Review Like feature on the backend API.

## Base URL
- Local Dev API Base: `http://localhost:5000/api` or `https://localhost:7001/api`

---

## 1. Happy Path: Toggle Like/Unlike on a Review

### Test Case 1: First Like from a Customer
- **Endpoint**: `POST /api/reviews/{reviewId}/like`
- **Authentication**: JWT Token with role `Customer`
- **Sample Request**:
  - URL: `POST http://localhost:5000/api/reviews/1/like`
  - Headers:
    - `Authorization: Bearer <Customer_JWT_Token>`
- **Expected Success Response (200 OK)**:
```json
{
  "isSuccess": true,
  "message": "Operation completed successfully.",
  "data": {
    "reviewId": 1,
    "likeCount": 1,
    "isLiked": true
  }
}
```

### Test Case 2: Unlike (Toggle Again)
- **Endpoint**: `POST /api/reviews/{reviewId}/like`
- **Authentication**: JWT Token with role `Customer` (Same user as Test Case 1)
- **Sample Request**:
  - URL: `POST http://localhost:5000/api/reviews/1/like`
  - Headers:
    - `Authorization: Bearer <Customer_JWT_Token>`
- **Expected Success Response (200 OK)**:
```json
{
  "isSuccess": true,
  "message": "Operation completed successfully.",
  "data": {
    "reviewId": 1,
    "likeCount": 0,
    "isLiked": false
  }
}
```

---

## 2. Error Cases

### Test Case 3: Guest Attempts to Like
- **Endpoint**: `POST /api/reviews/{reviewId}/like`
- **Authentication**: None (Guest)
- **Sample Request**:
  - URL: `POST http://localhost:5000/api/reviews/1/like`
- **Expected Response (401 Unauthorized)**:
```json
{
  "isSuccess": false,
  "message": "Unauthorized access."
}
```

### Test Case 4: Liking a Non-Existent Review
- **Endpoint**: `POST /api/reviews/{reviewId}/like`
- **Authentication**: JWT Token with role `Customer`
- **Sample Request**:
  - URL: `POST http://localhost:5000/api/reviews/999999/like`
  - Headers:
    - `Authorization: Bearer <Customer_JWT_Token>`
- **Expected Response (404 Not Found)**:
```json
{
  "isSuccess": false,
  "message": "Review with ID 999999 was not found."
}
```

---

## 3. Database Verification

To verify that reactions are tracked and soft-deleted correctly, run the following SQL queries in SSMS:

```sql
-- Check reactions for a review
SELECT rp.[ReactionProductID]
      ,rp.[ReviewProductID]
      ,rp.[AccountID]
      ,rt.[Code] AS ReactionType
      ,rp.[IsDeleted]
      ,rp.[CreatedAt]
      ,rp.[UpdatedAt]
  FROM [dbo].[ReviewProductReactions] rp
  JOIN [dbo].[ReactionTypes] rt ON rp.[ReactionTypeID] = rt.[ReactionTypeID]
  WHERE rp.[ReviewProductID] = 1;
```
