using ToyStore.Domain.Entities;

namespace ToyStore.Application.Features.Notifications;

public static class OrderAssignmentNotificationHelper
{
    public static Dictionary<string, string> CreatePlaceholders(Order order)
    {
        return new Dictionary<string, string>
        {
            ["OrderCode"] = order.OrderCode,
            ["CustomerName"] = string.IsNullOrWhiteSpace(order.ShippingName) ? "Customer" : order.ShippingName.Trim(),
            ["TotalAmount"] = $"{order.TotalAmount:N0}",
        };
    }

    public static Dictionary<string, string> CreatePlaceholders(
        int orderId,
        string? orderCode,
        string? customerName = null,
        decimal? totalAmount = null)
    {
        return new Dictionary<string, string>
        {
            ["OrderCode"] = string.IsNullOrWhiteSpace(orderCode) ? $"#{orderId}" : orderCode,
            ["CustomerName"] = string.IsNullOrWhiteSpace(customerName) ? "Customer" : customerName.Trim(),
            ["TotalAmount"] = totalAmount.HasValue ? $"{totalAmount.Value:N0}" : "0",
        };
    }
}
