using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToyStore.Application.Common.Helpers;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Checkouts;
using ToyStore.Application.DTOs.Orders;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Constants;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Options;

namespace ToyStore.Infrastructure.Services;

/// <summary>
/// Xu ly toan bo nghiep vu quan ly don hang phia admin.
/// Moi transition la 1 method rieng, khong dung method chung UpdateStatus.
/// </summary>
public class AdminOrderService : IAdminOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;
    private readonly IGhnClient _ghnClient;
    private readonly GhnOptions _ghnOptions;
    private readonly ShopAddressOptions _shopAddress;
    private readonly IValidator<ShipOrderRequestDto> _shipValidator;
    private readonly IValidator<CancelOrderRequestDto> _cancelValidator;
    private readonly IValidator<AssignOrderRequestDto> _assignValidator;
    private readonly IValidator<ConfirmOrderRequestDto> _confirmValidator;
    private readonly IValidator<ProcessOrderRequestDto> _processValidator;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ILogger<AdminOrderService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly IOrderLifecycleService _orderLifecycle;
    private readonly IOrderAccessService _orderAccess;

    // Role names khop voi ClaimTypes.Role trong JWT
    private const string RoleStaff = "Staff";
    private const string RoleMerchandise = "Merchandise";
    private const string RoleAdmin = "Admin";

    public AdminOrderService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IMapper mapper,
        IGhnClient ghnClient,
        IOptions<GhnOptions> ghnOptions,
        IOptions<ShopAddressOptions> shopAddress,
        IValidator<ShipOrderRequestDto> shipValidator,
        IValidator<CancelOrderRequestDto> cancelValidator,
        IValidator<AssignOrderRequestDto> assignValidator,
        IValidator<ConfirmOrderRequestDto> confirmValidator,
        IValidator<ProcessOrderRequestDto> processValidator,
        IDomainEventPublisher eventPublisher,
        ILogger<AdminOrderService> logger,
        ITimeProvider timeProvider,
        IOrderLifecycleService orderLifecycle,
        IOrderAccessService orderAccess)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _mapper = mapper;
        _ghnClient = ghnClient;
        _ghnOptions = ghnOptions.Value;
        _shopAddress = shopAddress.Value;
        _shipValidator = shipValidator;
        _cancelValidator = cancelValidator;
        _assignValidator = assignValidator;
        _confirmValidator = confirmValidator;
        _processValidator = processValidator;
        _eventPublisher = eventPublisher;
        _logger = logger;
        _timeProvider = timeProvider;
        _orderLifecycle = orderLifecycle;
        _orderAccess = orderAccess;
    }



    // ── UC1: Danh sach don hang ───────────────────────────────────────────────

    public async Task<Result<PaginatedResponse<AdminOrderListItemDto>>> GetListAsync(
        AdminOrderQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var isPrivileged = _orderAccess.IsPrivileged(_currentUser.RoleId);
        var restrictToAssignment = !isPrivileged || query.AssignedToMe;
        var assignmentRoleId = _orderAccess.GetRequiredAssignmentRoleId(_currentUser.RoleId);

        var allowedStatuses = isPrivileged && !query.AssignedToMe
            ? OrderStatuses.AdminVisibleStatuses
            : OrderStatuses.AdminVisibleStatuses;

        var pageSize = Math.Min(query.PageSize, 100);
        var pageNumber = Math.Max(query.PageNumber, 1);

        var statusIds = query.StatusIds is { Count: > 0 }
            ? (IReadOnlyCollection<int>?)query.StatusIds
            : null;

        var assignmentScope = restrictToAssignment ? query.AssignmentScope : null;

        var items = await _unitOfWork.Orders.GetAdminPagedAsync(
            allowedStatuses,
            pageNumber,
            pageSize,
            query.StatusId,
            statusIds,
            restrictToAssignment,
            _currentUser.AccountId,
            assignmentRoleId,
            assignmentScope,
            query.Keyword,
            query.FromDate,
            query.ToDate,
            cancellationToken);

        var count = await _unitOfWork.Orders.CountAdminAsync(
            allowedStatuses,
            query.StatusId,
            statusIds,
            restrictToAssignment,
            _currentUser.AccountId,
            assignmentRoleId,
            assignmentScope,
            query.Keyword,
            query.FromDate,
            query.ToDate,
            cancellationToken);

        var dtos = _mapper.Map<List<AdminOrderListItemDto>>(items);

        if (dtos.Count > 0)
        {
            var orderIds = dtos.Select(x => x.OrderId).ToList();
            var activeAssignments = await _unitOfWork.OrderAssignments.GetActiveAssignmentsForOrdersAsync(orderIds, cancellationToken);

            for (var i = 0; i < dtos.Count; i++)
            {
                ApplyAssignmentNamesToDto(
                    dtos[i],
                    items[i],
                    activeAssignments.Where(a => a.OrderId == dtos[i].OrderId));
            }
        }

        return Result<PaginatedResponse<AdminOrderListItemDto>>.Success(
            new PaginatedResponse<AdminOrderListItemDto>(dtos, count, pageNumber, pageSize));
    }

    // ── UC2: Chi tiet don hang ────────────────────────────────────────────────

    public async Task<Result<AdminOrderDetailDto>> GetDetailAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var access = await _orderAccess.EnsureCanViewAsync(orderId, cancellationToken);
        if (access.IsFailure)
        {
            return access.ErrorCode == "FORBIDDEN"
                ? Result<AdminOrderDetailDto>.Failure("FORBIDDEN", access.ErrorMessage!)
                : Result<AdminOrderDetailDto>.Failure(access.ErrorCode!, access.ErrorMessage!);
        }

        var order = _orderAccess.IsPrivileged(_currentUser.RoleId)
            ? await _unitOfWork.Orders.GetByIdForAdminAsync(orderId, cancellationToken)
            : await _unitOfWork.Orders.GetByIdForAssignedOperationalAsync(
                orderId,
                _currentUser.AccountId,
                _orderAccess.GetRequiredAssignmentRoleId(_currentUser.RoleId),
                cancellationToken);

        if (order is null)
            return Result<AdminOrderDetailDto>.NotFound("Order", orderId);

        var dto = _mapper.Map<AdminOrderDetailDto>(order);

        var activeAssignments = await _unitOfWork.OrderAssignments.GetActiveAssignmentsAsync(orderId, cancellationToken);
        ApplyAssignmentNamesToDto(dto, order, activeAssignments);

        if (!_orderAccess.IsPrivileged(_currentUser.RoleId))
        {
            dto.IsAssignedToCurrentUser = activeAssignments.Any(a =>
                a.AccountId == _currentUser.AccountId
                && a.RoleId == _orderAccess.GetRequiredAssignmentRoleId(_currentUser.RoleId));
        }
        else
        {
            dto.IsAssignedToCurrentUser = true;
        }

        return Result<AdminOrderDetailDto>.Success(dto);
    }

    public async Task<Result<ConfirmOrderResponseDto>> ConfirmOrderAsync(
        int orderId,
        ConfirmOrderRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!IsStaffOrAdmin())
            return Result<ConfirmOrderResponseDto>.Failure("FORBIDDEN",
                "Only Staff or Admin can confirm orders.");

        var access = await _orderAccess.EnsureCanMutateAsync(orderId, OrderMutation.Confirm, cancellationToken);
        if (access.IsFailure)
            return Result<ConfirmOrderResponseDto>.Failure(access.ErrorCode!, access.ErrorMessage!);

        var validation = await _confirmValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return validation.ToResult<ConfirmOrderResponseDto>();

        var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(cancellationToken);

        var order = await _unitOfWork.Orders.GetByIdForUpdateAsync(orderId, cancellationToken);
        if (order is null)
            return Result<ConfirmOrderResponseDto>.NotFound("Order", orderId);

        // BƯỚC 1: Chỉ cho phép xác nhận các đơn hàng đang ở trạng thái chờ xử lý (Pending)
        if (order.Status.StatusName != OrderStatuses.Pending)
            return Result<ConfirmOrderResponseDto>.UnprocessableEntity(
                $"Order is in status '{order.Status.StatusName}'; cannot transition to '{OrderStatuses.Confirmed}'.");

        if (!statusMap.TryGetValue(OrderStatuses.Confirmed, out var confirmedId))
            return Result<ConfirmOrderResponseDto>.Failure("CONFIGURATION_ERROR",
                "Status 'Confirmed' not found in database.");

        var now = _timeProvider.UtcNow;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // BƯỚC 2: Cập nhật trạng thái đơn hàng thành Confirmed (Đã xác nhận)
            // Gán Staff chịu trách nhiệm xử lý đơn hàng này (AssignedToStaffId) là tài khoản hiện tại.
            order.AssignedToStaffId = _currentUser.AccountId;
            order.StatusId = confirmedId;
            order.ConfirmedAt = now;
            order.UpdatedAt = now;

            // BƯỚC 3: Ghi nhận lịch sử thay đổi trạng thái đơn hàng
            await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                StatusId = confirmedId,
                ChangedBy = _currentUser.AccountId,
                Note = request.Note,
                CreatedAt = now
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} confirmed by account {AccountId}",
                orderId, _currentUser.AccountId);

            // BƯỚC 4: Phát event để gửi thông báo cho khách hàng và đẩy đơn hàng sang cho bộ phận Kho (Merchandise) chuẩn bị đóng gói
            var orderPayload = new { orderId = order.OrderId, orderCode = order.OrderCode };
            await _eventPublisher.PublishAsync("Order", order.OrderId.ToString(), NotificationEventTypes.OrderConfirmed, orderPayload, CancellationToken.None);
            await _eventPublisher.PublishAsync("Order", order.OrderId.ToString(), NotificationEventTypes.MerchReadyToPack, orderPayload, CancellationToken.None);

            return Result<ConfirmOrderResponseDto>.Success(new ConfirmOrderResponseDto
            {
                OrderId = order.OrderId,
                StatusName = OrderStatuses.Confirmed,
                ConfirmedAt = now
            });
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    // ── UC4: Merchandise chuyen Processing ───────────────────────────────────

    public async Task<Result<ProcessOrderResponseDto>> ProcessOrderAsync(
        int orderId,
        ProcessOrderRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!IsMerchandiseOrAdmin())
            return Result<ProcessOrderResponseDto>.Failure("FORBIDDEN",
                "Only Merchandise or Admin can process orders.");

        var access = await _orderAccess.EnsureCanMutateAsync(orderId, OrderMutation.Process, cancellationToken);
        if (access.IsFailure)
            return Result<ProcessOrderResponseDto>.Failure(access.ErrorCode!, access.ErrorMessage!);

        var validation = await _processValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return validation.ToResult<ProcessOrderResponseDto>();

        var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(cancellationToken);

        var order = await _unitOfWork.Orders.GetByIdForUpdateAsync(orderId, cancellationToken);
        if (order is null)
            return Result<ProcessOrderResponseDto>.NotFound("Order", orderId);

        // BƯỚC 1: Chỉ cho phép chuyển trạng thái đóng gói (Processing) đối với các đơn hàng đã được xác nhận (Confirmed) trước đó
        if (order.Status.StatusName != OrderStatuses.Confirmed)
            return Result<ProcessOrderResponseDto>.UnprocessableEntity(
                $"Order is in status '{order.Status.StatusName}'; cannot transition to '{OrderStatuses.Processing}'.");

        if (!statusMap.TryGetValue(OrderStatuses.Processing, out var processingId))
            return Result<ProcessOrderResponseDto>.Failure("CONFIGURATION_ERROR",
                "Status 'Processing' not found in database.");

        var now = _timeProvider.UtcNow;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // BƯỚC 2: Cập nhật trạng thái đơn hàng thành Processing (Đang xử lý / Đóng gói)
            // Gán mã nhân viên Kho chịu trách nhiệm đóng gói đơn hàng này (AssignedToMerchId)
            order.StatusId = processingId;
            order.UpdatedAt = now;
            if (IsMerchandiseOrAdmin())
            {
                order.AssignedToMerchId = _currentUser.AccountId;
            }

            // BƯỚC 3: Ghi nhận lịch sử trạng thái đóng gói
            await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                StatusId = processingId,
                ChangedBy = _currentUser.AccountId,
                Note = request.Note,
                CreatedAt = now
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} moved to Processing by account {AccountId}",
                orderId, _currentUser.AccountId);

            // BƯỚC 4: Phát event để cập nhật trên UI hoặc gửi thông báo cho khách hàng biết đơn hàng đang được đóng gói
            await _eventPublisher.PublishAsync("Order", order.OrderId.ToString(), NotificationEventTypes.OrderPacking,
                new { orderId = order.OrderId, orderCode = order.OrderCode }, CancellationToken.None);

            return Result<ProcessOrderResponseDto>.Success(new ProcessOrderResponseDto
            {
                OrderId = order.OrderId,
                StatusName = OrderStatuses.Processing
            });
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    // ── UC5: Merchandise tao don ship ─────────────────────────────────────────
    // (Xem ShipOrderAsync o cuoi file — triển khai trong todo ship-ghn-integration)

    public async Task<Result<ShipOrderResponseDto>> ShipOrderAsync(
        int orderId,
        ShipOrderRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!IsMerchandiseOrAdmin())
            return Result<ShipOrderResponseDto>.Failure("FORBIDDEN",
                "Only Merchandise or Admin can create shipping orders.");

        var access = await _orderAccess.EnsureCanMutateAsync(orderId, OrderMutation.Ship, cancellationToken);
        if (access.IsFailure)
            return Result<ShipOrderResponseDto>.Failure(access.ErrorCode!, access.ErrorMessage!);

        var validation = await _shipValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return validation.ToResult<ShipOrderResponseDto>();

        var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(cancellationToken);

        var order = await _unitOfWork.Orders.GetByIdForUpdateAsync(orderId, cancellationToken);
        if (order is null)
            return Result<ShipOrderResponseDto>.NotFound("Order", orderId);

        if (order.Status.StatusName != OrderStatuses.Processing)
            return Result<ShipOrderResponseDto>.UnprocessableEntity(
                $"Order is in status '{order.Status.StatusName}'; cannot transition to '{OrderStatuses.Shipped}'.");

        if (!statusMap.TryGetValue(OrderStatuses.Shipped, out var shippedId))
            return Result<ShipOrderResponseDto>.Failure("CONFIGURATION_ERROR",
                "Status 'Shipped' not found in database.");

        // BƯỚC 1: Lấy chi tiết các sản phẩm trong đơn và chuẩn hóa kích thước, cân nặng thực tế bằng GhnPackageCalculator
        var shippingItems = await _unitOfWork.Orders.GetShippingItemsForOrderAsync(orderId, cancellationToken);
        var sanitizedShippingItems = GhnPackageCalculator.SanitizeItems(
            shippingItems,
            _ghnOptions.DefaultItemWeight,
            _ghnOptions.DefaultLength,
            _ghnOptions.DefaultWidth,
            _ghnOptions.DefaultHeight);

        // BƯỚC 2: Kiểm soát giới hạn kích thước tuyệt đối (Bulky Limit Guard). 
        // Nếu kích thước đóng gói vượt quá 200cm, chặn tạo đơn hàng và yêu cầu chỉnh sửa lại
        if (GhnShippingLimits.HasUnshippableDimensions(sanitizedShippingItems))
        {
            var violations = GhnShippingLimits.GetViolations(
                sanitizedShippingItems,
                GhnShippingLimits.Type5ServiceId);
            return Result<ShipOrderResponseDto>.UnprocessableEntity(
                GhnShippingLimits.FormatViolationMessage(violations));
        }

        var requestedServiceTypeId = GhnShippingLimits.Type2ServiceId;
        if (int.TryParse(request.ServiceType, out var parsedServiceTypeId) && parsedServiceTypeId > 0)
            requestedServiceTypeId = parsedServiceTypeId;

        // BƯỚC 3: Thiết lập số tiền COD (nếu phương thức thanh toán là COD thì thu hộ COD, ngược lại đã thanh toán online COD = 0)
        var codAmount = string.Equals(order.PaymentMethod, "SHIP_COD", StringComparison.OrdinalIgnoreCase)
            ? order.TotalAmount
            : 0m;

        // Tự động phân cấp gói dịch vụ giao nhận (Ví dụ: tự nâng cấp lên hàng cồng kềnh Type 5 khi vượt hạn mức Type 2)
        var package = GhnPackageCalculator.CalculateForServiceType(
            shippingItems,
            requestedServiceTypeId,
            _ghnOptions.DefaultItemWeight,
            _ghnOptions.DefaultLength,
            _ghnOptions.DefaultWidth,
            _ghnOptions.DefaultHeight);

        var serviceTypeId = package.ServiceTypeId;

        if (string.IsNullOrWhiteSpace(order.ShippingWardCode))
        {
            return Result<ShipOrderResponseDto>.UnprocessableEntity(
                "Order is missing a valid receiver ward code (ShippingWardCode). Update the shipping address before creating a GHN shipment.");
        }

        if (order.ShippingDistrictId <= 0)
        {
            return Result<ShipOrderResponseDto>.UnprocessableEntity(
                "Order is missing a valid receiver district (ShippingDistrictId). Update the shipping address before creating a GHN shipment.");
        }

        if (package.Items.Count == 0)
        {
            return Result<ShipOrderResponseDto>.UnprocessableEntity(
                "Order has no shippable items with product dimensions. Ensure each product has ProductDetails (weight and size) before shipping.");
        }

        var ghnRequest = new ShippingOrderCreateRequestDto
        {
            ClientOrderCode = order.OrderCode,
            ToName = order.ShippingName,
            ToPhone = order.ShippingPhone,
            ToAddress = order.ShippingAddress,
            ToDistrictId = order.ShippingDistrictId,
            ToWardCode = order.ShippingWardCode,
            ServiceTypeId = serviceTypeId,
            InsuranceValue = 0m,
            CodAmount = codAmount,
            Weight = Math.Max(package.Weight, 1),
            Length = package.Length,
            Width = package.Width,
            Height = package.Height,
            Note = request.Note,
            RequiredNote = !string.IsNullOrWhiteSpace(request.RequiredNote) ? request.RequiredNote : "KHONGCHOXEMHANG",
            Items = package.Items.Select(x => new ShippingOrderCreateItemDto
            {
                Name = x.Name,
                Code = x.Code,
                Quantity = x.Quantity,
                Price = x.Price,
                Weight = x.Weight,
                Length = x.Length,
                Width = x.Width,
                Height = x.Height,
                Category = x.Category
            }).ToList()
        };

        // BƯỚC 4: THIẾT KẾ AN TOÀN NGOÀI TRANSACTION (Out-of-transaction API Call):
        // Gọi API của bên thứ 3 (GHN) ngoài database transaction để tránh chiếm giữ kết nối DB (connection pool)
        // và giữ các khóa dòng lâu trong trường hợp mạng trễ/nghẽn.
        var ghnResult = await _ghnClient.CreateOrderAsync(ghnRequest, cancellationToken);
        if (!ghnResult.IsSuccess)
        {
            _logger.LogWarning("GHN create order failed for Order {OrderId}: {Error}",
                orderId, ghnResult.ErrorMessage);

            if (string.Equals(ghnResult.ErrorCode, "GHN_DIMENSION_ERROR", StringComparison.Ordinal))
                return Result<ShipOrderResponseDto>.UnprocessableEntity(ghnResult.ErrorMessage!);

            return Result<ShipOrderResponseDto>.BadGateway(
                $"Shipping provider error: {ghnResult.ErrorMessage}");
        }

        var ghnData = ghnResult.Data!;

        // Lấy thời gian dự kiến giao hàng từ GHN (nếu cổng không có sẵn thì gọi lấy leadtime bổ sung)
        DateTime? estimatedDelivery = ghnData.ExpectedDeliveryTime;
        if (estimatedDelivery is null)
        {
            var leadtimeResult = await _ghnClient.GetLeadtimeAsync(new LeadtimeRequestDTO
            {
                FromDistrictId = _ghnOptions.FromDistrictId,
                FromWardCode = _ghnOptions.FromWardCode,
                ToDistrictId = order.ShippingDistrictId,
                ToWardCode = order.ShippingWardCode,
                ServiceTypeId = serviceTypeId
            }, cancellationToken);

            if (leadtimeResult.IsSuccess && leadtimeResult.Data!.LeadtimeUnix > 0)
                estimatedDelivery = leadtimeResult.Data!.EstimatedDeliveryTime;
        }

        // Lấy phí giao hàng thực tế từ phản hồi GHN
        decimal actualFee = ghnData.TotalFee;
        if (actualFee <= 0)
        {
            var feeResult = await _ghnClient.GetFeeAsync(new FeeRequestDTO
            {
                FromDistrictId = _ghnOptions.FromDistrictId,
                FromWardCode = _ghnOptions.FromWardCode,
                ToDistrictId = order.ShippingDistrictId,
                ToWardCode = order.ShippingWardCode,
                InsuranceValue = 0m,
                CodValue = codAmount,
                Weight = Math.Max(package.Weight, 1),
                Length = package.Length,
                Width = package.Width,
                Height = package.Height,
                ServiceTypeId = package.ServiceTypeId
            }, cancellationToken);

            if (feeResult.IsSuccess)
                actualFee = feeResult.Data!.Fee;
        }

        var now = _timeProvider.UtcNow;

        // BƯỚC 5: GHI NHẬN THÔNG TIN VẬN ĐƠN VÀO CƠ SỞ DỮ LIỆU (Database Transaction)
        // Khi API đã xử lý thành công, tiến hành ghi bản ghi lịch sử vận đơn và chuyển đơn hàng sang trạng thái Shipped.
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // A. Ghi log lịch sử vận dịch giao nhận (ShippingProviderTransaction)
            await _unitOfWork.Orders.AddShippingTransactionAsync(new ShippingProviderTransaction
            {
                OrderId = order.OrderId,
                Provider = request.Provider.ToUpperInvariant(),
                ProviderOrderCode = ghnData.OrderCode,
                TrackingNumber = ghnData.OrderCode,
                ServiceType = request.ServiceType,
                Status = "ready_to_pick",
                ShippingFee = actualFee,
                CodAmount = codAmount,
                EstimatedDelivery = estimatedDelivery,
                CreatedAt = now,
                RowVersion = []
            }, cancellationToken);

            // B. Cập nhật mã vận đơn và phí vận chuyển thực tế lên đơn hàng
            // Đảm bảo ConfirmedAt được thiết lập để thỏa mãn ràng buộc CK_Orders_Timestamps
            if (order.ConfirmedAt == null)
            {
                order.ConfirmedAt = order.OrderDate <= now ? order.OrderDate : now;
            }
            order.StatusId = shippedId;
            order.ShippedAt = now < order.ConfirmedAt ? order.ConfirmedAt : now;
            order.ActualShippingFee = actualFee > 0 ? actualFee : null;
            if (actualFee > 0)
                order.EstimatedShippingFee = actualFee;
            order.ShippingOrderCode = ghnData.OrderCode;
            order.UpdatedAt = now;

            // C. Ghi nhận nhật ký trạng thái Shipped
            await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                StatusId = shippedId,
                ChangedBy = _currentUser.AccountId,
                Note = request.Note,
                CreatedAt = now
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation(
                "Order {OrderId} shipped via {Provider}, tracking={Tracking}",
                orderId, request.Provider, ghnData.OrderCode);

            // D. Phát event báo tin cho khách hàng mã vận đơn để theo dõi đơn hàng
            await _eventPublisher.PublishAsync("Order", order.OrderId.ToString(), NotificationEventTypes.OrderShipped,
                new { orderId = order.OrderId, orderCode = order.OrderCode, trackingNumber = ghnData.OrderCode }, CancellationToken.None);

            return Result<ShipOrderResponseDto>.Success(new ShipOrderResponseDto
            {
                TrackingNumber = ghnData.OrderCode,
                ProviderOrderCode = ghnData.OrderCode,
                EstimatedDelivery = estimatedDelivery,
                ShippingFee = actualFee
            });
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    // ── UC6: Huy don hang ─────────────────────────────────────────────────────

    public async Task<Result<CancelOrderResponseDto>> CancelOrderAsync(
        int orderId,
        CancelOrderRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!IsStaffOrAdmin())
            return Result<CancelOrderResponseDto>.Failure("FORBIDDEN",
                "Only Staff or Admin can cancel orders.");

        var access = await _orderAccess.EnsureCanMutateAsync(orderId, OrderMutation.Cancel, cancellationToken);
        if (access.IsFailure)
            return Result<CancelOrderResponseDto>.Failure(access.ErrorCode!, access.ErrorMessage!);

        var validation = await _cancelValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return validation.ToResult<CancelOrderResponseDto>();

        var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(cancellationToken);

        var order = await _unitOfWork.Orders.GetByIdForUpdateAsync(orderId, cancellationToken);
        if (order is null)
            return Result<CancelOrderResponseDto>.NotFound("Order", orderId);

        // BƯỚC 1: Kiểm soát trạng thái cuối (Terminal Status Guard).
        // Không cho phép hủy đơn nếu đã Completed, Cancelled, Refunded, hoặc Delivered.
        // Delivered = đơn đã giao tới tay khách, không thể hủy hay thu hồi.
        bool isTerminal = order.Status.StatusName is
            OrderStatuses.Cancelled or
            OrderStatuses.Refunded or
            OrderStatuses.Completed or
            OrderStatuses.Delivered;
        if (isTerminal)
            return Result<CancelOrderResponseDto>.UnprocessableEntity(
                $"Order is in status '{order.Status.StatusName}'; cancellation is not allowed.");

        // BƯỚC 1b: Kiểm tra GHN status — nếu shipper đang trên đường giao hàng hoặc đã giao xong
        // (delivering / money_collect_delivering / delivered) thì không cho hủy vì đơn đã rời kho.
        var latestGhnStatus = order.ShippingProviderTransactions
            .Where(t => t.RefundId == null)
            .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
            .Select(t => t.Status)
            .FirstOrDefault();
        bool isOutForDelivery = latestGhnStatus is
            ShippingStatuses.Delivering or
            ShippingStatuses.MoneyCollectDelivering or
            ShippingStatuses.Delivered;
        if (isOutForDelivery)
            return Result<CancelOrderResponseDto>.UnprocessableEntity(
                $"Order cannot be cancelled: the shipment is already out for delivery (GHN status: '{latestGhnStatus}'). " +
                "Cancellation is only possible while the package is still in the warehouse.");

        // BƯỚC 2: Chỉ Admin mới có quyền tối cao hủy đơn ở mọi trạng thái trung gian, 
        // Staff chỉ được hủy đơn hàng ở giai đoạn Pending hoặc Confirmed.
        if (!IsAdmin() && !OrderStatuses.CancellableStatuses.Contains(order.Status.StatusName))
            return Result<CancelOrderResponseDto>.UnprocessableEntity(
                $"Order is in status '{order.Status.StatusName}'; cancellation is only allowed for Pending or Confirmed orders.");

        if (!statusMap.TryGetValue(OrderStatuses.Cancelled, out var cancelledId))
            return Result<CancelOrderResponseDto>.Failure("CONFIGURATION_ERROR",
                "Status 'Cancelled' not found in database.");

        // BƯỚC 3: Nếu đơn hàng đã có mã vận đơn gửi đi của GHN, gọi API GHN hủy vận đơn trước.
        // Điều này phòng tránh việc đơn hàng bị hủy trên hệ thống nhưng shipper vẫn đi giao hàng.
        if (!string.IsNullOrEmpty(order.ShippingOrderCode))
        {
            var ghnCancel = await _ghnClient.CancelOrderAsync(order.ShippingOrderCode, cancellationToken);
            if (!ghnCancel.IsSuccess)
            {
                _logger.LogWarning(
                    "GHN cancel failed for {GhnCode} (order {OrderId}): {Error}",
                    order.ShippingOrderCode, orderId, ghnCancel.ErrorMessage);
                return Result<CancelOrderResponseDto>.BadGateway(
                    $"Cannot cancel order: the GHN shipment could not be cancelled ({ghnCancel.ErrorMessage}). " +
                    "The shipment may already be in transit. Please cancel directly on the GHN portal first.");
            }
        }

        // BƯỚC 4: Thực thi hủy đơn và thực hiện nghiệp vụ nghiệp vụ hoàn trả kho, hoàn ví tự động, khôi phục voucher thông qua OrderLifecycleService.
        var result = await _orderLifecycle.CancelOrderInternalAsync(
            order,
            request.Reason ?? "Admin cancelled",
            cancelledByAccountId: _currentUser.AccountId,
            restoreCart: false,
            restoreVoucher: true,
            cancellationToken: cancellationToken);
        if (!result.IsSuccess)
        {
            return Result<CancelOrderResponseDto>.Failure(result.ErrorCode!, result.ErrorMessage!);
        }

        var cancelPayload = new { orderId = order.OrderId, orderCode = order.OrderCode, reason = request.Reason };
        await _eventPublisher.PublishAsync("Order", order.OrderId.ToString(), NotificationEventTypes.OrderCancelled, cancelPayload, CancellationToken.None);

        if (!(order.PaymentMethod == "SE_PAY" && order.PaymentStatus != "PAID"))
        {
            await _eventPublisher.PublishAsync("Order", order.OrderId.ToString(), NotificationEventTypes.StaffCancelRequested, cancelPayload, CancellationToken.None);
        }

        return Result<CancelOrderResponseDto>.Success(new CancelOrderResponseDto
        {
            OrderId = order.OrderId,
            CancelledAt = order.CancelledAt ?? _timeProvider.UtcNow
        });
    }

    // ── UC7: Admin assign lai don ─────────────────────────────────────────────

    public async Task<Result> AssignOrderAsync(
        int orderId,
        AssignOrderRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!IsAdmin())
            return Result.Failure("FORBIDDEN", "Only Admin can reassign orders.");

        var validation = await _assignValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return validation.ToResult();

        var order = await _unitOfWork.Orders.GetByIdForUpdateAsync(orderId, cancellationToken);
        if (order is null)
            return Result.NotFound("Order", orderId);

        // BƯỚC 1: Xác thực tài khoản nhân viên đích nhận phân công phải tồn tại và đang hoạt động (Active)
        var targetAccount = await _unitOfWork.Accounts.GetByIdAsync(request.TargetAccountId, cancellationToken);
        if (targetAccount is null || targetAccount.IsDeleted || !targetAccount.IsActive)
            return Result.Failure("NOT_FOUND",
                $"Account with ID '{request.TargetAccountId}' was not found or is inactive.");

        // BƯỚC 2: Kiểm tra vai trò của nhân viên được phân công có phù hợp với giai đoạn hiện tại của đơn hàng không
        // (Ví dụ: Không được gán nhân viên Bán hàng xử lý khâu Đóng gói ở Kho).
        var validationError = ValidateAssigneeRoleForOrderStage(
            order.Status.StatusName, targetAccount.Role?.RoleName ?? string.Empty);
        if (validationError is not null)
            return Result.UnprocessableEntity(validationError);

        var assignmentRoleId = ResolveOrderAssignmentRoleId(targetAccount.RoleId, order.Status.StatusName);
        if (assignmentRoleId is null)
        {
            return Result.Failure("VALIDATION_ERROR",
                "Cannot determine assignment role for the target account and order status.");
        }

        // BƯỚC 3: Xác định ca trực ngày hôm nay của nhân viên đích và kiểm tra xem họ có đang trong ca trực hoạt động (OnDuty) không
        var todayVn = _timeProvider.UtcNow.AddHours(7).Date;
        var schedules = await _unitOfWork.WorkSchedules.GetByAccountAndDateAsync(
            request.TargetAccountId, todayVn, cancellationToken);
        var onDutySchedule = schedules.FirstOrDefault(s => s.Status == "OnDuty");
        if (onDutySchedule is null)
        {
            return Result.UnprocessableEntity(
                "Target account has no OnDuty work schedule for today.");
        }

        var now = _timeProvider.UtcNow;
        var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(cancellationToken);

        if (!statusMap.TryGetValue(order.Status.StatusName, out var currentStatusId))
            return Result.Failure("CONFIGURATION_ERROR", "Current status not found in database.");

        var note = string.IsNullOrWhiteSpace(request.Note)
            ? "Reassigned by Admin"
            : $"Reassigned by Admin: {request.Note}";

        var activeAssignments = await _unitOfWork.OrderAssignments.GetActiveAssignmentsAsync(orderId, cancellationToken);
        var hasExistingForRole = activeAssignments.Any(a => a.RoleId == assignmentRoleId.Value);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (hasExistingForRole)
            {
                // BƯỚC 4A: Nếu vai trò này đã được gán trước đó, thực hiện chuyển giao/phân công lại (Reassign)
                // Hàm này sẽ hủy kích hoạt phân công cũ và gán phân công mới an toàn.
                await _unitOfWork.OrderAssignments.ReassignAsync(
                    orderId,
                    assignmentRoleId.Value,
                    onDutySchedule.ScheduleId,
                    _currentUser.AccountId,
                    note,
                    cancellationToken);
            }
            else
            {
                // BƯỚC 4B: Nếu vai trò này chưa được gán, kiểm tra tải trọng ca trực mới (CurrentLoad vs MaxLoad)
                // và tạo bản ghi phân công mới (OrderAssignment), đồng thời tăng tải trọng ca trực lên 1.
                var schedule = await _unitOfWork.WorkSchedules.GetByIdForUpdateAsync(
                    onDutySchedule.ScheduleId, cancellationToken);
                if (schedule?.StaffShiftCapacity is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result.Failure("CONFIGURATION_ERROR", "Shift capacity not found for target schedule.");
                }

                if (schedule.StaffShiftCapacity.CurrentLoad >= schedule.StaffShiftCapacity.MaxLoad)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result.Failure("BUSINESS_RULE_VIOLATION", "Target schedule is at full capacity.");
                }

                await _unitOfWork.OrderAssignments.AddAsync(new OrderAssignment
                {
                    OrderId = orderId,
                    ScheduleId = schedule.ScheduleId,
                    AccountId = schedule.AccountId,
                    RoleId = assignmentRoleId.Value,
                    IsActive = true,
                    AssignedAt = now,
                    AssignedBy = _currentUser.AccountId,
                    Notes = note
                }, cancellationToken);

                schedule.StaffShiftCapacity.CurrentLoad += 1;
                schedule.StaffShiftCapacity.UpdatedAt = now;

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // BƯỚC 5: Cập nhật trường denormalized tương ứng trên Orders (AssignedToStaffId hoặc AssignedToMerchId)
            if (assignmentRoleId == OrderAccessRoles.AssignmentStaff)
            {
                order.AssignedToStaffId = request.TargetAccountId;
            }
            else if (assignmentRoleId == OrderAccessRoles.AssignmentMerchandise)
            {
                order.AssignedToMerchId = request.TargetAccountId;
            }

            order.UpdatedAt = now;

            await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                StatusId = currentStatusId,
                ChangedBy = _currentUser.AccountId,
                Note = note,
                CreatedAt = now
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation(
                "Order {OrderId} assigned to account {TargetId} (OA role {RoleId}) by Admin {AdminId}",
                orderId, request.TargetAccountId, assignmentRoleId, _currentUser.AccountId);

            // BƯỚC 6: Phát event báo nhân viên mới được giao việc để hiển thị thông báo
            await _eventPublisher.PublishAsync("Order", order.OrderId.ToString(), NotificationEventTypes.StaffOrderAssigned,
                new { orderId = order.OrderId, orderCode = order.OrderCode, targetAccountId = request.TargetAccountId }, CancellationToken.None);

            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            return Result.Failure("BUSINESS_RULE_VIOLATION", ex.Message);
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    // ── UC3b: Tu dong confirm sau khi thanh toan ──────────────────────────────

    public async Task AutoConfirmAfterPaymentAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        // ── THIẾT KẾ AN TOÀN (Safe-fail): ──────────────────────────────────────────────────────────
        // Gọi hàm này NGAY SAU KHI nhận được webhook báo thanh toán thành công (PAID).
        // Nếu việc tự động xác nhận đơn hàng (Pending -> Confirmed) bị thất bại, chỉ ghi nhận log lỗi 
        // và KHÔNG rollback giao dịch thanh toán. Thanh toán thành công là thực tế tiền đã vào tài khoản,
        // nếu hệ thống không tự động confirm được thì nhân viên/admin vẫn có thể nhấn nút xác nhận thủ công.
        // ───────────────────────────────────────────────────────────────────────────────────────────

        try
        {
            var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(cancellationToken);

            var order = await _unitOfWork.Orders.GetByIdForUpdateAsync(orderId, cancellationToken);
            if (order is null)
            {
                _logger.LogWarning("AutoConfirm: Order {OrderId} not found", orderId);
                return;
            }

            // Chỉ xác nhận tự động khi đơn hàng đã thanh toán thành công (PAID) và trạng thái hiện tại là Pending (Chờ xác nhận)
            if (order.PaymentStatus != "PAID" || order.Status.StatusName != OrderStatuses.Pending)
            {
                _logger.LogInformation(
                    "AutoConfirm: Order {OrderId} skipped (PaymentStatus={PS}, Status={S})",
                    orderId, order.PaymentStatus, order.Status.StatusName);
                return;
            }

            if (!statusMap.TryGetValue(OrderStatuses.Confirmed, out var confirmedId))
            {
                _logger.LogError("AutoConfirm: Status 'Confirmed' not found in database");
                return;
            }

            var now = _timeProvider.UtcNow;

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // Cập nhật trạng thái đơn hàng thành Confirmed
                order.StatusId = confirmedId;
                order.ConfirmedAt = now;
                order.UpdatedAt = now;

                var staffAssignment = await _unitOfWork.OrderAssignments.GetActiveAssignmentsAsync(orderId, cancellationToken);
                var staffRow = staffAssignment.FirstOrDefault(a => a.RoleId == OrderAccessRoles.AssignmentStaff);
                order.AssignedToStaffId = staffRow?.AccountId;

                await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
                {
                    OrderId = order.OrderId,
                    StatusId = confirmedId,
                    ChangedBy = null,
                    Note = "Auto-confirmed: payment received",
                    CreatedAt = now
                }, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Order {OrderId} auto-confirmed after payment", orderId);

                // Gửi tin nhắn thông báo cho khách hàng và đẩy đơn sang bộ phận kho để chuẩn bị đóng gói
                var autoConfirmPayload = new { orderId = order.OrderId, orderCode = order.OrderCode };
                await _eventPublisher.PublishAsync("Order", order.OrderId.ToString(),
                    NotificationEventTypes.OrderConfirmed, autoConfirmPayload, CancellationToken.None);
                await _eventPublisher.PublishAsync("Order", order.OrderId.ToString(),
                    NotificationEventTypes.MerchReadyToPack, autoConfirmPayload, CancellationToken.None);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            // KHONG re-throw — payment da thanh cong, confirm loi thi Staff xu ly thu cong
            _logger.LogError(ex,
                "AutoConfirm failed for Order {OrderId} — payment remains valid, manual confirm required",
                orderId);
        }
    }

    // ── Role helpers ──────────────────────────────────────────────────────────

    private bool IsStaffOrAdmin() => _currentUser.RoleName is RoleStaff or RoleAdmin;
    private bool IsMerchandiseOrAdmin() => _currentUser.RoleName is RoleMerchandise or RoleAdmin;
    private bool IsAdmin() => _currentUser.RoleName == RoleAdmin;

    private static IReadOnlyCollection<string> GetAllowedStatusesForRole(string roleName) =>
        roleName switch
        {
            RoleStaff => OrderStatuses.StaffVisibleStatuses,
            RoleMerchandise => OrderStatuses.MerchandiseVisibleStatuses,
            _ => OrderStatuses.AdminVisibleStatuses
        };

    /// <summary>
    /// Tra ve thong bao loi neu role cua assignee khong phu hop voi giai doan don hang.
    /// Tra ve null neu hop le.
    /// </summary>
    private static void ApplyAssignmentNamesToDto(
        AdminOrderListItemDto dto,
        Order order,
        IEnumerable<OrderAssignment> activeAssignments)
    {
        ApplyAssignmentNames(
            activeAssignments,
            order,
            out var staffId,
            out var staffName,
            out var merchId,
            out var merchName);

        if (staffId.HasValue)
        {
            dto.AssignedToStaffId = staffId;
            dto.AssignedToStaffName = staffName;
        }

        if (merchId.HasValue)
        {
            dto.AssignedToMerchId = merchId;
            dto.AssignedToMerchName = merchName;
        }
    }

    private static void ApplyAssignmentNamesToDto(
        AdminOrderDetailDto dto,
        Order order,
        IEnumerable<OrderAssignment> activeAssignments)
    {
        ApplyAssignmentNames(
            activeAssignments,
            order,
            out var staffId,
            out var staffName,
            out var merchId,
            out var merchName);

        if (staffId.HasValue)
        {
            dto.AssignedToStaffId = staffId;
            dto.AssignedToStaffName = staffName;
        }

        if (merchId.HasValue)
        {
            dto.AssignedToMerchId = merchId;
            dto.AssignedToMerchName = merchName;
        }
    }

    /// <summary>Active OA first; fallback to Orders snapshot (Staff/Merch PIC).</summary>
    private static void ApplyAssignmentNames(
        IEnumerable<OrderAssignment> activeAssignments,
        Order order,
        out int? staffId,
        out string? staffName,
        out int? merchId,
        out string? merchName)
    {
        var staffAssig = activeAssignments.FirstOrDefault(a => a.RoleId == OrderAccessRoles.AssignmentStaff);
        var merchAssig = activeAssignments.FirstOrDefault(a => a.RoleId == OrderAccessRoles.AssignmentMerchandise);

        if (staffAssig != null)
        {
            staffId = staffAssig.AccountId;
            staffName = staffAssig.Account?.AccountName;
        }
        else
        {
            staffId = order.AssignedToStaffId;
            staffName = order.AssignedToStaff?.AccountName;
        }

        if (merchAssig != null)
        {
            merchId = merchAssig.AccountId;
            merchName = merchAssig.Account?.AccountName;
        }
        else
        {
            merchId = order.AssignedToMerchId;
            merchName = order.AssignedToMerch?.AccountName;
        }
    }

    private static byte? ResolveOrderAssignmentRoleId(byte targetAccountRoleId, string orderStatusName)
    {
        if (targetAccountRoleId == OrderAccessRoles.Staff)
            return OrderAccessRoles.AssignmentStaff;

        if (targetAccountRoleId == OrderAccessRoles.Merchandise)
            return OrderAccessRoles.AssignmentMerchandise;

        if (targetAccountRoleId != OrderAccessRoles.Admin)
            return null;

        if (orderStatusName is OrderStatuses.Pending or OrderStatuses.Confirmed or OrderStatuses.DeliveryFailed)
            return OrderAccessRoles.AssignmentStaff;

        if (orderStatusName is OrderStatuses.Processing or OrderStatuses.Shipped or OrderStatuses.Delivering or OrderStatuses.WaitingReturn or OrderStatuses.ReturnFailed or OrderStatuses.Lost or OrderStatuses.Damaged)
            return OrderAccessRoles.AssignmentMerchandise;

        return null;
    }

    private static string? ValidateAssigneeRoleForOrderStage(string statusName, string assigneeRole)
    {
        // Pending / Confirmed / DeliveryFailed -> Staff hoac Admin hoac Merchandise
        if (statusName is OrderStatuses.Pending or OrderStatuses.Confirmed or OrderStatuses.DeliveryFailed)
        {
            if (assigneeRole is not (RoleStaff or RoleAdmin or RoleMerchandise))
                return $"Cannot assign an order in status '{statusName}' to a '{assigneeRole}' account. Expected Staff, Merchandise, or Admin.";
            return null;
        }

        // Processing / Shipped / Delivering / WaitingReturn / ReturnFailed / Lost / Damaged -> Merchandise hoac Admin
        if (statusName is OrderStatuses.Processing or OrderStatuses.Shipped or OrderStatuses.Delivering or OrderStatuses.WaitingReturn or OrderStatuses.ReturnFailed or OrderStatuses.Lost or OrderStatuses.Damaged)
        {
            if (assigneeRole is not (RoleMerchandise or RoleAdmin))
                return $"Cannot assign an order in status '{statusName}' to a '{assigneeRole}' account. Expected Merchandise or Admin.";
            return null;
        }

        // Cac trang thai cuoi (Delivered, Completed, Cancelled) -> khong cho assign
        return $"Order in status '{statusName}' cannot be reassigned.";
    }
}


