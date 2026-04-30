using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Common.Models;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Infrastructure.Data;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly SEP490ToyStoreContext _context;

    public AccountRepository(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    public async Task<List<AccountModel>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Account> query = _context.Accounts
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Include(x => x.Role);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalizedSearchTerm = searchTerm.Trim();
            query = query.Where(x =>
                x.AccountName.Contains(normalizedSearchTerm) ||
                (x.PhoneNumber != null && x.PhoneNumber.Contains(normalizedSearchTerm)) ||
                x.Email.Contains(normalizedSearchTerm));
        }

        query = (sortBy?.Trim().ToLowerInvariant(), sortDesc) switch
        {
            ("accountname", true) => query.OrderByDescending(x => x.AccountName),
            ("accountname", false) => query.OrderBy(x => x.AccountName),
            ("phonenumber", true) => query.OrderByDescending(x => x.PhoneNumber),
            ("phonenumber", false) => query.OrderBy(x => x.PhoneNumber),
            ("email", true) => query.OrderByDescending(x => x.Email),
            ("email", false) => query.OrderBy(x => x.Email),
            ("rolename", true) => query.OrderByDescending(x => x.Role.RoleName),
            ("rolename", false) => query.OrderBy(x => x.Role.RoleName),
            ("isactive", true) => query.OrderByDescending(x => x.IsActive),
            ("isactive", false) => query.OrderBy(x => x.IsActive),
            ("createdat", true) => query.OrderByDescending(x => x.CreatedAt),
            ("createdat", false) => query.OrderBy(x => x.CreatedAt),
            (_, true) => query.OrderByDescending(x => x.AccountId),
            _ => query.OrderBy(x => x.AccountId)
        };

        return await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AccountModel
            {
                AccountId = x.AccountId,
                RoleId = x.RoleId,
                RoleName = x.Role.RoleName,
                EmployeeCode = x.EmployeeCode,
                AccountName = x.AccountName,
                PhoneNumber = x.PhoneNumber,
                Email = x.Email,
                ImageUrl = x.ImageUrl,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                Provider = x.Provider,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Account> query = _context.Accounts
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalizedSearchTerm = searchTerm.Trim();
            query = query.Where(x =>
                x.AccountName.Contains(normalizedSearchTerm) ||
                (x.PhoneNumber != null && x.PhoneNumber.Contains(normalizedSearchTerm)) ||
                x.Email.Contains(normalizedSearchTerm));
        }

        return query.CountAsync(cancellationToken);
    }

    public Task<AccountModel?> GetByIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return _context.Accounts
            .AsNoTracking()
            .Where(x => x.AccountId == accountId && !x.IsDeleted)
            .Select(x => new AccountModel
            {
                AccountId = x.AccountId,
                RoleId = x.RoleId,
                RoleName = x.Role.RoleName,
                EmployeeCode = x.EmployeeCode,
                AccountName = x.AccountName,
                PhoneNumber = x.PhoneNumber,
                Email = x.Email,
                ImageUrl = x.ImageUrl,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                Provider = x.Provider,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return _context.Accounts
            .AsNoTracking()
            .AnyAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken);
    }

    public Task<bool> ExistsByEmployeeCodeAsync(string employeeCode, CancellationToken cancellationToken = default)
    {
        var normalizedEmployeeCode = employeeCode.Trim().ToLowerInvariant();
        return _context.Accounts
            .AsNoTracking()
            .Where(x => x.EmployeeCode != null)
            .AnyAsync(x => x.EmployeeCode!.ToLower() == normalizedEmployeeCode, cancellationToken);
    }

    public Task<AccountRoleModel?> GetRoleByIdAsync(byte roleId, CancellationToken cancellationToken = default)
    {
        return _context.Roles
            .AsNoTracking()
            .Where(x => x.RoleId == roleId)
            .Select(x => new AccountRoleModel
            {
                RoleId = x.RoleId,
                RoleName = x.RoleName
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<AccountModel> CreateAsync(
        byte roleId,
        string? employeeCode,
        string accountName,
        string? phoneNumber,
        string email,
        string passwordHash,
        bool isActive,
        string? provider,
        CancellationToken cancellationToken = default)
    {
        var entity = new Account
        {
            RoleId = roleId,
            EmployeeCode = employeeCode,
            AccountName = accountName,
            PhoneNumber = phoneNumber,
            Email = email,
            PasswordHash = passwordHash,
            IsActive = isActive,
            IsDeleted = false,
            Provider = provider,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Accounts.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var roleName = await _context.Roles
            .AsNoTracking()
            .Where(x => x.RoleId == roleId)
            .Select(x => x.RoleName)
            .FirstAsync(cancellationToken);

        return new AccountModel
        {
            AccountId = entity.AccountId,
            RoleId = entity.RoleId,
            RoleName = roleName,
            EmployeeCode = entity.EmployeeCode,
            AccountName = entity.AccountName,
            PhoneNumber = entity.PhoneNumber,
            Email = entity.Email,
            ImageUrl = entity.ImageUrl,
            IsActive = entity.IsActive,
            IsDeleted = entity.IsDeleted,
            Provider = entity.Provider,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public async Task<AccountModel> UpdateStatusAsync(
        int accountId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Accounts
            .FirstAsync(x => x.AccountId == accountId && !x.IsDeleted, cancellationToken);

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var roleName = await _context.Roles
            .AsNoTracking()
            .Where(x => x.RoleId == entity.RoleId)
            .Select(x => x.RoleName)
            .FirstAsync(cancellationToken);

        return new AccountModel
        {
            AccountId = entity.AccountId,
            RoleId = entity.RoleId,
            RoleName = roleName,
            EmployeeCode = entity.EmployeeCode,
            AccountName = entity.AccountName,
            PhoneNumber = entity.PhoneNumber,
            Email = entity.Email,
            ImageUrl = entity.ImageUrl,
            IsActive = entity.IsActive,
            IsDeleted = entity.IsDeleted,
            Provider = entity.Provider,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public Task<AccountAuthModel?> GetByEmailForAuthAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return _context.Accounts
            .AsNoTracking()
            .Where(x => x.Email.ToLower() == normalizedEmail)
            .Select(x => new AccountAuthModel
            {
                AccountId = x.AccountId,
                RoleId = x.RoleId,
                RoleName = x.Role.RoleName,
                AccountName = x.AccountName,
                Email = x.Email,
                ImageUrl = x.ImageUrl,
                PasswordHash = x.PasswordHash,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpdatePasswordHashAsync(int accountId, string passwordHash, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Accounts
            .FirstAsync(x => x.AccountId == accountId && !x.IsDeleted, cancellationToken);

        entity.PasswordHash = passwordHash;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
