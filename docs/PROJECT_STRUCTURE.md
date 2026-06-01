# 📚 ToyStore Backend - Cấu Trúc Dự Án

## Tổng Quan Kiến Trúc

```
┌─────────────────────────────────────────────────────────────────┐
│                         ToyStore.API                             │
│                    (Presentation Layer)                          │
│              Controllers, Middleware, Extensions                 │
└──────────────────────────┬──────────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────────┐
│                    ToyStore.Application                          │
│                    (Business Logic Layer)                        │
│         DTOs, Interfaces, Validators, Common Helpers             │
└──────────────────────────┬──────────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────────┐
│                    ToyStore.Infrastructure                       │
│                    (Data Access Layer)                           │
│            Repositories, Services, DbContext, Migrations         │
└──────────────────────────┬──────────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────────┐
│                      ToyStore.Domain                             │
│                      (Core Layer)                                │
│                  Entities, Enums, Value Objects                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 1. ToyStore.Domain (Core Layer)

**Mục đích:** Chứa các Entity và Enum - là "trái tim" của ứng dụng.

**Không phụ thuộc vào bất kỳ layer nào khác.**

### Cấu trúc thư mục:
```
ToyStore.Domain/
├── Entities/
│   ├── Entity.cs              # Base class cho tất cả entity
│   ├── Product.cs             # Thông tin sản phẩm
│   ├── Category.cs            # Danh mục sản phẩm
│   ├── Order.cs               # Đơn hàng
│   ├── OrderItem.cs           # Chi tiết đơn hàng
│   ├── User.cs                # Người dùng
│   ├── UserBehavior.cs        # Hành vi người dùng (cho AI)
│   └── PaymentWebhook.cs      # Log thanh toán
│
└── Enums/
    ├── OrderStatus.cs         # Pending, Confirmed, Shipping, Delivered, Cancelled
    ├── PaymentMethod.cs       # COD, VNPay, Momo, ZaloPay
    ├── ToyCategory.cs         # Educational, Creative, Outdoor, Electronic...
    ├── AgeRange.cs            # Baby, Toddler, Preschool, Kid, Teen
    └── BehaviorType.cs        # View, AddToCart, Purchase, Rate...
```

### Chức năng chính:
| File | Chức năng |
|------|-----------|
| `Entity.cs` | Base class với Id (Guid), Equals, GetHashCode |
| `Product.cs` | Thông tin sản phẩm: Name, Price, Stock, SKU, Slug... |
| `Order.cs` | Đơn hàng: OrderNumber, Status, TotalAmount, Address... |
| `UserBehavior.cs` | Tracking hành vi để đề xuất sản phẩm |

---

## 2. ToyStore.Application (Business Logic Layer)

**Mục đích:** Chứa logic nghiệp vụ, DTOs, Interfaces, Validators.

**Phụ thuộc:** ToyStore.Domain

### Cấu trúc thư mục:
```
ToyStore.Application/
├── DTOs/
│   ├── ProductDtos.cs         # ProductDto, CreateProductDto, UpdateProductDto
│   ├── OrderDtos.cs           # OrderDto, CreateOrderDto, OrderListDto
│   ├── RecommendationDtos.cs  # RecommendationDto, TrackBehaviorDto
│   └── PaginatedResponse.cs   # Response phân trang
│
├── Interfaces/
│   ├── Repositories/
│   │   ├── IProductRepository.cs      # CRUD + query products
│   │   ├── IOrderRepository.cs        # CRUD + query orders
│   │   ├── IUserBehaviorRepository.cs # Tracking behaviors
│   │   └── IUnitOfWork.cs             # Transaction management
│   │
│   └── Services/
│       ├── IProductService.cs         # Business logic products
│       ├── IOrderService.cs           # Business logic orders
│       ├── IWebhookService.cs         # Payment webhooks
│       ├── IRecommendationService.cs  # AI recommendations
│       └── IChatbotService.cs         # Chatbot
│
├── Validators/
│   ├── CreateProductValidator.cs  # Validate tạo sản phẩm
│   ├── CreateOrderValidator.cs    # Validate tạo đơn hàng
│   └── CommonValidators.cs        # Email, Phone, Password...
│
└── Common/
    ├── Helpers/
    │   ├── StringHelper.cs        # GenerateSlug, RemoveVietnameseAccents
    │   ├── DateTimeHelper.cs      # Vietnam timezone, RelativeTime
    │   └── MoneyHelper.cs         # FormatVND, ToReadableAmount
    │
    ├── Extensions/
    │   ├── StringExtensions.cs    # IsNullOrEmpty, IsValidPhone
    │   └── EnumerableExtensions.cs # ForEach, Batch, Shuffle
    │
    ├── Exceptions/
    │   └── ApplicationExceptions.cs # NotFoundException, ValidationException...
    │
    └── Models/
        ├── Result.cs              # Result pattern cho error handling
        └── ResultExtensions.cs    # Map, OnSuccess, OnFailure
