# Role API — Test Guide

## Base URL

```
https://localhost:{PORT}/api/roles
```

---

## Endpoints

### 1. GET /api/roles — View Role List

**Happy path (200):**

```http
GET /api/roles
```

Expected response:

```json
[
  {
    "roleId": 1,
    "roleName": "Admin",
    "description": "System administrator"
  }
]
```

**Authorization Error (401):**

```http
GET /api/roles
```

Expected response:

```json
{
  "message": "Unauthorized"
}
```

**Forbidden (403) — invalid role:**

Use token without required roles.

Expected response:

```json
{
  "message": "Forbidden"
}
```
