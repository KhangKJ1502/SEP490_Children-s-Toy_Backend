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
    private readonly IValidator<ChangePasswordDto> _changePasswordValidator;

    public ProfileService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ILogger<ProfileService> logger,
        IValidator<UpdateProfileDto> updateProfileValidator,
        IValidator<ChangePasswordDto> changePasswordValidator)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _logger = logger;
        _updateProfileValidator = updateProfileValidator;
        _changePasswordValidator = changePasswordValidator;
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

        var normalizedImageUrl = dto.ImageUrl != null
            ? NormalizeNullable(dto.ImageUrl)
            : existing.ImageUrl;
        var normalizedPhoneNumber = dto.PhoneNumber != null
            ? NormalizeNullable(dto.PhoneNumber)
            : existing.PhoneNumber;

        if (dto.PhoneNumber != null && normalizedPhoneNumber != null)
        {
            var isPhoneNumberExisted = await _unitOfWork.Accounts.ExistsByPhoneNumberAsync(
                normalizedPhoneNumber,
                accountId,
                cancellationToken);

            if (isPhoneNumberExisted)
            {
                return Result<ProfileDto>.ValidationFailure(new Dictionary<string, string[]>
                {
                    ["PhoneNumber"] = ["Phone number already exists."]
                });
            }
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {

            var updated = await _unitOfWork.Accounts.UpdateProfileAsync(
                accountId,
                normalizedImageUrl,
                normalizedPhoneNumber,
                cancellationToken);

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

    public async Task<Result> ChangeMyPasswordAsync(
        ChangePasswordDto dto,
        CancellationToken cancellationToken = default)
    {
        var accountId = _currentUserService.AccountId;
        if (accountId <= 0)
        {
            return Result.Failure("UNAUTHORIZED", "Unauthorized.");
        }

        var validationResult = await _changePasswordValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

            return Result.ValidationFailure(errors);
        }

        var existing = await _unitOfWork.Accounts.GetByIdForProfileAsync(accountId, cancellationToken);
        if (existing == null)
        {
            return Result.NotFound("Account", accountId);
        }

        if (!VerifyPassword(dto.CurrentPassword, existing.PasswordHash))
        {
            return Result.ValidationFailure(new Dictionary<string, string[]>
            {
                ["CurrentPassword"] = ["Current password is incorrect."]
            });
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _unitOfWork.Accounts.UpdatePasswordHashAsync(
                accountId,
                HashPassword(dto.NewPassword),
                cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            _logger.LogInformation("Password changed for account {AccountId}.", accountId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to change password for account {AccountId}.", accountId);
            throw;
        }
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
