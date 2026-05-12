# Manage Refund API Tests

This document outlines the test cases and API endpoints for the Manage Refund module.

## 1. Customer Endpoints

### 1.1 Create Refund (Customer)
- **Endpoint**: `POST /api/refunds`
- **Role**: `Customer`
- **Request Body**:
```json
{
  "orderId": 123,
  "refundReasonId": 1,
  "reasonDetails": "Sản phẩm bị lỗi kỹ thuật",
  "images": [
    "https://example.com/image1.jpg",
    "https://example.com/image2.jpg"
  ]
}
```
- **Test Cases**:
  - `201 Created`: Order is delivered within 7 days, max 2 requests.
  - `400 Bad Request`: Order is not delivered or >7 days since delivery.
  - `400 Bad Request`: Already has active refund request.

### 1.2 Get Refunds List (Customer)
- **Endpoint**: `GET /api/refunds?page=1&pageSize=10`
- **Role**: `Customer`
- **Test Cases**:
  - `200 OK`: Returns paginated list of refunds for the current customer.

### 1.3 Get Refund Details (Customer)
- **Endpoint**: `GET /api/refunds/{refundId}`
- **Role**: `Customer`
- **Test Cases**:
  - `200 OK`: Returns refund details including images.
  - `404 Not Found`: Refund does not exist or belongs to another customer.

### 1.4 Cancel Refund (Customer)
- **Endpoint**: `POST /api/refunds/{refundId}/cancel`
- **Role**: `Customer`
- **Test Cases**:
  - `200 OK`: Successfully cancels a "Requested" refund.
  - `400 Bad Request`: Attempting to cancel an "Approved" or "Completed" refund.

---

## 2. Admin Endpoints

### 2.1 Create Refund on behalf of Customer (Admin/Staff)
- **Endpoint**: `POST /api/admin/refunds`
- **Role**: `Admin`, `Staff`
- **Request Body**:
```json
{
  "orderId": 123,
  "refundReasonId": 2,
  "reasonDetails": "Khách hàng khiếu nại qua điện thoại, đồng ý hoàn tiền",
  "images": []
}
```
- **Test Cases**:
  - `201 Created`: Successfully creates refund (auto-approved and completed side effects).
  - `400 Bad Request`: Order is in an invalid status (e.g. Cancelled).

### 2.2 Get Refunds List (Admin)
- **Endpoint**: `GET /api/admin/refunds?page=1&pageSize=10&refundStatus=Requested`
- **Role**: `Admin`, `Staff`
- **Test Cases**:
  - `200 OK`: Returns all refunds with optional filters (customerId, orderId, refundStatus, etc).

### 2.3 Get Refund Details (Admin)
- **Endpoint**: `GET /api/admin/refunds/{refundId}`
- **Role**: `Admin`, `Staff`
- **Test Cases**:
  - `200 OK`: Returns refund details.

### 2.4 Update Refund Status (Admin)
- **Endpoint**: `PATCH /api/admin/refunds/{refundId}/status`
- **Role**: `Admin`, `Staff`
- **Request Body (Approve)**:
```json
{
  "status": "Approved"
}
```
- **Request Body (Reject)**:
```json
{
  "status": "Rejected",
  "rejectReason": "Lý do không hợp lệ"
}
```
- **Request Body (Complete)**:
```json
{
  "status": "Completed"
}
```
- **Test Cases**:
  - `200 OK`: Successful transition `Requested -> Approved`, `Approved -> Completed`, or `Requested/Approved -> Rejected`.
  - `400 Bad Request`: Invalid transition (e.g. `Completed -> Requested`).
