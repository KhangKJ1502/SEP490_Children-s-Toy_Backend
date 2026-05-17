# Campaign Approval Workflow — API Test Guide

> Rule 7.1 compliant test document.

## Base URL

```
https://localhost:7xxx/api/campaigns
```

---

## Workflow Overview

```
Draft → (POST /submit) → PendingApproval → (POST /review Approved) → Approved → (POST /schedule) → Scheduled
                                         → (POST /review Rejected) → Rejected  → (PUT update) → Draft → submit again
```

---

## 1. Create Campaign (Draft)

**POST** `/api/campaigns`

```json
{
  "campaignName": "Summer Sale 2026",
  "templateCode": "PROMO_GENERAL",
  "sourceType": "ADMIN",
  "targetType": "ALL",
  "titleOverride": "Big Summer Sale!",
  "messageOverride": "Up to 50% off all toys this summer!",
  "createdByAccountId": 1
}
```

| Case | Expected |
|------|----------|
| Valid request | `201 Created` — Campaign created with `status: "Draft"` |
| Missing `campaignName` | `400 Bad Request` — Validation error |
| Duplicate name | `409 Conflict` |

---

## 2. Submit for Review

**POST** `/api/campaigns/{id}/submit`

> Requires JWT token (Staff or Admin). No request body.

| Case | Expected |
|------|----------|
| Campaign in Draft | `204 No Content` — Status → `PendingApproval` |
| Campaign NOT in Draft (e.g. Approved) | `409 Conflict` — "Only Draft campaigns can be submitted" |
| Campaign not found | `404 Not Found` |
| Not authenticated | `401 Unauthorized` |

**DB Check:**
```sql
SELECT Status, SubmittedByAccountID, SubmittedAt FROM Notification.Campaigns WHERE CampaignID = ?;
SELECT * FROM Notification.CampaignApprovalLogs WHERE CampaignID = ? ORDER BY CreatedAt DESC;
```

---

## 3. Review Campaign (Admin)

**POST** `/api/campaigns/{id}/review`

> Requires JWT token. In production, should be restricted to Admin role.

### 3a. Approve

```json
{
  "action": "Approved",
  "reviewNote": null
}
```

| Case | Expected |
|------|----------|
| Campaign in PendingApproval + action=Approved | `204 No Content` — Status → `Approved` |
| Campaign NOT in PendingApproval | `409 Conflict` |
| Invalid action value | `400 Bad Request` |

### 3b. Reject

```json
{
  "action": "Rejected",
  "reviewNote": "Content needs revision — please update the message."
}
```

| Case | Expected |
|------|----------|
| Campaign in PendingApproval + action=Rejected + reviewNote | `204 No Content` — Status → `Rejected` |
| action=Rejected WITHOUT reviewNote | `400 Bad Request` — "Review note is required when rejecting" |
| reviewNote exceeds 500 chars | `400 Bad Request` |

**DB Check:**
```sql
SELECT Status, ReviewedByAccountID, ReviewedAt, ReviewNote FROM Notification.Campaigns WHERE CampaignID = ?;
SELECT Action, Note, CreatedAt FROM Notification.CampaignApprovalLogs WHERE CampaignID = ? ORDER BY CreatedAt DESC;
```

---

## 4. Schedule Campaign

**POST** `/api/campaigns/{id}/schedule`

> Requires JWT token. Campaign must be in `Approved` status.

```json
{
  "scheduledAt": "2026-06-01T08:00:00Z"
}
```

Or to send immediately (null):
```json
{
  "scheduledAt": null
}
```

| Case | Expected |
|------|----------|
| Campaign in Approved + future scheduledAt | `204 No Content` — Status → `Scheduled` |
| **Campaign NOT in Approved (e.g. Draft)** | **`409 Conflict` — "Campaign has not been approved by Admin yet"** |
| scheduledAt in the past | `400 Bad Request` — "Scheduled time must not be in the past" |
| Campaign already has a schedule | `409 Conflict` — "Campaign already has a schedule" |
| Campaign not found | `404 Not Found` |

**DB Check:**
```sql
SELECT Status FROM Notification.Campaigns WHERE CampaignID = ?;
SELECT ScheduledAt, ExecutionStatus, ScheduledBy FROM Notification.CampaignSchedules WHERE CampaignID = ?;
SELECT Action, Note FROM Notification.CampaignApprovalLogs WHERE CampaignID = ? ORDER BY CreatedAt DESC;
```

---

## 5. Full Happy Path Sequence

```
1. POST /api/campaigns               → 201, Status=Draft
2. POST /api/campaigns/{id}/submit   → 204, Status=PendingApproval
3. POST /api/campaigns/{id}/review   { action:"Approved" } → 204, Status=Approved
4. POST /api/campaigns/{id}/schedule { scheduledAt:"2026-06-01T08:00:00Z" } → 204, Status=Scheduled
   (Background job picks up and sends → Status=Sending → Sent)
```

---

## 6. Reject & Re-submit Sequence

```
1. POST /api/campaigns               → 201, Status=Draft
2. POST /api/campaigns/{id}/submit   → 204, Status=PendingApproval
3. POST /api/campaigns/{id}/review   { action:"Rejected", reviewNote:"Fix message" } → 204, Status=Rejected
4. PUT  /api/campaigns/{id}          (edit) → 200, Status=Draft (auto-reset from Rejected)
5. POST /api/campaigns/{id}/submit   → 204, Status=PendingApproval (again)
6. POST /api/campaigns/{id}/review   { action:"Approved" } → 204, Status=Approved
7. POST /api/campaigns/{id}/schedule → 204, Status=Scheduled
```

---

## 7. Regression Tests

| Feature | Endpoint | Expected |
|---------|----------|----------|
| Get campaigns list | `GET /api/campaigns?status=PendingApproval` | Returns campaigns in new status |
| Get campaigns list | `GET /api/campaigns?status=Approved` | Returns approved campaigns |
| Get by ID | `GET /api/campaigns/{id}` | Returns full Campaign DTO including SubmittedAt, ReviewedAt, ReviewNote |
| Cancel Draft | `POST /api/campaigns/{id}/cancel` | 204, Status=Cancelled |
| Cancel PendingApproval | `POST /api/campaigns/{id}/cancel` | 204, Status=Cancelled |
| Cancel Sent | `POST /api/campaigns/{id}/cancel` | 409, cannot cancel |
| Worker: Scheduled→Sending→Sent | Background job (no API call) | Status transitions automatically |
