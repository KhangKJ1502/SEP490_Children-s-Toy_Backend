using ToyStore.Application.DTOs.Addresses;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Services;

public interface IAddressService
{
    Task<Result<List<AddressDto>>> GetMyAddressesAsync(CancellationToken cancellationToken = default);
    Task<Result<AddressDto>> CreateMyAddressAsync(CreateAddressDto dto, CancellationToken cancellationToken = default);
    Task<Result<AddressDto>> UpdateMyAddressAsync(int addressId, UpdateAddressDto dto, CancellationToken cancellationToken = default);
    Task<Result> DeleteMyAddressAsync(int addressId, CancellationToken cancellationToken = default);
    Task<Result<List<ProvinceOptionDto>>> GetProvincesAsync(CancellationToken cancellationToken = default);
    Task<Result<List<DistrictOptionDto>>> GetDistrictsAsync(int provinceId, CancellationToken cancellationToken = default);
    Task<Result<List<WardOptionDto>>> GetWardsAsync(int districtId, CancellationToken cancellationToken = default);
}
