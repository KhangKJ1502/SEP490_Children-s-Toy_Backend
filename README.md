# ToyStore Backend

SEP490_Children's Toy_Backend là hệ thống backend cho nền tảng thương mại điện tử đồ chơi trẻ em, được phát triển bằng ASP.NET Core (.NET 8) theo kiến trúc Clean Architecture.

## 🚀 Quick Start với Docker

### Yêu cầu

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (cho development)

### Chạy Production

```bash
# Start all services (API + SQL Server + Worker)
deploy.bat up

# Hoặc dùng docker-compose trực tiếp
docker-compose up -d
```

**Truy cập:**

- API: http://localhost:5000
- Swagger: http://localhost:5000/swagger

### Chạy Development

```bash
# Chỉ chạy SQL Server và Redis
deploy.bat dev

# Sau đó chạy API locally
dotnet run --project ToyStore.API
```

## 📁 Cấu trúc dự án

```
ToyStore/
├── ToyStore.API/              # Web API Layer
├── ToyStore.Application/      # Business Logic Layer
│   ├── Common/
│   │   ├── Helpers/          # StringHelper, DateTimeHelper, MoneyHelper
│   │   ├── Extensions/       # Extension methods
│   │   ├── Exceptions/       # Custom exceptions
│   │   └── Models/           # Result pattern
│   ├── DTOs/                 # Data Transfer Objects
│   ├── Interfaces/
│   │   ├── Repositories/     # Repository interfaces
│   │   └── Services/         # Service interfaces
│   └── Validators/           # Input validators
├── ToyStore.Domain/          # Domain Entities & Enums
├── ToyStore.Infrastructure/  # Data Access & External Services
├── ToyStore.Recommendation/  # AI Recommendation Engine
├── ToyStore.Chatbot/         # Chatbot Service
└── ToyStore.Worker/          # Background Jobs
```

## 🔧 Lệnh Docker

| Lệnh               | Mô tả                           |
| ------------------ | ------------------------------- |
| `deploy.bat up`    | Khởi động tất cả services       |
| `deploy.bat down`  | Dừng tất cả services            |
| `deploy.bat build` | Build lại images                |
| `deploy.bat logs`  | Xem logs                        |
| `deploy.bat dev`   | Chạy môi trường dev (chỉ DB)    |
| `deploy.bat clean` | Xóa tất cả containers và images |

## 📝 Database Migrations

```bash
# Tạo migration
dotnet ef migrations add Initial --project ToyStore.Infrastructure --startup-project ToyStore.API

# Apply migration
dotnet ef database update --project ToyStore.Infrastructure --startup-project ToyStore.API
```

## 🔐 Environment Variables

| Variable                               | Mô tả                                | Default     |
| -------------------------------------- | ------------------------------------ | ----------- |
| `ConnectionStrings__DefaultConnection` | SQL Server connection string         | -           |
| `ASPNETCORE_ENVIRONMENT`               | Environment (Development/Production) | Development |
| `Jwt__Secret`                          | JWT signing key                      | -           |

## 👥 Team

- SEP490 - Capstone Project
- SEP490 - Capstone Project
  > > > > > > > a798b56 (chore: init project with Clean Architecture rules, AI config files, and test templates)
