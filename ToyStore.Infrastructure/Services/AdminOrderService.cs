using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Checkouts;
using ToyStore.Application.DTOs.Orders;
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
    private readonly ILogger<AdminOrderService> _logger;

    // Role names khop voi ClaimTypes.Role trong JWT
    private const string RoleStaff       = "Staff";
    private const string RoleMerchandise = "Merchandise";
    private const string RoleAdmin       = "Admin";

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
        ILogger<AdminOrderService> logger)
    {
        _unitOfWork    = unitOfWork;
        _currentUser   = currentUser;
        _mapper        = mapper;
        _ghnClient     = ghnClient;
        _ghnOptions    = ghnOptions.Value;
        _shopAddress   = shopAddress.Value;
        _shipValidator = shipValidator;
        _cancelValidator = cancelValidator;
        _assignValidator = assignValidator;
        _logger        = logger;
    }

    // ── UC1: Danh sach don hang ───────────────────────────────────────────────

    public async Task<Result<PaginatedResponse<AdminOrderListItemDto>>> GetListAsync(
        AdminOrderQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var allowedStatuses = GetAllowedStatusesForRole(_currentUser.RoleName);

        var pageSize = Math.Min(query.PageSize, 100);
        var pageNumber = Math.Max(query.PageNumber, 1);

        var items = await _unitOfWork.Orders.GetAdminPagedAsync(
            allowedStatuses,
            pageNumber,
            pageSize,
            query.StatusId,
            query.AssignedToMe,
            _currentUser.AccountId,
            query.Keyword,
            query.FromDate,
            query.ToDate,
            cancellationToken);

        var count = await _unitOfWork.Orders.CountAdminAsync(
            allowedStatuses,
            query.StatusId,
            query.AssignedToMe,
            _currentUser.AccountId,
            query.Keyword,
            query.FromDate,
            query.ToDate,
            cancellationToken);

        var dtos = _mapper.Map<List<AdminOrderListItemDto>>(items);
        return Result<PaginatedResponse<AdminOrderListItemDto>>.Success(
            new PaginatedResponse<AdminOrderListItemDto>(dtos, count, pageNumber, pageSize));
    }

    // ── UC2: Chi tiet don hang ────────────────────────────────────────────────

    public async Task<Result<AdminOrderDetailDto>> GetDetailAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Orders.GetByIdForAdminAsync(orderId, cancellationToken);
        if (order is null)
            return Result<AdminOrderDetailDto>.NotFound("Order", orderId);

        var dto = _mapper.Map<AdminOrderDetailDto>(order);
        return Result<AdminOrderDetailDto>.Success(dto);
    }

    // ── UC3: Staff xac nhan don ───────────────────────────────────────────────

    public async Task<Result<ConfirmOrderResponseDto>> ConfirmOrderAsync(
        int orderId,
        ConfirmOrderRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!IsStaffOrAdmin())
            return Result<ConfirmOrderResponseDto>.Failure("FORBIDDEN",
                "Only Staff or Admin can confirm orders.");

        var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(cancellationToken);

        var order = await _unitOfWork.Orders.GetByIdForUpdateAsync(orderId, cancellationToken);
        if (order is null)
            return Result<ConfirmOrderResponseDto>.NotFound("Order", orderId);

        if (order.Status.StatusName != OrderStatuses.Pending)
            return Result<ConfirmOrderResponseDto>.UnprocessableEntity(
                $"Order is in status '{order.Status.StatusName}'; cannot transition to '{OrderStatuses.Confirmed}'.");

        if (!statusMap.TryGetValue(OrderStatuses.Confirmed, out var confirmedId))
            return Result<ConfirmOrderResponseDto>.Failure("CONFIGURATION_ERROR",
                "Status 'Confirmed' not found in database.");

        var now = DateTime.UtcNow;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            order.AssignedToStaffId = _currentUser.AccountId;
            order.StatusId          = confirmedId;
            order.ConfirmedAt       = now;
            order.UpdatedAt         = now;

            await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
            {
                OrderId   = order.OrderId,
                StatusId  = confirmedId,
                ChangedBy = _currentUser.AccountId,
                Note      = request.Note,
                CreatedAt = now
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} confirmed by account {AccountId}",
                orderId, _currentUser.AccountId);

            return Result<ConfirmOrderResponseDto>.Success(new ConfirmOrderResponseDto
            {
                OrderId     = order.OrderId,
                StatusName  = OrderStatuses.Confirmed,
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

        var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(cancellationToken);

        var order = await _unitOfWork.Orders.GetByIdForUpdateAsync(orderId, cancellationToken);
        if (order is null)
            return Result<ProcessOrderResponseDto>.NotFound("Order", orderId);

        if (order.Status.StatusName != OrderStatuses.Confirmed)
            return Result<ProcessOrderResponseDto>.UnprocessableEntity(
                $"Order is in status '{order.Status.StatusName}'; cannot transition to '{OrderStatuses.Processing}'.");

        if (!statusMap.TryGetValue(OrderStatuses.Processing, out var processingId))
            return Result<ProcessOrderResponseDto>.Failure("CONFIGURATION_ERROR",
                "Status 'Processing' not found in database.");

        var now = DateTime.UtcNow;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            order.AssignedToStaffId = _currentUser.AccountId;
            order.StatusId          = processingId;
            order.UpdatedAt         = now;

            await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
            {
                OrderId   = order.OrderId,
                StatusId  = processingId,
                ChangedBy = _currentUser.AccountId,
                Note      = request.Note,
                CreatedAt = now
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} moved to Processing by account {AccountId}",
                orderId, _currentUser.AccountId);

            return Result<ProcessOrderResponseDto>.Success(new ProcessOrderResponseDto
            {
                OrderId    = order.OrderId,
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

        // Xay dung request goi GHN
        var codAmount = string.Equals(order.PaymentMethod, "SHIP_CODE", StringComparison.OrdinalIgnoreCase)
            ? order.TotalAmount
            : 0m;

        // Phan giai serviceId tu serviceType hoac GhnOptions mac dinh
        int serviceId = 0;
        if (int.TryParse(request.ServiceType, out var parsedServiceId) && parsedServiceId > 0)
        {
            serviceId = parsedServiceId;
        }

        // Tinh toan trong luong / kich thuoc don gian tu so luong san pham
        var totalWeight = Math.Max(order.OrderDetails.Sum(d => d.Quantity) * 300, 100); // 300g/item
        var ghnRequest = new ShippingOrderCreateRequestDto
        {
            ClientOrderCode = order.OrderCode,
            ToName          = order.ShippingName,
            ToPhone         = order.ShippingPhone,
            ToAddress       = order.ShippingAddress,
            ToDistrictId    = order.ShippingDistrictId,
            ToWardCode      = order.ShippingWardCode,
            ServiceId       = serviceId,
            InsuranceValue  = order.SubTotal,
            CodAmount       = codAmount,
            Weight          = totalWeight,
            Length          = 30,
            Width           = 30,
            Height          = 10,
            Note            = request.Note,
            Items           = order.OrderDetails.Select(d => new ShippingOrderCreateItemDto
            {
                Name     = d.ProductName,
                Quantity = d.Quantity,
                Price    = d.UnitPrice,
                Weight   = 300
            }).ToList()
        };

        // Goi GHN truoc transaction — rollback DB neu loi
        var ghnResult = await _ghnClient.CreateOrderAsync(ghnRequest, cancellationToken);
        if (!ghnResult.IsSuccess)
        {
            _logger.LogWarning("GHN create order failed for Order {OrderId}: {Error}",
                orderId, ghnResult.ErrorMessage);
            return Result<ShipOrderResponseDto>.BadGateway(
                $"Shipping provider error: {ghnResult.ErrorMessage}");
        }

        var ghnData = ghnResult.Data!;

        // Lay leadtime (khong bat buoc, neu loi thi bo qua)
        DateTime? estimatedDelivery = null;
        if (serviceId > 0)
        {
            var leadtimeResult = await _ghnClient.GetLeadtimeAsync(new LeadtimeRequestDTO
            {
                FromDistrictId = _ghnOptions.FromDistrictId,
                FromWardCode   = _ghnOptions.FromWardCode,
                ToDistrictId   = order.ShippingDistrictId,
                ToWardCode     = order.ShippingWardCode,
                ServiceId      = serviceId
            }, cancellationToken);

            if (leadtimeResult.IsSuccess)
                estimatedDelivery = leadtimeResult.Data!.EstimatedDeliveryTime;
        }

        // Lay phi van chuyen thuc te
        decimal actualFee = 0m;
        if (serviceId > 0)
        {
            var feeResult = await _ghnClient.GetFeeAsync(new FeeRequestDTO
            {
                FromDistrictId = _ghnOptions.FromDistrictId,
                FromWardCode   = _ghnOptions.FromWardCode,
                ToDistrictId   = order.ShippingDistrictId,
                ToWardCode     = order.ShippingWardCode,
                InsuranceValue = order.SubTotal,
                CodValue       = codAmount,
                Weight         = totalWeight,
                Length         = 30,
                Width          = 30,
                Height         = 10,
                ServiceId      = serviceId
            }, cancellationToken);

            if (feeResult.IsSuccess)
                actualFee = feeResult.Data!.Fee;
        }

        var now = DateTime.UtcNow;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // INSERT ShippingProviderTransaction
            await _unitOfWork.Orders.AddShippingTransactionAsync(new ShippingProviderTransaction
            {
                OrderId           = order.OrderId,
                Provider          = request.Provider.ToUpperInvariant(),
                ProviderOrderCode = ghnData.OrderCode,
                TrackingNumber    = ghnData.OrderCode,
                ServiceType       = request.ServiceType,
                Status            = "ready_to_pick",
                ShippingFee       = actualFee,
                CodAmount         = codAmount,
                EstimatedDelivery = estimatedDelivery,
                CreatedAt         = now,
                RowVersion        = []
            }, cancellationToken);

            // UPDATE Order
            order.StatusId          = shippedId;
            order.ShippedAt         = now;
            order.ActualShippingFee = actualFee > 0 ? actualFee : null;
            order.ShippingOrderCode = ghnData.OrderCode;
            order.UpdatedAt         = now;

            await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
            {
                OrderId   = order.OrderId,
                StatusId  = shippedId,
                ChangedBy = _currentUser.AccountId,
                Note      = request.Note,
                CreatedAt = now
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation(
                "Order {OrderId} shipped via {Provider}, tracking={Tracking}",
                orderId, request.Provider, ghnData.OrderCode);

            return Result<ShipOrderResponseDto>.Success(new ShipOrderResponseDto
            {
                TrackingNumber    = ghnData.OrderCode,
                ProviderOrderCode = ghnData.OrderCode,
                EstimatedDelivery = estimatedDelivery,
                ShippingFee       = actualFee
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

        var validation = await _cancelValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return validation.ToResult<CancelOrderResponseDto>();

        var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(cancellationToken);

        var order = await _unitOfWork.Orders.GetByIdForUpdateAsync(orderId, cancellationToken);
        if (order is null)
            return Result<CancelOrderResponseDto>.NotFound("Order", orderId);

        if (!OrderStatuses.CancellableStatuses.Contains(order.Status.StatusName))
            return Result<CancelOrderResponseDto>.UnprocessableEntity(
                $"Order is in status '{order.Status.StatusName}'; cancellation is only allowed for Pending or Confirmed orders.");

        if (!statusMap.TryGetValue(OrderStatuses.Cancelled, out var cancelledId))
            return Result<CancelOrderResponseDto>.Failure("CONFIGURATION_ERROR",
                "Status 'Cancelled' not found in database.");

        var now = DateTime.UtcNow;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            order.StatusId     = cancelledId;
            order.CancelledAt  = now;
            order.CancelReason = request.Reason;
            order.CancelledBy  = _currentUser.AccountId;
            order.UpdatedAt    = now;

            await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
            {
                OrderId   = order.OrderId,
                StatusId  = cancelledId,
                ChangedBy = _currentUser.AccountId,
                Note      = request.Reason,
                CreatedAt = now
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} cancelled by account {AccountId}",
                orderId, _currentUser.AccountId);

            return Result<CancelOrderResponseDto>.Success(new CancelOrderResponseDto
            {
                OrderId     = order.OrderId,
                CancelledAt = now
            });
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
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

        // Validate tai khoan dich ton tai va hop le
        var targetAccount = await _unitOfWork.Accounts.GetByIdAsync(request.TargetAccountId, cancellationToken);
        if (targetAccount is null || targetAccount.IsDeleted || !targetAccount.IsActive)
            return Result.Failure("NOT_FOUND",
                $"Account with ID '{request.TargetAccountId}' was not found or is inactive.");

        // Kiem tra role phu hop voi giai doan don hang
        var validationError = ValidateAssigneeRoleForOrderStage(
            order.Status.StatusName, targetAccount.Role?.RoleName ?? string.Empty);
        if (validationError is not null)
            return Result.UnprocessableEntity(validationError);

        var now = DateTime.UtcNow;
        var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(cancellationToken);

        if (!statusMap.TryGetValue(order.Status.StatusName, out var currentStatusId))
            return Result.Failure("CONFIGURATION_ERROR", "Current status not found in database.");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            order.AssignedToStaffId = request.TargetAccountId;
            order.UpdatedAt         = now;

            var note = string.IsNullOrWhiteSpace(request.Note)
                ? "Reassigned by Admin"
                : $"Reassigned by Admin: {request.Note}";

            await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
            {
                OrderId   = order.OrderId,
                StatusId  = currentStatusId,
                ChangedBy = _currentUser.AccountId,
                Note      = note,
                CreatedAt = now
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation(
                "Order {OrderId} reassigned to account {TargetId} by Admin {AdminId}",
                orderId, request.TargetAccountId, _currentUser.AccountId);

            return Result.Success();
        }
        catch
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
        // NOTE cho payment handler:
        // Goi method nay NGAY SAU KHI commit PaymentStatus = 'PAID'.
        // Neu method nay that bai, chi ghi log — KHONG rollback payment.
        // Thanh toan da thanh cong la thuc te; Staff co the confirm thu cong.

        try
        {
            var statusMap = await _unitOfWork.Orders.GetStatusMapAsync(cancellationToken);

            var order = await _unitOfWork.Orders.GetByIdForUpdateAsync(orderId, cancellationToken);
            if (order is null)
            {
                _logger.LogWarning("AutoConfirm: Order {OrderId} not found", orderId);
                return;
            }

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

            var now = DateTime.UtcNow;

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                order.StatusId          = confirmedId;
                order.ConfirmedAt       = now;
                order.UpdatedAt         = now;
                order.AssignedToStaffId = null;

                await _unitOfWork.Orders.AddStatusHistoryAsync(new OrderStatusHistory
                {
                    OrderId   = order.OrderId,
                    StatusId  = confirmedId,
                    ChangedBy = null,
                    Note      = "Auto-confirmed: payment received",
                    CreatedAt = now
                }, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Order {OrderId} auto-confirmed after payment", orderId);
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

    private bool IsStaffOrAdmin()   => _currentUser.RoleName is RoleStaff or RoleAdmin;
    private bool IsMerchandiseOrAdmin() => _currentUser.RoleName is RoleMerchandise or RoleAdmin;
    private bool IsAdmin()          => _currentUser.RoleName == RoleAdmin;

    private static IReadOnlyCollection<string> GetAllowedStatusesForRole(string roleName) =>
        roleName switch
        {
            RoleStaff       => OrderStatuses.StaffVisibleStatuses,
            RoleMerchandise => OrderStatuses.MerchandiseVisibleStatuses,
            _               => OrderStatuses.AdminVisibleStatuses
        };

    /// <summary>
    /// Tra ve thong bao loi neu role cua assignee khong phu hop voi giai doan don hang.
    /// Tra ve null neu hop le.
    /// </summary>
    private static string? ValidateAssigneeRoleForOrderStage(string statusName, string assigneeRole)
    {
        // Pending / Confirmed -> Staff hoac Admin
        if (statusName is OrderStatuses.Pending or OrderStatuses.Confirmed)
        {
            if (assigneeRole is not (RoleStaff or RoleAdmin or RoleMerchandise))
                return $"Cannot assign an order in status '{statusName}' to a '{assigneeRole}' account. Expected Staff, Merchandise, or Admin.";
            return null;
        }

        // Processing / Shipped / Delivering -> Merchandise hoac Admin
        if (statusName is OrderStatuses.Processing or OrderStatuses.Shipped or OrderStatuses.Delivering)
        {
            if (assigneeRole is not (RoleMerchandise or RoleAdmin))
                return $"Cannot assign an order in status '{statusName}' to a '{assigneeRole}' account. Expected Merchandise or Admin.";
            return null;
        }

        // Cac trang thai cuoi (Delivered, Completed, Cancelled) -> khong cho assign
        return $"Order in status '{statusName}' cannot be reassigned.";
    }
}
