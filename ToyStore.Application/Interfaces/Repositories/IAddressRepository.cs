using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

public interface IAddressRepository
{
    Task<List<Address>> GetActiveByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);
    Task<Address?> GetActiveByIdAsync(int addressId, CancellationToken cancellationToken = default);
    Task<int> CountActiveByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);
    Task AddAsync(Address address, CancellationToken cancellationToken = default);
    Task<Province?> GetProvinceByIdAsync(int provinceId, CancellationToken cancellationToken = default);
    Task<District?> GetDistrictByIdAsync(int districtId, CancellationToken cancellationToken = default);
    Task<Ward?> GetWardByCodeAsync(string wardCode, CancellationToken cancellationToken = default);
    Task<List<Province>> GetProvincesAsync(CancellationToken cancellationToken = default);
    Task<List<District>> GetDistrictsByProvinceIdAsync(int provinceId, CancellationToken cancellationToken = default);
    Task<List<Ward>> GetWardsByDistrictIdAsync(int districtId, CancellationToken cancellationToken = default);
}
