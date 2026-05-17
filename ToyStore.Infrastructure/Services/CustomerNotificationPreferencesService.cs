using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs.Profiles;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Services;

public class CustomerNotificationPreferencesService : ICustomerNotificationPreferencesService
{
    private const byte CustomerRoleId = 1;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CustomerNotificationPreferencesService(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<CustomerNotificationPreferencesDto>> GetMyAsync(CancellationToken cancellationToken = default)
    {
        if (_currentUser.AccountId <= 0)
            return Result<CustomerNotificationPreferencesDto>.Failure("UNAUTHORIZED", "Unauthorized.");

        if (_currentUser.RoleId != CustomerRoleId)
            return Result<CustomerNotificationPreferencesDto>.Failure("FORBIDDEN", "Only customer can access this resource.");

        var prefs = await _unitOfWork.UserPreferences.GetByAccountIdAsync(_currentUser.AccountId, cancellationToken);

        if (prefs is null)
        {
            return Result<CustomerNotificationPreferencesDto>.Success(DefaultDtoForMissingRow());
        }

        return Result<CustomerNotificationPreferencesDto>.Success(Map(prefs));
    }

    public async Task<Result<CustomerNotificationPreferencesDto>> UpdateMyAsync(
        UpdateCustomerNotificationPreferencesDto dto,
        CancellationToken cancellationToken = default)
    {
        if (_currentUser.AccountId <= 0)
            return Result<CustomerNotificationPreferencesDto>.Failure("UNAUTHORIZED", "Unauthorized.");

        if (_currentUser.RoleId != CustomerRoleId)
            return Result<CustomerNotificationPreferencesDto>.Failure("FORBIDDEN", "Only customer can access this resource.");

        if (dto is null)
            return Result<CustomerNotificationPreferencesDto>.Failure("VALIDATION_ERROR", "Request body is required.");

        var updated = await _unitOfWork.UserPreferences.UpsertForAccountAsync(
            _currentUser.AccountId,
            dto,
            cancellationToken);

        return Result<CustomerNotificationPreferencesDto>.Success(Map(updated));
    }

    private static CustomerNotificationPreferencesDto Map(UserPreference p) => new()
    {
        EmailOptIn = p.EmailOptIn,
        WebPushOptIn = p.WebPushOptIn,
        OrderUpdates = p.OrderUpdates,
        Promotions = p.Promotions,
        StockAlerts = p.StockAlerts,
        BlogAlerts = p.BlogAlerts,
    };

    /// <summary>Matches database defaults when trigger did not run (legacy accounts).</summary>
    private static CustomerNotificationPreferencesDto DefaultDtoForMissingRow() => new()
    {
        EmailOptIn = true,
        WebPushOptIn = false,
        OrderUpdates = true,
        Promotions = true,
        StockAlerts = true,
        BlogAlerts = true,
    };
}