```

### Chức năng chính:
| Thư mục | Chức năng |
|---------|-----------|
| `DTOs/` | Data Transfer Objects - định nghĩa dữ liệu trao đổi giữa API và Client |
| `Interfaces/` | Contracts cho Repositories và Services |
| `Validators/` | Kiểm tra dữ liệu đầu vào với error messages tiếng Việt |
| `Common/` | Utilities dùng chung: helpers, extensions, exceptions |

---

## 3. ToyStore.Infrastructure (Data Access Layer)

**Mục đích:** Triển khai các interfaces, kết nối database, external services.

**Phụ thuộc:** ToyStore.Domain, ToyStore.Application

### Cấu trúc thư mục:
```
ToyStore.Infrastructure/
├── Data/
│   ├── ToyStoreDbContext.cs           # EF Core DbContext
│   ├── Configurations/
│   │   └── EntityConfigurations.cs    # Fluent API config
│   └── Migrations/                    # Database migrations
│
├── Repositories/
│   ├── ProductRepository.cs           # Implement IProductRepository
│   ├── OrderRepository.cs             # Implement IOrderRepository
│   ├── UserBehaviorRepository.cs      # Implement IUserBehaviorRepository
│   └── UnitOfWork.cs                  # Implement IUnitOfWork
│
├── Services/
│   ├── ProductService.cs              # Business logic products
│   ├── OrderService.cs                # Business logic orders
│   └── WebhookService.cs              # Payment webhook handling
│
└── DependencyInjection.cs             # Register services với DI
```

### Chức năng chính:
| File | Chức năng |
|------|-----------|
| `ToyStoreDbContext.cs` | Cấu hình Entity Framework, DbSets |
| `ProductRepository.cs` | CRUD products, query by slug/category/price |
| `OrderRepository.cs` | CRUD orders, query by status/user/date |
| `UnitOfWork.cs` | Quản lý transaction, coordinate repositories |
| `ProductService.cs` | Validate, create, update products với business rules |
| `OrderService.cs` | Tạo order, quản lý stock, xử lý thanh toán |
| `DependencyInjection.cs` | `AddInfrastructure()` - register tất cả services |

---

## 4. ToyStore.API (Presentation Layer)

**Mục đích:** REST API endpoints, middleware, authentication.

**Phụ thuộc:** ToyStore.Application, ToyStore.Infrastructure

### Cấu trúc thư mục:
```
ToyStore.API/
├── Controllers/
│   ├── ProductsController.cs      # GET/POST/PUT/DELETE products
│   ├── OrdersController.cs        # GET/POST orders
│   ├── RecommendationsController.cs # AI suggestions
│   └── ChatbotController.cs       # Chatbot API
│
├── Extensions/
│   └── ResultToActionResultExtensions.cs # Convert Result to ActionResult
│
├── appsettings.json               # Configuration
├── appsettings.Development.json   # Dev config
├── appsettings.Production.json    # Production config
└── Program.cs                     # Application startup
```

### Chức năng chính:
| File | Chức năng |
|------|-----------|
| `ProductsController.cs` | API endpoints: `/api/products` |
| `OrdersController.cs` | API endpoints: `/api/orders` |
| `Program.cs` | Cấu hình DI, middleware, Swagger |

---

## 5. ToyStore.Recommendation (AI Module)

**Mục đích:** Đề xuất sản phẩm dựa trên hành vi người dùng.

**Phụ thuộc:** ToyStore.Domain, ToyStore.Application

### Cấu trúc thư mục:
```
ToyStore.Recommendation/
├── Services/
│   └── RecommendationService.cs   # Implement IRecommendationService
│
├── Models/
│   ├── RecommendationScore.cs     # Điểm đề xuất
│   └── ScoreComponents.cs         # Thành phần tính điểm
│
└── DependencyInjection.cs         # Register recommendation services
```

### Chức năng chính:
| Method | Chức năng |
|--------|-----------|
| `GetPersonalizedRecommendationsAsync()` | Đề xuất theo lịch sử user |
| `GetSimilarProductsAsync()` | Sản phẩm tương tự |
| `GetFrequentlyBoughtTogetherAsync()` | Thường mua cùng |
| `TrackBehaviorAsync()` | Ghi nhận hành vi |

---

## 6. ToyStore.Chatbot (AI Module)

**Mục đích:** Chatbot hỗ trợ khách hàng tự động.

**Phụ thuộc:** ToyStore.Domain, ToyStore.Application

### Cấu trúc thư mục:
```
ToyStore.Chatbot/
├── Services/
│   └── ChatbotService.cs          # Implement IChatbotService
│
├── Models/
│   ├── ChatIntent.cs              # Intent detection
│   └── ChatResponse.cs            # Response templates
│
└── DependencyInjection.cs
```

### Chức năng chính:
| Method | Chức năng |
|--------|-----------|
| `ProcessMessageAsync()` | Xử lý tin nhắn từ user |
| `GetProductSuggestionsAsync()` | Gợi ý sản phẩm qua chat |

---

## 7. ToyStore.Worker (Background Jobs)

**Mục đích:** Xử lý tác vụ nền: cập nhật order, gửi email...

**Phụ thuộc:** ToyStore.Application, ToyStore.Infrastructure

### Cấu trúc thư mục:
```
ToyStore.Worker/
├── Workers/
│   ├── OrderStatusWorker.cs       # Auto-update order status
│   └── RecommendationWorker.cs    # Recalculate recommendations
│
└── Program.cs
```

### Chức năng chính:
| Worker | Chức năng |
|--------|-----------|
| `OrderStatusWorker` | Tự động confirm order đã thanh toán, hủy order timeout |
| `RecommendationWorker` | Cập nhật điểm đề xuất định kỳ |

---

## Dependency Flow

```
             ┌─────────────┐
             │   Domain    │  ◀── Không phụ thuộc gì
             └──────┬──────┘
                    │
             ┌──────▼──────┐
             │ Application │  ◀── Chỉ phụ thuộc Domain
             └──────┬──────┘
                    │
        ┌───────────┼───────────┐
        │           │           │
 ┌──────▼──────┐ ┌──▼───┐ ┌─────▼─────┐
 │Infrastructure│ │ Rec  │ │  Chatbot  │  ◀── Implement interfaces
 └──────┬──────┘ └──────┘ └───────────┘
        │
 ┌──────▼──────┐
 │     API     │  ◀── Presentation, startup
 └─────────────┘
        │
 ┌──────▼──────┐
 │   Worker    │  ◀── Background jobs
 └─────────────┘
