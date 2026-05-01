# Campaign Image Upload API — Test Guide

## Base URL

```
https://localhost:{PORT}/api/campaigns/upload-image
```

---

## Endpoint

### POST /api/campaigns/upload-image — Upload Campaign Image

**Content-Type:** `multipart/form-data`

**Form Data:**

| Field | Type | Required | Description                            |
| ----- | ---- | -------- | -------------------------------------- |
| file  | File | Yes      | Image file (jpg, jpeg, png, webp, gif) |

**Happy path (200):**

```http
POST /api/campaigns/upload-image
```

Expected response:

```json
{
  "url": "https://res.cloudinary.com/..."
}
```

**Validation Error (400) — missing file:**

```http
POST /api/campaigns/upload-image
```

Expected response:

```json
{
  "message": "No file was provided."
}
```

**Validation Error (400) — invalid file type:**

Upload a file with an extension not in `jpg, jpeg, png, webp, gif`.

Expected response:

```json
{
  "message": "Invalid file type. Allowed types: jpg, jpeg, png, gif, webp."
}
```

**Validation Error (400) — file too large:**

Upload a file larger than 5MB.

Expected response:

```json
{
  "message": "File size exceeds the 5MB limit."
}
```

---

## Notes

- The upload uses Cloudinary and returns a public HTTPS URL.
- Use the returned `url` as `imageUrl` when creating or updating a campaign.
