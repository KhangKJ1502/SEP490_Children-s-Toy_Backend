namespace ToyStore.Application.DTOs.Customers;

public class CustomerDeliveryAbuseSummaryDto
{
    public int AccountId { get; set; }

    public int SuspiciousOrderCount { get; set; }

    public string? LastFailCode { get; set; }

    public DateTime? LastOrderDate { get; set; }
}