```

---

## NuGet Packages

| Project | Package | Mục đích |
|---------|---------|----------|
| Domain | - | Không có dependency |
| Application | - | Không có dependency |
| Infrastructure | `Microsoft.EntityFrameworkCore.SqlServer` | SQL Server provider |
| Infrastructure | `Microsoft.EntityFrameworkCore.Tools` | Migrations |
| API | `Swashbuckle.AspNetCore` | Swagger UI |
| All | `Microsoft.Extensions.DependencyInjection` | Dependency Injection |

---

## Luồng Transaction (Unit of Work)

### Transaction là gì?
Transaction đảm bảo **nhiều thao tác** phải **cùng thành công hoặc cùng thất bại**. Nếu 1 thao tác lỗi → tất cả sẽ bị hủy (rollback).

### Khi nào cần Transaction?

| ✅ CẦN Transaction | ❌ KHÔNG CẦN Transaction |
|-------------------|-------------------------|
| Tạo Order + Giảm Stock | Xem sản phẩm |
| Hủy Order + Hoàn Stock | Tìm kiếm |
| Thanh toán + Update Order | Cập nhật profile |
| Chuyển tiền A → B | Thêm sản phẩm mới |

### Luồng Transaction khi tạo Order:

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. CLIENT gửi request POST /api/orders                          │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. CONTROLLER nhận request                                       │
│    OrdersController.CreateOrder(CreateOrderDto dto)              │
│    └──▶ Gọi _orderService.CreateAsync(dto)                      │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ 3. SERVICE bắt đầu Transaction                                   │
│                                                                  │
│    await _unitOfWork.BeginTransactionAsync();  ◀── BẮT ĐẦU      │
│    try {                                                         │
│        // Bước 1: Validate đầu vào                               │
│        var errors = CreateOrderValidator.Validate(dto);          │
│        if (!errors.IsValid) throw ValidationException;           │
│                                                                  │
│        // Bước 2: Kiểm tra stock từng sản phẩm                   │
│        foreach (var item in dto.Items) {                         │
│            var product = await _unitOfWork.Products              │
│                .GetByIdAsync(item.ProductId);                    │
│            if (product.StockQuantity < item.Quantity)            │
│                throw BusinessRuleException("Không đủ hàng");     │
│        }                                                         │
│                                                                  │
│        // Bước 3: Tạo Order                                      │
│        var order = new Order { ... };                            │
│        await _unitOfWork.Orders.AddAsync(order);                 │
│                                                                  │
│        // Bước 4: Giảm stock từng sản phẩm                       │
│        foreach (var item in dto.Items) {                         │
│            product.StockQuantity -= item.Quantity;               │
│            _unitOfWork.Products.Update(product);                 │
│        }                                                         │
│                                                                  │
│        // Bước 5: Lưu tất cả thay đổi                            │
│        await _unitOfWork.SaveChangesAsync();                     │
│                                                                  │
│        // Bước 6: Commit transaction                             │
│        await _unitOfWork.CommitTransactionAsync(); ◀── HOÀN TẤT │
│    }                                                             │
│    catch {                                                       │
│        await _unitOfWork.RollbackTransactionAsync(); ◀── HỦY BỎ │
│        throw;                                                    │
│    }                                                             │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ 4. REPOSITORY thao tác với DbContext                             │
│                                                                  │
│    Orders.AddAsync(order)    ──▶  _dbSet.AddAsync(order)        │
│    Products.Update(product)  ──▶  _dbSet.Update(product)        │
│                                                                  │
│    Tất cả dùng CHUNG 1 DbContext instance                       │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ 5. DATABASE thực thi SQL                                         │
│                                                                  │
│    BEGIN TRANSACTION                                             │
│        INSERT INTO Orders (Id, OrderNumber, ...) VALUES (...)    │
│        INSERT INTO OrderItems (OrderId, ProductId, ...) VALUES   │
│        UPDATE Products SET StockQuantity = 95 WHERE Id = ...     │
│        UPDATE Products SET StockQuantity = 48 WHERE Id = ...     │
│    COMMIT TRANSACTION  ──▶ ✅ Thành công!                        │
│                                                                  │
│    (Nếu lỗi: ROLLBACK ──▶ ❌ Hủy tất cả, DB không thay đổi gì)  │
└─────────────────────────────────────────────────────────────────┘
```

