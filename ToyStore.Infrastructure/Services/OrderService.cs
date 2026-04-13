using ToyStore.Application.Common.Exceptions;
using ToyStore.Application.DTOs;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Application.Validators;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Enums;

namespace ToyStore.Infrastructure.Services;

/// <summary>
/// Order service implementation with validation and error handling.
/// </summary>
public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    
    public OrderService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }
    
    public async Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ValidationException("Id", "ID đơn hàng không hợp lệ.");
            
        var order = await _unitOfWork.Orders.GetWithItemsAsync(id, cancellationToken);
        return order != null ? MapToDto(order) : null;
    }
    
    public async Task<OrderDto?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            throw new ValidationException("OrderNumber", "Mã đơn hàng không được để trống.");
            
        var order = await _unitOfWork.Orders.GetByOrderNumberAsync(orderNumber, cancellationToken);
        return order != null ? MapToDto(order) : null;
    }
    
    public async Task<IReadOnlyList<OrderListDto>> GetUserOrdersAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ValidationException("UserId", "ID người dùng không hợp lệ.");
            
        var orders = await _unitOfWork.Orders.GetByUserIdAsync(userId, cancellationToken);
        return orders.Select(MapToListDto).ToList();
    }
    
    public async Task<PaginatedResponse<OrderListDto>> GetOrdersAsync(
        int pageNumber = 1,
        int pageSize = 20,
        Guid? userId = null,
        OrderStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        // Validate pagination
        if (pageNumber < 1)
            throw new ValidationException("PageNumber", "Số trang phải lớn hơn hoặc bằng 1.");
        if (pageSize < 1 || pageSize > 100)
            throw new ValidationException("PageSize", "Kích thước trang phải từ 1 đến 100.");
        if (startDate.HasValue && endDate.HasValue && startDate > endDate)
            throw new ValidationException("Date", "Ngày bắt đầu phải nhỏ hơn ngày kết thúc.");
            
        var (orders, totalCount) = await _unitOfWork.Orders.GetPagedAsync(
            pageNumber, pageSize, userId, status, startDate, endDate, searchTerm, cancellationToken);
            
        var items = orders.Select(MapToListDto).ToList();
        return new PaginatedResponse<OrderListDto>(items, totalCount, pageNumber, pageSize);
    }
    
    public async Task<OrderDto> CreateAsync(CreateOrderDto dto, CancellationToken cancellationToken = default)
    {
        // Validate input
        var validationResult = CreateOrderValidator.Validate(dto);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.ToErrorDictionary());
        
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var order = new Order
            {
                OrderNumber = Order.GenerateOrderNumber(),
                UserId = dto.UserId,
                Status = OrderStatus.Pending,
                OrderDate = DateTime.UtcNow,
                ShippingAddress = dto.ShippingAddress.Trim(),
                BillingAddress = dto.BillingAddress?.Trim() ?? dto.ShippingAddress.Trim(),
                RecipientName = dto.RecipientName.Trim(),
                RecipientPhone = dto.RecipientPhone.Trim(),
                Notes = dto.Notes?.Trim(),
                PaymentMethod = dto.PaymentMethod,
                PaymentStatus = "Pending",
                DiscountCode = dto.DiscountCode?.Trim()
            };
            
            decimal subTotal = 0;
            var outOfStockProducts = new List<string>();
            var notFoundProducts = new List<Guid>();
            
            // Validate all products first
            foreach (var item in dto.Items)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId, cancellationToken);
                
                if (product == null)
                {
                    notFoundProducts.Add(item.ProductId);
                    continue;
                }
                
                if (!product.IsActive)
                    throw new BusinessRuleException($"Sản phẩm '{product.Name}' hiện không còn bán.");
                    
                if (product.StockQuantity < item.Quantity)
                    outOfStockProducts.Add($"{product.Name} (còn {product.StockQuantity}, yêu cầu {item.Quantity})");
            }
            
            // Throw aggregated errors
            if (notFoundProducts.Any())
                throw new NotFoundException("Products", string.Join(", ", notFoundProducts));
                
            if (outOfStockProducts.Any())
                throw new BusinessRuleException(
                    "INSUFFICIENT_STOCK", 
                    $"Không đủ hàng trong kho: {string.Join("; ", outOfStockProducts)}");
            
            // Create order items
            foreach (var item in dto.Items)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId, cancellationToken);
                
                var orderItem = new OrderItem
                {
                    ProductId = product!.Id,
                    ProductName = product.Name,
                    ProductSKU = product.SKU,
                    ProductImageUrl = product.ImageUrl,
                    Quantity = item.Quantity,
                    UnitPrice = product.EffectivePrice,
                    DiscountAmount = 0
                };
                orderItem.CalculateTotal();
                
                order.Items.Add(orderItem);
                subTotal += orderItem.TotalPrice;
                
                // Reserve stock
                product.StockQuantity -= item.Quantity;
                product.PurchaseCount += item.Quantity;
                _unitOfWork.Products.Update(product);
            }
            
            order.SubTotal = subTotal;
            order.ShippingCost = CalculateShippingCost(subTotal);
            order.TaxAmount = 0; // VAT included in price
            order.TotalAmount = order.SubTotal + order.ShippingCost - order.DiscountAmount;
            
            // Validate minimum order amount
            if (order.TotalAmount < 50000)
                throw new BusinessRuleException("Giá trị đơn hàng tối thiểu là 50.000 VND.");
            
            await _unitOfWork.Orders.AddAsync(order, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            
            return MapToDto(order);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
    
    public async Task<OrderDto?> UpdateStatusAsync(
        Guid id, UpdateOrderStatusDto dto, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ValidationException("Id", "ID đơn hàng không hợp lệ.");
            
        var order = await _unitOfWork.Orders.GetByIdAsync(id, cancellationToken);
        if (order == null)
            throw NotFoundException.For<Order>(id);
        
        // Validate status transition
        ValidateStatusTransition(order.Status, dto.Status);
        
        order.Status = dto.Status;
        
        if (!string.IsNullOrEmpty(dto.TrackingNumber))
            order.TrackingNumber = dto.TrackingNumber.Trim();
        if (!string.IsNullOrEmpty(dto.ShippingCarrier))
            order.ShippingCarrier = dto.ShippingCarrier.Trim();
        if (!string.IsNullOrEmpty(dto.Notes))
            order.Notes = dto.Notes.Trim();
            
        if (dto.Status == OrderStatus.Shipped)
            order.EstimatedDeliveryDate = DateTime.UtcNow.AddDays(3);
        if (dto.Status == OrderStatus.Delivered)
            order.DeliveredAt = DateTime.UtcNow;
        
        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return MapToDto(order);
    }
    
    public async Task<bool> CancelAsync(Guid id, string reason, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ValidationException("Id", "ID đơn hàng không hợp lệ.");
            
        if (string.IsNullOrWhiteSpace(reason))
            throw new ValidationException("Reason", "Vui lòng nhập lý do hủy đơn.");
            
        var order = await _unitOfWork.Orders.GetWithItemsAsync(id, cancellationToken);
        if (order == null)
            throw NotFoundException.For<Order>(id);
        
        // Only allow cancellation for certain statuses
        var cancellableStatuses = new[] { OrderStatus.Pending, OrderStatus.Confirmed };
        if (!cancellableStatuses.Contains(order.Status))
            throw new BusinessRuleException(
                "CANNOT_CANCEL",
                $"Không thể hủy đơn hàng ở trạng thái '{order.Status}'. Chỉ có thể hủy đơn hàng đang chờ xử lý hoặc đã xác nhận.");
        
        order.Status = OrderStatus.Cancelled;
        order.CancelledAt = DateTime.UtcNow;
        order.CancellationReason = reason.Trim();
        
        // Restore stock
        foreach (var item in order.Items)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId, cancellationToken);
            if (product != null)
            {
                product.StockQuantity += item.Quantity;
                _unitOfWork.Products.Update(product);
            }
        }
        
        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return true;
    }
    
    public async Task<bool> ProcessPaymentSuccessAsync(
        Guid orderId, string transactionId, CancellationToken cancellationToken = default)
    {
        if (orderId == Guid.Empty)
            throw new ValidationException("OrderId", "ID đơn hàng không hợp lệ.");
            
        if (string.IsNullOrWhiteSpace(transactionId))
            throw new ValidationException("TransactionId", "Mã giao dịch không hợp lệ.");
            
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId, cancellationToken);
        if (order == null)
            throw NotFoundException.For<Order>(orderId);
        
        if (order.PaymentStatus == "Paid")
            throw new BusinessRuleException("Đơn hàng đã được thanh toán trước đó.");
            
        if (order.Status == OrderStatus.Cancelled)
            throw new BusinessRuleException("Không thể thanh toán cho đơn hàng đã hủy.");
        
        order.PaymentStatus = "Paid";
        order.PaymentTransactionId = transactionId.Trim();
        order.PaidAt = DateTime.UtcNow;
        order.Status = OrderStatus.Confirmed;
        
        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return true;
    }
    
    #region Private Methods
    
    private static void ValidateStatusTransition(OrderStatus currentStatus, OrderStatus newStatus)
    {
        var validTransitions = new Dictionary<OrderStatus, OrderStatus[]>
        {
            { OrderStatus.Pending, new[] { OrderStatus.Confirmed, OrderStatus.Cancelled } },
            { OrderStatus.Confirmed, new[] { OrderStatus.Processing, OrderStatus.Cancelled } },
            { OrderStatus.Processing, new[] { OrderStatus.Shipped, OrderStatus.Cancelled } },
            { OrderStatus.Shipped, new[] { OrderStatus.Delivered } },
            { OrderStatus.Delivered, Array.Empty<OrderStatus>() },
            { OrderStatus.Cancelled, Array.Empty<OrderStatus>() }
        };
        
        if (!validTransitions.TryGetValue(currentStatus, out var allowedStatuses))
            throw new BusinessRuleException($"Trạng thái đơn hàng '{currentStatus}' không hợp lệ.");
            
        if (!allowedStatuses.Contains(newStatus))
            throw new BusinessRuleException(
                "INVALID_STATUS_TRANSITION",
                $"Không thể chuyển trạng thái từ '{currentStatus}' sang '{newStatus}'.");
    }
    
    private static decimal CalculateShippingCost(decimal subTotal)
    {
        // Free shipping for orders over 500,000 VND
        return subTotal >= 500000 ? 0 : 30000;
    }
    
    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            UserId = order.UserId,
            CustomerName = order.User?.FullName ?? order.RecipientName,
            CustomerEmail = order.User?.Email ?? "",
            Status = order.Status,
            OrderDate = order.OrderDate,
            SubTotal = order.SubTotal,
            DiscountAmount = order.DiscountAmount,
            DiscountCode = order.DiscountCode,
            ShippingCost = order.ShippingCost,
            TaxAmount = order.TaxAmount,
            TotalAmount = order.TotalAmount,
            Currency = order.Currency,
            ShippingAddress = order.ShippingAddress,
            RecipientName = order.RecipientName,
            RecipientPhone = order.RecipientPhone,
            Notes = order.Notes,
            PaymentMethod = order.PaymentMethod,
            PaymentStatus = order.PaymentStatus,
            TrackingNumber = order.TrackingNumber,
            EstimatedDeliveryDate = order.EstimatedDeliveryDate,
            Items = order.Items.Select(MapItemToDto).ToList(),
            CreatedAt = order.CreatedAt
        };
    }
    
    private static OrderListDto MapToListDto(Order order)
    {
        return new OrderListDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerName = order.User?.FullName ?? order.RecipientName,
            Status = order.Status,
            OrderDate = order.OrderDate,
            TotalAmount = order.TotalAmount,
            ItemCount = order.Items.Count,
            PaymentStatus = order.PaymentStatus
        };
    }
    
    private static OrderItemDto MapItemToDto(OrderItem item)
    {
        return new OrderItemDto
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            ProductSKU = item.ProductSKU,
            ProductImageUrl = item.ProductImageUrl,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            DiscountAmount = item.DiscountAmount,
            TotalPrice = item.TotalPrice
        };
    }
    
    #endregion
}
