using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ToyStore.Application.DTOs.Profiles;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Services;

public class ProfileService : IProfileService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly ILogger<ProfileService> _logger;
    private readonly IValidator<UpdateProfileDto> _updateProfileValidator;

    public ProfileService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ILogger<ProfileService> logger,
        IValidator<UpdateProfileDto> updateProfileValidator)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _logger = logger;
        _updateProfileValidator = updateProfileValidator;
    }

    public async Task<Result<ProfileDto>> GetMyProfileAsync(CancellationToken cancellationToken = default)
    {
        var accountId = _currentUserService.AccountId;
        if (accountId <= 0)
        {
            return Result<ProfileDto>.Failure("UNAUTHORIZED", "Unauthorized.");
        }

        var account = await _unitOfWork.Accounts.GetByIdAsync(accountId, cancellationToken);
        if (account == null)
        {
            return Result<ProfileDto>.NotFound("Account", accountId);
        }

        return Result<ProfileDto>.Success(_mapper.Map<ProfileDto>(account));
    }

    public async Task<Result<ProfileDto>> UpdateMyProfileAsync(
        UpdateProfileDto dto,
        CancellationToken cancellationToken = default)
    {
        var accountId = _currentUserService.AccountId;
        if (accountId <= 0)
        {
            return Result<ProfileDto>.Failure("UNAUTHORIZED", "Unauthorized.");
        }

        var validationResult = await _updateProfileValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

            return Result<ProfileDto>.ValidationFailure(errors);
        }

        var existing = await _unitOfWork.Accounts.GetByIdForProfileAsync(accountId, cancellationToken);
        if (existing == null)
        {
            return Result<ProfileDto>.NotFound("Account", accountId);
        }

        var isPasswordChangeRequested = HasPasswordChangeRequest(dto);
        if (isPasswordChangeRequested && !VerifyPassword(dto.CurrentPassword!, existing.PasswordHash))
        {
            return Result<ProfileDto>.Failure("INVALID_CREDENTIALS", "Current password is incorrect.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var normalizedImageUrl = NormalizeNullable(dto.ImageUrl);
            var normalizedPhoneNumber = NormalizeNullable(dto.PhoneNumber);

            var updated = await _unitOfWork.Accounts.UpdateProfileAsync(
                accountId,
                normalizedImageUrl,
                normalizedPhoneNumber,
                cancellationToken);

            if (isPasswordChangeRequested)
            {
                await _unitOfWork.Accounts.UpdatePasswordHashAsync(
                    accountId,
                    HashPassword(dto.NewPassword!),
                    cancellationToken);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Profile updated for account {AccountId}.", accountId);
            return Result<ProfileDto>.Success(_mapper.Map<ProfileDto>(updated));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to update profile for account {AccountId}.", accountId);
            throw;
        }
    }


    private static bool HasPasswordChangeRequest(UpdateProfileDto dto)
    {
        return !string.IsNullOrWhiteSpace(dto.CurrentPassword)
               || !string.IsNullOrWhiteSpace(dto.NewPassword)
               || !string.IsNullOrWhiteSpace(dto.ConfirmNewPassword);
    }

    private static string? NormalizeNullable(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var hash = HashPassword(password);
        return string.Equals(hash, storedHash, StringComparison.OrdinalIgnoreCase);
    }

    private static string HashPassword(string password)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(hashBytes);
    }

}
