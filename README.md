# ToyStore BackEnd

ASP.NET Core 8 backend cho hệ thống bán đồ chơi — SEP490 Capstone Project.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 8 Web API |
| ORM | Entity Framework Core 8 (DB First) |
| Database | SQL Server (SEP409_ToyStore) |
| Auth | JWT Bearer |
| Architecture | Clean Architecture |

---

## Cài đặt lần đầu (Clone project về)

### 1. Restore packages
```bash
dotnet restore ToyStore.sln
```

### 2. Tạo DB từ schema
Mở SSMS → kết nối SQL Server → chạy file:
```
docs/database/schema.sql
```

### 3. Cấu hình connection string
Sửa `ToyStore.API/appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=SEP409_ToyStore;User ID=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
  }
}
```

### 4. Chạy project
```bash
dotnet run --project ToyStore.API
```

Swagger: `https://localhost:7xxx/swagger`

---

## DB First — Đồng bộ Database khi làm nhóm

> ⚠️ **Đây là bước QUAN TRỌNG nhất khi làm nhóm.**

### Khi bạn thay đổi DB (thêm bảng/cột)

1. Sửa DB trên **SSMS local** của bạn
2. Viết file SQL: `docs/database/changes/YYYYMMDD_HHMM_MoTa.sql`
3. Chạy lại scaffold để sync C# models:
```powershell
dotnet ef dbcontext scaffold "Server=DESKTOP-T27O90D\SQLEXPRESS;Database=SEP409_ToyStore;User ID=sa;Password=khangmc1502@;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer --project ToyStore.Infrastructure --startup-project ToyStore.API --output-dir Models --context-dir Data --context SEP490ToyStoreContext --no-onconfiguring --force
```
4. Commit **TẤT CẢ** (SQL script + C# models + docs)
5. Push + thông báo nhóm chat

### Khi pull code về có DB thay đổi

```bash
git pull
# Mở SSMS → chạy file SQL mới trong docs/database/changes/ theo thứ tự ngày
dotnet build ToyStore.sln  # verify OK
```

📖 **Hướng dẫn đầy đủ:** [`docs/database/changes/README.md`](docs/database/changes/README.md)

---

## Cấu trúc project

```
ToyStore.API/           → Controllers, Middleware, Program.cs
ToyStore.Application/   → DTOs, Interfaces, Validators, Common/Result pattern
ToyStore.Infrastructure/
  ├── Data/
  │   └── SEP490ToyStoreContext.cs   ← DbContext (DO NOT edit directly)
  ├── Models/                         ← Scaffolded entities (DO NOT edit directly)
  ├── Repositories/                   ← Repository implementations
  ├── Services/                       ← Service implementations
  └── DependencyInjection.cs
ToyStore.Domain/        → (Legacy — chỉ còn Common helpers)
ToyStore.Recommendation/→ Recommendation engine
ToyStore.Chatbot/       → Chatbot service
ToyStore.Worker/        → Background workers (order auto-cancel, etc.)
docs/
  ├── database/
  │   ├── schema.sql        ← Full DB schema (chạy lần đầu)
  │   ├── erd.md            ← Entity Relationship Diagram
  │   ├── CHANGELOG.md      ← Lịch sử thay đổi schema
  │   └── changes/          ← SQL incremental scripts (đồng bộ nhóm)
  └── tests/                ← Test guides cho từng feature
```

---

## Quy tắc code

📖 Xem [`CODING_RULES.md`](CODING_RULES.md) — **Bắt buộc đọc trước khi code.**

Tóm tắt nhanh:
- **KHÔNG** sửa trực tiếp `Models/*.cs` hoặc `SEP490ToyStoreContext.cs`
- **KHÔNG** dùng `dotnet ef migrations add` — project dùng DB First
- **PHẢI** viết SQL change script khi thay đổi DB
- **PHẢI** cập nhật `docs/database/erd.md` và `CHANGELOG.md` khi thay đổi schema

---

## Git Commit Convention

```
feat(auth): implement JWT login
fix(order): handle null payment status  
chore(db): AddWeightToProducts — scaffold + SQL change script
docs(readme): update setup guide
refactor(product): extract category filter
```