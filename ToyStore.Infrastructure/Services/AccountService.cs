using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Accounts;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Application.Validators.Accounts;

namespace ToyStore.Infrastructure.Services;

public class AccountService : IAccountService
{
    private const byte StaffRoleId = 3;
    private const byte MerchandiserRoleId = 4;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AccountService> _logger;
    private readonly CreateAccountValidator _createAccountValidator;
    private readonly UpdateAccountStatusValidator _updateAccountStatusValidator;

    public AccountService(IUnitOfWork unitOfWork, ILogger<AccountService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _createAccountValidator = new CreateAccountValidator();
        _updateAccountStatusValidator = new UpdateAccountStatusValidator();
    }

    public async Task<Result<PaginatedResponse<AccountListDto>>> GetAccountsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
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
            cancellationToken);

        var totalCount = await _unitOfWork.Accounts.CountAsync(
            normalizedSearchTerm,
            cancellationToken);

        var mappedItems = items.Select(MapToListDto).ToList();

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

        return Result<AccountDto>.Success(MapToDto(account));
    }

    public async Task<Result<AccountDto>> CreateAccountAsync(
        CreateAccountDto dto,
        CancellationToken cancellationToken = default)
    {
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
                cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation(
                "Account {AccountId} created successfully with role {RoleId}.",
                created.AccountId,
                created.RoleId);

            return Result<AccountDto>.Success(MapToDto(created));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to create account with email {Email}.", dto.Email);
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
            return Result<AccountDto>.Success(MapToDto(existing));
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

            return Result<AccountDto>.Success(MapToDto(updated));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to update status for account {AccountId}.", accountId);
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

    private static AccountListDto MapToListDto(AccountModel model)
    {
        return new AccountListDto
        {
            AccountId = model.AccountId,
            AccountName = model.AccountName,
            PhoneNumber = model.PhoneNumber,
            Email = model.Email,
            ImageUrl = model.ImageUrl,
            RoleId = model.RoleId,
            RoleName = model.RoleName,
            IsActive = model.IsActive,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt
        };
    }

    private static AccountDto MapToDto(AccountModel model)
    {
        return new AccountDto
        {
            AccountId = model.AccountId,
            RoleId = model.RoleId,
            RoleName = model.RoleName,
            EmployeeCode = model.EmployeeCode,
            AccountName = model.AccountName,
            PhoneNumber = model.PhoneNumber,
            Email = model.Email,
            IsActive = model.IsActive,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt
        };
    }
}
