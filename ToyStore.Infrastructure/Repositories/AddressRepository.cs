using Microsoft.EntityFrameworkCore;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class AddressRepository : IAddressRepository
{
    private readonly SEP490ToyStoreContext _context;

    public AddressRepository(SEP490ToyStoreContext context)
    {
        _context = context;
    }

    public Task<List<Address>> GetActiveByAccountIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return _context.Addresses
            .Include(x => x.Province)
            .Include(x => x.District)
            .Include(x => x.WardCodeNavigation)
            .Where(x => x.AccountId == accountId && !x.IsDeleted)
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.AddressId)
            .ToListAsync(cancellationToken);
    }

    public Task<Address?> GetActiveByIdAsync(int addressId, CancellationToken cancellationToken = default)
    {
        return _context.Addresses
            .Include(x => x.Province)
            .Include(x => x.District)
            .Include(x => x.WardCodeNavigation)
            .FirstOrDefaultAsync(x => x.AddressId == addressId && !x.IsDeleted, cancellationToken);
    }

    public Task<int> CountActiveByAccountIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return _context.Addresses.CountAsync(x => x.AccountId == accountId && !x.IsDeleted, cancellationToken);
    }

    public Task AddAsync(Address address, CancellationToken cancellationToken = default)
    {
        return _context.Addresses.AddAsync(address, cancellationToken).AsTask();
    }

    public Task<Province?> GetProvinceByIdAsync(int provinceId, CancellationToken cancellationToken = default)
    {
        return _context.Provinces.FirstOrDefaultAsync(x => x.ProvinceId == provinceId, cancellationToken);
    }

    public Task<District?> GetDistrictByIdAsync(int districtId, CancellationToken cancellationToken = default)
    {
        return _context.Districts.FirstOrDefaultAsync(x => x.DistrictId == districtId, cancellationToken);
    }

    public Task<Ward?> GetWardByCodeAsync(string wardCode, CancellationToken cancellationToken = default)
    {
        return _context.Wards.FirstOrDefaultAsync(x => x.WardCode == wardCode, cancellationToken);
    }

    public Task<List<Province>> GetProvincesAsync(CancellationToken cancellationToken = default)
    {
        return _context.Provinces
            .AsNoTracking()
            .OrderBy(x => x.ProvinceName)
            .ToListAsync(cancellationToken);
    }

    public Task<List<District>> GetDistrictsByProvinceIdAsync(int provinceId, CancellationToken cancellationToken = default)
    {
        return _context.Districts
            .AsNoTracking()
            .Where(x => x.ProvinceId == provinceId)
            .OrderBy(x => x.DistrictName)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Ward>> GetWardsByDistrictIdAsync(int districtId, CancellationToken cancellationToken = default)
    {
        return _context.Wards
            .AsNoTracking()
            .Where(x => x.DistrictId == districtId)
            .OrderBy(x => x.WardName)
            .ToListAsync(cancellationToken);
    }
}
