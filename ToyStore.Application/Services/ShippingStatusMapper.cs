using Microsoft.Extensions.Logging;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Constants;
using ToyStore.Domain.Enums;

namespace ToyStore.Application.Services;

public class ShippingStatusMapper : IShippingStatusMapper
{
    private readonly ILogger<ShippingStatusMapper> _logger;

    public ShippingStatusMapper(ILogger<ShippingStatusMapper> logger)
    {
        _logger = logger;
    }

    public OrderStatus? MapToInternalStatus(string? providerStatus)
    {
        if (string.IsNullOrWhiteSpace(providerStatus)) return null;

        var status = providerStatus.ToLowerInvariant();

        switch (status)
        {
            // Shipped
            case ShippingStatuses.ReadyToPick:
            case ShippingStatuses.Picking:
            case ShippingStatuses.Picked:
            case ShippingStatuses.Storing:
            case ShippingStatuses.Sorting:
            case ShippingStatuses.Transporting:
                return OrderStatus.Shipped;

            // Delivering
            case ShippingStatuses.Delivering:
            case ShippingStatuses.MoneyCollectDelivering:
                return OrderStatus.Delivering;

            // Delivered
            case ShippingStatuses.Delivered:
                return OrderStatus.Delivered;

            // Cancelled
            case ShippingStatuses.Cancel:
            case ShippingStatuses.DeliveryFail:
            case ShippingStatuses.Lost:
            case ShippingStatuses.Damage:
            case ShippingStatuses.Exception:
            case string s when s.StartsWith("return"):
                return OrderStatus.Cancelled;

            default:
                _logger.LogWarning("Unknown shipping provider status encountered: {Status}", providerStatus);
                return null;
        }
    }

    public string GetStatusDescription(string? providerStatus)
    {
        if (string.IsNullOrWhiteSpace(providerStatus)) return "Đang xử lý";

        return providerStatus.ToLowerInvariant() switch
        {
            ShippingStatuses.ReadyToPick => "Chờ lấy hàng",
            ShippingStatuses.Picking => "Đang lấy hàng",
            ShippingStatuses.Picked => "Đã lấy hàng",
            ShippingStatuses.Storing => "Đang nhập kho",
            ShippingStatuses.Transporting => "Đang luân chuyển",
            ShippingStatuses.Sorting => "Đang phân loại",
            ShippingStatuses.Delivering => "Đang giao hàng",
            ShippingStatuses.MoneyCollectDelivering => "Đang giao hàng và thu tiền",
            ShippingStatuses.Delivered => "Giao hàng thành công",
            ShippingStatuses.DeliveryFail => "Giao hàng thất bại",
            ShippingStatuses.Cancel => "Đã hủy đơn vận chuyển",
            ShippingStatuses.WaitingToReturn => "Chờ chuyển hoàn",
            ShippingStatuses.Return => "Đang chuyển hoàn",
            ShippingStatuses.Returned => "Đã chuyển hoàn",
            ShippingStatuses.ReturnFail => "Chuyển hoàn thất bại",
            ShippingStatuses.ReturnSorting => "Đang phân loại hoàn hàng",
            ShippingStatuses.ReturnTransporting => "Đang luân chuyển hoàn hàng",
            ShippingStatuses.Returning => "Đang trong quá trình hoàn hàng",
            ShippingStatuses.Exception => "Đơn hàng gặp sự cố",
            ShippingStatuses.Damage => "Hàng bị hư hỏng",
            ShippingStatuses.Lost => "Hàng bị thất lạc",
            _ => providerStatus
        };
    }

    public string NormalizeStatus(string? providerStatus, OrderStatus currentInternalStatus)
    {
        // If we have a provider status, try to map it to a normalized business status
        var mapped = MapToInternalStatus(providerStatus);
        if (mapped.HasValue)
        {
            return mapped.Value.ToString();
        }

        // Fallback to current internal status if provider status is unknown or missing
        return currentInternalStatus.ToString();
    }
}