### Ví dụ lỗi và Rollback:

```
Tình huống: Tạo order với 2 sản phẩm
- Sản phẩm A: Còn 10, mua 5  ✅
- Sản phẩm B: Còn 2, mua 5   ❌ (không đủ)

Kết quả:
┌────────────────────────────────────────┐
│ BEGIN TRANSACTION                      │
│   INSERT Order ✅                      │
│   UPDATE Product A (stock: 10→5) ✅    │
│   UPDATE Product B (stock: 2→-3) ❌    │
│   ──▶ LỖI! Không đủ stock             │
│ ROLLBACK TRANSACTION                   │
│   ──▶ Order bị hủy                    │
│   ──▶ Product A stock vẫn = 10        │
│   ──▶ Database KHÔNG thay đổi gì!     │
└────────────────────────────────────────┘
```

### Code thực tế trong OrderService:

```csharp
public async Task<OrderDto> CreateAsync(CreateOrderDto dto, CancellationToken ct)
{
    // Bắt đầu transaction
    await _unitOfWork.BeginTransactionAsync(ct);
    
    try
    {
        // 1. Validate
        var validation = CreateOrderValidator.Validate(dto);
        if (!validation.IsValid)
            throw new ValidationException(validation.ToErrorDictionary());
        
        // 2. Tạo order
        var order = new Order { OrderNumber = GenerateOrderNumber(), ... };
        await _unitOfWork.Orders.AddAsync(order, ct);
        
        // 3. Xử lý từng sản phẩm
        foreach (var item in dto.Items)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId, ct);
            
            // Kiểm tra stock
            if (product.StockQuantity < item.Quantity)
                throw new BusinessRuleException("INSUFFICIENT_STOCK", 
                    $"Sản phẩm {product.Name} chỉ còn {product.StockQuantity}");
            
            // Giảm stock
            product.StockQuantity -= item.Quantity;
            _unitOfWork.Products.Update(product);
            
            // Thêm order item
            order.Items.Add(new OrderItem { ProductId = product.Id, ... });
        }
        
        // 4. Lưu tất cả
        await _unitOfWork.SaveChangesAsync(ct);
        
        // 5. Commit - xác nhận thành công
        await _unitOfWork.CommitTransactionAsync(ct);
        
        return MapToDto(order);
    }
    catch
    {
        // Có lỗi → Rollback tất cả
        await _unitOfWork.RollbackTransactionAsync(ct);
        throw;
    }
}
```

### Tóm tắt:

| Bước | Hành động | Nếu lỗi |
|------|-----------|---------|
| `BeginTransactionAsync()` | Bắt đầu transaction | - |
| `AddAsync()`, `Update()` | Thao tác trong memory | Chưa ảnh hưởng DB |
| `SaveChangesAsync()` | Gửi SQL xuống DB | Rollback |
| `CommitTransactionAsync()` | Xác nhận thành công | Rollback |
| `RollbackTransactionAsync()` | Hủy tất cả thay đổi | - |

---

## Cách chạy Project

### Development:
```bash
# Chạy database (Docker)
docker-compose -f docker-compose.dev.yml up -d

# Chạy API
dotnet run --project ToyStore.API

# Truy cập Swagger
http://localhost:5000/swagger
```

### Production (Docker):
```bash
# Chạy tất cả services
docker-compose up -d

# API: http://localhost:5000
```
