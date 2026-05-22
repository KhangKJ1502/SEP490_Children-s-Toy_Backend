using ToyStore.Application.DTOs.Checkouts;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Services;

public interface IGhnClient
{
    Task<Result<FeeResponseDTO>> GetFeeAsync(FeeRequestDTO request, CancellationToken cancellationToken = default);
    Task<Result<LeadtimeResponseDTO>> GetLeadtimeAsync(LeadtimeRequestDTO request, CancellationToken cancellationToken = default);
    Task<Result<ShippingOrderCreateResponseDto>> CreateOrderAsync(ShippingOrderCreateRequestDto request, CancellationToken cancellationToken = default);
    Task<Result<int>> ResolveServiceIdAsync(int toDistrictId, int? preferredServiceTypeId = null, CancellationToken cancellationToken = default);

    Task<Result<List<GhnProvinceDto>>> GetProvincesAsync(CancellationToken cancellationToken = default);
    Task<Result<List<GhnDistrictDto>>> GetDistrictsAsync(int provinceId, CancellationToken cancellationToken = default);
    Task<Result<List<GhnWardDto>>> GetWardsAsync(int districtId, CancellationToken cancellationToken = default);
}
