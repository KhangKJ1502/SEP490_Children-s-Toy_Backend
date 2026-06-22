using ToyStore.Application.DTOs.Checkouts;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Services;

public interface IGhnClient
{
    Task<Result<FeeResponseDTO>> GetFeeAsync(FeeRequestDTO request, CancellationToken cancellationToken = default);
    Task<Result<LeadtimeResponseDTO>> GetLeadtimeAsync(LeadtimeRequestDTO request, CancellationToken cancellationToken = default);
    Task<Result<ShippingOrderCreateResponseDto>> CreateOrderAsync(ShippingOrderCreateRequestDto request, CancellationToken cancellationToken = default);
    Task<Result> CancelOrderAsync(string providerOrderCode, CancellationToken cancellationToken = default);
}
