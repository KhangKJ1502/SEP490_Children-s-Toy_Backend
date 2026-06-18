using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ToyStore.Domain.Entities;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Accounts;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.Infrastructure.Services;

public class AccountService : IAccountService
{
    private const byte StaffRoleId = 3;
    private const byte MerchandiserRoleId = 4;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AccountService> _logger;
    private readonly IValidator<CreateAccountDto> _createAccountValidator;
    private readonly IValidator<UpdateAccountInfoDto> _updateAccountInfoValidator;
    private readonly IValidator<UpdateAccountStatusDto> _updateAccountStatusValidator;
    private readonly IValidator<UpdateAccountPasswordDto> _updateAccountPasswordValidator;

    public AccountService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<AccountService> logger,
        IValidator<CreateAccountDto> createAccountValidator,
        IValidator<UpdateAccountInfoDto> updateAccountInfoValidator,
        IValidator<UpdateAccountStatusDto> updateAccountStatusValidator,
        IValidator<UpdateAccountPasswordDto> updateAccountPasswordValidator)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
        _createAccountValidator = createAccountValidator;
        _updateAccountInfoValidator = updateAccountInfoValidator;
        _updateAccountStatusValidator = updateAccountStatusValidator;
        _updateAccountPasswordValidator = updateAccountPasswordValidator;
    }

    public async Task<Result<PaginatedResponse<AccountListDto>>> GetAccountsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        byte? roleId = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
        {
            return Result<PaginatedResponse<AccountListDto>>.Failure("VALIDATION_ERROR", "Page number must be greater than 0.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return Result<PaginatedResponse<AccountListDto>>.Failure("VALIDATION_ERROR", "Page size must be between 1 and 100.");
        }

        var normalizedSearchTerm = NormalizeNullable(searchTerm);

        var items = await _unitOfWork.Accounts.GetPagedAsync(
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            normalizedSearchTerm,
            roleId,
            cancellationToken);

        var totalCount = await _unitOfWork.Accounts.CountAsync(
            normalizedSearchTerm,
            roleId,
            cancellationToken);

        var mappedItems = _mapper.Map<List<AccountListDto>>(items);

        var response = new PaginatedResponse<AccountListDto>(mappedItems, totalCount, pageNumber, pageSize);
        return Result<PaginatedResponse<AccountListDto>>.Success(response);
    }

    public async Task<Result<AccountDto>> GetAccountByIdAsync(
        int accountId,
        CancellationToken cancellationToken = default)
    {
        if (accountId <= 0)
        {
            return Result<AccountDto>.Failure("VALIDATION_ERROR", "Account ID must be greater than 0.");
        }

        var account = await _unitOfWork.Accounts.GetByIdAsync(accountId, cancellationToken);
        if (account == null)
        {
            return Result<AccountDto>.NotFound("Account", accountId);
        }

        return Result<AccountDto>.Success(_mapper.Map<AccountDto>(account));
    }

    public async Task<Result<AccountDto>> CreateAccountAsync(
        CreateAccountDto dto,
        CancellationToken cancellationToken = default)
    {
        dto.AccountName = dto.AccountName?.Trim() ?? string.Empty;

        var validationResult = await _createAccountValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

            return Result<AccountDto>.ValidationFailure(errors);
        }

        if (!IsAllowedCreateRole(dto.RoleId))
        {
            return Result<AccountDto>.ValidationFailure(new Dictionary<string, string[]>
            {
                { nameof(CreateAccountDto.RoleId), new[] { "Role ID must be either 3 (Staff) or 4 (Merchandiser)." } }
            });
        }

        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var existedEmail = await _unitOfWork.Accounts.ExistsByEmailAsync(normalizedEmail, cancellationToken);
        if (existedEmail)
        {
            return Result<AccountDto>.Conflict("Email already exists.");
        }

        var role = await _unitOfWork.Accounts.GetRoleByIdAsync(dto.RoleId, cancellationToken);
        if (role == null)
        {
            return Result<AccountDto>.NotFound("Role", dto.RoleId);
        }

        var generatedEmployeeCode = await GenerateEmployeeCodeAsync(cancellationToken);
        if (generatedEmployeeCode == null)
        {
            return Result<AccountDto>.Failure("EMPLOYEE_CODE_GENERATION_FAILED", "Unable to generate employee code. Please try again.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var created = await _unitOfWork.Accounts.CreateAsync(
                role.RoleId,
                generatedEmployeeCode,
                dto.AccountName.Trim(),
                NormalizeNullable(dto.PhoneNumber),
                normalizedEmail,
                HashPassword(dto.Password),
                true,
                "Local",
                cancellationToken,
                hasPassword: true);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation(
                "Account {AccountId} created successfully with role {RoleId}.",
                created.AccountId,
                created.RoleId);

            return Result<AccountDto>.Success(_mapper.Map<AccountDto>(created));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to create account with email {Email}.", dto.Email);
            throw;
        }
    }

    public async Task<Result<AccountDto>> UpdateAccountInfoAsync(
        int accountId,
        UpdateAccountInfoDto dto,
        CancellationToken cancellationToken = default)
    {
        if (accountId <= 0)
        {
            return Result<AccountDto>.Failure("VALIDATION_ERROR", "Account ID must be greater than 0.");
        }

        dto.AccountName = dto.AccountName?.Trim() ?? string.Empty;
        dto.PhoneNumber = NormalizeNullable(dto.PhoneNumber);

        var validationResult = await _updateAccountInfoValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

            return Result<AccountDto>.ValidationFailure(errors);
        }

        var existing = await _unitOfWork.Accounts.GetByIdForProfileAsync(accountId, cancellationToken);
        if (existing == null)
        {
            return Result<AccountDto>.NotFound("Account", accountId);
        }

        var nextIsActive = dto.IsActive!.Value;

        if (dto.PhoneNumber != null)
        {
            var isPhoneNumberExisted = await _unitOfWork.Accounts.ExistsByPhoneNumberAsync(
                dto.PhoneNumber,
                accountId,
                cancellationToken);

            if (isPhoneNumberExisted)
            {
                return Result<AccountDto>.ValidationFailure(new Dictionary<string, string[]>
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
                dto.AccountName,
                existing.ImageUrl,
                dto.PhoneNumber,
                existing.Dob,
                existing.SexId,
                cancellationToken);

            if (existing.IsActive != nextIsActive)
            {
                updated = await _unitOfWork.Accounts.UpdateStatusAsync(
                    accountId,
                    nextIsActive,
                    cancellationToken);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Account {AccountId} info updated successfully.", accountId);
            return Result<AccountDto>.Success(_mapper.Map<AccountDto>(updated));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to update account info for account {AccountId}.", accountId);
            throw;
        }
    }

    public async Task<Result<AccountDto>> UpdateAccountStatusAsync(
        int accountId,
        UpdateAccountStatusDto dto,
        CancellationToken cancellationToken = default)
    {
        if (accountId <= 0)
        {
            return Result<AccountDto>.Failure("VALIDATION_ERROR", "Account ID must be greater than 0.");
        }

        var validationResult = await _updateAccountStatusValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

            return Result<AccountDto>.ValidationFailure(errors);
        }

        var existing = await _unitOfWork.Accounts.GetByIdAsync(accountId, cancellationToken);
        if (existing == null)
        {
            return Result<AccountDto>.NotFound("Account", accountId);
        }

        if (existing.IsActive == dto.IsActive!.Value)
        {
            return Result<AccountDto>.Success(_mapper.Map<AccountDto>(existing));
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var updated = await _unitOfWork.Accounts.UpdateStatusAsync(
                accountId,
                dto.IsActive.Value,
                cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation(
                "Account {AccountId} status updated to {IsActive}.",
                accountId,
                updated.IsActive);

            return Result<AccountDto>.Success(_mapper.Map<AccountDto>(updated));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to update status for account {AccountId}.", accountId);
            throw;
        }
    }

    public async Task<Result> UpdateAccountPasswordAsync(
        int accountId,
        UpdateAccountPasswordDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(_currentUserService.RoleName, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure("FORBIDDEN", "Only admin can update account password.");
        }

        if (accountId <= 0)
        {
            return Result.Failure("VALIDATION_ERROR", "Account ID must be greater than 0.");
        }

        var validationResult = await _updateAccountPasswordValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

            return Result.ValidationFailure(errors);
        }

        var existing = await _unitOfWork.Accounts.GetByIdAsync(accountId, cancellationToken);
        if (existing == null)
        {
            return Result.NotFound("Account", accountId);
        }

        if (!IsAllowedCreateRole(existing.RoleId))
        {
            return Result.Failure("VALIDATION_ERROR", "Admin can only update password for Staff or Merchandise accounts.");
        }

        if (VerifyPassword(dto.NewPassword, existing.PasswordHash))
        {
            return Result.ValidationFailure(new Dictionary<string, string[]>
            {
                [nameof(UpdateAccountPasswordDto.NewPassword)] = ["New password must be different from current password."]
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
            _logger.LogInformation("Admin {AdminId} updated password for account {AccountId}.", _currentUserService.AccountId, accountId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to update password for account {AccountId}.", accountId);
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

    private static bool IsAllowedCreateRole(byte roleId)
    {
        return roleId == StaffRoleId || roleId == MerchandiserRoleId;
    }

    private async Task<string?> GenerateEmployeeCodeAsync(CancellationToken cancellationToken)
    {
        const int maxAttempts = 100;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var code = $"{RandomNumberGenerator.GetInt32(0, 10_000):D4}{GenerateTwoUppercaseLetters()}";
            var existed = await _unitOfWork.Accounts.ExistsByEmployeeCodeAsync(code, cancellationToken);
            if (!existed)
            {
                return code;
            }
        }

        return null;
    }

    private static string GenerateTwoUppercaseLetters()
    {
        Span<char> chars = stackalloc char[2];
        chars[0] = (char)('A' + RandomNumberGenerator.GetInt32(0, 26));
        chars[1] = (char)('A' + RandomNumberGenerator.GetInt32(0, 26));
        return new string(chars);
    }

    private static string HashPassword(string password)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(hashBytes);
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var hash = HashPassword(password);
        return string.Equals(hash, storedHash, StringComparison.OrdinalIgnoreCase);
    }

}
