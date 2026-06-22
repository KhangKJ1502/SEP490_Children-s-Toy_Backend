namespace ToyStore.Application.DTOs.Refunds;

public class UpdateRefundStatusDto
{
    public string Status { get; set; } = null!;
    public string? RejectReason { get; set; }
    public string? ShippingOrderCode { get; set; }
    public string? ReturnShippingOrderCode { get; set; }
    public bool? InspectionPassed { get; set; }
    public string? InspectionNote { get; set; }
    public string? AdminNote { get; set; }
}
