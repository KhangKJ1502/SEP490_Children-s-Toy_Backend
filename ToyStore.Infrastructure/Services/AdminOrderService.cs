using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToyStore.Application.Common.Models;
using ToyStore.Application.Constants;
using ToyStore.Domain.Entities;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Checkouts;
using ToyStore.Application.DTOs.Orders;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Constants;
using ToyStore.Application.Common.Helpers;
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
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ILogger<AdminOrderService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly IOrderLifecycleService _orderLifecycle;
    private readonly IOrderAccessService _orderAccess;

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
        IDomainEventPublisher eventPublisher,
        ILogger<AdminOrderService> logger,
        ITimeProvider timeProvider,
        IOrderLifecycleService orderLifecycle,
        IOrderAccessService orderAccess)
    {
        _unitOfWork      = unitOfWork;
        _currentUser     = currentUser;
        _mapper          = mapper;
        _ghnClient       = ghnClient;
        _ghnOptions      = ghnOptions.Value;
        _shopAddress     = shopAddress.Value;
        _shipValidator   = shipValidator;
        _cancelValidator = cancelValidator;
        _assignValidator = assignValidator;
        _eventPublisher  = eventPublisher;
        _logger          = logger;
        _timeProvider    = timeProvider;
        _orderLifecycle  = orderLifecycle;
        _orderAccess     = orderAccess;
    }

    // ── UC1: Danh sach don hang ───────────────────────────────────────────────

    public async Task<Result<PaginatedResponse<AdminOrderListItemDto>>> GetListAsync(
        AdminOrderQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var isPrivileged = _orderAccess.IsPrivileged(_currentUser.RoleId);
        var restrictToAssignment = !isPrivileged && query.AssignedToMe;
        var assignmentRoleId = _orderAccess.GetRequiredAssignmentRoleId(_currentUser.RoleId);

        var allowedStatuses = isPrivileged
            ? OrderStatuses.AdminVisibleStatuses
            : query.AssignedToMe
                ? OrderStatuses.AdminVisibleStatuses
                : GetAllowedStatusesForRole(_currentUser.RoleName);

        var pageSize = Math.Min(query.PageSize, 100);
        var pageNumber = Math.Max(query.PageNumber, 1);

        var statusIds = query.StatusIds is { Count: > 0 }
            ? (IReadOnlyCollection<int>?)query.StatusIds
            : null;

        var items = await _unitOfWork.Orders.GetAdminPagedAsync(
            allowedStatuses,
            pageNumber,
            pageSize,
            query.StatusId,
            statusIds,
            restrictToAssignment,
            _currentUser.AccountId,
            assignmentRoleId,
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
            query.Keyword,
            query.FromDate,
            query.ToDate,
            cancellationToken);

        var dtos = _mapper.Map<List<AdminOrderListItemDto>>(items);

        if (dtos.Count > 0)
        {
            var orderIds = dtos.Select(x => x.OrderId).ToList();
            var activeAssignments = await _unitOfWork.OrderAssignments.GetActiveAssignmentsForOrdersAsync(orderIds, cancellationToken);

            foreach (var dto in dtos)
            {
                ApplyAssignmentNamesToDto(
                    dto,
                    activeAssignments.Where(a => a.OrderId == dto.OrderId));
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
        ApplyAssignmentNamesToDto(dto, activeAssignments);

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

        var access = await _orderAccess.EnsureCanMutateAsync(orderId, OrderMutation.Confirm, cancellationToken);
        if (access.IsFailure)
            return Result<ConfirmOrderResponseDto>.Failure(access.ErrorCode!, access.ErrorMessage!);

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

        var now = _timeProvider.UtcNow;

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

            // Publish: customer notification + merchandise ready-to-pack
            var orderPayload = new { orderId = order.OrderId, orderCode = order.OrderCode };
            await _eventPublisher.PublishAsync("Order", order.OrderId.ToString(), NotificationEventTypes.OrderConfirmed, orderPayload, CancellationToken.None);
            await _eventPublisher.PublishAsync("Order", order.OrderId.ToString(), NotificationEventTypes.MerchReadyToPack, orderPayload, CancellationToken.None);

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

        var access = await _orderAccess.EnsureCanMutateAsync(orderId, OrderMutation.Process, cancellationToken);
        if (access.IsFailure)
            return Result<ProcessOrderResponseDto>.Failure(access.ErrorCode!, access.ErrorMessage!);

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

        var now = _timeProvider.UtcNow;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Phan cong thuc te nam o OrderAssignments; khong ghi de AssignedToStaffId bang Merch.
            order.StatusId = processingId;
            order.UpdatedAt = now;

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

            await _eventPublisher.PublishAsync("Order", order.OrderId.ToString(), NotificationEventTypes.OrderPacking,
                new { orderId = order.OrderId, orderCode = order.OrderCode }, CancellationToken.None);

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

        // Lấy chi tiết các item kèm kích thước, cân nặng thực tế cho don hang
        var shippingItems = await _unitOfWork.Orders.GetShippingItemsForOrderAsync(orderId, cancellationToken);
        var package = GhnPackageCalculator.Calculate(
            shippingItems,
            _ghnOptions.DefaultItemWeight,
            _ghnOptions.DefaultLength,
            _ghnOptions.DefaultWidth,
            _ghnOptions.DefaultHeight);

        // Xay dung request goi GHN
        var codAmount = string.Equals(order.PaymentMethod, "SHIP_COD", StringComparison.OrdinalIgnoreCase)
            ? order.TotalAmount
            : 0m;

        // Resolve service_type_id: honour admin's explicit choice first, otherwise use package.ServiceTypeId
        int serviceTypeId = package.ServiceTypeId;
        if (int.TryParse(request.ServiceType, out var parsedServiceTypeId) && parsedServiceTypeId > 0)
        {
            serviceTypeId = parsedServiceTypeId;
        }

        var ghnRequest = new ShippingOrderCreateRequestDto
        {
            ClientOrderCode = order.OrderCode,
            ToName          = order.ShippingName,
            ToPhone         = order.ShippingPhone,
            ToAddress       = order.ShippingAddress,
            ToDistrictId    = order.ShippingDistrictId,
            ToWardCode      = order.ShippingWardCode,
            ServiceTypeId   = serviceTypeId,
            InsuranceValue  = 0m,
            CodAmount       = codAmount,
            Weight          = Math.Max(package.Weight, 1),
            Length          = package.Length,
            Width           = package.Width,
            Height          = package.Height,
            Note            = request.Note,
            RequiredNote    = !string.IsNullOrWhiteSpace(request.RequiredNote) ? request.RequiredNote : "KHONGCHOXEMHANG",
            Items           = package.Items.Select(x => new ShippingOrderCreateItemDto
            {
                Name     = x.Name,
                Code     = x.Code,
                Quantity = x.Quantity,
                Price    = x.Price,
                Weight   = x.Weight,
                Length   = x.Length,
                Width    = x.Width,
                Height   = x.Height,
                Category = x.Category
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
        DateTime? estimatedDelivery = ghnData.ExpectedDeliveryTime;
        if (estimatedDelivery is null)
        {
            var leadtimeResult = await _ghnClient.GetLeadtimeAsync(new LeadtimeRequestDTO
            {
                FromDistrictId = _ghnOptions.FromDistrictId,
                FromWardCode   = _ghnOptions.FromWardCode,
                ToDistrictId   = order.ShippingDistrictId,
                ToWardCode     = order.ShippingWardCode,
                ServiceTypeId  = serviceTypeId
            }, cancellationToken);

            if (leadtimeResult.IsSuccess && leadtimeResult.Data!.LeadtimeUnix > 0)
                estimatedDelivery = leadtimeResult.Data!.EstimatedDeliveryTime;
        }

        // Fetch the actual fee
        decimal actualFee = ghnData.TotalFee;
        if (actualFee <= 0)
        {
            var feeResult = await _ghnClient.GetFeeAsync(new FeeRequestDTO
            {
                FromDistrictId = _ghnOptions.FromDistrictId,
                FromWardCode   = _ghnOptions.FromWardCode,
                ToDistrictId   = order.ShippingDistrictId,
                ToWardCode     = order.ShippingWardCode,
                InsuranceValue = 0m,
                CodValue       = codAmount,
                Weight         = Math.Max(package.Weight, 1),
                Length         = package.Length,
                Width          = package.Width,
                Height         = package.Height,
                ServiceTypeId  = package.ServiceTypeId
            }, cancellationToken);

            if (feeResult.IsSuccess)
                actualFee = feeResult.Data!.Fee;
        }

        var now = _timeProvider.UtcNow;

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
            // Sync EstimatedShippingFee with the real GHN fee so customers see the accurate price
            if (actualFee > 0)
                order.EstimatedShippingFee = actualFee;
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

            await _eventPublisher.PublishAsync("Order", order.OrderId.ToString(), NotificationEventTypes.OrderShipped,
                new { orderId = order.OrderId, orderCode = order.OrderCode, trackingNumber = ghnData.OrderCode }, CancellationToken.None);

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

        if (!OrderStatuses.CancellableStatuses.Contains(order.Status.StatusName))
            return Result<CancelOrderResponseDto>.UnprocessableEntity(
                $"Order is in status '{order.Status.StatusName}'; cancellation is only allowed for Pending or Confirmed orders.");

        if (!statusMap.TryGetValue(OrderStatuses.Cancelled, out var cancelledId))
            return Result<CancelOrderResponseDto>.Failure("CONFIGURATION_ERROR",
                "Status 'Cancelled' not found in database.");

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
            OrderId     = order.OrderId,
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

        var assignmentRoleId = ResolveOrderAssignmentRoleId(targetAccount.RoleId, order.Status.StatusName);
        if (assignmentRoleId is null)
        {
            return Result.Failure("VALIDATION_ERROR",
                "Cannot determine assignment role for the target account and order status.");
        }

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

        try
        {
            if (hasExistingForRole)
            {
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
                var schedule = await _unitOfWork.WorkSchedules.GetByIdForUpdateAsync(
                    onDutySchedule.ScheduleId, cancellationToken);
                if (schedule?.StaffShiftCapacity is null)
                {
                    return Result.Failure("CONFIGURATION_ERROR", "Shift capacity not found for target schedule.");
                }

                if (schedule.StaffShiftCapacity.CurrentLoad >= schedule.StaffShiftCapacity.MaxLoad)
                {
                    return Result.Failure("BUSINESS_RULE_VIOLATION", "Target schedule is at full capacity.");
                }

                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                try
                {
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
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
                }
                catch
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    throw;
                }
            }

            if (assignmentRoleId == OrderAccessRoles.AssignmentStaff)
            {
                order.AssignedToStaffId = request.TargetAccountId;
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

            _logger.LogInformation(
                "Order {OrderId} assigned to account {TargetId} (OA role {RoleId}) by Admin {AdminId}",
                orderId, request.TargetAccountId, assignmentRoleId, _currentUser.AccountId);

            await _eventPublisher.PublishAsync("Order", order.OrderId.ToString(), NotificationEventTypes.StaffOrderAssigned,
                new { orderId = order.OrderId, orderCode = order.OrderCode, targetAccountId = request.TargetAccountId }, CancellationToken.None);

            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure("BUSINESS_RULE_VIOLATION", ex.Message);
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

            var now = _timeProvider.UtcNow;

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                order.StatusId    = confirmedId;
                order.ConfirmedAt = now;
                order.UpdatedAt   = now;

                var staffAssignment = await _unitOfWork.OrderAssignments.GetActiveAssignmentsAsync(orderId, cancellationToken);
                var staffRow = staffAssignment.FirstOrDefault(a => a.RoleId == OrderAccessRoles.AssignmentStaff);
                order.AssignedToStaffId = staffRow?.AccountId;

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

                // Notify customer + merchandise team
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
    private static void ApplyAssignmentNamesToDto(
        AdminOrderListItemDto dto,
        IEnumerable<OrderAssignment> assignments)
    {
        var staffAssig = assignments.FirstOrDefault(a => a.RoleId == OrderAccessRoles.AssignmentStaff);
        var merchAssig = assignments.FirstOrDefault(a => a.RoleId == OrderAccessRoles.AssignmentMerchandise);

        if (staffAssig != null)
        {
            dto.AssignedToStaffId = staffAssig.AccountId;
            dto.AssignedToStaffName = staffAssig.Account?.AccountName;
        }

        if (merchAssig != null)
        {
            dto.AssignedToMerchId = merchAssig.AccountId;
            dto.AssignedToMerchName = merchAssig.Account?.AccountName;
        }
    }

    private static void ApplyAssignmentNamesToDto(
        AdminOrderDetailDto dto,
        IEnumerable<OrderAssignment> assignments)
    {
        var staffAssig = assignments.FirstOrDefault(a => a.RoleId == OrderAccessRoles.AssignmentStaff);
        var merchAssig = assignments.FirstOrDefault(a => a.RoleId == OrderAccessRoles.AssignmentMerchandise);

        if (staffAssig != null)
        {
            dto.AssignedToStaffId = staffAssig.AccountId;
            dto.AssignedToStaffName = staffAssig.Account?.AccountName;
        }

        if (merchAssig != null)
        {
            dto.AssignedToMerchId = merchAssig.AccountId;
            dto.AssignedToMerchName = merchAssig.Account?.AccountName;
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
