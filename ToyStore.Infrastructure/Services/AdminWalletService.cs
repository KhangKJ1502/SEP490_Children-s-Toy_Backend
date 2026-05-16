using FluentValidation;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Wallets;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.Infrastructure.Services;

public class AdminWalletService : IAdminWalletService
{
    private const string WalletStatusActive = "Active";
    private const string WalletStatusFrozen = "Frozen";
    private const string WalletStatusClosed = "Closed";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateWalletStatusDto> _updateWalletStatusValidator;

    public AdminWalletService(
        IUnitOfWork unitOfWork,
        IValidator<UpdateWalletStatusDto> updateWalletStatusValidator)
    {
        _unitOfWork = unitOfWork;
        _updateWalletStatusValidator = updateWalletStatusValidator;
    }

    public async Task<Result<PaginatedResponse<AdminWalletListDto>>> GetWalletsAsync(
        AdminWalletQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var status = NormalizeWalletStatus(query.Status);

        if (!string.IsNullOrWhiteSpace(status)
            && !IsKnownStatus(status))
        {
            return Result<PaginatedResponse<AdminWalletListDto>>.Failure(
                "VALIDATION_ERROR",
                "Status filter is invalid.");
        }

        var items = await _unitOfWork.Wallets.GetAdminPagedAsync(
            pageNumber,
            pageSize,
            NormalizeNullable(query.Account),
            status,
            cancellationToken);

        var totalCount = await _unitOfWork.Wallets.CountAdminAsync(
            NormalizeNullable(query.Account),
            status,
            cancellationToken);

        var mappedItems = items.Select(MapWallet).ToList();

        var response = new PaginatedResponse<AdminWalletListDto>(
            mappedItems,
            totalCount,
            pageNumber,
            pageSize);

        return Result<PaginatedResponse<AdminWalletListDto>>.Success(response);
    }

    public async Task<Result<AdminWalletListDto>> UpdateWalletStatusAsync(
        int walletId,
        UpdateWalletStatusDto dto,
        CancellationToken cancellationToken = default)
    {
        if (walletId <= 0)
        {
            return Result<AdminWalletListDto>.Failure("VALIDATION_ERROR", "Wallet ID must be greater than 0.");
        }

        var validationResult = await _updateWalletStatusValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

            return Result<AdminWalletListDto>.ValidationFailure(errors);
        }

        var wallet = await _unitOfWork.Wallets.GetByIdWithAccountAsync(walletId, cancellationToken);
        if (wallet == null)
        {
            return Result<AdminWalletListDto>.NotFound("Wallet", walletId);
        }

        if (string.Equals(wallet.Status, WalletStatusClosed, StringComparison.OrdinalIgnoreCase))
        {
            return Result<AdminWalletListDto>.BusinessError("Closed wallet cannot be reopened or modified.");
        }

        var nextStatus = NormalizeWalletStatus(dto.Status) ?? WalletStatusActive;
        var shouldReactivate = string.Equals(nextStatus, WalletStatusActive, StringComparison.OrdinalIgnoreCase);

        if (!string.Equals(wallet.Status, nextStatus, StringComparison.OrdinalIgnoreCase))
        {
            wallet.Status = nextStatus;
            wallet.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Wallets.UpdateWallet(wallet);
        }

        if (shouldReactivate)
        {
            var activePin = await _unitOfWork.Wallets.GetActivePinByWalletIdAsync(wallet.WalletId, cancellationToken);
            if (activePin != null)
            {
                activePin.FailedAttempts = 0;
                activePin.TotalFailedAttempts = 0;
                activePin.LockedUntil = null;
                activePin.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Wallets.UpdatePin(activePin);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AdminWalletListDto>.Success(MapWallet(wallet));
    }

    private static AdminWalletListDto MapWallet(Domain.Entities.Wallet wallet)
    {
        var accountName = wallet.Account.AccountName?.Trim() ?? string.Empty;
        var email = wallet.Account.Email?.Trim() ?? string.Empty;
        var accountDisplay = string.IsNullOrWhiteSpace(email)
            ? accountName
            : $"{accountName} ({email})";

        return new AdminWalletListDto
        {
            WalletId = wallet.WalletId,
            Account = accountDisplay,
            Status = wallet.Status,
            CreatedAt = wallet.CreatedAt,
            UpdatedAt = wallet.UpdatedAt
        };
    }

    private static string? NormalizeNullable(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static string? NormalizeWalletStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (string.Equals(trimmed, WalletStatusActive, StringComparison.OrdinalIgnoreCase))
        {
            return WalletStatusActive;
        }

        if (string.Equals(trimmed, WalletStatusFrozen, StringComparison.OrdinalIgnoreCase))
        {
            return WalletStatusFrozen;
        }

        if (string.Equals(trimmed, WalletStatusClosed, StringComparison.OrdinalIgnoreCase))
        {
            return WalletStatusClosed;
        }

        return trimmed;
    }

    private static bool IsKnownStatus(string status)
    {
        return string.Equals(status, WalletStatusActive, StringComparison.Ordinal)
            || string.Equals(status, WalletStatusFrozen, StringComparison.Ordinal)
            || string.Equals(status, WalletStatusClosed, StringComparison.Ordinal);
    }
}
