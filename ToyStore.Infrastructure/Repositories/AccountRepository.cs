using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class AccountRepository : IAccountRepository
{
    private const byte StaffRoleId = 3;
    private const byte MerchandiseRoleId = 4;

    private readonly SEP490ToyStoreContext _context;

    public AccountRepository(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    public async Task<List<Account>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        byte? roleId = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Account> query = _context.Accounts
            .AsNoTracking()
            .Include(x => x.Role)
            .Where(x => !x.IsDeleted && (x.RoleId == StaffRoleId || x.RoleId == MerchandiseRoleId));

        if (roleId.HasValue)
        {
            query = query.Where(x => x.RoleId == roleId.Value);
        }

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
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(
        string? searchTerm = null,
        byte? roleId = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Account> query = _context.Accounts
            .AsNoTracking()
            .Where(x => !x.IsDeleted && (x.RoleId == StaffRoleId || x.RoleId == MerchandiseRoleId));

        if (roleId.HasValue)
        {
            query = query.Where(x => x.RoleId == roleId.Value);
        }

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

    public Task<Account?> GetByIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return _context.Accounts
            .AsNoTracking()
            .Include(x => x.Role)
            .Include(x => x.Sex)
            .Where(x => x.AccountId == accountId && !x.IsDeleted)
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

    public Task<bool> ExistsByPhoneNumberAsync(
        string phoneNumber,
        int excludeAccountId,
        CancellationToken cancellationToken = default)
    {
        var normalizedPhoneNumber = phoneNumber.Trim();
        return _context.Accounts
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.AccountId != excludeAccountId && x.PhoneNumber != null)
            .AnyAsync(x => x.PhoneNumber == normalizedPhoneNumber, cancellationToken);
    }

    public Task<Role?> GetRoleByIdAsync(byte roleId, CancellationToken cancellationToken = default)
    {
        return _context.Roles
            .AsNoTracking()
            .Where(x => x.RoleId == roleId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Account> CreateAsync(
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

        await _context.Entry(entity).Reference(x => x.Role).LoadAsync(cancellationToken);

        return entity;
    }

    public async Task<Account> UpdateStatusAsync(
        int accountId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Accounts
            .Include(x => x.Role)
            .FirstAsync(x => x.AccountId == accountId && !x.IsDeleted, cancellationToken);

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await _context.Entry(entity).Reference(x => x.Role).LoadAsync(cancellationToken);
        return entity;
    }

    public Task<Account?> GetByEmailForAuthAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return _context.Accounts
            .AsNoTracking()
            .Include(x => x.Role)
            .Where(x => x.Email.ToLower() == normalizedEmail)
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

    public async Task UpdateProviderAsync(int accountId, string? provider, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Accounts
            .FirstAsync(x => x.AccountId == accountId && !x.IsDeleted, cancellationToken);

        entity.Provider = provider;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<Account?> GetByIdForProfileAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return _context.Accounts
            .Include(x => x.Role)
            .Include(x => x.Sex)
            .Where(x => x.AccountId == accountId && !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Account> UpdateProfileAsync(
        int accountId,
        string? accountName,
        string? imageUrl,
        string? phoneNumber,
        DateTime? dob,
        byte? sexId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Accounts
            .Include(x => x.Role)
            .Include(x => x.Sex)
            .FirstAsync(x => x.AccountId == accountId && !x.IsDeleted, cancellationToken);

        entity.AccountName = accountName ?? entity.AccountName;
        entity.ImageUrl = imageUrl;
        entity.PhoneNumber = phoneNumber;
        entity.Dob = dob;
        entity.SexId = sexId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        await _context.Entry(entity).Reference(x => x.Sex).LoadAsync(cancellationToken);
        return entity;
    }

    public Task<List<Account>> GetByRoleIdsAsync(byte[] roleIds, CancellationToken cancellationToken = default)
    {
        return _context.Accounts
            .AsNoTracking()
            .Where(a => a.IsActive && !a.IsDeleted && roleIds.Contains(a.RoleId))
            .ToListAsync(cancellationToken);
    }

    public Task<List<Account>> GetActiveCustomersAsync(CancellationToken cancellationToken = default)
    {
        // Customer role is assumed to be RoleId 1; adjust if schema differs
        return _context.Accounts
            .AsNoTracking()
            .Where(a => a.IsActive && !a.IsDeleted && a.RoleId == 1)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountActiveCustomersByIdsAsync(
        IReadOnlyCollection<int> accountIds,
        CancellationToken cancellationToken = default)
    {
        if (accountIds.Count == 0)
            return Task.FromResult(0);

        return _context.Accounts
            .AsNoTracking()
            .Where(a => accountIds.Contains(a.AccountId) && a.IsActive && !a.IsDeleted && a.RoleId == 1)
            .CountAsync(cancellationToken);
    }

}
