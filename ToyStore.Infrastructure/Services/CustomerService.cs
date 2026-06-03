using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Customers;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private const byte CustomerRoleId = 1;
    private const int CodRestrictionThreshold = 2;
    private const int ManualBlockThreshold = 3;
    private const string StatusNormal = "NORMAL";
    private const string StatusCodProbation = "COD_PROBATION";
    private const string StatusPendingReview = "PENDING_ADMIN_REVIEW";
    private const string StatusManuallyBlocked = "MANUALLY_BLOCKED";
    private const string StatusAppealApprovedStrict = "APPEAL_APPROVED_STRICT";
    private const string StatusPermanentBlocked = "PERMANENT_BLOCKED";
    private readonly IUnitOfWork _unitOfWork;
    private readonly SEP490ToyStoreContext _db;
    private readonly IMapper _mapper;
    private readonly ILogger<CustomerService> _logger;
    private readonly IValidator<UpdateCustomerDto> _updateCustomerValidator;
    private readonly ICurrentUserService _currentUserService;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly ITimeProvider _timeProvider;

    public CustomerService(
        IUnitOfWork unitOfWork,
        SEP490ToyStoreContext db,
        IMapper mapper,
        ILogger<CustomerService> logger,
        IValidator<UpdateCustomerDto> updateCustomerValidator,
        ICurrentUserService currentUserService,
        INotificationDispatcher notificationDispatcher,
        ITimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _db = db;
        _mapper = mapper;
        _logger = logger;
        _updateCustomerValidator = updateCustomerValidator;
        _currentUserService = currentUserService;
        _notificationDispatcher = notificationDispatcher;
        _timeProvider = timeProvider;
    }

    public async Task<Result<PaginatedResponse<CustomerListDto>>> GetCustomersAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
        {
            return Result<PaginatedResponse<CustomerListDto>>.Failure("VALIDATION_ERROR", "Page number must be greater than 0.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return Result<PaginatedResponse<CustomerListDto>>.Failure("VALIDATION_ERROR", "Page size must be between 1 and 100.");
        }

        var normalizedSearchTerm = NormalizeNullable(searchTerm);

        var customers = await _unitOfWork.Accounts.GetPagedAsync(
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            normalizedSearchTerm,
            CustomerRoleId,
            cancellationToken);

        var totalCount = await _unitOfWork.Accounts.CountAsync(
            normalizedSearchTerm,
            CustomerRoleId,
            cancellationToken);

        var mappedItems = _mapper.Map<List<CustomerListDto>>(customers);
        await EnrichDeliveryAbuseSummariesAsync(mappedItems, cancellationToken);
        var response = new PaginatedResponse<CustomerListDto>(mappedItems, totalCount, pageNumber, pageSize);
        return Result<PaginatedResponse<CustomerListDto>>.Success(response);
    }

    public async Task<Result<CustomerDetailDto>> GetCustomerByIdAsync(
        int accountId,
        CancellationToken cancellationToken = default)
    {
        if (accountId <= 0)
        {
            return Result<CustomerDetailDto>.Failure("VALIDATION_ERROR", "Account ID must be greater than 0.");
        }

        var customer = await _unitOfWork.Accounts.GetByIdAsync(accountId, cancellationToken);
        if (customer == null || customer.RoleId != CustomerRoleId)
        {
            return Result<CustomerDetailDto>.NotFound("Customer", accountId);
        }

        var dto = _mapper.Map<CustomerDetailDto>(customer);
        await EnrichDeliveryAbuseSummaryAsync(dto, cancellationToken);
        return Result<CustomerDetailDto>.Success(dto);
    }

    public async Task<Result<CustomerDetailDto>> UpdateCustomerAsync(
        int accountId,
        UpdateCustomerDto dto,
        CancellationToken cancellationToken = default)
    {
        if (accountId <= 0)
        {
            return Result<CustomerDetailDto>.Failure("VALIDATION_ERROR", "Account ID must be greater than 0.");
        }

        var validationResult = await _updateCustomerValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

            return Result<CustomerDetailDto>.ValidationFailure(errors);
        }

        var existingCustomer = await _unitOfWork.Accounts.GetByIdAsync(accountId, cancellationToken);
        if (existingCustomer == null || existingCustomer.RoleId != CustomerRoleId)
        {
            return Result<CustomerDetailDto>.NotFound("Customer", accountId);
        }

        var nextIsActive = dto.IsActive!.Value;
        if (existingCustomer.IsActive == nextIsActive)
        {
            return Result<CustomerDetailDto>.Success(_mapper.Map<CustomerDetailDto>(existingCustomer));
        }

        if (nextIsActive
            && await _db.CustomerDeliveryAbuseCases
                .AsNoTracking()
                .AnyAsync(x => x.AccountId == accountId && x.Status == StatusPermanentBlocked, cancellationToken))
        {
            return Result<CustomerDetailDto>.BusinessError(
                "This customer is permanently locked for delivery abuse and cannot be reactivated.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _unitOfWork.Accounts.UpdateStatusAsync(accountId, nextIsActive, cancellationToken);
            if (nextIsActive)
            {
                await MarkDeliveryAbuseAppealApprovedAsync(accountId, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            var updated = await _unitOfWork.Accounts.GetByIdAsync(accountId, cancellationToken);
            if (updated == null || updated.RoleId != CustomerRoleId)
            {
                return Result<CustomerDetailDto>.NotFound("Customer", accountId);
            }

            var updatedDto = _mapper.Map<CustomerDetailDto>(updated);
            await EnrichDeliveryAbuseSummaryAsync(updatedDto, cancellationToken);

            _logger.LogInformation("Customer {AccountId} status updated to {IsActive}.", accountId, nextIsActive);
            return Result<CustomerDetailDto>.Success(updatedDto);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to update customer status for {AccountId}.", accountId);
            throw;
        }
    }

    public async Task<Result<CustomerDetailDto>> BlockCustomerForDeliveryAbuseAsync(
        int accountId,
        ManualBlockCustomerDto dto,
        CancellationToken cancellationToken = default)
    {
        if (accountId <= 0)
        {
            return Result<CustomerDetailDto>.Failure("VALIDATION_ERROR", "Account ID must be greater than 0.");
        }

        var customer = await _db.Accounts
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.AccountId == accountId && !x.IsDeleted, cancellationToken);

        if (customer is null || customer.RoleId != CustomerRoleId)
        {
            return Result<CustomerDetailDto>.NotFound("Customer", accountId);
        }

        if (!customer.IsActive)
        {
            return Result<CustomerDetailDto>.BusinessError("This customer account is already inactive.");
        }

        var abuseSummary = (await _unitOfWork.Accounts.GetDeliveryAbuseSummariesAsync(
                new[] { accountId },
                cancellationToken))
            .FirstOrDefault();

        if (abuseSummary is null || abuseSummary.SuspiciousOrderCount < ManualBlockThreshold)
        {
            return Result<CustomerDetailDto>.BusinessError(
                "This customer has not reached the manual lock threshold yet.");
        }

        var now = _timeProvider.UtcNow;
        var adminId = _currentUserService.AccountId == 0 ? (int?)null : _currentUserService.AccountId;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            customer.IsActive = false;
            customer.UpdatedAt = now;

            var abuseCase = await GetOrCreateDeliveryAbuseCaseAsync(accountId, now, cancellationToken);
            if (abuseCase.Status is StatusPermanentBlocked)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result<CustomerDetailDto>.BusinessError(
                    "This customer is permanently locked for delivery abuse and cannot appeal.");
            }

            var note = string.IsNullOrWhiteSpace(dto.Note)
                ? "Manually locked by admin after reaching 3 unpaid COD delivery-abuse orders."
                : dto.Note.Trim();

            abuseCase.Status = StatusManuallyBlocked;
            abuseCase.WarningLevel = ManualBlockThreshold;
            abuseCase.SuspiciousOrderCount = abuseSummary.SuspiciousOrderCount;
            abuseCase.LastGHNFailCode = abuseSummary.LastFailCode;
            abuseCase.LastSuspiciousOrderDate = abuseSummary.LastOrderDate;
            abuseCase.ReviewRequestedAt ??= now;
            abuseCase.BlockedAt = now;
            abuseCase.BlockedBy = adminId;
            abuseCase.Note = $"{note} Suspicious orders: {abuseSummary.SuspiciousOrderCount}. Last GHN fail code: {abuseSummary.LastFailCode ?? "N/A"}.";
            abuseCase.UpdatedAt = now;

            await _db.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to manually block delivery-abuse customer {AccountId}.", accountId);
            throw;
        }

        await NotifyCustomerBlockedAsync(customer, abuseSummary.SuspiciousOrderCount, cancellationToken);

        var updated = await _unitOfWork.Accounts.GetByIdAsync(accountId, cancellationToken);
        var updatedDto = _mapper.Map<CustomerDetailDto>(updated);
        await EnrichDeliveryAbuseSummaryAsync(updatedDto, cancellationToken);
        return Result<CustomerDetailDto>.Success(updatedDto);
    }

    private static string? NormalizeNullable(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private async Task EnrichDeliveryAbuseSummariesAsync(
        List<CustomerListDto> customers,
        CancellationToken cancellationToken)
    {
        var accountIds = customers.Select(x => x.AccountId).ToArray();
        var summaries = await _unitOfWork.Accounts.GetDeliveryAbuseSummariesAsync(accountIds, cancellationToken);
        var summaryByAccountId = summaries.ToDictionary(x => x.AccountId);

        foreach (var customer in customers)
        {
            if (!summaryByAccountId.TryGetValue(customer.AccountId, out var summary))
            {
                continue;
            }

            customer.SuspiciousDeliveryFailOrderCount = summary.SuspiciousOrderCount;
            customer.IsCodRestricted = IsCodRestricted(summary);
            customer.IsManualBlockRecommended = customer.IsActive && IsManualBlockRecommended(summary);
            customer.IsSuspiciousDeliveryAbuse = summary.SuspiciousOrderCount >= ManualBlockThreshold
                || summary.PolicyStatus is StatusPendingReview or StatusManuallyBlocked or StatusPermanentBlocked;
            customer.LastSuspiciousGHNFailCode = summary.LastFailCode;
            customer.LastSuspiciousOrderDate = summary.LastOrderDate;
        }
    }

    private async Task EnrichDeliveryAbuseSummaryAsync(
        CustomerDetailDto customer,
        CancellationToken cancellationToken)
    {
        var summaries = await _unitOfWork.Accounts.GetDeliveryAbuseSummariesAsync(
            new[] { customer.AccountId },
            cancellationToken);
        var summary = summaries.FirstOrDefault();
        if (summary is null)
        {
            return;
        }

        customer.SuspiciousDeliveryFailOrderCount = summary.SuspiciousOrderCount;
        customer.IsCodRestricted = IsCodRestricted(summary);
        customer.IsManualBlockRecommended = customer.IsActive && IsManualBlockRecommended(summary);
        customer.IsSuspiciousDeliveryAbuse = summary.SuspiciousOrderCount >= ManualBlockThreshold
            || summary.PolicyStatus is StatusPendingReview or StatusManuallyBlocked or StatusPermanentBlocked;
        customer.LastSuspiciousGHNFailCode = summary.LastFailCode;
        customer.LastSuspiciousOrderDate = summary.LastOrderDate;
    }

    private static bool IsCodRestricted(CustomerDeliveryAbuseSummaryDto summary)
    {
        if (summary.PolicyStatus is StatusCodProbation)
        {
            return false;
        }

        return summary.PolicyStatus is StatusPendingReview
            or StatusManuallyBlocked
            or StatusAppealApprovedStrict
            or StatusPermanentBlocked
            || summary.SuspiciousOrderCount >= CodRestrictionThreshold;
    }

    private static bool IsManualBlockRecommended(CustomerDeliveryAbuseSummaryDto summary)
    {
        return summary.PolicyStatus is StatusPendingReview
            || summary.SuspiciousOrderCount >= ManualBlockThreshold;
    }

    private async Task<CustomerDeliveryAbuseCase> GetOrCreateDeliveryAbuseCaseAsync(
        int accountId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var abuseCase = await _db.CustomerDeliveryAbuseCases
            .FirstOrDefaultAsync(x => x.AccountId == accountId, cancellationToken);

        if (abuseCase is not null)
        {
            return abuseCase;
        }

        abuseCase = new CustomerDeliveryAbuseCase
        {
            AccountId = accountId,
            Status = StatusPendingReview,
            WarningLevel = 0,
            SuspiciousOrderCount = 0,
            CreatedAt = now
        };

        _db.CustomerDeliveryAbuseCases.Add(abuseCase);
        return abuseCase;
    }

    private async Task MarkDeliveryAbuseAppealApprovedAsync(
        int accountId,
        CancellationToken cancellationToken)
    {
        var abuseCase = await _db.CustomerDeliveryAbuseCases
            .FirstOrDefaultAsync(x => x.AccountId == accountId, cancellationToken);

        if (abuseCase is null || abuseCase.Status is not StatusManuallyBlocked)
        {
            return;
        }

        var now = _timeProvider.UtcNow;
        abuseCase.Status = StatusAppealApprovedStrict;
        abuseCase.AppealDecision = "APPROVED";
        abuseCase.AppealReviewedAt = now;
        abuseCase.AppealReviewedBy = _currentUserService.AccountId == 0 ? null : _currentUserService.AccountId;
        abuseCase.StrictPeriodUntil = now.AddMonths(1);
        abuseCase.CountingFrom = now;
        abuseCase.Note = "Appeal accepted. Customer is active again under stricter delivery-abuse monitoring for one month.";
        abuseCase.UpdatedAt = now;
    }

    private Task NotifyCustomerBlockedAsync(
        Account customer,
        int suspiciousOrderCount,
        CancellationToken cancellationToken)
    {
        return _notificationDispatcher.DispatchAsync(new NotificationContext
        {
            RecipientAccountId = customer.AccountId,
            RecipientType = RecipientTypes.Customer,
            NotificationType = NotificationTypes.System,
            Title = "Your account has been locked",
            Message =
                $"Your account has been locked after {suspiciousOrderCount} unpaid COD orders reached repeated delivery failures. " +
                "If you believe this decision is incorrect, please contact our support team by email for review. " +
                "Repeated abuse after an accepted appeal may result in a permanent, non-appealable lock.",
            SendBell = true,
            SendEmail = true,
            ActionTarget = "/profile/orders",
            Payload = new Dictionary<string, object>
            {
                ["accountId"] = customer.AccountId,
                ["suspiciousOrderCount"] = suspiciousOrderCount,
                ["policy"] = "DELIVERY_ABUSE_MANUAL_LOCK"
            },
            IdempotencyKey = $"customer-delivery-abuse-manual-lock:{customer.AccountId}:{suspiciousOrderCount}"
        }, cancellationToken);
    }
}
